namespace ThreeDGod.AI;

public static class PromptSafety
{
    private static readonly char[] ShellMetacharacters =
        ['&', '|', ';', '`', '$', '\n', '\r', '\0', '<', '>', '(', ')', '{', '}', '!'];

    public static bool ContainsShellMetacharacters(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return false;
        return value.IndexOfAny(ShellMetacharacters) >= 0;
    }

    public static bool ArgsAreSafe(IReadOnlyDictionary<string, string> args)
    {
        foreach (var pair in args)
        {
            if (ContainsShellMetacharacters(pair.Key) || ContainsShellMetacharacters(pair.Value))
                return false;
        }
        return true;
    }
}
