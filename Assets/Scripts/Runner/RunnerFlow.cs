using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class RunnerFlow : MonoBehaviour
{
    public static readonly float[] Durations = { 0, 2f, 1.6f, 1.3f, 1f, .8f };
    public int Tier { get; private set; }
    public float Remaining { get; private set; }
    public float DurationBonus;
    public float Multiplier => Tier == 0 ? 1 : 1.25f + Tier * .25f;
    public RunnerPlatform CurrentPlatform { get; private set; }
    private Player player;
    private Collider2D body;
    private int lastPlatform = -1;
    private bool jumped;
    private float airborneTime;
    private float furthestLanding = float.NegativeInfinity;
    private readonly HashSet<int> visited = new HashSet<int>();
    private readonly Queue<int> recent = new Queue<int>();
    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[12];
    private ContactFilter2D filter;

    private void Awake()
    {
        player = GetComponent<Player>();
        body = GetComponent<Collider2D>();
        filter = new ContactFilter2D();
        filter.SetLayerMask(LayerMask.GetMask("Ground"));
        filter.useTriggers = false;
    }

    private void Update()
    {
        if (RunnerGame.Instance == null || RunnerGame.Instance.IsPaused || player.IsDead) return;
        AdvanceTimer(Time.deltaTime);
    }

    public void AdvanceTimer(float delta)
    {
        if (Tier == 0) return;
        Remaining -= delta;
        while (Tier > 0 && Remaining <= 0)
        {
            Tier--;
            if (Tier > 0) Remaining += Durations[Tier] + DurationBonus;
        }
        if (Tier == 0) Remaining = 0;
    }

    public void AddTier()
    {
        Tier = Mathf.Min(5, Tier + 1);
        Remaining = Durations[Tier] + DurationBonus;
    }

    public void NotifyJump() { jumped = true; }

    public void ResetAfterFall()
    {
        Tier = 0; Remaining = 0; jumped = false; airborneTime = 0;
        CurrentPlatform = null;
    }

    private void FixedUpdate()
    {
        if (player.IsDead || RunnerGame.Instance == null || RunnerGame.Instance.IsPaused) return;
        RunnerPlatform ground = null;
        if (player.rb.velocity.y <= .2f)
        {
            Bounds bounds = body.bounds;
            int count = Physics2D.BoxCast(new Vector2(bounds.center.x, bounds.min.y + .08f),
                new Vector2(bounds.size.x * .75f, .08f), 0, Vector2.down, filter, groundHits, .2f);
            for (int i = 0; i < count; i++)
            {
                RunnerPlatform platform = groundHits[i].collider.GetComponent<RunnerPlatform>();
                if (platform != null && bounds.min.y >= platform.Top - .16f) { ground = platform; break; }
            }
        }
        CurrentPlatform = ground;
        if (ground == null) { airborneTime += Time.fixedDeltaTime; return; }
        RegisterLanding(ground, airborneTime);
        RunnerGame.Instance.RecordLanding(ground);
        airborneTime = 0;
        RunnerGame.Instance.TryEnterSanctuary(ground);
    }

    public bool RegisterLanding(RunnerPlatform platform, float airTime)
    {
        float distance = RunnerGame.Instance != null ? RunnerGame.Instance.AbsoluteX(transform.position.x) : transform.position.x;
        bool valid = jumped && airTime >= .12f && lastPlatform >= 0 && platform.Id != lastPlatform &&
            !visited.Contains(platform.Id) && distance > furthestLanding + .75f;
        if (valid) AddTier();
        if (lastPlatform != platform.Id)
        {
            if (visited.Add(platform.Id)) { recent.Enqueue(platform.Id); if (recent.Count > 128) visited.Remove(recent.Dequeue()); }
            furthestLanding = Mathf.Max(furthestLanding, distance);
            lastPlatform = platform.Id;
        }
        if (airTime > .04f) jumped = false;
        return valid;
    }
}
