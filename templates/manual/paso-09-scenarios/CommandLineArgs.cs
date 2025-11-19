using System.Text.RegularExpressions;

namespace MiProyecto.scenarios;

/// <summary>
/// Dictionary with input parameters of console application
/// </summary>
internal class CommandLineArgs : Dictionary<string, string>
{
    private const string Pattern = @"\/(?<argname>\w+):(?<argvalue>.+)";
    private readonly Regex _regex = new(Pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(2));

    /// <summary>
    /// Determine if the user pass at least one valid parameter
    /// </summary>
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

        if (args == null || args.Length == 0)
            return;

        foreach (var groups in args.Select(arg => _regex.Match(arg)).Where(match => match.Success).Select(match => match.Groups))
        {
            this.Add(groups["argname"].Value, groups["argvalue"].Value);
        }
    }
}
