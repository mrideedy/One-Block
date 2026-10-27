using UnityEngine;

namespace OneBlock.Data
{
    [CreateAssetMenu(fileName = "CharacterSkin", menuName = "One Block/Character Skin")]
    public class CharacterSkin : ScriptableObject
    {
        public string id;
        public string displayName;
        public int price;
        [Tooltip("Set when the item is exclusive to an ad bundle (then it can't be bought with coins).")]
        public string bundleId;

        public Sprite[] idle;
        public Sprite[] run;
        public Sprite jump;
        public Sprite fall;
        public Sprite hit;
        public Sprite portrait;
    }
}
