using UnityEngine;

public class RunnerGoldFlight : MonoBehaviour
{
    public int Amount { get; private set; }
    private bool collected;
    private float age, speed = 25;
    private Sprite sprite;
    public static RunnerGoldFlight Spawn(Vector3 origin, int amount)
    {
        RunnerGame game = RunnerGame.Instance;
        GameObject root = new GameObject("Enemy gold return");
        root.transform.SetParent(game.Generator.transform);
        Camera view = Camera.main;
        if (view != null)
        {
            Vector3 point = view.WorldToViewportPoint(origin);
            point.x = Mathf.Clamp(point.x, -.04f, 1.04f); point.y = Mathf.Clamp(point.y, -.04f, 1.04f);
            origin = view.ViewportToWorldPoint(point); origin.z = 0;
        }
        root.transform.position = origin;
        RunnerGoldFlight gold = root.AddComponent<RunnerGoldFlight>(); gold.Amount = Mathf.Max(0, amount);
        gold.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1);
        for (int i = 0; i < 3; i++)
        {
            GameObject pip = new GameObject("Gold"); pip.transform.SetParent(root.transform, false);
            pip.transform.localPosition = new Vector3((i - 1) * .28f, Mathf.Abs(i - 1) * .16f, 0);
            pip.transform.localScale = Vector3.one * .24f; pip.transform.localRotation = Quaternion.Euler(0, 0, 45);
            SpriteRenderer visual = pip.AddComponent<SpriteRenderer>(); visual.sprite = gold.sprite;
            visual.color = new Color(1, .78f, .2f); visual.sortingLayerName = "Player"; visual.sortingOrder = 25;
        }
        return gold;
    }
    private void Update()
    {
        RunnerGame game = RunnerGame.Instance;
        if (game == null || game.IsPaused || game.Player == null || game.Player.IsDead || collected) return;
        age += Time.deltaTime; speed += 120 * Time.deltaTime;
        Vector3 target = game.Player.transform.position + Vector3.up * .2f;
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        if ((target - transform.position).sqrMagnitude < .5f || age >= 1.2f) Collect();
    }
    public void Collect()
    {
        RunnerGame game = RunnerGame.Instance;
        if (collected || game == null || game.IsPaused) return;
        collected = true; game.AddCoins(Amount); Destroy(gameObject);
    }
    private void OnDestroy() { if (sprite != null) Destroy(sprite); }
}
