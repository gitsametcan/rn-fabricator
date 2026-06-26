namespace Fabricator.Cli.Templates;

internal static class TemplateCommandOutput
{
    public static void WriteErrors(TextWriter writer, IEnumerable<string> errors)
    {
        foreach (var error in errors)
        {
            writer.WriteLine($"- {error}");
        }
    }

    public static void WriteNext(TextWriter writer, string action)
    {
        writer.WriteLine($"Next: {action}");
    }

    public static string RenderYesNo(bool value)
    {
        return value ? "yes" : "no";
    }
}
