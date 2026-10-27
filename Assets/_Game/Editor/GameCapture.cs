using System.IO;
using UnityEngine;

namespace OneBlock.EditorTools
{
    /// <summary>
    /// Renders the main camera plus screen-space UI into a PNG, independent of whether the Game view is
    /// visible or focused (ScreenCapture needs a repainting Game view).
    /// </summary>
    public static class GameCapture
    {
        public static void Capture(string path, int width = 1920, int height = 1080)
        {
            var cam = Camera.main;
            if (cam == null) return;

            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude);
            var modes = new RenderMode[canvases.Length];
            var layers = new int[canvases.Length];
            var orders = new int[canvases.Length];
            // Camera-space UI is sorted together with world sprites, so lift it above every sorting layer
            var sortingLayers = SortingLayer.layers;
            int topLayer = sortingLayers[sortingLayers.Length - 1].id;
            for (int i = 0; i < canvases.Length; i++)
            {
                modes[i] = canvases[i].renderMode;
                layers[i] = canvases[i].sortingLayerID;
                orders[i] = canvases[i].sortingOrder;
                if (modes[i] != RenderMode.ScreenSpaceOverlay || !canvases[i].isRootCanvas) continue;
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = cam;
                canvases[i].planeDistance = 1f;
                canvases[i].sortingLayerID = topLayer;
                canvases[i].sortingOrder = 20000 + orders[i];
            }

            var prevTarget = cam.targetTexture;
            cam.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = prevTarget;

            for (int i = 0; i < canvases.Length; i++)
            {
                if (modes[i] != RenderMode.ScreenSpaceOverlay || !canvases[i].isRootCanvas) continue;
                canvases[i].renderMode = modes[i];
                canvases[i].sortingLayerID = layers[i];
                canvases[i].sortingOrder = orders[i];
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
        }
    }
}
