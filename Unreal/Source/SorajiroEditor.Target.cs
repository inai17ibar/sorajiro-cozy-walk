using UnrealBuildTool;
using System.Collections.Generic;

public class SorajiroEditorTarget : TargetRules
{
    public SorajiroEditorTarget(TargetInfo Target) : base(Target)
    {
        Type = TargetType.Editor;
        DefaultBuildSettings = BuildSettingsVersion.V5;
        ExtraModuleNames.Add("Sorajiro");
    }
}
