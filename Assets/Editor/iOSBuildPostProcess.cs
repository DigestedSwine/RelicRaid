#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

// Stamps ITSAppUsesNonExemptEncryption=false into the built Info.plist so TestFlight never shows the
// export-compliance prompt. Runs automatically after every iOS build (no PlayerSettings API exists for it).
// Guarded by UNITY_IOS so it only compiles when the active target is iOS (keeps Windows/desktop builds clean).
public static class iOSBuildPostProcess
{
    [PostProcessBuild(100)]
    public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.iOS) return;

        string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
        if (!File.Exists(plistPath)) return;

        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);
        plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
        plist.WriteToFile(plistPath);
    }
}
#endif
