using UnityEngine;

public class RunnerHUD : MonoBehaviour
{
    private RunnerGame game;
    private GUIStyle title, label, small, button, centered;
    private Font font;
    private readonly Color teal = new Color(.35f, .95f, .8f);
    private readonly Color gold = new Color(1, .8f, .35f);
    private Enemy[] enemies = new Enemy[0];
    private float refresh;
    private void Awake() { game = GetComponent<RunnerGame>(); }
    private void Update()
    {
        if (Time.unscaledTime < refresh) return;
        refresh = Time.unscaledTime + .4f;
        enemies = FindObjectsOfType<Enemy>();
    }
    private void InitializeStyles()
    {
        font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "Arial" }, 24);
        label = new GUIStyle(GUI.skin.label) { font = font, fontSize = 20, normal = { textColor = Color.white } };
        title = new GUIStyle(label) { fontSize = 38, fontStyle = FontStyle.Bold };
        small = new GUIStyle(label) { fontSize = 15, normal = { textColor = new Color(.7f, .78f, .85f) }, wordWrap = true };
        centered = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        button = new GUIStyle(GUI.skin.button) { font = font, fontSize = 19 };
    }
    private void Panel(Rect rect, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
    private void Bar(Rect rect, float fraction, Color color)
    {
        Panel(rect, new Color(.13f, .18f, .23f, .95f));
        Panel(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fraction), rect.height), color);
    }
    private void OnGUI()
    {
        if (game == null || game.Player == null) return;
        if (label == null) InitializeStyles();
        Matrix4x4 original = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 1280f, Screen.height / 720f, 1));
        Panel(new Rect(22, 22, 278, 122), new Color(.035f, .06f, .1f, .9f));
        GUI.Label(new Rect(40, 30, 260, 48), $"{game.Distance:0000} m", title);
        GUI.Label(new Rect(42, 80, 250, 27), $"第 {game.Stage} 阶段     金币 {game.Coins}     击杀 {game.Kills}", small);
        Bar(new Rect(42, 116, 246, 8), (float)game.Player.CurrentHealth / game.Player.MaxHealth, new Color(.95f, .3f, .36f));
        GUI.Label(new Rect(42, 145, 300, 25), $"生命 {game.Player.CurrentHealth}/{game.Player.MaxHealth}   分数 {game.Score}", small);

        Panel(new Rect(980, 22, 278, 112), new Color(.035f, .06f, .1f, .9f));
        GUI.color = teal;
        GUI.Label(new Rect(1000, 31, 245, 35), $"FLOW {game.Flow.Tier}   ×{game.Flow.Multiplier:0.00}", label);
        GUI.color = Color.white;
        float duration = game.Flow.Tier > 0 ? RunnerFlow.Durations[game.Flow.Tier] + game.Flow.DurationBonus : 1;
        Bar(new Rect(1000, 76, 235, 7), game.Flow.Remaining / duration, teal);
        GUI.Label(new Rect(1000, 91, 240, 27), "跳到新平台 / 击杀 → 叠加速度", small);

        float gap = game.Shadow.Distance;
        bool danger = gap < 14;
        Panel(new Rect(427, 102, 426, 49), new Color(.035f, .06f, .1f, .85f));
        GUI.color = danger ? new Color(1, .4f, .48f) : new Color(.8f, .72f, 1);
        string pressure = danger ? "快跑！" : game.Shadow.CatchupBonus > 1 ? "恶魔正在加速追赶" : "保持节奏";
        GUI.Label(new Rect(438, 109, 405, 35), game.InBossArena ? "区域终点 · 追逐已暂停" : game.IsShopping ? "安全区 · 影子追逐已暂停" : $"影子距离 {Mathf.Max(0, gap):0}m   {pressure}", centered);
        GUI.color = Color.white;
        if (danger && !game.IsPaused && !game.InBossArena) Panel(new Rect(0, 0, 9, 720), new Color(.8f, .15f, .3f, .4f + .2f * Mathf.Sin(Time.time * 10)));
        GUI.Label(new Rect(28, 674, 1100, 28), "A/D 移动    Space 跳跃    Shift 冲刺    鼠标左键 挥剑 / 反弹法球    R 重开", small);
        GUI.Label(new Rect(1050, 674, 220, 28), $"SEED {game.Generator.Seed}", new GUIStyle(small) { alignment = TextAnchor.MiddleRight, fontSize = 12 });

        if (!game.IsPaused)
        {
            foreach (Enemy enemy in enemies)
            {
                if (enemy == null || !enemy.ShowHealthBar || Camera.main == null) continue;
                Bounds bounds = enemy.GetComponent<Collider2D>().bounds;
                Vector3 point = Camera.main.WorldToViewportPoint(new Vector3(bounds.center.x, bounds.max.y + .4f, 0));
                if (point.z < 0 || point.x < 0 || point.x > 1) continue;
                Bar(new Rect(point.x * 1280 - 25, (1 - point.y) * 720, 50, 5), (float)enemy.CurrentHealth / enemy.MaxHealth, new Color(1, .4f, .4f));
            }
        }
        if (game.InBossArena && !game.IsGameOver)
        {
            Panel(new Rect(300, 560, 680, 80), new Color(.035f, .06f, .1f, .92f));
            GUI.Label(new Rect(320, 570, 640, 30), "Boss 场地 · 自由移动预览", centered);
            GUI.Label(new Rect(320, 606, 640, 24), game.AtArenaExit ? "按 E 穿过出口，进入下一区域" : "Boss 对战尚未制作 · 前往右侧出口可进入下一区域", new GUIStyle(small) { alignment = TextAnchor.MiddleCenter });
        }
        if (game.IsShopping) Shop();
        if (game.IsGameOver)
        {
            Panel(new Rect(0, 0, 1280, 720), new Color(.02f, .025f, .06f, .85f));
            GUI.Label(new Rect(380, 205, 600, 60), "逃生结束", title);
            GUI.Label(new Rect(382, 280, 600, 40), game.EndReason, label);
            GUI.Label(new Rect(382, 330, 600, 40), $"{game.Distance:0}m    分数 {game.Score}    击杀 {game.Kills}", label);
            if (GUI.Button(new Rect(382, 407, 340, 56), "重新出发  [R]", button)) game.Restart();
        }
        GUI.matrix = original;
    }
    private void Shop()
    {
        Panel(new Rect(0, 0, 1280, 720), new Color(.02f, .035f, .065f, .88f));
        GUI.color = gold;
        GUI.Label(new Rect(110, 115, 700, 65), "喘息之地", title);
        GUI.color = Color.white;
        GUI.Label(new Rect(113, 182, 1000, 35), $"影子与加速计时暂停。金币 {game.Coins} · 本次购买的能力持续到本局结束。", label);
        for (int slot = 0; slot < 3; slot++)
        {
            int item = game.Offers[slot];
            float x = 112 + slot * 358;
            Panel(new Rect(x, 247, 336, 226), new Color(.09f, .13f, .19f, 1));
            Panel(new Rect(x, 247, 336, 3), teal);
            GUI.Label(new Rect(x + 20, 269, 300, 37), $"[{slot + 1}]  {RunnerGame.ItemNames[item]}", label);
            GUI.Label(new Rect(x + 20, 323, 295, 65), RunnerGame.ItemDescriptions[item], small);
            bool enabled = GUI.enabled;
            GUI.enabled = !game.Purchased[slot] && game.Coins >= game.Price(slot) && !(item == 3 && game.Player.CurrentHealth >= game.Player.MaxHealth);
            if (GUI.Button(new Rect(x + 20, 407, 295, 43), game.Purchased[slot] ? "已购买" : $"购买 · {game.Price(slot)} 金币", button)) game.Buy(slot);
            GUI.enabled = enabled;
        }
        if (GUI.Button(new Rect(112, 505, 270, 49), $"刷新商品 · {RunnerGame.RerollCost} 金币（一次）", button)) game.Reroll();
        if (GUI.Button(new Rect(782, 505, 382, 49), "继续逃生  [Enter]", button)) game.LeaveSanctuary();
    }
    private void OnDestroy() { if (font != null) Destroy(font); }
}
