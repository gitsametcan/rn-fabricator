namespace Fabricator.Tests;

public sealed class ConsoleOutputScope : IDisposable
{
    private readonly StringWriter _errorWriter;
    private readonly StringWriter _outputWriter;
    private readonly TextWriter _originalError;
    private readonly TextWriter _originalOut;

    private ConsoleOutputScope()
    {
        _errorWriter = new StringWriter();
        _outputWriter = new StringWriter();
        _originalError = Console.Error;
        _originalOut = Console.Out;
        Console.SetError(_errorWriter);
        Console.SetOut(_outputWriter);
    }

    public static ConsoleOutputScope Capture()
    {
        return new ConsoleOutputScope();
    }

    public string ErrorOutput
    {
        get
        {
            _errorWriter.Flush();
            return _errorWriter.ToString();
        }
    }

    public override string ToString()
    {
        _outputWriter.Flush();
        return _outputWriter.ToString();
    }

    public void Dispose()
    {
        Console.SetError(_originalError);
        Console.SetOut(_originalOut);
        _errorWriter.Dispose();
        _outputWriter.Dispose();
    }
}
