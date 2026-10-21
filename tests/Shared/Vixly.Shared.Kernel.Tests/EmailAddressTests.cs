using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Shared.Kernel.Tests;

// =========================================================
// FILE: EmailAddressTests.cs
//
// PURPOSE:
// Verifies EmailAddress normalization and validation.
//
// RESPONSIBILITIES:
// - Cover accepted, normalized and rejected inputs
// - Cover value equality
//
// IMPORTANT NOTES:
// - Format checks only; mailbox existence is out of scope
// =========================================================
public sealed class EmailAddressTests
{
    [Theory]
    [InlineData("user@example.com", "user@example.com")]
    [InlineData("  User@Example.COM ", "user@example.com")]
    [InlineData("first.last+tag@sub.example.co", "first.last+tag@sub.example.co")]
    [InlineData("user@xn--exmple-cua.com", "user@xn--exmple-cua.com")]
    public void Create_AcceptsAndNormalizesValidAddresses(string input, string expected)
    {
        EmailAddress email = EmailAddress.Create(input);

        Assert.Equal(expected, email.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no-at-sign")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData("user@localhost")]
    [InlineData("us er@example.com")]
    [InlineData("user@-example.com")]
    [InlineData("user@example.c0m")]
    [InlineData("Name <user@example.com>")]
    [InlineData("sofía@andes-courier.test")]
    [InlineData("user@exämple.com")]
    public void TryCreate_RejectsInvalidAddresses(string? input)
    {
        bool created = EmailAddress.TryCreate(input, out EmailAddress? email);

        Assert.False(created);
        Assert.Null(email);
    }

    [Fact]
    public void TryCreate_RejectsValuesLongerThanTheMaximum()
    {
        string tooLong = new string('a', EmailAddress.MaxLength) + "@example.com";

        Assert.False(EmailAddress.TryCreate(tooLong, out _));
    }

    [Fact]
    public void Create_ThrowsDomainExceptionForInvalidInput()
    {
        DomainException exception =
            Assert.Throws<DomainException>(() => EmailAddress.Create("not-an-email"));

        Assert.Equal(KernelErrors.EmailInvalid, exception.Error);
    }

    [Fact]
    public void FromStored_ReadsAnAddressSavedUnderAnOlderRule()
    {
        Assert.Equal("sofía@andes-courier.test", EmailAddress.FromStored(" Sofía@Andes-Courier.test ").Value);
        Assert.Equal("user@example.com", EmailAddress.FromStored("User@Example.com").Value);
        Assert.Throws<ArgumentException>(() => EmailAddress.FromStored(" "));
    }

    [Fact]
    public void Equality_IsByNormalizedValue()
    {
        Assert.Equal(
            EmailAddress.Create("A@Example.com"),
            EmailAddress.Create("a@example.com"));
    }
}
