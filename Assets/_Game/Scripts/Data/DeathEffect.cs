using UnityEngine;

namespace OneBlock.Data
{
    public enum DeathEffectStyle
    {
        Explosion,
        Poof,
        Skull,
        Confetti
    }

    /// <summary>Particle burst that replaces the character when a run ends.</summary>
    [CreateAssetMenu(fileName = "DeathEffect", menuName = "One Block/Death Effect")]
    public class DeathEffect : ScriptableObject
    {
        public string id;
        public string displayName;
        public int price;
        [Tooltip("Set when the item is exclusive to an ad bundle (then it can't be bought with coins).")]
        public string bundleId;
        public Sprite icon;
        public DeathEffectStyle style;
        public Sprite[] particles;
        public Color[] palette = { Color.white };
    }
}
