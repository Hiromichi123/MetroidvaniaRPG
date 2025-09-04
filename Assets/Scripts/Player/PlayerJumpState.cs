using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerJumpState : PlayerState
{
    public PlayerJumpState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        rb.velocity = new Vector2(rb.velocity.x, player.jumpForce);
        player.StartCoroutine(player.JumpWindow(.1f)); // 起跳状态持续时间，防止误判为落地
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        if (Mathf.Abs(rb.velocity.y) > .1f)
        {   // 悬空则进入airState
            stateMachine.ChangeState(player.airState);
        }
    }
}
