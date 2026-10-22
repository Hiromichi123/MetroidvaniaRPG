using UnityEngine;

// Owns knockback independently of the AI so a hit can carry an enemy across gaps.
public class RunnerEnemyMomentum : MonoBehaviour
{
    public RunnerPlatform Platform;
    public bool IsSliding => remaining > 0;
    public bool WasPlayerLaunched { get; private set; }
    public bool IsPlayerDisplaced => WasPlayerLaunched &&
        (remaining > 0 || (airborne > 0 && airborne < 2 && enemy != null && Mathf.Abs(enemy.rb.velocity.x) > 6));
    private Enemy enemy;
    private Collider2D body;
    private PhysicsMaterial2D slideMaterial;
    private float remaining, airborne, deathAge;
    private bool frozen;
    private Vector2 frozenVelocity;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        body = GetComponent<Collider2D>();
        slideMaterial = new PhysicsMaterial2D("Runner sliding") { friction = 0, bounciness = 0 };
        body.sharedMaterial = slideMaterial;
    }

    public void Launch(int direction, float speed)
    {
        if (enemy.IsDead) return;
        WasPlayerLaunched = true;
        remaining = 1.05f;
        airborne = 0;
        enemy.HoldKnockback(remaining);
        enemy.rb.velocity = new Vector2(direction * speed, 6);
    }

    private void FixedUpdate()
    {
        RunnerGame game = RunnerGame.Instance;
        if (game == null) return;
        if (enemy.IsDead)
        {
            deathAge += Time.fixedDeltaTime;
            if (deathAge >= 2.5f) Destroy(gameObject);
            return;
        }
        if (game.IsPaused)
        {
            if (!frozen) { frozenVelocity = enemy.rb.velocity; enemy.rb.simulated = false; enemy.anim.speed = 0; frozen = true; }
            return;
        }
        if (frozen) { enemy.rb.simulated = true; enemy.rb.velocity = frozenVelocity; enemy.anim.speed = 1; frozen = false; }
        Bounds bounds = body.bounds;
        RaycastHit2D ground = Physics2D.BoxCast(new Vector2(bounds.center.x, bounds.min.y + .06f),
            new Vector2(bounds.size.x * .7f, .06f), 0, Vector2.down, .18f, LayerMask.GetMask("Ground"));
        bool grounded = ground.collider != null && enemy.rb.velocity.y <= .5f;
        if (grounded)
        {
            RunnerPlatform landed = ground.collider.GetComponent<RunnerPlatform>();
            if (landed != null) Platform = landed;
        }
        if (remaining > 0)
        {
            remaining = Mathf.Max(0, remaining - Time.fixedDeltaTime);
            enemy.HoldKnockback(.06f);
            if (grounded)
                enemy.rb.velocity = new Vector2(Mathf.MoveTowards(enemy.rb.velocity.x, 0, 3 * Time.fixedDeltaTime), enemy.rb.velocity.y);
            if (remaining <= 0 && grounded)
                enemy.rb.velocity = new Vector2(0, enemy.rb.velocity.y);
        }
        else if (!grounded && airborne < 2 && Mathf.Abs(enemy.rb.velocity.x) > 6)
        {
            airborne += Time.fixedDeltaTime;
            enemy.HoldKnockback(.06f);
        }
        if (transform.position.y < -transform.position.x * RunnerGenerator.Slope - 25 ||
            transform.position.x < game.Player.transform.position.x - game.Generator.Behind - 12)
            GetComponent<RunnerEnemySalvage>()?.Convert();
    }

    private void OnDestroy() { if (slideMaterial != null) Destroy(slideMaterial); }
}
