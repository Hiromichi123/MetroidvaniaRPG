using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMoveState : PlayerGroundedState
{
    public PlayerMoveState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();
        if (stateMachine.currentState != this) return;

        player.SetVelocity(xInput * player.MovementSpeed, rb.velocity.y);

        if (xInput == 0)
        {
            stateMachine.ChangeState(player.idleState);
            return;
        }

        if (Mathf.Abs(rb.velocity.y) > .1f)
        {   // 悬空则进入airState
            stateMachine.ChangeState(player.airState);
            return;
        }
    }
}
