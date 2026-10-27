using UnityEngine;

namespace OneBlock.Data
{
    /// <summary>Visual theme for a world: ground tiles, parallax layers and decor.</summary>
    [CreateAssetMenu(fileName = "WorldTheme", menuName = "One Block/World Theme")]
    public class WorldTheme : ScriptableObject
    {
        public string id;
        public string displayName;
        public int price;
        [Tooltip("Set when the world is exclusive to an ad bundle (then it can't be bought with coins).")]
        public string bundleId;
        public Sprite thumbnail;

        [Header("Ground tiles (100px = 1 cell, pivot bottom-left)")]
        public Sprite tileTop;
        public Sprite tileTopLeft;
        public Sprite tileTopRight;
        public Sprite tileTopSingle;
        public Sprite tileFill;
        public Sprite tileFillLeft;
        public Sprite tileFillRight;
        public Sprite tileFillSingle;

        [Header("Background")]
        public Sprite sky;
        public Sprite mountainsFar;
        public Sprite mountainsNear;
        public Sprite hills;
        public Color hillsFill = new Color(0.27f, 0.55f, 0.37f);
        public Color cameraBackground = new Color(0.55f, 0.8f, 0.96f);
        public Sprite[] clouds;

        [Header("Obstacles (same gameplay in every world, themed look)")]
        [Tooltip("Animated frames for a deadly cell (pivot at the cell floor).")]
        public Sprite[] hazardFrames;
        [Tooltip("What the hazard is called in tutorial text, e.g. \"spikes\", \"lava\".")]
        public string hazardName = "spikes";
        [Tooltip("Walk-cycle frames for patrolling enemies, facing left.")]
        public Sprite[] enemyFrames;
        [Tooltip("Enemy style name, used to pick the squash sound (e.g. \"Penguin\").")]
        public string enemyStyle = "Shadow";

        [Header("Decor")]
        public Sprite[] trees;
        public Sprite bush;
        public Sprite tuft;

        public Sprite GetTile(bool top, bool openLeft, bool openRight)
        {
            if (top)
            {
                if (openLeft && openRight) return tileTopSingle;
                if (openLeft) return tileTopLeft;
                return openRight ? tileTopRight : tileTop;
            }
            if (openLeft && openRight) return tileFillSingle;
            if (openLeft) return tileFillLeft;
            return openRight ? tileFillRight : tileFill;
        }
    }
}
