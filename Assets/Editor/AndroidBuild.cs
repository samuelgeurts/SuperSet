using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

//batch build of an APK to install on an Android phone:
//Unity -batchmode -quit -buildTarget Android -executeMethod AndroidBuild.Build -buildOutput <file.apk> [-buildVersion 1.0.0]
public static class AndroidBuild
{
	public static void Build ()
	{
		string output = "Build/SuperSet.apk";
		string version = null;
		string[] args = System.Environment.GetCommandLineArgs ();
		for (int i = 0; i < args.Length - 1; i++) {
			if (args [i] == "-buildOutput") {
				output = args [i + 1];
			}
			if (args [i] == "-buildVersion") {
				version = args [i + 1];
			}
		}

		//the Android settings live here, so the project files stay the same for the other platforms
		PlayerSettings.SetApplicationIdentifier (NamedBuildTarget.Android, "nl.sngsolutions.superset");
		PlayerSettings.SetScriptingBackend (NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
		PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
		PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
		PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation; //upright and sideways
		if (version != null) {
			PlayerSettings.bundleVersion = version;
		}
		EditorUserBuildSettings.buildAppBundle = false; //an APK, not an app bundle for the Play Store

		BuildPlayerOptions options = new BuildPlayerOptions ();
		options.scenes = new string[] { "Assets/Scenes/Card Deck Test.unity" };
		options.locationPathName = output;
		options.target = BuildTarget.Android;
		options.targetGroup = BuildTargetGroup.Android;
		options.options = BuildOptions.None;
		BuildReport report = BuildPipeline.BuildPlayer (options);
		Debug.Log ("Android build: " + report.summary.result + ", " + report.summary.totalSize + " bytes");
		EditorApplication.Exit (report.summary.result == BuildResult.Succeeded ? 0 : 1);
	}
}
