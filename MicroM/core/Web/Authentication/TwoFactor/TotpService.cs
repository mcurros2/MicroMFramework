using MicroM.Configuration;
using MicroM.DataDictionary.Entities;
using MicroM.Web.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SkiaSharp.QrCode.Image;
using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using static MicroM.Core.Base32;

namespace MicroM.Web.Authentication;

public class TotpService(
    ILogger<TotpService>? log = null,
    IMicroMAppConfiguration? app_config = null,
    IDeviceIdService? deviceIdService = null,
    ITwoFactorChallengeStore? challengeStore = null) : ITotpService
{
    private readonly ILogger<TotpService> _log = log ?? NullLogger<TotpService>.Instance;
    private readonly IMicroMAppConfiguration? _appConfig = app_config;
    private readonly IDeviceIdService? _deviceIdService = deviceIdService;
    private readonly ITwoFactorChallengeStore? _challengeStore = challengeStore;

    private IMicroMAppConfiguration AppConfig => _appConfig ?? throw new InvalidOperationException($"{nameof(IMicroMAppConfiguration)} is required for TOTP controller handlers.");
    private ITwoFactorChallengeStore ChallengeStore => _challengeStore ?? throw new InvalidOperationException($"{nameof(ITwoFactorChallengeStore)} is required for TOTP challenge handlers.");

    public static string GenerateTotpSecret()
    {
        // Generate a 160-bit (20 byte) random secret
        byte[] secretBytes = new byte[20];
        RandomNumberGenerator.Fill(secretBytes);
        return Base32Encode(secretBytes);
    }

    public string GenerateSecret() => GenerateTotpSecret();

    public async Task<TotpServiceResult> HandleStartTotpSetup(IAuthenticationProvider auth, string app_id, string user_name, TotpSetupRequest request, Dictionary<string, object> server_claims, CancellationToken ct)
    {
        var (result, app) = ValidateRequest(auth, app_id, user_name, "TOTP_SETUP");
        if (result != null || app == null) return result!;

        string authenticatorName = request.AuthenticatorName?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(authenticatorName))
        {
            _log.LogTrace("TOTP_SETUP: APP_ID {app_id} User: {username} empty authenticator name", app_id, user_name);
            return TotpServiceResult.Failed(TotpServiceResultStatus.InvalidUser);
        }

        using var ec = app.CreateDatabaseClient(_log, _deviceIdService, server_claims);
        await ec.Connect(ct);
        try
        {
            var loginData = await MicromUsers.GetUserData(app, user_name, null, string.Empty, ec, ct);
            if (loginData == null)
            {
                _log.LogTrace("TOTP_SETUP: APP_ID {app_id} User: {username} not found", app_id, user_name);
                return TotpServiceResult.Failed(TotpServiceResultStatus.InvalidUser);
            }

            string secret = GenerateSecret();
            string setupChallengeId = ChallengeStore.CreateChallenge(
                loginData.user_id,
                user_name,
                string.Empty,
                app.ApplicationID,
                string.Empty,
                new(StringComparer.OrdinalIgnoreCase)
                {
                    [TwoFactorChallengeMetadataKeys.Flow] = TwoFactorFlows.Authenticator,
                    [TwoFactorChallengeMetadataKeys.SetupTotpSecret] = secret,
                    [TwoFactorChallengeMetadataKeys.AuthenticatorName] = authenticatorName
                });

            string authenticatorUri = GetAuthenticatorUri(user_name, secret, app.ApplicationID);
            return TotpServiceResult.Success(new TotpSetupStartResponse
            {
                setup_challenge_id = setupChallengeId,
                qr_code_data_url = GetAuthenticatorQrCodeDataUrl(authenticatorUri)
            });
        }
        finally
        {
            await ec.Disconnect();
        }
    }

    public async Task<TotpServiceResult> HandleConfirmTotpSetup(IAuthenticationProvider auth, string app_id, string user_name, TotpConfirmRequest request, Dictionary<string, object> server_claims, CancellationToken ct)
    {
        var (result, app) = ValidateRequest(auth, app_id, user_name, "TOTP_CONFIRM");
        if (result != null || app == null) return result!;

        using var ec = app.CreateDatabaseClient(_log, _deviceIdService, server_claims);
        await ec.Connect(ct);
        try
        {
            if (string.IsNullOrWhiteSpace(request.SetupChallengeId))
            {
                _log.LogTrace("TOTP_CONFIRM: APP_ID {app_id} User: {username} empty setup challenge", app_id, user_name);
                return TotpServiceResult.Failed(TotpServiceResultStatus.SetupNotStarted);
            }

            var challenge = ChallengeStore.GetChallenge(request.SetupChallengeId);
            if (challenge == null || DateTime.UtcNow > challenge.ExpiresUtc || !challenge.Username.Equals(user_name, StringComparison.OrdinalIgnoreCase) || !challenge.ApplicationId.Equals(app.ApplicationID, StringComparison.OrdinalIgnoreCase))
            {
                _log.LogTrace("TOTP_CONFIRM: APP_ID {app_id} User: {username} setup challenge not found or expired", app_id, user_name);
                if (challenge != null) ChallengeStore.RemoveChallenge(request.SetupChallengeId);
                return TotpServiceResult.Failed(TotpServiceResultStatus.SetupNotStarted);
            }

            if (!challenge.Metadata.TryGetValue(TwoFactorChallengeMetadataKeys.SetupTotpSecret, out string? secret) || string.IsNullOrWhiteSpace(secret)
                || !challenge.Metadata.TryGetValue(TwoFactorChallengeMetadataKeys.AuthenticatorName, out string? authenticatorName) || string.IsNullOrWhiteSpace(authenticatorName))
            {
                _log.LogTrace("TOTP_CONFIRM: APP_ID {app_id} User: {username} setup has not been started", app_id, user_name);
                return TotpServiceResult.Failed(TotpServiceResultStatus.SetupNotStarted);
            }

            if (!VerifyCode(secret, request.Code))
            {
                _log.LogWarning("TOTP_CONFIRM: APP_ID {app_id} User: {username} invalid code", app_id, user_name);
                return TotpServiceResult.Failed(TotpServiceResultStatus.InvalidCode);
            }

            var dbResult = await MicromUsersAuthenticators.InsertConfirmed(app, challenge.UserId, authenticatorName, secret, ec, ct);
            if (dbResult.Failed)
            {
                _log.LogWarning("TOTP_CONFIRM: APP_ID {app_id} User: {username} failed to persist authenticator", app_id, user_name);
                return TotpServiceResult.Failed(TotpServiceResultStatus.DatabaseFailure, dbResult);
            }

            ChallengeStore.RemoveChallenge(request.SetupChallengeId);
            return TotpServiceResult.Success();
        }
        finally
        {
            await ec.Disconnect();
        }
    }

    public async Task<TotpServiceResult> HandleDisableTotp(IAuthenticationProvider auth, string app_id, string user_name, Dictionary<string, object> server_claims, CancellationToken ct)
    {
        var (result, app) = ValidateRequest(auth, app_id, user_name, "TOTP_DISABLE");
        if (result != null || app == null) return result!;

        using var ec = app.CreateDatabaseClient(_log, _deviceIdService, server_claims);
        await ec.Connect(ct);
        try
        {
            var loginData = await MicromUsers.GetUserData(app, user_name, null, string.Empty, ec, ct);
            if (loginData == null)
            {
                _log.LogTrace("TOTP_DISABLE: APP_ID {app_id} User: {username} not found", app_id, user_name);
                return TotpServiceResult.Failed(TotpServiceResultStatus.InvalidUser);
            }

            var dbResult = await MicromUsersAuthenticators.DeleteAll(app, loginData.user_id, ec, ct);
            if (dbResult.Failed)
            {
                _log.LogWarning("TOTP_DISABLE: APP_ID {app_id} User: {username} failed to reset TOTP", app_id, user_name);
                return TotpServiceResult.Failed(TotpServiceResultStatus.DatabaseFailure, dbResult);
            }

            return TotpServiceResult.Success();
        }
        finally
        {
            await ec.Disconnect();
        }
    }

    public async Task<TotpServiceResult> HandleLoginTotpRegistration(IAuthenticationProvider auth, string app_id, TwoFactorRegistrationRequest request, CancellationToken ct)
    {
        ApplicationOption? app = AppConfig.GetAppConfiguration(app_id);
        if (app == null)
        {
            _log.LogTrace("TOTP_REGISTER: Invalid APP_ID {app_id}", app_id);
            return TotpServiceResult.Failed(TotpServiceResultStatus.AppNotFound);
        }

        var challenge = ChallengeStore.GetChallenge(request.ChallengeId);
        if (challenge == null || DateTime.UtcNow > challenge.ExpiresUtc || !challenge.ApplicationId.Equals(app.ApplicationID, StringComparison.OrdinalIgnoreCase))
        {
            _log.LogWarning("TOTP_REGISTER: challenge {challenge_id} not found, expired, or for another application.", request.ChallengeId);
            if (challenge != null) ChallengeStore.RemoveChallenge(request.ChallengeId);
            return TotpServiceResult.Failed(TotpServiceResultStatus.InvalidUser);
        }

        var authenticator = auth.GetAuthenticator(app);
        if (authenticator is SQLServerAuthenticator)
        {
            if (!challenge.Metadata.TryGetValue(TwoFactorChallengeMetadataKeys.Flow, out string? flow)
                || !flow.Equals(TwoFactorFlows.SqlAdminSetup, StringComparison.OrdinalIgnoreCase)
                || !challenge.Metadata.TryGetValue(TwoFactorChallengeMetadataKeys.SetupTotpSecret, out string? setupSecret)
                || string.IsNullOrWhiteSpace(setupSecret))
            {
                _log.LogWarning("TOTP_REGISTER: challenge {challenge_id} is not an active SQL admin setup challenge.", request.ChallengeId);
                return TotpServiceResult.Failed(TotpServiceResultStatus.SetupNotStarted);
            }

            string authenticatorUri = GetAuthenticatorUri(challenge.Username, setupSecret, app.ApplicationID);
            return TotpServiceResult.Success(new TotpSetupStartResponse
            {
                setup_challenge_id = request.ChallengeId,
                qr_code_data_url = GetAuthenticatorQrCodeDataUrl(authenticatorUri)
            });
        }

        _log.LogError("TOTP_REGISTER: login-time TOTP registration is only supported for SQL Server initial setup. APP_ID {app_id} Authenticator: {authenticator}", app_id, app.AuthenticationType);
        return TotpServiceResult.Failed(TotpServiceResultStatus.UnsupportedAuthenticator);
    }

    public async Task<TotpAuthenticatorsResponse> HandleListAuthenticators(IAuthenticationProvider auth, string app_id, string user_name, Dictionary<string, object> server_claims, CancellationToken ct)
    {
        var (result, app) = ValidateRequest(auth, app_id, user_name, "TOTP_LIST");
        if (result != null || app == null) return new();

        using var ec = app.CreateDatabaseClient(_log, _deviceIdService, server_claims);
        await ec.Connect(ct);
        try
        {
            var loginData = await MicromUsers.GetUserData(app, user_name, null, string.Empty, ec, ct);
            if (loginData == null) return new();

            var authenticators = await MicromUsersAuthenticators.GetByUser(app, loginData.user_id, ec, ct);
            return new()
            {
                authenticators = authenticators.Select(item => new TotpAuthenticatorResponse
                {
                    authenticator_id = item.authenticator_id,
                    authenticator_name = item.authenticator_name
                }).ToList()
            };
        }
        finally
        {
            await ec.Disconnect();
        }
    }

    public async Task<TotpServiceResult> HandleDeleteAuthenticator(IAuthenticationProvider auth, string app_id, string user_name, TotpDeleteAuthenticatorRequest request, Dictionary<string, object> server_claims, CancellationToken ct)
    {
        var (result, app) = ValidateRequest(auth, app_id, user_name, "TOTP_DELETE");
        if (result != null || app == null) return result!;

        using var ec = app.CreateDatabaseClient(_log, _deviceIdService, server_claims);
        await ec.Connect(ct);
        try
        {
            var loginData = await MicromUsers.GetUserData(app, user_name, null, string.Empty, ec, ct);
            if (loginData == null) return TotpServiceResult.Failed(TotpServiceResultStatus.InvalidUser);

            var dbResult = await MicromUsersAuthenticators.Delete(app, loginData.user_id, request.AuthenticatorId, ec, ct);
            return dbResult.Failed
                ? TotpServiceResult.Failed(TotpServiceResultStatus.DatabaseFailure, dbResult)
                : TotpServiceResult.Success();
        }
        finally
        {
            await ec.Disconnect();
        }
    }

    private (TotpServiceResult? result, ApplicationOption? app) ValidateRequest(IAuthenticationProvider auth, string app_id, string user_name, string operation)
    {
        ApplicationOption? app = AppConfig.GetAppConfiguration(app_id);
        if (app == null)
        {
            _log.LogTrace("{operation}: Invalid APP_ID {app_id}", operation, app_id);
            return (TotpServiceResult.Failed(TotpServiceResultStatus.AppNotFound), null);
        }

        var authenticator = auth.GetAuthenticator(app);
        if (authenticator is not MicroMAuthenticator)
        {
            _log.LogError("{operation}: TOTP is only supported for MicroM authentication. APP_ID {app_id} Authenticator: {authenticator}", operation, app_id, app.AuthenticationType);
            return (TotpServiceResult.Failed(TotpServiceResultStatus.UnsupportedAuthenticator), app);
        }

        if (string.IsNullOrEmpty(user_name))
        {
            _log.LogTrace("{operation}: APP_ID {app_id} empty username", operation, app_id);
            return (TotpServiceResult.Failed(TotpServiceResultStatus.InvalidUser), app);
        }

        return (null, app);
    }

    /// <summary>
    /// Computes a TOTP code using RFC 6238 algorithm (same as ASP.NET Identity)
    /// </summary>
    private const int TotpModulo = 1000000;
    private const int Sha1HashSize = 20;

    internal static string ComputeTotp(ReadOnlySpan<byte> key, ulong timestep, ReadOnlySpan<byte> modifier)
    {
        if (key.Length < 16)
        {
            throw new ArgumentException("TOTP key must be at least 128 bits.", nameof(key));
        }

        if (modifier.Length > 1024)
        {
            throw new ArgumentException("Modifier is too large.", nameof(modifier));
        }

        int inputLength = sizeof(ulong) + modifier.Length;

        Span<byte> input = new byte[inputLength];

        Span<byte> hash = stackalloc byte[Sha1HashSize];

        BinaryPrimitives.WriteUInt64BigEndian(input[..sizeof(ulong)], timestep);
        modifier.CopyTo(input[sizeof(ulong)..]);

        int bytesWritten = HMACSHA1.HashData(key, input, hash);

        if (bytesWritten != Sha1HashSize)
        {
            throw new CryptographicException("Unexpected HMAC-SHA1 size.");
        }

        int offset = hash[^1] & 0x0F;

        int binary = BinaryPrimitives.ReadInt32BigEndian(hash.Slice(offset, 4)) & 0x7FFFFFFF;

        int otp = binary % TotpModulo;

        CryptographicOperations.ZeroMemory(hash);

        return otp.ToString("D6", CultureInfo.InvariantCulture);
    }

    public bool VerifyCode(string secret, string code, string? securityStampModifier = null, TotpSupportedDigits digits = TotpSupportedDigits.Six)
    {
        if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(code)) return false;

        if (code.Length != (int)digits || code.Any(c => !char.IsDigit(c))) return false;

        // Decode base32 secret to bytes
        byte[] secretBytes;
        try
        {
            secretBytes = Base32Decode(secret);
        }
        catch
        {
            return false;
        }

        if (secretBytes.Length < 16) return false;

        byte[]? modifierBytes = null;
        ReadOnlySpan<byte> modifier = ReadOnlySpan<byte>.Empty;
        if (!string.IsNullOrEmpty(securityStampModifier))
        {
            modifierBytes = Encoding.UTF8.GetBytes(securityStampModifier);
            if (modifierBytes.Length > 1024) return false;
            modifier = modifierBytes;
        }

        // Try current timestep and ±1 timestep for clock drift tolerance
        var unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long currentTimestep = unixTimestamp / 30; // 30-second timestep

        for (int i = -1; i <= 1; i++)
        {
            string expectedCode = ComputeTotp(secretBytes, (ulong)(currentTimestep + i), modifier);

            if (string.Equals(expectedCode, code, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public string GenerateCurrentCode(string secret, string? securityStampModifier = null)
    {
        byte[] secretBytes = Base32Decode(secret);
        byte[]? modifierBytes = null;
        ReadOnlySpan<byte> modifier = ReadOnlySpan<byte>.Empty;
        if (!string.IsNullOrEmpty(securityStampModifier))
        {
            modifierBytes = Encoding.UTF8.GetBytes(securityStampModifier);
            modifier = modifierBytes;
        }

        ulong timestep = (ulong)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30);
        return ComputeTotp(secretBytes, timestep, modifier);
    }

    public string GetAuthenticatorUri(string username, string secret, string issuer = "MicroM", TotpSupportedDigits digits = TotpSupportedDigits.Six)
    {
        return $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(username)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}&digits={(int)digits}";
    }

    public string GetAuthenticatorQrCodeDataUrl(string authenticatorUri)
    {
        if (string.IsNullOrWhiteSpace(authenticatorUri)) return string.Empty;

        byte[] pngBytes = QRCodeImageBuilder.GetPngBytes(authenticatorUri);
        return $"data:image/png;base64,{Convert.ToBase64String(pngBytes)}";
    }

    public string FormatSecret(string secret)
    {
        if (string.IsNullOrWhiteSpace(secret)) return string.Empty;

        secret = secret.Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();

        return string.Join(" ", Enumerable.Range(0, (secret.Length + 3) / 4).Select(i => secret.Substring(i * 4, Math.Min(4, secret.Length - (i * 4)))));
    }


}
