using OneBlock.Controls;
using OneBlock.Core;
using OneBlock.Gameplay;
using UnityEngine;

namespace OneBlock.DevTools
{
    /// <summary>
    /// Dev-only bot (inert in release builds) that plays the real game through simulated taps, using the
    /// same <see cref="BotPolicy"/> as the headless simulator.
    /// </summary>
    public class AutoPlayer : MonoBehaviour
    {
        public bool autoPlay;
        public bool autoRetry = true;
        public bool humanLike;

        [SerializeField] BlockBuilder builder;
        [SerializeField] PlayerController player;
        [SerializeField] TapInput tapInput;
        [SerializeField] RunController run;

        public int Runs { get; private set; }
        public float BestDistance { get; private set; }
        public float LastDistance { get; private set; }
        public DeathCause LastCause { get; private set; }

        BotPolicy _policy;
        float _cooldown;

        void OnEnable() => GameEvents.RunEnded += OnRunEnded;
        void OnDisable() => GameEvents.RunEnded -= OnRunEnded;

        void OnRunEnded(RunResult r)
        {
            Runs++;
            LastDistance = r.Distance;
            LastCause = r.Cause;
            BestDistance = Mathf.Max(BestDistance, r.Distance);
            Debug.Log($"[AutoPlayer] Run {Runs}: {r.Distance:0} m, cause {r.Cause}, coins {r.CoinsCollected}, blocks used {r.BlocksUsed}, blocks left {builder.Blocks}, at {player.Position.x:0.00},{player.Position.y:0.00}");
        }

        void Update()
        {
            if (!autoPlay || !Debug.isDebugBuild || GameManager.Instance == null) return;
            _policy ??= new BotPolicy(humanLike ? BotPolicy.Profile.Human : BotPolicy.Profile.Perfect, Runs + 1);
            _cooldown -= Time.deltaTime;
            var state = GameManager.Instance.State;

            if (state == GameState.Menu || state == GameState.GameOver || state == GameState.Continue)
            {
                if (_cooldown > 0f) return;
                _cooldown = 1f;
                _policy = null;
                if (state == GameState.Menu) run.Play();
                else if (state == GameState.Continue) run.DeclineRevive();
                else if (autoRetry) run.Restart();
                return;
            }
            if (state != GameState.Playing || _cooldown > 0f) return;

            if (run.TutorialHolding || _policy.ShouldTap(builder, player, GameManager.Instance.Config.startSpeed, Time.deltaTime))
            {
                tapInput.SimulateTap();
                _cooldown = 0.08f;
            }
        }
    }
}
