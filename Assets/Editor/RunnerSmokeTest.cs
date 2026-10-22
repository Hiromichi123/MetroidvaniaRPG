using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class RunnerSmokeTest
{
    private static int phase, checks;
    private static double deadline;
    private static RunnerGame game;
    private static Player player;
    private static float pausedFlow, pausedShadow;
    private static GameObject scratch;
    static RunnerSmokeTest()
    {
        if (SessionState.GetBool("RunnerSmokeTest", false)) EditorApplication.update += Tick;
    }

    [MenuItem("Tools/Runner/Run smoke test")]
    public static void Run()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        phase = checks = 0;
        SessionState.SetBool("RunnerSmokeTest", true);
        EditorSceneManager.OpenScene("Assets/Scenes/RunnerScene.unity");
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.isPlaying = true;
    }
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception(name);
        checks++;
        Debug.Log("RUNNER CHECK PASS: " + name);
    }
    private static void Wait(float seconds) { deadline = EditorApplication.timeSinceStartup + seconds; }
    private static void Tick()
    {
        if (!Application.isPlaying || !EditorApplication.isPlaying) return;
        try
        {
            if (phase == 0) { Wait(.5f); phase = 1; return; }
            if (EditorApplication.timeSinceStartup < deadline) return;
            switch (phase)
            {
                case 1:
                    game = RunnerGame.Instance; player = game.Player;
                    Check(game != null && player != null && UnityEngine.Object.FindObjectsOfType<Player>().Length == 1, "runner scene initializes a single player");
                    Check(game.Generator.ActiveCount > 5 && game.Distance == 0 && game.Coins == 0, "initial route and fresh run state");
                    Check(player.GetComponent<CombatHUD>() == null && game.GetComponent<RunnerHUD>() != null, "runner HUD replaces arena victory UI");
                    Check(game.Flow.CurrentPlatform != null, "physical ground sensor recognizes starting platform");
                    CheckTuning();
                    CheckAuthoredAssets();
                    player.enabled = false;
                    player.rb.simulated = false;
                    CheckGeneration();
                    CheckFlow();
                    CheckCombatRewards();
                    player.TakeDamage(60, player.transform.position);
                    scratch = new GameObject("Test sanctuary");
                    RunnerPlatform safe = scratch.AddComponent<RunnerPlatform>();
                    safe.Id = 100003; safe.Width = 28; safe.IsSafe = true;
                    player.transform.position = new Vector3(-10, 1.35f, 0);
                    Check(game.TryEnterSanctuary(safe), "landing inside sanctuary opens shop once");
                    Check(game.IsShopping && !player.rb.simulated, "shop freezes player physics");
                    game.Flow.AddTier(); pausedFlow = game.Flow.Remaining; pausedShadow = game.Shadow.X;
                    game.Shadow.Advance(10);
                    Check(game.Shadow.X == pausedShadow, "shadow simulation pauses in sanctuary");
                    Wait(.3f); phase++; break;
                case 2:
                    Check(Mathf.Approximately(game.Flow.Remaining, pausedFlow), "flow countdown pauses while shopping");
                    int slot = Array.FindIndex(game.Offers, item => item != 3);
                    Check(slot >= 0 && !game.Buy(slot), "shop rejects unaffordable purchase");
                    game.AddCoins(1000);
                    int coins = game.Coins, price = game.Price(slot), itemId = game.Offers[slot];
                    Check(game.Buy(slot) && game.Coins == coins - price && game.Ranks[itemId] == 1, "purchase charges exact cost and applies upgrade");
                    Check(!game.Buy(slot), "purchased offer cannot be charged twice");
                    Check(game.Reroll() && !game.Reroll(), "shop allows exactly one paid reroll");
                    CheckAllUpgrades();
                    game.LeaveSanctuary();
                    Check(!game.IsShopping && player.rb.simulated, "leaving sanctuary resumes physics");
                    Check(!game.TryEnterSanctuary(scratch.GetComponent<RunnerPlatform>()), "same sanctuary cannot be farmed repeatedly");
                    UnityEngine.Object.Destroy(scratch);
                    player.rb.simulated = false;
                    game.Shadow.Advance(3); game.Shadow.Advance(100);
                    Check(game.IsGameOver && game.EndReason == "被影子吞没", "shadow catching player ends run despite hit protection");
                    game.Restart(); Wait(.6f); phase++; break;
                case 3:
                    game = RunnerGame.Instance; player = game.Player;
                    Check(!game.IsGameOver && !game.IsShopping && game.Coins == 0 && game.Kills == 0 && game.Flow.Tier == 0, "restart resets currency flow upgrades and chase");
                    Check(game.Ranks.All(rank => rank == 0) && player.CurrentHealth == player.MaxHealth, "new run has fresh health and build");
                    player.rb.simulated = false;
                    player.transform.position = new Vector3(2200, -2200 * RunnerGenerator.Slope + 1.9f, 0);
                    Wait(.2f); phase++; break;
                case 4:
                    Check(game.Distance >= 2200 && Mathf.Approximately(game.DistanceOffset, 2000), "long run rebases world while preserving distance");
                    Check(Mathf.Abs(player.transform.position.x - 200) < 1 && game.Generator.ActiveCount < 30, "rebase keeps coordinates small and world bounded");
                    Check(!game.IsGameOver && !player.IsDead, "descending below original minus-30 limit is safe");
                    player.transform.position = new Vector3(200, -200 * RunnerGenerator.Slope - 25, 0);
                    Wait(.15f); phase++; break;
                case 5:
                    Check(!game.IsGameOver && game.Falls == 1 && game.Flow.Tier == 0 && player.rb.velocity.sqrMagnitude < .01f,
                        "falling returns to last platform with zero velocity and no flow");
                    game.Restart(); Wait(.5f); phase++; break;
                case 6:
                    game = RunnerGame.Instance; player = game.Player;
                    player.enabled = false;
                    player.rb.simulated = true;
                    player.rb.gravityScale = 3.5f;
                    player.transform.position = new Vector3(11, 1.36f, 0);
                    player.rb.velocity = new Vector2(player.moveSpeed * 1.5f, player.jumpForce);
                    game.Flow.NotifyJump();
                    Wait(1.3f); phase++; break;
                case 7:
                    Check(!game.IsGameOver && player.transform.position.x > 23, "physics jump carries runner toward next platform at 1.5x speed");
                    Check(game.Flow.Tier >= 1, "real cross-platform landing awards flow boost");
                    game.Restart(); Wait(.5f); phase++; break;
                case 8:
                    game = RunnerGame.Instance; player = game.Player;
                    player.enabled = false;
                    for (int i = 0; i < 5; i++) game.Flow.AddTier();
                    player.transform.position = new Vector3(11, 1.36f, 0);
                    player.rb.velocity = new Vector2(player.MovementSpeed, player.jumpForce);
                    game.Flow.NotifyJump();
                    Wait(1.6f); phase++; break;
                case 9:
                    Check(!game.IsGameOver && player.transform.position.x > 37 && game.Flow.Tier > 0, "maximum-speed physics jump can continue along generated route");
                    File.WriteAllText("runner-test-result.txt", "PASS: " + checks + " runner checks; 24 procedural seeds verified");
                    Debug.Log("RUNNER SMOKE TEST PASSED: " + checks);
                    Finish(0); break;
            }
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            File.WriteAllText("runner-test-result.txt", "FAIL: " + error);
            Finish(1);
        }
    }

    private static void CheckFlow()
    {
        RunnerFlow flow = game.Flow;
        flow.AdvanceTimer(100);
        flow.AddTier();
        Check(flow.Tier == 1 && Mathf.Approximately(flow.Multiplier, 1.5f) && Mathf.Approximately(player.MovementSpeed, player.moveSpeed * 1.5f), "first boost grants fifty percent movement speed");
        for (int i = 0; i < 8; i++) flow.AddTier();
        Check(flow.Tier == 5 && flow.Multiplier == 2.5f && Mathf.Approximately(flow.Remaining, .8f), "boost caps at five tiers with shorter duration");
        flow.AdvanceTimer(.9f);
        Check(flow.Tier == 4, "expired boost decays by one tier");
        flow.AdvanceTimer(100);
        Check(flow.Tier == 0 && flow.Multiplier == 1 && flow.Remaining == 0, "all boost tiers eventually expire");
        GameObject a = new GameObject("Test landing A"), b = new GameObject("Test landing B");
        RunnerPlatform pa = a.AddComponent<RunnerPlatform>(), pb = b.AddComponent<RunnerPlatform>();
        pa.Id = 100001; pb.Id = 100002;
        player.transform.position = new Vector3(10, 5, 0);
        flow.RegisterLanding(pa, .3f);
        flow.NotifyJump();
        Check(!flow.RegisterLanding(pa, .3f), "jumping on same platform cannot grant boost");
        player.transform.position = new Vector3(20, 5, 0); flow.NotifyJump();
        Check(flow.RegisterLanding(pb, .3f), "forward jump to new platform grants boost");
        player.transform.position = new Vector3(25, 5, 0); flow.NotifyJump();
        Check(!flow.RegisterLanding(pa, .3f), "revisiting earlier platform cannot farm boost");
        UnityEngine.Object.Destroy(a); UnityEngine.Object.Destroy(b);
        flow.AdvanceTimer(100);
    }

    private static void CheckCombatRewards()
    {
        player.transform.position = new Vector3(0, 5, 0);
        if (player.facingDir < 0) player.Flip();
        Enemy_Skeleton enemy = UnityEngine.Object.Instantiate(game.Generator.EnemyPrefab, new Vector3(1.1f, 5, 0), Quaternion.identity);
        enemy.enabled = false;
        enemy.rb.gravityScale = 0;
        Physics2D.SyncTransforms();
        int coins = game.Coins;
        Check(CombatHit.Strike(player, 1000, player.attackSize, player.attackOffset) == 1 && enemy.IsDead, "runner combat kills spawned skeleton");
        Check(game.Kills == 1 && game.Coins == coins + 8 && game.Flow.Tier == 1, "kill awards coins score and a flow tier");
        CombatHit.Strike(player, 1000, player.attackSize, player.attackOffset);
        Check(game.Kills == 1 && game.Coins == coins + 8, "dead enemy cannot give duplicate rewards");
        player.rb.velocity = new Vector2(12, 3);
        player.stateMachine.ChangeState(player.primaryAttackState);
        Check(player.rb.velocity.x == 12 && player.rb.velocity.y == 3, "runner attack preserves entry momentum and airborne velocity");
        player.stateMachine.ChangeState(player.idleState);
        UnityEngine.Object.Destroy(enemy.gameObject);
        // Spend the earned coins to exercise insufficient-currency behavior in the shop.
        game.Flow.AdvanceTimer(100);
    }

    private static void CheckGeneration()
    {
        bool reachable = true, descending = true, bounded = true, branches = false, safe = false;
        bool spacing = true, varied = true;
        float gravity = Mathf.Abs(Physics2D.gravity.y) * player.rb.gravityScale;
        for (int seed = 1; seed <= 24; seed++)
        {
            GameObject root = new GameObject("Generation test " + seed);
            RunnerGenerator generator = root.AddComponent<RunnerGenerator>();
            generator.EnemyVariants = new Enemy[] { null };
            generator.Seed = seed;
            generator.Initialize(game);
            float shortest = float.MaxValue, longest = 0;
            var sanctuaries = new Dictionary<int, float>();
            for (float x = 0; x <= 2000; x += 40)
            {
                generator.EnsureAhead(x);
                RunnerPlatform[] main = generator.Platforms.Where(p => !p.IsBranch).OrderBy(p => p.Left).ToArray();
                for (int i = 1; i < main.Length; i++)
                {
                    float height = main[i].Top - main[i - 1].Top;
                    float discriminant = player.jumpForce * player.jumpForce - 2 * gravity * height;
                    float flight = discriminant >= 0 ? (player.jumpForce + Mathf.Sqrt(discriminant)) / gravity : 0;
                    reachable &= discriminant >= 0 && main[i].Left - main[i - 1].Right + .8f < player.moveSpeed * .8f * flight;
                    spacing &= main[i].Left - main[i - 1].Right >= 3.3f;
                }
                foreach (RunnerPlatform platform in main)
                    if (platform.IsSafe) sanctuaries[platform.Id] = platform.Left;
                    else { shortest = Mathf.Min(shortest, platform.Width); longest = Mathf.Max(longest, platform.Width); }
                bounded &= generator.ActiveCount < 30;
                branches |= generator.Platforms.Any(p => p.IsBranch);
                safe |= generator.Platforms.Any(p => p.IsSafe);
                if (x >= 1100) descending &= Mathf.Abs(main.Last().Top + main.Last().Left * RunnerGenerator.Slope) < 12;
            }
            varied &= shortest <= 9 && longest >= 20;
            float[] stops = sanctuaries.Values.OrderBy(value => value).ToArray();
            spacing &= stops.Length >= 2;
            for (int i = 1; i < stops.Length; i++) spacing &= stops[i] - stops[i - 1] >= 750;
            UnityEngine.Object.DestroyImmediate(root);
        }
        Check(reachable, "24 seeds keep all mandatory platform transitions reachable without boost");
        Check(descending, "procedural routes maintain steeper right-down trend");
        Check(bounded, "generated platform count remains bounded during recycling");
        Check(branches && safe, "routes include upper branches and periodic sanctuaries");
        Check(spacing, "platform gaps are larger and sanctuaries are roughly 800m apart");
        Check(varied, "all procedural seeds mix short and long platforms");
    }

    private static void CheckTuning()
    {
        Check(Camera.main.orthographicSize >= 11 && game.Generator.Ahead >= 130, "larger default view has sufficient generation ahead");
        Check(game.CityBackgroundSprite != null && game.BackgroundSprite.texture.name == "layer_1" && game.CityBackgroundSprite.texture.name == "layer_2", "sky and city backgrounds reuse battle demo assets");
        Check(game.Generator.SurfaceSprite.texture.name == "floor_tile_2" && game.Generator.BodySprite.texture.name == "floor_tile_3", "ground reuses battle demo floor stones");
        Check(game.Generator.SanctuarySpacing == 800 && RunnerGenerator.Slope == .28f, "scene uses less frequent rest stops and steeper slope");
        Vector3 original = player.transform.position;
        float baseSpeed = game.Shadow.Speed;
        player.transform.position = new Vector3(game.Shadow.X + 100, original.y, 0);
        float farSpeed = game.Shadow.Speed;
        Check(baseSpeed >= 8.5f && farSpeed > baseSpeed + 7, "demon has higher base speed and distance catchup bonus");
        game.Shadow.Advance(3);
        float oldSpeed = game.Shadow.CurrentSpeed;
        game.Shadow.Advance(.1f);
        Check(game.Shadow.CurrentSpeed > oldSpeed && game.Shadow.CurrentSpeed < farSpeed, "catchup acceleration is gradual rather than instant");
        player.transform.position = original;
    }

    private static void CheckAuthoredAssets()
    {
        RunnerBackdrop backdrop = UnityEngine.Object.FindObjectOfType<RunnerBackdrop>();
        Check(backdrop != null && backdrop.Sky != null && backdrop.City != null &&
            backdrop.City.GetComponentsInChildren<SpriteRenderer>().Length == 3, "complete demo backdrop contains sky and centre left right town sprites");
        SpriteRenderer town = backdrop.City.GetComponent<SpriteRenderer>();
        Check(town.sortingLayerID == backdrop.Sky.sortingLayerID && town.sortingOrder > backdrop.Sky.sortingOrder,
            "town is rendered in front of sky rather than covered by it");
        Vector3 point = Camera.main.WorldToViewportPoint(town.bounds.center);
        Check(point.y > 0 && point.y < 1, "town remains inside camera view");
        UnityEngine.Object.DestroyImmediate(backdrop.gameObject);
        UnityEngine.Object.DestroyImmediate(Camera.main.GetComponent<RunnerCamera>());
        Camera.main.gameObject.AddComponent<RunnerCamera>().Initialize(player, null, null);
        backdrop = UnityEngine.Object.FindObjectOfType<RunnerBackdrop>();
        Check(backdrop != null && backdrop.City != null, "complete town backdrop loads even when old scene sprite references are missing");

        RunnerGroundModule shortBlock = game.Generator.ShortModule, wideBlock = game.Generator.WideModule;
        Check(shortBlock != null && wideBlock != null && shortBlock.NativeWidth == 4 && shortBlock.Height == 2 &&
            wideBlock.NativeWidth == 10 && wideBlock.Height == 3, "authored short and wide blocks keep demo dimensions and rows");
        Check(shortBlock.SourceCells.Equals(new RectInt(-8, -5, 4, 2)) && wideBlock.SourceCells.Equals(new RectInt(-4, -5, 10, 3)),
            "ground modules come from authored SampleScene block regions");
        bool preserved = true;
        foreach (RunnerPlatform platform in game.Generator.Platforms)
        {
            RunnerGroundModule module = platform.GetComponentInChildren<RunnerGroundModule>();
            RunnerGroundModule original = platform.Width < 10 ? shortBlock : wideBlock;
            preserved &= module != null;
            if (module == null) continue;
            int right = Mathf.RoundToInt(platform.Width) - 1;
            preserved &= module.Tiles.GetTile(new Vector3Int(0, -1, 0)) == original.Tiles.GetTile(new Vector3Int(0, -1, 0));
            preserved &= module.Tiles.GetTile(new Vector3Int(right, -1, 0)) == original.Tiles.GetTile(new Vector3Int(original.NativeWidth - 1, -1, 0));
            preserved &= module.Tiles.GetTile(new Vector3Int(0, -module.Height, 0)) == original.Tiles.GetTile(new Vector3Int(0, -original.Height, 0));
        }
        Check(preserved, "generated platforms preserve authored caps carpet sides and underside tiles");
        typeof(Player).GetProperty("stateMachine").SetValue(player, null);
        player.SendMessage("Update");
        Check(player.stateMachine != null && player.stateMachine.currentState != null, "player restores missing FSM after script reload");
        Enemy enemy = UnityEngine.Object.Instantiate(game.Generator.EnemyPrefab, new Vector3(1000, 100, 0), Quaternion.identity);
        if (enemy != null)
        {
            typeof(Enemy).GetProperty("stateMachine").SetValue(enemy, null);
            enemy.SendMessage("Update");
            Check(enemy.stateMachine != null && enemy.stateMachine.currentState != null, "enemy restores missing FSM after script reload");
            UnityEngine.Object.DestroyImmediate(enemy.gameObject);
        }
    }

    private static void CheckAllUpgrades()
    {
        game.AddCoins(5000);
        bool applied = true;
        for (int item = 0; item < 8; item++)
        {
            if (item == 0 && game.Ranks[item] > 0) continue;
            game.Offers[0] = item;
            game.Purchased[0] = false;
            int oldRank = game.Ranks[item];
            int oldHealth = player.CurrentHealth, oldDamage = player.attackDamage, oldArmor = game.Armor;
            float oldDuration = game.Flow.DurationBonus, oldMagnet = game.MagnetRadius, oldCooldown = player.dashCoolDown;
            bool bought = game.Buy(0);
            if (item == 3 && oldHealth == player.MaxHealth) continue;
            applied &= bought && game.Ranks[item] == oldRank + 1;
            switch (item)
            {
                case 0: applied &= game.HasDoubleJump; break;
                case 1: applied &= game.Flow.DurationBonus > oldDuration; break;
                case 2: applied &= player.attackDamage == oldDamage + 8; break;
                case 3: applied &= player.CurrentHealth > oldHealth; break;
                case 4: applied &= game.MagnetRadius == oldMagnet + 2; break;
                case 5: applied &= player.dashCoolDown < oldCooldown; break;
                case 6: applied &= game.Armor == oldArmor + 3; break;
                case 7: applied &= game.Ranks[7] > 0; break;
            }
        }
        Check(applied, "all eight shop upgrades apply their advertised effects");
    }

    private static void Finish(int code)
    {
        SessionState.SetBool("RunnerSmokeTest", false);
        EditorApplication.update -= Tick;
        if (Application.isBatchMode) EditorApplication.Exit(code);
        else EditorApplication.isPlaying = false;
    }
}
