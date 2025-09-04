using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPrimaryAttackState : PlayerState
{
    private int comboCounter; // 连击计数器
    private float lastTimeAttack;
    private float comboWindow = 1f;
    public PlayerPrimaryAttackState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        if (comboCounter > 2 || Time.time - lastTimeAttack > comboWindow)
            comboCounter = 0;

        player.anim.SetInteger("ComboCounter", comboCounter);

        float attackDir = player.facingDir;
        if (xInput != 0) attackDir = xInput; // 有输入则按输入方向攻击，连段更灵活

        player.SetVelocity(player.attackMovement[comboCounter].x * attackDir, player.attackMovement[comboCounter].y); // 连段攻击的位移
        player.anim.speed = 1f + player.attackSpeedIncrease;

        stateTimer = .1f; // 增加动作的惯性时间，防止状态突变
    }

    public override void Exit()
    {
        base.Exit();

        comboCounter++;
        lastTimeAttack = Time.time;

        player.StartCoroutine("BusyFor", .1f); // 用于抵消idleState的短暂真空
    }

    public override void Update()
    {
        base.Update();

        if (stateTimer < 0) // 攻击状态停止移动
            player.zeroVelocity();

        if (triggerCalled) // 这里是每段attack动画结束触发
            {
                stateMachine.ChangeState(player.idleState);
            }
    }
}
