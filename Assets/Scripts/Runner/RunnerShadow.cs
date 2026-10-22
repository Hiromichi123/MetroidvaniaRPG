using UnityEngine;

public class RunnerShadow : MonoBehaviour
{
    public float X { get; private set; } = -6;
    public float Distance => game.Player.transform.position.x - X;
    public float BaseSpeed => Mathf.Min(12.5f, 8.5f + (game.Stage - 1) * .35f);
    public float CatchupBonus => Mathf.SmoothStep(0, 7.5f, Mathf.InverseLerp(28, 90, Distance));
    public float Speed => BaseSpeed + CatchupBonus;
    public float CurrentSpeed { get; private set; }
    private float startDelay = 3;
    private RunnerGame game;
    private SpriteRenderer body;
    private Sprite[] frames;
    private GameObject glow;

    public void Initialize(RunnerGame owner, Texture2D sheet)
    {
        game = owner;
        CurrentSpeed = BaseSpeed;
        body = gameObject.AddComponent<SpriteRenderer>();
        body.sortingOrder = 12;
        body.color = new Color(.24f, .12f, .35f, .95f);
        if (sheet != null)
        {
            // Existing Bringer of Death sheet: eight columns and eight rows.
            int w = sheet.width / 8, h = sheet.height / 8;
            frames = new Sprite[8];
            for (int i = 0; i < frames.Length; i++)
                frames[i] = Sprite.Create(sheet, new Rect(i * w + 80, sheet.height - h, 52, 65), new Vector2(.5f, .5f), 16);
            body.sprite = frames[0];
        }
        transform.localScale = new Vector3(4f, 4f, 1);
        glow = new GameObject("Shadow aura");
        glow.transform.SetParent(transform, false);
        SpriteRenderer aura = glow.AddComponent<SpriteRenderer>();
        aura.sprite = body.sprite;
        aura.color = new Color(.5f, .18f, .8f, .2f);
        aura.sortingOrder = 11;
        glow.transform.localScale = Vector3.one * 1.15f;
    }

    private void Update()
    {
        if (game.Player == null || game.IsPaused || game.InBossArena) return;
        Advance(Time.deltaTime);
        transform.position = new Vector3(X - 5f, -X * RunnerGenerator.Slope + 6.3f + Mathf.Sin(Time.time * 2) * .35f, 0);
        if (frames != null)
        {
            body.sprite = frames[Mathf.FloorToInt(Time.time * 8) % frames.Length];
            glow.GetComponent<SpriteRenderer>().sprite = body.sprite;
        }
    }

    public void Advance(float delta)
    {
        if (game.IsPaused || game.InBossArena) return;
        if (startDelay > 0) { startDelay = Mathf.Max(0, startDelay - delta); return; }
        CurrentSpeed = Mathf.MoveTowards(CurrentSpeed, Speed, 3f * delta);
        X += CurrentSpeed * delta;
        if (Distance <= .6f) game.EndRun("被影子吞没");
    }

    public void GiveHeadStart(float behindX) { X = Mathf.Min(X, behindX); startDelay = 2; }
    public void Rebase(Vector2 shift) { X -= shift.x; transform.position -= (Vector3)shift; }
    private void OnDestroy() { if (frames != null) foreach (Sprite frame in frames) Destroy(frame); }
}
