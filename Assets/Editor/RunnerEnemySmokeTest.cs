using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class RunnerEnemySmokeTest
{
    private static int phase, checks;
    private static double deadline;
    private static RunnerGame game;
    private static Player player;
    private static RunnerEnemy enemy, caster;
    private static RunnerProjectile projectile;
    private static GameObject floor, wall, sanctuary;
    private static float startY;
    private static Vector3 pausedPosition;
    static RunnerEnemySmokeTest()
    {
        if (SessionState.GetBool("RunnerEnemySmoke", false)) EditorApplication.update += Tick;
    }
    [MenuItem("Tools/Runner/Test enemy species")]
    public static void Run()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        phase = checks = 0;
        SessionState.SetBool("RunnerEnemySmoke", true);
        EditorSceneManager.OpenScene("Assets/Scenes/RunnerScene.unity");
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
        EditorApplication.isPlaying = true;
    }
    private static void Check(bool ok, string text)
    {
        if (!ok) throw new Exception(text);
        checks++; Debug.Log("ENEMY CHECK PASS: " + text);
    }
    private static void Wait(float seconds) { deadline = EditorApplication.timeSinceStartup + seconds; }
    private static void Tick()
    {
        if (!EditorApplication.isPlaying || !Application.isPlaying) return;
        try
        {
            if (phase == 0) { Wait(.5f); phase++; return; }
            if (EditorApplication.timeSinceStartup < deadline) return;
            switch (phase)
            {
                case 1:
                    game = RunnerGame.Instance; player = game.Player;
                    game.GetComponent<RunnerSkyDirector>().enabled = false;
                    game.Shadow.enabled = false;
                    player.enabled = false;
                    player.rb.gravityScale = 0;
                    player.zeroVelocity(); player.transform.position = new Vector3(100, 100, 0);
                    floor = new GameObject("Enemy test floor"); floor.layer = LayerMask.NameToLayer("Ground");
                    floor.transform.position = new Vector3(100, 98.1f, 0);
                    floor.AddComponent<BoxCollider2D>().size = new Vector2(45, 1);
                    Check(game.Generator.EnemyVariants.Length == 4 && game.Generator.EnemyVariants.All(unit => unit != null), "four enemy species registered");
                    Check(game.Generator.EnemyVariants.OfType<RunnerEnemy>().Select(unit => unit.Kind).Distinct().Count() == 3, "slime reaver and necromancer have distinct types");
                    CheckAnimationAssets();
                    CheckSpawnDiversity();
                    enemy = Spawn(RunnerEnemyKind.Slime, 97.4f);
                    startY = enemy.transform.position.y;
                    Check(enemy.BeginAttack(player) && enemy.IsWinding, "slime starts visible windup");
                    enemy.enabled = true;
                    Wait(.4f); phase++; break;
                case 2:
                    Check(enemy.transform.position.y > startY + .15f && enemy.rb.velocity.x > 0, "slime performs a forward physics hop");
                    UnityEngine.Object.DestroyImmediate(enemy.gameObject);
                    ResetPlayer();
                    enemy = Spawn(RunnerEnemyKind.Reaver, 97.9f);
                    Check(enemy.BeginAttack(player), "reaver starts charged melee attack");
                    enemy.enabled = true;
                    Wait(.2f); phase++; break;
                case 3:
                    Check(player.CurrentHealth == player.MaxHealth && enemy.IsWinding, "charged melee deals no damage before windup");
                    Wait(.65f); phase++; break;
                case 4:
                    Check(player.CurrentHealth == player.MaxHealth - enemy.Damage, "reaver delivers one charged melee hit");
                    UnityEngine.Object.DestroyImmediate(enemy.gameObject);
                    ResetPlayer();
                    caster = Spawn(RunnerEnemyKind.Necromancer, 93);
                    Check(caster.BeginAttack(player), "necromancer starts aimed cast");
                    Check(caster.TakeDamage(1, player.transform.position) && !caster.IsWinding, "damage interrupts caster windup immediately");
                    caster.enabled = true;
                    Wait(1.05f); phase++; break;
                case 5:
                    Check(!UnityEngine.Object.FindObjectsOfType<RunnerProjectile>().Any(shot => shot.Owner == caster), "interrupted cast does not emit delayed projectile");
                    caster.enabled = false;
                    UnityEngine.Object.DestroyImmediate(caster.gameObject);
                    ResetPlayer();
                    caster = Spawn(RunnerEnemyKind.Necromancer, 93);
                    Check(caster.BeginAttack(player), "fresh caster can attack"); caster.enabled = true;
                    Wait(.85f); phase++; break;
                case 6:
                    projectile = UnityEngine.Object.FindObjectsOfType<RunnerProjectile>().FirstOrDefault(shot => shot.Owner == caster);
                    Check(projectile != null, "caster releases atlas-animated projectile after windup");
                    caster.enabled = false;
                    Wait(.5f); phase++; break;
                case 7:
                    Check(player.CurrentHealth == player.MaxHealth - caster.Damage && projectile == null, "projectile sweep hits player once then disappears");
                    ResetPlayer(); Wait(.75f); phase++; break;
                case 8:
                    wall = new GameObject("Projectile blocker"); wall.layer = LayerMask.NameToLayer("Wall");
                    wall.transform.position = new Vector3(98.5f, 100, 0); wall.AddComponent<BoxCollider2D>().size = new Vector2(.2f, 3);
                    projectile = Bolt(caster, new Vector3(97, 100, 0));
                    Wait(.45f); phase++; break;
                case 9:
                    Check(projectile == null && player.CurrentHealth == player.MaxHealth, "wall blocks projectile before it reaches player");
                    UnityEngine.Object.DestroyImmediate(wall);
                    projectile = Bolt(caster, new Vector3(97, 100, 0));
                    caster.TakeDamage(1000, player.transform.position);
                    Wait(.4f); phase++; break;
                case 10:
                    Check(projectile == null && player.CurrentHealth == player.MaxHealth, "caster death clears its remaining projectiles");
                    UnityEngine.Object.DestroyImmediate(caster.gameObject);
                    caster = Spawn(RunnerEnemyKind.Necromancer, 93);
                    projectile = Bolt(caster, new Vector3(96, 100, 0));
                    sanctuary = new GameObject("Projectile pause sanctuary");
                    RunnerPlatform stop = sanctuary.AddComponent<RunnerPlatform>(); stop.Id = 991001; stop.Width = 30; stop.IsSafe = true;
                    stop.transform.position = new Vector3(110, 98.6f, 0);
                    Check(game.TryEnterSanctuary(stop), "test enters sanctuary with projectile in flight");
                    pausedPosition = projectile.transform.position;
                    Wait(.4f); phase++; break;
                case 11:
                    Check(projectile != null && projectile.transform.position == pausedPosition, "projectiles pause during sanctuary shopping");
                    game.LeaveSanctuary();
                    UnityEngine.Object.DestroyImmediate(projectile.gameObject);
                    UnityEngine.Object.DestroyImmediate(caster.gameObject);
                    UnityEngine.Object.DestroyImmediate(sanctuary);
                    ResetPlayer();
                    CheckRewards();
                    File.WriteAllText("enemy-test-result.txt", "PASS: " + checks + " enemy checks");
                    Debug.Log("ENEMY SMOKE TEST PASSED: " + checks); Finish(0); break;
            }
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            File.WriteAllText("enemy-test-result.txt", "FAIL: " + error);
            Finish(1);
        }
    }
    private static RunnerEnemy Spawn(RunnerEnemyKind kind, float x)
    {
        RunnerEnemy prefab = game.Generator.EnemyVariants.OfType<RunnerEnemy>().First(unit => unit.Kind == kind);
        RunnerEnemy unit = UnityEngine.Object.Instantiate(prefab);
        unit.enabled = false;
        Collider2D body = unit.GetComponent<Collider2D>();
        float feet = body.bounds.min.y - unit.transform.position.y;
        unit.transform.position = new Vector3(x, 98.6f - feet + .04f, 0);
        Physics2D.SyncTransforms();
        return unit;
    }
    private static RunnerProjectile Bolt(RunnerEnemy owner, Vector3 position)
    {
        RunnerProjectile shot = UnityEngine.Object.Instantiate(owner.ProjectilePrefab, position, Quaternion.identity);
        shot.Initialize(owner, Vector2.right, owner.Damage);
        return shot;
    }
    private static void ResetPlayer()
    {
        player.transform.position = new Vector3(100, 100, 0); player.zeroVelocity(); player.Heal(player.MaxHealth);
        if (player.facingDir < 0) player.Flip();
        Physics2D.SyncTransforms();
    }
    private static void CheckAnimationAssets()
    {
        bool clips = true, unique = true;
        var textures = new HashSet<Texture>();
        foreach (RunnerEnemy prefab in game.Generator.EnemyVariants.OfType<RunnerEnemy>())
        {
            Animator animator = prefab.GetComponentInChildren<Animator>();
            foreach (string state in new[] { "Idle", "Move", "Attack", "Hit", "Dead" })
                clips &= animator.runtimeAnimatorController.animationClips.Any(clip => clip.name == state);
            Sprite sprite = prefab.GetComponentInChildren<SpriteRenderer>().sprite;
            unique &= textures.Add(sprite.texture);
            clips &= sprite.rect.width > 0 && prefab.GetComponent<Collider2D>() != null;
        }
        Check(clips && unique, "three source atlases have complete idle move attack hit death clips");
        Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Runner/Enemies/Textures/RunnerNecromancer.png");
        Check(atlas.width == 2720, "large necromancer atlas retains original pixel resolution");
    }
    private static void CheckSpawnDiversity()
    {
        var types = new HashSet<string>();
        bool onSafe = false;
        for (float x = 0; x <= 850; x += 50)
        {
            game.Generator.EnsureAhead(x);
            foreach (Enemy unit in game.Generator.GetComponentsInChildren<Enemy>())
                {
                    types.Add(unit.DisplayName);
                    onSafe |= unit.Momentum != null && unit.Momentum.Platform != null && unit.Momentum.Platform.IsSafe;
                }
        }
        Check(types.Count == 4, "procedural route spawns all four enemy species");
        Check(!onSafe, "sanctuary platforms remain free of enemies");
    }
    private static void CheckRewards()
    {
        foreach (RunnerEnemyKind kind in Enum.GetValues(typeof(RunnerEnemyKind)))
        {
            RunnerEnemy unit = Spawn(kind, 101.2f);
            int coins = game.Coins, kills = game.Kills, score = game.Score, tier = game.Flow.Tier;
            Check(CombatHit.Strike(player, 1000, player.attackSize, player.attackOffset) == 1 && unit.IsDead, kind + " can be killed by player melee");
            Check(game.Coins == coins + unit.CoinValue && game.Kills == kills + 1 && game.Score == score + unit.ScoreValue && game.Flow.Tier == Mathf.Min(5, tier + 1), kind + " gives its configured coins score and flow");
            CombatHit.Strike(player, 1000, player.attackSize, player.attackOffset);
            Check(game.Coins == coins + unit.CoinValue && !unit.GetComponent<Collider2D>().enabled && !unit.rb.simulated, kind + " death disables collision and prevents repeated rewards");
            UnityEngine.Object.DestroyImmediate(unit.gameObject);
        }
    }
    private static void Finish(int code)
    {
        SessionState.SetBool("RunnerEnemySmoke", false);
        EditorApplication.update -= Tick;
        if (Application.isBatchMode) EditorApplication.Exit(code);
        else EditorApplication.isPlaying = false;
    }
}
