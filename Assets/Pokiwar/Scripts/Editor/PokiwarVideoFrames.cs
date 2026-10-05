using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;

namespace Pokiwar.EditorTools
{
    /// <summary>Writes JPG frames of a reference video at given seconds: -video path -times "12,130.5" -out dir [-width 960].</summary>
    public static class PokiwarVideoFrames
    {
        private static VideoPlayer player;
        private static readonly Queue<double> pending = new Queue<double>();
        private static string outDir;
        private static int width = 960;
        private static double target = -1;
        private static double started;
        private static double stepStarted;
        private static bool seeking;

        public static void Run()
        {
            var args = Environment.GetCommandLineArgs();
            string video = Arg(args, "-video");
            outDir = Arg(args, "-out");
            string times = Arg(args, "-times");
            if (int.TryParse(Arg(args, "-width"), out var w)) width = w;
            if (string.IsNullOrEmpty(video) || string.IsNullOrEmpty(outDir) || string.IsNullOrEmpty(times))
            {
                Debug.LogError("[FRAMES] usage: -video <path> -times \"1,2.5\" -out <dir> [-width 960]");
                EditorApplication.Exit(2);
                return;
            }
            Directory.CreateDirectory(outDir);
            foreach (var t in times.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
                pending.Enqueue(double.Parse(t, CultureInfo.InvariantCulture));
            var go = new GameObject("FrameGrabber") { hideFlags = HideFlags.HideAndDontSave };
            player = go.AddComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.source = VideoSource.Url;
            player.url = video;
            player.renderMode = VideoRenderMode.APIOnly;
            player.audioOutputMode = VideoAudioOutputMode.None;
            player.skipOnDrop = false;
            player.sendFrameReadyEvents = true;
            player.frameReady += OnFrame;
            player.errorReceived += (p, msg) => { Debug.LogError("[FRAMES] " + msg); EditorApplication.Exit(3); };
            player.prepareCompleted += p =>
            {
                Debug.Log("[FRAMES] prepared " + p.width + "x" + p.height + " " + p.frameRate.ToString("0.##") + " fps, " + p.length.ToString("0") + " s");
                Next();
            };
            started = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
            player.Prepare();
        }

        private static void Next()
        {
            if (pending.Count == 0)
            {
                Debug.Log("[FRAMES] done");
                EditorApplication.Exit(0);
                return;
            }
            target = pending.Dequeue();
            seeking = true;
            stepStarted = EditorApplication.timeSinceStartup;
            player.time = target;
            player.Play();
        }

        private static void OnFrame(VideoPlayer p, long frame)
        {
            if (!seeking || p.time + 0.5 < target) return;
            seeking = false;
            p.Pause();
            Save(p.texture, target);
            Next();
        }

        private static void Tick()
        {
            if (player != null && seeking && EditorApplication.timeSinceStartup - stepStarted > 20)
            {
                Debug.LogError("[FRAMES] no frame at " + target + " s");
                seeking = false;
                Next();
            }
            if (EditorApplication.timeSinceStartup - started > 900)
            {
                Debug.LogError("[FRAMES] timeout");
                EditorApplication.Exit(4);
            }
        }

        private static void Save(Texture src, double t)
        {
            int h = Mathf.RoundToInt(width * (float)src.height / src.width);
            var rt = RenderTexture.GetTemporary(width, h, 0);
            Graphics.Blit(src, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(width, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, h), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            int s = (int)t;
            string name = "t" + (s / 60).ToString("00") + "m" + (s % 60).ToString("00") + "s" + ((int)((t - s) * 10)).ToString() + ".jpg";
            File.WriteAllBytes(Path.Combine(outDir, name), tex.EncodeToJPG(85));
            UnityEngine.Object.DestroyImmediate(tex);
            Debug.Log("[FRAMES] wrote " + name);
        }

        private static string Arg(string[] a, string key)
        {
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == key) return a[i + 1];
            return null;
        }
    }
}
