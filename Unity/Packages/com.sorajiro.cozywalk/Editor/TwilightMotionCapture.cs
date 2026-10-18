using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sorajiro.Editor
{
    public static class TwilightMotionCapture
    {
        [MenuItem("Sorajiro/Twilight/8. Record 30s scripted motion (Play)")]
        public static void Begin()
        {
            if(!EditorApplication.isPlaying || SceneManager.GetActiveScene().path!=TwilightLookDevBuilder.ScenePath) throw new Exception("Enter Play in Twilight first.");
            if(UnityEngine.Object.FindAnyObjectByType<MotionCaptureProbe>()) throw new Exception("Capture already running.");
            var motor=UnityEngine.Object.FindAnyObjectByType<SorajiroMotor>();
            if(motor.Paused || !motor.Focused) throw new Exception("Focused unpaused player required.");
            string folder=Path.Combine(TwilightLookDevBuilder.Repository,"Artifacts/Visual/motion",DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff"));
            if(Directory.Exists(folder))throw new IOException("Capture revision exists.");
            new GameObject("Editor_ONLY_Motion_Capture").AddComponent<MotionCaptureProbe>().Initialize(motor,folder);
        }
    }
}
