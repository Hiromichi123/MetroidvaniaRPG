using UnityEngine;

public class RunnerProgressHUD : MonoBehaviour
{
    public const float WindowRadius = 500;
    public static readonly Rect Frame = new Rect(320, 8, 640, 78);
    public static readonly Color PassedColor = new Color(.6f, .32f, .85f);
    public static readonly Color GapColor = new Color(1, .82f, .2f);
    public static readonly Color FutureColor = new Color(.76f, .79f, .82f);
    private RunnerGame game;
    private GUIStyle text, center, right;
    private Font font;
    public static float Normalize(float distance, float player) => Mathf.Clamp01(.5f + (distance - player) / (WindowRadius * 2));
    public static bool IsBossStop(int stop) => stop > 0 && stop % 3 == 0;
    private void Awake() { game = GetComponent<RunnerGame>(); }
    private void Paint(Rect rect, Color color)
    {
        GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = Color.white;
    }
    private void Diamond(float x, float y, float size, Color color)
    {
        Matrix4x4 matrix = GUI.matrix;
        GUIUtility.RotateAroundPivot(45, new Vector2(x, y));
        Paint(new Rect(x - size * .5f, y - size * .5f, size, size), color); GUI.matrix = matrix;
    }
    private void OnGUI()
    {
        if (game == null || game.Player == null) return;
        if (text == null)
        {
            font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Arial" }, 14);
            text = new GUIStyle(GUI.skin.label) { font = font, fontSize = 12, normal = { textColor = Color.white } };
            center = new GUIStyle(text) { alignment = TextAnchor.MiddleCenter };
            right = new GUIStyle(text) { alignment = TextAnchor.MiddleRight };
        }
        Matrix4x4 original = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 1280f, Screen.height / 720f, 1));
        Paint(Frame, new Color(.025f, .035f, .065f, .94f));
        Paint(new Rect(320, 8, 640, 1), PassedColor);
        float player = game.RoutePosition;
        float start = X(0, player), furthest = X(game.Distance, player);
        Paint(new Rect(332, 36, 616, 8), FutureColor);
        Paint(new Rect(start, 36, Mathf.Max(0, furthest - start), 8), PassedColor);
        float shadow = X(game.AbsoluteX(game.Shadow.X), player);
        Paint(new Rect(Mathf.Min(shadow, 640), 36, Mathf.Abs(640 - shadow), 8), GapColor);
        Diamond(shadow, 32, 12, PassedColor);
        Paint(new Rect(shadow - 4, 30, 2, 2), Color.white); Paint(new Rect(shadow + 2, 30, 2, 2), Color.white);
        Diamond(640, 39, 11, new Color(.4f, 1, .85f));
        Paint(new Rect(639, 27, 2, 21), Color.white);
        GUI.color = GapColor;
        GUI.Label(new Rect(332, 10, 160, 22), $"影距 {Mathf.Max(0, game.Shadow.Distance):0}m", text);
        GUI.color = Color.white;
        GUI.Label(new Rect(498, 10, 284, 22), $"区域 {game.RegionIndex + 1} · {game.RegionName}", center);
        GUI.Label(new Rect(580, 65, 120, 18), $"{player:0}m", center);
        GUI.Label(new Rect(332, 65, 90, 18), "← 500m", text);
        GUI.Label(new Rect(858, 65, 90, 18), "500m →", right);
        Marker(0, player, "起点", new Color(.8f, .8f, .9f), true);
        int first = Mathf.Max(1, Mathf.FloorToInt((player - WindowRadius) / game.Generator.SanctuarySpacing));
        int last = Mathf.CeilToInt((player + WindowRadius) / game.Generator.SanctuarySpacing);
        for (int i = first; i <= last; i++)
        {
            float distance = game.Generator.SanctuaryMarkers.TryGetValue(i, out float actual) ? actual : i * game.Generator.SanctuarySpacing;
            if (Mathf.Abs(distance - player) > WindowRadius) continue;
            Marker(distance, player, IsBossStop(i) ? $"休息 / BOSS {distance:0}m" : $"休息 {distance:0}m",
                IsBossStop(i) ? new Color(1, .46f, .28f) : new Color(.4f, .95f, .7f), false);
        }
        if (game.HistoricalBest > 0)
        {
            float best = X(game.HistoricalBest, player);
            Diamond(best, 39, 6, new Color(1, .85f, .45f));
            GUI.color = new Color(1, .85f, .45f);
            GUI.Label(new Rect(790, 10, 158, 22), $"最佳 {game.HistoricalBest:0}m", right);
            GUI.color = Color.white;
        }
        GUI.matrix = original;
    }
    private static float X(float distance, float player) => 332 + Normalize(distance, player) * 616;
    private void Marker(float distance, float player, string caption, Color color, bool clamp)
    {
        if (!clamp && Mathf.Abs(distance - player) > WindowRadius) return;
        if (distance <= game.Distance) color = PassedColor;
        float x = X(distance, player);
        Paint(new Rect(x - 1, 32, 2, 16), color);
        GUI.color = color;
        GUI.Label(new Rect(Mathf.Clamp(x - 75, 332, 798), 47, 150, 18), caption, center);
        GUI.color = Color.white;
    }
    private void OnDestroy() { if (font != null) Destroy(font); }
}
