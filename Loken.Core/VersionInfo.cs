namespace Loken.Core;

using System.Reflection;
using System.Linq;

public static class VersionInfo
{
	private const string BaseVersion = "0.1";

	static VersionInfo()
	{
		var assembly = typeof(VersionInfo).Assembly;
		var commit = GetCommitHash(assembly);

		Version = string.IsNullOrEmpty(commit)
			? $"{BaseVersion}-unknown"
			: $"{BaseVersion}-{commit}";
	}

	public static string Version { get; }

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
