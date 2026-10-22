using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerGroundedState : PlayerState
{
    public PlayerGroundedState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName)
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

        if(Input.GetKeyDown(KeyCode.Mouse0) && !player.isBusy)
        {
            stateMachine.ChangeState(player.primaryAttackState);
            return;
        }

        if (!player.IsGroundedDetected())
            {
                stateMachine.ChangeState(player.airState);
                return;
            }

        player.TryBufferedJump();
    }
}
