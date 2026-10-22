using UnityEngine;

public class RunnerEnemySalvage : MonoBehaviour
{
    private Enemy enemy;
    private Collider2D body;
    private bool seen;
    private float age;
    private void Awake() { enemy = GetComponent<Enemy>(); body = GetComponent<Collider2D>(); }
    private void Update()
    {
        RunnerGame game = RunnerGame.Instance;
        Camera view = Camera.main;
        if (game == null || game.IsPaused || enemy.IsDead || enemy.RewardClaimed || view == null) return;
        age += Time.deltaTime;
        Bounds bounds = body.bounds;
        Vector3 low = view.WorldToViewportPoint(bounds.min), high = view.WorldToViewportPoint(bounds.max);
        bool visible = high.z > 0 && high.x >= 0 && low.x <= 1 && high.y >= 0 && low.y <= 1;
        if (visible) seen = true;
        bool outside = high.x < -.08f || low.x > 1.12f || high.y < -.12f || low.y > 1.15f;
        if (seen && age > .35f && outside) Convert();
    }
    public bool Convert()
    {
        RunnerGame game = RunnerGame.Instance;
        if (game == null || game.IsPaused || enemy.IsDead) return false;
        if (game.ConvertEnemyToGold(enemy)) return true;
        gameObject.SetActive(false);
        Destroy(gameObject);
        return false;
    }
}
