namespace {ProjectName}.scenarios;

/// <summary>
/// Enumerate the exit codes
/// </summary>
public enum ExitCode
{
    Success = 0,
    UnknownError = 1,
    InvalidParameters = 2,
    OutputFolderError = 3,
    DatabaseConnectionError = 4,
    ScenarioExecutionError = 5
}
