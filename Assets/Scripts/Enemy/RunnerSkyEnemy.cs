using UnityEngine;

public class RunnerSkyEnemy : Enemy
{
    public Sprite[] HoverFrames, CastFrames, HurtFrames, DeathFrames;
    public RunnerProjectile ProjectilePrefab;
    public float Windup = .8f;
    public float ShotInterval = 2.8f;
    public bool IsCasting { get; private set; }
    public bool HasEntered => entry >= 1;
    public override int CoinValue => 20;
    public override int ScoreValue => 220;
    public override string DisplayName => "空中幽灵";
    protected override bool UsesStateMachine => false;
    protected override bool UsesGroundMomentum => false;
    private SpriteRenderer visual;
    private LineRenderer warning;
    private Material warningMaterial;
    private float entry, cooldown = 2, timer, frameTime, deathTime;
    private Vector2 aim;
    private bool wasPaused;

    protected override void Awake()
    {
        base.Awake();
        visual = GetComponentInChildren<SpriteRenderer>();
        warning = gameObject.AddComponent<LineRenderer>();
        warning.positionCount = 2; warning.useWorldSpace = true;
        warning.startWidth = warning.endWidth = .045f;
        warning.sortingLayerName = "Player"; warning.sortingOrder = 10;
        warningMaterial = new Material(Shader.Find("Sprites/Default")); warning.sharedMaterial = warningMaterial;
        warning.startColor = new Color(.4f, .9f, 1, .8f); warning.endColor = new Color(.4f, .9f, 1, .15f);
        warning.enabled = false;
    }
    public Vector3 ViewportAnchor => new Vector3(Mathf.Lerp(1.10f, .83f, Mathf.SmoothStep(0, 1, entry)),
        Mathf.Lerp(.84f, .73f, entry), 10);

    protected override void Update()
    {
        base.Update();
        RunnerGame game = RunnerGame.Instance;
        if (game == null) return;
        if (game.IsPaused) { wasPaused = true; return; }
        if (wasPaused) { wasPaused = false; CancelCast(); cooldown = Mathf.Max(cooldown, 1); }
        frameTime += Time.deltaTime;
        if (IsDead)
        {
            deathTime += Time.deltaTime;
            Animate(DeathFrames, deathTime, false);
            if (deathTime >= .8f) Destroy(gameObject);
            return;
        }
        if (IsStunned) { Animate(HurtFrames, frameTime, false); return; }
        rb.velocity = Vector2.zero;
        Camera view = Camera.main;
        if (view == null) return;
        entry = Mathf.Min(1, entry + Time.deltaTime / 1.6f);
        Vector3 target = view.ViewportToWorldPoint(ViewportAnchor); target.z = 0;
        transform.position = target + Vector3.up * Mathf.Sin(Time.time * 2.5f) * .18f;
        visual.flipX = game.Player.transform.position.x > transform.position.x;
        if (game.Flow.CurrentPlatform != null && game.Flow.CurrentPlatform.IsSafe)
        { CancelCast(); cooldown = 1; Animate(HoverFrames, frameTime, true); return; }
        if (IsCasting)
        {
            timer -= Time.deltaTime;
            warning.SetPosition(0, transform.position);
            warning.SetPosition(1, (Vector2)transform.position + aim * 6);
            Animate(CastFrames, Windup - timer, false);
            if (timer <= 0)
            {
                RunnerProjectile shot = Instantiate(ProjectilePrefab, transform.position, Quaternion.identity, game.Generator.transform);
                shot.Speed = 11; shot.Lifetime = 4;
                shot.Initialize(this, aim, 16);
                CancelCast(); cooldown = ShotInterval;
            }
        }
        else
        {
            Animate(HoverFrames, frameTime, true);
            cooldown -= Time.deltaTime;
            if (HasEntered && cooldown <= 0) BeginCast();
        }
    }
    public bool BeginCast()
    {
        RunnerGame game = RunnerGame.Instance;
        if (game == null || game.IsPaused || IsDead || IsStunned || IsCasting || !HasEntered || ProjectilePrefab == null ||
            (game.Flow.CurrentPlatform != null && game.Flow.CurrentPlatform.IsSafe)) return false;
        // Lock the direction during windup so movement and jumping can evade it.
        aim = ((Vector2)game.Player.GetComponent<Collider2D>().bounds.center - (Vector2)transform.position).normalized;
        IsCasting = true; timer = Windup; warning.enabled = true;
        return true;
    }
    private void Animate(Sprite[] frames, float time, bool loop)
    {
        if (frames == null || frames.Length == 0) return;
        int frame = Mathf.Max(0, Mathf.FloorToInt(time * 10));
        visual.sprite = frames[loop ? frame % frames.Length : Mathf.Min(frame, frames.Length - 1)];
    }
    private void CancelCast() { IsCasting = false; if (warning != null) warning.enabled = false; }
    protected override void OnCombatInterrupted() { CancelCast(); cooldown = 1.2f; frameTime = 0; }
    protected override void OnHurt() { }
    protected override void OnDeath()
    {
        CancelCast(); deathTime = 0;
        foreach (RunnerProjectile shot in FindObjectsOfType<RunnerProjectile>())
            if (shot.Owner == this) Destroy(shot.gameObject);
    }
    private void OnDestroy() { if (warningMaterial != null) Destroy(warningMaterial); }
    protected override void OnDrawGizmos() { }
}
