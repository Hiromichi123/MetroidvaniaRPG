using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {
    // 移动量
    #region Move
    [Header("移动")]
    [SerializeField]public float moveSpeed;
    [SerializeField]public float jumpForce;
    public int facingDir { get; private set; } = 1;
    private bool facingRight = true;
    #endregion
    // 冲刺量
    #region Dash
    [Header("冲刺")]
    [SerializeField]public float dashCoolDown;
    private float dashUsageTimer;
    [SerializeField]public float dashSpeed;
    [SerializeField]public float dashDuration;
    public float dashDir { get; private set; } // 冲刺方向，区别于faceDir
    #endregion
    // 碰撞检测量
    #region Collision
    [Header("碰撞检测")]
    [SerializeField]public Transform groundCheck;
    [SerializeField]public float groundCheckDistance;
    [SerializeField]public LayerMask whatIsGround;
    [SerializeField]public Transform wallCheck;
    [SerializeField]public float wallCheckDistance;
    [SerializeField]public LayerMask whatIsWall;
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
    #endregion

    private void Awake()
    { // 优先级高于Start的初始化
        stateMachine = new PlayerStateMachine();

        idleState = new PlayerIdleState(this, stateMachine, "Idle");
        moveState = new PlayerMoveState(this, stateMachine, "Move");
        jumpState = new PlayerJumpState(this, stateMachine, "Jump");
        airState = new PlayerAirState(this, stateMachine, "Jump");
        dashState = new PlayerDashState(this, stateMachine, "Dash");
        wallSlideState = new PlayerWallSlideState(this, stateMachine, "WallSlide");
        wallJumpState = new PlayerWallJumpState(this, stateMachine, "Jump");
    }

    private void Start() { // 初始化
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();

        stateMachine.Initialize(idleState);
    }

    private void Update() { // 每帧更新
        stateMachine.currentState.Update();
        CheckForDashInput();
    }

    private void CheckForDashInput() { // 冲刺检查
        dashUsageTimer -= Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.LeftShift) && dashUsageTimer < 0) {
            dashUsageTimer = dashCoolDown; // 给予冲刺时间
            dashDir = Input.GetAxisRaw("Horizontal");
            if (dashDir == 0) dashDir = facingDir;
            stateMachine.ChangeState(dashState);
        }
    }

    public void SetVelocity(float _xVelocity, float _yVelocity) { // 设置速度
        rb.velocity = new Vector2(_xVelocity, _yVelocity);
        FlipController(_xVelocity); // 根据x轴速度翻转
    }

    public bool IsGroundedDetected() => Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, whatIsGround);
    public bool IsWallDetected() => Physics2D.Raycast(wallCheck.position, Vector2.right * facingDir, wallCheckDistance, whatIsWall);

    private void OnDrawGizmos() {
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(groundCheck.position, new Vector2(groundCheck.position.x, groundCheck.position.y - groundCheckDistance));
        Gizmos.color = Color.red;
        Gizmos.DrawLine(wallCheck.position, new Vector2(wallCheck.position.x + wallCheckDistance, wallCheck.position.y));
    }

    public void Flip() {
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
}
