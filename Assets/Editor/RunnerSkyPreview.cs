using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class RunnerSkyPreview
{
    private static int phase;
    private static double deadline;
    private static RunnerSkyEnemy flyer;
    static RunnerSkyPreview()
    {
        if (SessionState.GetBool("RunnerSkyPreview", false)) EditorApplication.update += Tick;
    }
    public static void Capture()
    {
        SessionState.SetBool("RunnerSkyPreview", true);
        EditorSceneManager.OpenScene("Assets/Scenes/RunnerScene.unity");
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
        EditorApplication.isPlaying = true;
    }
    private static void Tick()
    {
        if (!Application.isPlaying) return;
        if (phase == 0) { deadline = EditorApplication.timeSinceStartup + .6; phase++; return; }
        if (EditorApplication.timeSinceStartup < deadline) return;
        if (phase == 1)
        {
            RunnerGame game = RunnerGame.Instance;
            game.Player.enabled = false; game.Player.zeroVelocity();
            game.Shadow.enabled = false;
            RunnerSkyDirector director = game.GetComponent<RunnerSkyDirector>(); director.enabled = false;
            flyer = director.SpawnNow();
            deadline = EditorApplication.timeSinceStartup + 1.7; phase++; return;
        }
        if (phase == 2)
        {
            flyer.BeginCast();
            deadline = EditorApplication.timeSinceStartup + 1; phase++; return;
        }
        Camera camera = Camera.main;
        RenderTexture target = new RenderTexture(1280, 720, 24);
        camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
        Texture2D pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
        File.WriteAllBytes("RUNNER_SKY_PREVIEW.png", pixels.EncodeToPNG());
        RenderTexture.active = null; camera.targetTexture = null;
        Object.DestroyImmediate(pixels); target.Release(); Object.DestroyImmediate(target);
        SessionState.SetBool("RunnerSkyPreview", false); EditorApplication.update -= Tick;
        Debug.Log("SKY PREVIEW CAPTURED"); EditorApplication.Exit(0);
    }
}
