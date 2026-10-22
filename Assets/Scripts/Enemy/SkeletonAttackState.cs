using UnityEngine;

public class SkeletonAttackState : EnemyState
{
    private readonly Enemy_Skeleton enemy;
    private bool hit;
    public SkeletonAttackState(Enemy_Skeleton enemy, EnemyStateMachine machine) : base(enemy, machine, "Idle")
    {
        this.enemy = enemy;
    }
    public override void Enter()
    {
        base.Enter();
        enemy.zeroVelocity();
        enemy.anim.Play("skeletonAttack", 0, 0);
        stateTimer = enemy.attackDuration;
        hit = false;
    }
    public override void Update()
    {
        base.Update();
        if (!hit && stateTimer <= enemy.attackDuration - enemy.attackWindup)
        {
            hit = true;
            CombatHit.Strike(enemy, enemy.attackDamage, enemy.attackSize, enemy.attackOffset);
        }
        if (stateTimer <= 0) stateMachine.ChangeState(enemy.battleState);
    }
    public override void Exit()
    {
        base.Exit();
        enemy.nextAttackTime = Time.time + enemy.attackCooldown;
    }
}
