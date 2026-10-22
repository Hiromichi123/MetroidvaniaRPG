using UnityEngine;

public class RunnerSkyDirector : MonoBehaviour
{
    public float FirstArrival = 6;
    public float RespawnDelay = 12;
    public RunnerSkyEnemy ActiveEnemy { get; private set; }
    private float timer;
    private RunnerGame game;
    private RunnerSkyEnemy prefab;
    private void Awake()
    {
        game = GetComponent<RunnerGame>(); timer = FirstArrival;
        GameObject asset = Resources.Load<GameObject>("Runner/Enemies/RunnerSkyGhost");
        if (asset != null) prefab = asset.GetComponent<RunnerSkyEnemy>();
    }
    private void Update()
    {
        if (game == null || game.IsPaused || game.InBossArena || prefab == null) return;
        if (ActiveEnemy != null) { timer = RespawnDelay; return; }
        if (game.Flow.CurrentPlatform != null && game.Flow.CurrentPlatform.IsSafe) return;
        timer -= Time.deltaTime;
        if (timer <= 0) SpawnNow();
    }
    public RunnerSkyEnemy SpawnNow()
    {
        if (prefab == null || ActiveEnemy != null || game.IsPaused || game.InBossArena) return ActiveEnemy;
        Vector3 position = Camera.main != null ? Camera.main.ViewportToWorldPoint(new Vector3(1.1f, .84f, 10)) : game.Player.transform.position + new Vector3(20, 8);
        position.z = 0;
        ActiveEnemy = Instantiate(prefab, position, Quaternion.identity, game.Generator.transform);
        timer = RespawnDelay;
        return ActiveEnemy;
    }
}
