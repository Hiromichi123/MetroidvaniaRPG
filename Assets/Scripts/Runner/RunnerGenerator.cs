using System.Collections.Generic;
using UnityEngine;

public class RunnerGenerator : MonoBehaviour
{
    public const float Slope = .28f;
    public int Seed = 0;
    public float Ahead = 130;
    public float Behind = 48;
    public float SanctuarySpacing = 800;
    public Sprite SurfaceSprite;
    public Sprite BodySprite;
    public Sprite LampSprite;
    public Enemy_Skeleton EnemyPrefab;
    public Enemy[] EnemyVariants;
    public RunnerGroundModule ShortModule;
    public RunnerGroundModule WideModule;
    public readonly List<RunnerPlatform> Platforms = new List<RunnerPlatform>();
    public int GeneratedCount { get; private set; }
    public int ActiveCount => Platforms.Count;
    private System.Random random;
    private float nextX;
    private float previousTop;
    private float nextSanctuary;
    private int sequence;
    private int sinceEnemy;
    private int spawnedEnemies;
    private float pendingEnemyGold;
    public RunnerPlatform PendingArena { get; private set; }
    public readonly Dictionary<int, float> SanctuaryMarkers = new Dictionary<int, float>();
    public const float EnemyGoldPerMeter = .375f;
    private RunnerGame game;
    private Sprite solid;
    private readonly List<Sprite> runtimeTiles = new List<Sprite>();

    public void Initialize(RunnerGame owner)
    {
        game = owner;
        if (EnemyVariants == null || EnemyVariants.Length == 0)
        {
            var variants = new List<Enemy>();
            foreach (string name in new[] { "RunnerSlime", "RunnerSkeleton", "RunnerNecromancer", "RunnerReaver" })
            {
                Enemy enemy = name == "RunnerSkeleton" ? EnemyPrefab : null;
                if (enemy == null)
                {
                    GameObject prefab = Resources.Load<GameObject>("Runner/Enemies/" + name);
                    if (prefab != null) enemy = prefab.GetComponent<Enemy>();
                }
                if (enemy != null) variants.Add(enemy);
            }
            EnemyVariants = variants.ToArray();
        }
        if (ShortModule == null)
        {
            GameObject prefab = Resources.Load<GameObject>("Runner/GroundModules/DemoShortBlock");
            if (prefab != null) ShortModule = prefab.GetComponent<RunnerGroundModule>();
        }
        if (WideModule == null)
        {
            GameObject prefab = Resources.Load<GameObject>("Runner/GroundModules/DemoWideBlock");
            if (prefab != null) WideModule = prefab.GetComponent<RunnerGroundModule>();
        }
        random = new System.Random(Seed);
        SurfaceSprite = FullRect(SurfaceSprite);
        BodySprite = FullRect(BodySprite);
        solid = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1);
        nextSanctuary = SanctuarySpacing;
        CreatePlatform(-9, 0, 23, false, false);
        nextX = 14;
        previousTop = 0;
        EnsureAhead(0);
    }

    private float Range(float min, float max) => min + (float)random.NextDouble() * (max - min);

    private Sprite FullRect(Sprite surfaceSprite)
    {
        if (surfaceSprite == null) return null;
        Sprite tile = Sprite.Create(surfaceSprite.texture, surfaceSprite.rect, new Vector2(.5f, .5f),
            surfaceSprite.pixelsPerUnit, 0, SpriteMeshType.FullRect);
        runtimeTiles.Add(tile);
        return tile;
    }

    public void EnsureAhead(float playerX)
    {
        while (nextX < playerX + Ahead && PendingArena == null) GenerateNext();
        for (int i = Platforms.Count - 1; i >= 0; i--)
        {
            RunnerPlatform platform = Platforms[i];
            if (platform == null) { Platforms.RemoveAt(i); continue; }
            if (platform.Right < playerX - Behind && platform != game.RespawnPlatform &&
                (PendingArena == null || platform.ArenaOwnerId != PendingArena.Id))
            {
                Platforms.RemoveAt(i);
                Destroy(platform.gameObject);
            }
        }
    }

    private void GenerateNext()
    {
        bool safe = game.AbsoluteX(nextX) >= nextSanctuary;
        bool forest = game.CurrentRegion != null && game.CurrentRegion.ForestLayout;
        bool boss = safe && Mathf.RoundToInt(nextSanctuary / SanctuarySpacing) % 3 == 0;
        float gap = forest ? Range(3.8f, 6) : Range(3.4f, 5.8f);
        float left = nextX + gap;
        float width = safe ? 28 : sequence % 3 == 0 ? Range(6, 9) : sequence % 3 == 1 ? Range(11, 15) : Range(18, 24);
        if (forest && !safe) width += 2;
        if (boss) width = 36;
        width = Mathf.Round(width);
        float top = Mathf.Clamp(-left * Slope + Range(-1, 1), previousTop - 8.5f, previousTop + .65f);
        if (!safe && sequence % 7 == 3) top = previousTop + .55f;
        // Larger gaps still respect the player's unboosted jump, including uphill links.
        float gravity = Mathf.Abs(Physics2D.gravity.y) * game.Player.rb.gravityScale;
        float jump = game.Player.jumpForce;
        float discriminant = Mathf.Max(0, jump * jump - 2 * gravity * (top - previousTop));
        float flight = (jump + Mathf.Sqrt(discriminant)) / gravity;
        float reachableGap = game.Player.moveSpeed * .8f * flight - 1f;
        gap = Mathf.Min(gap, reachableGap);
        left = nextX + gap;
        RunnerPlatform main = CreatePlatform(left, top, width, safe, false);
        if (safe)
        {
            main.MilestoneDistance = nextSanctuary;
            SanctuaryMarkers[Mathf.RoundToInt(nextSanctuary / SanctuarySpacing)] = game.AbsoluteX(main.Left + 2);
        }
        if (boss)
        {
            main.IsBossGate = main.IsArenaPlatform = true;
            main.ArenaOwnerId = main.Id;
            PendingArena = main;
            for (int i = 0; i < 4; i++)
            {
                RunnerPlatform shelf = CreatePlatform(left + 4 + i * 7.2f, top + (i % 2 == 0 ? 2.7f : 5.4f), 5, false, true);
                shelf.IsArenaPlatform = true;
                shelf.ArenaOwnerId = main.Id;
            }
            Visual(main.transform, "Region exit", solid, new Vector2(.4f, 3), new Vector2(width * .5f - 2, 1.5f), new Color(.4f, 1, .8f, .8f), 6);
        }
        pendingEnemyGold += (gap + width) * EnemyGoldPerMeter;
        if (safe) { nextSanctuary += SanctuarySpacing; sinceEnemy = 0; }
        else
        {
            int coinCount = width < 10 ? 2 : 3;
            for (int i = 0; i < coinCount; i++) SpawnCoin(main, width * (i + 1) / (coinCount + 1), 1);
            if (sequence > 0 && ++sinceEnemy >= 1 && width >= 8 && !NearSanctuary(left))
            {
                int crowd = width >= 18 ? 3 : width >= 11 ? 2 : 1;
                int reward = Mathf.FloorToInt(pendingEnemyGold);
                pendingEnemyGold -= reward;
                for (int i = 0; i < crowd; i++)
                    SpawnEnemy(main, crowd == 1 ? width * .65f : Mathf.Lerp(width * .4f, width - 1.5f, (float)i / (crowd - 1)),
                        reward / crowd + (i < reward % crowd ? 1 : 0));
                sinceEnemy = 0;
            }
            if (sequence % 4 == 2 && width >= 12)
            {
                float branchWidth = Mathf.Round(Mathf.Min(10, width - 4.5f));
                RunnerPlatform branch = CreatePlatform(left + 3.5f, top + 2.1f, branchWidth, false, true);
                for (int i = 0; i < 3; i++) SpawnCoin(branch, branchWidth * (i + 1) / 4, 2);
            }
        }
        previousTop = top;
        nextX = left + width;
        sequence++;
    }

    private bool NearSanctuary(float x) => Mathf.Abs(game.AbsoluteX(x) - (nextSanctuary - SanctuarySpacing)) < 35;

    private RunnerPlatform CreatePlatform(float left, float top, float width, bool safe, bool branch)
    {
        GameObject root = new GameObject(safe ? "Sanctuary" : branch ? "Upper route" : "Run platform");
        root.layer = LayerMask.NameToLayer("Ground");
        root.transform.SetParent(transform);
        root.transform.position = new Vector3(left + width * .5f, top, 0);
        RunnerPlatform platform = root.AddComponent<RunnerPlatform>();
        platform.Id = ++GeneratedCount;
        platform.Width = width;
        platform.IsSafe = safe;
        platform.IsBranch = branch;
        BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(width, .65f);
        collider.offset = new Vector2(0, -.325f);
        collider.usedByEffector = true;
        PlatformEffector2D effector = root.AddComponent<PlatformEffector2D>();
        effector.useOneWay = true;
        effector.useOneWayGrouping = true;
        effector.surfaceArc = 165;
        RunnerGroundModule template = width < 10 ? ShortModule : WideModule;
        if (template != null)
        {
            RunnerGroundModule module = Instantiate(template, root.transform);
            module.transform.localPosition = new Vector3(-width * .5f, 0, 0);
            module.SetWidth(Mathf.RoundToInt(width));
            if (game.CurrentRegion != null) module.Tiles.color = game.CurrentRegion.GroundTint;
        }
        else
        {
            Sprite stone = sequence % 2 == 0 || BodySprite == null ? SurfaceSprite : BodySprite;
            Visual(root.transform, "Stone", stone, new Vector2(width, 1), new Vector2(0, -.5f), Color.white, 0);
        }
        if (safe || branch)
            Visual(root.transform, "Edge", solid, new Vector2(width, .035f), new Vector2(0, -.015f),
                safe ? new Color(1, .78f, .28f) : new Color(.5f, .65f, 1), 1);
        if (safe && LampSprite != null)
        {
            Visual(root.transform, "Sanctuary beacon", LampSprite, new Vector2(1.3f, 2.6f), new Vector2(-width * .5f + 2, 1.3f), Color.white, 2);
            Visual(root.transform, "Sanctuary beacon", LampSprite, new Vector2(1.3f, 2.6f), new Vector2(width * .5f - 2, 1.3f), Color.white, 2);
        }
        if (game.CurrentRegion != null && game.CurrentRegion.Decoration != null)
            Visual(root.transform, "Region foliage", game.CurrentRegion.Decoration, new Vector2(1.5f, .7f),
                new Vector2(-width * .5f + 1.5f, .3f), Color.white, 3);
        Platforms.Add(platform);
        return platform;
    }

    private void Visual(Transform parent, string name, Sprite sprite, Vector2 size, Vector2 position, Color color, int order)
    {
        if (sprite == null) return;
        GameObject visual = new GameObject(name);
        visual.transform.SetParent(parent, false);
        visual.transform.localPosition = position;
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        if (sprite == solid) visual.transform.localScale = new Vector3(size.x, size.y, 1);
        else if (sprite == LampSprite || (game.CurrentRegion != null && sprite == game.CurrentRegion.Decoration))
            visual.transform.localScale = Vector3.one * (size.y / sprite.bounds.size.y);
        else { renderer.drawMode = SpriteDrawMode.Tiled; renderer.size = size; }
    }

    private void SpawnCoin(RunnerPlatform platform, float offset, int value)
    {
        GameObject coin = new GameObject("Gold");
        coin.transform.SetParent(platform.transform, false);
        coin.transform.localPosition = new Vector3(offset - platform.Width * .5f, 1.25f, 0);
        coin.transform.localScale = new Vector3(.26f, .26f, 1);
        coin.transform.localRotation = Quaternion.Euler(0, 0, 45);
        SpriteRenderer renderer = coin.AddComponent<SpriteRenderer>();
        renderer.sprite = solid;
        renderer.color = new Color(1, .76f, .21f);
        renderer.sortingOrder = 4;
        coin.AddComponent<RunnerPickup>().Value = value;
    }

    private void SpawnEnemy(RunnerPlatform platform, float offset, int gold)
    {
        Enemy[] roster = game.RegionIndex > 0 && game.CurrentRegion != null ? game.CurrentRegion.Enemies : EnemyVariants;
        Enemy template = roster != null && roster.Length > 0 ?
            roster[spawnedEnemies < roster.Length ? spawnedEnemies : random.Next(roster.Length)] : EnemyPrefab;
        if (template == null) return;
        spawnedEnemies++;
        Enemy enemy = Instantiate(template, platform.transform);
        enemy.RunnerCoinValue = Mathf.Max(1, gold);
        enemy.FlipController(-1);
        Collider2D body = enemy.GetComponent<Collider2D>();
        float feet = body.bounds.min.y - enemy.transform.position.y;
        enemy.transform.position = new Vector3(platform.Left + offset, platform.Top - feet + .04f, 0);
        enemy.transform.SetParent(transform, true);
        if (enemy.Momentum != null) enemy.Momentum.Platform = platform;
        if (enemy is Enemy_Skeleton skeleton)
        {
            skeleton.attackDamage = 15;
            skeleton.detectionRange = 8;
            skeleton.moveSpeed = 1.8f;
        }
    }

    public void Rebase(Vector2 shift)
    {
        foreach (RunnerPlatform platform in Platforms) if (platform != null) platform.transform.position -= (Vector3)shift;
        foreach (Enemy enemy in GetComponentsInChildren<Enemy>()) enemy.transform.position -= (Vector3)shift;
        foreach (RunnerProjectile projectile in FindObjectsOfType<RunnerProjectile>())
            if (projectile.GetComponentInParent<RunnerPlatform>() == null) projectile.transform.position -= (Vector3)shift;
        foreach (RunnerGoldFlight gold in FindObjectsOfType<RunnerGoldFlight>()) gold.transform.position -= (Vector3)shift;
        nextX -= shift.x;
        previousTop -= shift.y;
    }

    public void ReleaseArena() { PendingArena = null; }

    private void OnDestroy()
    {
        if (solid != null) Destroy(solid);
        foreach (Sprite tile in runtimeTiles) if (tile != null) Destroy(tile);
    }
}
