using System.CommandLine;

namespace Jex.Cli.Commands;

internal static class CommandExtensions
{
    /// <summary>Sets the action; a <see cref="JexException"/> becomes a one-line error and its exit code.</summary>
    public static Command Handle(this Command command, CliContext context, Action<ParseResult> handler)
    {
        command.SetAction(parseResult =>
        {
            try
            {
                handler(parseResult);
                return 0;
            }
            catch (JexException ex)
            {
                context.Error.WriteLine($"jex: {ex.Message}");
                return ex.ExitCode;
            }
        });
        return command;
    }

    public static Option<string?> ApiProjectOption() => new("--api-project")
    {
        Description = "Web API project to use (path to the .csproj or its directory). Default: the only *.WebAPI.csproj in the solution.",
    };

    public static string Bool(bool value) => value ? "true" : "false";
}
