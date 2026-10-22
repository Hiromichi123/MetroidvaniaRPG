using UnityEditor;
using UnityEngine;

public static class RunnerRegionAssets
{
    [MenuItem("Tools/Runner/Build region assets")]
    public static void Build()
    {
        System.IO.Directory.CreateDirectory("Assets/Resources/Runner/Regions");
        AssetDatabase.Refresh();
        GameObject root = new GameObject("ForestBackdrop");
        RunnerBackdrop backdrop = root.AddComponent<RunnerBackdrop>();
        GameObject sky = new GameObject("Forest sky"); sky.transform.SetParent(root.transform, false);
        backdrop.Sky = sky.AddComponent<SpriteRenderer>();
        backdrop.Sky.sprite = SpriteAt("Assets/Graphics/Surroundings/Forest_Bg/background_layer_1.png");
        GameObject trees = new GameObject("Forest middle"); trees.transform.SetParent(root.transform, false);
        Sprite middle = SpriteAt("Assets/Graphics/Surroundings/Forest_Bg/background_layer_2.png");
        Sprite front = SpriteAt("Assets/Graphics/Surroundings/Forest_Bg/background_layer_3.png");
        trees.AddComponent<SpriteRenderer>().sprite = middle;
        backdrop.City = trees.transform; trees.transform.localScale = Vector3.one * 1.4f;
        float width = middle.bounds.size.x;
        for (int i = -1; i <= 1; i++)
        {
            if (i != 0) Layer(trees.transform, "Forest middle repeat", middle, i * width, 0);
            Layer(trees.transform, "Forest Foreground", front, i * width, -.01f);
        }
        const string forestPath = "Assets/Resources/Runner/Regions/ForestBackdrop.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, forestPath); Object.DestroyImmediate(root);
        const string catalogPath = "Assets/Resources/Runner/Regions/RegionCatalog.asset";
        RunnerRegionCatalog catalog = AssetDatabase.LoadAssetAtPath<RunnerRegionCatalog>(catalogPath);
        if (catalog == null) { catalog = ScriptableObject.CreateInstance<RunnerRegionCatalog>(); AssetDatabase.CreateAsset(catalog, catalogPath); }
        Enemy skeleton = AssetDatabase.LoadAssetAtPath<Enemy_Skeleton>("Assets/Runner/Prefabs/RunnerSkeleton.prefab");
        catalog.Regions = new[] {
            new RunnerRegionDefinition { Name = "旧城", Backdrop = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Runner/DemoBackdrop.prefab").GetComponent<RunnerBackdrop>(),
                Enemies = new[] { EnemyAt("RunnerSlime"), skeleton, EnemyAt("RunnerNecromancer"), EnemyAt("RunnerReaver") } },
            new RunnerRegionDefinition { Name = "暮色森林", Backdrop = AssetDatabase.LoadAssetAtPath<GameObject>(forestPath).GetComponent<RunnerBackdrop>(),
                Enemies = new[] { EnemyAt("RunnerSlime"), EnemyAt("RunnerReaver") }, ForestLayout = true,
                GroundTint = new Color(.78f, .94f, .8f), Decoration = SpriteAt("Assets/Graphics/Surroundings/Valley_Decor/grass_1.png") }
        };
        EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
        Debug.Log("REGION ASSETS BUILT");
        if (Application.isBatchMode) EditorApplication.Exit(0);
    }
    private static void Layer(Transform parent, string name, Sprite sprite, float x, float z)
    {
        GameObject layer = new GameObject(name); layer.transform.SetParent(parent, false);
        layer.transform.localPosition = new Vector3(x, 0, z); layer.AddComponent<SpriteRenderer>().sprite = sprite;
    }
    private static Sprite SpriteAt(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);
    private static Enemy EnemyAt(string name) => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Runner/Enemies/" + name + ".prefab").GetComponent<Enemy>();
}
