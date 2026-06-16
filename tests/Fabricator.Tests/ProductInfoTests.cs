using Fabricator.Core;

namespace Fabricator.Tests;

public sealed class ProductInfoTests
{
    [Fact]
    public void ProductNameMatchesCliCommandName()
    {
        Assert.Equal("rn-fabricator", ProductInfo.Name);
    }
}
