namespace Loken.Core;

using System.Reflection;
using System.Linq;

public static class VersionInfo
{
	/// <summary>Base semver for Loken. The commit hash from the time of publishing is appended.</summary>
	private const string BaseVersion = "0.1";

	static VersionInfo()
	{
		var assembly = typeof(VersionInfo).Assembly;
		var commit = GetCommitHash(assembly);

		Version = string.IsNullOrEmpty(commit)
			? $"{BaseVersion}-unknown"
			: $"{BaseVersion}-{commit}";
	}

	/// <summary>The version string, e.g. "0.1-a1b2c3d" or "0.1-unknown".</summary>
	public static string Version { get; }

	/// <summary>Resolves the commit hash from assembly attributes.</summary>
	/// <remarks>
	/// Priority:
	/// 1. <c>AssemblyMetadata("GitCommitHash")</c> — set explicitly via MSBuild target or <c>build.sh</c>.
	/// 2. <c>AssemblyInformationalVersion</c> — the .NET SDK automatically appends
	///    the source revision (full hash) after a '+' when building from a git repo.
	/// 3. <c>null</c> — when no source revision information is available.
	/// </remarks>
	private static string? GetCommitHash(Assembly assembly)
	{
		// 1. Check for explicit AssemblyMetadata("GitCommitHash").
		var metadataAttributes = assembly.GetCustomAttributes<AssemblyMetadataAttribute>();
		var explicitHash = metadataAttributes
			.FirstOrDefault(a => a.Key == "GitCommitHash")?.Value;

		if (!string.IsNullOrEmpty(explicitHash))
			return explicitHash;

		// 2. Fall back to the informational version which .NET SDK populates automatically.
		var infoVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
		if (!string.IsNullOrEmpty(infoVersion))
		{
			// Format: "1.0.0+a1b2c3d4e5f6..." — extract after '+'
			var plusIndex = infoVersion.IndexOf('+');
			if (plusIndex >= 0 && plusIndex < infoVersion.Length - 1)
			{
				var hash = infoVersion.Substring(plusIndex + 1);
				// Use the short form (first 7 characters) if available.
				return hash.Length > 7 ? hash[..7] : hash;
			}
		}

		return null;
	}
}
