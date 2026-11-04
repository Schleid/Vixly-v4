using System.Security.Cryptography;
using System.Text;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Printing;

namespace Vixly.Operations.Application.Printing;

// =========================================================
// FILE: OperationsPrintAgentKeys.cs
//
// PURPOSE:
// Makes and checks the keys of dock print programs (Slice 3B).
//
// RESPONSIBILITIES:
// - Write a key as vxpa.<agent>.<secret>; find the agent a key belongs to
//
// IMPORTANT NOTES:
// - Only the hash of the secret is stored; hashes are compared in fixed time
// - A stopped agent's key no longer works
// =========================================================
public sealed class OperationsPrintAgentKeys
{
    private const string Prefix = "vxpa.";

    private readonly IOperationsPrintingRepository _printing;

    private readonly IOperationsSecretGenerator _secrets;

    public OperationsPrintAgentKeys(IOperationsPrintingRepository printing, IOperationsSecretGenerator secrets)
    {
        _printing = printing ?? throw new ArgumentNullException(nameof(printing));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
    }

    public OperationsSecret NewSecret() => _secrets.NewToken();

    public static string Format(Guid agentId, string secret) => Prefix + agentId.ToString("N") + "." + secret;

    public async Task<OperationsPrintAgent?> AuthenticateAsync(string? key, CancellationToken cancellationToken)
    {
        if (key is not { Length: > 40 and < 300 } || !key.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return null;
        }

        int dot = key.IndexOf('.', Prefix.Length);

        if (dot < 0 || !Guid.TryParseExact(key[Prefix.Length..dot], "N", out Guid agentId)
            || await _printing.GetAgentAsync(agentId, cancellationToken) is not { IsActive: true } agent)
        {
            return null;
        }

        byte[] given = Encoding.UTF8.GetBytes(_secrets.HashToken(key[(dot + 1)..]));
        byte[] stored = Encoding.UTF8.GetBytes(agent.KeyHash);
        return CryptographicOperations.FixedTimeEquals(given, stored) ? agent : null;
    }
}
