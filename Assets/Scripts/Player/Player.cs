using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {
    #region Attack
    [Header("Attack details")]
    public Vector2[] attackMovement; // 连段攻击的位移
    public float attackSpeedIncrease; // 攻速增益
    public bool isBusy { get; private set; } // 玩家忙碌状态，防止状态切换过快
    #endregion
    // 移动与跳跃
    #region Move&Jump
    [Header("移动与跳跃")]
    [SerializeField] public float moveSpeed;
    [SerializeField] public float jumpForce;
    public int facingDir { get; private set; } = 1;
    public bool isJumping { get; private set; } = false; // 是否在跳跃中
    private bool facingRight = true;
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
    // 碰撞检测量
    #region Collision
    [Header("碰撞检测")]
    [SerializeField] public Transform groundCheck;
    [SerializeField] public float groundCheckDistance;
    [SerializeField] public LayerMask whatIsGround;
    [SerializeField] public Transform wallCheck;
    [SerializeField] public float wallCheckDistance;
    [SerializeField] public LayerMask whatIsWall;
    #endregion

    // 组件
    #region Components
    public Animator anim { get; private set; }
    public Rigidbody2D rb { get; private set; }
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
    private void Awake()
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
    private void Start() {
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();

        stateMachine.Initialize(idleState);
    }

    // 每帧更新
    private void Update()
    {
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
    public void AnimationTrigger() => stateMachine.currentState.AnimationFinishTrigger();

    // 冲刺检查
    private void CheckForDashInput()
    {
        dashUsageTimer -= Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.LeftShift) && dashUsageTimer < 0)
        {
            dashUsageTimer = dashCoolDown; // 给予冲刺时间
            dashDir = Input.GetAxisRaw("Horizontal");
            if (dashDir == 0) dashDir = facingDir;
            stateMachine.ChangeState(dashState);
        }
    }

    #region Velocity Control
    // 速度清零
    public void zeroVelocity() => rb.velocity = Vector2.zero;
    // 设置速度
    public void SetVelocity(float _xVelocity, float _yVelocity)
    {
        rb.velocity = new Vector2(_xVelocity, _yVelocity);
        FlipController(_xVelocity); // 根据x轴速度翻转
    }
    #endregion

    #region Collision Detectors
    public bool IsGroundedDetected() => Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, whatIsGround);
    public bool IsWallDetected() => Physics2D.Raycast(wallCheck.position, Vector2.right * facingDir, wallCheckDistance, whatIsWall);

    private void OnDrawGizmos() {
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(groundCheck.position, new Vector2(groundCheck.position.x, groundCheck.position.y - groundCheckDistance));
        Gizmos.color = Color.red;
        Gizmos.DrawLine(wallCheck.position, new Vector2(wallCheck.position.x + wallCheckDistance, wallCheck.position.y));
    }
    #endregion

    #region Flip
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
}
