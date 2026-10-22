using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Skeleton : Enemy
{
    [Header("Melee attack")]
    public int attackDamage = 20;
    public float attackRange = 1.65f;
    public Vector2 attackSize = new Vector2(2f, 2.2f);
    public Vector2 attackOffset = new Vector2(1.2f, -0.1f);
    public float attackWindup = 0.65f;
    public float attackDuration = 1.5f;
    public float attackCooldown = 0.55f;
    [System.NonSerialized] public float nextAttackTime;
    public SkeletonAttackState attackState { get; private set; }
    #region States
    public SkeletonIdleState idleState { get; private set; }
    public SkeletonMoveState moveState { get; private set; }
    public SkeletonBattleState battleState { get; private set; }
    #endregion

    protected override void Awake()
    {
        base.Awake();
        CreateStates();
    }

    private void CreateStates()
    {
        attackState = new SkeletonAttackState(this, stateMachine);

        idleState = new SkeletonIdleState(this, stateMachine, "Idle", this as Enemy_Skeleton);
        moveState = new SkeletonMoveState(this, stateMachine, "Move", this as Enemy_Skeleton);
        battleState = new SkeletonBattleState(this, stateMachine, "Move", this as Enemy_Skeleton);
    }

    protected override void RestoreStates()
    {
        CreateStates();
        stateMachine.Initialize(idleState);
    }

    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(idleState);
    }

    protected override void Update()
    {
        base.Update();
    }
}
