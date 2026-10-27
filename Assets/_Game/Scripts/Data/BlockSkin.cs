using UnityEngine;

namespace OneBlock.Data
{
    /// <summary>Cosmetic look for placed blocks. All skins behave identically.</summary>
    [CreateAssetMenu(fileName = "BlockSkin", menuName = "One Block/Block Skin")]
    public class BlockSkin : ScriptableObject
    {
        public string id;
        public string displayName;
        public int price;
        [Tooltip("Set when the item is exclusive to an ad bundle (then it can't be bought with coins).")]
        public string bundleId;
        public Sprite sprite;
    }
}
