using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class RunnerImpactSmokeTest
{
    private static int phase, checks;
    private static double deadline;
    private static RunnerGame game;
    private static Player player;
    private static Enemy[] crowd;
    private static RunnerPlatform a, b;
    static RunnerImpactSmokeTest()
    {
        if (SessionState.GetBool("RunnerImpactTest", false)) EditorApplication.update += Tick;
    }
    [MenuItem("Tools/Runner/Test crowd combat and respawn")]
    public static void Run()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        phase = checks = 0;
        SessionState.SetBool("RunnerImpactTest", true);
        EditorSceneManager.OpenScene("Assets/Scenes/RunnerScene.unity");
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
        EditorApplication.isPlaying = true;
    }
    private static void Check(bool ok, string message)
    {
        if (!ok) throw new Exception(message);
        checks++; Debug.Log("IMPACT CHECK PASS: " + message);
    }
    private static void Wait(float seconds) { deadline = EditorApplication.timeSinceStartup + seconds; }
    private static void Tick()
    {
        if (!Application.isPlaying || !EditorApplication.isPlaying) return;
        try
        {
            if (phase == 0) { Wait(.5f); phase++; return; }
            if (EditorApplication.timeSinceStartup < deadline) return;
            switch (phase)
            {
                case 1:
                    game = RunnerGame.Instance; player = game.Player;
                    game.Shadow.enabled = false;
                    Check(game.Generator.GetComponentsInChildren<Enemy>().Length >= 6, "initial route retains smaller enemy groups");
                    Check(game.Generator.GetComponentsInChildren<Enemy>().GroupBy(e => e.Momentum.Platform).Any(g => g.Count() == 3), "wide platforms retain three-enemy groups");
                    Check(game.Generator.EnemyVariants.All(e => e.MaxHealth >= 90) && game.Generator.EnemyPrefab.MaxHealth == 140, "all four species have increased health");
                    Check(player.attackSize.x >= 3.8f, "runner melee covers multiple packed targets");
                    Check(Physics2D.GetIgnoreLayerCollision(LayerMask.NameToLayer("Enemy"), LayerMask.NameToLayer("Enemy")), "enemy bodies can accumulate without blocking a shared knockback");
                    player.enabled = false; player.rb.gravityScale = 0;
                    a = Platform(100, 100, 10); b = Platform(116.5f, 97, 14);
                    player.transform.position = new Vector3(101.4f, 101.3f, 0); player.zeroVelocity();
                    if (player.facingDir < 0) player.Flip();
                    crowd = new Enemy[3];
                    for (int i = 0; i < 3; i++)
                    {
                        Enemy prefab = game.Generator.EnemyVariants.OfType<RunnerEnemy>().First(e => e.Kind == RunnerEnemyKind.Slime);
                        Enemy e = UnityEngine.Object.Instantiate(prefab, game.Generator.transform);
                        e.enabled = false; e.Momentum.Platform = a;
                        float feet = e.GetComponent<Collider2D>().bounds.min.y - e.transform.position.y;
                        e.transform.position = new Vector3(103 + i * .3f, a.Top - feet + .04f, 0);
                        crowd[i] = e;
                    }
                    Physics2D.SyncTransforms();
                    Check(crowd.All(e => !e.ShowHealthBar), "undamaged enemies hide their health bars");
                    Check(CombatHit.Strike(player, 25, player.attackSize, player.attackOffset) == 3, "one strike damages all three packed enemies");
                    Check(crowd.All(e => e.ShowHealthBar && e.CurrentHealth == e.MaxHealth - 25), "only hurt living enemies display health bars");
                    Check(crowd.All(e => e.rb.velocity.x >= 24 && e.IsStunned), "all targets receive forward sliding momentum together");
                    Check(UnityEngine.Object.FindObjectsOfType<RunnerHitImpact>().Length >= 3, "each simultaneous hit emits an impact burst");
                    Wait(.8f); phase++; break;
                case 2:
                    Check(crowd.All(e => e.transform.position.x > b.Left && e.Momentum.Platform == b), "three enemies physically cross a 4.5m gap and land on the lower platform");
                    Check(crowd.Max(e => e.transform.position.x) - crowd.Min(e => e.transform.position.x) < 1.5f, "knocked enemies remain packed on the next platform");
                    UnityEngine.Object.DestroyImmediate(a.gameObject);
                    Check(crowd.All(e => e != null && e.transform.parent == game.Generator.transform), "recycling their original platform does not delete pushed enemies");
                    player.transform.position = new Vector3(crowd.Min(e => e.transform.position.x) - 1.4f, 98.3f, 0);
                    Physics2D.SyncTransforms();
                    Check(CombatHit.Strike(player, 25, player.attackSize, player.attackOffset) == 3, "a second strike pushes the packed group together again");
                    Check(crowd.All(e => e.rb.velocity.x >= 24 && e.CurrentHealth == 40), "increased health supports repeated group knockbacks");
                    game.RecordLanding(b);
                    game.Flow.NotifyJump(); game.Flow.AddTier();
                    int hp = player.CurrentHealth;
                    player.transform.position = new Vector3(116, -60, 0); player.rb.velocity = new Vector2(20, -40);
                    game.RespawnFromFall();
                    Check(!game.IsGameOver && game.Falls == 1 && player.CurrentHealth == hp, "fall respawns without ending the run or reducing health");
                    Check(player.transform.position.x >= b.Left && player.transform.position.x <= b.Right && player.GetComponent<Collider2D>().bounds.min.y >= b.Top, "respawn returns to the last landed platform");
                    Check(player.rb.velocity == Vector2.zero && game.Flow.Tier == 0 && !player.isBusy && !player.isJumping, "respawn clears velocity flow and stale attack or jump state");
                    Check(!player.TakeDamage(10, player.transform.position + Vector3.left), "respawn grants brief damage protection");
                    Check(game.Shadow.Distance >= 18, "respawn gives the shadow a safe chase gap");
                    float relativeX = player.transform.position.x - b.transform.position.x;
                    b.transform.position += new Vector3(-2000, 560, 0);
                    game.RespawnFromFall();
                    Check(Mathf.Abs(player.transform.position.x - b.transform.position.x - relativeX) < .01f, "checkpoint tracks its platform after a world-coordinate shift");
                    File.WriteAllText("impact-test-result.txt", "PASS: " + checks + " crowd and respawn checks");
                    Debug.Log("IMPACT SMOKE TEST PASSED: " + checks); Finish(0); break;
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e); File.WriteAllText("impact-test-result.txt", "FAIL: " + e); Finish(1);
        }
    }
    private static RunnerPlatform Platform(float x, float y, float width)
    {
        GameObject root = new GameObject("Impact test platform"); root.layer = LayerMask.NameToLayer("Ground");
        root.transform.position = new Vector3(x, y, 0);
        RunnerPlatform p = root.AddComponent<RunnerPlatform>(); p.Width = width; p.Id = 950000 + (int)x;
        BoxCollider2D body = root.AddComponent<BoxCollider2D>(); body.size = new Vector2(width, 1); body.offset = new Vector2(0, -.5f);
        return p;
    }
    private static void Finish(int code)
    {
        SessionState.SetBool("RunnerImpactTest", false); EditorApplication.update -= Tick;
        if (Application.isBatchMode) EditorApplication.Exit(code); else EditorApplication.isPlaying = false;
    }
}
