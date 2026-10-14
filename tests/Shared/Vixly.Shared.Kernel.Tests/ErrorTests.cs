using Vixly.Shared.Kernel.Errors;

namespace Vixly.Shared.Kernel.Tests;

// =========================================================
// FILE: ErrorTests.cs
//
// PURPOSE:
// Verifies the Error type that replaces sentences in Domain
// and Application (ADR-0005).
//
// RESPONSIBILITIES:
// - Cover code and argument-name validation
// - Cover the restricted argument value types
// - Cover value equality independent of argument order
//
// IMPORTANT NOTES:
// - ToString output is checked because logs depend on it
// =========================================================
public sealed class ErrorTests
{
    [Theory]
    [InlineData("platform.identity.email_invalid")]
    [InlineData("shared.time.must_be_utc")]
    [InlineData("operations.shipments.not_found")]
    public void Create_AcceptsWellFormedCodes(string code)
    {
        Assert.Equal(code, Error.Create(code).Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("noplane")]
    [InlineData("Platform.Identity.Bad")]
    [InlineData("platform..double")]
    [InlineData("platform.identity.")]
    [InlineData("platform.identity.has space")]
    [InlineData("1platform.identity.x")]
    public void Create_RejectsMalformedCodes(string code)
    {
        Assert.Throws<ArgumentException>(() => Error.Create(code));
    }

    [Theory]
    [InlineData("Count")]
    [InlineData("max_length")]
    [InlineData("1count")]
    [InlineData("")]
    public void Create_RejectsBadArgumentNames(string name)
    {
        Assert.Throws<ArgumentException>(() => Error.Create("shared.x.y", (name, 1)));
    }

    [Fact]
    public void Create_RejectsUnsupportedValueTypes()
    {
        Assert.Throws<ArgumentException>(() => Error.Create("shared.x.y", ("value", new object())));
        Assert.Throws<ArgumentException>(() => Error.Create("shared.x.y", ("value", 1.5d)));
        Assert.Throws<ArgumentException>(() => Error.Create("shared.x.y", ("value", Guid.NewGuid())));
    }

    [Fact]
    public void Create_AcceptsTheSupportedValueTypes()
    {
        Error error = Error.Create(
            "shared.x.y",
            ("text", "a"),
            ("count", 1),
            ("big", 1L),
            ("amount", 12.5m),
            ("flag", true),
            ("day", new DateOnly(2026, 10, 6)),
            ("moment", new DateTimeOffset(2026, 10, 6, 0, 0, 0, TimeSpan.Zero)));

        Assert.Equal(7, error.Arguments.Count);
    }

    [Fact]
    public void Create_RejectsDuplicateArgumentNames()
    {
        Assert.Throws<ArgumentException>(() => Error.Create("shared.x.y", ("a", 1), ("a", 2)));
    }

    [Fact]
    public void Equality_IgnoresArgumentOrder()
    {
        Error first = Error.Create("shared.x.y", ("a", 1), ("b", "two"));
        Error second = Error.Create("shared.x.y", ("b", "two"), ("a", 1));

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Equality_ComparesCodeAndArgumentValues()
    {
        Assert.NotEqual(Error.Create("shared.x.y"), Error.Create("shared.x.z"));
        Assert.NotEqual(Error.Create("shared.x.y", ("a", 1)), Error.Create("shared.x.y", ("a", 2)));
        Assert.NotEqual(Error.Create("shared.x.y", ("a", 1)), Error.Create("shared.x.y"));
    }

    [Fact]
    public void ToString_ShowsCodeAndInvariantArguments()
    {
        Error error = Error.Create("shared.x.y", ("b", 12.5m), ("a", 3));

        Assert.Equal("shared.x.y(a=3, b=12.5)", error.ToString());
        Assert.Equal("shared.x.y", Error.Create("shared.x.y").ToString());
    }
}
