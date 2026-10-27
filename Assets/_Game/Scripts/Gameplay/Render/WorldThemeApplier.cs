using OneBlock.Core;
using OneBlock.Data;
using OneBlock.Save;
using UnityEngine;

namespace OneBlock.Gameplay
{
    /// <summary>
    /// Dresses the scene in the selected world: sky, parallax layers, camera clear colour, ground tiles
    /// and decor. Switching world rebuilds the level preview on the menu.
    /// </summary>
    public class WorldThemeApplier : MonoBehaviour
    {
        [SerializeField] RunController run;
        [SerializeField] Camera targetCamera;
        [SerializeField] SkyBackground sky;
        [SerializeField] ParallaxLayer mountainsFar;
        [SerializeField] ParallaxLayer mountainsNear;
        [SerializeField] ParallaxLayer hills;
        [SerializeField] TerrainRenderer terrain;

        WorldTheme _current;

        public WorldTheme Current => _current;

        void OnEnable() => GameEvents.CosmeticsChanged += OnCosmeticsChanged;
        void OnDisable() => GameEvents.CosmeticsChanged -= OnCosmeticsChanged;

        /// <summary>Called by RunController before the first world reset.</summary>
        public void Init()
        {
            Apply(resetWorld: false);
        }

        void OnCosmeticsChanged()
        {
            if (run.Cosmetics.Catalog.World(SaveSystem.Data.selectedWorld) != _current) Apply(resetWorld: true);
        }

        void Apply(bool resetWorld)
        {
            var catalog = run.Cosmetics.Catalog;
            string id = SaveSystem.Data.selectedWorld;
            if (!Inventory.IsOwned(id)) id = catalog.worlds[0].id;
            var theme = catalog.World(id);
            _current = theme;

            targetCamera.backgroundColor = theme.cameraBackground;
            sky.Configure(targetCamera, theme.sky, theme.clouds);
            mountainsFar.SetSprite(theme.mountainsFar, Color.clear);
            mountainsNear.SetSprite(theme.mountainsNear, Color.clear);
            hills.SetSprite(theme.hills, theme.hillsFill);
            terrain.SetTheme(theme);
            run.DynamicRenderer.SetEnemyFrames(theme.enemyFrames);

            // Mid-run the level keeps its current look; on the menu rebuild so the preview matches
            if (resetWorld && GameManager.Instance != null && GameManager.Instance.State == GameState.Menu)
                run.ResetWorld(GameManager.Instance.SelectedMode);
        }
    }
}
