using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class RunnerSkySmokeTest
{
    private static int phase, checks;
    private static double deadline;
    private static RunnerGame game;
    private static Player player;
    private static RunnerSkyDirector director;
    private static RunnerSkyEnemy flyer;
    private static RunnerProjectile shot;
    private static Vector3 pausedPosition;
    private static int beforeKills, beforeCoins;
    static RunnerSkySmokeTest()
    {
        if (SessionState.GetBool("RunnerSkyTest", false)) EditorApplication.update += Tick;
    }
    [MenuItem("Tools/Runner/Test sky ghost and reflection")]
    public static void Run()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        phase = checks = 0;
        SessionState.SetBool("RunnerSkyTest", true);
        EditorSceneManager.OpenScene("Assets/Scenes/RunnerScene.unity");
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
        EditorApplication.isPlaying = true;
    }
    private static void Check(bool ok, string message)
    {
        if (!ok) throw new Exception(message);
        checks++; Debug.Log("SKY CHECK PASS: " + message);
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
                    game = RunnerGame.Instance; player = game.Player; game.Shadow.enabled = false;
                    director = game.GetComponent<RunnerSkyDirector>(); director.enabled = false;
                    player.enabled = false; player.rb.gravityScale = 0; player.zeroVelocity();
                    player.transform.position = new Vector3(100, 100, 0);
                    Camera.main.GetComponent<RunnerCamera>().enabled = false;
                    Camera.main.transform.position = new Vector3(100, 100, -10);
                    Check(game.Generator.SanctuarySpacing == 800 && game.Stage == 1, "rest stops are configured at 800m intervals");
                    flyer = director.SpawnNow();
                    flyer.GetComponent<RunnerEnemySalvage>().enabled = false;
                    Check(flyer != null && Camera.main.WorldToViewportPoint(flyer.transform.position).x > 1, "sky ghost enters from outside the upper-right edge");
                    Check(flyer.Momentum == null && flyer.rb.bodyType == RigidbodyType2D.Kinematic && !flyer.ShowHealthBar, "airborne enemy is independent of ground physics and hides full health");
                    Check(flyer.HoverFrames.Length == 8 && flyer.CastFrames.Length == 8 && flyer.DeathFrames.Length == 5, "existing ghost atlas provides hover cast and death frames");
                    Check(director.SpawnNow() == flyer, "director limits sky pressure to one active flyer");
                    Wait(1.75f); phase++; break;
                case 2:
                    Vector3 viewport = Camera.main.WorldToViewportPoint(flyer.transform.position);
                    Check(flyer.HasEntered && Mathf.Abs(viewport.x - .83f) < .02f && Mathf.Abs(viewport.y - .73f) < .03f, "flyer settles in the upper-right camera region");
                    Camera.main.transform.position += new Vector3(40, -12, 0);
                    Wait(.1f); phase++; break;
                case 3:
                    Check(Mathf.Abs(Camera.main.WorldToViewportPoint(flyer.transform.position).x - .83f) < .02f, "flyer follows the camera while the route descends");
                    player.transform.position = Camera.main.transform.position + new Vector3(-3, 0, 10); player.zeroVelocity();
                    Check(flyer.BeginCast() && flyer.IsCasting, "sky enemy telegraphs its aimed shot");
                    Wait(.9f); phase++; break;
                case 4:
                    shot = FindShot();
                    Check(shot != null && !shot.IsReflected && shot.Owner == flyer, "sky enemy emits a hostile ranged projectile after windup");
                    UnityEngine.Object.DestroyImmediate(shot.gameObject);
                    flyer.enabled = false;
                    player.transform.position = new Vector3(100, 100, 0); player.zeroVelocity();
                    flyer.transform.position = new Vector3(108, 104, 0);
                    if (player.facingDir < 0) player.Flip();
                    Physics2D.SyncTransforms();
                    shot = Bolt(new Vector3(101.4f, 100, 0), Vector2.left, 12);
                    CombatHit.Strike(player, player.attackDamage, player.attackSize, player.attackOffset);
                    Check(shot.IsReflected && shot.Owner == player && shot.SourceEnemy == flyer && shot.Direction.x > 0 && shot.Direction.y > 0, "sword strike returns the projectile toward its caster and changes ownership");
                    Check(!shot.TryReflect(player) && shot.Damage >= 50 && shot.Speed > player.MovementSpeed, "reflected projectile is stronger faster and cannot reflect twice");
                    Wait(.5f); phase++; break;
                case 5:
                    Check(shot == null && flyer.CurrentHealth == flyer.MaxHealth - 50 && player.CurrentHealth == player.MaxHealth, "returned projectile damages the flyer while ignoring the player");
                    Check(flyer.ShowHealthBar, "damaged flyer displays its health bar");
                    Wait(.2f); phase++; break;
                case 6:
                    player.stateMachine.ChangeState(player.primaryAttackState); player.OpenParryWindow(.2f);
                    shot = Bolt(new Vector3(104, 100, 0), Vector2.left, 12); shot.Speed = 200;
                    Wait(.04f); phase++; break;
                case 7:
                    Check(shot.IsReflected && player.CurrentHealth == player.MaxHealth, "swept sword window reflects fast incoming projectiles without tunneling");
                    if (shot != null) UnityEngine.Object.DestroyImmediate(shot.gameObject);
                    player.CloseParryWindow(); player.stateMachine.ChangeState(player.idleState);
                    shot = Bolt(new Vector3(103, 100, 0), Vector2.left, 12);
                    GameObject safe = new GameObject("Sky pause test"); RunnerPlatform p = safe.AddComponent<RunnerPlatform>();
                    p.Width = 30; p.Id = 930001; p.IsSafe = true; safe.transform.position = new Vector3(100, 98, 0);
                    Check(game.TryEnterSanctuary(p), "safe stop opens while sky pressure is present");
                    pausedPosition = shot.transform.position;
                    Check(!shot.TryReflect(player) && !flyer.BeginCast(), "shop blocks reflection and new enemy casts");
                    Wait(.3f); phase++; break;
                case 8:
                    Check(shot != null && shot.transform.position == pausedPosition, "sky projectiles pause in the shop");
                    game.LeaveSanctuary(); UnityEngine.Object.DestroyImmediate(shot.gameObject);
                    player.rb.gravityScale = 0; player.zeroVelocity();
                    shot = Bolt(new Vector3(101.3f, 100, 0), Vector2.left, 100);
                    beforeKills = game.Kills; beforeCoins = game.Coins;
                    Check(shot.TryReflect(player), "reflected killing shot can be prepared");
                    Wait(.7f); phase++; break;
                case 9:
                    Check(flyer.IsDead && shot == null && !flyer.ShowHealthBar, "reflected shot kills the flyer and hides its health bar");
                    Check(game.Kills == beforeKills + 1 && game.Coins == beforeCoins + 20 && game.Flow.Tier > 0, "reflected kill credits coins score and flow to the player once");
                    File.WriteAllText("sky-test-result.txt", "PASS: " + checks + " sky and reflection checks");
                    Debug.Log("SKY SMOKE TEST PASSED: " + checks); Finish(0); break;
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e); File.WriteAllText("sky-test-result.txt", "FAIL: " + e); Finish(1);
        }
    }
    private static RunnerProjectile FindShot()
    {
        foreach (RunnerProjectile unit in UnityEngine.Object.FindObjectsOfType<RunnerProjectile>()) if (unit.Owner == flyer) return unit;
        return null;
    }
    private static RunnerProjectile Bolt(Vector3 point, Vector2 direction, int damage)
    {
        RunnerProjectile unit = UnityEngine.Object.Instantiate(flyer.ProjectilePrefab, point, Quaternion.identity);
        unit.Lifetime = 4; unit.Initialize(flyer, direction, damage); return unit;
    }
    private static void Finish(int code)
    {
        SessionState.SetBool("RunnerSkyTest", false); EditorApplication.update -= Tick;
        if (Application.isBatchMode) EditorApplication.Exit(code); else EditorApplication.isPlaying = false;
    }
}
