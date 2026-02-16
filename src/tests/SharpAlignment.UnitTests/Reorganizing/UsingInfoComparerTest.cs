using FluentAssertions;
using SharpAlignment.Reorganizing;
using Xunit;

namespace SharpAlignment.UnitTests.Reorganizing;

public class UsingInfoComparerTest
{
    [Fact]
    public void Compare_DefaultSort_IsAlphabetical()
    {
        var comparer = new UsingInfoComparer(systemUsingFirst: false);
        var left = new UsingInfo("System");
        var right = new UsingInfo("FluentAssertions");

        var result = comparer.Compare(left, right);

        result.Should().BePositive();
    }

    [Fact]
    public void Compare_SystemUsingFirstOption_PutsSystemFirst()
    {
        var comparer = new UsingInfoComparer(systemUsingFirst: true);
        var left = new UsingInfo("System");
        var right = new UsingInfo("FluentAssertions");

        var result = comparer.Compare(left, right);

        result.Should().BeNegative();
    }
}