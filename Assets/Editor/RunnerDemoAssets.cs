using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class RunnerDemoAssets
{
    [MenuItem("Tools/Runner/Refresh authored demo modules")]
    public static void Export()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory("Assets/Resources/Runner/GroundModules");
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        Tilemap source = Object.FindObjectsOfType<Tilemap>().First(map => map.name == "Ground");
        ExportBlock(source, new RectInt(-8, -5, 4, 2), "DemoShortBlock", 1);
        ExportBlock(source, new RectInt(-4, -5, 10, 3), "DemoWideBlock", 4);

        GameObject sourceCity = GameObject.Find("BG_City_Layer");
        GameObject backdrop = Object.Instantiate(sourceCity.transform.parent.gameObject);
        backdrop.name = "DemoBackdrop";
        foreach (ParallaxBackground old in backdrop.GetComponentsInChildren<ParallaxBackground>(true)) Object.DestroyImmediate(old);
        RunnerBackdrop component = backdrop.AddComponent<RunnerBackdrop>();
        component.Sky = backdrop.GetComponentsInChildren<SpriteRenderer>().First(renderer => renderer.name == "BG_Sky_Layer");
        component.City = backdrop.GetComponentsInChildren<Transform>().First(child => child.name == "BG_City_Layer");
        PrefabUtility.SaveAsPrefabAsset(backdrop, "Assets/Resources/Runner/DemoBackdrop.prefab");
        Object.DestroyImmediate(backdrop);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var scene = EditorSceneManager.OpenScene("Assets/Scenes/RunnerScene.unity");
        RunnerGame game = Object.FindObjectOfType<RunnerGame>();
        game.BackgroundPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Runner/DemoBackdrop.prefab").GetComponent<RunnerBackdrop>();
        game.Generator.ShortModule = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Runner/GroundModules/DemoShortBlock.prefab").GetComponent<RunnerGroundModule>();
        game.Generator.WideModule = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Runner/GroundModules/DemoWideBlock.prefab").GetComponent<RunnerGroundModule>();
        foreach (RunnerBackdrop old in Object.FindObjectsOfType<RunnerBackdrop>())
            if (old.gameObject.scene == scene) Object.DestroyImmediate(old.gameObject);
        GameObject preview = (GameObject)PrefabUtility.InstantiatePrefab(game.BackgroundPrefab.gameObject, scene);
        preview.name = "Demo Background";
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("AUTHORED DEMO BACKDROP AND GROUND BLOCKS EXPORTED");
    }

    private static void ExportBlock(Tilemap source, RectInt area, string name, int edgeColumns)
    {
        GameObject root = new GameObject(name);
        Grid grid = root.AddComponent<Grid>();
        Grid sourceGrid = source.GetComponentInParent<Grid>();
        grid.cellSize = sourceGrid.cellSize;
        grid.cellGap = sourceGrid.cellGap;
        GameObject layer = new GameObject("Authored Ground");
        layer.transform.SetParent(root.transform, false);
        Tilemap map = layer.AddComponent<Tilemap>();
        map.tileAnchor = source.tileAnchor;
        TilemapRenderer renderer = layer.AddComponent<TilemapRenderer>();
        TilemapRenderer original = source.GetComponent<TilemapRenderer>();
        renderer.sortingLayerID = original.sortingLayerID;
        renderer.sortingOrder = original.sortingOrder;
        renderer.sharedMaterial = original.sharedMaterial;
        for (int y = area.yMin; y < area.yMax; y++)
            for (int x = area.xMin; x < area.xMax; x++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                TileBase tile = source.GetTile(cell);
                if (tile == null) continue;
                Vector3Int destination = new Vector3Int(x - area.xMin, y - area.yMax, 0);
                map.SetTile(destination, tile);
                map.SetTileFlags(destination, TileFlags.None);
                map.SetColor(destination, source.GetColor(cell));
                map.SetTransformMatrix(destination, source.GetTransformMatrix(cell));
            }
        RunnerGroundModule module = root.AddComponent<RunnerGroundModule>();
        module.Tiles = map;
        module.NativeWidth = area.width;
        module.Height = area.height;
        module.PreserveLeft = module.PreserveRight = edgeColumns;
        module.SourceCells = area;
        PrefabUtility.SaveAsPrefabAsset(root, "Assets/Resources/Runner/GroundModules/" + name + ".prefab");
        Object.DestroyImmediate(root);
    }
}
