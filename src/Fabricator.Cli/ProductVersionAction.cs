using System.CommandLine;
using System.CommandLine.Invocation;

namespace Fabricator.Cli;

public sealed class ProductVersionAction : SynchronousCommandLineAction
{
    public override int Invoke(ParseResult parseResult)
    {
        parseResult.InvocationConfiguration.Output.WriteLine(ProductVersion.Current);
        return 0;
    }
}
