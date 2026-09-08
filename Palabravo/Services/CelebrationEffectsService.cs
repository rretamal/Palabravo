using Plugin.Maui.Audio;

namespace Palabravo.Services;

public sealed class CelebrationEffectsService(IAudioManager audioManager)
{
    private const string SoundEnabledKey = "celebration_sound_enabled";
    private readonly HashSet<IAudioPlayer> activePlayers = [];

    public bool SoundEnabled
    {
        get => Preferences.Default.Get(SoundEnabledKey, true);
        set => Preferences.Default.Set(SoundEnabledKey, value);
    }

    public void PlayGroupFound() => Play([
        new Tone(523.25, 0.11),
        new Tone(659.25, 0.16)
    ], 0.42);

    public void PlayVictory() => Play([
        new Tone(523.25, 0.10),
        new Tone(659.25, 0.10),
        new Tone(783.99, 0.24)
    ], 0.52);

    public void PlayRankUp() => Play([
        new Tone(523.25, 0.09),
        new Tone(659.25, 0.09),
        new Tone(783.99, 0.09),
        new Tone(1046.50, 0.28)
    ], 0.54);

    public void ResetPreferences() => Preferences.Default.Remove(SoundEnabledKey);

    public static void PerformHaptic(bool success)
    {
        try
        {
            HapticFeedback.Default.Perform(success
                ? HapticFeedbackType.LongPress
                : HapticFeedbackType.Click);
        }
        catch (Exception exception) when (exception is FeatureNotSupportedException or PermissionException)
        {
        }
    }

    private void Play(IReadOnlyList<Tone> tones, double volume)
    {
        if (!SoundEnabled)
            return;

        MemoryStream? stream = null;
        IAudioPlayer? player = null;
        try
        {
            stream = new MemoryStream(CreateWave(tones));
            player = audioManager.CreatePlayer(stream);
            player.Volume = volume;

            var capturedPlayer = player;
            var capturedStream = stream;
            capturedPlayer.PlaybackEnded += (_, _) => Release(capturedPlayer, capturedStream);
            capturedPlayer.Error += (_, _) => Release(capturedPlayer, capturedStream);

            lock (activePlayers)
                activePlayers.Add(capturedPlayer);

            capturedPlayer.Play();
        }
        catch
        {
            if (player is not null && stream is not null)
                Release(player, stream);
            else
                stream?.Dispose();
        }
    }

    private void Release(IAudioPlayer player, Stream stream)
    {
        bool wasActive;
        lock (activePlayers)
            wasActive = activePlayers.Remove(player);

        if (!wasActive)
            return;

        player.Dispose();
        stream.Dispose();
    }

    private static byte[] CreateWave(IReadOnlyList<Tone> tones)
    {
        const int sampleRate = 44_100;
        const short channels = 1;
        const short bitsPerSample = 16;
        var sampleCount = tones.Sum(tone => (int)(sampleRate * tone.Duration));
        var dataSize = sampleCount * channels * bitsPerSample / 8;

        using var output = new MemoryStream(44 + dataSize);
        using var writer = new BinaryWriter(output);
        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + dataSize);
        writer.Write("WAVE"u8.ToArray());
        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(channels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * channels * bitsPerSample / 8);
        writer.Write((short)(channels * bitsPerSample / 8));
        writer.Write(bitsPerSample);
        writer.Write("data"u8.ToArray());
        writer.Write(dataSize);

        foreach (var tone in tones)
        {
            var toneSamples = (int)(sampleRate * tone.Duration);
            var attackSamples = Math.Max(1, (int)(sampleRate * 0.012));
            var releaseSamples = Math.Max(1, Math.Min(toneSamples / 2, (int)(sampleRate * 0.06)));

            for (var i = 0; i < toneSamples; i++)
            {
                var attack = Math.Min(1d, i / (double)attackSamples);
                var release = Math.Min(1d, (toneSamples - i - 1) / (double)releaseSamples);
                var envelope = Math.Min(attack, release);
                var time = i / (double)sampleRate;
                var fundamental = Math.Sin(2 * Math.PI * tone.Frequency * time);
                var overtone = Math.Sin(2 * Math.PI * tone.Frequency * 2 * time) * 0.18;
                writer.Write((short)(short.MaxValue * 0.34 * envelope * (fundamental + overtone)));
            }
        }

        return output.ToArray();
    }

    private readonly record struct Tone(double Frequency, double Duration);
}
