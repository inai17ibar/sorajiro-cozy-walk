#!/usr/bin/env python3
"""Bounded, local-only Unity Pipeline commands for the Sorajiro art review.

Uses the owner's installed CLI/Editor. Does not install software, upload assets,
accept arbitrary C#, or save an edited owner scene. Run each stage separately.
"""
import argparse
import json
import subprocess
import time
from pathlib import Path

COMMANDS = {
    "status": """var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var asset = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.HighDefinition.HDRenderPipelineAsset;
return new { scene = scene.path, playing = UnityEditor.EditorApplication.isPlaying,
dirty = scene.isDirty, compiling = UnityEditor.EditorApplication.isCompiling,
gpu = UnityEngine.SystemInfo.graphicsDeviceName, api = UnityEngine.SystemInfo.graphicsDeviceType.ToString(),
ssgiSupported = asset != null && asset.currentPlatformRenderPipelineSettings.supportSSGI };""",
    "stop": "UnityEditor.EditorApplication.isPlaying = false; return \"Stop requested; wait for Edit mode before generating.\";",
    "refresh": "if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) throw new System.Exception(\"Stop Play first\"); UnityEditor.AssetDatabase.Refresh(); return \"Refresh requested; wait for compilation\";",
    "create": "return Sorajiro.Editor.ArtReviewAutomation.Schedule(\"create\");",
    "open": "Sorajiro.Editor.TwilightLookDevBuilder.Open(); return Sorajiro.Editor.TwilightLookDevBuilder.ScenePath;",
    "edit-tests": "return Sorajiro.Editor.ArtReviewAutomation.Schedule(\"edit-tests\");",
    "capture": "Sorajiro.Editor.TwilightVerification.Capture(); return \"Asynchronous 8-shot capture started; wait for complete.txt\";",
    "play": "Sorajiro.Editor.TwilightLookDevBuilder.Preflight(); UnityEditor.EditorApplication.isPlaying = true; return \"Play requested\";",
    "runtime-tests": "return Sorajiro.Editor.ArtReviewAutomation.Schedule(\"runtime-tests\");",
    "motion": "Sorajiro.Editor.ArtReviewAutomation.RecordScriptedMotion(); return \"Scripted focus fixture + asynchronous 30s actual Play capture started; wait for complete.txt\";",
}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("stage", choices=COMMANDS)
    parser.add_argument("--project", type=Path, default=Path.home()/"Documents/Unity/sorajiro-unity")
    args = parser.parse_args()
    project = args.project.expanduser().resolve(strict=True)
    manifest = project/"Packages/manifest.json"
    if "com.sorajiro.cozywalk" not in json.loads(manifest.read_text()).get("dependencies", {}):
        raise SystemExit("Refusing a project without the Sorajiro package link")
    cli = Path.home()/".unity/bin/unity"
    command = [str(cli), "--no-banner", "--non-interactive", "command", "--project-path", str(project),
               "--timeout", "300", "eval", COMMANDS[args.stage], "--json"]
    result = subprocess.run(command, text=True, capture_output=True, timeout=330)
    print(result.stdout, end="")
    if result.stderr:
        print(result.stderr, end="")
    if result.returncode:
        raise SystemExit(result.returncode)
    envelope = json.loads(result.stdout)
    if envelope.get("success") is False or envelope.get("data", {}).get("result", {}).get("success") is False:
        raise SystemExit("Unity reported a command failure")
    if args.stage in ("create", "edit-tests", "runtime-tests"):
        job = Path(envelope["data"]["result"]["result"]).resolve()
        job.relative_to(Path(__file__).resolve().parents[1]/"Artifacts/UnityEditor/PipelineJobs")
        deadline = time.monotonic()+300
        while time.monotonic()<deadline:
            try:
                record=json.loads(job.read_text())
            except json.JSONDecodeError:
                time.sleep(.25)
                continue
            if record["state"] in ("completed", "failed"):
                print(json.dumps(record, ensure_ascii=False, indent=2))
                if record["state"]=="failed":
                    raise SystemExit(1)
                return
            time.sleep(.25)
        raise SystemExit("Editor job still pending; inspect "+str(job)+" before retrying. No completion claim.")


if __name__ == "__main__":
    main()
