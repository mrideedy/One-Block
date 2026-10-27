using OneBlock.Controls;
using OneBlock.Core;
using OneBlock.Data;
using OneBlock.DevTools;
using OneBlock.Gameplay;
using OneBlock.Services;
using OneBlock.Util;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace OneBlock.EditorTools
{
    /// <summary>
    /// Builds Game.unity from scratch: systems, camera, parallax background, world, player and HUD.
    /// Re-running replaces the scene, so tweak values here (or in GameConfig) rather than by hand.
    /// </summary>
    public static class GameSceneBuilder
    {
        const string UiArt = "Assets/_Game/Art/Generated/UI/";

        [MenuItem("One Block/Setup/Rebuild Game Scene", priority = 30)]
        public static void Build()
        {
            EnsureTmpResources();
            ProjectSetup.EnsureFolder(ProjectSetup.ScenesDir);

            // Create the scene first: NewScene(Single) unloads unreferenced assets, which would turn
            // anything loaded before it into dead references.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var config = ProjectSetup.EnsureConfig();
            var assets = DataAssetBuilder.BuildAll();
            var world = assets.defaultWorld;

            // ---------------------------------------------------------------- systems
            var systems = new GameObject("[Systems]");
            Set(systems.AddComponent<GameManager>(), "config", config);
            var tap = systems.AddComponent<TapInput>();
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            es.transform.SetParent(systems.transform);

            // ---------------------------------------------------------------- camera
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = config.orthographicSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = world.cameraBackground;
            camGo.transform.position = new Vector3(0, 2.2f, -10);
            camGo.AddComponent<AudioListener>();
            Set(camGo.AddComponent<CameraFit>(), "config", config);
            var follow = camGo.AddComponent<CameraFollow>();

            // ---------------------------------------------------------------- background
            var bg = new GameObject("[Background]");
            var skyRoot = new GameObject("Sky");
            skyRoot.transform.SetParent(bg.transform);
            var skySr = Child<SpriteRenderer>(skyRoot.transform, "Gradient");
            skySr.sortingOrder = -100;
            var sky = skyRoot.AddComponent<SkyBackground>();
            Set(sky, "sky", skySr);
            Set(sky, "targetCamera", cam);
            skySr.sprite = world.sky;
            SetArray(sky, "cloudSprites", world.clouds);

            // Camera sits at y≈2.2 when the ground surface is row 0; baseY is chosen so each layer's
            // bottom lands just below the ground there, then drifts with its vertical factor.
            Parallax(bg.transform, "MountainsFar", cam, world.mountainsFar, 0.08f, 0.1f, -2.6f, 1.3f, -90, null);
            Parallax(bg.transform, "MountainsNear", cam, world.mountainsNear, 0.16f, 0.2f, -2.8f, 1.2f, -80, null);
            var hillsFill = Parallax(bg.transform, "Hills", cam, world.hills, 0.3f, 0.35f, -2.5f, 1.4f, -70, assets.whitePixel);
            hillsFill.color = world.hillsFill;
            var worldApplier = bg.AddComponent<WorldThemeApplier>();
            Set(worldApplier, "targetCamera", cam);
            Set(worldApplier, "sky", sky);
            Set(worldApplier, "mountainsFar", bg.transform.Find("MountainsFar").GetComponent<ParallaxLayer>());
            Set(worldApplier, "mountainsNear", bg.transform.Find("MountainsNear").GetComponent<ParallaxLayer>());
            Set(worldApplier, "hills", bg.transform.Find("Hills").GetComponent<ParallaxLayer>());

            // ---------------------------------------------------------------- world
            var worldGo = new GameObject("[World]");
            var run = worldGo.AddComponent<RunController>();
            var streamer = worldGo.AddComponent<LevelStreamer>();
            var builder = worldGo.AddComponent<BlockBuilder>();
            var terrain = new GameObject("Terrain").AddComponent<TerrainRenderer>();
            terrain.transform.SetParent(worldGo.transform);
            var collectibles = new GameObject("Collectibles").AddComponent<CollectibleSystem>();
            collectibles.transform.SetParent(worldGo.transform);
            var dynamicRenderer = new GameObject("Dynamic").AddComponent<DynamicWorldRenderer>();
            dynamicRenderer.transform.SetParent(worldGo.transform);

            var preview = new GameObject("BuildPreview").AddComponent<SpriteRenderer>();
            preview.transform.SetParent(worldGo.transform);
            preview.sprite = assets.blockPreview;
            preview.sortingOrder = TerrainRenderer.OrderBlock - 1;
            Set(builder, "preview", preview);
            Set(builder, "previewSprite", assets.blockPreview);
            Set(builder, "previewBlockedSprite", assets.blockPreviewBlocked);

            var playerGo = new GameObject("Player");
            var player = playerGo.AddComponent<PlayerController>();
            var anim = playerGo.AddComponent<PlayerAnimator>();
            var body = Child<SpriteRenderer>(playerGo.transform, "Body");
            body.sprite = assets.defaultCharacter.idle[0];
            body.sortingOrder = 10;
            Set(anim, "body", body);

            // Cosmetics: trail streak behind the body and the death-effect particle burst
            var trailGo = new GameObject("Trail");
            trailGo.transform.SetParent(playerGo.transform, false);
            trailGo.transform.localPosition = new Vector3(-0.1f, 0.5f, 0f);
            var trail = trailGo.AddComponent<TrailRenderer>();
            trail.sharedMaterial = TrailMaterial();
            trail.time = 0.28f;
            trail.minVertexDistance = 0.04f;
            trail.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
            trail.numCapVertices = 4;
            trail.sortingOrder = body.sortingOrder - 1;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.enabled = false;

            var effectsGo = new GameObject("DeathEffects");
            effectsGo.transform.SetParent(worldGo.transform);
            var burst = effectsGo.AddComponent<EffectBurst>();

            var cosmetics = playerGo.AddComponent<PlayerCosmetics>();
            Set(cosmetics, "catalog", DataAssetBuilder.LoadCatalog());
            Set(cosmetics, "player", player);
            Set(cosmetics, "animator", anim);
            Set(cosmetics, "terrain", terrain);
            Set(cosmetics, "collectibles", collectibles);
            Set(cosmetics, "trail", trail);
            Set(cosmetics, "effects", burst);

            Set(run, "assets", assets);
            Set(run, "player", player);
            Set(run, "playerAnimator", anim);
            Set(run, "builder", builder);
            Set(run, "streamer", streamer);
            Set(run, "terrain", terrain);
            Set(run, "collectibles", collectibles);
            Set(run, "cameraFollow", follow);
            Set(run, "tapInput", tap);
            Set(run, "cosmetics", cosmetics);
            Set(run, "worlds", worldApplier);
            Set(worldApplier, "run", run);
            Set(worldApplier, "terrain", terrain);
            Set(run, "dynamicRenderer", dynamicRenderer);
            Set(run, "elevatorSprite", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Generated/Props/moving_platform.png"));
            SetArray(run, "enemyFrames", new Object[]
            {
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Generated/Props/enemy_0.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Generated/Props/enemy_1.png")
            });

            var bot = systems.AddComponent<AutoPlayer>();
            Set(bot, "builder", builder);
            Set(bot, "player", player);
            Set(bot, "tapInput", tap);
            Set(bot, "run", run);

            // ---------------------------------------------------------------- UI
            var ui = UiBuilder.Build(assets, run);
            // ---------------------------------------------------------------- game feel
            var feel = new GameObject("GameFeel").AddComponent<GameFeel>();
            feel.transform.SetParent(worldGo.transform);
            Set(feel, "run", run);
            Set(feel, "cameraFollow", follow);
            Set(feel, "collectibles", collectibles);
            Set(feel, "builder", builder);
            const string fx = "Assets/_Game/Art/Generated/";
            Set(feel, "puff", AssetDatabase.LoadAssetAtPath<Sprite>(fx + "FX/fx_puff.png"));
            Set(feel, "spark", AssetDatabase.LoadAssetAtPath<Sprite>(fx + "FX/fx_spark.png"));
            Set(feel, "square", AssetDatabase.LoadAssetAtPath<Sprite>(fx + "FX/fx_square.png"));
            Set(feel, "star", AssetDatabase.LoadAssetAtPath<Sprite>(fx + "Props/star.png"));
            Set(feel, "ring", AssetDatabase.LoadAssetAtPath<Sprite>(fx + "UI/ring.png"));
            Set(feel, "streak", assets.whitePixel);
            var feelFont = FontAssetBuilder.EnsureFont();
            Set(feel, "font", feelFont);
            Set(feel, "fontMaterial", FontAssetBuilder.EnsureOutlineMaterial(feelFont));

            // ---------------------------------------------------------------- audio
            var audioGo = new GameObject("[Audio]");
            var audio = audioGo.AddComponent<OneBlock.Audio.AudioManager>();
            var library = AssetDatabase.LoadAssetAtPath<OneBlock.Audio.AudioLibrary>(Audio.AudioGenerator.LibraryPath);
            if (library == null) Debug.LogWarning("[One Block] No AudioLibrary yet: run One Block/Audio/Generate All Audio, then rebuild the scene.");
            Set(audio, "library", library);
            Set(audio, "run", run);

            Set(systems.AddComponent<AdsBootstrap>(), "config", AdsSetup.EnsureConfig());
            AdsSetup.Apply();
            var ads = systems.AddComponent<MockAdService>();
            Set(ads, "overlay", ui.AdOverlay);
            Set(ads, "label", ui.AdLabel);

            EditorSceneManager.SaveScene(scene, ProjectSetup.GameScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ProjectSetup.GameScenePath, true) };
            Debug.Log("[One Block] Game scene rebuilt.");
        }

        static SpriteRenderer Parallax(Transform parent, string name, Camera cam, Sprite sprite,
            float factor, float vertical, float baseY, float scale, int order, Sprite fillSprite)
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(parent);
            var layer = go.AddComponent<ParallaxLayer>();
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            sr.drawMode = SpriteDrawMode.Tiled;
            Set(layer, "targetCamera", cam);
            SetFloat(layer, "scrollFactor", factor);
            SetFloat(layer, "verticalFactor", vertical);
            SetFloat(layer, "baseY", baseY);
            SetFloat(layer, "scale", scale);

            if (fillSprite == null) return null;
            var fill = Child<SpriteRenderer>(go.transform, "FillBelow");
            fill.sprite = fillSprite;
            fill.sortingOrder = order;
            Set(layer, "fillBelow", fill);
            return fill;
        }

        // ---------------------------------------------------------------- helpers

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
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = pos;
            r.sizeDelta = size;
        }

        static Image Img(RectTransform r, Sprite sprite, bool raycast)
        {
            var img = r.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = raycast;
            return img;
        }

        static T Child<T>(Transform parent, string name) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<T>();
        }

        static Sprite Load(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogError($"[One Block] {target.GetType().Name}.{field} not found"); return; }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetFloat(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static Material TrailMaterial()
        {
            const string path = ProjectSetup.DataDir + "/Materials/Trail.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;
            ProjectSetup.EnsureFolder(ProjectSetup.DataDir + "/Materials");
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
            mat = new Material(shader) { name = "Trail" };
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static void EnsureTmpResources()
        {
            if (TMP_Settings.instance != null) return;
            TMP_PackageResourceImporter.ImportResources(true, false, false);
            AssetDatabase.Refresh();
        }
    }
}
