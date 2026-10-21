using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsNumberAllocator.cs
//
// PURPOSE:
// Takes the next number with one atomic MERGE (D-O12).
//
// RESPONSIBILITIES:
// - Create the year's row on first use; increment it under a key-range lock
// - Join the caller's transaction when there is one
//
// IMPORTANT NOTES:
// - HOLDLOCK serialises callers on the same tenant, kind and year; different keys never wait on each other
// - A number taken by a command that later fails is not reused (gap)
// =========================================================
internal sealed class OperationsNumberAllocator : IOperationsNumberAllocator
{
    private const string Sql = """
        MERGE ops.NumberSequences WITH (HOLDLOCK) AS target
        USING (SELECT @tenant AS TenantId, @kind AS Kind, @year AS [Year]) AS source
            ON target.TenantId = source.TenantId AND target.Kind = source.Kind AND target.[Year] = source.[Year]
        WHEN MATCHED THEN
            UPDATE SET LastNumber = target.LastNumber + 1, LastModifiedUtc = @now
        WHEN NOT MATCHED THEN
            INSERT (Id, TenantId, Kind, [Year], LastNumber, CreatedUtc) VALUES (NEWID(), @tenant, @kind, @year, 1, @now)
        OUTPUT inserted.LastNumber;
        """;

    private readonly OperationsDbContext _context;

    private readonly IOperationsTenantScope _scope;

    private readonly TimeProvider _timeProvider;

    public OperationsNumberAllocator(OperationsDbContext context, IOperationsTenantScope scope, TimeProvider timeProvider)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<long> NextAsync(OperationsNumberKind kind, int year, CancellationToken cancellationToken)
    {
        Guid tenantId = _scope.TenantId ?? throw new InvalidOperationException("Numbers are issued inside a tenant (D-F1).");
        DbConnection connection = _context.Database.GetDbConnection();
        bool opened = connection.State != ConnectionState.Open;

        if (opened)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using DbCommand command = connection.CreateCommand();
            command.CommandText = Sql;
            command.Transaction = _context.Database.CurrentTransaction?.GetDbTransaction();
            Add(command, "@tenant", tenantId);
            Add(command, "@kind", (int)kind);
            Add(command, "@year", year);
            Add(command, "@now", _timeProvider.GetUtcNow());

            return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken), System.Globalization.CultureInfo.InvariantCulture);
        }
        finally
        {
            if (opened)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static void Add(DbCommand command, string name, object value)
    {
        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
