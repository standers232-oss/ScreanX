using System;
using System.IO;
using System.Media;
using System.Threading.Tasks;

namespace ScreanX.Services
{
    public static class SoundService
    {
        private static byte[]? _cachedShutterWav;

        static SoundService()
        {
            try
            {
                _cachedShutterWav = GenerateCameraShutterWav();
            }
            catch { }
        }

        public static void PlayCaptureSound()
        {
            if (!SettingsService.Current.PlaySound) return;

            Task.Run(() =>
            {
                try
                {
                    if (_cachedShutterWav != null)
                    {
                        using var ms = new MemoryStream(_cachedShutterWav);
                        using var sp = new SoundPlayer(ms);
                        sp.Play();
                    }
                    else
                    {
                        SystemSounds.Asterisk.Play();
                    }
                }
                catch
                {
                    try { SystemSounds.Asterisk.Play(); } catch { }
                }
            });
        }

        /// <summary>
        /// Generates a synthesized camera click sound WAV in memory.
        /// </summary>
        private static byte[] GenerateCameraShutterWav()
        {
            int sampleRate = 44100;
            double duration = 0.08; // 80 ms quick pleasant snap
            int totalSamples = (int)(sampleRate * duration);
            short[] samples = new short[totalSamples];
            var rand = new Random(42);

            for (int i = 0; i < totalSamples; i++)
            {
                double t = (double)i / sampleRate;
                // Envelope decay
                double envelope = Math.Exp(-t * 60.0);
                // Crisp click + noise
                double click = Math.Sin(2.0 * Math.PI * 1800.0 * t) * 0.4 + (rand.NextDouble() * 2.0 - 1.0) * 0.6;
                samples[i] = (short)(click * envelope * 22000.0);
            }

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            // RIFF header
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + totalSamples * 2);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16); // subchunk1 size
            writer.Write((short)1); // PCM
            writer.Write((short)1); // mono
            writer.Write(sampleRate);
            writer.Write(sampleRate * 2); // byte rate
            writer.Write((short)2); // block align
            writer.Write((short)16); // bits per sample
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(totalSamples * 2);

            foreach (var s in samples)
            {
                writer.Write(s);
            }

            return stream.ToArray();
        }
    }
}
