using System;
using System.Collections.Generic;
using UnityEngine;

namespace OneBlock.EditorTools.Audio
{
    public enum Wave { Square, Pulse, Saw, SuperSaw, Triangle, Sine, Fm, Pluck, Organ, Reed }

    /// <summary>A synth patch.</summary>
    public sealed class Instrument
    {
        public Wave Wave = Wave.Square;
        public float Gain = 0.25f;
        public float Pan;
        public int Octave;

        public float A = 0.005f, D = 0.12f, S = 0.7f, R = 0.08f;

        public bool Filter;
        public float Cutoff = 6000f, EnvCutoff, FilterDecay = 0.25f, Resonance = 0.15f;

        public float PulseWidth = 0.5f, PwmDepth, PwmRate = 0.5f;
        public float VibDepth, VibRate = 5.5f, VibDelay = 0.12f;
        public float Detune = 0.15f;
        public int Voices = 5;
        public float FmRatio = 2f, FmIndex = 2f, FmDecay = 0.4f;
        public float PluckDecay = 0.4f, PluckBright = 0.6f;
        public float Drive;
        public float Glide;

        public float Reverb = 0.15f, Delay;
        public float Duck;        // 0..1 sidechain from the kick
    }

    public enum DrumKind { Kick, Snare, Clap, HatClosed, HatOpen, Crash, Tom, Shaker, Rim, Doum, Tek, Bubble, Snap }

    /// <summary>Per-song drum sound design.</summary>
    public sealed class DrumKit
    {
        public float KickPitch = 52f, KickPunch = 180f, KickDecay = 0.32f, KickDrive = 0.3f;
        public float SnareTone = 185f, SnareNoise = 0.8f, SnareDecay = 0.18f, SnareReverb = 0.25f;
        public bool GatedSnare;
        public float HatDecay = 0.045f, HatTone = 8000f;
        public float Gain = 1f;
    }

    /// <summary>Renders synth notes and drum hits into stereo buffers.</summary>
    public static class Synth
    {
        // ------------------------------------------------------------------ tones

        /// <summary>Renders one note into a mono buffer (stereo panning is applied by the caller via writer).</summary>
        public static void RenderNote(Instrument inst, float midi, float fromMidi, float durSec, float vel, Action<int, float> write, uint seed)
        {
            float tail = inst.Wave == Wave.Pluck ? Mathf.Max(inst.R, inst.PluckDecay * 2.5f) : inst.R;
            int total = Mathf.CeilToInt((durSec + tail) * Dsp.Rate);
            midi += inst.Octave * 12;
            fromMidi += inst.Octave * 12;

            var svf = new Dsp.Svf();
            var noise = new Dsp.Noise(seed);
            double phase = 0, phase2 = 0;
            double[] uni = inst.Wave == Wave.SuperSaw ? new double[Mathf.Max(1, inst.Voices)] : null;
            if (uni != null) for (int v = 0; v < uni.Length; v++) uni[v] = Dsp.Frac(v * 0.618f);

            float[] ks = null;
            int ksIdx = 0;
            float ksFilter = Mathf.Lerp(0.2f, 0.95f, inst.PluckBright);
            if (inst.Wave == Wave.Pluck)
            {
                int n = Mathf.Max(2, Mathf.RoundToInt(Dsp.Rate / Dsp.MidiToHz(midi)));
                ks = new float[n];
                float lp = 0f;
                for (int i = 0; i < n; i++)
                {
                    lp += (noise.Next() - lp) * ksFilter;
                    ks[i] = lp;
                }
            }
            // Per-sample loss so the pluck decays by 60 dB in PluckDecay seconds regardless of pitch
            float ksLoss = ks != null ? Mathf.Pow(0.001f, 1f / (Mathf.Max(0.05f, inst.PluckDecay) * Dsp.MidiToHz(midi))) : 1f;

            for (int i = 0; i < total; i++)
            {
                float t = i / (float)Dsp.Rate;
                float env = Dsp.Adsr(t, durSec, inst.A, inst.D, inst.S, inst.R);
                if (env <= 0f && t > durSec) break;

                float m = midi;
                if (inst.Glide > 0f && fromMidi != midi) m = Mathf.Lerp(fromMidi, midi, Mathf.Clamp01(t / inst.Glide));
                if (inst.VibDepth > 0f)
                    m += inst.VibDepth * Mathf.Sin(Dsp.TwoPi * inst.VibRate * t) * Mathf.Clamp01((t - inst.VibDelay) / 0.25f);
                float hz = Dsp.MidiToHz(m);
                float dt = hz / Dsp.Rate;

                float s;
                switch (inst.Wave)
                {
                    case Wave.Square:
                        s = Dsp.Pulse((float)phase, dt, 0.5f);
                        break;
                    case Wave.Pulse:
                    case Wave.Reed:
                    {
                        float w = inst.PulseWidth + inst.PwmDepth * Mathf.Sin(Dsp.TwoPi * inst.PwmRate * t);
                        s = Dsp.Pulse((float)phase, dt, Mathf.Clamp(w, 0.05f, 0.95f));
                        break;
                    }
                    case Wave.Saw:
                        s = Dsp.Saw((float)phase, dt);
                        break;
                    case Wave.SuperSaw:
                    {
                        s = 0f;
                        for (int v = 0; v < uni.Length; v++)
                        {
                            float spread = uni.Length > 1 ? (v / (float)(uni.Length - 1) - 0.5f) * 2f : 0f;
                            float vhz = hz * Mathf.Pow(2f, spread * inst.Detune / 12f);
                            float vdt = vhz / Dsp.Rate;
                            s += Dsp.Saw((float)uni[v], vdt);
                            uni[v] += vdt;
                            if (uni[v] >= 1.0) uni[v] -= 1.0;
                        }
                        s /= Mathf.Sqrt(uni.Length);
                        break;
                    }
                    case Wave.Triangle:
                        s = Dsp.Triangle((float)phase);
                        break;
                    case Wave.Sine:
                        s = Mathf.Sin(Dsp.TwoPi * (float)phase);
                        break;
                    case Wave.Fm:
                    {
                        float index = inst.FmIndex * Dsp.ExpDecay(t, inst.FmDecay);
                        float mod = Mathf.Sin(Dsp.TwoPi * (float)phase2);
                        s = Mathf.Sin(Dsp.TwoPi * (float)phase + index * mod);
                        phase2 += dt * inst.FmRatio;
                        if (phase2 >= 1.0) phase2 -= Math.Floor(phase2);
                        break;
                    }
                    case Wave.Organ:
                    {
                        float p = Dsp.TwoPi * (float)phase;
                        s = Mathf.Sin(p) * 0.6f + Mathf.Sin(p * 2f) * 0.3f + Mathf.Sin(p * 3f) * 0.18f + Mathf.Sin(p * 4f) * 0.1f;
                        break;
                    }
                    case Wave.Pluck:
                    {
                        int next = ksIdx + 1 >= ks.Length ? 0 : ksIdx + 1;
                        s = ks[ksIdx];
                        ks[ksIdx] = (ks[ksIdx] * 0.5f + ks[next] * 0.5f) * ksLoss;
                        ksIdx = next;
                        s *= 1.6f;
                        break;
                    }
                    default:
                        s = 0f;
                        break;
                }
                if (inst.Wave != Wave.SuperSaw && inst.Wave != Wave.Pluck)
                {
                    phase += dt;
                    if (phase >= 1.0) phase -= Math.Floor(phase);
                }

                if (inst.Wave == Wave.Reed) s = svf.Band(s, 1400f + 900f * env, 0.35f) * 2.2f;
                else if (inst.Filter)
                {
                    float fenv = Dsp.ExpDecay(t, inst.FilterDecay);
                    s = svf.Low(s, inst.Cutoff + inst.EnvCutoff * fenv, inst.Resonance);
                }
                if (inst.Drive > 0f) s = Dsp.SoftClip(s * (1f + inst.Drive * 6f)) / Mathf.Lerp(1f, 0.85f, inst.Drive);

                write(i, s * env * vel);
            }
        }

        // ------------------------------------------------------------------ drums

        public static float[] RenderDrum(DrumKind kind, DrumKit kit, uint seed)
        {
            var noise = new Dsp.Noise(seed);
            float len = kind switch
            {
                DrumKind.Kick => kit.KickDecay * 2.2f,
                DrumKind.Snare => kit.GatedSnare ? 0.42f : kit.SnareDecay * 3f,
                DrumKind.Clap => 0.45f,
                DrumKind.HatClosed => kit.HatDecay * 4f,
                DrumKind.HatOpen => 0.45f,
                DrumKind.Crash => 2.2f,
                DrumKind.Tom => 0.5f,
                DrumKind.Shaker => 0.12f,
                DrumKind.Rim => 0.08f,
                DrumKind.Doum => 0.5f,
                DrumKind.Tek => 0.12f,
                DrumKind.Bubble => 0.18f,
                DrumKind.Snap => 0.15f,
                _ => 0.3f
            };
            int n = Mathf.CeilToInt(len * Dsp.Rate);
            var buf = new float[n];
            var f1 = new Dsp.Svf();
            var f2 = new Dsp.Svf();
            double phase = 0;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Dsp.Rate;
                float s = 0f;
                switch (kind)
                {
                    case DrumKind.Kick:
                    {
                        float hz = kit.KickPitch + kit.KickPunch * Dsp.ExpDecay(t, 0.035f);
                        phase += hz / Dsp.Rate;
                        s = Mathf.Sin(Dsp.TwoPi * (float)phase) * Dsp.ExpDecay(t, kit.KickDecay);
                        s += noise.Next() * Dsp.ExpDecay(t, 0.004f) * 0.35f;
                        s = Dsp.SoftClip(s * (1f + kit.KickDrive * 3f));
                        break;
                    }
                    case DrumKind.Snare:
                    {
                        phase += kit.SnareTone / Dsp.Rate;
                        float body = Mathf.Sin(Dsp.TwoPi * (float)phase) * Dsp.ExpDecay(t, 0.06f) * 0.55f;
                        float nz = f1.High(noise.Next(), 1500f, 0.1f);
                        float decay = kit.GatedSnare ? (t < 0.3f ? Dsp.ExpDecay(t, 0.35f) : Dsp.ExpDecay(t - 0.3f, 0.015f) * 0.42f) : Dsp.ExpDecay(t, kit.SnareDecay);
                        s = body + nz * decay * kit.SnareNoise;
                        break;
                    }
                    case DrumKind.Clap:
                    {
                        float burst = 0f;
                        for (int b = 0; b < 3; b++)
                        {
                            float bt = t - b * 0.011f;
                            if (bt >= 0f) burst = Mathf.Max(burst, Dsp.ExpDecay(bt, b == 2 ? 0.12f : 0.008f));
                        }
                        s = f1.Band(noise.Next(), 1300f, 0.4f) * burst * 2.2f;
                        break;
                    }
                    case DrumKind.HatClosed:
                        s = f1.High(noise.Next(), kit.HatTone, 0.2f) * Dsp.ExpDecay(t, kit.HatDecay) * 0.7f;
                        break;
                    case DrumKind.HatOpen:
                        s = f1.High(noise.Next(), kit.HatTone * 0.9f, 0.2f) * Dsp.ExpDecay(t, 0.16f) * 0.6f;
                        break;
                    case DrumKind.Crash:
                        s = f2.High(f1.High(noise.Next(), 4000f, 0.1f), 3000f, 0.1f) * Dsp.ExpDecay(t, 0.7f) * 0.6f;
                        break;
                    case DrumKind.Tom:
                    {
                        float hz = 90f + 70f * Dsp.ExpDecay(t, 0.05f);
                        phase += hz / Dsp.Rate;
                        s = Mathf.Sin(Dsp.TwoPi * (float)phase) * Dsp.ExpDecay(t, 0.18f);
                        break;
                    }
                    case DrumKind.Shaker:
                        s = f1.Band(noise.Next(), 6500f, 0.3f) * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.1f)) * 0.8f;
                        break;
                    case DrumKind.Rim:
                    {
                        phase += 1700f / Dsp.Rate;
                        s = (Mathf.Sin(Dsp.TwoPi * (float)phase) * 0.6f + f1.Band(noise.Next(), 2500f, 0.5f) * 0.5f) * Dsp.ExpDecay(t, 0.018f);
                        break;
                    }
                    case DrumKind.Doum:
                    {
                        float hz = 95f + 60f * Dsp.ExpDecay(t, 0.02f);
                        phase += hz / Dsp.Rate;
                        s = Mathf.Sin(Dsp.TwoPi * (float)phase) * Dsp.ExpDecay(t, 0.22f) + f1.Low(noise.Next(), 900f, 0.1f) * Dsp.ExpDecay(t, 0.01f) * 0.4f;
                        break;
                    }
                    case DrumKind.Tek:
                    {
                        phase += 620f / Dsp.Rate;
                        s = (Mathf.Sin(Dsp.TwoPi * (float)phase) * 0.5f + f1.Band(noise.Next(), 3200f, 0.4f) * 0.9f) * Dsp.ExpDecay(t, 0.03f);
                        break;
                    }
                    case DrumKind.Bubble:
                    {
                        float hz = 500f + 1800f * (t / 0.18f);
                        phase += hz / Dsp.Rate;
                        s = Mathf.Sin(Dsp.TwoPi * (float)phase) * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.18f)) * 0.5f;
                        break;
                    }
                    case DrumKind.Snap:
                        s = f1.Band(noise.Next(), 2200f, 0.6f) * Dsp.ExpDecay(t, 0.02f) * 1.6f;
                        break;
                }
                buf[i] = s * kit.Gain;
            }
            return buf;
        }
    }

    // ====================================================================== song

    /// <summary>
    /// A song under construction: note and drum events on a beat grid, rendered into two stems ("main" and
    /// the adaptive "drive" layer) that loop seamlessly.
    /// </summary>
    public sealed class Song
    {
        public string Id, Title;
        public float Bpm = 120f;
        public int KeyRoot = 60;              // midi of the tonic (used for musical coin pickups)
        public int[] Scale = { 0, 2, 4, 5, 7, 9, 11 };
        public int Bars = 32;
        public float Swing;                   // 0..0.5 delay of off-beat 16ths
        public DrumKit Kit = new();
        public float ReverbRoom = 0.78f, ReverbDamp = 0.45f, ReverbMix = 1f;
        public float DelayBeats = 0.75f, DelayFeedback = 0.35f;
        public float DriveGain = 1.8f;

        struct NoteEv { public Instrument Inst; public float Beat, Beats, Midi, From, Vel; public bool Drive; }
        struct DrumEv { public DrumKind Kind; public float Beat, Vel; public bool Drive; }

        readonly List<NoteEv> _notes = new();
        readonly List<DrumEv> _drums = new();
        readonly Dictionary<Instrument, float> _lastMidi = new();

        public float SecPerBeat => 60f / Bpm;
        public float LengthSeconds => Bars * 4 * SecPerBeat;

        float SwingBeat(float beat)
        {
            if (Swing <= 0f) return beat;
            float step = beat * 4f;
            int whole = Mathf.FloorToInt(step + 0.0001f);
            if (Mathf.Abs(step - whole) < 0.001f && whole % 2 == 1) return beat + Swing * 0.25f;
            return beat;
        }

        public void Note(Instrument inst, float beat, float beats, float midi, float vel = 1f, bool drive = false)
        {
            float from = _lastMidi.TryGetValue(inst, out var last) ? last : midi;
            _lastMidi[inst] = midi;
            _notes.Add(new NoteEv { Inst = inst, Beat = SwingBeat(beat), Beats = beats, Midi = midi, From = from, Vel = vel, Drive = drive });
        }

        public void Hit(DrumKind kind, float beat, float vel = 1f, bool drive = false) =>
            _drums.Add(new DrumEv { Kind = kind, Beat = SwingBeat(beat), Vel = vel, Drive = drive });

        // ------------------------------------------------------------------ notation helpers

        static readonly Dictionary<char, int> NoteBase = new() { ['C'] = 0, ['D'] = 2, ['E'] = 4, ['F'] = 5, ['G'] = 7, ['A'] = 9, ['B'] = 11 };

        /// <summary>"C#5" / "Bb4" to midi (C4 = 60).</summary>
        public static int Midi(string name)
        {
            int i = 0;
            int pc = NoteBase[char.ToUpperInvariant(name[i++])];
            while (i < name.Length && (name[i] == '#' || name[i] == 'b'))
            {
                pc += name[i] == '#' ? 1 : -1;
                i++;
            }
            int octave = int.Parse(name.Substring(i));
            return (octave + 1) * 12 + pc;
        }

        /// <summary>
        /// Melody in 16th-note steps: "E5:2 G5:2 C6:4 _:8". Returns the parsed notes for harmonising.
        /// A bar is 16 steps; the string may span several bars.
        /// </summary>
        public List<(float beat, float beats, int midi)> Melody(Instrument inst, int bar, string notes, float vel = 1f, bool drive = false, int transpose = 0, float legato = 0.92f)
        {
            var parsed = new List<(float, float, int)>();
            float step = 0f;
            foreach (var tok in notes.Split(new[] { ' ', '\n', '\t', '|' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = tok.Split(':');
                float len = parts.Length > 1 ? float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture) : 2f;
                if (parts[0] != "_")
                {
                    int midi = Midi(parts[0]) + transpose;
                    float beat = bar * 4 + step / 4f;
                    float beats = len / 4f;
                    Note(inst, beat, beats * legato, midi, vel, drive);
                    parsed.Add((beat, beats * legato, midi));
                }
                step += len;
            }
            return parsed;
        }

        /// <summary>Plays each melody note shifted down by scale degrees (e.g. 2 = a diatonic third below).</summary>
        public void Harmonise(Instrument inst, List<(float beat, float beats, int midi)> melody, int degreesDown, float vel, bool drive = false)
        {
            foreach (var (beat, beats, midi) in melody)
                Note(inst, beat, beats, ShiftDegrees(midi, -degreesDown), vel, drive);
        }

        public int ShiftDegrees(int midi, int degrees)
        {
            int rel = midi - KeyRoot;
            int oct = Mathf.FloorToInt(rel / 12f);
            int pc = rel - oct * 12;
            int idx = 0, best = int.MaxValue;
            for (int i = 0; i < Scale.Length; i++)
            {
                int d = Mathf.Abs(Scale[i] - pc);
                if (d < best) { best = d; idx = i; }
            }
            int target = idx + degrees;
            int octShift = Mathf.FloorToInt(target / (float)Scale.Length);
            int wrapped = target - octShift * Scale.Length;
            return KeyRoot + (oct + octShift) * 12 + Scale[wrapped] + (pc - Scale[idx]);
        }

        // ------------------------------------------------------------------ chords

        public struct Chord
        {
            public int Root;       // pitch class 0..11
            public int[] Tones;    // intervals from the root
        }

        public static Chord ParseChord(string symbol)
        {
            int i = 1;
            int root = NoteBase[char.ToUpperInvariant(symbol[0])];
            if (i < symbol.Length && (symbol[i] == '#' || symbol[i] == 'b'))
            {
                root += symbol[i] == '#' ? 1 : -1;
                i++;
            }
            root = (root + 12) % 12;
            string q = symbol.Substring(i);
            int[] tones = q switch
            {
                "m" => new[] { 0, 3, 7 },
                "m7" => new[] { 0, 3, 7, 10 },
                "7" => new[] { 0, 4, 7, 10 },
                "maj7" => new[] { 0, 4, 7, 11 },
                "sus2" => new[] { 0, 2, 7 },
                "sus4" => new[] { 0, 5, 7 },
                "dim" => new[] { 0, 3, 6 },
                "5" => new[] { 0, 7, 12 },
                "add9" => new[] { 0, 4, 7, 14 },
                _ => new[] { 0, 4, 7 }
            };
            return new Chord { Root = root, Tones = tones };
        }

        /// <summary>Chord tones voiced as close as possible around a centre pitch.</summary>
        public static List<int> Voice(Chord c, int center)
        {
            int baseMidi = center - 6 + ((c.Root - (center - 6)) % 12 + 12) % 12;
            var list = new List<int>();
            foreach (int t in c.Tones) list.Add(baseMidi + t);
            list.Sort();
            return list;
        }

        public static int BassNote(Chord c, int center) => center - 6 + ((c.Root - (center - 6)) % 12 + 12) % 12;

        /// <summary>16-step bass pattern: r root, o octave, f fifth, t third, - hold, . rest.</summary>
        public void Bass(Instrument inst, int bar, string chord, string pattern, int center = 40, float vel = 1f, bool drive = false)
        {
            var c = ParseChord(chord);
            int root = BassNote(c, center);
            ForPattern(pattern, (step, len, ch) =>
            {
                int midi = char.ToLowerInvariant(ch) switch
                {
                    'o' => root + 12,
                    'f' => root + 7,
                    't' => root + c.Tones[1],
                    'l' => root - 5,
                    _ => root
                };
                float v = char.IsUpper(ch) ? vel : vel * 0.85f;
                Note(inst, bar * 4 + step / 4f, len / 4f * 0.9f, midi, v, drive);
            });
        }

        /// <summary>16-step chord stabs: x hit, X accent, - hold, . rest.</summary>
        public void Stabs(Instrument inst, int bar, string chord, string pattern, int center = 64, float vel = 1f, bool drive = false)
        {
            var voiced = Voice(ParseChord(chord), center);
            ForPattern(pattern, (step, len, ch) =>
            {
                float v = ch == 'X' ? vel : vel * 0.8f;
                foreach (int m in voiced) Note(inst, bar * 4 + step / 4f, len / 4f * 0.9f, m, v / Mathf.Sqrt(voiced.Count), drive);
            });
        }

        public void Pad(Instrument inst, int bar, string chord, int bars = 1, int center = 60, float vel = 1f, bool drive = false)
        {
            var voiced = Voice(ParseChord(chord), center);
            foreach (int m in voiced) Note(inst, bar * 4, bars * 4 - 0.05f, m, vel / Mathf.Sqrt(voiced.Count), drive);
        }

        /// <summary>Arpeggio across the chord over the bar. mode: up, down, updown, alberti.</summary>
        public void Arp(Instrument inst, int bar, string chord, int stepsPerBeat, string mode, int center, int octaves = 2, float vel = 1f, bool drive = false, float gate = 0.8f)
        {
            var voiced = Voice(ParseChord(chord), center);
            var seq = new List<int>();
            for (int o = 0; o < octaves; o++) foreach (int m in voiced) seq.Add(m + 12 * o);
            if (mode == "down") seq.Reverse();
            else if (mode == "updown")
            {
                var down = new List<int>(seq);
                down.Reverse();
                for (int i = 1; i < down.Count - 1; i++) seq.Add(down[i]);
            }
            else if (mode == "alberti" && voiced.Count >= 3)
                seq = new List<int> { voiced[0], voiced[2], voiced[1], voiced[2] };

            int steps = 4 * stepsPerBeat;
            float stepBeats = 1f / stepsPerBeat;
            for (int i = 0; i < steps; i++)
            {
                float accent = i % stepsPerBeat == 0 ? 1f : 0.78f;
                Note(inst, bar * 4 + i * stepBeats, stepBeats * gate, seq[i % seq.Count], vel * accent, drive);
            }
        }

        /// <summary>16-step drum pattern: x hit, X accent, o ghost, . rest.</summary>
        public void Drums(DrumKind kind, int bar, string pattern, float vel = 1f, bool drive = false)
        {
            for (int i = 0; i < pattern.Length && i < 16; i++)
            {
                char ch = pattern[i];
                if (ch == '.' || ch == '-') continue;
                float v = ch == 'X' ? vel : ch == 'o' ? vel * 0.45f : vel * 0.8f;
                Hit(kind, bar * 4 + i / 4f, v, drive);
            }
        }

        static void ForPattern(string pattern, Action<int, int, char> onNote)
        {
            for (int i = 0; i < pattern.Length && i < 16; i++)
            {
                char ch = pattern[i];
                if (ch == '.' || ch == '-') continue;
                int len = 1;
                while (i + len < pattern.Length && pattern[i + len] == '-') len++;
                onNote(i, len, ch);
            }
        }

        // ------------------------------------------------------------------ rendering

        public sealed class Stems
        {
            public float[] MainL, MainR, DriveL, DriveR;
            public int Length;
        }

        const float Tail = 4f;

        public Stems Render()
        {
            int loop = Mathf.RoundToInt(LengthSeconds * Dsp.Rate);
            int total = loop + Mathf.RoundToInt(Tail * Dsp.Rate);

            // Sidechain envelope from every kick (main + drive)
            var duck = new float[total];
            foreach (var d in _drums)
            {
                if (d.Kind != DrumKind.Kick) continue;
                int start = BeatToSample(d.Beat);
                int n = Mathf.RoundToInt(0.3f * Dsp.Rate);
                for (int i = 0; i < n && start + i < total; i++)
                {
                    float t = i / (float)Dsp.Rate;
                    float e = (t < 0.004f ? t / 0.004f : Mathf.Exp(-(t - 0.004f) / 0.09f)) * d.Vel;
                    if (e > duck[start + i]) duck[start + i] = e;
                }
            }

            var stems = new Stems { Length = loop };
            RenderStem(false, total, duck, out stems.MainL, out stems.MainR);
            RenderStem(true, total, duck, out stems.DriveL, out stems.DriveR);
            // The drive layer is sparse (hats, octave arps, crashes); lift it so full intensity is clearly felt
            for (int i = 0; i < total; i++)
            {
                stems.DriveL[i] *= DriveGain;
                stems.DriveR[i] *= DriveGain;
            }

            // Normalise both stems with one gain so the drive layer sits right on top of the main mix
            float peak = 0.0001f;
            for (int i = 0; i < total; i++)
            {
                peak = Mathf.Max(peak, Mathf.Abs(stems.MainL[i] + stems.DriveL[i]));
                peak = Mathf.Max(peak, Mathf.Abs(stems.MainR[i] + stems.DriveR[i]));
            }
            float gain = 0.9f / peak;
            Finish(stems.MainL, gain, total, loop);
            Finish(stems.MainR, gain, total, loop);
            Finish(stems.DriveL, gain, total, loop);
            Finish(stems.DriveR, gain, total, loop);
            return stems;
        }

        static void Finish(float[] buf, float gain, int total, int loop)
        {
            // Wrap the tail (reverb, delay, releases past the loop end) back onto the start for a seamless loop
            for (int i = loop; i < total; i++) buf[i - loop] += buf[i];
            for (int i = 0; i < loop; i++) buf[i] = Dsp.SoftClip(buf[i] * gain * 1.1f) / 1.1f * 1.02f;
        }

        int BeatToSample(float beat) => Mathf.RoundToInt(beat * SecPerBeat * Dsp.Rate);

        void RenderStem(bool drive, int total, float[] duck, out float[] outL, out float[] outR)
        {
            var dryL = new float[total];
            var dryR = new float[total];
            var rev = new float[total];
            var dly = new float[total];

            uint seed = 1;
            foreach (var n in _notes)
            {
                if (n.Drive != drive) continue;
                var inst = n.Inst;
                int start = BeatToSample(n.Beat);
                float panL = Mathf.Cos((inst.Pan + 1f) * Mathf.PI * 0.25f) * 1.414f;
                float panR = Mathf.Sin((inst.Pan + 1f) * Mathf.PI * 0.25f) * 1.414f;
                float gain = inst.Gain;
                Synth.RenderNote(inst, n.Midi, n.From, n.Beats * SecPerBeat, n.Vel, (i, s) =>
                {
                    int idx = start + i;
                    if (idx < 0 || idx >= total) return;
                    float v = s * gain;
                    if (inst.Duck > 0f) v *= 1f - inst.Duck * duck[idx];
                    dryL[idx] += v * panL;
                    dryR[idx] += v * panR;
                    rev[idx] += v * inst.Reverb;
                    dly[idx] += v * inst.Delay;
                }, seed++ * 2654435761u);
            }

            var cache = new Dictionary<DrumKind, float[]>();
            foreach (var d in _drums)
            {
                if (d.Drive != drive) continue;
                if (!cache.TryGetValue(d.Kind, out var buf))
                {
                    buf = Synth.RenderDrum(d.Kind, Kit, (uint)(d.Kind + 11) * 7919u);
                    cache[d.Kind] = buf;
                }
                float pan = d.Kind switch
                {
                    DrumKind.HatClosed => 0.25f, DrumKind.HatOpen => 0.3f, DrumKind.Shaker => -0.35f,
                    DrumKind.Tek => 0.2f, DrumKind.Rim => -0.2f, DrumKind.Crash => -0.15f, DrumKind.Bubble => 0.4f,
                    _ => 0f
                };
                float send = d.Kind switch
                {
                    DrumKind.Snare => Kit.SnareReverb, DrumKind.Clap => 0.3f, DrumKind.Crash => 0.2f,
                    DrumKind.Kick => 0.02f, DrumKind.Bubble => 0.35f, _ => 0.08f
                };
                float gL = Mathf.Cos((pan + 1f) * Mathf.PI * 0.25f) * 1.414f, gR = Mathf.Sin((pan + 1f) * Mathf.PI * 0.25f) * 1.414f;
                int start = BeatToSample(d.Beat);
                float g = 0.55f * d.Vel;
                for (int i = 0; i < buf.Length && start + i < total; i++)
                {
                    float v = buf[i] * g;
                    dryL[start + i] += v * gL;
                    dryR[start + i] += v * gR;
                    rev[start + i] += v * send;
                }
            }

            var reverb = new Dsp.Reverb(ReverbRoom, ReverbDamp);
            var delay = new Dsp.PingPong(DelayBeats * SecPerBeat, DelayFeedback);
            for (int i = 0; i < total; i++)
            {
                delay.Process(dly[i], dly[i], out float dl, out float dr);
                reverb.Process(rev[i] + (dl + dr) * 0.15f, rev[i] + (dl + dr) * 0.15f, out float rl, out float rr);
                dryL[i] += dl + rl * ReverbMix * 3f;
                dryR[i] += dr + rr * ReverbMix * 3f;
            }
            outL = dryL;
            outR = dryR;
        }
    }
}
