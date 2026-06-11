// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Studio.Deployment;

namespace TheTechIdea.Beep.Studio.Deployment;

/// <summary>
/// Default implementation of <see cref="IDeploymentMetadataService"/>. Resolves
/// the deployment metadata (code revision + build id + version) from the
/// environment, the manifest, the git CLI, and the assembly. Mints and
/// verifies HMAC-SHA256 approval tokens bound to a specific deployment.
/// </summary>
public sealed class DeploymentMetadataService : IDeploymentMetadataService
{
    private readonly object _lock = new();
    private DeploymentMetadata? _override;
    private readonly byte[] _hmacKey;
    private DeploymentMetadata? _cached;

    public DeploymentMetadataService(string? hmacKey = null)
    {
        _hmacKey = ResolveHmacKey(hmacKey);
    }

    // ── Resolution ─────────────────────────────────────────────────────────

    public Task<StudioResult<DeploymentMetadata>> GetCurrentAsync(CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (_override != null) return Task.FromResult(StudioResult<DeploymentMetadata>.Ok(_override));
            if (_cached != null) return Task.FromResult(StudioResult<DeploymentMetadata>.Ok(_cached));

            try
            {
                _cached = ResolveMetadata();
                return Task.FromResult(StudioResult<DeploymentMetadata>.Ok(_cached));
            }
            catch (Exception ex)
            {
                return Task.FromResult(StudioResult<DeploymentMetadata>.Fail(StudioErrorCode.DeploymentMetadataMissing, ex.Message, ex));
            }
        }
    }

    public void Override(DeploymentMetadata? metadata)
    {
        lock (_lock) { _override = metadata; _cached = metadata; }
    }

    // ── Token issuer / verifier ─────────────────────────────────────────────

    public Task<StudioResult<ApprovalToken>> IssueApprovalTokenAsync(ApprovalTokenRequest request, CancellationToken ct = default)
    {
        if (request == null) return Task.FromResult(StudioResult<ApprovalToken>.Fail(StudioErrorCode.InvalidArgument, "request is required."));

        // Read the current deployment on the same thread; if it fails, the
        // verifier would later fail too — surface the error here.
        var deployResult = GetCurrentAsync(ct).GetAwaiter().GetResult();
        if (!deployResult.IsSuccess)
            return Task.FromResult(StudioResult<ApprovalToken>.Fail(deployResult.Error.Code, deployResult.Error.Message, deployResult.Error.Exception));

        var deploy = deployResult.Value!;
        var issuedAt = request.IssuedAt == default ? DateTimeOffset.UtcNow : request.IssuedAt;
        var expiresAt = issuedAt + (request.Lifetime == default ? TimeSpan.FromMinutes(15) : request.Lifetime);

        var claims = new ApprovalTokenClaims(
            ApprovalId: request.ApprovalId,
            PlanHash: request.PlanHash,
            Tier: request.Tier,
            CodeRevisionRef: deploy.CodeRevisionRef,
            CodeRevisionSha: deploy.CodeRevisionSha,
            IssuedAt: issuedAt,
            ExpiresAt: expiresAt);

        var token = SignClaims(claims);
        return Task.FromResult(StudioResult<ApprovalToken>.Ok(new ApprovalToken(token, claims, issuedAt, expiresAt)));
    }

    public async Task<StudioResult<ApprovalTokenClaims>> VerifyApprovalTokenAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return StudioResult<ApprovalTokenClaims>.Fail(StudioErrorCode.InvalidArgument, "token is required.");

        if (!TryUnsignToken(token, out var claims))
            return StudioResult<ApprovalTokenClaims>.Fail(StudioErrorCode.ApprovalTokenInvalid, "Signature mismatch.");

        if (claims!.ExpiresAt < DateTimeOffset.UtcNow)
            return StudioResult<ApprovalTokenClaims>.Fail(StudioErrorCode.ApprovalTokenInvalid, $"Token expired at {claims.ExpiresAt:o}.");

        // Bind the token to the current code revision: a token issued for rev A
        // cannot be replayed against rev B.
        var currentResult = await GetCurrentAsync(ct);
        if (!currentResult.IsSuccess)
            return StudioResult<ApprovalTokenClaims>.Fail(StudioErrorCode.DeploymentMetadataMissing,
                "Cannot verify token without current deployment metadata.");

        var current = currentResult.Value!;
        if (!string.Equals(current.CodeRevisionSha, claims.CodeRevisionSha, StringComparison.OrdinalIgnoreCase))
            return StudioResult<ApprovalTokenClaims>.Fail(StudioErrorCode.ApprovalTokenInvalid,
                $"Token was issued for revision {claims.CodeRevisionSha[..Math.Min(8, claims.CodeRevisionSha.Length)]} but the current revision is {current.CodeRevisionSha[..Math.Min(8, current.CodeRevisionSha.Length)]}.");

        return StudioResult<ApprovalTokenClaims>.Ok(claims);
    }

    // ── Internal: HMAC + resolution ───────────────────────────────────────

    private byte[] ResolveHmacKey(string? overrideKey)
    {
        if (!string.IsNullOrWhiteSpace(overrideKey)) return Encoding.UTF8.GetBytes(overrideKey);

        var env = Environment.GetEnvironmentVariable(StudioConstants.ApprovalHmacKeyEnvVar);
        if (!string.IsNullOrWhiteSpace(env)) return Encoding.UTF8.GetBytes(env);

        // Ephemeral dev key. Real hosts MUST set BEEP_APPROVAL_HMAC_KEY.
        // The key changes every process restart, so tokens are not portable
        // across restarts — that's the intended behavior in dev.
        return Encoding.UTF8.GetBytes($"ephemeral-dev-key-{Guid.NewGuid():N}");
    }

    private string SignClaims(ApprovalTokenClaims claims)
    {
        var headerJson = JsonSerializer.Serialize(new { alg = "HS256", typ = "beep-approval-v1" });
        var payloadJson = JsonSerializer.Serialize(claims);
        var header = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
        var payload = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
        var signature = ComputeHmac($"{header}.{payload}");
        return $"{header}.{payload}.{signature}";
    }

    private bool TryUnsignToken(string token, out ApprovalTokenClaims? claims)
    {
        claims = null;
        var parts = token.Split('.');
        if (parts.Length != 3) return false;
        var expected = ComputeHmac($"{parts[0]}.{parts[1]}");
        if (!FixedTimeEquals(expected, parts[2])) return false;
        try
        {
            var payload = Base64UrlDecode(parts[1]);
            claims = JsonSerializer.Deserialize<ApprovalTokenClaims>(payload);
            return claims != null;
        }
        catch { return false; }
    }

    private string ComputeHmac(string input)
    {
        using var hmac = new HMACSHA256(_hmacKey);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Base64UrlEncode(hash);
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var ab = Encoding.UTF8.GetBytes(a);
        var bb = Encoding.UTF8.GetBytes(b);
        return ab.Length == bb.Length && CryptographicOperations.FixedTimeEquals(ab, bb);
    }

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string s)
    {
        var padded = s.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4) { case 2: padded += "=="; break; case 3: padded += "="; break; }
        return Convert.FromBase64String(padded);
    }

    private static DeploymentMetadata ResolveMetadata()
    {
        // 1. BEEP_DEPLOYMENT_METADATA_JSON env var (set by CI)
        var envJson = Environment.GetEnvironmentVariable(StudioConstants.DeploymentMetadataEnvVar);
        if (!string.IsNullOrWhiteSpace(envJson))
        {
            try
            {
                var m = JsonSerializer.Deserialize<DeploymentMetadata>(envJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (m != null && !string.IsNullOrWhiteSpace(m.CodeRevisionSha)) return m;
            }
            catch { /* fall through to next source */ }
        }

        // 2. BEEP_CODE_REVISION_SHA + BEEP_CODE_REVISION_REF env vars
        var sha = Environment.GetEnvironmentVariable("BEEP_CODE_REVISION_SHA");
        var refr = Environment.GetEnvironmentVariable("BEEP_CODE_REVISION_REF");
        if (!string.IsNullOrWhiteSpace(sha) && !string.IsNullOrWhiteSpace(refr))
        {
            return new DeploymentMetadata(
                CodeRevisionRef: refr,
                CodeRevisionSha: sha,
                BuildId: Environment.GetEnvironmentVariable("BEEP_BUILD_ID"),
                BuildUrl: Environment.GetEnvironmentVariable("BEEP_BUILD_URL"),
                Version: Environment.GetEnvironmentVariable("BEEP_VERSION"),
                BuiltAt: DateTimeOffset.UtcNow,
                Labels: null);
        }

        // 3. git rev-parse (dev only)
        try
        {
            var (gitSha, gitRef) = RunGitRevParse();
            if (!string.IsNullOrWhiteSpace(gitSha) && !string.IsNullOrWhiteSpace(gitRef))
            {
                return new DeploymentMetadata(
                    CodeRevisionRef: gitRef,
                    CodeRevisionSha: gitSha,
                    BuildId: null,
                    BuildUrl: null,
                    Version: GetAssemblyInformationalVersion(),
                    BuiltAt: DateTimeOffset.UtcNow,
                    Labels: null);
            }
        }
        catch { /* fall through */ }

        // 4. Assembly InformationalVersion
        var v = GetAssemblyInformationalVersion();
        if (!string.IsNullOrWhiteSpace(v))
        {
            return new DeploymentMetadata(
                CodeRevisionRef: "release",
                CodeRevisionSha: v,
                BuildId: null,
                BuildUrl: null,
                Version: v,
                BuiltAt: DateTimeOffset.UtcNow,
                Labels: null);
        }

        throw new InvalidOperationException(
            "No deployment metadata found. Set BEEP_DEPLOYMENT_METADATA_JSON, BEEP_CODE_REVISION_SHA + BEEP_CODE_REVISION_REF, or run from a git repo.");
    }

    private static (string Sha, string Ref) RunGitRevParse()
    {
        try
        {
            var sha = RunProcess("git", "rev-parse HEAD")?.Trim();
            var refr = RunProcess("git", "symbolic-ref --short HEAD")?.Trim();
            if (!string.IsNullOrEmpty(sha) && !string.IsNullOrEmpty(refr)) return (sha!, "refs/heads/" + refr);
        }
        catch { }
        return (string.Empty, string.Empty);
    }

    private static string? RunProcess(string exe, string args)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo(exe, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Environment.CurrentDirectory
            };
            using var p = System.Diagnostics.Process.Start(psi);
            p!.WaitForExit(2000);
            return p.StandardOutput.ReadToEnd();
        }
        catch { return null; }
    }

    private static string? GetAssemblyInformationalVersion()
    {
        try
        {
            return Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        }
        catch { return null; }
    }
}
