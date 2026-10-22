using UnityEngine;

public class SkeletonBattleState : EnemyState
{
    private Player player;
    private readonly Enemy_Skeleton enemy;
    public SkeletonBattleState(Enemy enemyBase, EnemyStateMachine machine, string animation, Enemy_Skeleton enemy)
        : base(enemyBase, machine, animation) { this.enemy = enemy; }
    public override void Enter()
    {
        base.Enter();
        player = Object.FindObjectOfType<Player>();
        enemy.anim.SetBool("Idle", false);
        enemy.anim.Play("skeletonMove", 0, 0);
    }
    public override void Update()
    {
        base.Update();
        if (player == null || player.IsDead || Vector2.Distance(player.transform.position, enemy.transform.position) > enemy.detectionRange * 1.5f)
        {
            enemy.zeroVelocity();
            stateMachine.ChangeState(enemy.idleState);
            return;
        }
        Vector2 delta = player.transform.position - enemy.transform.position;
        int direction = delta.x >= 0 ? 1 : -1;
        enemy.FlipController(direction);
        if (Mathf.Abs(delta.x) <= enemy.attackRange && Mathf.Abs(delta.y) <= 1.5f)
        {
            enemy.zeroVelocity();
            enemy.anim.SetBool("Move", false);
            enemy.anim.SetBool("Idle", true);
            if (Time.time >= enemy.nextAttackTime) stateMachine.ChangeState(enemy.attackState);
            return;
        }
        enemy.anim.SetBool("Idle", false);
        enemy.anim.SetBool("Move", true);
        Vector2 edge = (Vector2)enemy.transform.position + new Vector2(direction * 0.85f, -0.5f);
        bool floorAhead = Physics2D.Raycast(edge, Vector2.down, 1.7f, LayerMask.GetMask("Ground", "Wall"));
        enemy.SetVelocity(enemy.IsWallDetected() || !floorAhead ? 0 : enemy.moveSpeed * direction, rb.velocity.y);
    }
}
