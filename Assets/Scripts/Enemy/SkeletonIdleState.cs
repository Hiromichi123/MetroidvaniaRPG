using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkeletonIdleState : SkeletonGroundedState
{
    public SkeletonIdleState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Skeleton _enemy)
        : base(_enemyBase, _stateMachine, _animBoolName, _enemy)
    {
    }
    
    public override void Enter()
    {
        base.Enter();

        enemy.zeroVelocity();
        enemy.anim.Play("skeletonIdle", 0, 0);
        stateTimer = enemy.idleTime;
    }

    public override void Update()
    {
        base.Update();
        if (stateMachine.currentState != this) return;

        if (stateTimer < 0)
            stateMachine.ChangeState(enemy.moveState);
    }

    public override void Exit()
    {
        base.Exit();
    }
}
