using System;
using UnityEngine;

namespace OneBlock.Audio
{
    /// <summary>Every sound effect in the game. Generated clips are named after these values.</summary>
    public enum SfxId
    {
        // UI
        UiClick, UiTab, UiBack, UiToggle, Purchase, Denied, Equip, AdReward, Toast,
        // Movement & pickups
        Hop, WallBump, Coin, BlockPack, BuildDenied,
        // Block placement, one per skin
        Place_Normal, Place_Bouncy, Place_Ice, Place_Explosive, Place_Ghost, Place_Stone, Place_Wood,
        Place_Grass, Place_Crystal, Place_Galaxy, Place_Candy, Place_Coral, Place_Neon,
        // Enemy squashes, one per enemy style
        Squash_Shadow, Squash_Scorpion, Squash_Penguin, Squash_MagmaBlob, Squash_Alien, Squash_GummyBear, Squash_Crab, Squash_Drone,
        // Deaths, one per death effect (+ default and falling)
        Death_Default, Death_Explosion, Death_Poof, Death_Skull, Death_Confetti, Death_Sprinkles, Death_Bubbles, Death_Glitch, FallWhistle,
        // Moments & jingles
        Revive, TapToGo, Tick, MissionComplete, LevelUp, NewBest, GameOver, DailyBonus, Milestone, TutorialPop, IntroSting
    }

    [Serializable]
    public class MusicTrack
    {
        public string id;
        public string title;
        [Tooltip("World that plays this track automatically (empty for the menu theme).")]
        public string worldId;
        public AudioClip main;
        [Tooltip("Adaptive layer, sample-aligned with main; faded in as the run speeds up.")]
        public AudioClip drive;
        [Tooltip("Tonic as a midi note; musical pickups (coins) play in this key.")]
        public int keyRoot = 60;
    }

    /// <summary>All generated music and sound effects.</summary>
    [CreateAssetMenu(fileName = "AudioLibrary", menuName = "One Block/Audio Library")]
    public class AudioLibrary : ScriptableObject
    {
        public MusicTrack[] tracks;
        [Tooltip("Indexed by SfxId.")]
        public AudioClip[] sfx;

        public MusicTrack Track(string id)
        {
            if (tracks == null) return null;
            foreach (var t in tracks) if (t != null && t.id == id) return t;
            return null;
        }

        public MusicTrack TrackForWorld(string worldId)
        {
            if (tracks == null) return null;
            foreach (var t in tracks) if (t != null && t.worldId == worldId) return t;
            return null;
        }

        public AudioClip Clip(SfxId id)
        {
            int i = (int)id;
            return sfx != null && i < sfx.Length ? sfx[i] : null;
        }
    }
}
