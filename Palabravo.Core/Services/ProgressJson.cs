using System.Text.Json;
using Palabravo.Core.Models;

namespace Palabravo.Core.Services;

public static class ProgressJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Serialize(PlayerProgress progress) => JsonSerializer.Serialize(progress, Options);

    public static PlayerProgress DeserializeSafe(string? json) => DeserializeSafe(json, out _);

    public static PlayerProgress DeserializeSafe(string? json, out bool migrated)
    {
        migrated = false;
        if (string.IsNullOrWhiteSpace(json))
            return new PlayerProgress();

        try
        {
            var progress = JsonSerializer.Deserialize<PlayerProgress>(json, Options);
            if (progress?.SchemaVersion == PlayerProgress.CurrentSchemaVersion)
            {
                progress.SpecialBadges ??= [];
                return progress;
            }

            if (progress?.SchemaVersion == 2)
            {
                progress.SchemaVersion = PlayerProgress.CurrentSchemaVersion;
                progress.SpecialBadges ??= [];
                migrated = true;
                return progress;
            }

            // El catálogo definitivo reemplaza los retos 2 y 3 del prototipo. La
            // migración conserva el onboarding, pero reinicia todo progreso jugable.
            if (progress?.SchemaVersion == 1)
            {
                migrated = true;
                return new PlayerProgress { TutorialSeen = progress.TutorialSeen };
            }

            return new PlayerProgress();
        }
        catch (JsonException)
        {
            return new PlayerProgress();
        }
    }
}
