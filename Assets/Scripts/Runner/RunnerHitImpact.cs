using UnityEngine;

public class RunnerHitImpact : MonoBehaviour
{
    private LineRenderer[] rays;
    private Material material;
    private float age;
    public static void Spawn(Vector2 position, int direction)
    {
        GameObject root = new GameObject("Hit sparks");
        root.transform.position = position;
        RunnerHitImpact flash = root.AddComponent<RunnerHitImpact>();
        flash.material = new Material(Shader.Find("Sprites/Default"));
        flash.rays = new LineRenderer[6];
        for (int i = 0; i < flash.rays.Length; i++)
        {
            GameObject ray = new GameObject("Spark"); ray.transform.SetParent(root.transform, false);
            LineRenderer line = ray.AddComponent<LineRenderer>();
            line.sharedMaterial = flash.material;
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.sortingLayerName = "Player"; line.sortingOrder = 20;
            line.startWidth = .065f; line.endWidth = .015f;
            float angle = (i * 60 + (direction > 0 ? 0 : 30)) * Mathf.Deg2Rad;
            ray.transform.localRotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
            flash.rays[i] = line;
        }
    }
    private void Update()
    {
        if (RunnerGame.Instance != null && RunnerGame.Instance.IsPaused) return;
        age += Time.deltaTime;
        float t = age / .18f;
        if (t >= 1) { Destroy(gameObject); return; }
        foreach (LineRenderer line in rays)
        {
            line.SetPosition(0, Vector3.right * (.08f + t * .45f));
            line.SetPosition(1, Vector3.right * (.35f + t * .65f));
            line.startColor = line.endColor = new Color(1, .85f, .45f, 1 - t);
        }
    }
    private void OnDestroy() { if (material != null) Destroy(material); }
}
