using System;
using System.Collections.Generic;
using System.IO;
using OneBlock.Audio;
using UnityEditor;
using UnityEngine;

namespace OneBlock.EditorTools.Audio
{
    /// <summary>
    /// Renders the soundtrack and all sound effects to WAV (Assets/_Game/Audio/Generated), applies import
    /// settings and builds the AudioLibrary asset. Music is compressed to Vorbis by Unity on import.
    /// </summary>
    public static class AudioGenerator
    {
        public const string Root = "Assets/_Game/Audio/Generated";
        public const string LibraryPath = ProjectSetup.DataDir + "/AudioLibrary.asset";
        public static string StatusFile => Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, "Temp", "audio_generation.txt");

        [MenuItem("One Block/Audio/Generate All Audio", priority = 1)]
        public static void GenerateAllMenu() => GenerateAll(music: true, sfx: true);

        [MenuItem("One Block/Audio/Generate Sound Effects Only", priority = 2)]
        public static void GenerateSfxMenu() => GenerateAll(music: false, sfx: true);

        public static void GenerateAll(bool music, bool sfx)
        {
            var log = new System.Text.StringBuilder();
            var clock = System.Diagnostics.Stopwatch.StartNew();
            string projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            try
            {
                if (music)
                {
                    foreach (var (song, worldId) in Songs.All())
                    {
                        EditorUtility.DisplayProgressBar("One Block audio", $"Rendering {song.Title}", 0.5f);
                        var t0 = clock.Elapsed.TotalSeconds;
                        var stems = song.Render();
                        Dsp.WriteWav(Path.Combine(projectRoot, $"{Root}/Music/{song.Id}_main.wav"), stems.MainL, stems.MainR, stems.Length);
                        Dsp.WriteWav(Path.Combine(projectRoot, $"{Root}/Music/{song.Id}_drive.wav"), stems.DriveL, stems.DriveR, stems.Length);
                        log.AppendLine($"music {song.Id} '{song.Title}' {stems.Length / (float)Dsp.Rate:0.0}s in {clock.Elapsed.TotalSeconds - t0:0.0}s; mainRms {Rms(stems.MainL):0.000} driveRms {Rms(stems.DriveL):0.000}");
                    }
                }
                if (sfx)
                {
                    foreach (SfxId id in Enum.GetValues(typeof(SfxId)))
                    {
                        var data = SfxDesigner.Design(id);
                        Dsp.WriteWav(Path.Combine(projectRoot, $"{Root}/Sfx/{id}.wav"), data, null, data.Length);
                    }
                    log.AppendLine($"sfx {Enum.GetValues(typeof(SfxId)).Length} clips");
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            ApplyImportSettings();
            BuildLibrary();
            log.AppendLine($"done in {clock.Elapsed.TotalSeconds:0.0}s");
            File.WriteAllText(StatusFile, log.ToString());
            Debug.Log("[One Block] Audio generated.\n" + log);
        }

        /// <summary>Renders one song (by index in <see cref="Songs.All"/>) to WAV without importing. Returns a summary line.</summary>
        public static string RenderSong(int index)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            int i = 0;
            foreach (var (song, _) in Songs.All())
            {
                if (i++ != index) continue;
                var clock = System.Diagnostics.Stopwatch.StartNew();
                var stems = song.Render();
                Dsp.WriteWav(Path.Combine(projectRoot, $"{Root}/Music/{song.Id}_main.wav"), stems.MainL, stems.MainR, stems.Length);
                Dsp.WriteWav(Path.Combine(projectRoot, $"{Root}/Music/{song.Id}_drive.wav"), stems.DriveL, stems.DriveR, stems.Length);
                string bands = WriteAnalysis(Path.Combine(projectRoot, "Temp", $"audio_{song.Id}.png"), stems);
                return bands + $" | {song.Id} '{song.Title}' {stems.Length / (float)Dsp.Rate:0.0}s in {clock.Elapsed.TotalSeconds:0.0}s, mainRms {Rms(stems.MainL):0.000}, driveRms {Rms(stems.DriveL):0.000}, peak {Peak(stems.MainL, stems.DriveL):0.00}";
            }
            return "no song";
        }

        /// <summary>Imports rendered audio and rebuilds the library.</summary>
        public static void ImportAndBuild()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            ApplyImportSettings();
            BuildLibrary();
        }

        /// <summary>
        /// Diagnostic image: per 1/20 s column, RMS in four bands (sub/bass, low-mid, high-mid, air) for the
        /// main stem (top) and main+drive (bottom). Lets balance problems be spotted without listening.
        /// </summary>
        static string WriteAnalysis(string path, Song.Stems stems)
        {
            const int bandH = 60, gap = 6;
            int cols = Mathf.CeilToInt(stems.Length / (Dsp.Rate / 20f));
            int width = Mathf.Min(1600, cols);
            int samplesPerCol = Mathf.CeilToInt(stems.Length / (float)width);
            int height = (bandH * 4 + gap) * 2 + gap;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            var pixels = new Color[width * height];
            Color[] tints = { new(1f, 0.45f, 0.3f), new(1f, 0.85f, 0.3f), new(0.4f, 0.9f, 0.5f), new(0.4f, 0.7f, 1f) };
            var total = new double[4];

            for (int pass = 0; pass < 2; pass++)
            {
                var lp1 = new Dsp.OnePole(); var lp2 = new Dsp.OnePole(); var lp3 = new Dsp.OnePole();
                int yBase = pass == 0 ? height - bandH * 4 - gap : gap;
                for (int x = 0; x < width; x++)
                {
                    var energy = new double[4];
                    for (int s = x * samplesPerCol; s < Mathf.Min(stems.Length, (x + 1) * samplesPerCol); s++)
                    {
                        float v = stems.MainL[s] + (pass == 1 ? stems.DriveL[s] : 0f);
                        float a = lp1.Low(v, 250f), b = lp2.Low(v, 2000f), c = lp3.Low(v, 8000f);
                        energy[0] += a * a; energy[1] += (b - a) * (b - a); energy[2] += (c - b) * (c - b); energy[3] += (v - c) * (v - c);
                        if (pass == 0) { total[0] += a * a; total[1] += (b - a) * (b - a); total[2] += (c - b) * (c - b); total[3] += (v - c) * (v - c); }
                    }
                    for (int band = 0; band < 4; band++)
                    {
                        float rms = Mathf.Sqrt((float)(energy[band] / samplesPerCol));
                        float db = 20f * Mathf.Log10(Mathf.Max(1e-5f, rms));
                        int bar = Mathf.RoundToInt(Mathf.InverseLerp(-50f, -5f, db) * bandH);
                        int rowBottom = yBase + (3 - band) * bandH;
                        for (int y = 0; y < bar; y++) pixels[(rowBottom + y) * width + x] = tints[band];
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            string Db(double e) => (10.0 * Math.Log10(Math.Max(1e-12, e / stems.Length))).ToString("0.0");
            return $"bands dB <250:{Db(total[0])} 250-2k:{Db(total[1])} 2k-8k:{Db(total[2])} >8k:{Db(total[3])}";
        }

        static float Peak(float[] a, float[] b)
        {
            float p = 0f;
            for (int i = 0; i < a.Length; i++) p = Mathf.Max(p, Mathf.Abs(a[i] + b[i]));
            return p;
        }

        static float Rms(float[] buf)
        {
            double sum = 0;
            int n = Mathf.Min(buf.Length, Dsp.Rate * 30);
            for (int i = 0; i < n; i++) sum += buf[i] * buf[i];
            return (float)Math.Sqrt(sum / Mathf.Max(1, n));
        }

        static void ApplyImportSettings()
        {
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { Root }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (AssetImporter.GetAtPath(path) is not AudioImporter importer) continue;
                    bool isMusic = path.Contains("/Music/");
                    var settings = importer.defaultSampleSettings;
                    if (isMusic)
                    {
                        // In memory (not streamed) so the two stems stay sample-aligned when looping
                        settings.loadType = AudioClipLoadType.CompressedInMemory;
                        settings.compressionFormat = AudioCompressionFormat.Vorbis;
                        settings.quality = 0.55f;
                        importer.forceToMono = false;
                        importer.loadInBackground = true;
                    }
                    else
                    {
                        settings.loadType = AudioClipLoadType.DecompressOnLoad;
                        settings.compressionFormat = AudioCompressionFormat.ADPCM;
                        importer.forceToMono = true;
                    }
                    settings.preloadAudioData = true;
                    importer.defaultSampleSettings = settings;
                    importer.SaveAndReimport();
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        public static AudioLibrary BuildLibrary()
        {
            var lib = AssetDatabase.LoadAssetAtPath<AudioLibrary>(LibraryPath);
            if (lib == null)
            {
                lib = ScriptableObject.CreateInstance<AudioLibrary>();
                AssetDatabase.CreateAsset(lib, LibraryPath);
            }

            var tracks = new List<MusicTrack>();
            foreach (var (song, worldId) in Songs.All())
            {
                tracks.Add(new MusicTrack
                {
                    id = song.Id,
                    title = song.Title,
                    worldId = worldId,
                    keyRoot = song.KeyRoot,
                    main = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Root}/Music/{song.Id}_main.wav"),
                    drive = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Root}/Music/{song.Id}_drive.wav"),
                });
            }
            lib.tracks = tracks.ToArray();

            var ids = (SfxId[])Enum.GetValues(typeof(SfxId));
            lib.sfx = new AudioClip[ids.Length];
            foreach (var id in ids) lib.sfx[(int)id] = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Root}/Sfx/{id}.wav");

            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
            return lib;
        }
    }
}
