namespace ThreeDGod.Core.Editing;

public interface IEditCommand
{
    Guid CommandId { get; }
    string DescriptionResourceKey { get; }
    DateTime Timestamp { get; }
    IReadOnlyList<Guid> AffectedObjectIds { get; }
    Task ExecuteAsync(CancellationToken cancellationToken = default);
    Task UndoAsync(CancellationToken cancellationToken = default);
    bool TryMerge(IEditCommand next);
}

public sealed class CompositeCommand : IEditCommand
{
    private readonly List<IEditCommand> _commands;

    public CompositeCommand(string descriptionResourceKey, IEnumerable<IEditCommand> commands)
    {
        DescriptionResourceKey = descriptionResourceKey;
        _commands = commands.ToList();
        if (_commands.Count == 0)
            throw new ArgumentException("Composite command requires at least one child.");
        CommandId = Guid.NewGuid();
        Timestamp = DateTime.UtcNow;
        AffectedObjectIds = _commands.SelectMany(c => c.AffectedObjectIds).Distinct().ToArray();
    }

    public Guid CommandId { get; }
    public string DescriptionResourceKey { get; }
    public DateTime Timestamp { get; }
    public IReadOnlyList<Guid> AffectedObjectIds { get; }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        foreach (var command in _commands)
            await command.ExecuteAsync(cancellationToken);
    }

    public async Task UndoAsync(CancellationToken cancellationToken = default)
    {
        for (var i = _commands.Count - 1; i >= 0; i--)
            await _commands[i].UndoAsync(cancellationToken);
    }

    public bool TryMerge(IEditCommand next) => false;
}

public sealed class PropertyChangeCommand : IEditCommand
{
    private readonly Action<object?> _apply;
    private readonly object? _oldValue;
    private object? _newValue;
    private readonly string _propertyKey;

    public PropertyChangeCommand(
        Guid targetId,
        string propertyKey,
        object? oldValue,
        object? newValue,
        Action<object?> apply,
        string descriptionResourceKey = "edit.property")
    {
        CommandId = Guid.NewGuid();
        Timestamp = DateTime.UtcNow;
        AffectedObjectIds = [targetId];
        DescriptionResourceKey = descriptionResourceKey;
        _propertyKey = propertyKey;
        _oldValue = oldValue;
        _newValue = newValue;
        _apply = apply;
    }

    public Guid CommandId { get; }
    public string DescriptionResourceKey { get; }
    public DateTime Timestamp { get; }
    public IReadOnlyList<Guid> AffectedObjectIds { get; }
    public string PropertyKey => _propertyKey;

    public Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _apply(_newValue);
        return Task.CompletedTask;
    }

    public Task UndoAsync(CancellationToken cancellationToken = default)
    {
        _apply(_oldValue);
        return Task.CompletedTask;
    }

    public bool TryMerge(IEditCommand next)
    {
        if (next is not PropertyChangeCommand other)
            return false;
        if (other.AffectedObjectIds.Count != 1 || AffectedObjectIds.Count != 1)
            return false;
        if (other.AffectedObjectIds[0] != AffectedObjectIds[0])
            return false;
        if (other._propertyKey != _propertyKey)
            return false;
        _newValue = other._newValue;
        return true;
    }
}

public sealed class CollectionChangeCommand<T> : IEditCommand
{
    private readonly IList<T> _target;
    private readonly T _item;
    private readonly bool _add;

    public CollectionChangeCommand(Guid ownerId, IList<T> target, T item, bool add, string descriptionResourceKey)
    {
        CommandId = Guid.NewGuid();
        Timestamp = DateTime.UtcNow;
        AffectedObjectIds = [ownerId];
        DescriptionResourceKey = descriptionResourceKey;
        _target = target;
        _item = item;
        _add = add;
    }

    public Guid CommandId { get; }
    public string DescriptionResourceKey { get; }
    public DateTime Timestamp { get; }
    public IReadOnlyList<Guid> AffectedObjectIds { get; }

    public Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (_add)
            _target.Add(_item);
        else
            _target.Remove(_item);
        return Task.CompletedTask;
    }

    public Task UndoAsync(CancellationToken cancellationToken = default)
    {
        if (_add)
            _target.Remove(_item);
        else
            _target.Add(_item);
        return Task.CompletedTask;
    }

    public bool TryMerge(IEditCommand next) => false;
}

public sealed class CommandStack
{
    private readonly List<IEditCommand> _undo = [];
    private readonly List<IEditCommand> _redo = [];
    private readonly List<IEditCommand> _openTransaction = [];
    private IEditCommand? _preview;
    private bool _transactionOpen;

    public int UndoCount => _undo.Count;
    public int RedoCount => _redo.Count;
    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    public void BeginTransaction()
    {
        if (_transactionOpen)
            throw new InvalidOperationException("A transaction is already open.");
        _transactionOpen = true;
        _openTransaction.Clear();
    }

    public async Task AbortTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (!_transactionOpen)
            return;
        _transactionOpen = false;
        for (var i = _openTransaction.Count - 1; i >= 0; i--)
            await _openTransaction[i].UndoAsync(cancellationToken);
        _openTransaction.Clear();
    }

    public async Task CommitTransactionAsync(string descriptionResourceKey = "edit.composite", CancellationToken cancellationToken = default)
    {
        if (!_transactionOpen)
            throw new InvalidOperationException("No open transaction.");
        _transactionOpen = false;
        if (_openTransaction.Count == 0)
            return;
        var composite = new CompositeCommand(descriptionResourceKey, _openTransaction.ToArray());
        _openTransaction.Clear();
        _undo.Add(composite);
        _redo.Clear();
        await Task.CompletedTask;
    }

    public void BeginPreview(IEditCommand command)
    {
        _preview = command;
    }

    public async Task UpdatePreviewAsync(IEditCommand command, CancellationToken cancellationToken = default)
    {
        if (_preview is null)
        {
            _preview = command;
            await command.ExecuteAsync(cancellationToken);
            return;
        }

        if (_preview.TryMerge(command))
        {
            await _preview.ExecuteAsync(cancellationToken);
            return;
        }

        await command.ExecuteAsync(cancellationToken);
        _preview = command;
    }

    public async Task CommitPreviewAsync(CancellationToken cancellationToken = default)
    {
        if (_preview is null)
            return;
        if (_transactionOpen)
            _openTransaction.Add(_preview);
        else
        {
            _undo.Add(_preview);
            _redo.Clear();
        }
        _preview = null;
        await Task.CompletedTask;
    }

    public async Task ExecuteAsync(IEditCommand command, CancellationToken cancellationToken = default)
    {
        await command.ExecuteAsync(cancellationToken);
        if (_transactionOpen)
        {
            _openTransaction.Add(command);
            return;
        }
        _undo.Add(command);
        _redo.Clear();
    }

    public async Task UndoAsync(CancellationToken cancellationToken = default)
    {
        if (_undo.Count == 0)
            return;
        var command = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);
        await command.UndoAsync(cancellationToken);
        _redo.Add(command);
    }

    public async Task RedoAsync(CancellationToken cancellationToken = default)
    {
        if (_redo.Count == 0)
            return;
        var command = _redo[^1];
        _redo.RemoveAt(_redo.Count - 1);
        await command.ExecuteAsync(cancellationToken);
        _undo.Add(command);
    }
}
