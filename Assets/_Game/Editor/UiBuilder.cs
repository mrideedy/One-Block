using OneBlock.Data;
using OneBlock.Gameplay;
using OneBlock.Services;
using OneBlock.UI;
using OneBlock.Util;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace OneBlock.EditorTools
{
    /// <summary>
    /// Builds the whole uGUI hierarchy for the Game scene (landscape, 1920×1080 reference):
    /// HUD, main menu, pause, continue, results, settings, stats, toast and the mock ad overlay.
    /// </summary>
    public static class UiBuilder
    {
        const string Ui = "Assets/_Game/Art/Generated/UI/";
        const string Icons = Ui + "Icons/";

        const float MenuX = 0f;     // menu column is centred; the camera frames the hero further left on the menu

        static TMP_FontAsset _font;
        static Material _outline;

        static readonly Color Ink = new Color(0.11f, 0.15f, 0.2f, 0.94f);
        static readonly Color Dim = new Color(0.04f, 0.06f, 0.1f, 0.6f);
        static readonly Color Soft = new Color(1f, 1f, 1f, 0.72f);
        static readonly Color Gold = new Color(1f, 0.78f, 0.24f);
        static readonly Color Coral = new Color(1f, 0.42f, 0.36f);

        public struct Result
        {
            public GameObject AdOverlay;
            public TMP_Text AdLabel;
        }

        public static Result Build(GameAssets assets, RunController run)
        {
            _font = FontAssetBuilder.EnsureFont();
            _outline = FontAssetBuilder.EnsureOutlineMaterial(_font);

            var canvasGo = new GameObject("[UI]", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;

            var safe = Rect(canvasGo.transform, "SafeArea");
            Stretch(safe);
            safe.gameObject.AddComponent<SafeArea>();
            var router = safe.gameObject.AddComponent<UIRouter>();

            var hud = BuildHud(safe, assets);
            var menu = BuildMainMenu(safe, assets, run, router);
            var pause = BuildPause(safe, run);
            var cont = BuildContinue(safe, run);
            var results = BuildResults(safe, run);
            var settings = BuildSettings(safe);
            var stats = BuildStats(safe);
            var shop = BuildShop(safe, run, router);
            var missions = BuildMissions(safe, run, router, assets);
            var (toast, toastText) = BuildToast(safe);
            BuildTutorialOverlay((RectTransform)hud.transform);
            BuildMissionBanner((RectTransform)hud.transform);
            BuildIntro(safe, assets);

            Set(router, "mainMenu", menu);
            Set(router, "hud", hud);
            Set(router, "pause", pause);
            Set(router, "continueScreen", cont);
            Set(router, "results", results);
            Set(router, "settings", settings);
            Set(router, "stats", stats);
            Set(router, "shop", shop);
            Set(router, "missions", missions);
            Set(router, "toast", toast);
            Set(router, "toastText", toastText);

            // The mock ad covers everything, including the safe-area margins
            var ad = Rect(canvasGo.transform, "MockAdOverlay");
            Stretch(ad);
            Img(ad, Ui + "white.png", new Color(0.02f, 0.02f, 0.04f, 0.96f), raycast: true);
            var adLabel = Text(ad, "Label", "TEST AD", 120, Color.white, V(0, 0), V(1400, 500), outline: true);
            ad.gameObject.SetActive(false);

            return new Result { AdOverlay = ad.gameObject, AdLabel = adLabel };
        }

        // ================================================================ HUD

        static HudScreen BuildHud(RectTransform root, GameAssets assets)
        {
            var screen = Screen<HudScreen>(root, "HUD", dim: false);
            var r = (RectTransform)screen.transform;

            var pause = IconButton(r, "PauseButton", Icons + "icon_pause.png", new Vector2(0, 1), V(48 + 60, -40 - 60), 120);

            var distance = Pill(r, "DistancePill", new Vector2(1, 1), V(-48 - 150, -40 - 55), V(300, 110), null, "0 m", out var distanceText);
            var blocks = Pill(r, "BlocksPill", new Vector2(1, 1), V(-48 - 300 - 24 - 110, -40 - 55), V(220, 110), assets.defaultBlock.sprite, "10", out var blocksText);
            Pill(r, "CoinsPill", new Vector2(1, 1), V(-48 - 110, -40 - 110 - 20 - 45), V(220, 90), Load(Icons + "hud_coin.png"), "0", out var coinsText);
            coinsText.fontSize = 52;
            var coinIcon = (RectTransform)coinsText.transform.parent.Find("Icon");

            var floating = Text(r, "BlocksGained", "+3", 64, Gold, V(-48 - 300 - 24 - 110, -150), V(240, 90), outline: true);
            Anchor(floating.rectTransform, new Vector2(1, 1), V(-48 - 300 - 24 - 110, -150), V(240, 90));
            floating.alpha = 0f;

            // "Tap to go" after a revive
            var prompt = Rect(r, "TapPrompt");
            Anchor(prompt, V(0.5f, 0.5f), V(0, 120), V(900, 300));
            var tapIcon = Rect(prompt, "Icon");
            Anchor(tapIcon, V(0.5f, 0.5f), V(0, 70), V(120, 120));
            Img(tapIcon, Icons + "icon_tap.png", Color.white, raycast: false);
            Text(prompt, "Label", "TAP TO GO!", 110, Color.white, V(0, -70), V(900, 140), outline: true);
            prompt.gameObject.SetActive(false);

            Set(screen, "pauseButton", pause);
            Set(screen, "blocksPill", blocks);
            Set(screen, "blocksPillBackground", blocks.GetComponent<Image>());
            Set(screen, "blocksText", blocksText);
            Set(screen, "coinsText", coinsText);
            Set(screen, "distanceText", distanceText);
            Set(screen, "coinIcon", coinIcon);
            Set(screen, "tapPrompt", prompt.gameObject);
            Set(screen, "floatingText", floating);
            Set(screen, "blockIcon", blocks.Find("Icon").GetComponent<Image>());
            return screen;
        }

        // ================================================================ Main menu

        static MainMenuScreen BuildMainMenu(RectTransform root, GameAssets assets, RunController run, UIRouter router)
        {
            var screen = Screen<MainMenuScreen>(root, "MainMenu", dim: false);
            var r = (RectTransform)screen.transform;

            // Logo: "ONE" over "BL▢CK" with the yellow block standing in for the O
            var logo = Rect(r, "Logo");
            Anchor(logo, V(0.5f, 0.5f), V(MenuX, 300), V(1000, 380));
            Text(logo, "One", "ONE", 200, Color.white, V(0, 92), V(900, 200), outline: true);
            var bl = Text(logo, "BL", "BL", 200, Color.white, V(-265, -92), V(360, 200), outline: true);
            bl.alignment = TextAlignmentOptions.Right;
            var ck = Text(logo, "CK", "CK", 200, Color.white, V(265, -92), V(360, 200), outline: true);
            ck.alignment = TextAlignmentOptions.Left;
            var block = Rect(logo, "Block");
            Anchor(block, V(0.5f, 0.5f), V(0, -84), V(150, 150));
            Img(block, assets.defaultBlock.sprite, Color.white, raycast: false);

            Text(r, "Tagline", "TAP TO BUILD YOUR WAY FORWARD", 42, Color.white, V(MenuX, 88), V(1200, 60), outline: true).characterSpacing = 8;

            // Play
            var playHolder = Rect(r, "PlayHolder");
            Anchor(playHolder, V(0.5f, 0.5f), V(MenuX, -60), V(480, 170));
            var play = TextButton(playHolder, "PlayButton", Ui + "button_green.png", V(0, 0), V(480, 170), "PLAY", 100, Icons + "icon_play.png");

            // Mode selector
            string[] labels = { "ENDLESS", "DAILY", "ONE BLOCK" };
            string[] modeIcons = { "icon_infinity", "icon_calendar", "icon_target" };
            var modeButtons = new Button[3];
            var modeBgs = new Image[3];
            var modeLabels = new TMP_Text[3];
            for (int i = 0; i < 3; i++)
            {
                var b = Rect(r, $"Mode_{labels[i]}");
                Anchor(b, V(0.5f, 0.5f), V(MenuX - 350 + i * 350, -225), V(320, 96));
                var bg = Img(b, Ui + "pill_white.png", Ink, raycast: true);
                bg.type = Image.Type.Sliced;
                var btn = b.gameObject.AddComponent<Button>();
                btn.targetGraphic = bg;
                btn.transition = Selectable.Transition.None;
                b.gameObject.AddComponent<ButtonJuice>();
                var ic = Rect(b, "Icon");
                Anchor(ic, V(0, 0.5f), V(58, 0), V(54, 54));
                Img(ic, Icons + modeIcons[i] + ".png", Color.white, raycast: false);
                var label = Text(b, "Label", labels[i], 44, Color.white, V(28, 2), V(240, 90));
                modeButtons[i] = btn;
                modeBgs[i] = bg;
                modeLabels[i] = label;
                // Icon follows the label colour so the selected (yellow) pill stays readable
                var tint = ic.GetComponent<Image>();
                tint.color = Color.white;
            }
            // Daily badge: "+coins" while today's bonus is unclaimed, then the streak with a flame
            var dailyBadge = Rect((RectTransform)modeButtons[1].transform, "DailyBadge");
            Anchor(dailyBadge, V(1, 1), V(-30, -4), V(130, 56));
            var badgeBg = Img(dailyBadge, Ui + "pill_white.png", Coral, raycast: false);
            badgeBg.type = Image.Type.Sliced;
            badgeBg.pixelsPerUnitMultiplier = 1.8f;
            var badgeIcon = Rect(dailyBadge, "Icon");
            Anchor(badgeIcon, V(0, 0.5f), V(32, 2), V(50, 50));
            var badgeIconImg = Img(badgeIcon, Icons + "hud_coin.png", Color.white, raycast: false);
            var badgeText = Text(dailyBadge, "Text", "+50", 34, Color.white, V(18, 2), V(80, 52), outline: true);
            badgeText.enableAutoSizing = true;
            badgeText.fontSizeMin = 20;
            badgeText.fontSizeMax = 34;

            var desc = Text(r, "ModeDescription", "Classic mode.\nGo as far as you can!", 38, Color.white, V(MenuX, -335), V(1300, 110), outline: true);
            desc.fontStyle = FontStyles.Normal;

            // Top-right wallet and best
            Pill(r, "CoinsPill", V(1, 1), V(-48 - 130, -40 - 50), V(260, 100), Load(Icons + "hud_coin.png"), "0", out var coins);
            Pill(r, "BestPill", V(1, 1), V(-48 - 220, -40 - 100 - 20 - 45), V(440, 90), Load(Icons + "icon_trophy.png"), "BEST —", out var best);
            best.fontSize = 44;

            // Top-left: current world, opens the Worlds tab
            var worldPill = Pill(r, "WorldButton", V(0, 1), V(48 + 190, -40 - 50), V(380, 100), Load(Icons + "icon_world.png"), "GRASSLAND", out var worldText);
            worldText.fontSize = 46;
            worldPill.GetComponent<Image>().raycastTarget = true;
            var worldButton = worldPill.gameObject.AddComponent<Button>();
            worldButton.targetGraphic = worldPill.GetComponent<Image>();
            worldButton.transition = Selectable.Transition.None;
            worldPill.gameObject.AddComponent<ButtonJuice>();

            // Bottom-right: settings, stats, shop
            var settingsBtn = IconButton(r, "SettingsButton", Icons + "icon_settings.png", V(1, 0), V(-48 - 65, 40 + 65), 130);
            var statsBtn = IconButton(r, "StatsButton", Icons + "icon_stats.png", V(1, 0), V(-48 - 130 - 24 - 65, 40 + 65), 130);
            var shopBtn = IconButton(r, "ShopButton", Icons + "icon_shop.png", V(1, 0), V(-48 - 2 * (130 + 24) - 65, 40 + 65), 130);
            var missionsBtn = IconButton(r, "MissionsButton", Icons + "icon_missions.png", V(1, 0), V(-48 - 3 * (130 + 24) - 65, 40 + 65), 130);
            var missionsBadge = Rect((RectTransform)missionsBtn.transform, "Badge");
            Anchor(missionsBadge, V(1, 1), V(-8, -8), V(54, 54));
            Img(missionsBadge, Ui + "circle.png", new Color(0.9f, 0.28f, 0.3f), raycast: false);
            var missionsBadgeText = Text(missionsBadge, "Count", "1", 36, Color.white, V(0, 2), V(54, 54), outline: true);

            // Player level under the world pill; opens the missions screen
            var levelPill = Pill(r, "LevelButton", V(0, 1), V(48 + 150, -40 - 100 - 20 - 45), V(300, 90), Load(Icons + "hud_star.png"), "LV 1", out var levelText);
            levelText.fontSize = 44;
            var levelTextRt = levelText.rectTransform;
            levelTextRt.offsetMin = new Vector2(levelTextRt.offsetMin.x, 14);
            var levelBar = Rect(levelPill, "Bar");
            levelBar.anchorMin = levelBar.anchorMax = V(0, 0);
            levelBar.pivot = V(0, 0.5f);
            levelBar.anchoredPosition = V(96, 20);
            levelBar.sizeDelta = V(170, 12);
            var levelBarBg = Img(levelBar, Ui + "pill_white.png", new Color(0, 0, 0, 0.45f), raycast: false);
            levelBarBg.type = Image.Type.Sliced;
            levelBarBg.pixelsPerUnitMultiplier = 8f;
            var levelFill = Rect(levelBar, "Fill");
            levelFill.anchorMin = levelFill.anchorMax = V(0, 0.5f);
            levelFill.pivot = V(0, 0.5f);
            levelFill.anchoredPosition = Vector2.zero;
            levelFill.sizeDelta = V(60, 12);
            var levelFillImg = Img(levelFill, Ui + "pill_white.png", Gold, raycast: false);
            levelFillImg.type = Image.Type.Sliced;
            levelFillImg.pixelsPerUnitMultiplier = 8f;
            levelPill.GetComponent<Image>().raycastTarget = true;
            var levelButton = levelPill.gameObject.AddComponent<Button>();
            levelButton.targetGraphic = levelPill.GetComponent<Image>();
            levelButton.transition = Selectable.Transition.None;
            levelPill.gameObject.AddComponent<ButtonJuice>();

            Set(screen, "run", run);
            Set(screen, "router", router);
            Set(screen, "logo", logo);
            Set(screen, "logoBlock", block);
            Set(screen, "playButton", play);
            SetArray(screen, "modeButtons", modeButtons);
            SetArray(screen, "modeBackgrounds", modeBgs);
            SetArray(screen, "modeLabels", modeLabels);
            Set(screen, "modeDescription", desc);
            Set(screen, "dailyBadge", dailyBadge.gameObject);
            Set(screen, "dailyBadgeBackground", badgeBg);
            Set(screen, "dailyBadgeIcon", badgeIconImg);
            Set(screen, "dailyBadgeText", badgeText);
            Set(screen, "coinSprite", Load(Icons + "hud_coin.png"));
            Set(screen, "flameSprite", Load(Icons + "icon_flame.png"));
            Set(screen, "coinsText", coins);
            Set(screen, "bestText", best);
            Set(screen, "settingsButton", settingsBtn);
            Set(screen, "statsButton", statsBtn);
            Set(screen, "shopButton", shopBtn);
            Set(screen, "missionsButton", missionsBtn);
            Set(screen, "missionsBadge", missionsBadge.gameObject);
            Set(screen, "missionsBadgeText", missionsBadgeText);
            Set(screen, "levelText", levelText);
            Set(screen, "levelFill", levelFill);
            Set(screen, "levelButton", levelButton);
            Set(screen, "worldButton", worldButton);
            Set(screen, "worldText", worldText);
            return screen;
        }

        // ================================================================ Pause

        static PauseScreen BuildPause(RectTransform root, RunController run)
        {
            var screen = Screen<PauseScreen>(root, "Pause", dim: true);
            var panel = Panel(screen.transform, V(760, 780));

            Text(panel, "Title", "PAUSED", 110, Color.white, V(0, 290), V(700, 140), outline: true);
            var resume = TextButton(panel, "Resume", Ui + "button_green.png", V(0, 140), V(520, 130), "RESUME", 64, Icons + "icon_play.png");
            var restart = TextButton(panel, "Restart", Ui + "button_yellow.png", V(0, -10), V(520, 130), "RESTART", 64, Icons + "icon_retry.png");
            var home = TextButton(panel, "Home", Ui + "button_dark.png", V(0, -160), V(520, 130), "HOME", 64, Icons + "icon_home.png");

            var music = Toggle(panel, "Music", SettingToggle.Setting.Music, Icons + "icon_music.png", null, V(-150, -300));
            var sound = Toggle(panel, "Sound", SettingToggle.Setting.Sound, Icons + "icon_sound_on.png", Icons + "icon_sound_off.png", V(0, -300));
            var haptics = Toggle(panel, "Haptics", SettingToggle.Setting.Haptics, Icons + "icon_vibrate.png", null, V(150, -300));

            Set(screen, "panel", panel);
            Set(screen, "run", run);
            Set(screen, "resumeButton", resume);
            Set(screen, "restartButton", restart);
            Set(screen, "homeButton", home);
            Set(screen, "musicToggle", music);
            Set(screen, "soundToggle", sound);
            Set(screen, "hapticsToggle", haptics);
            return screen;
        }

        static SettingToggle Toggle(RectTransform parent, string name, SettingToggle.Setting setting, string onIcon, string offIcon, Vector2 pos)
        {
            var b = IconButton(parent, name + "Toggle", onIcon, V(0.5f, 0.5f), pos, 120);
            var t = b.gameObject.AddComponent<SettingToggle>();
            var icon = b.transform.Find("Icon").GetComponent<Image>();
            var so = new SerializedObject(t);
            so.FindProperty("setting").enumValueIndex = (int)setting;
            so.FindProperty("button").objectReferenceValue = b;
            so.FindProperty("icon").objectReferenceValue = icon;
            if (offIcon != null)
            {
                so.FindProperty("onSprite").objectReferenceValue = Load(onIcon);
                so.FindProperty("offSprite").objectReferenceValue = Load(offIcon);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return t;
        }

        // ================================================================ Continue

        static ContinueScreen BuildContinue(RectTransform root, RunController run)
        {
            var screen = Screen<ContinueScreen>(root, "Continue", dim: true);
            var panel = Panel(screen.transform, V(900, 800));

            var title = Text(panel, "Title", "GAME OVER", 100, Coral, V(0, 315), V(860, 130), outline: true);
            Text(panel, "DistanceLabel", "DISTANCE", 38, Soft, V(0, 235), V(600, 50));
            var distance = Text(panel, "Distance", "0 m", 90, Color.white, V(0, 170), V(700, 110), outline: true);

            var ringRect = Rect(panel, "Countdown");
            Anchor(ringRect, V(0.5f, 0.5f), V(0, 25), V(170, 170));
            Img(ringRect, Ui + "ring.png", new Color(1, 1, 1, 0.18f), raycast: false);
            var fill = Rect(ringRect, "Fill");
            Stretch(fill);
            var fillImg = Img(fill, Ui + "ring.png", Gold, raycast: false);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Radial360;
            fillImg.fillOrigin = (int)Image.Origin360.Top;
            fillImg.fillClockwise = false;
            var count = Text(ringRect, "Number", "6", 90, Color.white, V(0, 4), V(170, 140), outline: true);

            var reward = Text(panel, "Reward", "Continue from where you fell with +3 blocks", 40, Soft, V(0, -100), V(820, 60));
            reward.fontStyle = FontStyles.Normal;
            var revive = TextButton(panel, "Revive", Ui + "button_green.png", V(0, -205), V(580, 140), "REVIVE", 70, Icons + "icon_ad.png");
            var giveUp = TextButton(panel, "GiveUp", Ui + "button_red.png", V(0, -335), V(360, 100), "GIVE UP", 48, null);

            Set(screen, "panel", panel);
            Set(screen, "run", run);
            Set(screen, "title", title);
            Set(screen, "distanceText", distance);
            Set(screen, "rewardText", reward);
            Set(screen, "countdownText", count);
            Set(screen, "countdownRing", fillImg);
            Set(screen, "reviveButton", revive);
            Set(screen, "reviveLabel", revive.transform.Find("Label").GetComponent<TMP_Text>());
            Set(screen, "giveUpButton", giveUp);
            return screen;
        }

        // ================================================================ Results

        static ResultsScreen BuildResults(RectTransform root, RunController run)
        {
            var screen = Screen<ResultsScreen>(root, "Results", dim: true);
            var panel = Panel(screen.transform, V(1040, 960));

            var badge = Rect(panel, "NewBestCrown");
            Anchor(badge, V(0.5f, 0.5f), V(0, 475), V(150, 150));
            Img(badge, Icons + "icon_crown.png", Gold, raycast: false);

            var title = Text(panel, "Title", "GAME OVER", 104, Coral, V(0, 375), V(980, 130), outline: true);
            var mode = Text(panel, "Mode", "ENDLESS RUN", 36, Soft, V(0, 297), V(800, 50));
            mode.characterSpacing = 6;
            var distance = Text(panel, "Distance", "0 m", 170, Color.white, V(0, 178), V(980, 200), outline: true);
            var best = Text(panel, "Best", "BEST  0 m", 46, Soft, V(0, 62), V(800, 60));
            var dailyLine = Text(panel, "Daily", "STREAK DAY 1  ·  +50 BONUS COINS", 40, Gold, V(0, 10), V(960, 56), outline: true);
            dailyLine.gameObject.SetActive(false);

            // XP bar: "LV 3 [=====     ] +23 XP"
            var xpRow = Rect(panel, "Xp");
            Anchor(xpRow, V(0.5f, 0.5f), V(0, -60), V(860, 60));
            var xpLevel = Text(xpRow, "Level", "LV 1", 44, Color.white, V(-360, 2), V(150, 60), outline: true);
            xpLevel.alignment = TextAlignmentOptions.Left;
            var xpBar = Rect(xpRow, "Bar");
            Anchor(xpBar, V(0.5f, 0.5f), V(-10, 0), V(470, 36));
            var xpBarBg = Img(xpBar, Ui + "pill_white.png", new Color(0, 0, 0, 0.45f), raycast: false);
            xpBarBg.type = Image.Type.Sliced;
            xpBarBg.pixelsPerUnitMultiplier = 2.6f;
            var xpFill = Rect(xpBar, "Fill");
            xpFill.anchorMin = xpFill.anchorMax = V(0, 0.5f);
            xpFill.pivot = V(0, 0.5f);
            xpFill.anchoredPosition = Vector2.zero;
            xpFill.sizeDelta = V(100, 36);
            var xpFillImg = Img(xpFill, Ui + "pill_white.png", new Color(0.55f, 0.45f, 1f), raycast: false);
            xpFillImg.type = Image.Type.Sliced;
            xpFillImg.pixelsPerUnitMultiplier = 2.6f;
            var xpText = Text(xpRow, "Gained", "+0 XP", 40, Color.white, V(350, 2), V(260, 60), outline: true);
            xpText.alignment = TextAlignmentOptions.Right;
            xpText.enableAutoSizing = true;
            xpText.fontSizeMin = 24;
            xpText.fontSizeMax = 40;

            var coinRow = Rect(panel, "Coins");
            Anchor(coinRow, V(0.5f, 0.5f), V(-190, -170), V(400, 110));
            var coinIcon = Rect(coinRow, "Icon");
            Anchor(coinIcon, V(0, 0.5f), V(55, 0), V(90, 90));
            Img(coinIcon, Icons + "hud_coin.png", Color.white, raycast: false);
            var coins = Text(coinRow, "Amount", "+0", 80, Gold, V(80, 2), V(300, 110), outline: true);
            coins.alignment = TextAlignmentOptions.Left;

            var doubleBtn = TextButton(panel, "DoubleCoins", Ui + "button_blue.png", V(230, -170), V(400, 116), "x2 COINS", 54, Icons + "icon_ad.png");

            var retry = TextButton(panel, "Retry", Ui + "button_green.png", V(-90, -345), V(600, 150), "PLAY AGAIN", 72, Icons + "icon_retry.png");
            var home = IconButton(panel, "Home", Icons + "icon_home.png", V(0.5f, 0.5f), V(330, -345), 150);

            Set(screen, "panel", panel);
            Set(screen, "run", run);
            Set(screen, "title", title);
            Set(screen, "modeText", mode);
            Set(screen, "distanceText", distance);
            Set(screen, "bestText", best);
            Set(screen, "dailyText", dailyLine);
            Set(screen, "levelText", xpLevel);
            Set(screen, "xpFill", xpFill);
            Set(screen, "xpText", xpText);
            Set(screen, "newBestBadge", badge.gameObject);
            Set(screen, "coinsText", coins);
            Set(screen, "doubleCoinsButton", doubleBtn);
            Set(screen, "retryButton", retry);
            Set(screen, "homeButton", home);
            return screen;
        }

        // ================================================================ Settings & stats

        static SettingsScreen BuildSettings(RectTransform root)
        {
            var screen = Screen<SettingsScreen>(root, "Settings", dim: true);
            var panel = Panel(screen.transform, V(820, 860));
            Text(panel, "Title", "SETTINGS", 96, Color.white, V(0, 335), V(700, 120), outline: true);
            var close = IconButton(panel, "Close", Icons + "icon_close.png", V(1, 1), V(-30, -30), 100);

            var toggles = new[]
            {
                SettingRow(panel, "Music", "MUSIC", SettingToggle.Setting.Music, Icons + "icon_music.png", 195),
                SettingRow(panel, "Sound", "SOUND", SettingToggle.Setting.Sound, Icons + "icon_sound_on.png", 70),
                SettingRow(panel, "Haptics", "VIBRATION", SettingToggle.Setting.Haptics, Icons + "icon_vibrate.png", -55)
            };

            // Soundtrack picker: < TRACK NAME >
            var pickerRow = Rect(panel, "MusicPicker");
            Anchor(pickerRow, V(0.5f, 0.5f), V(0, -195), V(680, 130));
            var pickerBg = Img(pickerRow, Ui + "pill_white.png", new Color(1, 1, 1, 0.07f), raycast: false);
            pickerBg.type = Image.Type.Sliced;
            var prevBtn = IconButton(pickerRow, "Prev", Icons + "icon_play.png", V(0, 0.5f), V(65, 0), 90);
            prevBtn.transform.Find("Icon").localScale = new Vector3(-1f, 1f, 1f);
            var nextBtn = IconButton(pickerRow, "Next", Icons + "icon_play.png", V(1, 0.5f), V(-65, 0), 90);
            var trackTitle = Text(pickerRow, "Title", "AUTO", 46, Gold, V(0, 16), V(460, 60), outline: true);
            trackTitle.enableAutoSizing = true;
            trackTitle.fontSizeMin = 26;
            trackTitle.fontSizeMax = 46;
            var trackSub = Text(pickerRow, "Subtitle", "Every world plays its own music", 28, Soft, V(0, -32), V(460, 40));
            trackSub.enableAutoSizing = true;
            trackSub.fontSizeMin = 18;
            trackSub.fontSizeMax = 28;
            var picker = pickerRow.gameObject.AddComponent<MusicPicker>();
            Set(picker, "previous", prevBtn);
            Set(picker, "next", nextBtn);
            Set(picker, "title", trackTitle);
            Set(picker, "subtitle", trackSub);

            var version = Text(panel, "Version", "One Block v0.1.0", 32, new Color(1, 1, 1, 0.4f), V(-150, -350), V(400, 50));
            // Consent "Privacy options" entry point (only visible where the consent SDK requires it)
            var privacy = TextButton(panel, "Privacy", Ui + "button_dark.png", V(190, -350), V(300, 70), "PRIVACY", 34, null);
            version.fontStyle = FontStyles.Normal;

            Set(screen, "panel", panel);
            Set(screen, "closeButton", close);
            SetArray(screen, "toggles", toggles);
            Set(screen, "versionText", version);
            Set(screen, "privacyButton", privacy);
            return screen;
        }

        static SettingToggle SettingRow(RectTransform panel, string name, string label, SettingToggle.Setting setting, string icon, float y)
        {
            var row = Rect(panel, name + "Row");
            Anchor(row, V(0.5f, 0.5f), V(0, y), V(680, 110));
            var bg = Img(row, Ui + "pill_white.png", new Color(1, 1, 1, 0.07f), raycast: true);
            bg.type = Image.Type.Sliced;
            var btn = row.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.transition = Selectable.Transition.None;
            row.gameObject.AddComponent<ButtonJuice>();

            var ic = Rect(row, "Icon");
            Anchor(ic, V(0, 0.5f), V(70, 0), V(70, 70));
            var iconImg = Img(ic, icon, Color.white, raycast: false);
            var text = Text(row, "Label", label, 56, Color.white, V(-20, 2), V(360, 100));
            text.alignment = TextAlignmentOptions.Left;
            text.rectTransform.anchoredPosition = V(20, 2);
            var state = Text(row, "State", "ON", 56, Color.white, V(250, 2), V(160, 100));

            var t = row.gameObject.AddComponent<SettingToggle>();
            var so = new SerializedObject(t);
            so.FindProperty("setting").enumValueIndex = (int)setting;
            so.FindProperty("button").objectReferenceValue = btn;
            so.FindProperty("icon").objectReferenceValue = iconImg;
            so.FindProperty("stateLabel").objectReferenceValue = state;
            so.ApplyModifiedPropertiesWithoutUndo();
            return t;
        }

        static StatsScreen BuildStats(RectTransform root)
        {
            var screen = Screen<StatsScreen>(root, "Stats", dim: true);
            var panel = Panel(screen.transform, V(860, 800));
            Text(panel, "Title", "STATS", 96, Color.white, V(0, 305), V(700, 120), outline: true);
            var close = IconButton(panel, "Close", Icons + "icon_close.png", V(1, 1), V(-30, -30), 100);

            TMP_Text StatRow(string name, string label, float y)
            {
                var row = Rect(panel, name);
                Anchor(row, V(0.5f, 0.5f), V(0, y), V(740, 100));
                var bg = Img(row, Ui + "pill_white.png", new Color(1, 1, 1, 0.07f), raycast: false);
                bg.type = Image.Type.Sliced;
                var l = Text(row, "Label", label, 46, Soft, V(-120, 2), V(440, 90));
                l.alignment = TextAlignmentOptions.Left;
                var v = Text(row, "Value", "0", 54, Color.white, V(185, 2), V(330, 90));
                v.alignment = TextAlignmentOptions.Right;
                v.richText = true;
                return v;
            }

            Set(screen, "panel", panel);
            Set(screen, "closeButton", close);
            Set(screen, "bestText", StatRow("Best", "ENDLESS BEST", 170));
            Set(screen, "challengeBestText", StatRow("ChallengeBest", "ONE BLOCK BEST", 55));
            Set(screen, "streakText", StatRow("Streak", "DAILY STREAK", -60));
            Set(screen, "runsText", StatRow("Runs", "RUNS PLAYED", -175));
            Set(screen, "earnedText", StatRow("Earned", "COINS EARNED", -290));
            return screen;
        }

        // ================================================================ Shop

        static ShopScreen BuildShop(RectTransform root, RunController run, UIRouter router)
        {
            var screen = Screen<ShopScreen>(root, "Shop", dim: true);
            var panel = Panel(screen.transform, V(1740, 940));
            var close = IconButton(panel, "Close", Icons + "icon_close.png", V(1, 1), V(-30, -30), 100);

            // Left column: title, equipped preview, wallet
            const float leftX = -640f;
            Text(panel, "Title", "SHOP", 110, Color.white, V(leftX, 370), V(400, 140), outline: true);
            var frame = Rect(panel, "PreviewFrame");
            Anchor(frame, V(0.5f, 0.5f), V(leftX, 40), V(380, 470));
            var frameImg = Img(frame, Ui + "panel_white.png", new Color(1, 1, 1, 0.06f), raycast: false);
            frameImg.type = Image.Type.Sliced;
            Text(frame, "Label", "EQUIPPED", 38, Soft, V(0, 190), V(360, 50)).characterSpacing = 6;
            var preview = Rect(frame, "Icon");
            Anchor(preview, V(0.5f, 0.5f), V(0, 10), V(300, 300));
            var previewImg = Img(preview, (Sprite)null, Color.white, raycast: false);
            previewImg.preserveAspect = true;
            var previewName = Text(frame, "Name", "Default", 56, Color.white, V(0, -190), V(360, 70), outline: true);
            previewName.enableAutoSizing = true;
            previewName.fontSizeMin = 30;
            previewName.fontSizeMax = 56;
            Pill(panel, "Coins", V(0.5f, 0.5f), V(leftX, -340), V(320, 100), Load(Icons + "hud_coin.png"), "0", out var coins);

            // Tabs
            string[] tabNames = { "CHARACTERS", "BLOCKS", "TRAILS", "EFFECTS", "WORLDS", "BUNDLES" };
            int tabCount = tabNames.Length;
            var tabs = new Button[tabCount];
            var tabBgs = new Image[tabCount];
            var tabLabels = new TMP_Text[tabCount];
            const float gridX = 234f;
            for (int i = 0; i < tabCount; i++)
            {
                var t = Rect(panel, $"Tab_{tabNames[i]}");
                Anchor(t, V(0.5f, 0.5f), V(gridX - 490 + i * 196, 360), V(188, 88));
                var bg = Img(t, Ui + "pill_white.png", Ink, raycast: true);
                bg.type = Image.Type.Sliced;
                var btn = t.gameObject.AddComponent<Button>();
                btn.targetGraphic = bg;
                btn.transition = Selectable.Transition.None;
                t.gameObject.AddComponent<ButtonJuice>();
                var label = Text(t, "Label", tabNames[i], 38, Color.white, V(0, 2), V(168, 76));
                label.enableAutoSizing = true;
                label.fontSizeMin = 22;
                label.fontSizeMax = 38;
                tabs[i] = btn;
                tabBgs[i] = bg;
                tabLabels[i] = label;
            }
            // Bundles tab gets the ad icon so it stands out
            {
                var bundleTab = (RectTransform)tabs[tabCount - 1].transform;
                var ic = Rect(bundleTab, "AdIcon");
                Anchor(ic, V(0, 1), V(24, -8), V(38, 38));
                Img(ic, Icons + "icon_ad.png", Color.white, raycast: false);
            }

            // Scrolling grid: 5 columns of item cards, or 3 big bundle cards
            var viewport = Rect(panel, "Viewport");
            Anchor(viewport, V(0.5f, 0.5f), V(gridX, -76), V(1190, 748));
            viewport.gameObject.AddComponent<RectMask2D>();
            var viewportImg = Img(viewport, Ui + "white.png", new Color(0, 0, 0, 0), raycast: true);   // catches drags between cards
            viewportImg.preserveAspect = false;
            var content = Rect(viewport, "Content");
            content.anchorMin = V(0, 1);
            content.anchorMax = V(1, 1);
            content.pivot = V(0.5f, 1);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = V(0, 620);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 40f;

            var cards = new ShopCard[15];
            for (int i = 0; i < cards.Length; i++)
            {
                int col = i % 5, row = i / 5;
                cards[i] = Card(content, $"Card_{i}", V(-464 + col * 232, -(10 + 140 + row * 300)), top: true);
            }
            var bundleCards = new BundleCard[3];
            for (int i = 0; i < bundleCards.Length; i++)
                bundleCards[i] = BundleCardView(content, $"Bundle_{i}", V(-386 + i * 386, -(10 + 340)));

            Set(screen, "panel", panel);
            Set(screen, "run", run);
            Set(screen, "router", router);
            Set(screen, "closeButton", close);
            SetArray(screen, "tabButtons", tabs);
            SetArray(screen, "tabBackgrounds", tabBgs);
            SetArray(screen, "tabLabels", tabLabels);
            Set(screen, "scroll", scroll);
            SetArray(screen, "cards", cards);
            SetArray(screen, "bundleCards", bundleCards);
            Set(screen, "coinsText", coins);
            Set(screen, "previewIcon", previewImg);
            Set(screen, "previewName", previewName);
            return screen;
        }

        static BundleCard BundleCardView(RectTransform parent, string name, Vector2 pos)
        {
            var r = Rect(parent, name);
            Anchor(r, V(0.5f, 1f), pos, V(370, 680));
            var bg = Img(r, Ui + "panel_white.png", new Color(0.19f, 0.24f, 0.32f), raycast: true);
            bg.type = Image.Type.Sliced;
            bg.pixelsPerUnitMultiplier = 1.4f;

            var thumb = Rect(r, "Thumbnail");
            Anchor(thumb, V(0.5f, 0.5f), V(0, 216), V(336, 210));
            var thumbImg = Img(thumb, (Sprite)null, Color.white, raycast: false);
            thumbImg.preserveAspect = true;

            var ribbon = Rect(r, "Ribbon");
            Anchor(ribbon, V(0.5f, 0.5f), V(-112, 308), V(130, 48));
            var ribbonBg = Img(ribbon, Ui + "pill_white.png", new Color(0.95f, 0.3f, 0.55f), raycast: false);
            ribbonBg.type = Image.Type.Sliced;
            Text(ribbon, "Label", "BUNDLE", 30, Color.white, V(0, 2), V(120, 44), outline: true);

            var nameText = Text(r, "Name", "Bundle", 54, Color.white, V(0, 76), V(340, 64), outline: true);
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = 30;
            nameText.fontSizeMax = 54;

            var icons = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                var slot = Rect(r, $"Content_{i}");
                Anchor(slot, V(0.5f, 0.5f), V(-123 + i * 82, 0), V(74, 74));
                Img(slot, Ui + "circle.png", new Color(0.08f, 0.1f, 0.14f, 0.7f), raycast: false);
                var ic = Rect(slot, "Icon");
                Anchor(ic, V(0.5f, 0.5f), V(0, 0), V(58, 58));
                icons[i] = Img(ic, (Sprite)null, Color.white, raycast: false);
                icons[i].preserveAspect = true;
            }

            var desc = Text(r, "Description", "", 26, Soft, V(0, -80), V(330, 80));
            desc.enableAutoSizing = true;
            desc.fontSizeMin = 18;
            desc.fontSizeMax = 26;

            var bar = Rect(r, "Progress");
            Anchor(bar, V(0.5f, 0.5f), V(0, -160), V(320, 44));
            var barBg = Img(bar, Ui + "pill_white.png", new Color(0.06f, 0.08f, 0.12f, 0.9f), raycast: false);
            barBg.type = Image.Type.Sliced;
            var fill = Rect(bar, "Fill");
            fill.anchorMin = fill.anchorMax = V(0, 0.5f);
            fill.pivot = V(0, 0.5f);
            fill.anchoredPosition = Vector2.zero;
            fill.sizeDelta = V(160, 44);
            var fillImg = Img(fill, Ui + "pill_white.png", new Color(1f, 0.76f, 0.23f), raycast: false);
            fillImg.type = Image.Type.Sliced;
            var progressText = Text(bar, "Text", "0 / 15 ADS", 30, Color.white, V(0, 2), V(300, 44), outline: true);

            var action = TextButton(r, "Action", Ui + "button_green.png", V(0, -262), V(320, 110), "WATCH AD", 50, Icons + "icon_ad.png");
            // Give the icon its own column so the label never runs into it
            ((RectTransform)action.transform.Find("Icon")).anchoredPosition = V(-112, 6);
            var actionLabelRt = (RectTransform)action.transform.Find("Label");
            actionLabelRt.anchoredPosition = V(30, 6);
            actionLabelRt.sizeDelta = V(230, 110);

            var card = r.gameObject.AddComponent<BundleCard>();
            Set(card, "thumbnail", thumbImg);
            Set(card, "nameText", nameText);
            Set(card, "descriptionText", desc);
            SetArray(card, "contentIcons", icons);
            Set(card, "progressFill", fill);
            Set(card, "progressText", progressText);
            Set(card, "actionButton", action);
            Set(card, "actionBackground", action.targetGraphic);
            Set(card, "actionIcon", action.transform.Find("Icon").gameObject);
            Set(card, "actionLabel", action.transform.Find("Label").GetComponent<TMP_Text>());
            Set(card, "watchSprite", Load(Ui + "button_green.png"));
            Set(card, "equipSprite", Load(Ui + "button_yellow.png"));
            Set(card, "equippedSprite", Load(Ui + "button_dark.png"));
            return card;
        }

        static ShopCard Card(RectTransform panel, string name, Vector2 pos, bool top = false)
        {
            var r = Rect(panel, name);
            Anchor(r, top ? V(0.5f, 1f) : V(0.5f, 0.5f), pos, V(214, 280));
            var bg = Img(r, Ui + "panel_white.png", new Color(0.19f, 0.24f, 0.32f), raycast: true);
            bg.type = Image.Type.Sliced;
            bg.pixelsPerUnitMultiplier = 1.4f;
            var btn = r.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.transition = Selectable.Transition.None;
            r.gameObject.AddComponent<ButtonJuice>();

            var icon = Rect(r, "Icon");
            Anchor(icon, V(0.5f, 0.5f), V(0, 42), V(160, 160));
            var iconImg = Img(icon, (Sprite)null, Color.white, raycast: false);
            iconImg.preserveAspect = true;

            var nameText = Text(r, "Name", "Item", 34, Color.white, V(0, -60), V(200, 44));
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = 20;
            nameText.fontSizeMax = 34;

            var price = Rect(r, "Price");
            Anchor(price, V(0.5f, 0.5f), V(0, -108), V(190, 52));
            var coin = Rect(price, "Coin");
            Anchor(coin, V(0.5f, 0.5f), V(-58, 0), V(44, 44));
            Img(coin, Icons + "hud_coin.png", Color.white, raycast: false);
            var priceText = Text(price, "Amount", "500", 44, Color.white, V(22, 2), V(140, 52), outline: true);
            priceText.alignment = TextAlignmentOptions.Left;
            priceText.rectTransform.anchoredPosition = V(40, 2);

            var state = Text(r, "State", "EQUIP", 36, Color.white, V(0, -108), V(200, 52), outline: true);

            var lockBadge = Rect(r, "Lock");
            Anchor(lockBadge, V(1, 1), V(-30, -30), V(48, 48));
            var lockBg = Img(lockBadge, Ui + "circle.png", new Color(0.08f, 0.1f, 0.14f, 0.9f), raycast: false);
            var lockIcon = Rect(lockBadge, "Icon");
            Anchor(lockIcon, V(0.5f, 0.5f), V(0, 1), V(30, 30));
            Img(lockIcon, Icons + "icon_lock.png", Color.white, raycast: false);

            // Replaces the price for items that only come from an ad bundle
            var adTag = Rect(r, "AdTag");
            Anchor(adTag, V(0.5f, 0.5f), V(0, -108), V(190, 52));
            var adBg = Img(adTag, Ui + "pill_white.png", new Color(0.95f, 0.3f, 0.55f), raycast: false);
            adBg.type = Image.Type.Sliced;
            var adIcon = Rect(adTag, "Icon");
            Anchor(adIcon, V(0.5f, 0.5f), V(-66, 1), V(40, 40));
            Img(adIcon, Icons + "icon_ad.png", Color.white, raycast: false);
            var adLabel = Text(adTag, "Label", "BUNDLE", 32, Color.white, V(20, 2), V(130, 48), outline: true);
            adLabel.enableAutoSizing = true;
            adLabel.fontSizeMin = 20;
            adLabel.fontSizeMax = 32;
            adTag.gameObject.SetActive(false);

            var card = r.gameObject.AddComponent<ShopCard>();
            Set(card, "button", btn);
            Set(card, "background", bg);
            Set(card, "icon", iconImg);
            Set(card, "nameText", nameText);
            Set(card, "priceRow", price.gameObject);
            Set(card, "priceText", priceText);
            Set(card, "stateText", state);
            Set(card, "lockBadge", lockBadge.gameObject);
            Set(card, "adTag", adTag.gameObject);
            return card;
        }

        // ================================================================ Missions

        static MissionsScreen BuildMissions(RectTransform root, RunController run, UIRouter router, GameAssets assets)
        {
            var screen = Screen<MissionsScreen>(root, "Missions", dim: true);
            var panel = Panel(screen.transform, V(1360, 900));
            var close = IconButton(panel, "Close", Icons + "icon_close.png", V(1, 1), V(-30, -30), 100);
            Text(panel, "Title", "MISSIONS", 96, Color.white, V(0, 360), V(900, 120), outline: true);

            // Level + XP bar
            var levelRow = Rect(panel, "Level");
            Anchor(levelRow, V(0.5f, 0.5f), V(0, 250), V(1160, 80));
            var star = Rect(levelRow, "Star");
            Anchor(star, V(0.5f, 0.5f), V(-540, 0), V(76, 76));
            Img(star, Icons + "hud_star.png", Color.white, raycast: false);
            var levelText = Text(levelRow, "Text", "LEVEL 1", 56, Gold, V(-370, 2), V(260, 80), outline: true);
            levelText.alignment = TextAlignmentOptions.Left;
            var bar = Rect(levelRow, "Bar");
            Anchor(bar, V(0.5f, 0.5f), V(190, 0), V(760, 46));
            var barBg = Img(bar, Ui + "pill_white.png", new Color(0, 0, 0, 0.45f), raycast: false);
            barBg.type = Image.Type.Sliced;
            barBg.pixelsPerUnitMultiplier = 2f;
            var fill = Rect(bar, "Fill");
            fill.anchorMin = fill.anchorMax = V(0, 0.5f);
            fill.pivot = V(0, 0.5f);
            fill.anchoredPosition = Vector2.zero;
            fill.sizeDelta = V(200, 46);
            var fillImg = Img(fill, Ui + "pill_white.png", new Color(0.55f, 0.45f, 1f), raycast: false);
            fillImg.type = Image.Type.Sliced;
            fillImg.pixelsPerUnitMultiplier = 2f;
            var xpText = Text(bar, "Text", "0 / 100 XP", 34, Color.white, V(0, 2), V(740, 46), outline: true);

            int slots = OneBlock.Save.Missions.Slots;
            var rowBgs = new Image[slots];
            var rowIcons = new Image[slots];
            var rowTexts = new TMP_Text[slots];
            var rowFills = new RectTransform[slots];
            var rowProgress = new TMP_Text[slots];
            var rowCoins = new TMP_Text[slots];
            var rowXp = new TMP_Text[slots];
            var claims = new Button[slots];
            var rerolls = new Button[slots];
            for (int i = 0; i < slots; i++)
            {
                var row = Rect(panel, $"Mission_{i}");
                Anchor(row, V(0.5f, 0.5f), V(0, 105 - i * 185), V(1240, 165));
                var bg = Img(row, Ui + "pill_white.png", new Color(1, 1, 1, 0.07f), raycast: false);
                bg.type = Image.Type.Sliced;
                bg.pixelsPerUnitMultiplier = 1.2f;
                rowBgs[i] = bg;

                var iconBg = Rect(row, "IconBg");
                Anchor(iconBg, V(0.5f, 0.5f), V(-530, 0), V(120, 120));
                Img(iconBg, Ui + "circle.png", new Color(0.06f, 0.08f, 0.12f, 0.6f), raycast: false);
                var icon = Rect(iconBg, "Icon");
                Anchor(icon, V(0.5f, 0.5f), V(0, 0), V(84, 84));
                rowIcons[i] = Img(icon, (Sprite)null, Color.white, raycast: false);
                rowIcons[i].preserveAspect = true;

                var desc = Text(row, "Text", "Mission", 46, Color.white, V(-150, 32), V(620, 60));
                desc.alignment = TextAlignmentOptions.Left;
                desc.enableAutoSizing = true;
                desc.fontSizeMin = 28;
                desc.fontSizeMax = 46;
                rowTexts[i] = desc;

                var pbar = Rect(row, "Bar");
                Anchor(pbar, V(0.5f, 0.5f), V(-150, -36), V(620, 38));
                var pbg = Img(pbar, Ui + "pill_white.png", new Color(0, 0, 0, 0.45f), raycast: false);
                pbg.type = Image.Type.Sliced;
                pbg.pixelsPerUnitMultiplier = 2.4f;
                var pfill = Rect(pbar, "Fill");
                pfill.anchorMin = pfill.anchorMax = V(0, 0.5f);
                pfill.pivot = V(0, 0.5f);
                pfill.anchoredPosition = Vector2.zero;
                pfill.sizeDelta = V(100, 38);
                var pfillImg = Img(pfill, Ui + "pill_white.png", Gold, raycast: false);
                pfillImg.type = Image.Type.Sliced;
                pfillImg.pixelsPerUnitMultiplier = 2.4f;
                rowFills[i] = pfill;
                rowProgress[i] = Text(pbar, "Text", "0 / 5", 30, Color.white, V(0, 2), V(600, 38), outline: true);

                var coin = Rect(row, "Coin");
                Anchor(coin, V(0.5f, 0.5f), V(230, 30), V(52, 52));
                Img(coin, Icons + "hud_coin.png", Color.white, raycast: false);
                var coinsText = Text(row, "Reward", "+50", 44, Gold, V(320, 32), V(130, 56), outline: true);
                coinsText.alignment = TextAlignmentOptions.Left;
                rowCoins[i] = coinsText;
                var xp = Text(row, "Xp", "+20 XP", 32, new Color(0.75f, 0.68f, 1f), V(290, -34), V(180, 44), outline: true);
                xp.alignment = TextAlignmentOptions.Left;
                rowXp[i] = xp;

                claims[i] = TextButton(row, "Claim", Ui + "button_green.png", V(495, 0), V(210, 110), "CLAIM", 50, null);
                rerolls[i] = TextButton(row, "Reroll", Ui + "button_dark.png", V(495, 0), V(210, 110), "NEW", 44, Icons + "icon_ad.png");
                ((RectTransform)rerolls[i].transform.Find("Icon")).anchoredPosition = V(-55, 6);
                var rerollLabel = (RectTransform)rerolls[i].transform.Find("Label");
                rerollLabel.anchoredPosition = V(28, 6);
                rerollLabel.sizeDelta = V(120, 110);
            }

            var hint = Text(panel, "Hint", "Complete missions to earn coins and XP. Watch an ad to swap one you don't like.", 32, Soft, V(0, -395), V(1200, 50));
            hint.enableAutoSizing = true;
            hint.fontSizeMin = 22;
            hint.fontSizeMax = 32;

            // Icons per MissionType (enum order)
            string world = "Assets/_Game/Art/Generated/Worlds/grassland/";
            var typeIcons = new Object[]
            {
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Generated/Props/flag.png"),   // RunDistance
                Load(Icons + "hud_coin.png"),                                                          // RunCoins
                Load(Icons + "hud_coin.png"),                                                          // TotalCoins
                AssetDatabase.LoadAssetAtPath<Sprite>(world + "enemy_0.png"),                          // Squash
                assets.defaultBlock.sprite,                                                            // Bridge
                AssetDatabase.LoadAssetAtPath<Sprite>(world + "hazard_0.png"),                         // Cover
                assets.blockPack,                                                                      // Packs
                assets.defaultBlock.sprite,                                                            // BlocksPlaced
                Load(Icons + "icon_play.png"),                                                         // Runs
                Load(Icons + "icon_target.png"),                                                       // ChallengeDistance
                Load(Icons + "icon_calendar.png"),                                                     // PlayDaily
            };

            Set(screen, "panel", panel);
            Set(screen, "run", run);
            Set(screen, "router", router);
            Set(screen, "closeButton", close);
            Set(screen, "levelText", levelText);
            Set(screen, "xpFill", fill);
            Set(screen, "xpText", xpText);
            SetArray(screen, "rowBackgrounds", rowBgs);
            SetArray(screen, "rowIcons", rowIcons);
            SetArray(screen, "rowTexts", rowTexts);
            SetArray(screen, "rowFills", rowFills);
            SetArray(screen, "rowProgress", rowProgress);
            SetArray(screen, "rowCoins", rowCoins);
            SetArray(screen, "rowXp", rowXp);
            SetArray(screen, "claimButtons", claims);
            SetArray(screen, "rerollButtons", rerolls);
            SetArray(screen, "typeIcons", typeIcons);
            return screen;
        }

        static void BuildMissionBanner(RectTransform hud)
        {
            var root = Rect(hud, "MissionBanner");
            Stretch(root);
            var b = Rect(root, "Banner");
            Anchor(b, V(0.5f, 1f), V(0, -105), V(700, 120));
            var bg = Img(b, Ui + "pill_white.png", new Color(0.2f, 0.55f, 0.28f, 0.96f), raycast: false);
            bg.type = Image.Type.Sliced;
            var group = b.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            var check = Rect(b, "Check");
            Anchor(check, V(0, 0.5f), V(70, 0), V(70, 70));
            Img(check, Icons + "icon_check.png", Color.white, raycast: false);
            var title = Text(b, "Title", "MISSION COMPLETE!", 44, Gold, V(50, 24), V(540, 56), outline: true);
            title.alignment = TextAlignmentOptions.Left;
            title.enableAutoSizing = true;
            title.fontSizeMin = 28;
            title.fontSizeMax = 44;
            var desc = Text(b, "Text", "", 34, Color.white, V(50, -26), V(540, 46));
            desc.alignment = TextAlignmentOptions.Left;
            desc.enableAutoSizing = true;
            desc.fontSizeMin = 24;
            desc.fontSizeMax = 36;

            var banner = root.gameObject.AddComponent<MissionBanner>();
            Set(banner, "banner", b);
            Set(banner, "group", group);
            Set(banner, "description", desc);
        }

        // ================================================================ FTUE & intro

        static void BuildTutorialOverlay(RectTransform hud)
        {
            var r = Rect(hud, "TutorialOverlay");
            Stretch(r);
            r.SetAsFirstSibling();   // under the HUD pills and pause button

            var dimRect = Rect(r, "Dim");
            Stretch(dimRect);
            dimRect.offsetMin = V(-400, -400);
            dimRect.offsetMax = V(400, 400);
            Img(dimRect, Ui + "white.png", new Color(0.03f, 0.05f, 0.09f, 1f), raycast: false);
            var dim = dimRect.gameObject.AddComponent<CanvasGroup>();
            dim.blocksRaycasts = false;
            dim.interactable = false;

            var ring = Rect(r, "Ring");
            Anchor(ring, V(0.5f, 0.5f), V(0, 0), V(190, 190));
            Img(ring, Ui + "ring.png", Gold, raycast: false);

            var hand = Rect(r, "Hand");
            Anchor(hand, V(0.5f, 0.5f), V(0, 0), V(130, 130));
            var handShadow = Rect(hand, "Shadow");
            Anchor(handShadow, V(0.5f, 0.5f), V(4, -6), V(150, 150));
            Img(handShadow, Icons + "icon_tap.png", new Color(0, 0, 0, 0.55f), raycast: false);
            var handFill = Rect(hand, "Fill");
            Stretch(handFill);
            Img(handFill, Icons + "icon_tap.png", Color.white, raycast: false);

            // Instruction card, top centre
            var cardRect = Rect(r, "Card");
            Anchor(cardRect, V(0.5f, 0.5f), V(0, 250), V(1180, 210));
            var cardBg = Img(cardRect, Ui + "pill_white.png", Ink, raycast: false);
            cardBg.type = Image.Type.Sliced;
            var card = cardRect.gameObject.AddComponent<CanvasGroup>();
            card.blocksRaycasts = false;
            var cardTitle = Text(cardRect, "Title", "TAP TO PLACE A BLOCK", 72, Gold, V(0, 38), V(1100, 90), outline: true);
            var cardBody = Text(cardRect, "Body", "Bridge the gap so your hero can cross!", 44, Color.white, V(0, -44), V(1100, 70));
            cardBody.enableAutoSizing = true;
            cardBody.fontSizeMin = 28;
            cardBody.fontSizeMax = 44;

            // Non-blocking banner for the intro line and the closing tip
            var bannerRect = Rect(r, "Banner");
            Anchor(bannerRect, V(0.5f, 0.5f), V(0, 260), V(1180, 190));
            var bannerBg = Img(bannerRect, Ui + "pill_white.png", new Color(0.11f, 0.15f, 0.2f, 0.85f), raycast: false);
            bannerBg.type = Image.Type.Sliced;
            var banner = bannerRect.gameObject.AddComponent<CanvasGroup>();
            banner.blocksRaycasts = false;
            var bannerTitle = Text(bannerRect, "Title", "LET'S GO!", 64, Color.white, V(0, 34), V(1100, 80), outline: true);
            var bannerBody = Text(bannerRect, "Body", "", 40, Soft, V(0, -40), V(1100, 64));
            bannerBody.enableAutoSizing = true;
            bannerBody.fontSizeMin = 26;
            bannerBody.fontSizeMax = 40;

            var praise = Text(r, "Praise", "NICE!", 110, Gold, V(0, 0), V(600, 140), outline: true);

            var overlay = r.gameObject.AddComponent<TutorialOverlay>();
            Set(overlay, "dim", dim);
            Set(overlay, "ring", ring);
            Set(overlay, "hand", hand);
            Set(overlay, "card", card);
            Set(overlay, "cardTitle", cardTitle);
            Set(overlay, "cardBody", cardBody);
            Set(overlay, "banner", banner);
            Set(overlay, "bannerTitle", bannerTitle);
            Set(overlay, "bannerBody", bannerBody);
            Set(overlay, "praise", praise);
        }

        static void BuildIntro(RectTransform safe, GameAssets assets)
        {
            var r = Rect(safe, "Intro");
            Stretch(r);
            var group = r.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = true;   // swallow taps (they skip the intro) until it's gone

            // Sky backdrop that bleeds past the safe area
            var bgRect = Rect(r, "Background");
            Stretch(bgRect);
            bgRect.offsetMin = V(-400, -400);
            bgRect.offsetMax = V(400, 400);
            var bg = Img(bgRect, assets.defaultWorld.sky, Color.white, raycast: true);
            bg.preserveAspect = false;
            var bgGroup = bgRect.gameObject.AddComponent<CanvasGroup>();
            var clouds = new RectTransform[3];
            var cloudSpots = new[] { V(-620, 330), V(560, 420), V(380, -260) };
            for (int i = 0; i < clouds.Length; i++)
            {
                clouds[i] = Rect(bgRect, $"Cloud_{i}");
                Anchor(clouds[i], V(0.5f, 0.5f), cloudSpots[i], i == 2 ? V(300, 150) : V(420, 210));
                var sprite = assets.defaultWorld.clouds != null && assets.defaultWorld.clouds.Length > 0
                    ? assets.defaultWorld.clouds[i % assets.defaultWorld.clouds.Length] : null;
                Img(clouds[i], sprite, new Color(1f, 1f, 1f, i == 2 ? 0.7f : 0.95f), raycast: false);
            }

            // Logo, laid out exactly like the menu logo so the hand-off is seamless
            var logo = Rect(r, "Logo");
            Anchor(logo, V(0.5f, 0.5f), V(MenuX, 300), V(1000, 380));
            var logoGroup = logo.gameObject.AddComponent<CanvasGroup>();
            logoGroup.blocksRaycasts = false;

            var shadow = Rect(logo, "Shadow");
            Anchor(shadow, V(0.5f, 0.5f), V(0, -170), V(170, 34));
            Img(shadow, Ui + "circle.png", new Color(0, 0, 0, 0.25f), raycast: false).preserveAspect = false;

            var dust = new Image[4];
            for (int i = 0; i < dust.Length; i++)
            {
                var d = Rect(logo, $"Dust_{i}");
                Anchor(d, V(0.5f, 0.5f), V(0, -150), V(56 - i * 6, 56 - i * 6));
                dust[i] = Img(d, Ui + "circle.png", new Color(1f, 1f, 1f, 0.9f), raycast: false);
            }

            var one = Text(logo, "One", "ONE", 200, Color.white, V(0, 92), V(900, 200), outline: true);
            var bl = Text(logo, "BL", "BL", 200, Color.white, V(-265, -92), V(360, 200), outline: true);
            bl.alignment = TextAlignmentOptions.Right;
            var ck = Text(logo, "CK", "CK", 200, Color.white, V(265, -92), V(360, 200), outline: true);
            ck.alignment = TextAlignmentOptions.Left;
            var block = Rect(logo, "Block");
            Anchor(block, V(0.5f, 0.5f), V(0, -84), V(150, 150));
            Img(block, assets.defaultBlock.sprite, Color.white, raycast: false);

            var intro = r.gameObject.AddComponent<IntroScreen>();
            Set(intro, "group", group);
            Set(intro, "background", bgGroup);
            Set(intro, "logoGroup", logoGroup);
            Set(intro, "block", block);
            Set(intro, "one", one.rectTransform);
            Set(intro, "bl", bl.rectTransform);
            Set(intro, "ck", ck.rectTransform);
            Set(intro, "shadow", shadow);
            SetArray(intro, "dust", dust);
            SetArray(intro, "clouds", clouds);
        }

        static (CanvasGroup, TMP_Text) BuildToast(RectTransform root)
        {
            var r = Rect(root, "Toast");
            Anchor(r, V(0.5f, 0f), V(0, 170), V(1000, 100));
            var bg = Img(r, Ui + "pill_white.png", Ink, raycast: false);
            bg.type = Image.Type.Sliced;
            var group = r.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            var text = Text(r, "Text", "", 44, Color.white, V(0, 2), V(940, 90));
            return (group, text);
        }

        // ================================================================ building blocks

        static T Screen<T>(RectTransform root, string name, bool dim) where T : UIScreen
        {
            var r = Rect(root, name);
            Stretch(r);
            r.gameObject.AddComponent<CanvasGroup>();
            if (dim) Img(r, Ui + "white.png", Dim, raycast: true);   // also swallows taps meant for the game
            var screen = r.gameObject.AddComponent<T>();
            r.gameObject.SetActive(false);
            return screen;
        }

        static RectTransform Panel(Transform parent, Vector2 size)
        {
            var r = Rect(parent, "Panel");
            Anchor(r, V(0.5f, 0.5f), V(0, 0), size);
            var img = Img(r, Ui + "panel_white.png", new Color(0.12f, 0.16f, 0.22f, 0.985f), raycast: true);
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 0.6f;
            return r;
        }

        static Button TextButton(RectTransform parent, string name, string sprite, Vector2 pos, Vector2 size, string label, float fontSize, string icon)
        {
            var r = Rect(parent, name);
            Anchor(r, V(0.5f, 0.5f), pos, size);
            var img = Img(r, sprite, Color.white, raycast: true);
            img.type = Image.Type.Sliced;
            var btn = r.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);
            btn.colors = colors;
            r.gameObject.AddComponent<ButtonJuice>();

            float iconSize = size.y * 0.46f;
            float textOffset = icon != null ? iconSize * 0.62f : 0f;
            if (icon != null)
            {
                var ic = Rect(r, "Icon");
                // Icon sits left of the label; label width estimated from glyph count
                float labelWidth = label.Length * fontSize * 0.52f;
                Anchor(ic, V(0.5f, 0.5f), V(-labelWidth * 0.5f + textOffset - iconSize * 0.65f, 6), V(iconSize, iconSize));
                Img(ic, icon, Color.white, raycast: false);
            }
            var t = Text(r, "Label", label, fontSize, Color.white, V(textOffset, 6), V(size.x - 20, size.y), outline: true);
            t.enableAutoSizing = true;
            t.fontSizeMax = fontSize;
            t.fontSizeMin = fontSize * 0.5f;
            return btn;
        }

        static Button IconButton(RectTransform parent, string name, string icon, Vector2 anchor, Vector2 pos, float size)
        {
            var r = Rect(parent, name);
            Anchor(r, anchor, pos, V(size, size));
            var img = Img(r, Ui + "button_dark.png", Color.white, raycast: true);
            img.type = Image.Type.Sliced;
            var btn = r.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            r.gameObject.AddComponent<ButtonJuice>();
            var ic = Rect(r, "Icon");
            Anchor(ic, V(0.5f, 0.5f), V(0, 5), V(size * 0.52f, size * 0.52f));
            Img(ic, icon, Color.white, raycast: false);
            return btn;
        }

        static RectTransform Pill(RectTransform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Sprite icon, string text, out TextMeshProUGUI label)
        {
            var r = Rect(parent, name);
            Anchor(r, anchor, pos, size);
            var bg = Img(r, Ui + "pill_white.png", Ink, raycast: false);
            bg.type = Image.Type.Sliced;

            float textLeft = 24;
            if (icon != null)
            {
                float iconSize = size.y * 0.62f;
                var ic = Rect(r, "Icon");
                ic.anchorMin = ic.anchorMax = V(0, 0.5f);
                ic.anchoredPosition = V(size.y * 0.5f, 0);
                ic.sizeDelta = V(iconSize, iconSize);
                var iconImg = ic.gameObject.AddComponent<Image>();
                iconImg.sprite = icon;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
                textLeft = size.y * 0.5f + iconSize * 0.5f + 8;
            }

            var t = Rect(r, "Text");
            t.anchorMin = Vector2.zero;
            t.anchorMax = Vector2.one;
            t.offsetMin = V(textLeft, 0);
            t.offsetMax = V(-24, 0);
            label = t.gameObject.AddComponent<TextMeshProUGUI>();
            Style(label, text, 60, Color.white, outline: false);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.enableAutoSizing = true;
            label.fontSizeMin = 28;
            label.fontSizeMax = 60;
            return r;
        }

        static TextMeshProUGUI Text(RectTransform parent, string name, string text, float size, Color color, Vector2 pos, Vector2 box, bool outline = false)
        {
            var r = Rect(parent, name);
            Anchor(r, V(0.5f, 0.5f), pos, box);
            var t = r.gameObject.AddComponent<TextMeshProUGUI>();
            Style(t, text, size, color, outline);
            return t;
        }

        static void Style(TextMeshProUGUI t, string text, float size, Color color, bool outline)
        {
            t.font = _font;
            if (outline) t.fontSharedMaterial = _outline;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
        }

        static Image Img(RectTransform r, string spritePath, Color color, bool raycast) => Img(r, Load(spritePath), color, raycast);

        static Image Img(RectTransform r, Sprite sprite, Color color, bool raycast)
        {
            var img = r.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            img.preserveAspect = sprite != null && sprite.border == Vector4.zero;
            return img;
        }

        static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static void Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        static void Anchor(RectTransform r, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            r.anchorMin = r.anchorMax = anchor;
            r.pivot = V(0.5f, 0.5f);
            r.anchoredPosition = pos;
            r.sizeDelta = size;
        }

        static Vector2 V(float x, float y) => new Vector2(x, y);

        static Sprite Load(string path)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s == null) Debug.LogWarning($"[One Block] UI sprite missing: {path}");
            return s;
        }

        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogError($"[One Block] {target.GetType().Name}.{field} not found"); return; }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogError($"[One Block] {target.GetType().Name}.{field} not found"); return; }
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
