using System.Collections.Generic;

namespace OneBlock.EditorTools.Audio
{
    /// <summary>
    /// The game's soundtrack. Every song follows the same arrangement skeleton (A, B, A with harmony, bridge)
    /// but has its own key, tempo, melodies, instruments, grooves and adaptive "drive" layer.
    /// </summary>
    public static class Songs
    {
        public sealed class Style
        {
            public Instrument Lead, Harmony, Bass, Chords, Arp, Counter;
            public string BassA, BassB, BassBridge;
            public int BassCenter = 40;
            public bool PadChords;
            public bool PowerChords;
            public string StabA = "x.......x.......", StabB = "x.......x.......";
            public int ChordCenter = 62;
            public int ArpRate = 4, ArpCenter = 67, ArpOctaves = 2;
            public string ArpMode = "up";
            public bool ArpInA, ArpInB = true, ArpInBridge = true;

            public DrumKind SnareKind = DrumKind.Snare, HatKind = DrumKind.HatClosed;
            public string KickA = "x.......x.......", SnareA = "....x.......x...", HatA = "x.x.x.x.x.x.x.x.";
            public string KickB = "x.......x.......", SnareB = "....x.......x...", HatB = "x.x.x.x.x.x.x.x.";
            public string KickBridge = "x...............", SnareBridge = "................", HatBridge = "..x...x...x...x.";
            public DrumKind PercKind = DrumKind.Shaker;
            public string PercA, PercB, PercBridge;

            public DrumKind DriveHatKind = DrumKind.HatClosed;
            public string DriveHat = "xoxoxoxoxoxoxoxo", DriveOpen = "......x.......x.";
            public DrumKind DriveExtraKind = DrumKind.Tom;
            public string DriveExtra;
            public bool DriveArp = true;
            public float LeadVel = 1f;
        }

        public sealed class Score
        {
            public string[] ChordsA, ChordsB, ChordsBridge;   // one chord per bar (A and bridge repeat to 8 bars)
            public string[] MelodyA, MelodyB, Bridge;         // one 16-step string per bar
            public bool Short;                                // A + B only (16 bars)
        }

        public static IEnumerable<(Song song, string worldId)> All()
        {
            yield return (Menu(), "");
            yield return (Grassland(), "world_grassland");
            yield return (Desert(), "world_desert");
            yield return (Snow(), "world_snow");
            yield return (Volcano(), "world_volcano");
            yield return (Space(), "world_space");
            yield return (Candy(), "world_candy");
            yield return (Ocean(), "world_ocean");
            yield return (Neon(), "world_neon");
        }

        // ================================================================== arranger

        static string At(string[] a, int i) => a[i % a.Length];

        static void Arrange(Song s, Style st, Score sc)
        {
            int sections = sc.Short ? 2 : 4;
            s.Bars = sections * 8;
            for (int sec = 0; sec < sections; sec++)
            {
                int first = sec * 8;
                bool isB = sec == 1, isA2 = sec == 2, isBridge = sec == 3;
                string[] chords = isB ? sc.ChordsB : isBridge ? sc.ChordsBridge : sc.ChordsA;

                // Drive layer: crash into every section and a fill out of it
                s.Hit(DrumKind.Crash, first * 4, 0.7f, drive: true);
                s.Drums(DrumKind.Snare, first + 7, "........x.x.xxXX", 0.6f, drive: true);

                for (int b = 0; b < 8; b++)
                {
                    int bar = first + b;
                    string chord = At(chords, b);
                    string chordForStab = st.PowerChords ? chord.Substring(0, chord.Length > 1 && (chord[1] == '#' || chord[1] == 'b') ? 2 : 1) + "5" : chord;

                    // Lead
                    if (isBridge)
                    {
                        if (sc.Bridge != null) s.Melody(st.Lead, bar, At(sc.Bridge, b), st.LeadVel * 0.85f);
                    }
                    else
                    {
                        var mel = s.Melody(st.Lead, bar, isB ? At(sc.MelodyB, b) : At(sc.MelodyA, b), st.LeadVel);
                        if (isA2 && st.Harmony != null) s.Harmonise(st.Harmony, mel, 2, 0.7f);
                        if (isA2 && st.Counter != null)
                            foreach (var (beat, beats, midi) in mel) s.Note(st.Counter, beat, beats, midi + 12, 0.55f, drive: true);
                        if (isB && st.Counter != null && b % 2 == 1)
                            foreach (var (beat, beats, midi) in mel) s.Note(st.Counter, beat, beats, midi + 12, 0.35f, drive: true);
                    }

                    // Bass
                    string bassPat = isBridge ? st.BassBridge : isB || isA2 ? st.BassB : st.BassA;
                    if (!string.IsNullOrEmpty(bassPat)) s.Bass(st.Bass, bar, chord, bassPat, st.BassCenter);

                    // Chords
                    if (st.Chords != null)
                    {
                        if (st.PadChords) s.Pad(st.Chords, bar, chordForStab, 1, st.ChordCenter);
                        else s.Stabs(st.Chords, bar, chordForStab, isB || isA2 ? st.StabB : st.StabA, st.ChordCenter);
                    }

                    // Arp (+ an octave-up copy in the drive layer)
                    bool arp = st.Arp != null && (isBridge ? st.ArpInBridge : isB || isA2 ? st.ArpInB || isA2 : st.ArpInA);
                    if (arp) s.Arp(st.Arp, bar, chord, st.ArpRate, st.ArpMode, st.ArpCenter, st.ArpOctaves);
                    if (st.Arp != null && st.DriveArp && (isB || isA2 || isBridge))
                        s.Arp(st.Arp, bar, chord, st.ArpRate, st.ArpMode, st.ArpCenter + 12, 1, 0.55f, drive: true);

                    // Drums
                    string kick = isBridge ? st.KickBridge : isB || isA2 ? st.KickB : st.KickA;
                    string snare = isBridge ? st.SnareBridge : isB || isA2 ? st.SnareB : st.SnareA;
                    string hat = isBridge ? st.HatBridge : isB || isA2 ? st.HatB : st.HatA;
                    string perc = isBridge ? st.PercBridge : isB || isA2 ? st.PercB : st.PercA;
                    s.Drums(DrumKind.Kick, bar, kick);
                    s.Drums(st.SnareKind, bar, snare);
                    s.Drums(st.HatKind, bar, hat, 0.8f);
                    if (!string.IsNullOrEmpty(perc)) s.Drums(st.PercKind, bar, perc, 0.7f);

                    // Drive rhythm
                    if (!isBridge || b >= 4)
                    {
                        s.Drums(st.DriveHatKind, bar, st.DriveHat, 0.55f, drive: true);
                        s.Drums(DrumKind.HatOpen, bar, st.DriveOpen, 0.5f, drive: true);
                        if (!string.IsNullOrEmpty(st.DriveExtra)) s.Drums(st.DriveExtraKind, bar, st.DriveExtra, 0.6f, drive: true);
                    }
                }
            }
        }

        // ================================================================== songs

        /// <summary>Main menu: bouncy, friendly, instantly recognisable.</summary>
        static Song Menu()
        {
            var s = new Song { Id = "theme", Title = "One Block Theme", Bpm = 112, KeyRoot = 60, Scale = new[] { 0, 2, 4, 5, 7, 9, 11 }, Swing = 0.12f };
            var st = new Style
            {
                Lead = new Instrument { Wave = Wave.Fm, FmRatio = 2f, FmIndex = 1.6f, FmDecay = 0.15f, A = 0.002f, D = 0.35f, S = 0.3f, R = 0.2f, Gain = 0.3f, Reverb = 0.2f, Delay = 0.12f },
                Harmony = new Instrument { Wave = Wave.Pluck, PluckDecay = 0.5f, PluckBright = 0.75f, A = 0.001f, D = 1f, S = 1f, R = 0.1f, Gain = 0.16f, Pan = -0.3f, Reverb = 0.2f },
                Bass = new Instrument { Wave = Wave.Triangle, A = 0.003f, D = 0.1f, S = 0.8f, R = 0.04f, Gain = 0.42f, Duck = 0.15f },
                Chords = new Instrument { Wave = Wave.SuperSaw, Voices = 3, Detune = 0.08f, Filter = true, Cutoff = 1700f, A = 0.25f, D = 0.5f, S = 0.75f, R = 0.4f, Gain = 0.1f, Reverb = 0.35f, Duck = 0.35f },
                Arp = new Instrument { Wave = Wave.Pluck, PluckDecay = 0.35f, PluckBright = 0.8f, A = 0.001f, D = 1f, S = 1f, R = 0.05f, Gain = 0.13f, Pan = 0.35f, Delay = 0.2f },
                Counter = new Instrument { Wave = Wave.Fm, FmRatio = 3.5f, FmIndex = 1.2f, FmDecay = 0.2f, A = 0.002f, D = 0.3f, S = 0.2f, R = 0.2f, Gain = 0.1f, Pan = 0.3f },
                PadChords = true, ChordCenter = 62,
                BassA = "r...o...r...o...", BassB = "r...o.r.r...o.r.",
                ArpRate = 2, ArpMode = "up", ArpCenter = 72, ArpInA = true,
                SnareKind = DrumKind.Clap,
                KickA = "x.......x.......", SnareA = "....x.......x...", HatA = "..x...x...x...x.",
                KickB = "x.....x.x.......", SnareB = "....x.......x...", HatB = "x.x.x.x.x.x.x.x.",
                PercKind = DrumKind.Shaker, PercB = "..x...x...x..xx.",
                DriveHat = "xoxoxoxoxoxoxoxo", DriveOpen = "......x.......x.",
            };
            var sc = new Score
            {
                Short = true,
                ChordsA = new[] { "C", "Am", "F", "G" },
                ChordsB = new[] { "F", "G", "Em", "Am", "F", "G", "C", "C" },
                MelodyA = new[]
                {
                    "E5:2 G5:2 C6:3 B5:1 A5:2 G5:2 E5:4",
                    "A5:4 G5:2 E5:2 C5:4 E5:4",
                    "F5:2 A5:2 C6:4 A5:2 F5:2 G5:4",
                    "D5:3 E5:1 D5:2 B4:2 G4:6 _:2",
                    "E5:2 G5:2 C6:3 B5:1 A5:2 G5:2 E5:4",
                    "A5:2 C6:2 E6:4 D6:2 C6:2 A5:4",
                    "F5:2 E5:2 F5:2 A5:2 G5:4 F5:2 D5:2",
                    "D5:2 E5:2 F5:2 D5:2 C5:8",
                },
                MelodyB = new[]
                {
                    "C6:6 A5:2 F5:4 A5:4",
                    "B5:6 G5:2 D5:4 G5:4",
                    "G5:4 B5:4 E6:6 D6:2",
                    "C6:4 B5:2 A5:2 E5:8",
                    "A5:2 C6:2 F6:4 E6:2 D6:2 C6:4",
                    "B5:2 D6:2 G6:4 F6:2 D6:2 B5:4",
                    "C6:4 G5:2 E5:2 G5:2 C6:6",
                    "_:8 G5:2 A5:2 B5:4",
                },
            };
            Arrange(s, st, sc);
            return s;
        }

        /// <summary>Grassland: sunny chiptune pop with a ukulele strum.</summary>
        static Song Grassland()
        {
            var s = new Song { Id = "grassland", Title = "Green Run", Bpm = 126, KeyRoot = 67, Scale = new[] { 0, 2, 4, 5, 7, 9, 11 }, Swing = 0.1f };
            var st = new Style
            {
                Lead = new Instrument { Wave = Wave.Pulse, PulseWidth = 0.5f, PwmDepth = 0.15f, PwmRate = 0.35f, A = 0.004f, D = 0.15f, S = 0.6f, R = 0.06f, VibDepth = 0.12f, VibDelay = 0.2f, Gain = 0.2f, Reverb = 0.15f, Delay = 0.12f },
                Harmony = new Instrument { Wave = Wave.Pulse, PulseWidth = 0.25f, A = 0.004f, D = 0.15f, S = 0.5f, R = 0.05f, Gain = 0.1f, Pan = -0.35f },
                Bass = new Instrument { Wave = Wave.Triangle, A = 0.002f, D = 0.1f, S = 0.85f, R = 0.03f, Gain = 0.45f, Duck = 0.2f },
                Chords = new Instrument { Wave = Wave.Pluck, PluckDecay = 0.55f, PluckBright = 0.7f, A = 0.001f, D = 1f, S = 1f, R = 0.08f, Gain = 0.18f, Pan = 0.25f, Reverb = 0.15f },
                Arp = new Instrument { Wave = Wave.Square, Filter = true, Cutoff = 3000f, A = 0.002f, D = 0.08f, S = 0.3f, R = 0.05f, Gain = 0.07f, Pan = -0.3f, Delay = 0.25f },
                Counter = new Instrument { Wave = Wave.Fm, FmRatio = 3.5f, FmIndex = 2f, FmDecay = 0.25f, A = 0.002f, D = 0.3f, S = 0.2f, R = 0.15f, Gain = 0.11f, Pan = 0.35f },
                ChordCenter = 62, StabA = "x..x..x.x..x..x.", StabB = "x.xx.xx.x.xx.xx.",
                BassA = "r...r.o.r...r.o.", BassB = "r.r.o.r.r.r.o.r.", BassBridge = "r-------o-------",
                ArpRate = 4, ArpMode = "updown", ArpCenter = 67,
                KickA = "x.......x.......", SnareA = "....x.......x...", HatA = "x.x.x.x.x.x.x.x.",
                KickB = "x.....x.x.......", SnareB = "....x.......x..o", HatB = "x.x.x.x.x.x.x.x.",
                KickBridge = "x.......x.......", SnareBridge = "............x...", HatBridge = "..x...x...x...x.",
                PercKind = DrumKind.Shaker, PercB = "..x...x...x...x.",
            };
            var sc = new Score
            {
                ChordsA = new[] { "G", "D", "Em", "C" },
                ChordsB = new[] { "C", "D", "Bm", "Em", "C", "D", "G", "G" },
                ChordsBridge = new[] { "Em", "C", "G", "D" },
                MelodyA = new[]
                {
                    "B4:2 D5:2 G5:4 F#5:2 G5:2 A5:4",
                    "F#5:4 E5:2 D5:2 A4:8",
                    "G5:2 F#5:2 E5:2 B4:2 E5:4 G5:4",
                    "E5:6 D5:2 C5:2 B4:2 A4:4",
                    "B4:2 D5:2 G5:4 A5:2 B5:2 D6:4",
                    "C6:2 B5:2 A5:4 F#5:4 D5:4",
                    "E5:2 G5:2 B5:4 A5:2 G5:2 E5:4",
                    "C5:2 E5:2 D5:4 G4:8",
                },
                MelodyB = new[]
                {
                    "E5:4 G5:4 C6:6 B5:2",
                    "A5:4 F#5:4 D5:8",
                    "D5:2 F#5:2 B5:4 A5:2 F#5:2 D5:4",
                    "E5:6 F#5:2 G5:8",
                    "G5:2 A5:2 C6:4 B5:2 A5:2 G5:4",
                    "F#5:2 G5:2 A5:4 D6:8",
                    "B5:4 A5:2 G5:2 D5:4 B4:4",
                    "G5:12 _:4",
                },
                Bridge = new[] { "B5:16", "C6:8 E6:8", "D6:16", "A5:16" },
            };
            Arrange(s, st, sc);
            return s;
        }

        /// <summary>Desert: snake-charmer reed over oud strums and hand drums (D Phrygian dominant).</summary>
        static Song Desert()
        {
            var s = new Song { Id = "desert", Title = "Sand Serpent", Bpm = 116, KeyRoot = 62, Scale = new[] { 0, 1, 4, 5, 7, 8, 10 }, Swing = 0.06f };
            s.Kit.KickPitch = 48f; s.Kit.KickDrive = 0.15f;
            var st = new Style
            {
                Lead = new Instrument { Wave = Wave.Reed, PulseWidth = 0.22f, A = 0.02f, D = 0.1f, S = 0.85f, R = 0.1f, VibDepth = 0.25f, VibRate = 6f, VibDelay = 0.12f, Gain = 0.26f, Reverb = 0.22f, Delay = 0.15f },
                Harmony = new Instrument { Wave = Wave.Pluck, PluckDecay = 0.5f, PluckBright = 0.5f, A = 0.001f, D = 1f, S = 1f, R = 0.08f, Gain = 0.2f, Pan = -0.25f },
                Bass = new Instrument { Wave = Wave.Triangle, A = 0.003f, D = 0.15f, S = 0.7f, R = 0.05f, Gain = 0.42f, Duck = 0.1f },
                Chords = new Instrument { Wave = Wave.Pluck, PluckDecay = 0.45f, PluckBright = 0.45f, A = 0.001f, D = 1f, S = 1f, R = 0.08f, Gain = 0.15f, Pan = 0.3f, Reverb = 0.2f },
                Arp = new Instrument { Wave = Wave.Pluck, PluckDecay = 0.3f, PluckBright = 0.65f, A = 0.001f, D = 1f, S = 1f, R = 0.05f, Gain = 0.1f, Pan = -0.3f, Delay = 0.2f },
                Counter = new Instrument { Wave = Wave.Fm, FmRatio = 3f, FmIndex = 2f, FmDecay = 0.2f, A = 0.002f, D = 0.25f, S = 0.2f, R = 0.15f, Gain = 0.1f, Pan = 0.35f },
                ChordCenter = 57, StabA = "x..x..x.x..x.x..", StabB = "x..x..x.x..x.x.x",
                BassA = "r..r..r.r..r.r..", BassB = "r..r..r.r..r.rf.", BassBridge = "r.......r.......",
                ArpRate = 4, ArpMode = "updown", ArpCenter = 62, ArpInBridge = true,
                SnareKind = DrumKind.Tek, HatKind = DrumKind.Shaker,
                KickA = "................", SnareA = "...x.x.....x.x.x", HatA = "x.x.x.x.x.x.x.x.",
                KickB = "x.......x.......", SnareB = "..xx.x.x..xx.x.x", HatB = "x.x.x.x.x.x.x.x.",
                KickBridge = "................", SnareBridge = "...x.......x....", HatBridge = "x...x...x...x...",
                PercKind = DrumKind.Doum, PercA = "X.....x.X.......", PercB = "X..x..x.X..x....", PercBridge = "X.......X.......",
                DriveHatKind = DrumKind.Tek, DriveHat = "oxoxoxoxoxoxoxox", DriveOpen = "....x.......x...",
                DriveExtraKind = DrumKind.Snap, DriveExtra = "....x.......x...",
            };
            var sc = new Score
            {
                ChordsA = new[] { "D", "Eb", "D", "Cm" },
                ChordsB = new[] { "Gm", "Cm", "Eb", "D", "Gm", "Cm", "Eb", "D" },
                ChordsBridge = new[] { "D", "Eb" },
                MelodyA = new[]
                {
                    "D5:4 Eb5:2 D5:2 C5:2 Bb4:2 A4:4",
                    "Bb4:2 C5:2 Bb4:2 A4:2 G4:4 F#4:4",
                    "G4:2 A4:2 Bb4:2 A4:2 F#4:2 G4:2 A4:4",
                    "G4:4 F#4:2 Eb4:2 D4:8",
                    "A4:2 D5:2 F#5:4 G5:2 F#5:2 Eb5:4",
                    "D5:2 Eb5:2 G5:4 F#5:2 Eb5:2 D5:4",
                    "C5:2 D5:2 Eb5:2 D5:2 C5:2 Bb4:2 A4:4",
                    "Bb4:2 A4:2 G4:2 F#4:2 D4:8",
                },
                MelodyB = new[]
                {
                    "G5:6 F#5:2 G5:4 A5:4",
                    "Bb5:4 A5:2 G5:2 Eb5:8",
                    "G5:2 Bb5:2 A5:2 G5:2 F#5:4 Eb5:4",
                    "D5:12 _:4",
                    "D6:4 C6:2 Bb5:2 A5:4 G5:4",
                    "C6:4 Bb5:2 A5:2 G5:4 Eb5:4",
                    "F#5:2 G5:2 A5:4 Bb5:2 A5:2 G5:4",
                    "F#5:4 Eb5:2 F#5:2 D5:8",
                },
                Bridge = new[] { "A5:8 Bb5:8", "A5:4 G5:4 F#5:8", "D5:1 Eb5:1 D5:1 Eb5:1 D5:4 _:8", "F#5:8 D5:8" },
            };
            Arrange(s, st, sc);
            return s;
        }

        /// <summary>Snow: dreamy celesta and music box over a slow, breathing groove.</summary>
        static Song Snow()
        {
            var s = new Song { Id = "snow", Title = "Frostbite Glide", Bpm = 96, KeyRoot = 69, Scale = new[] { 0, 2, 3, 5, 7, 8, 10 }, ReverbRoom = 0.86f, ReverbDamp = 0.3f };
            s.Kit.KickPitch = 48f; s.Kit.KickDecay = 0.35f; s.Kit.KickDrive = 0.08f;
            s.Kit.SnareTone = 220f; s.Kit.SnareNoise = 1f; s.Kit.SnareDecay = 0.22f; s.Kit.SnareReverb = 0.4f; s.Kit.HatDecay = 0.03f;
            var st = new Style
            {
                Lead = new Instrument { Wave = Wave.Fm, FmRatio = 4f, FmIndex = 1.2f, FmDecay = 0.5f, A = 0.002f, D = 0.6f, S = 0.25f, R = 0.6f, Gain = 0.26f, Reverb = 0.35f, Delay = 0.2f },
                Harmony = new Instrument { Wave = Wave.Fm, FmRatio = 2f, FmIndex = 0.8f, FmDecay = 0.5f, A = 0.004f, D = 0.5f, S = 0.3f, R = 0.5f, Gain = 0.12f, Pan = -0.3f, Reverb = 0.35f },
                Bass = new Instrument { Wave = Wave.Sine, A = 0.01f, D = 0.3f, S = 0.75f, R = 0.2f, Gain = 0.4f, Duck = 0.1f },
                Chords = new Instrument { Wave = Wave.SuperSaw, Voices = 5, Detune = 0.12f, Filter = true, Cutoff = 1400f, A = 0.6f, D = 0.5f, S = 0.8f, R = 0.8f, Gain = 0.09f, Reverb = 0.45f, Duck = 0.15f },
                Arp = new Instrument { Wave = Wave.Pluck, PluckDecay = 0.8f, PluckBright = 0.92f, A = 0.001f, D = 1f, S = 1f, R = 0.1f, Gain = 0.12f, Pan = 0.3f, Delay = 0.25f, Reverb = 0.3f },
                Counter = new Instrument { Wave = Wave.Sine, VibDepth = 0.15f, VibRate = 5f, A = 0.05f, D = 0.3f, S = 0.7f, R = 0.3f, Gain = 0.09f, Pan = -0.2f, Reverb = 0.4f },
                PadChords = true, ChordCenter = 60,
                BassA = "r-------r---f---", BassB = "r-------r---f-o-", BassBridge = "r---------------",
                ArpRate = 2, ArpMode = "up", ArpCenter = 69, ArpInA = true,
                KickA = "x.........x.....", SnareA = "........x.......", HatA = "..x...x...x...x.",
                KickB = "x.....x...x.....", SnareB = "........x......o", HatB = "x.x.x.x.x.x.x.x.",
                KickBridge = "................", SnareBridge = "................", HatBridge = "................",
                PercKind = DrumKind.Shaker, PercA = "x.x.x.x.x.x.x.x.", PercB = "x.x.x.x.x.x.x.x.", PercBridge = "x...x...x...x...",
                DriveHatKind = DrumKind.Shaker, DriveHat = "xoxoxoxoxoxoxoxo", DriveOpen = "........x.......",
            };
            var sc = new Score
            {
                ChordsA = new[] { "Am", "F", "C", "G" },
                ChordsB = new[] { "F", "G", "Em", "Am", "Dm", "Am", "F", "E" },
                ChordsBridge = new[] { "Am", "F", "C", "G" },
                MelodyA = new[]
                {
                    "E5:4 A5:4 C6:4 B5:4",
                    "A5:8 G5:4 F5:4",
                    "E5:4 G5:4 C6:6 D6:2",
                    "B5:12 _:4",
                    "E6:4 D6:2 C6:2 B5:4 A5:4",
                    "C6:6 A5:2 F5:8",
                    "G5:4 C6:4 E6:4 D6:4",
                    "D6:8 B5:8",
                },
                MelodyB = new[]
                {
                    "A5:6 C6:2 F6:8",
                    "D6:6 B5:2 G5:8",
                    "E6:4 D6:4 B5:8",
                    "C6:4 B5:2 A5:2 E5:8",
                    "F5:4 A5:4 D6:6 C6:2",
                    "C6:8 A5:8",
                    "A5:4 C6:4 F6:4 E6:4",
                    "G#5:8 B5:8",
                },
                Bridge = new[] { "A5:16", "C6:16", "G5:16", "B5:16" },
            };
            Arrange(s, st, sc);
            return s;
        }

        /// <summary>Volcano: fast, distorted and heroic.</summary>
        static Song Volcano()
        {
            var s = new Song { Id = "volcano", Title = "Magma Rush", Bpm = 142, KeyRoot = 64, Scale = new[] { 0, 2, 3, 5, 7, 8, 10 }, DelayBeats = 0.5f, DelayFeedback = 0.25f };
            s.Kit.KickPitch = 50f; s.Kit.KickPunch = 220f; s.Kit.KickDecay = 0.25f; s.Kit.KickDrive = 0.7f;
            s.Kit.SnareTone = 200f; s.Kit.SnareDecay = 0.16f; s.Kit.SnareNoise = 1f;
            var st = new Style
            {
                Lead = new Instrument { Wave = Wave.Saw, Filter = true, Cutoff = 3200f, EnvCutoff = 3000f, FilterDecay = 0.15f, A = 0.003f, D = 0.2f, S = 0.7f, R = 0.05f, Drive = 0.25f, VibDepth = 0.1f, Gain = 0.19f, Reverb = 0.15f, Delay = 0.12f },
                Harmony = new Instrument { Wave = Wave.Square, Filter = true, Cutoff = 2500f, A = 0.003f, D = 0.2f, S = 0.6f, R = 0.05f, Gain = 0.09f, Pan = -0.35f },
                Bass = new Instrument { Wave = Wave.Saw, Filter = true, Cutoff = 450f, EnvCutoff = 1500f, FilterDecay = 0.08f, A = 0.002f, D = 0.1f, S = 0.8f, R = 0.03f, Drive = 0.6f, Gain = 0.32f, Duck = 0.3f },
                Chords = new Instrument { Wave = Wave.Saw, Filter = true, Cutoff = 2300f, A = 0.002f, D = 0.2f, S = 0.6f, R = 0.08f, Drive = 0.7f, Gain = 0.13f, Pan = 0.3f, Duck = 0.25f },
                Arp = new Instrument { Wave = Wave.Square, Filter = true, Cutoff = 2800f, A = 0.002f, D = 0.06f, S = 0.3f, R = 0.03f, Gain = 0.06f, Pan = -0.3f },
                Counter = new Instrument { Wave = Wave.Pulse, PulseWidth = 0.15f, A = 0.003f, D = 0.1f, S = 0.6f, R = 0.05f, Gain = 0.07f, Pan = 0.35f },
                PowerChords = true, ChordCenter = 52, StabA = "X..x..x...x.x...", StabB = "X..x..x...x.x.x.",
                BassA = "r.r.r.r.r.r.r.r.", BassB = "rrr.rrr.rrr.rro.", BassBridge = "r.r.r.r.r.r.r.r.",
                BassCenter = 40,
                ArpRate = 4, ArpMode = "up", ArpCenter = 64, ArpInB = true, ArpInBridge = false,
                KickA = "x...x...x...x...", SnareA = "....X.......X...", HatA = "x.x.x.x.x.x.x.x.",
                KickB = "x...x...x...x.x.", SnareB = "....X.......X..x", HatB = "x.x.x.x.x.x.x.x.",
                KickBridge = "x.x.x.x.x.x.x.x.", SnareBridge = "................", HatBridge = "x...x...x...x...",
                PercKind = DrumKind.Tom, PercBridge = "x..x..x...x..x..",
                DriveHat = "xoxoxoxoxoxoxoxo", DriveOpen = "..x...x...x...x.",
            };
            var sc = new Score
            {
                ChordsA = new[] { "Em", "C", "D", "B" },
                ChordsB = new[] { "Am", "Em", "C", "B", "Am", "Em", "C", "D" },
                ChordsBridge = new[] { "Em", "C", "D", "B" },
                MelodyA = new[]
                {
                    "E5:2 E5:2 G5:2 E5:2 B5:4 A5:2 G5:2",
                    "E5:2 E5:2 G5:2 E5:2 C6:4 B5:2 G5:2",
                    "F#5:2 F#5:2 A5:2 F#5:2 D6:4 C6:2 A5:2",
                    "B5:6 A5:2 G5:2 F#5:2 D#5:4",
                    "E5:2 G5:2 B5:2 E6:2 D6:2 B5:2 G5:4",
                    "E6:2 D6:2 C6:2 G5:2 E5:4 G5:4",
                    "F#5:2 A5:2 D6:2 F#6:2 E6:2 D6:2 A5:4",
                    "B5:8 D#6:4 F#6:4",
                },
                MelodyB = new[]
                {
                    "A5:6 B5:2 C6:4 E6:4",
                    "B5:6 A5:2 G5:8",
                    "G5:2 A5:2 B5:2 C6:2 E6:4 D6:4",
                    "D#6:8 B5:8",
                    "C6:4 B5:2 A5:2 E6:8",
                    "G6:4 F#6:2 E6:2 B5:8",
                    "C6:2 B5:2 C6:2 E6:2 G6:8",
                    "F#6:4 E6:2 D6:2 A5:8",
                },
                Bridge = new[] { "_:16", "_:16", "_:16", "_:8 B5:2 D#6:2 F#6:4" },
            };
            Arrange(s, st, sc);
            return s;
        }

        /// <summary>Space: arpeggiated synths, bleeps and wide echoes (D Dorian).</summary>
        static Song Space()
        {
            var s = new Song { Id = "space", Title = "Orbit Hop", Bpm = 118, KeyRoot = 62, Scale = new[] { 0, 2, 3, 5, 7, 9, 10 }, DelayBeats = 0.75f, DelayFeedback = 0.45f, ReverbRoom = 0.85f };
            s.Kit.KickPitch = 45f; s.Kit.KickDecay = 0.4f; s.Kit.KickDrive = 0.2f; s.Kit.HatTone = 9000f;
            var st = new Style
            {
                Lead = new Instrument { Wave = Wave.Fm, FmRatio = 1.5f, FmIndex = 3f, FmDecay = 0.08f, A = 0.002f, D = 0.15f, S = 0.55f, R = 0.1f, Gain = 0.2f, Delay = 0.35f, Reverb = 0.25f },
                Harmony = new Instrument { Wave = Wave.Sine, A = 0.01f, D = 0.2f, S = 0.7f, R = 0.2f, Gain = 0.1f, Pan = -0.3f, Delay = 0.2f },
                Bass = new Instrument { Wave = Wave.Saw, Filter = true, Cutoff = 330f, EnvCutoff = 900f, FilterDecay = 0.1f, A = 0.003f, D = 0.15f, S = 0.75f, R = 0.05f, Gain = 0.34f, Duck = 0.4f },
                Chords = new Instrument { Wave = Wave.SuperSaw, Voices = 5, Detune = 0.14f, Filter = true, Cutoff = 1200f, A = 0.4f, D = 0.5f, S = 0.8f, R = 0.6f, Gain = 0.09f, Reverb = 0.45f, Duck = 0.45f },
                Arp = new Instrument { Wave = Wave.Saw, Filter = true, Cutoff = 1600f, EnvCutoff = 2500f, FilterDecay = 0.09f, Resonance = 0.5f, A = 0.002f, D = 0.1f, S = 0.4f, R = 0.04f, Gain = 0.1f, Pan = -0.2f, Delay = 0.3f },
                Counter = new Instrument { Wave = Wave.Pulse, PulseWidth = 0.12f, A = 0.003f, D = 0.1f, S = 0.5f, R = 0.08f, Gain = 0.07f, Pan = 0.4f, Delay = 0.3f },
                PadChords = true, ChordCenter = 60,
                BassA = "r..r..r.r..r..r.", BassB = "r..r..r.r..r.ro.", BassBridge = "r-------r-------",
                ArpRate = 4, ArpMode = "updown", ArpCenter = 62, ArpInA = true,
                SnareKind = DrumKind.Clap,
                KickA = "x.......x.......", SnareA = "....x.......x...", HatA = "..x...x...x...x.",
                KickB = "x.....x.x.......", SnareB = "....x.......x...", HatB = "x.xxx.xxx.xxx.xx",
                KickBridge = "x...............", SnareBridge = "................", HatBridge = "..x...x...x...x.",
                PercKind = DrumKind.Bubble, PercB = "..........x.....",
                DriveHat = "xoxoxoxoxoxoxoxo", DriveOpen = "..x...x...x...x.",
                DriveExtraKind = DrumKind.Bubble, DriveExtra = "......x.......x.",
            };
            var sc = new Score
            {
                ChordsA = new[] { "Dm7", "G", "Am", "C" },
                ChordsB = new[] { "F", "C", "G", "Dm", "F", "C", "G", "G" },
                ChordsBridge = new[] { "Dm", "G" },
                MelodyA = new[]
                {
                    "A5:3 F5:1 D5:2 A5:2 C6:4 A5:4",
                    "B5:3 G5:1 D5:2 B5:2 D6:4 B5:4",
                    "C6:2 B5:2 A5:2 E5:2 A5:6 G5:2",
                    "E5:4 G5:4 C6:8",
                    "D6:3 C6:1 A5:2 F5:2 A5:4 D6:4",
                    "D6:3 B5:1 G5:2 B5:2 D6:4 F6:4",
                    "E6:4 D6:2 C6:2 B5:4 A5:4",
                    "G5:2 E5:2 G5:2 C6:2 E6:8",
                },
                MelodyB = new[]
                {
                    "F6:8 E6:4 C6:4",
                    "E6:8 D6:4 G5:4",
                    "B5:4 D6:4 G6:6 F6:2",
                    "F6:4 E6:2 D6:2 A5:8",
                    "A5:2 C6:2 F6:4 E6:2 C6:2 A5:4",
                    "G5:2 C6:2 E6:4 D6:2 C6:2 G5:4",
                    "B5:2 D6:2 G6:4 F6:2 D6:2 B5:4",
                    "D6:8 _:8",
                },
                Bridge = new[] { "D6:16", "B5:16", "F6:16", "G6:16" },
            };
            Arrange(s, st, sc);
            return s;
        }

        /// <summary>Candy Land (bundle): bubblegum marimba pop.</summary>
        static Song Candy()
        {
            var s = new Song { Id = "candy", Title = "Sugar Rush", Bpm = 134, KeyRoot = 65, Scale = new[] { 0, 2, 4, 5, 7, 9, 11 }, Swing = 0.08f };
            s.Kit.KickDrive = 0.25f; s.Kit.HatDecay = 0.035f;
            var st = new Style
            {
                Lead = new Instrument { Wave = Wave.Fm, FmRatio = 4f, FmIndex = 2.5f, FmDecay = 0.08f, A = 0.001f, D = 0.25f, S = 0.18f, R = 0.15f, Gain = 0.34f, Reverb = 0.18f },
                Harmony = new Instrument { Wave = Wave.Fm, FmRatio = 3.5f, FmIndex = 1.5f, FmDecay = 0.2f, Octave = 1, A = 0.001f, D = 0.3f, S = 0.15f, R = 0.2f, Gain = 0.1f, Pan = 0.35f },
                Bass = new Instrument { Wave = Wave.Square, Filter = true, Cutoff = 900f, EnvCutoff = 1200f, FilterDecay = 0.08f, A = 0.002f, D = 0.12f, S = 0.5f, R = 0.04f, Gain = 0.25f, Duck = 0.2f },
                Chords = new Instrument { Wave = Wave.Pluck, PluckDecay = 0.35f, PluckBright = 0.8f, A = 0.001f, D = 1f, S = 1f, R = 0.06f, Gain = 0.14f, Pan = -0.25f, Reverb = 0.15f },
                Arp = new Instrument { Wave = Wave.Fm, FmRatio = 3.5f, FmIndex = 1.5f, FmDecay = 0.1f, A = 0.001f, D = 0.15f, S = 0.1f, R = 0.08f, Gain = 0.08f, Pan = 0.3f, Delay = 0.2f },
                Counter = new Instrument { Wave = Wave.Pulse, PulseWidth = 0.25f, A = 0.002f, D = 0.1f, S = 0.5f, R = 0.05f, Gain = 0.07f, Pan = -0.35f },
                ChordCenter = 65, StabA = ".x.x.x.x.x.x.x.x", StabB = ".x.x.x.x.x.x.xxx",
                BassA = "r.o.r.o.r.o.r.o.", BassB = "r.o.r.o.r.o.rfo.", BassBridge = "r...o...r...o...",
                ArpRate = 4, ArpMode = "up", ArpCenter = 77,
                SnareKind = DrumKind.Clap,
                KickA = "x...x...x...x...", SnareA = "....x.......x...", HatA = "..x...x...x...x.",
                KickB = "x...x...x...x...", SnareB = "....x.......x.x.", HatB = "..x...x...x...x.",
                KickBridge = "x.......x.......", SnareBridge = "....x.......x...", HatBridge = "..x...x...x...x.",
                PercKind = DrumKind.Shaker, PercB = "xxxxxxxxxxxxxxxx",
                DriveHat = "xoxoxoxoxoxoxoxo", DriveOpen = "......x.......x.",
                DriveExtraKind = DrumKind.Bubble, DriveExtra = "...x.......x....",
            };
            var sc = new Score
            {
                ChordsA = new[] { "F", "Dm", "Bb", "C" },
                ChordsB = new[] { "Bb", "C", "Am", "Dm", "Bb", "C", "F", "F" },
                ChordsBridge = new[] { "F", "Bb" },
                MelodyA = new[]
                {
                    "C5:2 F5:2 A5:2 C6:2 A5:2 F5:2 A5:4",
                    "D6:2 C6:2 A5:2 F5:2 D5:4 F5:4",
                    "F5:2 Bb5:2 D6:4 C6:2 Bb5:2 A5:4",
                    "G5:2 A5:2 Bb5:2 G5:2 E5:4 C5:4",
                    "F5:1 G5:1 A5:2 C6:4 F6:4 E6:2 D6:2",
                    "C6:2 D6:2 A5:4 F5:4 A5:4",
                    "Bb5:2 C6:2 D6:2 F6:2 E6:2 D6:2 C6:4",
                    "C6:4 Bb5:2 G5:2 E5:8",
                },
                MelodyB = new[]
                {
                    "D6:6 C6:2 Bb5:4 F5:4",
                    "E6:6 D6:2 C6:4 G5:4",
                    "C6:2 E6:2 A6:4 G6:2 E6:2 C6:4",
                    "D6:4 F6:4 A6:8",
                    "F6:2 D6:2 Bb5:2 D6:2 F6:4 Bb6:4",
                    "G6:2 E6:2 C6:2 E6:2 G6:4 C7:4",
                    "A6:4 G6:2 F6:2 C6:4 A5:4",
                    "F6:8 _:8",
                },
                Bridge = new[] { "C6:4 _:4 A5:4 _:4", "Bb5:4 _:4 D6:8", "C6:4 _:4 F6:4 _:4", "D6:4 _:4 F6:8" },
            };
            Arrange(s, st, sc);
            return s;
        }

        /// <summary>Deep Ocean (bundle): chill harp, bubbles and a singing lead (Eb Lydian).</summary>
        static Song Ocean()
        {
            var s = new Song { Id = "ocean", Title = "Tide Diver", Bpm = 92, KeyRoot = 63, Scale = new[] { 0, 2, 4, 6, 7, 9, 11 }, ReverbRoom = 0.9f, ReverbDamp = 0.3f, DelayFeedback = 0.4f };
            s.Kit.KickDecay = 0.3f; s.Kit.KickDrive = 0.05f; s.Kit.KickPitch = 50f;
            var st = new Style
            {
                Lead = new Instrument { Wave = Wave.Sine, VibDepth = 0.2f, VibRate = 5f, VibDelay = 0.25f, A = 0.05f, D = 0.3f, S = 0.8f, R = 0.4f, Gain = 0.24f, Reverb = 0.45f, Delay = 0.25f },
                Harmony = new Instrument { Wave = Wave.Fm, FmRatio = 1f, FmIndex = 1.2f, FmDecay = 0.6f, A = 0.003f, D = 0.8f, S = 0.3f, R = 0.4f, Gain = 0.12f, Pan = -0.3f, Reverb = 0.4f },
                Bass = new Instrument { Wave = Wave.Triangle, A = 0.02f, D = 0.2f, S = 0.8f, R = 0.2f, Gain = 0.38f, Duck = 0.15f },
                Chords = new Instrument { Wave = Wave.SuperSaw, Voices = 4, Detune = 0.1f, Filter = true, Cutoff = 1100f, A = 0.8f, D = 0.5f, S = 0.8f, R = 1f, Gain = 0.1f, Reverb = 0.5f },
                Arp = new Instrument { Wave = Wave.Pluck, PluckDecay = 1.2f, PluckBright = 0.75f, A = 0.001f, D = 1f, S = 1f, R = 0.15f, Gain = 0.16f, Pan = 0.3f, Delay = 0.2f, Reverb = 0.35f },
                Counter = new Instrument { Wave = Wave.Fm, FmRatio = 3f, FmIndex = 1.5f, FmDecay = 0.3f, A = 0.002f, D = 0.4f, S = 0.2f, R = 0.3f, Gain = 0.08f, Pan = -0.4f, Reverb = 0.4f },
                PadChords = true, ChordCenter = 60,
                BassA = "r-----r-o-------", BassB = "r-----r-o---f---", BassBridge = "r---------------",
                ArpRate = 2, ArpMode = "updown", ArpCenter = 63, ArpInA = true,
                SnareKind = DrumKind.Rim, HatKind = DrumKind.Shaker,
                KickA = "x.........x.....", SnareA = "....x.......x...", HatA = "..x...x...x...x.",
                KickB = "x.....x...x.....", SnareB = "....x..x....x...", HatB = "..x...x...x...x.",
                KickBridge = "x...............", SnareBridge = "................", HatBridge = "................",
                PercKind = DrumKind.Bubble, PercBridge = "..x.....x...x...",
                DriveHatKind = DrumKind.Shaker, DriveHat = "xoxoxoxoxoxoxoxo", DriveOpen = "................",
                DriveExtraKind = DrumKind.Bubble, DriveExtra = "..x.....x...x...",
            };
            var sc = new Score
            {
                ChordsA = new[] { "Eb", "F", "Cm", "Bb" },
                ChordsB = new[] { "Gm", "F", "Cm", "Eb", "Gm", "F", "Bb", "Bb" },
                ChordsBridge = new[] { "Eb", "F" },
                MelodyA = new[]
                {
                    "G5:6 Bb5:2 D6:8",
                    "C6:6 A5:2 F5:8",
                    "G5:4 Eb5:4 C5:4 D5:4",
                    "D5:12 _:4",
                    "Bb5:4 G5:4 Eb6:6 D6:2",
                    "C6:4 A5:4 F6:8",
                    "Eb6:4 D6:2 C6:2 G5:4 Eb5:4",
                    "F5:8 D5:8",
                },
                MelodyB = new[]
                {
                    "Bb5:8 D6:8",
                    "C6:6 A5:2 F5:8",
                    "G5:4 C6:4 Eb6:8",
                    "D6:8 Bb5:8",
                    "D6:4 C6:4 Bb5:4 G5:4",
                    "A5:8 C6:8",
                    "D6:6 C6:2 Bb5:8",
                    "_:16",
                },
                Bridge = new[] { "Eb6:16", "F6:16", "G6:16", "A5:16" },
            };
            Arrange(s, st, sc);
            return s;
        }

        /// <summary>Neon City (bundle): synthwave with gated snares and a gliding lead.</summary>
        static Song Neon()
        {
            var s = new Song { Id = "neon", Title = "Neon Nights", Bpm = 106, KeyRoot = 60, Scale = new[] { 0, 2, 3, 5, 7, 8, 10 }, DelayBeats = 0.75f, DelayFeedback = 0.4f };
            s.Kit.GatedSnare = true; s.Kit.SnareReverb = 0.5f; s.Kit.KickPitch = 46f; s.Kit.KickPunch = 160f; s.Kit.KickDecay = 0.45f; s.Kit.KickDrive = 0.35f;
            var st = new Style
            {
                Lead = new Instrument { Wave = Wave.Pulse, PulseWidth = 0.5f, PwmDepth = 0.2f, PwmRate = 0.4f, Filter = true, Cutoff = 2600f, EnvCutoff = 1500f, FilterDecay = 0.3f, A = 0.01f, D = 0.3f, S = 0.75f, R = 0.25f, Glide = 0.06f, VibDepth = 0.15f, Gain = 0.22f, Delay = 0.3f, Reverb = 0.3f },
                Harmony = new Instrument { Wave = Wave.Saw, Filter = true, Cutoff = 1500f, A = 0.01f, D = 0.3f, S = 0.7f, R = 0.2f, Gain = 0.09f, Pan = -0.35f },
                Bass = new Instrument { Wave = Wave.Saw, Filter = true, Cutoff = 600f, EnvCutoff = 800f, FilterDecay = 0.12f, A = 0.002f, D = 0.1f, S = 0.8f, R = 0.04f, Gain = 0.3f, Duck = 0.5f },
                Chords = new Instrument { Wave = Wave.SuperSaw, Voices = 7, Detune = 0.22f, Filter = true, Cutoff = 2200f, A = 0.15f, D = 0.4f, S = 0.9f, R = 0.5f, Gain = 0.12f, Reverb = 0.35f, Duck = 0.55f },
                Arp = new Instrument { Wave = Wave.Saw, Filter = true, Cutoff = 2400f, EnvCutoff = 2000f, FilterDecay = 0.08f, A = 0.002f, D = 0.08f, S = 0.3f, R = 0.04f, Gain = 0.08f, Pan = 0.3f, Delay = 0.35f },
                Counter = new Instrument { Wave = Wave.Fm, FmRatio = 3.5f, FmIndex = 1.5f, FmDecay = 0.3f, A = 0.002f, D = 0.4f, S = 0.2f, R = 0.3f, Gain = 0.08f, Pan = -0.3f, Reverb = 0.3f },
                PadChords = true, ChordCenter = 60,
                BassA = "r.r.r.r.r.r.r.r.", BassB = "r.r.r.r.r.r.r.ro", BassBridge = "r.r.r.r.r.r.r.r.",
                ArpRate = 4, ArpMode = "up", ArpCenter = 60,
                KickA = "x.......x.......", SnareA = "....X.......X...", HatA = "x.x.x.x.x.x.x.x.",
                KickB = "x.......x.x.....", SnareB = "....X.......X...", HatB = "x.x.x.x.x.x.x.x.",
                KickBridge = "x.......x.......", SnareBridge = "................", HatBridge = "..x...x...x...x.",
                PercKind = DrumKind.Clap, PercB = "............x...",
                DriveHat = "xoxoxoxoxoxoxoxo", DriveOpen = "..x...x...x...x.",
            };
            var sc = new Score
            {
                ChordsA = new[] { "Cm", "Ab", "Eb", "Bb" },
                ChordsB = new[] { "Fm", "Ab", "Eb", "Bb", "Fm", "Ab", "G", "G" },
                ChordsBridge = new[] { "Cm", "Ab", "Eb", "Bb" },
                MelodyA = new[]
                {
                    "G5:6 Eb5:2 C5:4 Eb5:4",
                    "Ab5:6 G5:2 Eb5:8",
                    "G5:4 Bb5:4 Eb6:6 D6:2",
                    "D6:4 C6:2 Bb5:2 F5:8",
                    "C6:4 Bb5:2 G5:2 Eb5:4 G5:4",
                    "Ab5:4 C6:4 Eb6:8",
                    "G6:4 F6:2 Eb6:2 Bb5:4 G5:4",
                    "F5:4 G5:2 Bb5:2 D6:8",
                },
                MelodyB = new[]
                {
                    "C6:8 Ab5:4 F5:4",
                    "Eb6:8 C6:4 Ab5:4",
                    "Bb5:4 Eb6:4 G6:8",
                    "F6:8 D6:8",
                    "Ab6:6 G6:2 F6:4 C6:4",
                    "Eb6:6 F6:2 G6:8",
                    "B5:4 D6:4 G6:4 F6:4",
                    "D6:8 B5:8",
                },
                Bridge = new[] { "_:16", "_:16", "_:16", "_:16" },
            };
            Arrange(s, st, sc);
            return s;
        }
    }
}
