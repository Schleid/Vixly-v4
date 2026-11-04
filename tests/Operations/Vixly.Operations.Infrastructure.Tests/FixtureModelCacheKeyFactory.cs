using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Vixly.Operations.Infrastructure.Persistence;

namespace Vixly.Operations.Infrastructure.Tests;

// =========================================================
// FILE: FixtureModelCacheKeyFactory.cs
//
// PURPOSE:
// Keeps the test model apart from the real one in the EF model cache.
//
// RESPONSIBILITIES:
// - Key the model on whether the test entity is added
//
// IMPORTANT NOTES:
// - Tests only
// =========================================================
internal sealed class FixtureModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
    {
        return (context.GetType(), context is OperationsDbContext { HasTestModel: true }, designTime);
    }
}
