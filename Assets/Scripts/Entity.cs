using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Entity : MonoBehaviour
{
    [Header("Combat")]
    [SerializeField, Min(1)] private int maxHealth = 100;
    [SerializeField] private float hurtDuration = 0.22f;
    [SerializeField] private Vector2 knockback = new Vector2(3f, 2f);
    public int MaxHealth => maxHealth;
    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }
    public bool IsStunned => Time.time < hurtUntil;
    private float hurtUntil;
    private float invulnerableUntil;
    private SpriteRenderer[] renderers;
    private Color[] originalColors;
    protected virtual bool ReactToDamage => true;

    protected void ExtendHurt(float seconds) { hurtUntil = Mathf.Max(hurtUntil, Time.time + seconds); }
    protected void ClearHurt(float protection)
    {
        hurtUntil = 0;
        invulnerableUntil = Time.time + protection;
    }

    public void Heal(int amount)
    {
        if (!IsDead && amount > 0) CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
    }

    public virtual bool TakeDamage(int damage, Vector2 source)
    {
        if (damage <= 0 || IsDead || Time.time < invulnerableUntil) return false;
        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        IsDead = CurrentHealth == 0;
        hurtUntil = ReactToDamage ? Time.time + hurtDuration : 0;
        invulnerableUntil = Time.time + (this is Player ? 0.65f : 0.1f);
        if (IsDead || ReactToDamage) OnCombatInterrupted();
        if (IsDead)
        {
            StopAllCoroutines();
            rb.velocity = Vector2.zero;
            foreach (Collider2D collider in GetComponentsInChildren<Collider2D>()) collider.enabled = false;
            rb.simulated = false;
            OnDeath();
        }
        else
        {
            if (ReactToDamage)
            {
                rb.velocity = new Vector2((transform.position.x >= source.x ? 1 : -1) * knockback.x, knockback.y);
                OnHurt();
            }
        }
        return true;
    }

    protected virtual void OnCombatInterrupted() { }
    protected virtual void OnHurt() { }
    protected virtual void OnDeath()
    {
        if (anim != null) anim.speed = 0;
    }
    // 基本组件
    #region Components
    public Animator anim { get; private set; }
    public Rigidbody2D rb { get; private set; }
    #endregion
    // 速度
    #region Velocity
    // 速度清零
    public void zeroVelocity() => rb.velocity = Vector2.zero;
    // 设置速度
    public void SetVelocity(float _xVelocity, float _yVelocity)
    {
        rb.velocity = new Vector2(_xVelocity, _yVelocity);
        FlipController(_xVelocity); // 根据x轴速度翻转
    }
    #endregion
    // 转向
    #region Flip
    public int facingDir { get; private set; } = 1;
    protected bool facingRight = true;

    public void Flip()
    {
        facingDir *= -1;
        facingRight = !facingRight;
        transform.Rotate(0.0f, 180.0f, 0.0f);
    }

    public void FlipController(float _x) {
        if (_x > 0 && !facingRight) {
            Flip();
        } else if (_x < 0 && facingRight) {
            Flip();
        }
    }
    #endregion
    // 碰撞检测
    #region Collision
    [Header("碰撞检测")]
    [SerializeField] protected Transform groundCheck;
    [SerializeField] protected float groundCheckDistance;
    [SerializeField] protected LayerMask whatIsGround;
    [SerializeField] protected Transform wallCheck;
    [SerializeField] protected float wallCheckDistance;
    [SerializeField] protected LayerMask whatIsWall;

    public virtual bool IsGroundedDetected() => Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, whatIsGround);
    public virtual bool IsWallDetected() => Physics2D.Raycast(wallCheck.position, Vector2.right * facingDir, wallCheckDistance, whatIsWall);
    protected virtual void OnDrawGizmos() {
        if (groundCheck == null || wallCheck == null) return;
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(groundCheck.position, new Vector2(groundCheck.position.x, groundCheck.position.y - groundCheckDistance));
        Gizmos.color = Color.red;
        Gizmos.DrawLine(wallCheck.position, new Vector2(wallCheck.position.x + wallCheckDistance * facingDir, wallCheck.position.y));
    }
    #endregion

    protected virtual void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();
        CurrentHealth = maxHealth;
        renderers = GetComponentsInChildren<SpriteRenderer>();
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) originalColors[i] = renderers[i].color;
    }

    protected virtual void Start()
    {
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    protected virtual void Update()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            renderers[i].color = IsDead ? new Color(0.55f, 0.55f, 0.55f, 0.7f) :
                Time.time < invulnerableUntil && Mathf.FloorToInt(Time.time * 20) % 2 == 0 ? Color.red : originalColors[i];
        }
        if (RunnerGame.Instance == null && !IsDead && transform.position.y < -30f) TakeDamage(MaxHealth, transform.position);
    }
}
