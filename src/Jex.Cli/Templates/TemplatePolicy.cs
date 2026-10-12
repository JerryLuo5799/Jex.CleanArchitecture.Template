namespace Jex.Cli.Templates;

/// <summary>The Jex template package as installed on this machine.</summary>
public sealed record InstalledTemplate(string Source, SemVer? Version, bool IsDirectory)
{
    /// <summary>Directory installs (dotnet new install ./templates) and 0.0.0-* packages are development builds.</summary>
    public bool IsDev => IsDirectory || Version is { IsDev: true };

    public string Display => IsDirectory ? $"the directory install {Source}" : Version?.ToString() ?? "an unknown version";
}

public enum NewTemplateAction
{
    Use,
    Install,
}

public sealed record NewTemplateDecision(
    NewTemplateAction Action,
    SemVer? Version,
    string? UninstallSource,
    IReadOnlyList<string> Messages);

public sealed record ScaffoldTemplateDecision(string? Warning);

/// <summary>
/// Version rules. <c>dotnet new install</c> keeps a single version per machine, so only <c>jex new</c> may install
/// or switch template versions; scaffolding inside an existing project only checks compatibility.
/// </summary>
public static class TemplatePolicy
{
    public const string PackageId = "Jex.CleanArchitecture.Template";

    /// <summary>jex new: target = --template-version, else the CLI version (CLI and templates are released together).</summary>
    public static NewTemplateDecision ForNew(SemVer cliVersion, InstalledTemplate? installed, SemVer? requested, bool noInstall)
    {
        var messages = new List<string>();
        var target = requested ?? cliVersion;

        if (target.IsDev)
        {
            if (installed is { IsDev: true })
            {
                return Use($"Using the development templates ({installed.Display}).");
            }

            throw new JexException(requested is null
                ? $"This is a development build of jex ({cliVersion}) and no development templates are installed "
                  + $"({(installed is null ? "none installed" : $"installed: {installed.Display}")}). Run 'jex dev install' in the template "
                  + "repository, or pass --template-version <version> to use a published template package."
                : $"{target} is a local development version and cannot be installed from NuGet. Run 'jex dev install' in the template repository first.");
        }

        if (installed is null)
        {
            if (noInstall)
            {
                throw new JexException($"The Jex templates are not installed. Run 'dotnet new install {PackageId}::{target}', or rerun without --no-install.");
            }

            messages.Add($"Installing {PackageId} {target}.");
            return new NewTemplateDecision(NewTemplateAction.Install, target, null, messages);
        }

        if (installed.IsDirectory && requested is null)
        {
            return Use($"Using {installed.Display} (development templates).");
        }

        if (installed.Version is not null && installed.Version == target)
        {
            return Use(null);
        }

        if (requested is null && installed.Version is not null && installed.Version > cliVersion)
        {
            messages.Add(
                $"warning: the installed templates ({installed.Version}) are newer than this jex ({cliVersion}); consider 'dotnet tool update -g Jex.Cli'. "
                + $"Installing {target} so jex and the templates match (pass --template-version {installed.Version} to keep the installed ones).");
        }

        if (noInstall)
        {
            throw new JexException(
                $"Templates {installed.Display} are installed but {target} is required. Rerun without --no-install, or run 'dotnet new install {PackageId}::{target} --force'.");
        }

        messages.Add($"Switching the Jex templates: {installed.Display} -> {target}.");
        return new NewTemplateDecision(NewTemplateAction.Install, target, installed.IsDirectory ? installed.Source : null, messages);

        NewTemplateDecision Use(string? message) =>
            new(NewTemplateAction.Use, installed?.Version, null, message is null ? messages : [.. messages, message]);
    }

    /// <summary>
    /// jex scaffold: never installs. The project's jex.json templateVersion (or an explicit --template-version) must
    /// match the installed major version; development builds are tolerated only when no version was requested.
    /// </summary>
    public static ScaffoldTemplateDecision ForScaffold(string? projectVersion, InstalledTemplate? installed, SemVer? requested, bool force)
    {
        if (force)
        {
            return new ScaffoldTemplateDecision(
                $"--force: skipping the template version check and scaffolding with {installed?.Display ?? "whatever templates are installed"}.");
        }

        SemVer? project = SemVer.TryParse(projectVersion, out var parsed) ? parsed : null;
        var isExplicit = requested is not null;
        var want = requested ?? project;

        if (installed is null)
        {
            throw new JexException(
                $"The Jex templates are not installed. Run 'dotnet new install {PackageId}{(want is { IsDev: false } ? $"::{want}" : string.Empty)}' "
                + "(or 'jex dev install' in the template repository).");
        }

        if (want is null)
        {
            throw new JexException(
                $"jex.json has no usable templateVersion ('{projectVersion ?? "null"}'), so jex cannot tell which templates this project needs. "
                + "Pass --template-version <version the project was created with>, or --force to scaffold with the installed templates.");
        }

        if (!isExplicit && (installed.IsDev || want.IsDev))
        {
            return installed.IsDev && want.IsDev
                ? new ScaffoldTemplateDecision(null)
                : new ScaffoldTemplateDecision(
                    $"the project was created with templates {want} and {installed.Display} is installed; one of them is a development build, continuing.");
        }

        var installedVersion = installed.IsDirectory ? SemVer.Dev : installed.Version;
        if (installedVersion is null)
        {
            throw new JexException($"Cannot determine the installed template version ({installed.Source}). Pass --force to scaffold anyway.");
        }

        if (installedVersion.Major != want.Major || installedVersion.IsDev != want.IsDev)
        {
            throw new JexException(
                $"This project was created with templates {project?.ToString() ?? "(unknown version)"}"
                + (isExplicit ? $" and --template-version asks for {want}" : string.Empty)
                + $", but {installed.Display} is installed (different major version). Either switch this machine's templates: "
                + $"'dotnet new install {PackageId}::{want} --force', or rerun with --force to scaffold with the installed templates anyway.");
        }

        return installedVersion == want
            ? new ScaffoldTemplateDecision(null)
            : new ScaffoldTemplateDecision($"the installed templates ({installedVersion}) differ from {want} (same major version); continuing.");
    }
}
