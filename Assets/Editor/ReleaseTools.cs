using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ReleaseTools
{
    private const string ScenePath = "Assets/Scenes/Beethoven5Demo.unity";
    private const string BuildDirectory = "Builds/Windows/v0.0.1";
    private const string ExecutableName = "OrchestraConductor.exe";
    private const string ScreenshotPath = "Docs/Images/screenshot-v0.0.1.png";

    [MenuItem("Tools/Orchestra Conductor/Release/Build Windows v0.0.1")]
    public static void BuildWindowsRelease()
    {
        EnsureSceneExists();
        Directory.CreateDirectory(BuildDirectory);

        BuildPlayerOptions options = new()
        {
            scenes = new[] { ScenePath },
            locationPathName = Path.Combine(BuildDirectory, ExecutableName),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new BuildFailedException($"Windows build failed: {report.summary.result}");
        }

        Debug.Log($"Windows release build created at {Path.GetFullPath(BuildDirectory)}");
    }

    [MenuItem("Tools/Orchestra Conductor/Release/Capture README Screenshot")]
    public static void CaptureReadmeScreenshot()
    {
        EnsureSceneExists();
        EditorSceneManager.OpenScene(ScenePath);

        Camera camera = Object.FindFirstObjectByType<Camera>();
        if (camera == null)
        {
            throw new MissingReferenceException("No camera was found in Beethoven5Demo scene.");
        }

        string screenshotDirectory = Path.GetDirectoryName(ScreenshotPath) ?? "Docs";
        Directory.CreateDirectory(screenshotDirectory);

        RenderTexture renderTexture = new(1920, 1080, 24, RenderTextureFormat.ARGB32);
        Texture2D screenshot = new(1920, 1080, TextureFormat.RGB24, false);
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;

        try
        {
            camera.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;
            camera.Render();

            screenshot.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            screenshot.Apply();

            File.WriteAllBytes(ScreenshotPath, screenshot.EncodeToPNG());
            AssetDatabase.Refresh();
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            Object.DestroyImmediate(renderTexture);
            Object.DestroyImmediate(screenshot);
        }

        Debug.Log($"README screenshot saved to {Path.GetFullPath(ScreenshotPath)}");
    }

    public static void BuildWindowsReleaseFromCommandLine()
    {
        BuildWindowsRelease();
    }

    public static void CaptureReadmeScreenshotFromCommandLine()
    {
        CaptureReadmeScreenshot();
    }

    private static void EnsureSceneExists()
    {
        if (!File.Exists(ScenePath))
        {
            Beethoven5DemoSceneBuilder.CreateScene();
            AssetDatabase.SaveAssets();
        }
    }
}
