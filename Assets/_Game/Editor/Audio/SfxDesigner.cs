using System;
using OneBlock.Audio;
using UnityEngine;

namespace OneBlock.EditorTools.Audio
{
    /// <summary>Designs every sound effect from synthesis primitives. Jingles are in C so they suit any track.</summary>
    public static class SfxDesigner
    {
        sealed class Buf
        {
            public readonly float[] S;
            uint _seed = 7;
            public Buf(float seconds) => S = new float[Mathf.CeilToInt(seconds * Dsp.Rate)];

            void Add(int i, float v)
            {
                if (i >= 0 && i < S.Length) S[i] += v;
            }

            public void Tone(Instrument inst, float at, float dur, float midi, float vel = 1f)
            {
                int start = Mathf.RoundToInt(at * Dsp.Rate);
                Synth.RenderNote(inst, midi, midi, dur, vel, (i, s) => Add(start + i, s * inst.Gain), _seed++ * 2654435761u);
            }

            /// <summary>Oscillator with an exponential pitch glide and exponential decay.</summary>
            public void Sweep(Wave wave, float at, float dur, float hz0, float hz1, float gain, float decay, float attack = 0.002f, float vibHz = 0f, float vibDepth = 0f)
            {
                int start = Mathf.RoundToInt(at * Dsp.Rate), n = Mathf.RoundToInt(dur * Dsp.Rate);
                double phase = 0;
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)Dsp.Rate;
                    float k = t / dur;
                    float hz = hz0 * Mathf.Pow(hz1 / hz0, k);
                    if (vibHz > 0f) hz *= 1f + vibDepth * Mathf.Sin(Dsp.TwoPi * vibHz * t);
                    float dt = hz / Dsp.Rate;
                    float p = (float)phase;
                    float s = wave switch
                    {
                        Wave.Square => Dsp.Pulse(p, dt, 0.5f),
                        Wave.Saw => Dsp.Saw(p, dt),
                        Wave.Triangle => Dsp.Triangle(p),
                        _ => Mathf.Sin(Dsp.TwoPi * p)
                    };
                    phase += dt;
                    if (phase >= 1.0) phase -= Math.Floor(phase);
                    float env = Mathf.Min(1f, t / attack) * Dsp.ExpDecay(t, decay) * Mathf.Clamp01((dur - t) / 0.01f);
                    Add(start + i, s * env * gain);
                }
            }

            /// <summary>Filtered noise; band sweeps from hz0 to hz1. mode: low, band, high.</summary>
            public void Noise(float at, float dur, float hz0, float hz1, float gain, float decay, string mode = "band", float attack = 0.001f, float res = 0.3f)
            {
                int start = Mathf.RoundToInt(at * Dsp.Rate), n = Mathf.RoundToInt(dur * Dsp.Rate);
                var rng = new Dsp.Noise(_seed++ * 747796405u);
                var f = new Dsp.Svf();
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)Dsp.Rate;
                    float hz = hz0 * Mathf.Pow(hz1 / hz0, t / dur);
                    float x = rng.Next();
                    float s = mode == "low" ? f.Low(x, hz, res) : mode == "high" ? f.High(x, hz, res) : f.Band(x, hz, res) * 1.8f;
                    float env = Mathf.Min(1f, t / attack) * Dsp.ExpDecay(t, decay) * Mathf.Clamp01((dur - t) / 0.01f);
                    Add(start + i, s * env * gain);
                }
            }

            public void Drum(DrumKind kind, float at, float gain, DrumKit kit = null)
            {
                var d = Synth.RenderDrum(kind, kit ?? new DrumKit(), _seed++);
                int start = Mathf.RoundToInt(at * Dsp.Rate);
                for (int i = 0; i < d.Length; i++) Add(start + i, d[i] * gain);
            }

            public void Echo(float delay, float feedback, int taps)
            {
                int d = Mathf.RoundToInt(delay * Dsp.Rate);
                for (int tap = 1; tap <= taps; tap++)
                {
                    float g = Mathf.Pow(feedback, tap);
                    for (int i = S.Length - 1; i >= d * tap; i--) S[i] += S[i - d * tap] * g * 0.5f;
                }
            }

            public void Crush(int hold, float bits)
            {
                float levels = Mathf.Pow(2f, bits);
                float held = 0f;
                for (int i = 0; i < S.Length; i++)
                {
                    if (i % hold == 0) held = Mathf.Round(S[i] * levels) / levels;
                    S[i] = held;
                }
            }

            public void Drive(float amount)
            {
                for (int i = 0; i < S.Length; i++) S[i] = Dsp.SoftClip(S[i] * (1f + amount * 4f));
            }

            public float[] Finish(float peak)
            {
                float max = 0.0001f;
                int last = 0;
                for (int i = 0; i < S.Length; i++)
                {
                    float a = Mathf.Abs(S[i]);
                    if (a > max) max = a;
                    if (a > 0.0005f) last = i;
                }
                int len = Mathf.Min(S.Length, last + 256);
                var outBuf = new float[len];
                float g = peak / max;
                for (int i = 0; i < len; i++)
                {
                    float fadeOut = Mathf.Clamp01((len - i) / 200f);
                    outBuf[i] = S[i] * g * fadeOut;
                }
                return outBuf;
            }
        }

        // ------------------------------------------------------------------ patches

        static Instrument Bell(float ratio = 3.5f, float index = 2f, float decay = 0.15f, float r = 0.35f, float gain = 0.6f) =>
            new Instrument { Wave = Wave.Fm, FmRatio = ratio, FmIndex = index, FmDecay = decay, A = 0.001f, D = 0.2f, S = 0.25f, R = r, Gain = gain };

        static Instrument Chip(float gain = 0.4f, float cutoff = 3500f) =>
            new Instrument { Wave = Wave.Square, Filter = true, Cutoff = cutoff, A = 0.002f, D = 0.08f, S = 0.6f, R = 0.04f, Gain = gain };

        static Instrument Pluck(float decay = 0.3f, float bright = 0.8f, float gain = 0.6f) =>
            new Instrument { Wave = Wave.Pluck, PluckDecay = decay, PluckBright = bright, A = 0.001f, D = 1f, S = 1f, R = 0.05f, Gain = gain };

        static float M(string note) => Song.Midi(note);

        // ------------------------------------------------------------------ design

        public static float[] Design(SfxId id)
        {
            var rng = new System.Random((int)id * 97 + 13);
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            Buf b;
            switch (id)
            {
                // ---------------------------------------------------------- UI
                case SfxId.UiClick:
                    b = new Buf(0.1f);
                    b.Sweep(Wave.Sine, 0, 0.05f, 1500, 950, 0.7f, 0.014f);
                    b.Noise(0, 0.02f, 6000, 6000, 0.12f, 0.004f, "high");
                    return b.Finish(0.45f);
                case SfxId.UiTab:
                    b = new Buf(0.3f);
                    b.Tone(Pluck(0.15f, 0.9f), 0, 0.08f, M("E6"));
                    return b.Finish(0.45f);
                case SfxId.UiBack:
                    b = new Buf(0.3f);
                    b.Tone(Bell(2f, 1.2f, 0.08f, 0.1f), 0, 0.05f, M("G5"));
                    b.Tone(Bell(2f, 1.2f, 0.08f, 0.15f), 0.06f, 0.06f, M("D5"));
                    return b.Finish(0.45f);
                case SfxId.UiToggle:
                    b = new Buf(0.12f);
                    b.Sweep(Wave.Sine, 0, 0.07f, 700, 1400, 0.7f, 0.03f);
                    return b.Finish(0.45f);
                case SfxId.Purchase:
                    b = new Buf(1.0f);
                    b.Tone(Bell(3.5f, 2.5f, 0.05f, 0.1f), 0, 0.06f, M("B6"));
                    b.Tone(Bell(3.5f, 2.5f, 0.1f, 0.6f), 0.07f, 0.1f, M("E7"));
                    b.Noise(0.07f, 0.5f, 7000, 9000, 0.12f, 0.15f, "high");
                    b.Echo(0.09f, 0.35f, 2);
                    return b.Finish(0.8f);
                case SfxId.Denied:
                    b = new Buf(0.35f);
                    b.Tone(Chip(0.5f, 1200f), 0, 0.07f, M("A3"));
                    b.Tone(Chip(0.5f, 1200f), 0.11f, 0.1f, M("G#3"));
                    return b.Finish(0.6f);
                case SfxId.Equip:
                    b = new Buf(0.8f);
                    string[] up = { "C6", "E6", "G6", "C7" };
                    for (int i = 0; i < up.Length; i++) b.Tone(Bell(3.5f, 1.8f, 0.08f, 0.3f), i * 0.045f, 0.08f, M(up[i]), 1f - i * 0.1f);
                    return b.Finish(0.7f);
                case SfxId.AdReward:
                    b = new Buf(1.3f);
                    b.Noise(0, 0.4f, 400, 4500, 0.5f, 0.6f, "band", 0.3f);
                    foreach (var n in new[] { "C6", "E6", "G6", "C7" }) b.Tone(Bell(3.5f, 1.5f, 0.2f, 0.7f, 0.35f), 0.35f, 0.3f, M(n));
                    b.Echo(0.12f, 0.35f, 2);
                    return b.Finish(0.8f);
                case SfxId.Toast:
                    b = new Buf(0.12f);
                    b.Sweep(Wave.Sine, 0, 0.08f, 950, 520, 0.6f, 0.03f);
                    return b.Finish(0.35f);

                // ---------------------------------------------------------- movement & pickups
                case SfxId.Hop:
                    b = new Buf(0.12f);
                    b.Sweep(Wave.Square, 0, 0.08f, 360, 820, 0.25f, 0.05f);
                    return b.Finish(0.45f);
                case SfxId.WallBump:
                    b = new Buf(0.2f);
                    b.Sweep(Wave.Sine, 0, 0.14f, 160, 70, 0.9f, 0.06f);
                    b.Noise(0, 0.05f, 500, 300, 0.5f, 0.02f, "low");
                    return b.Finish(0.7f);
                case SfxId.Coin:
                    b = new Buf(0.5f);
                    b.Tone(Bell(3.5f, 2.2f, 0.05f, 0.28f), 0, 0.09f, 84f);
                    b.Tone(Bell(3.5f, 1.4f, 0.05f, 0.25f, 0.25f), 0.035f, 0.08f, 96f);
                    return b.Finish(0.7f);
                case SfxId.BlockPack:
                    b = new Buf(0.6f);
                    string[] arp = { "C5", "E5", "G5", "C6", "E6" };
                    for (int i = 0; i < arp.Length; i++) b.Tone(Chip(0.35f), i * 0.035f, 0.05f, M(arp[i]));
                    b.Noise(0.15f, 0.3f, 8000, 8000, 0.1f, 0.1f, "high");
                    return b.Finish(0.75f);
                case SfxId.BuildDenied:
                    b = new Buf(0.2f);
                    b.Sweep(Wave.Square, 0, 0.12f, 220, 170, 0.35f, 0.08f);
                    b.Noise(0, 0.02f, 2000, 2000, 0.4f, 0.006f);
                    return b.Finish(0.55f);

                // ---------------------------------------------------------- block placement
                case SfxId.Place_Normal:
                    b = new Buf(0.3f);
                    b.Sweep(Wave.Sine, 0, 0.09f, 540, 220, 0.9f, 0.045f);
                    b.Tone(Pluck(0.12f, 0.6f, 0.4f), 0.005f, 0.06f, M("G4"));
                    b.Noise(0, 0.015f, 2000, 2000, 0.5f, 0.004f, "low");
                    return b.Finish(0.8f);
                case SfxId.Place_Bouncy:
                    b = new Buf(0.4f);
                    b.Sweep(Wave.Sine, 0, 0.32f, 180, 520, 0.9f, 0.14f, 0.002f, 16f, 0.35f);
                    return b.Finish(0.8f);
                case SfxId.Place_Ice:
                    b = new Buf(0.6f);
                    b.Tone(Bell(5.2f, 3f, 0.04f, 0.35f), 0, 0.05f, M("E7"));
                    b.Tone(Bell(5.2f, 2f, 0.04f, 0.3f, 0.3f), 0.025f, 0.05f, M("A7"));
                    b.Noise(0, 0.04f, 8000, 8000, 0.2f, 0.01f, "high");
                    return b.Finish(0.7f);
                case SfxId.Place_Explosive:
                    b = new Buf(0.6f);
                    b.Drum(DrumKind.Kick, 0, 1f, new DrumKit { KickPitch = 45, KickDecay = 0.2f, KickDrive = 0.8f });
                    b.Noise(0.02f, 0.45f, 1400, 800, 0.35f, 0.14f);
                    return b.Finish(0.85f);
                case SfxId.Place_Ghost:
                    b = new Buf(0.8f);
                    b.Noise(0, 0.35f, 300, 1500, 0.3f, 0.2f, "band", 0.12f);
                    b.Sweep(Wave.Sine, 0.02f, 0.35f, 900, 450, 0.4f, 0.2f, 0.05f, 7f, 0.04f);
                    b.Echo(0.11f, 0.45f, 3);
                    return b.Finish(0.65f);
                case SfxId.Place_Stone:
                    b = new Buf(0.3f);
                    b.Noise(0, 0.05f, 1800, 1600, 1f, 0.015f);
                    b.Sweep(Wave.Sine, 0, 0.12f, 150, 90, 0.8f, 0.05f);
                    return b.Finish(0.8f);
                case SfxId.Place_Wood:
                    b = new Buf(0.25f);
                    b.Sweep(Wave.Sine, 0, 0.06f, 640, 600, 0.7f, 0.025f);
                    b.Sweep(Wave.Sine, 0.004f, 0.1f, 420, 380, 0.6f, 0.045f);
                    b.Noise(0, 0.015f, 900, 900, 0.5f, 0.006f);
                    return b.Finish(0.8f);
                case SfxId.Place_Grass:
                    b = new Buf(0.3f);
                    b.Sweep(Wave.Sine, 0, 0.1f, 210, 120, 0.9f, 0.05f);
                    b.Noise(0, 0.15f, 3000, 5000, 0.3f, 0.07f, "high");
                    return b.Finish(0.75f);
                case SfxId.Place_Crystal:
                    b = new Buf(1f);
                    b.Tone(Bell(4f, 2f, 0.1f, 0.6f, 0.4f), 0, 0.06f, M("A6"));
                    b.Tone(Bell(4f, 2f, 0.1f, 0.6f, 0.35f), 0.02f, 0.06f, M("C#7"));
                    b.Tone(Bell(4f, 2f, 0.1f, 0.6f, 0.3f), 0.04f, 0.06f, M("E7"));
                    return b.Finish(0.7f);
                case SfxId.Place_Galaxy:
                    b = new Buf(1f);
                    b.Sweep(Wave.Sine, 0, 0.35f, 400, 2400, 0.15f, 0.2f, 0.05f);
                    string[] stars = { "C7", "G7", "E7", "B7", "D7" };
                    for (int i = 0; i < stars.Length; i++) b.Tone(Bell(3.7f, 2.5f, 0.05f, 0.3f, 0.3f), i * 0.035f, 0.04f, M(stars[i]));
                    b.Echo(0.09f, 0.4f, 3);
                    return b.Finish(0.7f);
                case SfxId.Place_Candy:
                    b = new Buf(0.35f);
                    b.Sweep(Wave.Sine, 0, 0.07f, 300, 1150, 0.8f, 0.05f);
                    b.Tone(Pluck(0.12f, 0.95f, 0.35f), 0.04f, 0.05f, M("C7"));
                    return b.Finish(0.75f);
                case SfxId.Place_Coral:
                    b = new Buf(0.3f);
                    b.Sweep(Wave.Sine, 0, 0.06f, 500, 1600, 0.8f, 0.04f);
                    b.Sweep(Wave.Sine, 0.06f, 0.05f, 700, 2000, 0.6f, 0.03f);
                    return b.Finish(0.7f);
                case SfxId.Place_Neon:
                    b = new Buf(0.3f);
                    b.Sweep(Wave.Saw, 0, 0.13f, 2400, 300, 0.4f, 0.07f);
                    b.Tone(Chip(0.3f, 5000f), 0.01f, 0.03f, M("E6"));
                    b.Crush(3, 6f);
                    return b.Finish(0.7f);

                // ---------------------------------------------------------- squashes
                case SfxId.Squash_Shadow:
                    b = new Buf(0.3f);
                    b.Noise(0, 0.16f, 1600, 300, 0.7f, 0.08f);
                    b.Sweep(Wave.Sine, 0, 0.12f, 320, 110, 0.8f, 0.06f);
                    return b.Finish(0.85f);
                case SfxId.Squash_Scorpion:
                    b = new Buf(0.3f);
                    for (int i = 0; i < 7; i++) b.Noise(i * 0.016f + R(0f, 0.006f), 0.02f, 2600, 2200, R(0.5f, 1f), 0.006f);
                    b.Sweep(Wave.Sine, 0, 0.08f, 260, 140, 0.6f, 0.04f);
                    return b.Finish(0.85f);
                case SfxId.Squash_Penguin:
                    b = new Buf(0.25f);
                    b.Sweep(Wave.Sine, 0, 0.06f, 1250, 2100, 0.7f, 0.1f);
                    b.Sweep(Wave.Sine, 0.06f, 0.09f, 2100, 1050, 0.7f, 0.08f, 0.002f, 30f, 0.03f);
                    return b.Finish(0.8f);
                case SfxId.Squash_MagmaBlob:
                    b = new Buf(0.55f);
                    b.Noise(0, 0.45f, 4200, 3000, 0.45f, 0.18f, "high");
                    b.Sweep(Wave.Sine, 0, 0.18f, 190, 60, 0.9f, 0.08f);
                    return b.Finish(0.85f);
                case SfxId.Squash_Alien:
                    b = new Buf(0.35f);
                    b.Sweep(Wave.Sine, 0, 0.28f, 900, 190, 0.8f, 0.14f, 0.002f, 24f, 0.12f);
                    return b.Finish(0.8f);
                case SfxId.Squash_GummyBear:
                    b = new Buf(0.3f);
                    b.Sweep(Wave.Sine, 0, 0.14f, 120, 440, 0.9f, 0.08f, 0.002f, 14f, 0.08f);
                    b.Noise(0, 0.05f, 600, 400, 0.4f, 0.02f, "low");
                    return b.Finish(0.85f);
                case SfxId.Squash_Crab:
                    b = new Buf(0.3f);
                    b.Drum(DrumKind.Rim, 0, 1f);
                    b.Drum(DrumKind.Rim, 0.07f, 0.8f);
                    b.Sweep(Wave.Sine, 0.12f, 0.08f, 300, 160, 0.5f, 0.04f);
                    return b.Finish(0.85f);
                case SfxId.Squash_Drone:
                    b = new Buf(0.35f);
                    b.Sweep(Wave.Saw, 0, 0.22f, 1800, 150, 0.5f, 0.1f);
                    b.Noise(0, 0.1f, 2500, 1200, 0.5f, 0.04f);
                    b.Crush(4, 5f);
                    return b.Finish(0.85f);

                // ---------------------------------------------------------- deaths
                case SfxId.Death_Default:
                    b = new Buf(0.5f);
                    b.Tone(new Instrument { Wave = Wave.Square, Filter = true, Cutoff = 1800f, A = 0.002f, D = 0.3f, S = 0.6f, R = 0.08f, Gain = 0.4f, Glide = 0.25f }, 0, 0.28f, M("E5"));
                    b.Sweep(Wave.Square, 0, 0.3f, 520, 140, 0.35f, 0.18f);
                    b.Sweep(Wave.Sine, 0, 0.14f, 160, 70, 0.8f, 0.06f);
                    return b.Finish(0.85f);
                case SfxId.Death_Explosion:
                    b = new Buf(1.1f);
                    b.Sweep(Wave.Sine, 0, 0.7f, 95, 35, 1f, 0.3f);
                    b.Noise(0, 0.9f, 1200, 300, 0.9f, 0.35f, "low");
                    b.Drive(0.5f);
                    return b.Finish(0.95f);
                case SfxId.Death_Poof:
                    b = new Buf(0.6f);
                    b.Noise(0, 0.5f, 1600, 500, 1f, 0.2f, "low", 0.03f);
                    return b.Finish(0.85f);
                case SfxId.Death_Skull:
                    b = new Buf(1f);
                    string[] spooky = { "E5", "C5", "A4" };
                    for (int i = 0; i < spooky.Length; i++) b.Tone(Bell(1.41f, 2.5f, 0.3f, 0.4f, 0.5f), i * 0.13f, 0.12f, M(spooky[i]));
                    for (int i = 0; i < 8; i++) b.Drum(DrumKind.Rim, 0.05f + i * 0.04f + R(0f, 0.015f), R(0.4f, 0.8f));
                    return b.Finish(0.85f);
                case SfxId.Death_Confetti:
                    b = new Buf(1f);
                    b.Noise(0, 0.05f, 3000, 2000, 1f, 0.012f);
                    b.Sweep(Wave.Sine, 0.03f, 0.25f, 800, 2600, 0.35f, 0.3f);
                    for (int i = 0; i < 6; i++) b.Tone(Bell(3.5f, 1.5f, 0.05f, 0.25f, 0.3f), 0.12f + i * 0.05f + R(0f, 0.02f), 0.04f, M("C7") + new[] { 0, 4, 7, 12, 16, 19 }[i]);
                    return b.Finish(0.85f);
                case SfxId.Death_Sprinkles:
                    b = new Buf(0.8f);
                    for (int i = 0; i < 12; i++) b.Tone(Bell(3.5f, 2f, 0.03f, 0.15f, 0.3f), R(0f, 0.4f), 0.03f, R(88f, 104f));
                    b.Noise(0, 0.05f, 4000, 3000, 0.4f, 0.015f);
                    return b.Finish(0.8f);
                case SfxId.Death_Bubbles:
                    b = new Buf(0.8f);
                    for (int i = 0; i < 9; i++)
                    {
                        float h = R(400f, 900f);
                        b.Sweep(Wave.Sine, R(0f, 0.45f), 0.06f, h, h * R(2.5f, 3.5f), 0.5f, 0.04f);
                    }
                    return b.Finish(0.8f);
                case SfxId.Death_Glitch:
                    b = new Buf(0.6f);
                    for (int i = 0; i < 10; i++) b.Sweep(Wave.Square, i * 0.045f, 0.04f, R(150f, 1800f), R(150f, 1800f), R(0.2f, 0.45f), 0.05f);
                    b.Noise(0.2f, 0.12f, 3000, 800, 0.4f, 0.05f);
                    b.Crush(9, 4f);
                    return b.Finish(0.8f);
                case SfxId.FallWhistle:
                    b = new Buf(1.1f);
                    b.Sweep(Wave.Sine, 0, 0.95f, 1800, 260, 0.6f, 2f, 0.01f, 7f, 0.015f);
                    return b.Finish(0.65f);

                // ---------------------------------------------------------- moments & jingles
                case SfxId.Revive:
                    b = new Buf(0.9f);
                    b.Noise(0, 0.35f, 400, 5000, 0.4f, 0.5f, "band", 0.2f);
                    string[] rise = { "C5", "E5", "G5", "C6", "E6", "G6" };
                    for (int i = 0; i < rise.Length; i++) b.Tone(Chip(0.3f, 4000f), 0.2f + i * 0.045f, 0.06f, M(rise[i]));
                    return b.Finish(0.8f);
                case SfxId.TapToGo:
                    b = new Buf(0.7f);
                    b.Tone(Bell(3.5f, 1.8f, 0.1f, 0.45f), 0, 0.12f, M("G6"));
                    b.Tone(Bell(3.5f, 1.2f, 0.1f, 0.4f, 0.25f), 0.03f, 0.1f, M("D7"));
                    return b.Finish(0.7f);
                case SfxId.Tick:
                    b = new Buf(0.08f);
                    b.Sweep(Wave.Sine, 0, 0.04f, 1100, 1000, 0.7f, 0.015f);
                    b.Noise(0, 0.02f, 2500, 2500, 0.3f, 0.005f);
                    return b.Finish(0.5f);
                case SfxId.MissionComplete:
                    b = new Buf(1.1f);
                    string[] mc = { "C6", "E6", "G6" };
                    for (int i = 0; i < mc.Length; i++) b.Tone(Bell(3f, 1.6f, 0.1f, 0.25f), i * 0.075f, 0.07f, M(mc[i]));
                    b.Tone(Bell(3f, 2f, 0.2f, 0.7f), 0.225f, 0.3f, M("C7"));
                    b.Tone(Pluck(0.6f, 0.9f, 0.3f), 0.225f, 0.3f, M("G6"));
                    b.Noise(0.225f, 0.5f, 8000, 9000, 0.1f, 0.2f, "high");
                    return b.Finish(0.8f);
                case SfxId.LevelUp:
                    b = new Buf(1.6f);
                    var lead = new Instrument { Wave = Wave.Pulse, PulseWidth = 0.35f, A = 0.004f, D = 0.1f, S = 0.8f, R = 0.08f, Gain = 0.35f, VibDepth = 0.1f, VibDelay = 0.1f };
                    b.Tone(lead, 0f, 0.1f, M("G5"));
                    b.Tone(lead, 0.12f, 0.1f, M("C6"));
                    b.Tone(lead, 0.24f, 0.1f, M("E6"));
                    b.Tone(lead, 0.36f, 0.6f, M("G6"));
                    foreach (var n in new[] { "C5", "E5", "G5", "C6" }) b.Tone(Bell(2f, 1.2f, 0.3f, 0.6f, 0.2f), 0.36f, 0.6f, M(n));
                    b.Drum(DrumKind.Crash, 0.36f, 0.35f);
                    return b.Finish(0.85f);
                case SfxId.NewBest:
                    b = new Buf(1.8f);
                    var brass = new Instrument { Wave = Wave.Saw, Filter = true, Cutoff = 2400f, EnvCutoff = 2500f, FilterDecay = 0.15f, A = 0.01f, D = 0.2f, S = 0.8f, R = 0.15f, Gain = 0.25f };
                    string[] fan = { "C5", "E5", "G5", "C6", "E6", "G6" };
                    for (int i = 0; i < fan.Length; i++) b.Tone(brass, i * 0.065f, 0.06f, M(fan[i]));
                    foreach (var n in new[] { "C6", "E6", "G6", "C7" }) b.Tone(brass, 0.42f, 0.8f, M(n), 0.6f);
                    foreach (var n in new[] { "C6", "G6", "C7" }) b.Tone(Bell(3.5f, 2f, 0.2f, 0.8f, 0.2f), 0.42f, 0.2f, M(n));
                    b.Drum(DrumKind.Crash, 0.42f, 0.45f);
                    b.Drum(DrumKind.Kick, 0.42f, 0.6f);
                    return b.Finish(0.9f);
                case SfxId.GameOver:
                    b = new Buf(1.5f);
                    var sad = new Instrument { Wave = Wave.Square, Filter = true, Cutoff = 1500f, A = 0.01f, D = 0.1f, S = 0.8f, R = 0.1f, Gain = 0.3f, VibDepth = 0.25f, VibRate = 6f, VibDelay = 0.05f };
                    b.Tone(sad, 0f, 0.2f, M("G4"));
                    b.Tone(sad, 0.25f, 0.2f, M("F#4"));
                    b.Tone(sad, 0.5f, 0.2f, M("F4"));
                    b.Tone(sad, 0.75f, 0.55f, M("E4"));
                    return b.Finish(0.7f);
                case SfxId.DailyBonus:
                    b = new Buf(1.2f);
                    int[] penta = { 0, 2, 4, 7, 9, 12, 14, 16, 19, 21 };
                    for (int i = 0; i < penta.Length; i++) b.Tone(Bell(3.5f, 2.2f, 0.05f, 0.25f, 0.45f), i * 0.05f, 0.07f, 84f + penta[i]);
                    return b.Finish(0.8f);
                case SfxId.Milestone:
                    b = new Buf(0.8f);
                    b.Tone(Bell(3f, 1.5f, 0.15f, 0.4f), 0, 0.08f, M("C6"));
                    b.Tone(Bell(3f, 1.5f, 0.15f, 0.5f), 0.09f, 0.12f, M("G6"));
                    return b.Finish(0.6f);
                case SfxId.TutorialPop:
                    b = new Buf(0.15f);
                    b.Sweep(Wave.Sine, 0, 0.07f, 400, 950, 0.8f, 0.04f);
                    return b.Finish(0.6f);
                case SfxId.IntroSting:
                    b = new Buf(2.6f);
                    // Timed to IntroScreen: block falls (0.1-0.55), lands (0.55), "ONE" pops (0.75), BL/CK slide in (0.95), settle sparkle (1.35)
                    b.Noise(0.1f, 0.45f, 300, 3200, 0.35f, 1f, "band", 0.4f);
                    b.Drum(DrumKind.Kick, 0.55f, 1f, new DrumKit { KickPitch = 50, KickPunch = 150, KickDecay = 0.35f, KickDrive = 0.4f });
                    b.Noise(0.55f, 0.25f, 900, 400, 0.4f, 0.08f, "low");
                    b.Sweep(Wave.Sine, 0.76f, 0.07f, 500, 1250, 0.6f, 0.04f);
                    b.Tone(Bell(3.5f, 1.5f, 0.08f, 0.3f, 0.4f), 0.78f, 0.06f, M("G6"));
                    b.Noise(0.95f, 0.4f, 2400, 600, 0.3f, 0.5f, "band", 0.15f);
                    string[] sparkle = { "C6", "E6", "G6", "C7" };
                    for (int i = 0; i < sparkle.Length; i++) b.Tone(Bell(3.5f, 1.8f, 0.15f, 0.7f, 0.4f), 1.35f + i * 0.05f, 0.1f, M(sparkle[i]));
                    foreach (var n in new[] { "C5", "G5", "E6" }) b.Tone(Bell(2f, 0.8f, 0.4f, 0.9f, 0.18f), 1.5f, 0.6f, M(n));
                    b.Echo(0.13f, 0.3f, 2);
                    return b.Finish(0.85f);
            }
            return new float[64];
        }
    }
}
