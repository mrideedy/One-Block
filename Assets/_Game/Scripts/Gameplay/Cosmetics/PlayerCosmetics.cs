using OneBlock.Core;
using OneBlock.Data;
using OneBlock.Save;
using UnityEngine;

namespace OneBlock.Gameplay
{
    /// <summary>
    /// Applies equipped cosmetics: character skin, block skin (placed blocks, pickups, HUD icon),
    /// trail and death effect. Re-applies whenever the shop changes something.
    /// </summary>
    public class PlayerCosmetics : MonoBehaviour
    {
        [SerializeField] ShopCatalog catalog;
        [SerializeField] PlayerController player;
        [SerializeField] PlayerAnimator animator;
        [SerializeField] TerrainRenderer terrain;
        [SerializeField] CollectibleSystem collectibles;
        [SerializeField] TrailRenderer trail;
        [SerializeField] EffectBurst effects;

        public ShopCatalog Catalog => catalog;
        public BlockSkin CurrentBlock { get; private set; }
        DeathEffect _effect;

        void OnEnable()
        {
            GameEvents.CosmeticsChanged += Apply;
            GameEvents.StateChanged += OnStateChanged;
        }

        void OnDisable()
        {
            GameEvents.CosmeticsChanged -= Apply;
            GameEvents.StateChanged -= OnStateChanged;
        }

        /// <summary>Called by RunController once gameplay systems are initialised.</summary>
        public void Init()
        {
            player.Died += OnDied;
            Apply();
        }

        /// <summary>Clears trail and lingering particles when the world resets.</summary>
        public void ResetVisuals()
        {
            trail.Clear();
            effects.Clear();
        }

        void OnDestroy()
        {
            if (player != null) player.Died -= OnDied;
        }

        public void Apply()
        {
            var d = SaveSystem.Data;
            animator.SetSkin(catalog.Character(d.equippedCharacter));

            CurrentBlock = catalog.Block(d.equippedBlock);
            terrain.SetBlockSkin(CurrentBlock);
            collectibles.SetPackSprite(CurrentBlock.sprite);

            var trailSkin = catalog.Trail(d.equippedTrail);
            if (trailSkin != null)
            {
                trail.colorGradient = trailSkin.colors;
                trail.time = trailSkin.lifetime;
                trail.widthMultiplier = trailSkin.width;
            }
            trail.enabled = trailSkin != null;
            trail.Clear();

            _effect = catalog.Effect(d.equippedDeathEffect);
        }

        void OnStateChanged(GameState from, GameState to)
        {
            // Only streak while actually running; clear so a new run doesn't draw a line from the old spot
            bool running = to == GameState.Playing;
            trail.emitting = running;
            if (to == GameState.Menu || from == GameState.GameOver) trail.Clear();
        }

        void OnDied(DeathCause cause)
        {
            trail.emitting = false;
            if (_effect == null) return;
            effects.Play(_effect, player.Position + new Vector2(0f, 0.55f));
            animator.SetVisible(false);
        }
    }
}
