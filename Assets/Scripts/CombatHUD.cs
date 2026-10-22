using UnityEngine;
using UnityEngine.SceneManagement;

public class CombatHUD : MonoBehaviour
{
    private Player player;
    private Enemy[] enemies;
    private GUIStyle text;
    private bool restarting;

    private void Start()
    {
        player = FindObjectOfType<Player>();
        enemies = FindObjectsOfType<Enemy>();
    }

    private void Update()
    {
        if (!restarting && Input.GetKeyDown(KeyCode.R))
        {
            Restart();
        }
    }

    public void Restart()
    {
        if (restarting) return;
        restarting = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnGUI()
    {
        if (player == null || enemies == null) return;
        if (text == null) text = new GUIStyle(GUI.skin.label) { fontSize = 20, normal = { textColor = Color.white } };
        GUI.Box(new Rect(12, 12, 460, 112), GUIContent.none);
        GUI.Label(new Rect(24, 20, 440, 30), $"HP  {player.CurrentHealth} / {player.MaxHealth}", text);
        int alive = 0;
        foreach (Enemy enemy in enemies) if (enemy != null && !enemy.IsDead) alive++;
        GUI.Label(new Rect(24, 50, 440, 30), $"Enemies  {alive} / {enemies.Length}", text);
        GUI.Label(new Rect(24, 80, 440, 30), "A/D: Move  Space: Jump  Shift: Dash  LMB: Attack", new GUIStyle(text) { fontSize = 15 });
        if (player.IsDead || (enemies.Length > 0 && alive == 0))
        {
            Rect box = new Rect(Screen.width / 2f - 180, Screen.height / 2f - 55, 360, 110);
            GUI.Box(box, GUIContent.none);
            GUI.Label(new Rect(box.x + 25, box.y + 15, 320, 35), player.IsDead ? "DEFEATED" : "VICTORY", text);
            GUI.Label(new Rect(box.x + 25, box.y + 55, 320, 35), "Press R to restart", text);
        }
        foreach (Enemy enemy in enemies)
        {
            if (enemy == null || !enemy.ShowHealthBar || Camera.main == null) continue;
            Vector3 point = Camera.main.WorldToScreenPoint(enemy.transform.position + Vector3.up * 1.8f);
            if (point.z < 0) continue;
            Rect bar = new Rect(point.x - 35, Screen.height - point.y, 70, 8);
            GUI.color = Color.black;
            GUI.DrawTexture(bar, Texture2D.whiteTexture);
            GUI.color = Color.red;
            GUI.DrawTexture(new Rect(bar.x + 1, bar.y + 1, 68f * enemy.CurrentHealth / enemy.MaxHealth, 6), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
