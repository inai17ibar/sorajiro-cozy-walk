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
            if(!EditorApplication.isPlaying || (SceneManager.GetActiveScene().path!=TwilightLookDevBuilder.ScenePath && SceneManager.GetActiveScene().path!=BedsideCornerBuilder.ScenePath && SceneManager.GetActiveScene().path!=BeddingRefinementBuilder.ScenePath && SceneManager.GetActiveScene().path!=BeddingContactBuilder.ScenePath && SceneManager.GetActiveScene().path!=BeddingDrapeBuilder.ScenePath && SceneManager.GetActiveScene().path!=BeddingSurfaceBuilder.ScenePath && SceneManager.GetActiveScene().path!=BeddingRoundBuilder.ScenePath && SceneManager.GetActiveScene().path!=BeddingStableBuilder.ScenePath && SceneManager.GetActiveScene().path!=FleeceStudyBuilder.ScenePath && SceneManager.GetActiveScene().path!=BearFitBuilder.ScenePath && SceneManager.GetActiveScene().path!=BedsideBearBuilder.ScenePath)) throw new Exception("Enter Play in the generated Twilight or Bedside scene first.");
            if(UnityEngine.Object.FindAnyObjectByType<MotionCaptureProbe>()) throw new Exception("Capture already running.");
            var motor=UnityEngine.Object.FindAnyObjectByType<SorajiroMotor>();
            if(motor.Paused || !motor.Focused) throw new Exception("Focused unpaused player required.");
            string folder=Path.Combine(TwilightLookDevBuilder.Repository,"Artifacts/Visual/motion",DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff"));
            if(Directory.Exists(folder))throw new IOException("Capture revision exists.");
            new GameObject("Editor_ONLY_Motion_Capture").AddComponent<MotionCaptureProbe>().Initialize(motor,folder);
        }
    }
}
