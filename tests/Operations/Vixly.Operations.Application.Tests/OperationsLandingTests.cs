using Vixly.Operations.Application.Site;
using Vixly.Operations.Domain.Branding;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.Tests;

// =========================================================
// FILE: OperationsLandingTests.cs
//
// PURPOSE:
// Checks the landing page marketing rules (owner, 2026-10-30): real figures only, testimonials only with the customer's agreement.
//
// RESPONSIBILITIES:
// - Packages delivered: hidden below the minimum, always rounded down
// - A testimonial: refused without agreement, a bad rating or markup; the agreement is recorded
//
// IMPORTANT NOTES:
// - The website pages are checked in the Web tests (Landing_AirSea_*)
// =========================================================
public sealed class OperationsLandingTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 15, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(0, null)]
    [InlineData(499, null)]
    [InlineData(500, 500)]
    [InlineData(549, 500)]
    [InlineData(1_234, 1_200)]
    [InlineData(12_345, 12_000)]
    public void DeliveredPackages_HiddenBelowTheMinimum_AndRoundedDown(int count, int? shown)
    {
        Assert.Equal(shown, OperationsPublicFigures.Delivered(count));
    }

    [Fact]
    public void Testimonial_NeedsTheCustomersAgreement_AndPlainWords()
    {
        Guid tenant = Guid.NewGuid();
        Guid clerk = Guid.NewGuid();

        Assert.Equal("ops.brand.testimonial_consent_required",
            Assert.Throws<DomainException>(() => OperationsSiteTestimonial.Record(tenant, "Marie J.", null, "Fast.", 5, null, false, clerk, 0, Now)).Error.Code);
        Assert.Equal("ops.brand.testimonial_rating_invalid",
            Assert.Throws<DomainException>(() => OperationsSiteTestimonial.Record(tenant, "Marie J.", null, "Fast.", 6, null, true, clerk, 0, Now)).Error.Code);
        Assert.Equal("ops.brand.text_not_plain",
            Assert.Throws<DomainException>(() => OperationsSiteTestimonial.Record(tenant, "Marie J.", null, "<b>Fast</b>", null, null, true, clerk, 0, Now)).Error.Code);
        Assert.Equal("ops.brand.testimonial_quote_required",
            Assert.Throws<DomainException>(() => OperationsSiteTestimonial.Record(tenant, "Marie J.", null, "  ", null, null, true, clerk, 0, Now)).Error.Code);

        OperationsSiteTestimonial recorded = OperationsSiteTestimonial.Record(tenant, " Marie J. ", "Les Cayes", "Fast and careful.", 5, "Google", true, clerk, 2, Now);
        Assert.Equal("Marie J.", recorded.Name);
        Assert.Equal(clerk, recorded.ConsentRecordedByUserId);
        Assert.Equal(Now, recorded.ConsentRecordedUtc);
        Assert.True(recorded.Shown);

        recorded.ChangeShown(false, Now);
        Assert.False(recorded.Shown);
    }
}
