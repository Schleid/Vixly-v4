using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Vixly.Platform.Infrastructure.Tests;

// =========================================================
// FILE: FixtureModelCacheKeyFactory.cs
//
// PURPOSE:
// Keeps one cached model per convention mode of
// FixtureContext.
//
// RESPONSIBILITIES:
// - Include the convention flag in the model cache key
//
// IMPORTANT NOTES:
// - Without it, EF Core would reuse the first model built and
//   the with/without tests would interfere
// =========================================================
internal sealed class FixtureModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
    {
        bool applyConvention = context is FixtureContext fixture && fixture.ApplyConvention;

        return (context.GetType(), applyConvention, designTime);
    }
}
