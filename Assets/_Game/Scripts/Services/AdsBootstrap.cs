using UnityEngine;

namespace OneBlock.Services
{
    /// <summary>
    /// Picks the ad service at startup: the real network (AdMob) on devices when it's installed, otherwise
    /// the scene's <see cref="MockAdService"/> overlay.
    /// </summary>
    [DefaultExecutionOrder(-300)]
    public class AdsBootstrap : MonoBehaviour
    {
        [SerializeField] AdsConfig config;

        void Awake()
        {
            Ads.Config = config;
            if (!Application.isEditor && Ads.NetworkFactory != null && config != null)
            {
                Ads.Service = Ads.NetworkFactory(config);
                Ads.IsNetwork = true;
            }
        }
    }
}
