using System;
using System.IO;
using UnityEngine;

namespace OneBlock.Save
{
    /// <summary>
    /// JSON save in persistentDataPath. Writes go to a temp file first and the previous save is kept
    /// as a backup, so a crash or kill mid-write can't wipe progress.
    /// </summary>
    public static class SaveSystem
    {
        const string FileName = "save.json";
        const string TempName = "save.tmp";
        const string BackupName = "save.bak";

        static SaveData _data;
        static bool _dirty;

        // With "Enter Play Mode Options" (no domain reload) statics survive between play sessions in the
        // editor; drop the cache so every session reads the file fresh.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _data = null;
            _dirty = false;
        }

        public static SaveData Data
        {
            get
            {
                if (_data == null) Load();
                return _data;
            }
        }

        static string Dir => Application.persistentDataPath;
        static string MainPath => Path.Combine(Dir, FileName);
        static string TempPath => Path.Combine(Dir, TempName);
        static string BackupPath => Path.Combine(Dir, BackupName);

        public static void Load()
        {
            _data = TryRead(MainPath) ?? TryRead(BackupPath) ?? new SaveData();
            Migrate(_data);
            _dirty = false;
        }

        /// <summary>Mark data changed; flushed by <see cref="SaveIfDirty"/> at safe points (run end, pause, quit).</summary>
        public static void MarkDirty() => _dirty = true;

        public static void SaveIfDirty()
        {
            if (_dirty) Save();
        }

        public static void Save()
        {
            if (_data == null) return;
            try
            {
                string json = JsonUtility.ToJson(_data, prettyPrint: false);
                File.WriteAllText(TempPath, json);
                if (File.Exists(MainPath))
                {
                    if (File.Exists(BackupPath)) File.Delete(BackupPath);
                    File.Move(MainPath, BackupPath);
                }
                File.Move(TempPath, MainPath);
                _dirty = false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Save failed: {e.Message}");
            }
        }

        public static void ResetAll()
        {
            _data = new SaveData();
            Save();
        }

        static SaveData TryRead(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json)) return null;
                return JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] Could not read {Path.GetFileName(path)}: {e.Message}");
                return null;
            }
        }

        static void Migrate(SaveData data)
        {
            // Fields added after launch come back as defaults from older saves; make those safe
            if (data.playerLevel < 1) data.playerLevel = 1;
            data.missions ??= new System.Collections.Generic.List<MissionState>();
            data.bundleProgress ??= new System.Collections.Generic.List<BundleProgress>();
            data.dailyRecords ??= new System.Collections.Generic.List<DailyRecord>();
            data.version = SaveData.CurrentVersion;
        }
    }
}
