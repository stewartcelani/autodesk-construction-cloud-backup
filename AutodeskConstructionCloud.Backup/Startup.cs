using AutodeskConstructionCloud.Backup;
using CommandLine;

public static class Startup
{
    public static void OnParse(CommandLineArgs config)
    {
        
    }

    public static void OnParseError(IEnumerable<Error> errors)
    {
        Environment.Exit(-1);
    }
}