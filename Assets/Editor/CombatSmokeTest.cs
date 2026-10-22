using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Run from Tools/Combat/Run smoke test, or -executeMethod CombatSmokeTest.Run.
[InitializeOnLoad]
public static class CombatSmokeTest
{
    private static int phase;
    private static double deadline;
    private static Player player;
    private static Enemy_Skeleton enemy;
    private static int checks;
    static CombatSmokeTest()
    {
        if (SessionState.GetBool("CombatSmokeTest", false)) EditorApplication.update += Tick;
    }

    [MenuItem("Tools/Combat/Run smoke test")]
    public static void Run()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        phase = 0;
        checks = 0;
        SessionState.SetBool("CombatSmokeTest", true);
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.isPlaying = true;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        checks++;
        Debug.Log("COMBAT CHECK PASS: " + message);
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || !Application.isPlaying) return;
        try
        {
            if (phase == 0) { deadline = EditorApplication.timeSinceStartup + 2; phase = 1; return; }
            if (EditorApplication.timeSinceStartup < deadline) return;
            switch (phase)
            {
                case 1:
                    player = UnityEngine.Object.FindObjectOfType<Player>();
                    enemy = UnityEngine.Object.FindObjectOfType<Enemy_Skeleton>();
                    Check(UnityEngine.Object.FindObjectsOfType<Player>().Length == 1, "scene contains exactly one Player");
                    Check(player != null && enemy != null && player.CurrentHealth == 100 && enemy.CurrentHealth == 100, "scene health initializes");
                    foreach (string state in new[] { "skeletonAttack", "skeletonHit", "skeletonDead" })
                        Check(enemy.anim.HasState(0, Animator.StringToHash("Base Layer." + state)), "animation state " + state);
                    player.enabled = false;
                    enemy.enabled = false;
                    player.rb.gravityScale = enemy.rb.gravityScale = 0;
                    player.zeroVelocity(); enemy.zeroVelocity();
                    player.transform.position = new Vector3(0, 100, 0);
                    enemy.transform.position = new Vector3(1.2f, 100, 0);
                    if (player.facingDir < 0) player.Flip();
                    Physics2D.SyncTransforms();
                    Check(CombatHit.Strike(player, 25, player.attackSize, player.attackOffset) == 1 && enemy.CurrentHealth == 75, "frontal melee damages enemy");
                    Check(!enemy.TakeDamage(25, player.transform.position), "immediate repeated damage is rejected");
                    deadline = EditorApplication.timeSinceStartup + .3; phase++; break;
                case 2:
                    player.zeroVelocity(); enemy.zeroVelocity();
                    player.transform.position = new Vector3(0, 100, 0);
                    enemy.transform.position = new Vector3(1.2f, 100, 0);
                    Physics2D.SyncTransforms();
                    player.Flip();
                    Check(CombatHit.Strike(player, 25, player.attackSize, player.attackOffset) == 0, "attack does not hit behind player");
                    player.Flip();
                    GameObject wall = new GameObject("SmokeTestWall");
                    wall.layer = LayerMask.NameToLayer("Wall");
                    wall.transform.position = new Vector3(.6f, 100, 0);
                    wall.AddComponent<BoxCollider2D>().size = new Vector2(.1f, 4);
                    Physics2D.SyncTransforms();
                    Check(CombatHit.Strike(player, 25, player.attackSize, player.attackOffset) == 0, "wall blocks melee damage");
                    UnityEngine.Object.DestroyImmediate(wall);
                    enemy.gameObject.AddComponent<BoxCollider2D>();
                    Physics2D.SyncTransforms();
                    Check(CombatHit.Strike(player, 25, player.attackSize, player.attackOffset) == 1 && enemy.CurrentHealth == 50, "multiple colliders receive one hit per swing");
                    deadline = EditorApplication.timeSinceStartup + .3; phase++; break;
                case 3:
                    Check(enemy.TakeDamage(1000, player.transform.position) && enemy.IsDead && !enemy.rb.simulated, "lethal damage disables enemy physics");
                    Check(!enemy.GetComponent<Collider2D>().enabled, "dead enemy collider is disabled");
                    Check(CombatHit.Strike(enemy, 100, enemy.attackSize, enemy.attackOffset) == 0, "dead attacker cannot deal damage");
                    Check(player.TakeDamage(20, enemy.transform.position) && player.CurrentHealth == 80, "enemy damage reduces player health");
                    Check(!player.TakeDamage(20, enemy.transform.position), "player has recovery invulnerability");
                    deadline = EditorApplication.timeSinceStartup + .7; phase++; break;
                case 4:
                    Check(player.TakeDamage(1000, enemy.transform.position) && player.IsDead && !player.rb.simulated, "player death disables actions and physics");
                    player.GetComponent<CombatHUD>().Restart();
                    deadline = EditorApplication.timeSinceStartup + 1; phase++; break;
                case 5:
                    player = UnityEngine.Object.FindObjectOfType<Player>();
                    enemy = UnityEngine.Object.FindObjectOfType<Enemy_Skeleton>();
                    Check(player != null && enemy != null && !player.IsDead && !enemy.IsDead && player.CurrentHealth == 100 && enemy.CurrentHealth == 100, "restart resets player and enemy");
                    enemy.enabled = false;
                    player.rb.gravityScale = enemy.rb.gravityScale = 0;
                    player.transform.position = new Vector3(0, 100, 0);
                    enemy.transform.position = new Vector3(1.2f, 100, 0);
                    player.zeroVelocity(); enemy.zeroVelocity();
                    if (player.facingDir < 0) player.Flip();
                    Physics2D.SyncTransforms();
                    player.stateMachine.ChangeState(player.primaryAttackState);
                    deadline = EditorApplication.timeSinceStartup + .5; phase++; break;
                case 6:
                    Check(enemy.CurrentHealth == 75, "player attack state delivers exactly one timed hit");
                    player.enabled = false;
                    player.zeroVelocity(); enemy.zeroVelocity();
                    enemy.enabled = true;
                    enemy.FlipController(-1);
                    player.transform.position = new Vector3(0, 100, 0);
                    enemy.transform.position = new Vector3(1.2f, 100, 0);
                    Physics2D.SyncTransforms();
                    deadline = EditorApplication.timeSinceStartup + .2; phase++; break;
                case 7:
                    Check(player.CurrentHealth == 100, "enemy attack windup deals no early damage");
                    deadline = EditorApplication.timeSinceStartup + .55; phase++; break;
                case 8:
                    Check(player.CurrentHealth == 80, "enemy attack state hits after windup");
                    Check(player.IsStunned && player.anim.speed == 0, "enemy hit interrupts player action");
                    Check(enemy.TakeDamage(10, player.transform.position) && enemy.IsStunned, "enemy attack can be interrupted by a hit");
                    Check(CombatHit.Strike(enemy, 20, enemy.attackSize, enemy.attackOffset) == 0, "interrupted enemy cannot deal attack damage");
                    File.WriteAllText("combat-test-result.txt", "PASS: " + checks + " combat checks");
                    Debug.Log("COMBAT SMOKE TEST PASSED: " + checks);
                    Finish(0); break;
            }
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            File.WriteAllText("combat-test-result.txt", "FAIL: " + error);
            Finish(1);
        }
    }

    private static void Finish(int code)
    {
        SessionState.SetBool("CombatSmokeTest", false);
        EditorApplication.update -= Tick;
        if (Application.isBatchMode) EditorApplication.Exit(code);
        else EditorApplication.isPlaying = false;
    }
}
