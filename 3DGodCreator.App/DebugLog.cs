namespace ThreeDGodCreator.App;

public static class DebugLog
{
    public static event Action<string>? OnMessage;

    public static void Write(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
            OnMessage?.Invoke(message);
    }
}
