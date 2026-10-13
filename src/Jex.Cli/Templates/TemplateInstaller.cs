using System.Text.RegularExpressions;

namespace Jex.Cli.Templates;

/// <summary>Detects and installs the Jex template package with <c>dotnet new</c>.</summary>
public sealed partial class TemplateInstaller(CliContext context)
{
    public const string SolutionShortName = "jex";

    /// <summary>Every install that provides the Jex templates (normally zero or one).</summary>
    public IReadOnlyList<TemplateInstall> ListInstalls()
    {
        var capture = context.Runner.RunCapture(context.CurrentDirectory, "dotnet", ["new", "uninstall"]);
        var installs = TemplateInstallListParser.Parse(capture.StandardOutput);
        if (installs.Count == 0)
        {
            // Unrecognised output format: fall back to "is the jex template available at all?" without a version.
            var list = context.Runner.RunCapture(context.CurrentDirectory, "dotnet", ["new", "list", SolutionShortName]);
            return list.ExitCode == 0 && ListedShortName().IsMatch(list.StandardOutput)
                ? [new TemplateInstall("(unknown)", "unknown", [SolutionShortName])]
                : [];
        }

        return installs
            .Where(install => install.Source.Equals(TemplatePolicy.PackageId, StringComparison.OrdinalIgnoreCase)
                || install.ShortNames.Contains(SolutionShortName, StringComparer.OrdinalIgnoreCase))
            .ToList();
    }

    public InstalledTemplate? Detect()
    {
        var installs = ListInstalls();
        if (installs.Count == 0)
        {
            return null;
        }

        var pick = installs.FirstOrDefault(i => i.IsDirectory) ?? installs[0];
        if (installs.Count > 1)
        {
            context.Error.WriteLine(
                $"warning: the Jex templates are installed more than once ({string.Join(", ", installs.Select(i => i.Source))}); using {pick.Source}. "
                + "Remove the one you do not want with 'dotnet new uninstall <source>'.");
        }

        return new InstalledTemplate(
            pick.Source,
            pick.IsDirectory ? null : SemVer.TryParse(pick.Version, out var version) ? version : null,
            pick.IsDirectory);
    }

    public void EnsureForNew(SemVer? requested, bool noInstall)
    {
        var decision = TemplatePolicy.ForNew(context.Version, Detect(), requested, noInstall);
        foreach (var message in decision.Messages)
        {
            context.Out.WriteLine(message);
        }

        if (decision.Action != NewTemplateAction.Install)
        {
            return;
        }

        if (decision.UninstallSource is not null)
        {
            context.RunAllowingFailure(context.CurrentDirectory, "dotnet", ["new", "uninstall", decision.UninstallSource]);
        }

        context.Run(context.CurrentDirectory, "dotnet", ["new", "install", $"{TemplatePolicy.PackageId}@{decision.Version}", "--force"]);
    }

    public void CheckForScaffold(string? projectVersion, SemVer? requested, bool force)
    {
        var decision = TemplatePolicy.ForScaffold(projectVersion, Detect(), requested, force);
        if (decision.Warning is not null)
        {
            context.Error.WriteLine($"warning: {decision.Warning}");
        }
    }

    [GeneratedRegex(@"(^|\s)jex(,|\s|$)", RegexOptions.Multiline)]
    private static partial Regex ListedShortName();
}
