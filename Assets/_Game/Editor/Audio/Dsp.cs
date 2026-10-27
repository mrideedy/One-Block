using System;
using System.IO;
using UnityEngine;

namespace OneBlock.EditorTools.Audio
{
    /// <summary>Small offline DSP toolkit used to synthesize the game's music and sound effects.</summary>
    public static class Dsp
    {
        public const int Rate = 44100;
        public const float TwoPi = Mathf.PI * 2f;

        public static float MidiToHz(float midi) => 440f * Mathf.Pow(2f, (midi - 69f) / 12f);

        // ------------------------------------------------------------------ oscillators

        /// <summary>PolyBLEP residual for band-limited discontinuities.</summary>
        public static float PolyBlep(float t, float dt)
        {
            if (t < dt)
            {
                t /= dt;
                return t + t - t * t - 1f;
            }
            if (t > 1f - dt)
            {
                t = (t - 1f) / dt;
                return t * t + t + t + 1f;
            }
            return 0f;
        }

        public static float Saw(float phase, float dt) => 2f * phase - 1f - PolyBlep(phase, dt);

        public static float Pulse(float phase, float dt, float width)
        {
            float v = phase < width ? 1f : -1f;
            v += PolyBlep(phase, dt);
            v -= PolyBlep(Frac(phase + 1f - width), dt);
            return v;
        }

        public static float Triangle(float phase) => 1f - 4f * Mathf.Abs(phase - 0.5f);

        public static float Frac(float x) => x - Mathf.Floor(x);

        // ------------------------------------------------------------------ envelopes

        /// <summary>Linear attack, exponential-ish decay to sustain, release after the note length.</summary>
        public static float Adsr(float t, float length, float a, float d, float s, float r)
        {
            float level;
            if (t < a) level = a > 0f ? t / a : 1f;
            else if (t < a + d) level = Mathf.Lerp(1f, s, Curve((t - a) / d));
            else level = s;

            if (t > length)
            {
                float held = LevelAt(length, a, d, s);
                float k = r > 0f ? (t - length) / r : 1f;
                level = k >= 1f ? 0f : held * (1f - Curve(k));
            }
            return Mathf.Max(0f, level);
        }

        static float LevelAt(float t, float a, float d, float s)
        {
            if (t < a) return a > 0f ? t / a : 1f;
            if (t < a + d) return Mathf.Lerp(1f, s, Curve((t - a) / d));
            return s;
        }

        static float Curve(float k) => 1f - (1f - k) * (1f - k);

        public static float ExpDecay(float t, float time) => Mathf.Exp(-t / Mathf.Max(0.0001f, time));

        // ------------------------------------------------------------------ filters

        /// <summary>Topology-preserving state-variable filter (Simper/Zavalishin).</summary>
        public struct Svf
        {
            float _ic1, _ic2;

            public void Process(float input, float cutoff, float resonance, out float low, out float band, out float high)
            {
                cutoff = Mathf.Clamp(cutoff, 20f, Rate * 0.45f);
                float g = Mathf.Tan(Mathf.PI * cutoff / Rate);
                float k = 2f - 2f * Mathf.Clamp01(resonance) * 0.97f;
                float a1 = 1f / (1f + g * (g + k));
                float a2 = g * a1;
                float a3 = g * a2;
                float v3 = input - _ic2;
                float v1 = a1 * _ic1 + a2 * v3;
                float v2 = _ic2 + a2 * _ic1 + a3 * v3;
                _ic1 = 2f * v1 - _ic1;
                _ic2 = 2f * v2 - _ic2;
                low = v2;
                band = v1;
                high = input - k * v1 - v2;
            }

            public float Low(float input, float cutoff, float res)
            {
                Process(input, cutoff, res, out float l, out _, out _);
                return l;
            }

            public float High(float input, float cutoff, float res)
            {
                Process(input, cutoff, res, out _, out _, out float h);
                return h;
            }

            public float Band(float input, float cutoff, float res)
            {
                Process(input, cutoff, res, out _, out float b, out _);
                return b;
            }
        }

        public struct OnePole
        {
            float _z;
            public float Low(float x, float cutoff)
            {
                float a = Mathf.Exp(-TwoPi * cutoff / Rate);
                _z = x * (1f - a) + _z * a;
                return _z;
            }
            public float High(float x, float cutoff) => x - Low(x, cutoff);
        }

        public static float SoftClip(float x) => (float)Math.Tanh(x);

        // ------------------------------------------------------------------ noise

        public sealed class Noise
        {
            uint _s;
            public Noise(uint seed) => _s = seed == 0 ? 0x9E3779B9u : seed;
            public float Next()
            {
                _s ^= _s << 13;
                _s ^= _s >> 17;
                _s ^= _s << 5;
                return (_s / (float)uint.MaxValue) * 2f - 1f;
            }
        }

        // ------------------------------------------------------------------ effects

        /// <summary>Freeverb-style stereo reverb.</summary>
        public sealed class Reverb
        {
            static readonly int[] CombTuning = { 1116, 1188, 1277, 1356, 1422, 1491, 1557, 1617 };
            static readonly int[] AllpassTuning = { 556, 441, 341, 225 };
            const int StereoSpread = 23;

            readonly float[][] _combL = new float[8][], _combR = new float[8][];
            readonly int[] _combIdxL = new int[8], _combIdxR = new int[8];
            readonly float[] _combStoreL = new float[8], _combStoreR = new float[8];
            readonly float[][] _apL = new float[4][], _apR = new float[4][];
            readonly int[] _apIdxL = new int[4], _apIdxR = new int[4];
            readonly float _feedback, _damp;

            public Reverb(float roomSize = 0.8f, float damp = 0.4f)
            {
                _feedback = 0.7f + roomSize * 0.28f;
                _damp = damp;
                for (int i = 0; i < 8; i++)
                {
                    _combL[i] = new float[CombTuning[i]];
                    _combR[i] = new float[CombTuning[i] + StereoSpread];
                }
                for (int i = 0; i < 4; i++)
                {
                    _apL[i] = new float[AllpassTuning[i]];
                    _apR[i] = new float[AllpassTuning[i] + StereoSpread];
                }
            }

            public void Process(float inL, float inR, out float outL, out float outR)
            {
                float input = (inL + inR) * 0.015f;
                outL = outR = 0f;
                for (int i = 0; i < 8; i++)
                {
                    outL += Comb(_combL[i], ref _combIdxL[i], ref _combStoreL[i], input);
                    outR += Comb(_combR[i], ref _combIdxR[i], ref _combStoreR[i], input);
                }
                for (int i = 0; i < 4; i++)
                {
                    outL = Allpass(_apL[i], ref _apIdxL[i], outL);
                    outR = Allpass(_apR[i], ref _apIdxR[i], outR);
                }
            }

            float Comb(float[] buf, ref int idx, ref float store, float input)
            {
                float output = buf[idx];
                store = output * (1f - _damp) + store * _damp;
                buf[idx] = input + store * _feedback;
                if (++idx >= buf.Length) idx = 0;
                return output;
            }

            static float Allpass(float[] buf, ref int idx, float input)
            {
                float bufout = buf[idx];
                float output = -input + bufout;
                buf[idx] = input + bufout * 0.5f;
                if (++idx >= buf.Length) idx = 0;
                return output;
            }
        }

        /// <summary>Stereo ping-pong delay with a darkening feedback path.</summary>
        public sealed class PingPong
        {
            readonly float[] _l, _r;
            int _idx;
            readonly float _feedback;
            OnePole _lpL, _lpR;

            public PingPong(float seconds, float feedback)
            {
                int n = Mathf.Max(1, Mathf.RoundToInt(seconds * Rate));
                _l = new float[n];
                _r = new float[n];
                _feedback = feedback;
            }

            public void Process(float inL, float inR, out float outL, out float outR)
            {
                outL = _l[_idx];
                outR = _r[_idx];
                float mono = (inL + inR) * 0.5f;
                _l[_idx] = mono + _lpR.Low(outR, 4500f) * _feedback;
                _r[_idx] = _lpL.Low(outL, 4500f) * _feedback;
                if (++_idx >= _l.Length) _idx = 0;
            }
        }

        // ------------------------------------------------------------------ output

        public static void WriteWav(string path, float[] left, float[] right, int length)
        {
            bool stereo = right != null;
            int channels = stereo ? 2 : 1;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var fs = new FileStream(path, FileMode.Create);
            using var w = new BinaryWriter(fs);
            int dataBytes = length * channels * 2;
            w.Write(new[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F' });
            w.Write(36 + dataBytes);
            w.Write(new[] { (byte)'W', (byte)'A', (byte)'V', (byte)'E', (byte)'f', (byte)'m', (byte)'t', (byte)' ' });
            w.Write(16);
            w.Write((short)1);
            w.Write((short)channels);
            w.Write(Rate);
            w.Write(Rate * channels * 2);
            w.Write((short)(channels * 2));
            w.Write((short)16);
            w.Write(new[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' });
            w.Write(dataBytes);
            for (int i = 0; i < length; i++)
            {
                w.Write((short)Mathf.Clamp(Mathf.RoundToInt(left[i] * 32767f), -32768, 32767));
                if (stereo) w.Write((short)Mathf.Clamp(Mathf.RoundToInt(right[i] * 32767f), -32768, 32767));
            }
        }
    }
}
