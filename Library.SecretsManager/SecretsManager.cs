namespace Library.SecretsManager;

public static class SecretsManager
{
    public static string GetEnvironmentVariable(string environmentVariableName)
    {
        return Environment.GetEnvironmentVariable(environmentVariableName) ??
               throw new InvalidOperationException("Environment variable does not exist");
    }

    public static string GetEnvironmentVariableOrDefaultTo(string environmentVariableName, string defaultTo = "")
    {
        string? environmentVariable = Environment.GetEnvironmentVariable(environmentVariableName);
        return string.IsNullOrEmpty(environmentVariable) ? defaultTo : environmentVariable;
    }
}