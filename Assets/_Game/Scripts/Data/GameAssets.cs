using UnityEngine;

namespace OneBlock.Data
{
    /// <summary>Shared sprite references used by gameplay systems.</summary>
    [CreateAssetMenu(fileName = "GameAssets", menuName = "One Block/Game Assets")]
    public class GameAssets : ScriptableObject
    {
        [Header("Defaults")]
        public CharacterSkin defaultCharacter;
        public BlockSkin defaultBlock;
        public WorldTheme defaultWorld;

        [Header("Building")]
        public Sprite blockPreview;
        public Sprite blockPreviewBlocked;

        [Header("Props")]
        public Sprite spikes;
        public Sprite flag;
        public Sprite[] coinFrames;
        public Sprite blockPack;

        [Header("Misc")]
        public Sprite whitePixel;
    }
}
