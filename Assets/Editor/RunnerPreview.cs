using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class RunnerPreview
{
    private static int step;
    private static double deadline;
    static RunnerPreview()
    {
        if (SessionState.GetBool("RunnerPreview", false)) EditorApplication.update += Tick;
    }
    [MenuItem("Tools/Runner/Open runner scene")]
    public static void Open()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene("Assets/Scenes/RunnerScene.unity");
    }
    public static void Capture()
    {
        SessionState.SetBool("RunnerPreview", true);
        EditorSceneManager.OpenScene("Assets/Scenes/RunnerScene.unity");
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.isPlaying = true;
    }
    private static void Tick()
    {
        if (!Application.isPlaying) return;
        if (step == 0) { deadline = EditorApplication.timeSinceStartup + 1; step = 1; return; }
        if (EditorApplication.timeSinceStartup < deadline) return;
        Camera camera = Camera.main;
        RenderTexture target = new RenderTexture(1280, 720, 24);
        camera.targetTexture = target;
        camera.Render();
        RenderTexture.active = target;
        Texture2D pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        pixels.Apply();
        File.WriteAllBytes("runner-preview.png", pixels.EncodeToPNG());
        RenderTexture.active = null;
        camera.targetTexture = null;
        Object.DestroyImmediate(pixels);
        target.Release();
        Object.DestroyImmediate(target);
        SessionState.SetBool("RunnerPreview", false);
        EditorApplication.update -= Tick;
        if (Application.isBatchMode) EditorApplication.Exit(0);
        else EditorApplication.isPlaying = false;
    }
}
