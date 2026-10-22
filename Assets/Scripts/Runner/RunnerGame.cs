using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-200)]
public class RunnerGame : MonoBehaviour
{
    public static RunnerGame Instance { get; private set; }
    public Player PlayerPrefab;
    public RunnerGenerator Generator;
    public Sprite BackgroundSprite;
    public Sprite CityBackgroundSprite;
    public RunnerBackdrop BackgroundPrefab;
    public Texture2D ShadowSheet;
    public Player Player { get; private set; }
    public RunnerFlow Flow { get; private set; }
    public RunnerShadow Shadow { get; private set; }
    public int Coins { get; private set; }
    public int TotalIncome { get; private set; }
    public int Kills { get; private set; }
    private int combatScore;
    public int Score => Mathf.FloorToInt(Distance * 10) + combatScore;
    public float Distance { get; private set; }
    public int Stage => Mathf.FloorToInt(Distance / Generator.SanctuarySpacing) + 1;
    public float DistanceOffset { get; private set; }
    public bool IsShopping { get; private set; }
    public bool IsGameOver { get; private set; }
    public bool IsPaused => IsShopping || IsGameOver;
    public string EndReason { get; private set; }
    public float MagnetRadius { get; private set; } = 1.5f;
    public int Armor { get; private set; }
    public bool HasDoubleJump { get; private set; }
    public int[] Offers { get; private set; } = new int[3];
    public bool[] Purchased { get; private set; } = new bool[3];
    public readonly int[] Ranks = new int[8];
    public static readonly string[] ItemNames = { "滞空步", "节奏延长", "锋利刀刃", "急救补给", "磁力口袋", "疾风冲刺", "轻型护甲", "嗜血回响" };
    public static readonly string[] ItemDescriptions = { "解锁二段跳", "每档加速多持续 0.35 秒", "攻击伤害 +8", "恢复 45 点生命", "金币吸附范围 +2m", "冲刺冷却缩短 20%", "每次受伤减少 3 点", "击杀恢复 4 点生命" };
    public static readonly int[] ItemCosts = { 220, 160, 180, 110, 140, 180, 190, 220 };
    public const int RerollCost = 50;
    private readonly HashSet<int> visitedSanctuaries = new HashSet<int>();
    private RunnerPlatform sanctuary;
    private bool rerolled;
    private System.Random shopRandom;
    private bool restarting;
    public RunnerPlatform RespawnPlatform { get; private set; }
    public int Falls { get; private set; }
    private float landingOffset;
    private bool enemyCollisionWasIgnored;
    public const string BestDistanceKey = "Runner.BestDistance";
    public float HistoricalBest { get; private set; }
    public float BestDistance { get; private set; }
    public int RegionIndex { get; private set; }
    public bool InBossArena { get; private set; }
    public RunnerPlatform ArenaPlatform { get; private set; }
    public RunnerRegionCatalog Regions { get; private set; }
    public RunnerRegionDefinition CurrentRegion => Regions != null && Regions.Regions.Length > 0 ?
        Regions.Regions[RegionIndex % Regions.Regions.Length] : null;
    public string RegionName => CurrentRegion != null ? CurrentRegion.Name : "旧城";
    public float RoutePosition => Player != null ? AbsoluteX(Player.transform.position.x) : 0;
    public bool AtArenaExit => InBossArena && ArenaPlatform != null &&
        Player.transform.position.x >= ArenaPlatform.Right - 4 &&
        Player.GetComponent<Collider2D>().bounds.min.y >= ArenaPlatform.Top - .3f &&
        Player.GetComponent<Collider2D>().bounds.min.y < ArenaPlatform.Top + 2;

    private void Awake()
    {
        Instance = this;
        HistoricalBest = BestDistance = PlayerPrefs.GetFloat(BestDistanceKey, 0);
        Regions = Resources.Load<RunnerRegionCatalog>("Runner/Regions/RegionCatalog");
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        enemyCollisionWasIgnored = Physics2D.GetIgnoreLayerCollision(enemyLayer, enemyLayer);
        Physics2D.IgnoreLayerCollision(enemyLayer, enemyLayer, true);
        if (PlayerPrefab == null || Generator == null) { Debug.LogError("Runner scene is missing its assets."); enabled = false; return; }
        if (Generator.Seed == 0) Generator.Seed = Random.Range(1, int.MaxValue);
        shopRandom = new System.Random(Generator.Seed ^ 1789);
        Player = Instantiate(PlayerPrefab, new Vector3(0, 1.9f, 0), Quaternion.identity);
        Player.name = "Player";
        Player.attackSize = new Vector2(3.8f, 2.8f);
        Player.attackOffset = new Vector2(1.7f, -.1f);
        Flow = Player.gameObject.AddComponent<RunnerFlow>();
        Generator.Initialize(this);
        RecordLanding(Generator.Platforms[0]);
        Camera camera = Camera.main;
        if (camera != null) camera.gameObject.AddComponent<RunnerCamera>().Initialize(Player, BackgroundSprite, CityBackgroundSprite, BackgroundPrefab);
        GameObject shadow = new GameObject("The pursuing shadow");
        Shadow = shadow.AddComponent<RunnerShadow>();
        Shadow.Initialize(this, ShadowSheet);
        gameObject.AddComponent<RunnerHUD>();
        gameObject.AddComponent<RunnerProgressHUD>();
        gameObject.AddComponent<RunnerSkyDirector>();
    }

    private void Update()
    {
        if (Player == null) return;
        if (Input.GetKeyDown(KeyCode.R)) Restart();
        if (IsGameOver) return;
        if (Player.IsDead) { EndRun("生命耗尽"); return; }
        if (InBossArena)
        {
            if (Player.transform.position.y < ArenaPlatform.Top - 9) RespawnFromFall();
            if (Input.GetKeyDown(KeyCode.E) && AtArenaExit) ContinueToNextRegion();
            UpdateRecord();
            return;
        }
        if (IsShopping)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) Buy(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) Buy(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) Buy(2);
            if (Input.GetKeyDown(KeyCode.Return)) LeaveSanctuary();
            return;
        }
        Distance = Mathf.Max(Distance, AbsoluteX(Player.transform.position.x));
        UpdateRecord();
        Generator.EnsureAhead(Player.transform.position.x);
        if (Player.transform.position.y < -Player.transform.position.x * RunnerGenerator.Slope - 19)
        { RespawnFromFall(); return; }
        if (Player.transform.position.x > 2000) RebaseWorld();
    }

    public float AbsoluteX(float localX) => localX + DistanceOffset;

    private void UpdateRecord()
    {
        Distance = Mathf.Max(Distance, RoutePosition);
        if (Distance > BestDistance)
        {
            BestDistance = Distance;
            if (!Application.isBatchMode) PlayerPrefs.SetFloat(BestDistanceKey, BestDistance);
        }
    }
    private void FixedUpdate()
    {
        if (!InBossArena || IsPaused || ArenaPlatform == null) return;
        Vector2 position = Player.rb.position;
        float x = Mathf.Clamp(position.x, ArenaPlatform.Left + .7f, ArenaPlatform.Right - .7f);
        if (Mathf.Abs(x - position.x) > .001f)
        {
            Player.rb.position = new Vector2(x, position.y);
            Player.rb.velocity = new Vector2(0, Player.rb.velocity.y);
        }
    }
    public void EnterBossArena(RunnerPlatform platform)
    {
        if (IsPaused || InBossArena || platform == null || !platform.IsBossGate) return;
        InBossArena = true; ArenaPlatform = platform;
        foreach (Enemy enemy in FindObjectsOfType<Enemy>()) { enemy.gameObject.SetActive(false); Destroy(enemy.gameObject); }
        foreach (RunnerProjectile projectile in FindObjectsOfType<RunnerProjectile>()) Destroy(projectile.gameObject);
        foreach (RunnerGoldFlight gold in FindObjectsOfType<RunnerGoldFlight>()) gold.Collect();
        Flow.ResetAfterFall(); RecordLanding(platform);
        Player.ResetAfterFall(new Vector3(Mathf.Clamp(Player.transform.position.x, platform.Left + 2, platform.Right - 2),
            platform.Top + 1.4f, 0));
        Shadow.gameObject.SetActive(false);
        Camera.main?.GetComponent<RunnerCamera>()?.LockArena(platform);
    }
    public bool ContinueToNextRegion()
    {
        if (!AtArenaExit || IsPaused) return false;
        RegionIndex++;
        Generator.ReleaseArena();
        InBossArena = false;
        Flow.ResetAfterFall();
        Camera.main?.GetComponent<RunnerCamera>()?.UseRegion(CurrentRegion);
        Shadow.gameObject.SetActive(true);
        Shadow.GiveHeadStart(Player.transform.position.x - 25);
        Generator.EnsureAhead(Player.transform.position.x);
        return true;
    }

    public void RecordLanding(RunnerPlatform platform)
    {
        if (platform == null) return;
        RespawnPlatform = platform;
        landingOffset = Mathf.Clamp(Player.transform.position.x - platform.transform.position.x,
            -platform.Width * .5f + 1.5f, platform.Width * .5f - 1.5f);
    }

    public void RespawnFromFall()
    {
        if (IsPaused || Player.IsDead || RespawnPlatform == null) return;
        Physics2D.SyncTransforms();
        float feet = Player.GetComponent<Collider2D>().bounds.min.y - Player.transform.position.y;
        Vector3 position = new Vector3(RespawnPlatform.transform.position.x + landingOffset, RespawnPlatform.Top - feet + .08f, 0);
        Player.ResetAfterFall(position);
        Flow.ResetAfterFall();
        Falls++;
        Shadow.GiveHeadStart(position.x - 18);
        Physics2D.SyncTransforms();
    }

    public void AddCoins(int amount)
    {
        if (IsGameOver) return;
        amount = Mathf.Max(0, amount);
        Coins += amount; TotalIncome += amount;
    }

    public bool ConvertEnemyToGold(Enemy enemy)
    {
        if (IsPaused || enemy == null || enemy.IsDead || enemy.Momentum == null ||
            !enemy.Momentum.IsPlayerDisplaced || !enemy.TryClaimReward()) return false;
        RunnerGoldFlight.Spawn(enemy.transform.position, enemy.GoldValue);
        enemy.gameObject.SetActive(false);
        Destroy(enemy.gameObject);
        return true;
    }

    public void RewardKill(Entity victim, Entity attacker)
    {
        if (IsPaused || attacker != Player || !(victim is Enemy) || !victim.IsDead) return;
        Enemy defeated = (Enemy)victim;
        if (!defeated.TryClaimReward()) return;
        Kills++;
        AddCoins(defeated.GoldValue);
        combatScore += defeated.ScoreValue;
        Flow.AddTier();
        if (Ranks[7] > 0) Player.Heal(4 * Ranks[7]);
    }

    public bool TryEnterSanctuary(RunnerPlatform platform)
    {
        if (InBossArena || platform == null || !platform.IsSafe || IsPaused || Player.IsDead ||
            Player.transform.position.x < platform.Left + 2 || visitedSanctuaries.Contains(platform.Id)) return false;
        visitedSanctuaries.Add(platform.Id);
        sanctuary = platform;
        foreach (RunnerGoldFlight gold in FindObjectsOfType<RunnerGoldFlight>()) gold.Collect();
        IsShopping = true;
        Player.zeroVelocity();
        Player.rb.simulated = false;
        Player.anim.speed = 0;
        rerolled = false;
        RollOffers();
        return true;
    }

    private bool Eligible(int item)
    {
        if (item == 0) return Ranks[item] == 0;
        if (item == 3) return true;
        return Ranks[item] < 3;
    }

    private void RollOffers()
    {
        List<int> pool = new List<int>();
        for (int i = 0; i < ItemNames.Length; i++) if (Eligible(i)) pool.Add(i);
        for (int i = 0; i < 3; i++)
        {
            int pick = shopRandom.Next(pool.Count);
            Offers[i] = pool[pick];
            // Emergency supplies can repeat only when every permanent upgrade is capped.
            if (pool.Count > 1) pool.RemoveAt(pick);
            Purchased[i] = false;
        }
    }

    public int Price(int slot) => ItemCosts[Offers[slot]] + (Offers[slot] == 3 ? 0 : Ranks[Offers[slot]] * 40);

    public bool Buy(int slot)
    {
        if (!IsShopping || slot < 0 || slot >= Offers.Length || Purchased[slot]) return false;
        int item = Offers[slot], price = Price(slot);
        if (Coins < price || !Eligible(item) || (item == 3 && Player.CurrentHealth >= Player.MaxHealth)) return false;
        Coins -= price;
        Purchased[slot] = true;
        Ranks[item]++;
        switch (item)
        {
            case 0: HasDoubleJump = true; break;
            case 1: Flow.DurationBonus += .35f; break;
            case 2: Player.attackDamage += 8; break;
            case 3: Player.Heal(45); break;
            case 4: MagnetRadius += 2; break;
            case 5: Player.dashCoolDown = Mathf.Max(.35f, Player.dashCoolDown * .8f); break;
            case 6: Armor += 3; break;
        }
        return true;
    }

    public bool Reroll()
    {
        if (!IsShopping || rerolled || Coins < RerollCost) return false;
        Coins -= RerollCost;
        rerolled = true;
        RollOffers();
        return true;
    }

    public void LeaveSanctuary()
    {
        if (!IsShopping) return;
        IsShopping = false;
        Player.rb.simulated = true;
        Player.anim.speed = 1;
        Player.stateMachine.ChangeState(Player.idleState);
        Shadow.GiveHeadStart(sanctuary.Left - 12);
        if (sanctuary.IsBossGate) EnterBossArena(sanctuary);
    }

    public void EndRun(string reason)
    {
        if (IsGameOver) return;
        IsShopping = false;
        IsGameOver = true;
        EndReason = reason;
        PlayerPrefs.Save();
        Player.zeroVelocity();
        Player.rb.simulated = false;
        Player.anim.speed = 0;
        foreach (Enemy enemy in FindObjectsOfType<Enemy>())
        {
            enemy.enabled = false;
            if (enemy.rb != null) { enemy.zeroVelocity(); enemy.rb.simulated = false; }
            if (enemy.anim != null) enemy.anim.speed = 0;
        }
    }

    public void Restart()
    {
        if (restarting) return;
        restarting = true;
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void RebaseWorld()
    {
        Vector2 shift = new Vector2(2000, -2000 * RunnerGenerator.Slope);
        Generator.Rebase(shift);
        Player.transform.position -= (Vector3)shift;
        Shadow.Rebase(shift);
        if (Camera.main != null) Camera.main.transform.position -= (Vector3)shift;
        DistanceOffset += shift.x;
        Physics2D.SyncTransforms();
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        PlayerPrefs.Save();
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Enemy"), LayerMask.NameToLayer("Enemy"), enemyCollisionWasIgnored);
        Instance = null;
    }
}
