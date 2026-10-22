using UnityEngine;

public class RunnerProjectile : MonoBehaviour
{
    public Sprite[] Frames;
    public float Speed = 10;
    public float Lifetime = 1.8f;
    public float Radius = .22f;
    public Entity Owner { get; private set; }
    public Entity SourceEnemy { get; private set; }
    public int Damage { get; private set; }
    public bool IsReflected { get; private set; }
    public Vector2 Direction => direction;
    private Vector2 direction;
    private float age;
    private bool spent;
    private SpriteRenderer visual;
    public void Initialize(Entity owner, Vector2 aim, int damage)
    {
        Owner = SourceEnemy = owner;
        direction = aim.normalized;
        Damage = damage;
        visual = GetComponent<SpriteRenderer>();
        FaceDirection();
    }
    private void FaceDirection()
    {
        transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
    }
    public bool TryReflect(Player player)
    {
        if (spent || IsReflected || player == null || player.IsDead || player.IsStunned ||
            Owner == null || Owner.IsDead || (RunnerGame.Instance != null && RunnerGame.Instance.IsPaused)) return false;
        if (Physics2D.Linecast(player.transform.position, transform.position, LayerMask.GetMask("Ground", "Wall"))) return false;
        IsReflected = true;
        Owner = player;
        direction = SourceEnemy != null ? ((Vector2)SourceEnemy.GetComponent<Collider2D>().bounds.center - (Vector2)transform.position).normalized : -direction;
        Damage = Mathf.Max(Damage * 3, player.attackDamage * 2);
        Speed = Mathf.Max(24, Speed * 1.65f + player.MovementSpeed);
        Lifetime = 4;
        age = 0;
        if (visual != null) visual.color = new Color(1, .8f, .25f);
        FaceDirection();
        RunnerHitImpact.Spawn(transform.position, player.facingDir);
        Camera.main?.GetComponent<RunnerCamera>()?.AddImpact(1);
        return true;
    }
    private void FixedUpdate()
    {
        if (spent) return;
        if (Owner == null || Owner.IsDead) { Destroy(gameObject); return; }
        if (RunnerGame.Instance != null && RunnerGame.Instance.IsPaused) return;
        float delta = Time.fixedDeltaTime;
        if (IsReflected && SourceEnemy != null && !SourceEnemy.IsDead)
        {
            Vector2 target = ((Vector2)SourceEnemy.GetComponent<Collider2D>().bounds.center - (Vector2)transform.position).normalized;
            direction = Vector2.Lerp(direction, target, 1 - Mathf.Exp(-10 * delta)).normalized;
            FaceDirection();
        }
        float distance = Speed * delta;
        if (!IsReflected && RunnerGame.Instance != null &&
            RunnerGame.Instance.Player.TryParryProjectile(this, transform.position, (Vector2)transform.position + direction * distance)) return;
        int mask = IsReflected ? LayerMask.GetMask("Enemy", "Ground", "Wall") : LayerMask.GetMask("Player", "Ground", "Wall");
        RaycastHit2D hit = Physics2D.CircleCast(transform.position, Radius, direction, distance, mask);
        if (hit.collider != null)
        {
            spent = true;
            Entity target = IsReflected ? (Entity)hit.collider.GetComponentInParent<Enemy>() : hit.collider.GetComponentInParent<Player>();
            if (target != null && !target.IsDead && target.TakeDamage(Damage, Owner.transform.position) && IsReflected)
            {
                RunnerHitImpact.Spawn(hit.point, direction.x >= 0 ? 1 : -1);
                Camera.main?.GetComponent<RunnerCamera>()?.AddImpact(1);
                if (target.IsDead && RunnerGame.Instance != null) RunnerGame.Instance.RewardKill(target, Owner);
            }
            Destroy(gameObject);
            return;
        }
        transform.position += (Vector3)(direction * distance);
        age += delta;
        if (visual != null && Frames != null && Frames.Length > 0) visual.sprite = Frames[Mathf.FloorToInt(age * 12) % Frames.Length];
        if (age >= Lifetime) Destroy(gameObject);
    }
}
