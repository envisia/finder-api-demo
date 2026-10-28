namespace FinderDemo.Extensions;

internal static class PathExtensions
{
    /// <summary>
    /// Turns user input into a full path: trims it, expands environment variables and a leading <c>~</c> / <c>~/</c>
    /// (home directory), and resolves relative paths against the current directory.
    /// </summary>
    public static string ToFullPath(this string path)
    {
        path = Environment.ExpandEnvironmentVariables(path.Trim());
        if (path == "~" || path.StartsWith("~/", StringComparison.Ordinal) || path.StartsWith("~\\", StringComparison.Ordinal))
        {
            path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + path[1..];
        }

        return Path.GetFullPath(path);
    }
}
