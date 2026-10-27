using UnityEngine;

namespace OneBlock.Data
{
    /// <summary>Colour streak drawn behind the running character.</summary>
    [CreateAssetMenu(fileName = "TrailSkin", menuName = "One Block/Trail Skin")]
    public class TrailSkin : ScriptableObject
    {
        public string id;
        public string displayName;
        public int price;
        [Tooltip("Set when the item is exclusive to an ad bundle (then it can't be bought with coins).")]
        public string bundleId;
        public Sprite icon;
        public Gradient colors = new Gradient();
        public float width = 0.5f;
        public float lifetime = 0.28f;
    }
}
