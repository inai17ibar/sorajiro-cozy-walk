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
    "comfort-runtime": 'return Sorajiro.Editor.ComfortPlayVerification.Begin();',
    "play-feel-status": 'var p=UnityEngine.Object.FindAnyObjectByType<Sorajiro.ComfortPlay>();return new {ready=p && p.Ready,pitch=p&&p.Ready?p.CameraRig.pitch:0,yaw=p&&p.Ready?p.CameraRig.yaw:0,jumps=p?p.JumpSoundCount:0,camera=p&&p.Ready?p.CameraRig.transform.position:UnityEngine.Vector3.zero};',
    "comfort-create": 'return Sorajiro.Editor.ComfortPlayBuilder.Schedule("create");',
    "comfort-open": 'return Sorajiro.Editor.ComfortPlayBuilder.Schedule("open");',
    "comfort-check": 'return Sorajiro.Editor.ComfortPlayBuilder.Schedule("check");',
    "comfort-play": 'return Sorajiro.Editor.ComfortPlayBuilder.Schedule("play");',

    "light-create": 'return Sorajiro.Editor.FleeceLightingBuilder.Schedule("create");',
    "light-open": 'return Sorajiro.Editor.FleeceLightingBuilder.Schedule("open");',
    "light-check": 'return Sorajiro.Editor.FleeceLightingBuilder.Schedule("check");',
    "light-play": 'return Sorajiro.Editor.FleeceLightingBuilder.Schedule("play");',
    "light-capture": 'return Sorajiro.Editor.FleeceLightingCapture.Begin();',

    "star-runtime": 'return Sorajiro.Editor.StarPlayVerification.Begin();',
    "gameplay-status": 'var s=UnityEngine.Object.FindAnyObjectByType<Sorajiro.StarPlaySession>();return new { present=s!=null, ready=s && s.Ready, collected=s?s.CollectedCount:-1,total=s?s.TotalCount:-1,footsteps=s?s.FootstepCount:-1,chimes=s?s.PickupSoundCount:-1,speed=s?s.Motor.PlanarSpeed:0,grounded=s && s.Motor.Grounded,focused=s && s.Motor.Focused,paused=s && s.Motor.Paused,anim=s?s.Motor.GetComponent<Sorajiro.BearAnimationDriver>().animator.GetCurrentAnimatorStateInfo(0).normalizedTime:0 };',
    "star-create": 'return Sorajiro.Editor.StarPlayBuilder.Schedule("create");',
    "star-open": 'return Sorajiro.Editor.StarPlayBuilder.Schedule("open");',
    "star-check": 'return Sorajiro.Editor.StarPlayBuilder.Schedule("check");',
    "star-play": 'return Sorajiro.Editor.StarPlayBuilder.Schedule("play");',

    "star-info": 'return string.Join("; ", System.Linq.Enumerable.Select(System.Linq.Enumerable.Where(UnityEngine.Object.FindObjectsByType<UnityEngine.Renderer>(),r=>r.name.ToLowerInvariant().Contains("star")),r=>r.name+" center="+r.bounds.center+" size="+r.bounds.size));',
    "selected-light-info": 'var root=UnityEngine.GameObject.Find(Sorajiro.Editor.BedsideBearBuilder.LightsName);return string.Join("; ",System.Linq.Enumerable.Select(root.GetComponentsInChildren<UnityEngine.Light>(true),l=>l.name+" active="+l.gameObject.activeInHierarchy+" enabled="+l.enabled+" power="+l.intensity+" unit="+l.lightUnit+" pos="+l.transform.position+" forward="+l.transform.forward+" layers="+(uint)l.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalLightData>().lightlayersMask+" shadows="+l.renderingLayerMask));',
    "lighting-info": 'var a=(UnityEngine.Rendering.HighDefinition.HDRenderPipelineAsset)UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline; return new { asset=UnityEditor.AssetDatabase.GetAssetPath(a), lightLayers=a.currentPlatformRenderPipelineSettings.supportLightLayers, directionalShadows=a.currentPlatformRenderPipelineSettings.hdShadowInitParams.directionalShadowFilteringQuality.ToString() };',
    "status": """var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var asset = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.HighDefinition.HDRenderPipelineAsset;
return new { scene = scene.path, playing = UnityEditor.EditorApplication.isPlaying, editorPaused = UnityEditor.EditorApplication.isPaused,
dirty = scene.isDirty, compiling = UnityEditor.EditorApplication.isCompiling,
gpu = UnityEngine.SystemInfo.graphicsDeviceName, api = UnityEngine.SystemInfo.graphicsDeviceType.ToString(),
ssgiSupported = asset != null && asset.currentPlatformRenderPipelineSettings.supportSSGI };""",
    "reload-scripts": "if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new System.Exception(\"Stop Play and preserve scene edits first\"); UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceSynchronousImport); UnityEditor.EditorUtility.RequestScriptReload(); return \"Full script reload requested\";",
    "stop": "UnityEditor.EditorApplication.isPlaying = false; return \"Stop requested; wait for Edit mode before generating.\";",
    "refresh": "if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) throw new System.Exception(\"Stop Play first\"); UnityEditor.AssetDatabase.Refresh(); return \"Refresh requested; wait for compilation\";",
    "create": "return Sorajiro.Editor.ArtReviewAutomation.Schedule(\"create\");",
    "open": "Sorajiro.Editor.TwilightLookDevBuilder.Open(); return Sorajiro.Editor.TwilightLookDevBuilder.ScenePath;",
    "edit-tests": "return Sorajiro.Editor.ArtReviewAutomation.Schedule(\"edit-tests\");",
    "capture": "Sorajiro.Editor.TwilightVerification.Capture(); return \"Asynchronous 8-shot capture started; wait for complete.txt\";",
    "play": "Sorajiro.Editor.TwilightLookDevBuilder.Preflight(); UnityEditor.EditorApplication.isPlaying = true; return \"Play requested\";",
    "runtime-tests": "return Sorajiro.Editor.ArtReviewAutomation.Schedule(\"runtime-tests\");",
    "motion": "Sorajiro.Editor.ArtReviewAutomation.RecordScriptedMotion(); return \"Scripted focus fixture + asynchronous 30s actual Play capture started; wait for complete.txt\";",
    "corner-create": "return Sorajiro.Editor.ArtReviewAutomation.Schedule(\"corner-create\");",
    "corner-check": "return Sorajiro.Editor.ArtReviewAutomation.Schedule(\"corner-check\");",
    "corner-bake": "return Sorajiro.Editor.BedsideCornerBuilder.Bake();",
    "corner-open": "return Sorajiro.Editor.ArtReviewAutomation.Schedule(\"corner-open\");",
    "corner-capture": "return Sorajiro.Editor.ArtReviewAutomation.Schedule(\"corner-capture\");",
    "corner-play": "return Sorajiro.Editor.ArtReviewAutomation.Schedule(\"corner-play\");",
    "corner-runtime": "return Sorajiro.Editor.ArtReviewAutomation.Schedule(\"corner-runtime\");",
    "bedding-create": 'return Sorajiro.Editor.ArtReviewAutomation.Schedule("bedding-create");',
    "bedding-check": 'return Sorajiro.Editor.ArtReviewAutomation.Schedule("bedding-check");',
    "bedding-open": 'return Sorajiro.Editor.ArtReviewAutomation.Schedule("bedding-open");',
    "bedding-capture": 'return Sorajiro.Editor.ArtReviewAutomation.Schedule("bedding-capture");',
    "bedding-play": 'return Sorajiro.Editor.ArtReviewAutomation.Schedule("bedding-play");',
    "bedding-runtime": 'return Sorajiro.Editor.ArtReviewAutomation.Schedule("bedding-runtime");',
    "bedding-bake": "return Sorajiro.Editor.BeddingRefinementBuilder.Bake();",
    "contact-create": 'return Sorajiro.Editor.ArtReviewAutomation.Schedule("contact-create");',
    "contact-check": 'return Sorajiro.Editor.ArtReviewAutomation.Schedule("contact-check");',
    "contact-open": 'return Sorajiro.Editor.ArtReviewAutomation.Schedule("contact-open");',
    "contact-capture": 'return Sorajiro.Editor.ArtReviewAutomation.Schedule("contact-capture");',
    "contact-play": 'return Sorajiro.Editor.ArtReviewAutomation.Schedule("contact-play");',
    "contact-runtime": 'return Sorajiro.Editor.ArtReviewAutomation.Schedule("contact-runtime");',
    "contact-bake": "return Sorajiro.Editor.BeddingContactBuilder.Bake();",
    "drape-create": 'return Sorajiro.Editor.ArtReviewAutomation.Schedule("drape-create");',
    "drape-check": 'return Sorajiro.Editor.ArtReviewAutomation.Schedule("drape-check");',
    "drape-open": 'return Sorajiro.Editor.ArtReviewAutomation.Schedule("drape-open");',
    "drape-capture": 'return Sorajiro.Editor.ArtReviewAutomation.Schedule("drape-capture");',
    "drape-play": 'return Sorajiro.Editor.ArtReviewAutomation.Schedule("drape-play");',
    "drape-runtime": 'return Sorajiro.Editor.ArtReviewAutomation.Schedule("drape-runtime");',
    "drape-bake": "return Sorajiro.Editor.BeddingDrapeBuilder.Bake();",
    "surface-create": 'return Sorajiro.Editor.ArtReviewAutomation.Schedule("surface-create");',
    "surface-check": 'return Sorajiro.Editor.ArtReviewAutomation.Schedule("surface-check");',
    "surface-open": 'return Sorajiro.Editor.ArtReviewAutomation.Schedule("surface-open");',
    "surface-capture": 'return Sorajiro.Editor.ArtReviewAutomation.Schedule("surface-capture");',
    "surface-play": 'return Sorajiro.Editor.ArtReviewAutomation.Schedule("surface-play");',
    "surface-runtime": 'return Sorajiro.Editor.ArtReviewAutomation.Schedule("surface-runtime");',
    "surface-bake": "return Sorajiro.Editor.BeddingSurfaceBuilder.Bake();",
    "round-create": 'return Sorajiro.Editor.ArtReviewAutomation.Schedule("round-create");',
    "round-check": 'return Sorajiro.Editor.ArtReviewAutomation.Schedule("round-check");',
    "round-open": 'return Sorajiro.Editor.ArtReviewAutomation.Schedule("round-open");',
    "round-capture": 'return Sorajiro.Editor.ArtReviewAutomation.Schedule("round-capture");',
    "round-play": 'return Sorajiro.Editor.ArtReviewAutomation.Schedule("round-play");',
    "round-runtime": 'return Sorajiro.Editor.ArtReviewAutomation.Schedule("round-runtime");',
    "round-bake": "return Sorajiro.Editor.BeddingRoundBuilder.Bake();",
    "stable-create": 'return Sorajiro.Editor.ArtReviewAutomation.Schedule("stable-create");',
    "stable-check": 'return Sorajiro.Editor.ArtReviewAutomation.Schedule("stable-check");',
    "stable-open": 'return Sorajiro.Editor.ArtReviewAutomation.Schedule("stable-open");',
    "stable-capture": 'return Sorajiro.Editor.ArtReviewAutomation.Schedule("stable-capture");',
    "stable-play": 'return Sorajiro.Editor.ArtReviewAutomation.Schedule("stable-play");',
    "stable-runtime": 'return Sorajiro.Editor.ArtReviewAutomation.Schedule("stable-runtime");',
    "stable-bake": "return Sorajiro.Editor.BeddingStableBuilder.Bake();",
    "stable-save-apv": 'if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.Exception("Stop Play first"); var set=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.ProbeVolumeBakingSet>(Sorajiro.Editor.BeddingStableBuilder.Output+"/APV_BakingSet.asset"); UnityEditor.AssetDatabase.SaveAssetIfDirty(set); return "Reloaded APV persisted; run stable-check to verify receipt unchanged";',
    "bedbear-create": 'return Sorajiro.Editor.BedsideBearActions.Schedule("create");',
    "bedbear-open": 'return Sorajiro.Editor.BedsideBearActions.Schedule("open");',
    "bedbear-check": 'return Sorajiro.Editor.BedsideBearActions.Schedule("check");',
    "bedbear-capture": 'return Sorajiro.Editor.BedsideBearActions.Schedule("capture");',
    "bedbear-play": 'return Sorajiro.Editor.BedsideBearActions.Schedule("play");',
    "bedbear-runtime": 'return Sorajiro.Editor.BedsideBearActions.Schedule("runtime");',
    "bedbear-bake": 'return Sorajiro.Editor.BedsideBearBuilder.Bake();',
    "fit-create": 'return Sorajiro.Editor.BearFitActions.Schedule("create");',
    "fit-open": 'return Sorajiro.Editor.BearFitActions.Schedule("open");',
    "fit-check": 'return Sorajiro.Editor.BearFitActions.Schedule("check");',
    "fit-clips": 'return Sorajiro.Editor.BearFitActions.Schedule("clips");',
    "fit-capture": 'return Sorajiro.Editor.BearFitActions.Schedule("capture");',
    "fit-play": 'return Sorajiro.Editor.BearFitActions.Schedule("play");',
    "fit-runtime": 'return Sorajiro.Editor.BearFitActions.Schedule("runtime");',
    "fleece-create": 'return Sorajiro.Editor.FleeceStudyActions.Schedule("create");',
    "fleece-check": 'return Sorajiro.Editor.FleeceStudyActions.Schedule("check");',
    "fleece-open": 'return Sorajiro.Editor.FleeceStudyActions.Schedule("open");',
    "fleece-capture": 'return Sorajiro.Editor.FleeceStudyActions.Schedule("capture");',
    "fleece-play": 'return Sorajiro.Editor.FleeceStudyActions.Schedule("play");',
    "fleece-runtime": 'return Sorajiro.Editor.FleeceStudyActions.Schedule("runtime");',
    "fleece-room-preview": 'return Sorajiro.Editor.FleeceStudyActions.Schedule("room-preview");',
    "fleece-room-capture": 'return Sorajiro.Editor.FleeceStudyActions.Schedule("room-capture");',
    "save-fleece-trial": 'var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene(); if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || s.path!="" || !System.Linq.Enumerable.Any(s.GetRootGameObjects(),g=>g.name=="Fleece_Master_Bear_Trial") || System.IO.File.Exists(Sorajiro.Editor.FleeceStudyBuilder.ScenePath))throw new System.Exception("Only new unsaved fleece trial allowed"); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s,Sorajiro.Editor.FleeceStudyBuilder.ScenePath); return "Partial fleece trial preserved";',
    "cancel-bake": "UnityEditor.Lightmapping.Cancel(); return \"Bake cancellation requested; partial scene is preserved\";",
    "save-corner-trial": "var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene(); if(!s.path.StartsWith(\"Assets/Sorajiro/Generated/Bedside_\"))throw new System.Exception(\"Generated bedside scene required\"); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s); return \"Current generated trial saved and preserved\";",
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
    call_started=time.time()
    result = subprocess.run(command, text=True, capture_output=True, timeout=330)
    print(result.stdout, end="")
    if result.stderr:
        print(result.stderr, end="")
    envelope = json.loads(result.stdout)
    if result.returncode and (args.stage in ("corner-bake","bedding-bake","contact-bake","drape-bake","surface-bake","round-bake","stable-bake") or args.stage.startswith(("fleece-", "fit-", "bedbear-", "star-", "light-", "comfort-"))) and "Main thread operation timed out" in result.stdout:
        # Pipeline's RPC can time out while native light extraction continues. Do not start a second bake.
        jobs=Path(__file__).resolve().parents[1]/"Artifacts/UnityEditor/PipelineJobs"
        observation_deadline=time.monotonic()+30
        candidates=[]
        while time.monotonic()<observation_deadline:
            candidates=[p for p in jobs.glob("*_"+args.stage+".json") if p.stat().st_mtime>=call_started]
            if candidates:break
            time.sleep(.25)
        if len(candidates)==1:
            envelope={"success":True,"data":{"result":{"success":True,"result":str(candidates[0])}}}
            print("RPC timed out; observing the already-created bake job. Completion is still pending.")
        else:
            raise SystemExit("RPC timed out; no unique bake job found. Inspect Editor before retrying.")
    elif result.returncode:
        raise SystemExit(result.returncode)
    if envelope.get("success") is False or envelope.get("data", {}).get("result", {}).get("success") is False:
        raise SystemExit("Unity reported a command failure")
    if (args.stage.startswith(("bedding-","contact-","drape-","surface-","round-","stable-","fleece-","fit-","bedbear-","star-","light-","comfort-")) and args.stage!="stable-save-apv") or args.stage in ("create", "edit-tests", "runtime-tests", "corner-create", "corner-check", "corner-bake", "corner-runtime", "corner-open", "corner-capture", "corner-play"):
        job = Path(envelope["data"]["result"]["result"]).resolve()
        job.relative_to(Path(__file__).resolve().parents[1]/"Artifacts/UnityEditor/PipelineJobs")
        deadline = time.monotonic()+(960 if args.stage in ("corner-bake","bedding-bake","contact-bake","drape-bake","surface-bake","round-bake","stable-bake") else 300)
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
