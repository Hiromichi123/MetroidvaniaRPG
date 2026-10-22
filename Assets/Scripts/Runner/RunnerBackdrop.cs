using UnityEngine;

[DefaultExecutionOrder(100)]
public class RunnerBackdrop : MonoBehaviour
{
    public SpriteRenderer Sky;
    public Transform City;
    private Camera view;
    private float cityWidth;
    private Sprite skyMesh;

    public void Initialize(Camera camera)
    {
        view = camera;
        foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>())
        {
            // Keep the complete demo composition behind all gameplay renderers.
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = renderer == Sky ? -100 : renderer.name.Contains("Foreground") ? -80 : -90;
            renderer.color = Color.white;
        }
        SpriteRenderer center = City.GetComponent<SpriteRenderer>();
        cityWidth = center.sprite.bounds.size.x * Mathf.Abs(City.lossyScale.x);
        Sprite sprite = Sky.sprite;
        skyMesh = Sprite.Create(sprite.texture, sprite.rect, new Vector2(.5f, .5f), sprite.pixelsPerUnit, 0, SpriteMeshType.FullRect);
        Sky.sprite = skyMesh;
        Sky.drawMode = SpriteDrawMode.Tiled;
        Sky.size = new Vector2(200, skyMesh.bounds.size.y);
        UpdatePosition();
    }

    private void LateUpdate()
    {
        if (view == null) view = Camera.main;
        if (view != null) UpdatePosition();
    }

    private void UpdatePosition()
    {
        Vector3 camera = view.transform.position;
        Sky.transform.position = new Vector3(camera.x, camera.y, 3);
        // Repeat horizontally only; repeating the full sky vertically shows a
        // second moon at the bottom when the enlarged camera crosses a seam.
        float skyScale = Mathf.Max(1, (view.orthographicSize * 2 + 2) / skyMesh.bounds.size.y);
        Sky.transform.localScale = new Vector3(1, skyScale, 1);
        // The original centre, left and right city sprites are kept together.
        City.position = new Vector3(camera.x - Mathf.Repeat(camera.x * .5f, cityWidth), camera.y - 2.7f, 2);
    }

    private void OnDestroy() { if (skyMesh != null) Destroy(skyMesh); }
}
