using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Entity
{
    protected override bool ReactToDamage => RunnerGame.Instance == null;
    public float AttackRate => Mathf.Max(.1f, (1 + attackSpeedIncrease) * (RunnerGame.Instance != null ? 1.45f : 1));
    #region Attack
    [Header("Attack details")]
    public Vector2[] attackMovement; // 连段攻击的位移
    public int attackDamage = 25;
    public Vector2 attackSize = new Vector2(1.7f, 1.8f);
    public Vector2 attackOffset = new Vector2(1f, -0.1f);
    public float attackSpeedIncrease; // 攻速增益
    public bool isBusy { get; private set; } // 玩家忙碌状态，防止状态切换过快
    #endregion
    // 移动与跳跃
    #region Move&Jump
    [Header("移动与跳跃")]
    [SerializeField] public float moveSpeed;
    [SerializeField] public float jumpForce;
    public bool isJumping { get; private set; } = false; // 是否在跳跃中
    public float MovementSpeed => moveSpeed * (flow != null ? flow.Multiplier : 1);
    private RunnerFlow flow;
    private float jumpBufferedUntil = -1;
    private float groundedUntil = -1;
    private int airJumps;
    #endregion
    // 冲刺量
    #region Dash
    [Header("冲刺")]
    [SerializeField] public float dashCoolDown;
    private float dashUsageTimer;
    [SerializeField] public float dashSpeed;
    [SerializeField] public float dashDuration;
    public float dashDir { get; private set; } // 冲刺方向，区别于faceDir
    #endregion
    // 状态
    #region States
    public PlayerStateMachine stateMachine { get; private set; }
    public PlayerIdleState idleState { get; private set; }
    public PlayerMoveState moveState { get; private set; }
    public PlayerJumpState jumpState { get; private set; }
    public PlayerAirState airState { get; private set; }
    public PlayerDashState dashState { get; private set; }
    public PlayerWallSlideState wallSlideState { get; private set; }
    public PlayerWallJumpState wallJumpState { get; private set; }
    public PlayerPrimaryAttackState primaryAttackState { get; private set; }
    #endregion

    // 唤醒，初始化的初始化
    protected override void Awake()
    {
        base.Awake();
        CreateStates();
    }

    private void CreateStates()
    {
        stateMachine = new PlayerStateMachine();

        idleState = new PlayerIdleState(this, stateMachine, "Idle");
        moveState = new PlayerMoveState(this, stateMachine, "Move");
        jumpState = new PlayerJumpState(this, stateMachine, "Jump");
        airState = new PlayerAirState(this, stateMachine, "Jump");
        dashState = new PlayerDashState(this, stateMachine, "Dash");
        wallSlideState = new PlayerWallSlideState(this, stateMachine, "WallSlide");
        wallJumpState = new PlayerWallJumpState(this, stateMachine, "Jump");
        primaryAttackState = new PlayerPrimaryAttackState(this, stateMachine, "Attack");
    }

    // 初始化
    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(idleState);
        flow = GetComponent<RunnerFlow>();
        if (RunnerGame.Instance == null && GetComponent<CombatHUD>() == null) gameObject.AddComponent<CombatHUD>();
    }

    // 每帧更新
    protected override void Update()
    {
        if (stateMachine == null || stateMachine.currentState == null)
        {
            CreateStates();
            flow = GetComponent<RunnerFlow>();
            stateMachine.Initialize(idleState);
        }
        base.Update();
        if (RunnerGame.Instance != null && RunnerGame.Instance.IsPaused) return;
        if (IsRespawning) { zeroVelocity(); return; }
        if (IsDead || IsStunned) return;
        if (Input.GetKeyDown(KeyCode.Space)) jumpBufferedUntil = Time.time + .12f;
        if (IsGroundedDetected() && rb.velocity.y <= .1f && !isJumping)
        {
            groundedUntil = Time.time + .1f;
            airJumps = 0;
        }
        if (recovering) { recovering = false; anim.speed = 1; stateMachine.Initialize(IsGroundedDetected() ? (PlayerState)idleState : airState); }
        stateMachine.currentState.Update();
        CheckForDashInput();

    }

    // 玩家忙碌状态（协程等待一段时间后更改标志位）
    public IEnumerator BusyFor(float _seconds)
    {
        isBusy = true;
        yield return new WaitForSeconds(_seconds);
        isBusy = false;
    }

    // 玩家起跳状态（协程等待一段时间后更改标志位）
    public IEnumerator JumpWindow(float _seconds)
    {
        isJumping = true;
        yield return new WaitForSeconds(_seconds);
        isJumping = false;
    }

    // 动画触发
    public void AnimationTrigger() { if (!IsDead && !IsStunned) stateMachine.currentState.AnimationFinishTrigger(); }

    // 冲刺检查
    private void CheckForDashInput()
    {
        dashUsageTimer -= Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.LeftShift) && dashUsageTimer < 0 && !isBusy && stateMachine.currentState != primaryAttackState)
        {
            dashUsageTimer = dashCoolDown; // 给予冲刺时间
            dashDir = Input.GetAxisRaw("Horizontal");
            if (dashDir == 0) dashDir = facingDir;
            stateMachine.ChangeState(dashState);
        }
    }

    private bool recovering;
    private float respawnUntil;
    public bool IsRespawning => Time.time < respawnUntil;
    private float parryUntil;
    public bool IsSwordParrying => Time.time < parryUntil && !IsDead && !IsStunned &&
        stateMachine.currentState == primaryAttackState;
    public void OpenParryWindow(float seconds) { parryUntil = Time.time + seconds; }
    public void CloseParryWindow() { parryUntil = 0; }
    public bool TryParryProjectile(RunnerProjectile projectile, Vector2 from, Vector2 to)
    {
        if (!IsSwordParrying || projectile.IsReflected) return false;
        Vector2 center = (Vector2)transform.position + new Vector2(attackOffset.x * facingDir, attackOffset.y);
        Bounds sword = new Bounds(center, new Vector3(attackSize.x + projectile.Radius * 2, attackSize.y + projectile.Radius * 2, 2));
        Vector2 travel = to - from;
        bool intersects = sword.Contains(from) || (travel.sqrMagnitude > 0 &&
            sword.IntersectRay(new Ray(from, travel.normalized), out float distance) && distance <= travel.magnitude);
        return intersects && projectile.TryReflect(this);
    }

    public void ResetAfterFall(Vector3 position)
    {
        StopAllCoroutines();
        CloseParryWindow();
        stateMachine.currentState?.Exit();
        isBusy = false;
        isJumping = false;
        recovering = false;
        jumpBufferedUntil = groundedUntil = -1;
        airJumps = 0;
        ClearHurt(1.2f);
        rb.position = position;
        transform.position = position;
        rb.angularVelocity = 0;
        anim.speed = 1;
        stateMachine.Initialize(idleState);
        zeroVelocity();
        respawnUntil = Time.time + .2f;
    }
    protected override void OnCombatInterrupted()
    {
        stateMachine.currentState?.Exit();
        recovering = true;
        isBusy = false;
        anim.speed = 1;
    }
    protected override void OnHurt() { anim.speed = 0; }

    public override bool TakeDamage(int damage, Vector2 source)
    {
        RunnerGame game = RunnerGame.Instance;
        if (game != null)
        {
            if (game.IsPaused) return false;
            if (damage > 0) damage = Mathf.Max(1, damage - game.Armor);
        }
        return base.TakeDamage(damage, source);
    }

    public bool TryBufferedJump()
    {
        if (Time.time > jumpBufferedUntil) return false;
        bool grounded = Time.time <= groundedUntil && !isJumping;
        bool doubleJump = RunnerGame.Instance != null && RunnerGame.Instance.HasDoubleJump && airJumps < 1;
        if (!grounded && !doubleJump) return false;
        if (!grounded) airJumps++;
        groundedUntil = -1;
        jumpBufferedUntil = -1;
        stateMachine.ChangeState(jumpState);
        return true;
    }

    public void NotifyRunnerJump() { if (flow != null) flow.NotifyJump(); }
}
