using UnityEngine;

// Shared health and hit detection. An attack owns its hit list, so multiple colliders
// on the same character never multiply damage.
public static class CombatHit
{
    public static int Strike(Entity attacker, int damage, Vector2 size, Vector2 offset)
    {
        if (attacker.IsDead || attacker.IsStunned || (RunnerGame.Instance != null && RunnerGame.Instance.IsPaused)) return 0;
        Vector2 origin = attacker.transform.position;
        Vector2 center = origin + new Vector2(offset.x * attacker.facingDir, offset.y);
        var victims = new System.Collections.Generic.HashSet<Entity>();
        int hits = 0;
        int parries = 0;
        if (attacker is Player swordsman && RunnerGame.Instance != null)
        {
            Bounds sword = new Bounds(center, new Vector3(size.x, size.y, 2));
            foreach (RunnerProjectile shot in Object.FindObjectsOfType<RunnerProjectile>())
            {
                Vector3 nearest = sword.ClosestPoint(shot.transform.position);
                if ((nearest - shot.transform.position).sqrMagnitude <= shot.Radius * shot.Radius && shot.TryReflect(swordsman)) parries++;
            }
        }
        foreach (Collider2D collider in Physics2D.OverlapBoxAll(center, size, 0))
        {
            Entity target = collider.GetComponentInParent<Entity>();
            if (target == null || target == attacker || target.IsDead ||
                (attacker is Player) == (target is Player) || !victims.Add(target)) continue;
            Vector2 targetPoint = collider.bounds.center;
            if (Physics2D.Linecast(origin, targetPoint, LayerMask.GetMask("Ground", "Wall"))) continue;
            if (target.TakeDamage(damage, origin))
            {
                hits++;
                if (!target.IsDead && attacker is Player runner && target is Enemy enemy && enemy.Momentum != null)
                    enemy.Momentum.Launch(attacker.facingDir, 24 + Mathf.Min(6, Mathf.Abs(runner.rb.velocity.x) * .3f));
                if (attacker is Player && RunnerGame.Instance != null)
                    RunnerHitImpact.Spawn(targetPoint, attacker.facingDir);
                if (target.IsDead && RunnerGame.Instance != null) RunnerGame.Instance.RewardKill(target, attacker);
            }
        }
        if (hits + parries > 0 && attacker is Player && Camera.main != null)
            Camera.main.GetComponent<RunnerCamera>()?.AddImpact(hits + parries);
        return hits;
    }
}
