using ThreeDGod.Core.Editing;

namespace ThreeDGodCreator.Core.Tests;

public class CommandStackTests
{
    [Fact]
    public async Task HundredSliderPreviewUpdates_Commit_YieldsExactlyOneUndo()
    {
        var target = new SliderTarget { Value = 0 };
        var stack = new CommandStack();
        var id = Guid.NewGuid();
        var start = 0;
        stack.BeginPreview(new PropertyChangeCommand(id, "height", start, 0, v => target.Value = Convert.ToInt32(v)));
        for (var i = 1; i <= 100; i++)
        {
            await stack.UpdatePreviewAsync(new PropertyChangeCommand(id, "height", target.Value, i, v => target.Value = Convert.ToInt32(v)));
        }
        await stack.CommitPreviewAsync();
        Assert.Equal(100, target.Value);
        Assert.Equal(1, stack.UndoCount);
        await stack.UndoAsync();
        Assert.Equal(0, target.Value);
        Assert.Equal(0, stack.UndoCount);
        Assert.Equal(1, stack.RedoCount);
        await stack.RedoAsync();
        Assert.Equal(100, target.Value);
    }

    [Fact]
    public async Task PropertyAddRemoveAndComposite_Work()
    {
        var bag = new List<string>();
        var owner = Guid.NewGuid();
        var stack = new CommandStack();
        var name = "hero";
        await stack.ExecuteAsync(new PropertyChangeCommand(owner, "name", "", "hero", v => name = (string)v!));
        await stack.ExecuteAsync(new CollectionChangeCommand<string>(owner, bag, "sword", add: true, "edit.add"));
        await stack.ExecuteAsync(new CollectionChangeCommand<string>(owner, bag, "sword", add: false, "edit.remove"));

        Assert.Equal("hero", name);
        Assert.Empty(bag);

        await stack.UndoAsync();
        Assert.Contains("sword", bag);
        await stack.UndoAsync();
        Assert.Empty(bag);
        await stack.UndoAsync();
        Assert.Equal("", name);

        stack.BeginTransaction();
        await stack.ExecuteAsync(new CollectionChangeCommand<string>(owner, bag, "a", true, "edit.add"));
        await stack.ExecuteAsync(new CollectionChangeCommand<string>(owner, bag, "b", true, "edit.add"));
        await stack.CommitTransactionAsync("edit.composite");
        Assert.Equal(2, bag.Count);
        Assert.Equal(1, stack.UndoCount);
        await stack.UndoAsync();
        Assert.Empty(bag);
    }

    private sealed class SliderTarget
    {
        public int Value { get; set; }
    }
}
