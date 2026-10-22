using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : Entity
{
    [SerializeField] protected LayerMask whatIsPlayer;
    [Header("Move info")]
    public float moveSpeed;
    public float idleTime;
    public float detectionRange = 10f;
    private bool recovering;
    public EnemyStateMachine stateMachine { get; private set; }
    protected virtual bool UsesStateMachine => true;
    protected virtual bool UsesGroundMomentum => true;
    public virtual int CoinValue => 8;
    public virtual int ScoreValue => 100;
    public virtual string DisplayName => "骷髅";
    public RunnerEnemyMomentum Momentum { get; private set; }
    public bool ShowHealthBar => !IsDead && CurrentHealth < MaxHealth;
    public int RunnerCoinValue = -1;
    public int GoldValue => RunnerCoinValue >= 0 ? RunnerCoinValue : CoinValue;
    public bool RewardClaimed { get; private set; }
    public bool TryClaimReward()
    {
        if (RewardClaimed) return false;
        RewardClaimed = true;
        return true;
    }

    protected override void Awake()
    {
        base.Awake();
        stateMachine = new EnemyStateMachine();
        if (RunnerGame.Instance != null && UsesGroundMomentum) Momentum = gameObject.AddComponent<RunnerEnemyMomentum>();
        if (RunnerGame.Instance != null) gameObject.AddComponent<RunnerEnemySalvage>();
    }

    public void HoldKnockback(float seconds) { ExtendHurt(seconds); }

    protected override void Update()
    {
        if (UsesStateMachine && (stateMachine == null || stateMachine.currentState == null))
        {
            stateMachine = new EnemyStateMachine();
            RestoreStates();
        }
        base.Update();
        if (RunnerGame.Instance != null && RunnerGame.Instance.IsPaused) { if (Momentum == null) zeroVelocity(); return; }
        if (Momentum != null && Momentum.IsSliding && !IsDead) HoldKnockback(.06f);
        if (IsDead || IsStunned) return;
        if (UsesStateMachine && recovering)
        {
            recovering = false;
            if (this is Enemy_Skeleton skeleton) stateMachine.Initialize(skeleton.battleState);
        }
        if (UsesStateMachine) stateMachine.currentState?.Update();
    }

    protected virtual void RestoreStates() { }

    public virtual RaycastHit2D IsPlayerDetected()
    {
        RaycastHit2D hit = Physics2D.Raycast(wallCheck.position, Vector2.right * facingDir, detectionRange,
            whatIsPlayer | whatIsGround | whatIsWall);
        return hit.collider != null && hit.collider.GetComponentInParent<Player>() != null ? hit : default;
    }
    protected override void OnCombatInterrupted()
    {
        stateMachine.currentState?.Exit();
        recovering = true;
        anim.speed = 1;
    }
    protected override void OnHurt() { anim.Play("skeletonHit", 0, 0); }
    protected override void OnDeath() { anim.Play("skeletonDead", 0, 0); }
}
