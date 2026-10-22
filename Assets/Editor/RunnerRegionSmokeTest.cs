using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class RunnerRegionSmokeTest
{
    private static int phase, checks;
    private static double deadline;
    private static RunnerGame game;
    private static Player player;
    private static RunnerPlatform arena;
    private static Vector3 cameraPosition;
    private static float routeBefore, previousBest;
    private static Enemy stale;
    static RunnerRegionSmokeTest()
    {
        if (SessionState.GetBool("RunnerRegionTest", false)) EditorApplication.update += Tick;
    }
    [MenuItem("Tools/Runner/Test progress regions and arena")]
    public static void Run()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        phase = checks = 0; SessionState.SetBool("RunnerRegionTest", true);
        EditorSceneManager.OpenScene("Assets/Scenes/RunnerScene.unity");
        EditorApplication.update -= Tick; EditorApplication.update += Tick; EditorApplication.isPlaying = true;
    }
    private static void Check(bool ok, string message)
    {
        if (!ok) throw new Exception(message);
        checks++; Debug.Log("REGION CHECK PASS: " + message);
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
                    game.enabled = false; game.Shadow.enabled = false; game.GetComponent<RunnerSkyDirector>().enabled = false;
                    player.enabled = false; player.rb.simulated = false;
                    previousBest = game.HistoricalBest;
                    Check(game.GetComponent<RunnerProgressHUD>() != null && RunnerProgressHUD.Frame.width == 640 && RunnerProgressHUD.WindowRadius == 500,
                        "progress slot occupies half the screen and covers five hundred meters each way");
                    Check(RunnerProgressHUD.Normalize(2000, 2000) == .5f && RunnerProgressHUD.Normalize(1500, 2000) == 0 &&
                        RunnerProgressHUD.Normalize(2500, 2000) == 1, "player stays centered with correct forward and backward distances");
                    Check(RunnerProgressHUD.IsBossStop(3) && RunnerProgressHUD.IsBossStop(6) && !RunnerProgressHUD.IsBossStop(2),
                        "every third rest stop marks a boss node");
                    Check(game.Regions != null && game.Regions.Regions.Length >= 2 && game.CurrentRegion.Name == "旧城", "run starts with a valid city region catalog");
                    game.Generator.EnsureAhead(2390);
                    arena = game.Generator.PendingArena;
                    Check(arena != null && arena.IsBossGate && arena.IsSafe && arena.Width == 36, "third rest stop generates a large boss arena base");
                    Check(arena.MilestoneDistance == 2400 && game.Generator.SanctuaryMarkers.Count == 3,
                        "actual rest and boss marker positions are registered in absolute meters");
                    Check(game.Generator.Platforms.Count(p => p.IsArenaPlatform && p.IsBranch) == 4, "arena includes four elevated jump platforms");
                    int count = game.Generator.GeneratedCount;
                    game.Generator.EnsureAhead(5000);
                    Check(game.Generator.GeneratedCount == count, "generation waits at the region boundary until the arena exit");
                    Place(arena.Left + 3, arena.Top + 1.4f);
                    Check(game.TryEnterSanctuary(arena) && game.IsShopping, "third rest stop still offers its shop before the arena");
                    game.LeaveSanctuary();
                    Check(game.InBossArena && !game.IsPaused && player.rb.simulated, "leaving the shop activates a playable arena without pausing the player");
                    Check(!game.Shadow.gameObject.activeSelf && UnityEngine.Object.FindObjectsOfType<Enemy>().Length == 0,
                        "arena removes ordinary enemies and the pursuing shadow without creating a boss opponent");
                    Check(game.GetComponent<RunnerSkyDirector>().SpawnNow() == null, "sky enemies cannot enter the boss arena preview");
                    float shadow = game.Shadow.X; game.Shadow.Advance(10);
                    Check(game.Shadow.X == shadow, "shadow simulation remains stopped in the arena");
                    Check(Camera.main.GetComponent<RunnerCamera>().ArenaLocked, "arena camera uses a fixed view");
                    cameraPosition = Camera.main.transform.position;
                    game.enabled = true;
                    player.rb.velocity = new Vector2(8, player.jumpForce);
                    Wait(.45f); phase++; break;
                case 2:
                    Check((Camera.main.transform.position - cameraPosition).sqrMagnitude < .001f, "camera stays fixed while the player moves and jumps");
                    Check(player.transform.position.y > arena.Top + 2 && player.transform.position.x > arena.Left + 4,
                        "player can physically jump and move inside the fixed arena");
                    float half = Camera.main.orthographicSize * Camera.main.aspect;
                    Check(half >= arena.Width * .5f + 1, "fixed camera fits the whole main platform with margins");
                    Check(!game.ContinueToNextRegion(), "region exit cannot activate from the middle of the arena");
                    player.rb.simulated = false; Place(arena.Right - 2, arena.Top + 1.4f);
                    int coins = game.Coins, health = player.CurrentHealth;
                    Check(game.AtArenaExit && game.ContinueToNextRegion(), "right-side exit advances the region without pretending a boss fight occurred");
                    Check(game.RegionIndex == 1 && game.RegionName == "暮色森林" && !game.InBossArena, "first area ending switches to the forest region");
                    Check(!Camera.main.GetComponent<RunnerCamera>().ArenaLocked && game.Shadow.gameObject.activeSelf,
                        "normal camera following and shadow chase resume after the exit");
                    Check(game.Coins == coins && player.CurrentHealth == health, "area transition preserves player health and currency");
                    Enemy[] spawned = game.Generator.GetComponentsInChildren<Enemy>();
                    Check(spawned.Length > 0 && spawned.All(e => e is RunnerEnemy unit &&
                        (unit.Kind == RunnerEnemyKind.Slime || unit.Kind == RunnerEnemyKind.Reaver)), "forest generation uses its slime and reaver roster");
                    routeBefore = game.RoutePosition;
                    Wait(.2f); phase++; break;
                case 3:
                    Check(game.DistanceOffset == 2000 && Mathf.Abs(game.RoutePosition - routeBefore) < 1,
                        "area transition survives world-coordinate rebasing without losing route progress");
                    Check(game.HistoricalBest == previousBest && game.BestDistance >= game.Distance,
                        "previous-run best remains a stable marker while current record advances");
                    RunnerBackdrop forest = UnityEngine.Object.FindObjectOfType<RunnerBackdrop>();
                    Check(forest != null && forest.Sky.sprite.texture.name == "background_layer_1", "forest background replaces the city composition");
                    game.enabled = false;
                    game.Generator.EnsureAhead(4790 - game.DistanceOffset);
                    RunnerPlatform next = game.Generator.PendingArena;
                    Check(next != null && next.MilestoneDistance == 4800, "next boss gate follows the sixth rest stop");
                    Place(next.Left + 3, next.Top + 1.4f);
                    Check(game.TryEnterSanctuary(next), "next region retains its ending shop"); game.LeaveSanctuary();
                    Place(next.Right - 2, next.Top + 1.4f);
                    Check(game.ContinueToNextRegion() && game.RegionIndex == 2 && game.CurrentRegion.Name == "旧城",
                        "region themes cycle with a different monster roster at each exit");
                    Check(game.CurrentRegion.Enemies.Length == 4, "returning city restores the four-species roster");
                    Check(game.Generator.Platforms.Where(p => p.IsArenaPlatform).All(p => p.ArenaOwnerId == next.Id),
                        "completed older arenas are recycled while only the current gate stays protected");
                    TestRewardRules();
                    stale = SpawnEnemy(); stale.GetComponent<RunnerEnemySalvage>().enabled = false;
                    stale.rb.simulated = false; stale.Momentum.Launch(1, 24);
                    Wait(3.3f); phase++; break;
                case 4:
                    Check(!stale.Momentum.IsPlayerDisplaced && !game.ConvertEnemyToGold(stale),
                        "an old hit does not turn a later abandoned enemy into free gold");
                    File.WriteAllText("region-test-result.txt", "PASS: " + checks + " progress region arena and reward checks");
                    Debug.Log("REGION SMOKE TEST PASSED: " + checks); Finish(0); break;
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e); File.WriteAllText("region-test-result.txt", "FAIL: " + e); Finish(1);
        }
    }
    private static void TestRewardRules()
    {
        Camera.main.GetComponent<RunnerCamera>().enabled = false;
        Camera.main.transform.position = new Vector3(0, 100, -10); Place(0, 100);
        int wallet = game.Coins;
        Enemy untouched = SpawnEnemy();
        Check(!game.ConvertEnemyToGold(untouched), "simply outrunning an untouched enemy awards no coins");
        untouched.GetComponent<RunnerEnemySalvage>().Convert();
        Check(!untouched.gameObject.activeSelf && game.Coins == wallet && UnityEngine.Object.FindObjectsOfType<RunnerGoldFlight>().Length == 0,
            "abandoned enemy is cleaned up without creating a gold flight");
        Enemy pushed = SpawnEnemy(); pushed.Momentum.Launch(1, 24);
        Check(game.ConvertEnemyToGold(pushed), "a player-launched enemy can become gold when leaving the view");
        RunnerGoldFlight gold = UnityEngine.Object.FindObjectOfType<RunnerGoldFlight>();
        gold.Collect();
        Check(game.Coins == wallet + 7 && !game.ConvertEnemyToGold(pushed), "launched enemy reward is paid only once");
    }
    private static Enemy SpawnEnemy()
    {
        Enemy enemy = UnityEngine.Object.Instantiate(game.Generator.EnemyPrefab, new Vector3(2, 100, 0), Quaternion.identity);
        enemy.enabled = false; enemy.rb.gravityScale = 0; enemy.RunnerCoinValue = 7; return enemy;
    }
    private static void Place(float x, float y)
    {
        player.transform.position = new Vector3(x, y, 0); player.rb.position = player.transform.position;
        player.zeroVelocity(); Physics2D.SyncTransforms();
    }
    private static void Finish(int code)
    {
        SessionState.SetBool("RunnerRegionTest", false); EditorApplication.update -= Tick;
        if (Application.isBatchMode) EditorApplication.Exit(code); else EditorApplication.isPlaying = false;
    }
}
