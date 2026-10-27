using UnityEngine;

namespace OneBlock.Data
{
    /// <summary>
    /// A themed set (world + character + block + trail + death effect) that can't be bought with coins:
    /// it unlocks after the player watches <see cref="adsRequired"/> rewarded ads for this bundle.
    /// </summary>
    [CreateAssetMenu(fileName = "Bundle", menuName = "One Block/Ad Bundle")]
    public class Bundle : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public int adsRequired = 15;
        public Sprite thumbnail;

        public WorldTheme world;
        public CharacterSkin character;
        public BlockSkin block;
        public TrailSkin trail;
        public DeathEffect effect;

        /// <summary>Ids of every item the bundle grants (plus the bundle itself).</summary>
        public string[] ItemIds => new[] { id, world.id, character.id, block.id, trail.id, effect.id };
    }
}
