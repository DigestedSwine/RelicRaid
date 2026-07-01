using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// CI / headless build entry points. Invoked via:
//   Unity -batchmode -quit -projectPath . -buildTarget iOS     -executeMethod RelicRaidBuild.BuildiOS
//   Unity -batchmode -quit -projectPath . -buildTarget Android -executeMethod RelicRaidBuild.BuildAndroid
// push_testflight.sh then runs xcodebuild against the generated Builds/iOS Xcode project.
// Exits non-zero on failure so CI fails loudly. Local + CI behave identically (build number is computed here).
public static class RelicRaidBuild
{
    const string BundleId = "com.digestedswine.relicraid";
    const string TeamId   = "ZAP444WC3D";   // Apple Developer Team ID (signing cert OU)

    static string[] EnabledScenes()
    {
        var list = new List<string>();
        foreach (var s in EditorBuildSettings.scenes)
            if (s.enabled) list.Add(s.path);
        return list.ToArray();
    }

    // CI passes BUILD_NUMBER (e.g. the run number); fallback = always-increasing UTC timestamp.
    static void ApplyBuildNumber()
    {
        string bn = Environment.GetEnvironmentVariable("BUILD_NUMBER");
        if (string.IsNullOrEmpty(bn)) bn = DateTime.UtcNow.ToString("yyMMddHHmm");
        PlayerSettings.iOS.buildNumber = bn;                                       // string, must exceed last upload
        PlayerSettings.Android.bundleVersionCode = int.Parse(DateTime.UtcNow.ToString("yyMMddHH")); // int, monotonic
    }

    public static void BuildiOS()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);

        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);
        PlayerSettings.iOS.appleEnableAutomaticSigning = true;
        PlayerSettings.iOS.appleDeveloperTeamID = TeamId;
        ApplyBuildNumber();

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EnabledScenes(),
            locationPathName = "Builds/iOS",
            target = BuildTarget.iOS,
            targetGroup = BuildTargetGroup.iOS,
            options = BuildOptions.None,
        });
        Finish(report, "iOS");   // ITSAppUsesNonExemptEncryption stamped by iOSBuildPostProcess
    }

    public static void BuildAndroid()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, BundleId);
        EditorUserBuildSettings.buildAppBundle = true;   // .aab
        ApplyBuildNumber();

        // Release keystore from env (CI); unset env → Unity uses the debug keystore.
        string ks     = Environment.GetEnvironmentVariable("RELICRAID_KEYSTORE");
        string ksPass = Environment.GetEnvironmentVariable("RELICRAID_KEYSTORE_PASS");
        if (!string.IsNullOrEmpty(ks))
        {
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = ks;
            PlayerSettings.Android.keystorePass = ksPass;
            PlayerSettings.Android.keyaliasName = "relicraid";
            PlayerSettings.Android.keyaliasPass = ksPass;
        }

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EnabledScenes(),
            locationPathName = "Builds/Android/RelicRaid.aab",
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None,
        });
        Finish(report, "Android");
    }

    static void Finish(BuildReport report, string label)
    {
        var s = report.summary;
        if (s.result == BuildResult.Succeeded)
        {
            Debug.Log($"[RelicRaidBuild] {label} OK: {s.totalSize} bytes -> {s.outputPath}");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError($"[RelicRaidBuild] {label} FAILED: {s.result}, {s.totalErrors} error(s)");
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }
}
