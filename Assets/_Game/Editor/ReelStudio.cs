using System.Collections.Generic;
using OneBlock.EditorTools.Art;
using OneBlock.Gameplay;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace OneBlock.EditorTools
{
    /// <summary>
    /// Builds a 9:16 "reel stage" far away from the level: the live gameplay camera feeds a panel in the
    /// middle, a stretched darkened copy of it fills the background, and captions, a distance counter and the
    /// studio mark sit around it. <see cref="ClipRecorder"/> then records that stage camera at 1080x1920.
    /// Play Mode only.
    /// </summary>
    public static class ReelStudio
    {
        public const string CamTag = "ReelCam";
        static readonly Vector3 Stage = new(10000f, 10000f, 0f);
        const float HalfHeight = 9.6f;             // camera orthographic size -> 19.2 units tall
        const float Width = 10.8f;                 // 19.2 * 1080/1920

        static Camera _cam;
        static RenderTexture _rt;
        static GameObject _root;
        static TextMeshPro _caption, _distance;
        static RunController _run;
        static List<(float At, string Text)> _script;
        static double _started;
        static int _next;

        public static bool Active => _root != null;

        public static void Build(List<(float At, string Text)> script, string bottomLine = "FREE ON GOOGLE PLAY")
        {
            Teardown();
            EnsureTag();

            _run = Object.FindFirstObjectByType<RunController>();
            var game = Camera.main;
            _rt = new RenderTexture(1920, 1080, 24) { antiAliasing = 4, name = "ReelFeed" };
            game.targetTexture = _rt;

            _root = new GameObject("[ReelStage]");
            _root.transform.position = Stage;

            // Flat brand-ink surround reads cleaner behind text than a stretched copy of the feed.
            // The panel is wider than the frame so the action fills the screen; x nudges the hero off the edge.
            const float panel = 12.8f;
            Quad("Panel", new Vector3(1.3f, 0.6f, 0f), new Vector3(panel, panel * 9f / 16f, 1f), _rt, Color.white);

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Game/Art/Fonts/LilitaOne SDF.asset");
            _caption = Text("Caption", new Vector3(0, 6.9f, -1f), 1.6f, Color.white, font, 9.6f);
            _distance = Text("Distance", new Vector3(0, 5.2f, -1f), 1.2f, ArtCanvas.Hex("#FFD66B"), font, 9.6f);
            var title = Text("Title", new Vector3(0, -5.3f, -1f), 1.35f, Color.white, font, 9.6f);
            title.text = "ONE BLOCK";
            var cta = Text("Cta", new Vector3(0, -6.5f, -1f), 0.75f, new Color(1f, 1f, 1f, 0.85f), font, 9.6f);
            cta.text = bottomLine;
            Mark(new Vector3(0, -8.0f, -1f), 1.4f);

            _cam = new GameObject("[ReelCam]") { tag = CamTag }.AddComponent<Camera>();
            _cam.transform.position = Stage + new Vector3(0, 0, -10f);
            _cam.orthographic = true;
            _cam.orthographicSize = HalfHeight;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = ArtCanvas.Hex("#141A2E");
            // Must sit on display 0: Unity never renders other displays in the editor, and the Recorder would
            // then capture one stale frame forever. The game camera draws into the feed texture, not the screen.
            _cam.targetDisplay = 0;
            _cam.depth = 100;

            _script = script;
            _next = 0;
            _started = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
        }

        public static void Teardown()
        {
            EditorApplication.update -= Tick;
            if (Camera.main != null) Camera.main.targetTexture = null;
            if (_root != null) Object.DestroyImmediate(_root);
            if (_cam != null) Object.DestroyImmediate(_cam.gameObject);
            if (_rt != null) { _rt.Release(); Object.DestroyImmediate(_rt); }
            _root = null; _cam = null; _rt = null; _caption = null; _distance = null;
        }

        static void Tick()
        {
            if (_root == null) return;
            if (_run != null && _distance != null) _distance.text = $"{_run.Distance:0} m";

            float t = (float)(EditorApplication.timeSinceStartup - _started);
            while (_script != null && _next < _script.Count && t >= _script[_next].At)
            {
                _caption.text = _script[_next].Text;
                _next++;
            }
            // Gentle pop on each new caption
            if (_caption != null && _next > 0)
            {
                float since = t - _script[_next - 1].At;
                float pop = since < 0.35f ? 1f + Mathf.Sin(since / 0.35f * Mathf.PI) * 0.12f : 1f;
                _caption.transform.localScale = Vector3.one * pop;
            }
        }

        // ------------------------------------------------------------------ pieces

        static void Quad(string name, Vector3 local, Vector3 scale, Texture tex, Color tint, float alpha = 1f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(_root.transform, false);
            go.transform.localPosition = local;
            go.transform.localScale = scale;
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
            var mat = new Material(shader);
            if (tex != null) { mat.mainTexture = tex; if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex); }
            var c = new Color(tint.r, tint.g, tint.b, alpha);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            mat.color = c;
            if (alpha < 1f)
            {
                mat.SetFloat("_Surface", 1f);
                mat.renderQueue = 3000;
                mat.SetFloat("_Blend", 0f);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        static TextMeshPro Text(string name, Vector3 local, float size, Color color, TMP_FontAsset font, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root.transform, false);
            go.transform.localPosition = local;
            var t = go.AddComponent<TextMeshPro>();
            if (font != null) t.font = font;
            t.fontSize = size * 4f;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.enableWordWrapping = true;
            t.rectTransform.sizeDelta = new Vector2(width, size * 3f);
            return t;
        }

        static void Mark(Vector3 local, float size)
        {
            var canvas = BrandArt.Mark(ArtCanvas.Hex("#FFD66B"), true);
            var tex = new Texture2D(canvas.Width, canvas.Height, TextureFormat.RGBA32, false);
            for (int y = 0; y < canvas.Height; y++)
            for (int x = 0; x < canvas.Width; x++) tex.SetPixel(x, y, canvas.Get(x, y));
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width / size);
            var go = new GameObject("Mark");
            go.transform.SetParent(_root.transform, false);
            go.transform.localPosition = local;
            go.AddComponent<SpriteRenderer>().sprite = sprite;
        }

        /// <summary>The recorder picks the stage camera by tag, so make sure the tag exists.</summary>
        static void EnsureTag()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var tags = tagManager.FindProperty("tags");
            for (int i = 0; i < tags.arraySize; i++)
                if (tags.GetArrayElementAtIndex(i).stringValue == CamTag) return;
            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = CamTag;
            tagManager.ApplyModifiedProperties();
        }
    }
}
