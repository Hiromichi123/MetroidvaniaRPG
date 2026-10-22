using UnityEngine;

public class RunnerCamera : MonoBehaviour
{
    private Player player;
    private Camera view;
    private RunnerBackdrop scenery;
    private RunnerBackdrop template;
    public const float DefaultSize = 11f;
    private float shakeRemaining, shakeStrength;
    private Vector3 lastShake;
    public bool ArenaLocked { get; private set; }
    private RunnerPlatform arena;
    public void LockArena(RunnerPlatform platform)
    {
        arena = platform; ArenaLocked = true;
        lastShake = Vector3.zero; shakeRemaining = 0;
        SetArenaView();
    }
    private void SetArenaView()
    {
        if (view == null) view = GetComponent<Camera>();
        view.orthographicSize = Mathf.Max(12.5f, arena.Width / (2 * Mathf.Max(.6f, view.aspect)) + 2);
        transform.position = new Vector3(arena.transform.position.x, arena.Top + 5, -10);
    }
    public void UseRegion(RunnerRegionDefinition region)
    {
        ArenaLocked = false; arena = null;
        if (region == null || region.Backdrop == null) return;
        if (scenery != null) { scenery.gameObject.SetActive(false); Destroy(scenery.gameObject); }
        template = region.Backdrop;
        scenery = Instantiate(template);
        scenery.Initialize(view != null ? view : GetComponent<Camera>());
    }
    public void AddImpact(int targets)
    {
        shakeRemaining = .13f;
        shakeStrength = Mathf.Min(.25f, .09f + targets * .035f);
    }
    public void Initialize(Player target, Sprite background, Sprite cityBackground, RunnerBackdrop backgroundPrefab = null)
    {
        player = target;
        template = backgroundPrefab;
        view = GetComponent<Camera>();
        view.orthographic = true;
        view.orthographicSize = DefaultSize;
        view.backgroundColor = new Color(.055f, .075f, .12f);
        transform.position = new Vector3(target.transform.position.x + 4.5f, target.transform.position.y - 1.8f, -10);
        EnsureBackdrop();
    }

    private void EnsureBackdrop()
    {
        if (scenery != null) return;
        scenery = FindObjectOfType<RunnerBackdrop>();
        if (scenery == null)
        {
            if (template == null)
            {
                GameObject prefab = Resources.Load<GameObject>("Runner/DemoBackdrop");
                if (prefab != null) template = prefab.GetComponent<RunnerBackdrop>();
            }
            if (template == null) return;
            // Remove obsolete runtime layers left by a live script update.
            foreach (string name in new[] { "Battle demo sky", "Battle demo city", "Distant castle" })
            {
                GameObject old = GameObject.Find(name);
                if (old != null) Destroy(old);
            }
            scenery = Instantiate(template);
        }
        scenery.Initialize(view);
    }

    private void LateUpdate()
    {
        transform.position -= lastShake;
        lastShake = Vector3.zero;
        if (player == null) player = FindObjectOfType<Player>();
        if (player == null) return;
        if (view == null) view = GetComponent<Camera>();
        EnsureBackdrop();
        if (ArenaLocked && arena != null) { SetArenaView(); return; }
        float speed = RunnerGame.Instance != null ? RunnerGame.Instance.Flow.Multiplier : 1;
        view.orthographicSize = Mathf.Lerp(view.orthographicSize, DefaultSize + (speed - 1) * .65f, Time.deltaTime * 2);
        Vector3 target = new Vector3(player.transform.position.x + 4.5f + (speed - 1) * 2.5f, player.transform.position.y - 1.8f, -10);
        transform.position = Vector3.Lerp(transform.position, target, 1 - Mathf.Exp(-9 * Time.deltaTime));
        if (shakeRemaining > 0 && RunnerGame.Instance != null && !RunnerGame.Instance.IsPaused)
        {
            shakeRemaining = Mathf.Max(0, shakeRemaining - Time.deltaTime);
            float strength = shakeStrength * (shakeRemaining / .13f);
            lastShake = new Vector3(Mathf.Sin(Time.time * 137), Mathf.Cos(Time.time * 113), 0) * strength;
            transform.position += lastShake;
        }
    }
    private void OnDestroy() { if (scenery != null) Destroy(scenery.gameObject); }
}
