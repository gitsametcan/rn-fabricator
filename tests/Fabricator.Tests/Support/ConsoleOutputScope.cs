namespace Fabricator.Tests;

public sealed class ConsoleOutputScope : IDisposable
{
    private readonly StringWriter _writer;
    private readonly TextWriter _originalOut;

    private ConsoleOutputScope()
    {
        _writer = new StringWriter();
        _originalOut = Console.Out;
        Console.SetOut(_writer);
    }

    public static ConsoleOutputScope Capture()
    {
        return new ConsoleOutputScope();
    }

    public override string ToString()
    {
        _writer.Flush();
        return _writer.ToString();
    }

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        _writer.Dispose();
    }
}
