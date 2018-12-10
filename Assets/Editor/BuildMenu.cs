using UnityEditor;
using UnityEngine;
using UnityEditor.Build.Reporting;

public class BuildMenu : MonoBehaviour {
  [MenuItem("Build/Build Mac")]
  public static void BuildMac() {
    BuildPlayerOptions buildClientPlayerOptions = new BuildPlayerOptions();
    buildClientPlayerOptions.scenes = new[] { "Assets/Scenes/Server.unity" };
    buildClientPlayerOptions.locationPathName = "../builds/Server";
    buildClientPlayerOptions.target = BuildTarget.StandaloneOSX;
    buildClientPlayerOptions.options = BuildOptions.None;

    Build(buildClientPlayerOptions);
  }
  
  [MenuItem("Build/Build Windows")]
  public static void BuildWindows() {
    BuildPlayerOptions buildClientPlayerOptions = new BuildPlayerOptions();
    buildClientPlayerOptions.scenes = new[] { "Assets/Scenes/Server.unity" };
    buildClientPlayerOptions.locationPathName = "../builds/Server.exe";
    buildClientPlayerOptions.target = BuildTarget.StandaloneWindows64;
    buildClientPlayerOptions.options = BuildOptions.None;

    Build(buildClientPlayerOptions);
  }

  private static void Build(BuildPlayerOptions buildPlayerOptions) {
    BuildPipeline.BuildPlayer(buildPlayerOptions);
    BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
    BuildSummary summary = report.summary;

    if (summary.result == BuildResult.Succeeded) {
      Debug.Log("Build succeeded: " + summary.totalSize + " bytes");
    }

    if (summary.result == BuildResult.Failed) {
      Debug.Log("Build failed");
    }
  }
}