using System.Text.RegularExpressions;

namespace Jex.Cli.Templates;

/// <summary>One entry of <c>dotnet new uninstall</c>: a package id (with Version) or an installed directory (without).</summary>
public sealed record TemplateInstall(string Source, string? Version, IReadOnlyList<string> ShortNames)
{
    public bool IsDirectory => Version is null;
}

/// <summary>
/// Parses the English output of <c>dotnet new uninstall</c>: entries are indented by 3 spaces, their
/// <c>Version:</c> and <c>Templates:</c> headers by 6, and template lines by 9 ("Name (short-name) C#").
/// </summary>
public static partial class TemplateInstallListParser
{
    public static IReadOnlyList<TemplateInstall> Parse(string text)
    {
        var installs = new List<TemplateInstall>();
        string? source = null;
        string? version = null;
        var shortNames = new List<string>();
        var inTemplates = false;

        void Flush()
        {
            if (source is not null)
            {
                installs.Add(new TemplateInstall(source, version, shortNames));
            }
        }

        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r', ' ');
            if (line.Length == 0)
            {
                continue;
            }

            var indent = line.Length - line.TrimStart(' ').Length;
            var content = line.Trim();
            if (indent == 3)
            {
                Flush();
                source = content;
                version = null;
                shortNames = [];
                inTemplates = false;
            }
            else if (source is not null && indent == 6)
            {
                inTemplates = content == "Templates:";
                if (content.StartsWith("Version:", StringComparison.Ordinal))
                {
                    version = content["Version:".Length..].Trim();
                }
            }
            else if (source is not null && indent >= 9 && inTemplates)
            {
                foreach (Match match in ShortNameGroup().Matches(content))
                {
                    shortNames.AddRange(match.Groups[1].Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
                }
            }
        }

        Flush();
        return installs;
    }

    [GeneratedRegex(@"\(([^()]+)\)")]
    private static partial Regex ShortNameGroup();
}
