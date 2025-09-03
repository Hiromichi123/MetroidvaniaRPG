using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIdleState : PlayerGroundedState
{
    public PlayerIdleState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        player.zeroVelocity(); // 进入Idle时水平速度归0
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        if (xInput != 0 && !player.isBusy) // 有水平输入且不忙碌时，才进入移动状态
        {
            stateMachine.ChangeState(player.moveState);
        }
    }
}
