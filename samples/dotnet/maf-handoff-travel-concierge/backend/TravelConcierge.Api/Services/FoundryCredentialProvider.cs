using Azure.Core;
using Azure.Identity;

namespace TravelConcierge.Api.Services;

public sealed class FoundryCredentialProvider(
    IHostEnvironment environment,
    ILogger<FoundryCredentialProvider> logger) : IDisposable
{
    public const string AuthModeEnvironmentVariable = "FOUNDRY_AUTH_MODE";
    private const string TokenCacheName = "TravelConcierge.Foundry";
    private const string AuthRecordFileName = "foundry-auth-record.json";
    private static readonly string[] RequiredScopes =
    [
        "https://ai.azure.com/.default",
        "https://cognitiveservices.azure.com/.default",
    ];

    private readonly SemaphoreSlim _credentialLock = new(1, 1);
    private TokenCredential? _credential;

    public async ValueTask<TokenCredential> GetCredentialAsync(CancellationToken cancellationToken = default)
    {
        if (_credential is not null)
        {
            return _credential;
        }

        await _credentialLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_credential is not null)
            {
                return _credential;
            }

            _credential = ShouldUseDefaultCredential()
                ? await CreateDefaultCredentialAsync(cancellationToken).ConfigureAwait(false)
                : await CreatePersistentInteractiveCredentialAsync(cancellationToken).ConfigureAwait(false);

            return _credential;
        }
        finally
        {
            _credentialLock.Release();
        }
    }

    public void Dispose()
    {
        _credentialLock.Dispose();
        (_credential as IDisposable)?.Dispose();
    }

    private bool ShouldUseDefaultCredential()
    {
        string? mode = System.Environment.GetEnvironmentVariable(AuthModeEnvironmentVariable);
        if (string.Equals(mode, "interactive", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.Equals(mode, "default", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return !environment.IsDevelopment() || HasServicePrincipalEnvironment();
    }

    private static bool HasServicePrincipalEnvironment() =>
        !string.IsNullOrWhiteSpace(System.Environment.GetEnvironmentVariable("AZURE_CLIENT_ID")) &&
        !string.IsNullOrWhiteSpace(System.Environment.GetEnvironmentVariable("AZURE_TENANT_ID")) &&
        (!string.IsNullOrWhiteSpace(System.Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET")) ||
         !string.IsNullOrWhiteSpace(System.Environment.GetEnvironmentVariable("AZURE_CLIENT_CERTIFICATE_PATH")));

    private async Task<TokenCredential> CreateDefaultCredentialAsync(CancellationToken cancellationToken)
    {
        DefaultAzureCredential credential = new();
        await ValidateScopesAsync(credential, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Foundry authentication is using DefaultAzureCredential.");
        return credential;
    }

    private async Task<TokenCredential> CreatePersistentInteractiveCredentialAsync(CancellationToken cancellationToken)
    {
        string recordPath = GetAuthenticationRecordPath();
        AuthenticationRecord? record = await LoadAuthenticationRecordAsync(recordPath).ConfigureAwait(false);
        InteractiveBrowserCredential credential = CreateInteractiveCredential(record);

        try
        {
            if (record is null)
            {
                record = await credential.AuthenticateAsync(cancellationToken).ConfigureAwait(false);
                await SaveAuthenticationRecordAsync(recordPath, record).ConfigureAwait(false);
            }

            await ValidateScopesAsync(credential, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Foundry authentication is using the persistent local browser credential.");
            return credential;
        }
        catch (Exception exception) when (record is not null && IsAuthenticationFailure(exception))
        {
            logger.LogWarning(exception, "The cached Foundry account is no longer usable. Starting a fresh browser sign-in.");
            TryDeleteAuthenticationRecord(recordPath);

            InteractiveBrowserCredential refreshedCredential = CreateInteractiveCredential(authenticationRecord: null);
            AuthenticationRecord refreshedRecord = await refreshedCredential.AuthenticateAsync(cancellationToken).ConfigureAwait(false);
            await SaveAuthenticationRecordAsync(recordPath, refreshedRecord).ConfigureAwait(false);
            await ValidateScopesAsync(refreshedCredential, cancellationToken).ConfigureAwait(false);
            return refreshedCredential;
        }
    }

    private static InteractiveBrowserCredential CreateInteractiveCredential(AuthenticationRecord? authenticationRecord)
    {
        InteractiveBrowserCredentialOptions options = new()
        {
            AuthenticationRecord = authenticationRecord,
            TokenCachePersistenceOptions = new TokenCachePersistenceOptions
            {
                Name = TokenCacheName,
            },
        };

        string? tenantId = System.Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            options.TenantId = tenantId;
        }

        return new InteractiveBrowserCredential(options);
    }

    private static async Task ValidateScopesAsync(TokenCredential credential, CancellationToken cancellationToken)
    {
        foreach (string scope in RequiredScopes)
        {
            await credential.GetTokenAsync(new TokenRequestContext([scope]), cancellationToken).ConfigureAwait(false);
        }
    }

    private static string GetAuthenticationRecordPath()
    {
        string directory = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            "TravelConcierge");
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, AuthRecordFileName);
    }

    private static async Task<AuthenticationRecord?> LoadAuthenticationRecordAsync(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            await using FileStream stream = File.OpenRead(path);
            return await AuthenticationRecord.DeserializeAsync(stream).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or FormatException)
        {
            TryDeleteAuthenticationRecord(path);
            return null;
        }
    }

    private static async Task SaveAuthenticationRecordAsync(string path, AuthenticationRecord record)
    {
        string temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
        await using (FileStream stream = File.Create(temporaryPath))
        {
            await record.SerializeAsync(stream).ConfigureAwait(false);
        }

        File.Move(temporaryPath, path, overwrite: true);
    }

    private static bool IsAuthenticationFailure(Exception exception) =>
        exception is AuthenticationFailedException or CredentialUnavailableException ||
        exception.InnerException is not null && IsAuthenticationFailure(exception.InnerException);

    private static void TryDeleteAuthenticationRecord(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
