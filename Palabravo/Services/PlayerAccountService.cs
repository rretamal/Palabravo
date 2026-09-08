namespace Palabravo.Services;

public interface IGooglePlayGamesAuthService
{
    bool IsSupported { get; }
    Task<string> RequestServerAuthCodeAsync(CancellationToken cancellationToken = default);
}

public interface IPlayerAccountService
{
    PlayerAccountSnapshot GetSnapshot();
    Task<PlayerAccountActionResult> ContinueWithGoogleAsync(CancellationToken cancellationToken = default);
    Task<PlayerAccountDeletionResult> DeleteAccountAsync(CancellationToken cancellationToken = default);
}

public sealed record PlayerAccountSnapshot(
    string DisplayName,
    bool IsGoogleLinked,
    bool CanUseGooglePlayGames);

public sealed record PlayerAccountActionResult(bool IsSuccess, string Message);

public sealed record PlayerAccountDeletionResult(
    bool IsSuccess,
    string Message,
    string? RequestId = null);

public sealed class UnsupportedGooglePlayGamesAuthService : IGooglePlayGamesAuthService
{
    public bool IsSupported => false;

    public Task<string> RequestServerAuthCodeAsync(CancellationToken cancellationToken = default) =>
        throw new PlatformNotSupportedException("Google Play Games solo está disponible en Android.");
}
