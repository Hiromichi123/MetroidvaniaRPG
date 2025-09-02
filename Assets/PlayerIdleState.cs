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
        player.SetVelocity(0, player.rb.velocity.y); // 进入Idle时水平速度归0
        base.Enter();
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        if (xInput != 0)
        {
            stateMachine.ChangeState(player.moveState);
        }

        if (rb.velocity.y != 0)
        {   // 悬空则进入airState
            stateMachine.ChangeState(player.airState);
        }
    }
}
