using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAirState : PlayerState {
    public PlayerAirState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName) {
    }

    public override void Enter() {
        base.Enter();
    }

    public override void Exit(){
        base.Exit();
    }

    public override void Update() {
        base.Update();

        if (player.TryBufferedJump()) return;
        if (RunnerGame.Instance != null && Input.GetKeyDown(KeyCode.Mouse0) && !player.isBusy)
        { stateMachine.ChangeState(player.primaryAttackState); return; }

        if (player.IsGroundedDetected() && !player.isJumping && rb.velocity.y <= .1f)
        { stateMachine.ChangeState(xInput == 0 ? (PlayerState)player.idleState : player.moveState); return; }

        if (RunnerGame.Instance != null)
            player.SetVelocity(Mathf.MoveTowards(rb.velocity.x, player.MovementSpeed * xInput, 70 * Time.deltaTime), rb.velocity.y);
        else if (xInput != 0) player.SetVelocity(player.MovementSpeed * .8f * xInput, rb.velocity.y);

        if (player.IsWallDetected()) stateMachine.ChangeState(player.wallSlideState);
    }
}
