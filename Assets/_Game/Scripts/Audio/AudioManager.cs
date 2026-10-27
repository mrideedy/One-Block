using System;
using System.Collections.Generic;
using OneBlock.Core;
using OneBlock.Gameplay;
using OneBlock.Save;
using UnityEngine;

namespace OneBlock.Audio
{
    /// <summary>
    /// Music and sound effects.
    /// Music: each world has its own track (the menu has the theme). Tracks come as two sample-aligned stems,
    /// and the "drive" stem fades in as the run gets faster. Pausing muffles the music, dying slows it down
    /// like a record stopping, and the player can pick any unlocked track in Settings.
    /// SFX: one clip per element (block skin, enemy, death effect...), with coins played musically in the
    /// current track's key.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] AudioLibrary library;
        [SerializeField] RunController run;
        [Range(0f, 1f)] [SerializeField] float musicVolume = 0.62f;
        [Range(0f, 1f)] [SerializeField] float sfxVolume = 0.9f;
        [SerializeField] int sfxVoices = 14;

        public AudioLibrary Library => library;
        public MusicTrack CurrentTrack => _current?.Track;

        sealed class Deck
        {
            public GameObject Go;
            public AudioSource Main, Drive;
            public AudioLowPassFilter Filter;
            public MusicTrack Track;
            public float Fade, FadeTarget;
        }

        Deck _current, _previous;
        AudioSource[] _sfx;
        int _nextVoice;
        float _intensity;
        float _pitch = 1f, _pitchTarget = 1f;
        float _pitchRestoreAt = -1f;
        float _stateGain = 1f, _cutoff = 22000f;
        int _coinStep;
        float _lastCoinTime = -10f;
        int _lastMilestone;
        bool _wasBlocked;
        readonly Dictionary<string, SfxId> _cache = new();

        static readonly int[] CoinSteps = { 0, 2, 4, 7, 9, 12, 14, 16, 19, 21, 24 };

        // ------------------------------------------------------------------ lifecycle

        void Awake()
        {
            Instance = this;
            _current = CreateDeck("MusicA");
            _previous = CreateDeck("MusicB");
            _sfx = new AudioSource[sfxVoices];
            for (int i = 0; i < sfxVoices; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                _sfx[i] = s;
            }
        }

        Deck CreateDeck(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var d = new Deck { Go = go, Main = go.AddComponent<AudioSource>(), Drive = go.AddComponent<AudioSource>(), Filter = go.AddComponent<AudioLowPassFilter>() };
            foreach (var s in new[] { d.Main, d.Drive })
            {
                s.playOnAwake = false;
                s.loop = true;
                s.spatialBlend = 0f;
                s.volume = 0f;
            }
            d.Filter.cutoffFrequency = 22000f;
            return d;
        }

        void OnEnable()
        {
            GameEvents.BlockBuilt += OnBlockBuilt;
            GameEvents.BuildDenied += OnBuildDenied;
            GameEvents.CoinCollected += OnCoin;
            GameEvents.PackCollected += OnPack;
            GameEvents.EnemySquashed += OnSquash;
            GameEvents.PlayerDied += OnDied;
            GameEvents.Revived += OnRevived;
            GameEvents.RunStarted += OnRunStarted;
            GameEvents.RunEnded += OnRunEnded;
            GameEvents.MissionCompleted += OnMission;
            GameEvents.DistanceChanged += OnDistance;
            GameEvents.AwaitingTapChanged += OnAwaitingTap;
            GameEvents.Tutorial += OnTutorial;
            GameEvents.StateChanged += OnState;
        }

        void OnDisable()
        {
            GameEvents.BlockBuilt -= OnBlockBuilt;
            GameEvents.BuildDenied -= OnBuildDenied;
            GameEvents.CoinCollected -= OnCoin;
            GameEvents.PackCollected -= OnPack;
            GameEvents.EnemySquashed -= OnSquash;
            GameEvents.PlayerDied -= OnDied;
            GameEvents.Revived -= OnRevived;
            GameEvents.RunStarted -= OnRunStarted;
            GameEvents.RunEnded -= OnRunEnded;
            GameEvents.MissionCompleted -= OnMission;
            GameEvents.DistanceChanged -= OnDistance;
            GameEvents.AwaitingTapChanged -= OnAwaitingTap;
            GameEvents.Tutorial -= OnTutorial;
            GameEvents.StateChanged -= OnState;
        }

        void Start()
        {
            if (run != null && run.Player != null) run.Player.Hopped += () => Play(SfxId.Hop, 0.45f, 1f, 0.06f);
        }

        // ------------------------------------------------------------------ public API

        /// <summary>Plays a sound effect (respects the Sound setting).</summary>
        public void Play(SfxId id, float volume = 1f, float pitch = 1f, float jitter = 0.03f)
        {
            if (!SaveSystem.Data.sfxOn || library == null) return;
            var clip = library.Clip(id);
            if (clip == null) return;
            var src = _sfx[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _sfx.Length;
            src.pitch = pitch * (1f + UnityEngine.Random.Range(-jitter, jitter));
            src.PlayOneShot(clip, volume * sfxVolume);
        }

        /// <summary>Convenience for UI code that may run before the manager exists.</summary>
        public static void Sfx(SfxId id, float volume = 1f) => Instance?.Play(id, volume, 1f, 0.02f);

        /// <summary>Tracks the player can choose in Settings: the theme plus the tracks of owned worlds.</summary>
        public List<MusicTrack> UnlockedTracks()
        {
            var list = new List<MusicTrack>();
            if (library?.tracks == null) return list;
            foreach (var t in library.tracks)
                if (t != null && (string.IsNullOrEmpty(t.worldId) || Inventory.IsOwned(t.worldId))) list.Add(t);
            return list;
        }

        // ------------------------------------------------------------------ music

        MusicTrack DesiredTrack()
        {
            if (library == null) return null;
            string pick = SaveSystem.Data.musicTrack;
            if (!string.IsNullOrEmpty(pick))
            {
                var chosen = library.Track(pick);
                if (chosen != null && (string.IsNullOrEmpty(chosen.worldId) || Inventory.IsOwned(chosen.worldId))) return chosen;
            }
            var gm = GameManager.Instance;
            bool menu = gm == null || gm.State == GameState.Menu || gm.State == GameState.Boot;
            if (menu) return library.Track("theme") ?? library.tracks[0];
            string world = SaveSystem.Data.selectedWorld;
            return library.TrackForWorld(world) ?? library.Track("theme");
        }

        void StartTrack(MusicTrack track)
        {
            // Swap decks: the old one fades out while the new one fades in
            (_previous, _current) = (_current, _previous);
            _previous.FadeTarget = 0f;

            _current.Track = track;
            _current.Main.clip = track.main;
            _current.Drive.clip = track.drive;
            double at = AudioSettings.dspTime + 0.06;
            _current.Main.PlayScheduled(at);
            if (track.drive != null) _current.Drive.PlayScheduled(at);
            _current.Fade = 0f;
            _current.FadeTarget = 1f;
        }

        void Update()
        {
            var desired = DesiredTrack();
            if (desired != null && desired != _current.Track) StartTrack(desired);

            float dt = Time.unscaledDeltaTime;
            var gm = GameManager.Instance;
            var state = gm != null ? gm.State : GameState.Boot;

            // Intensity follows run distance (the level speeds up with it)
            float intensityTarget = 0f;
            if (state == GameState.Playing && run != null && !run.TutorialActive)
                intensityTarget = Mathf.InverseLerp(50f, 400f, run.Distance);
            _intensity = Mathf.MoveTowards(_intensity, intensityTarget, dt * 0.5f);

            (float gain, float cutoff) = state switch
            {
                GameState.Paused => (0.4f, 650f),
                GameState.Continue => (0.55f, 1400f),
                GameState.GameOver => (0.6f, 3000f),
                _ => (1f, 22000f)
            };
            _stateGain = Mathf.MoveTowards(_stateGain, gain, dt * 2f);
            _cutoff = Mathf.Lerp(_cutoff, cutoff, 1f - Mathf.Exp(-dt * 8f));
            if (_pitchRestoreAt > 0f && Time.unscaledTime >= _pitchRestoreAt)
            {
                _pitchRestoreAt = -1f;
                _pitchTarget = 1f;
            }
            _pitch = Mathf.MoveTowards(_pitch, _pitchTarget, dt * (_pitchTarget < _pitch ? 0.7f : 1.2f));

            float master = SaveSystem.Data.musicOn ? musicVolume : 0f;
            UpdateDeck(_current, master, dt);
            UpdateDeck(_previous, master, dt);

            // Wall bump when the player gets stopped by a tall wall
            if (run != null && run.Player != null && state == GameState.Playing)
            {
                bool blocked = run.Player.IsBlocked && run.Player.WallHeight >= 2 && run.Player.Grounded;
                if (blocked && !_wasBlocked)
                {
                    Play(SfxId.WallBump, 0.7f);
                    Haptics.Play(Haptics.Strength.Light);
                }
                _wasBlocked = blocked;
            }
        }

        void UpdateDeck(Deck d, float master, float dt)
        {
            d.Fade = Mathf.MoveTowards(d.Fade, d.FadeTarget, dt / 1.2f);
            float vol = master * _stateGain * d.Fade;
            d.Main.volume = vol;
            d.Drive.volume = vol * _intensity;
            d.Main.pitch = d.Drive.pitch = _pitch;
            d.Filter.cutoffFrequency = _cutoff;
            if (d.Fade <= 0f && d.FadeTarget <= 0f && d.Main.isPlaying)
            {
                d.Main.Stop();
                d.Drive.Stop();
                d.Track = null;
            }
        }

        // ------------------------------------------------------------------ gameplay hooks

        SfxId Lookup(string name, SfxId fallback)
        {
            if (string.IsNullOrEmpty(name)) return fallback;
            if (_cache.TryGetValue(name, out var id)) return id;
            id = Enum.TryParse(name, out SfxId parsed) ? parsed : fallback;
            _cache[name] = id;
            return id;
        }

        static string Title(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);

        void OnBlockBuilt(BuildKind kind)
        {
            string skin = run != null && run.Cosmetics != null && run.Cosmetics.CurrentBlock != null ? run.Cosmetics.CurrentBlock.id : "block_normal";
            string suffix = skin.StartsWith("block_") ? skin.Substring(6) : skin;
            Play(Lookup("Place_" + Title(suffix), SfxId.Place_Normal), 0.9f);
            Haptics.Play(Haptics.Strength.Light);
        }

        void OnBuildDenied()
        {
            Play(SfxId.BuildDenied, 0.8f);
            Haptics.Play(Haptics.Strength.Light);
        }

        void OnCoin()
        {
            if (Time.unscaledTime - _lastCoinTime > 0.9f) _coinStep = 0;
            _lastCoinTime = Time.unscaledTime;
            int key = _current.Track != null ? _current.Track.keyRoot : 60;
            int offset = ((key % 12) + 12) % 12;
            if (offset > 6) offset -= 12;
            int semis = offset + CoinSteps[Mathf.Min(_coinStep, CoinSteps.Length - 1)];
            _coinStep++;
            Play(SfxId.Coin, 0.55f, Mathf.Pow(2f, semis / 12f), 0f);
        }

        void OnPack(int count)
        {
            Play(SfxId.BlockPack, 0.9f);
            Haptics.Play(Haptics.Strength.Light);
        }

        void OnSquash()
        {
            var world = run != null && run.Cosmetics != null ? run.Cosmetics.Catalog.World(SaveSystem.Data.selectedWorld) : null;
            string style = world != null ? world.enemyStyle : "Shadow";
            Play(Lookup("Squash_" + style, SfxId.Squash_Shadow));
            Haptics.Play(Haptics.Strength.Medium);
        }

        void OnDied()
        {
            var cause = run != null ? run.LastDeathCause : DeathCause.None;
            if (cause == DeathCause.Fell) Play(SfxId.FallWhistle, 0.8f, 1f, 0f);
            else
            {
                string fx = SaveSystem.Data.equippedDeathEffect;
                string name = string.IsNullOrEmpty(fx) ? "" : "Death_" + Title(fx.StartsWith("fx_") ? fx.Substring(3) : fx);
                Play(Lookup(name, SfxId.Death_Default), 1f, 1f, 0f);
            }
            Haptics.Play(Haptics.Strength.Heavy);
            _pitchTarget = 0.55f;   // record-stop slowdown, then back up (still muffled) for the revive offer
            _pitchRestoreAt = Time.unscaledTime + 1.1f;
        }

        void OnRevived()
        {
            _pitchTarget = 1f;
            Play(SfxId.Revive);
        }

        void OnAwaitingTap(bool waiting)
        {
            if (!waiting && GameManager.Instance != null && GameManager.Instance.State == GameState.Playing) Play(SfxId.TapToGo, 0.8f);
        }

        void OnRunStarted(GameMode mode)
        {
            _pitchTarget = 1f;
            _pitch = 1f;
            _lastMilestone = 0;
            _coinStep = 0;
        }

        void OnRunEnded(RunResult r)
        {
            bool daily = r.Mode == GameMode.DailyChallenge;
            bool celebrate = r.IsNewBest && (!daily || r.PreviousBest > 0f);
            StartCoroutine(Later(0.35f, () => Play(celebrate ? SfxId.NewBest : SfxId.GameOver, 0.9f, 1f, 0f)));
            if (r.DailyFirstToday && r.DailyReward > 0) StartCoroutine(Later(1.0f, () => Play(SfxId.DailyBonus, 0.8f, 1f, 0f)));
            if (r.LevelAfter > r.LevelBefore) StartCoroutine(Later(1.5f, () =>
            {
                Play(SfxId.LevelUp, 1f, 1f, 0f);
                Haptics.Play(Haptics.Strength.Medium);
            }));
        }

        void OnMission(MissionState m)
        {
            Play(SfxId.MissionComplete, 0.9f, 1f, 0f);
            Haptics.Play(Haptics.Strength.Medium);
        }

        void OnDistance(float metres)
        {
            int hundreds = Mathf.FloorToInt(metres / 100f);
            if (hundreds > _lastMilestone && GameManager.Instance != null && GameManager.Instance.State == GameState.Playing)
            {
                _lastMilestone = hundreds;
                Play(SfxId.Milestone, 0.5f, 1f, 0f);
            }
        }

        void OnTutorial(TutorialEvent e)
        {
            switch (e.Phase)
            {
                case TutorialPhase.Hold: Play(SfxId.TutorialPop, 0.8f, 1f, 0f); break;
                case TutorialPhase.Success: Play(SfxId.Equip, 0.8f, 1f, 0f); break;
                case TutorialPhase.Complete: Play(SfxId.MissionComplete, 0.9f, 1f, 0f); break;
            }
        }

        void OnState(GameState from, GameState to)
        {
            if (to == GameState.Menu) _pitchTarget = 1f;
        }

        static System.Collections.IEnumerator Later(float seconds, Action action)
        {
            yield return new WaitForSecondsRealtime(seconds);
            action();
        }
    }
}
