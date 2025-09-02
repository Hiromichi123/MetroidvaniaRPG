using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWallJumpState : PlayerState
{
    public PlayerWallJumpState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        stateTimer = .4f; // 状态持续时间
        player.SetVelocity(5 * -player.facingDir, player.jumpForce); // 反方向跳跃
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        if (stateTimer < 0)
        {   // 悬空或超时，进入airState
            stateMachine.ChangeState(player.airState);
        }

        if (player.IsGroundedDetected())
        {   // 落地进入idleState
            stateMachine.ChangeState(player.idleState);
        }
    }
}
