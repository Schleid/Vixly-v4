using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Shared.Kernel.Tests;

// =========================================================
// FILE: PhoneNumberTests.cs
//
// PURPOSE:
// Verifies PhoneNumber normalization and digit-count limits.
//
// RESPONSIBILITIES:
// - Cover formatting removal and the leading plus sign
// - Cover both digit-count boundaries
//
// IMPORTANT NOTES:
// - Shape validation only
// =========================================================
public sealed class PhoneNumberTests
{
    [Theory]
    [InlineData("+1 (555) 123-4567", "+15551234567")]
    [InlineData("555.123.4567", "5551234567")]
    [InlineData("  +44 20 7946 0958  ", "+442079460958")]
    [InlineData("1234567", "1234567")]
    [InlineData("123456789012345", "123456789012345")]
    public void Create_NormalizesValidNumbers(string input, string expected)
    {
        Assert.Equal(expected, PhoneNumber.Create(input).Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("123456")]
    [InlineData("1234567890123456")]
    public void TryCreate_RejectsInvalidNumbers(string? input)
    {
        Assert.False(PhoneNumber.TryCreate(input, out PhoneNumber? phone));
        Assert.Null(phone);
    }

    [Fact]
    public void Create_KeepsOnlyALeadingPlus()
    {
        Assert.Equal("+15551234567", PhoneNumber.Create("+1 555 +123 4567").Value);
    }

    [Fact]
    public void Create_ThrowsDomainExceptionForInvalidInput()
    {
        DomainException exception =
            Assert.Throws<DomainException>(() => PhoneNumber.Create("12"));

        Assert.Equal(KernelErrors.PhoneInvalid, exception.Error);
    }
}
