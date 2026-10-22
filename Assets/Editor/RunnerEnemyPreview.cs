using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class RunnerEnemyPreview
{
    private static int step;
    private static double deadline;
    static RunnerEnemyPreview()
    {
        if (SessionState.GetBool("RunnerEnemyPreview", false)) EditorApplication.update += Tick;
    }
    public static void Capture()
    {
        SessionState.SetBool("RunnerEnemyPreview", true);
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
        if (step == 1)
        {
            RunnerGame game = RunnerGame.Instance;
            game.Shadow.enabled = false;
            game.Shadow.gameObject.SetActive(false);
            foreach (Enemy old in Object.FindObjectsOfType<Enemy>()) old.gameObject.SetActive(false);
            game.Player.enabled = false;
            game.Player.rb.simulated = false;
            Place(game.Player, -5);
            string[] names = { "RunnerSlime", "RunnerReaver", "RunnerNecromancer" };
            for (int i = 0; i < names.Length; i++)
            {
                GameObject prefab = Resources.Load<GameObject>("Runner/Enemies/" + names[i]);
                RunnerEnemy enemy = Object.Instantiate(prefab).GetComponent<RunnerEnemy>();
                enemy.enabled = false;
                Place(enemy, i * 4.5f);
                enemy.rb.simulated = false;
                enemy.anim.Play("Idle", 0, 0);
            }
            Camera camera = Camera.main;
            camera.GetComponent<RunnerCamera>().enabled = false;
            camera.orthographicSize = 6.5f;
            camera.transform.position = new Vector3(2, 2.5f, -10);
            deadline = EditorApplication.timeSinceStartup + .3;
            step = 2;
            return;
        }
        Camera view = Camera.main;
        RenderTexture target = new RenderTexture(1280, 720, 24);
        view.targetTexture = target;
        view.Render();
        RenderTexture.active = target;
        Texture2D pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        pixels.Apply();
        File.WriteAllBytes("RUNNER_ENEMIES_PREVIEW.png", pixels.EncodeToPNG());
        RenderTexture.active = null;
        view.targetTexture = null;
        Object.DestroyImmediate(pixels);
        target.Release();
        Object.DestroyImmediate(target);
        SessionState.SetBool("RunnerEnemyPreview", false);
        EditorApplication.update -= Tick;
        Debug.Log("ENEMY PREVIEW CAPTURED");
        EditorApplication.Exit(0);
    }
    private static void Place(Entity entity, float x)
    {
        Collider2D body = entity.GetComponent<Collider2D>();
        float feet = body.bounds.min.y - entity.transform.position.y;
        entity.transform.position = new Vector3(x, .04f - feet, 0);
        entity.rb.velocity = Vector2.zero;
    }
}
