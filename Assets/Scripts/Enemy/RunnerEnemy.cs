using UnityEngine;

public enum RunnerEnemyKind { Slime, Reaver, Necromancer }

public class RunnerEnemy : Enemy
{
    public RunnerEnemyKind Kind;
    public int Damage = 12;
    public int CoinReward = 8;
    public int ScoreReward = 100;
    public float Windup = .6f;
    public float ActiveTime = .35f;
    public float Cooldown = 1f;
    public float AttackRange = 3;
    public RunnerProjectile ProjectilePrefab;
    public bool IsWinding => phase == Phase.Windup;
    public override int CoinValue => CoinReward;
    public override int ScoreValue => ScoreReward;
    public override string DisplayName => Kind == RunnerEnemyKind.Slime ? "史莱姆" : Kind == RunnerEnemyKind.Reaver ? "镰刃怪" : "死灵法师";
    protected override bool UsesStateMachine => false;
    private enum Phase { Ready, Windup, Active }
    private Phase phase;
    private float timer, cooldown;
    private bool hit;
    private Player player;
    private Vector2 aim;
    private string animationName;
    private LineRenderer warning;
    private Material warningMaterial;
    private RunnerPlatform platform;

    protected override void Awake()
    {
        base.Awake();
        platform = GetComponentInParent<RunnerPlatform>();
        GameObject line = new GameObject("Attack warning");
        line.transform.SetParent(transform, false);
        warning = line.AddComponent<LineRenderer>();
        warning.positionCount = 2;
        warning.useWorldSpace = true;
        warning.startWidth = warning.endWidth = .065f;
        warning.sortingLayerName = "Player";
        warning.sortingOrder = 4;
        warningMaterial = new Material(Shader.Find("Sprites/Default"));
        warning.sharedMaterial = warningMaterial;
        warning.enabled = false;
    }

    protected override void Start() { base.Start(); if (!IsDead) Play("Idle"); }

    protected override void Update()
    {
        base.Update();
        if (Momentum != null && Momentum.Platform != null) platform = Momentum.Platform;
        if (IsDead) return;
        if (RunnerGame.Instance != null && RunnerGame.Instance.IsPaused)
        { anim.speed = 0; return; }
        anim.speed = 1;
        if (IsStunned) return;
        if (player == null) player = FindObjectOfType<Player>();
        cooldown = Mathf.Max(0, cooldown - Time.deltaTime);
        if (phase == Phase.Windup)
        {
            zeroVelocity();
            DrawWarning();
            timer -= Time.deltaTime;
            if (timer <= 0) ReleaseAttack();
            return;
        }
        if (phase == Phase.Active)
        {
            timer -= Time.deltaTime;
            if (Kind != RunnerEnemyKind.Necromancer && !hit)
            {
                Vector2 size = Kind == RunnerEnemyKind.Slime ? new Vector2(1.6f, 1.4f) : new Vector2(3.2f, 2.5f);
                Vector2 offset = Kind == RunnerEnemyKind.Slime ? new Vector2(.25f, 0) : new Vector2(1.4f, -.1f);
                hit = CombatHit.Strike(this, Damage, size, offset) > 0;
            }
            if (timer <= 0) { phase = Phase.Ready; cooldown = Cooldown; Play("Idle"); }
            return;
        }
        if (player == null || player.IsDead) { zeroVelocity(); Play("Idle"); return; }
        Vector2 delta = player.transform.position - transform.position;
        if (delta.sqrMagnitude > detectionRange * detectionRange || Mathf.Abs(delta.y) > 5)
        { zeroVelocity(); Play("Idle"); return; }
        int direction = delta.x >= 0 ? 1 : -1;
        FlipController(direction);
        if (IsGroundedDetected() && Mathf.Abs(delta.x) <= AttackRange && Mathf.Abs(delta.y) <= (Kind == RunnerEnemyKind.Necromancer ? 5 : 2))
        {
            zeroVelocity();
            if (cooldown <= 0 && BeginAttack(player)) return;
        }
        if (!IsGroundedDetected()) return;
        float desired = Kind == RunnerEnemyKind.Slime ? 0 : Kind == RunnerEnemyKind.Necromancer ?
            (Mathf.Abs(delta.x) < 3 ? -direction * moveSpeed : 0) : direction * moveSpeed;
        if (desired != 0 && CanStep(desired > 0 ? 1 : -1))
        {
            // Retreating does not turn the caster's aim away from the player.
            rb.velocity = new Vector2(desired, rb.velocity.y);
            Play("Move");
        }
        else { rb.velocity = new Vector2(0, rb.velocity.y); Play("Idle"); }
    }

    public bool BeginAttack(Player target)
    {
        if (target == null || target.IsDead || IsDead || IsStunned || phase != Phase.Ready || cooldown > 0 ||
            (RunnerGame.Instance != null && RunnerGame.Instance.IsPaused)) return false;
        Vector2 delta = target.transform.position - transform.position;
        if (Mathf.Abs(delta.x) > AttackRange || Mathf.Abs(delta.y) > (Kind == RunnerEnemyKind.Necromancer ? 5 : 2)) return false;
        FlipController(delta.x >= 0 ? 1 : -1);
        aim = ((Vector2)target.transform.position - ShotOrigin).normalized;
        if (Physics2D.Linecast(ShotOrigin, target.transform.position, LayerMask.GetMask("Ground", "Wall"))) return false;
        phase = Phase.Windup;
        timer = Windup;
        hit = false;
        zeroVelocity();
        Play("Attack", true);
        warning.enabled = true;
        DrawWarning();
        return true;
    }

    private Vector2 ShotOrigin => (Vector2)transform.position + new Vector2(facingDir * .65f, .2f);

    private void ReleaseAttack()
    {
        warning.enabled = false;
        phase = Phase.Active;
        timer = ActiveTime;
        if (Kind == RunnerEnemyKind.Necromancer)
        {
            if (ProjectilePrefab != null)
            {
                RunnerProjectile projectile = Instantiate(ProjectilePrefab, ShotOrigin, Quaternion.identity,
                    platform != null ? platform.transform : null);
                projectile.Initialize(this, aim, Damage);
            }
        }
        else if (Kind == RunnerEnemyKind.Slime)
        {
            float velocity = 4.8f * facingDir;
            if (platform != null)
            {
                float landing = Mathf.Clamp(transform.position.x + velocity * .6f, platform.Left + .8f, platform.Right - .8f);
                velocity = (landing - transform.position.x) / .6f;
            }
            rb.velocity = new Vector2(velocity, 7.5f);
            Play("Move", true);
        }
        else rb.velocity = new Vector2(CanStep(facingDir) ? facingDir * 4.5f : 0, 0);
    }

    private bool CanStep(int direction)
    {
        if (platform != null)
            return transform.position.x + direction * .9f > platform.Left + .5f && transform.position.x + direction * .9f < platform.Right - .5f;
        return Physics2D.Raycast((Vector2)transform.position + new Vector2(direction * .8f, 0), Vector2.down, 2, LayerMask.GetMask("Ground", "Wall"));
    }

    private void DrawWarning()
    {
        Color color = Kind == RunnerEnemyKind.Necromancer ? new Color(.7f, .4f, 1, .75f) :
            Kind == RunnerEnemyKind.Slime ? new Color(.4f, .8f, 1, .8f) : new Color(1, .3f, .3f, .85f);
        warning.startColor = warning.endColor = color;
        Vector2 origin = Kind == RunnerEnemyKind.Necromancer ? ShotOrigin :
            new Vector2(transform.position.x, GetComponent<Collider2D>().bounds.min.y + .08f);
        warning.SetPosition(0, origin);
        warning.SetPosition(1, origin + (Kind == RunnerEnemyKind.Necromancer ? aim * 5 : Vector2.right * facingDir * AttackRange));
    }

    private void Play(string state, bool force = false)
    {
        if (!force && animationName == state) return;
        animationName = state;
        anim.Play(state, 0, 0);
    }
    protected override void OnCombatInterrupted()
    {
        phase = Phase.Ready;
        cooldown = .55f;
        if (warning != null) warning.enabled = false;
        anim.speed = 1;
    }
    protected override void OnHurt() { Play("Hit", true); }
    protected override void OnDeath()
    {
        Play("Dead", true);
        foreach (RunnerProjectile projectile in FindObjectsOfType<RunnerProjectile>())
            if (projectile.Owner == this) Destroy(projectile.gameObject);
    }
    private void OnDestroy() { if (warningMaterial != null) Destroy(warningMaterial); }
}
