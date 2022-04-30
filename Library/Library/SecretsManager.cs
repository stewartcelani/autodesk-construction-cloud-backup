namespace Library;

public static class SecretsManager
{
    public static string GetEnvironmentVariableOrDefaultTo(string environmentVariableName, string defaultTo = "")
    {
        string? environmentVariable = Environment.GetEnvironmentVariable(environmentVariableName);
        return string.IsNullOrEmpty(environmentVariable) ? defaultTo : environmentVariable;
    }
}