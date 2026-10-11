using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

//batch build for the web: Unity -batchmode -quit -executeMethod WebGLBuild.Build -buildOutput <folder>
public static class WebGLBuild
{
	public static void Build ()
	{
		string output = "Build/WebGL";
		string[] args = System.Environment.GetCommandLineArgs ();
		for (int i = 0; i < args.Length - 1; i++) {
			if (args [i] == "-buildOutput") {
				output = args [i + 1];
			}
		}
		BuildPlayerOptions options = new BuildPlayerOptions ();
		options.scenes = new string[] { "Assets/Scenes/Card Deck Test.unity" };
		options.locationPathName = output;
		options.target = BuildTarget.WebGL;
		options.options = BuildOptions.None;
		BuildReport report = BuildPipeline.BuildPlayer (options);
		Debug.Log ("WebGL build: " + report.summary.result + ", " + report.summary.totalSize + " bytes");
		EditorApplication.Exit (report.summary.result == BuildResult.Succeeded ? 0 : 1);
	}
}
