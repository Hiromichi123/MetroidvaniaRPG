using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWallSlideState : PlayerState {
    public PlayerWallSlideState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName) {
    }

    public override void Enter() {
        base.Enter();
    }

    public override void Exit() {
        base.Exit();
    }

    public override void Update() {
        base.Update();

        if (Input.GetKeyDown(KeyCode.Space))
        {   // 滑墙时跳跃
            stateMachine.ChangeState(player.wallJumpState);
            return;
        }

        if (xInput != 0 && xInput != player.facingDir)
        { // 需要从墙上脱离
            stateMachine.ChangeState(player.idleState);
        }

        rb.velocity = new Vector2(0, -player.moveSpeed * .5f); // 滑墙速度0.5

        if (player.IsGroundedDetected()) {
            stateMachine.ChangeState(player.idleState);
        }
    }
}
