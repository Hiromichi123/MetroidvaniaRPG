using UnityEngine;

public class PlayerPrimaryAttackState : PlayerState
{
    private int comboCounter;
    private float lastTimeAttack = -10;
    private float elapsed;
    private bool hit;
    private bool queued;
    private readonly float[] hitTimes = { 0.2f, 0.12f, 0.25f };
    private readonly float[] endTimes = { 0.64f, 0.32f, 0.57f };
    public PlayerPrimaryAttackState(Player player, PlayerStateMachine machine, string animation)
        : base(player, machine, animation) { }
    public override void Enter()
    {
        base.Enter();
        if (comboCounter > 2 || Time.time - lastTimeAttack > 1f) comboCounter = 0;
        hit = false;
        queued = false;
        elapsed = 0;
        player.anim.SetInteger("ComboCounter", comboCounter);
        player.anim.Play("Base Layer.PrimaryAttack.playerAttack" + (comboCounter + 1), 0, 0);
        player.FlipController(Input.GetAxisRaw("Horizontal"));
        Vector2 movement = player.attackMovement != null && comboCounter < player.attackMovement.Length
            ? player.attackMovement[comboCounter] : Vector2.zero;
        if (RunnerGame.Instance == null) player.SetVelocity(movement.x * player.facingDir, movement.y);
        player.anim.speed = player.AttackRate;
        stateTimer = .1f;
    }
    public override void Update()
    {
        base.Update();
        elapsed += Time.deltaTime * player.anim.speed;
        if (Input.GetKeyDown(KeyCode.Mouse0)) queued = true;
        if (!hit && elapsed >= hitTimes[comboCounter])
        {
            hit = true;
            if (RunnerGame.Instance != null) player.OpenParryWindow(.2f);
            CombatHit.Strike(player, player.attackDamage + comboCounter * 5, player.attackSize, player.attackOffset);
        }
        if (RunnerGame.Instance != null)
        {
            if (player.TryBufferedJump()) return;
            player.SetVelocity(Mathf.MoveTowards(rb.velocity.x, xInput * player.MovementSpeed, 70 * Time.deltaTime), rb.velocity.y);
        }
        else if (stateTimer <= 0) player.zeroVelocity();
        if (triggerCalled || elapsed >= endTimes[comboCounter])
        {
            bool continueCombo = queued && comboCounter < 2;
            if (RunnerGame.Instance == null) stateMachine.ChangeState(player.idleState);
            else stateMachine.ChangeState(player.IsGroundedDetected() ? (xInput == 0 ? (PlayerState)player.idleState : player.moveState) : player.airState);
            if (continueCombo) stateMachine.ChangeState(player.primaryAttackState);
        }
    }
    public override void Exit()
    {
        base.Exit();
        player.CloseParryWindow();
        player.anim.speed = 1;
        comboCounter++;
        lastTimeAttack = Time.time;
        player.StartCoroutine(player.BusyFor(RunnerGame.Instance != null ? .05f : .1f));
    }
}
