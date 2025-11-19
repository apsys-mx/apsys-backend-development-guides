using System.Text.RegularExpressions;

namespace MiProyecto.migrations;

internal class CommandLineArgs : Dictionary<string, string>
{
    private const string Pattern = @"\/(?<argname>\w+):(?<argvalue>.+)";
    private readonly Regex _regex = new(Pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Determine if the user pass at least one valid parameter
    /// </summary>
    /// <returns></returns>
    public bool ContainsValidArguments()
    {
        return (this.ContainsKey("cnn"));
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public CommandLineArgs()
    {
        var args = Environment.GetCommandLineArgs();
        foreach (var groups in args.Select(arg => _regex.Match(arg)).Where(m => m.Success).Select(match => match.Groups))
            this.Add(groups["argname"].Value, groups["argvalue"].Value);
    }
}

internal enum ExitCode
{
    Success = 0,
    UnknownError = 1
}
