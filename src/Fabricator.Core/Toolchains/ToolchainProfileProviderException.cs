namespace Fabricator.Core.Toolchains;

public sealed class ToolchainProfileProviderException : Exception
{
    public ToolchainProfileProviderException(string message)
        : base(message)
    {
    }

    public ToolchainProfileProviderException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
