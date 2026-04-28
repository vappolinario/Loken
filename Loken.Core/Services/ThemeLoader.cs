using Loken.Core.Options;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Loken.Core.Services;

public interface IThemeLoader
{
    ThemeOptions Load(string path);
}

public class ThemeLoader : IThemeLoader
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .Build();

    public ThemeOptions Load(string path)
    {
        if (!File.Exists(path))
        {
            return new ThemeOptions();
        }

        var yaml = File.ReadAllText(path);
        return Deserializer.Deserialize<ThemeOptions>(yaml);
    }
}
