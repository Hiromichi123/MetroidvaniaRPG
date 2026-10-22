using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class RunnerEconomySmokeTest
{
    private static int phase, checks, wallet;
    private static double deadline;
    private static RunnerGame game;
    private static Player player;
    private static Enemy victim;
    private static RunnerGoldFlight gold;
    static RunnerEconomySmokeTest()
    {
        if (SessionState.GetBool("RunnerEconomyTest", false)) EditorApplication.update += Tick;
    }
    [MenuItem("Tools/Runner/Test income and uninterrupted attacks")]
    public static void Run()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        phase = checks = 0;
        SessionState.SetBool("RunnerEconomyTest", true);
        EditorSceneManager.OpenScene("Assets/Scenes/RunnerScene.unity");
        EditorApplication.update -= Tick; EditorApplication.update += Tick; EditorApplication.isPlaying = true;
    }
    private static void Check(bool ok, string message)
    {
        if (!ok) throw new Exception(message);
        checks++; Debug.Log("ECONOMY CHECK PASS: " + message);
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
                    game.Shadow.enabled = false; game.GetComponent<RunnerSkyDirector>().enabled = false;
                    player.enabled = false; player.rb.gravityScale = 0;
                    player.transform.position = new Vector3(100, 100, 0); player.zeroVelocity();
                    CheckIncome();
                    Check(RunnerGame.ItemCosts.Min() >= 110 && RunnerGame.ItemCosts.Max() <= 220 && RunnerGame.RerollCost == 50,
                        "shop prices scale to the new stage income");
                    player.stateMachine.ChangeState(player.primaryAttackState);
                    player.rb.velocity = new Vector2(12, 3);
                    Check(Mathf.Approximately(player.anim.speed, 1.45f), "runner attacks animate forty-five percent faster");
                    Check(player.TakeDamage(10, player.transform.position + Vector3.left), "enemy damage still reduces health");
                    Check(player.CurrentHealth == player.MaxHealth - 10 && !player.IsStunned && player.stateMachine.currentState == player.primaryAttackState &&
                        player.rb.velocity == new Vector2(12, 3) && Mathf.Approximately(player.anim.speed, 1.45f),
                        "damage preserves attack state animation and movement without knockback or interruption");
                    Check(!player.TakeDamage(10, player.transform.position), "damage protection still prevents overlapping hits");
                    player.stateMachine.ChangeState(player.idleState); player.zeroVelocity();
                    Camera.main.GetComponent<RunnerCamera>().enabled = false;
                    Camera.main.transform.position = new Vector3(100, 100, -10);
                    victim = UnityEngine.Object.Instantiate(game.Generator.EnemyPrefab, new Vector3(104, 100, 0), Quaternion.identity, game.Generator.transform);
                    victim.enabled = false; victim.rb.gravityScale = 0; victim.zeroVelocity(); victim.RunnerCoinValue = 9;
                    wallet = game.Coins;
                    Wait(.4f); phase++; break;
                case 2:
                    victim.Momentum.Launch(1, 24);
                    victim.transform.position = new Vector3(100, 75, 0);
                    Wait(.1f); phase++; break;
                case 3:
                    gold = UnityEngine.Object.FindObjectsOfType<RunnerGoldFlight>().FirstOrDefault(g => g.Amount == 9);
                    Check(gold != null && (victim == null || !victim.gameObject.activeSelf), "seen enemy leaving the screen becomes a flying gold bundle");
                    Check(game.Coins == wallet, "offscreen conversion waits for the gold flight before adding currency");
                    Wait(1.3f); phase++; break;
                case 4:
                    Check(gold == null && game.Coins == wallet + 9 && game.TotalIncome >= 9, "gold crosses the distance and is absorbed exactly once");
                    victim = UnityEngine.Object.Instantiate(game.Generator.EnemyPrefab, new Vector3(101.2f, 100, 0), Quaternion.identity);
                    victim.enabled = false; victim.rb.gravityScale = 0; victim.RunnerCoinValue = 7;
                    Physics2D.SyncTransforms();
                    int coins = game.Coins, kills = game.Kills;
                    CombatHit.Strike(player, 1000, player.attackSize, player.attackOffset);
                    Check(victim.IsDead && game.Coins == coins + 7 && game.Kills == kills + 1, "direct kills use the generated reward value");
                    Check(!game.ConvertEnemyToGold(victim), "dead enemy cannot also produce offscreen gold");
                    game.RewardKill(victim, player);
                    Check(game.Coins == coins + 7 && game.Kills == kills + 1, "kill and salvage share a single reward claim");
                    gold = RunnerGoldFlight.Spawn(new Vector3(120, 100, 0), 13);
                    gold.Collect(); gold.Collect();
                    Check(game.Coins == coins + 20, "gold bundles cannot be collected twice");
                    File.WriteAllText("economy-test-result.txt", "PASS: " + checks + " economy and damage checks");
                    Debug.Log("ECONOMY SMOKE TEST PASSED: " + checks); Finish(0); break;
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e); File.WriteAllText("economy-test-result.txt", "FAIL: " + e); Finish(1);
        }
    }
    private static void CheckIncome()
    {
        float sum = 0; int samples = 0, minimum = int.MaxValue, maximum = 0;
        for (int seed = 1; seed <= 12; seed++)
        {
            GameObject root = new GameObject("Income supply " + seed);
            RunnerGenerator generator = root.AddComponent<RunnerGenerator>();
            generator.Seed = seed; generator.Ahead = 1650; generator.Behind = 10000;
            generator.EnemyPrefab = game.Generator.EnemyPrefab; generator.EnemyVariants = game.Generator.EnemyVariants;
            generator.Initialize(game);
            Enemy[] enemies = root.GetComponentsInChildren<Enemy>();
            RunnerPickup[] pickups = root.GetComponentsInChildren<RunnerPickup>();
            Check(enemies.GroupBy(e => e.Momentum.Platform).All(g => g.Count() <= 3), "seed " + seed + " keeps enemy crowds at one to three");
            for (int stage = 0; stage < 2; stage++)
            {
                float left = stage * 800, right = left + 800;
                int enemyGold = enemies.Where(e => e.transform.position.x >= left && e.transform.position.x < right).Sum(e => e.GoldValue);
                int routeGold = pickups.Where(p => p.transform.position.x >= left && p.transform.position.x < right).Sum(p => p.Value);
                int income = enemyGold + routeGold + 20; // One sky kill per stage; optional extra kills remain a bonus.
                minimum = Mathf.Min(minimum, income); maximum = Mathf.Max(maximum, income); sum += income; samples++;
            }
            UnityEngine.Object.DestroyImmediate(root);
        }
        float average = sum / samples;
        File.WriteAllText("income-balance-result.txt", $"12 seeds / {samples} stages; mean {average:0.0}; range {minimum}-{maximum}; full route supply + one sky kill");
        Debug.Log($"INCOME AUDIT: average {average:0.0}; range {minimum}-{maximum}");
        Check(average >= 400 && average <= 500, "mean stage supply falls between four and five hundred coins");
        Check(minimum >= 370 && maximum <= 530, "individual stage supply stays near the income target");
    }
    private static void Finish(int code)
    {
        SessionState.SetBool("RunnerEconomyTest", false); EditorApplication.update -= Tick;
        if (Application.isBatchMode) EditorApplication.Exit(code); else EditorApplication.isPlaying = false;
    }
}
