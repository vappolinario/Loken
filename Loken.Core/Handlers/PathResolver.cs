namespace Loken.Core;

public interface IPathResolver
{
  public string WorkingDirectory { get; }
  string ResolveSafePath(string path);
}

public class PathResolver(string workingDir) : IPathResolver
{
    public string WorkingDirectory { get { return workingDir; } }
    public string ResolveSafePath(string path)
    {
        string workingDir = Path.GetFullPath(WorkingDirectory).TrimEnd(Path.DirectorySeparatorChar);
        string fullPath = Path.GetFullPath(Path.Combine(workingDir, path));

        if (!fullPath.StartsWith(workingDir, StringComparison.OrdinalIgnoreCase))
            throw new ExecutionFailedException($"Directory outside working directory: {path}");

        return fullPath;
    }
}
