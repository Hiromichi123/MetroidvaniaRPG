using UnityEngine;

public class RunnerPickup : MonoBehaviour
{
    public int Value = 2;
    private bool collected;
    private Vector3 home;
    private void Start() { home = transform.localPosition; }
    private void Update()
    {
        RunnerGame game = RunnerGame.Instance;
        if (game == null || game.Player == null || game.IsPaused || game.Player.IsDead) return;
        Vector2 delta = game.Player.transform.position - transform.position;
        if (delta.sqrMagnitude < game.MagnetRadius * game.MagnetRadius)
            transform.position = Vector3.MoveTowards(transform.position, game.Player.transform.position, 18 * Time.deltaTime);
        else transform.localPosition = home + Vector3.up * Mathf.Sin(Time.time * 3 + home.x) * .12f;
        if (delta.sqrMagnitude < .85f * .85f) Collect();
    }
    public void Collect()
    {
        if (collected || RunnerGame.Instance == null) return;
        collected = true;
        RunnerGame.Instance.AddCoins(Value);
        Destroy(gameObject);
    }
}
