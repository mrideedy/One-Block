using System.IO;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;

namespace OneBlock.EditorTools
{
    /// <summary>
    /// Records gameplay straight to MP4 for social clips. Play Mode only: start it, let the game run,
    /// and it stops itself. Files land in Clips/ next to the project.
    /// </summary>
    public static class ClipRecorder
    {
        public const string OutDir = "Clips";

        static RecorderController _controller;

        public static void Start(string name, int width = 1920, int height = 1080, int fps = 60, string cameraTag = null)
        {
            Directory.CreateDirectory(OutDir);
            var settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            var movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            movie.name = name;
            movie.Enabled = true;
            movie.EncoderSettings = new CoreEncoderSettings
            {
                EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High,
                Codec = CoreEncoderSettings.OutputCodec.MP4
            };
            // Camera input (not the Game view) so the clip is exactly 16:9 whatever the editor window size is
            movie.ImageInputSettings = new CameraInputSettings
            {
                Source = string.IsNullOrEmpty(cameraTag) ? ImageSource.MainCamera : ImageSource.TaggedCamera,
                CameraTag = cameraTag,
                CaptureUI = false,
                OutputWidth = width,
                OutputHeight = height
            };
            movie.AudioInputSettings.PreserveAudio = true;
            movie.OutputFile = Path.Combine(Path.GetFullPath(OutDir), name);

            settings.AddRecorderSettings(movie);
            settings.SetRecordModeToManual();
            settings.FrameRate = fps;

            _controller = new RecorderController(settings);
            _controller.PrepareRecording();
            _controller.StartRecording();
            Debug.Log($"[Clips] Recording {name} at {width}x{height}");
        }

        public static void Stop()
        {
            if (_controller == null) return;
            _controller.StopRecording();
            _controller = null;
            Debug.Log("[Clips] Stopped");
        }

        public static bool IsRecording => _controller != null && _controller.IsRecording();
    }
}
