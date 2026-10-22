using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RunnerSceneBuilder
{
    [MenuItem("Tools/Runner/Build runner scene")]
    public static void Build()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory("Assets/Runner/Prefabs");
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        Player sourcePlayer = Object.FindObjectOfType<Player>();
        Enemy_Skeleton sourceEnemy = Object.FindObjectOfType<Enemy_Skeleton>();
        if (sourcePlayer == null || sourceEnemy == null) throw new System.Exception("SampleScene must contain the player and skeleton templates.");
        GameObject player = Object.Instantiate(sourcePlayer.gameObject);
        player.name = "RunnerPlayer";
        player.transform.position = Vector3.zero;
        player.transform.rotation = Quaternion.identity;
        CombatHUD oldHud = player.GetComponent<CombatHUD>();
        if (oldHud != null) Object.DestroyImmediate(oldHud);
        PrefabUtility.SaveAsPrefabAsset(player, "Assets/Runner/Prefabs/RunnerPlayer.prefab");
        Object.DestroyImmediate(player);
        GameObject skeleton = Object.Instantiate(sourceEnemy.gameObject);
        skeleton.name = "RunnerSkeleton";
        skeleton.transform.position = Vector3.zero;
        skeleton.transform.rotation = Quaternion.identity;
        SerializedObject settings = new SerializedObject(skeleton.GetComponent<Enemy_Skeleton>());
        settings.FindProperty("maxHealth").intValue = 140;
        settings.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(skeleton, "Assets/Runner/Prefabs/RunnerSkeleton.prefab");
        Object.DestroyImmediate(skeleton);
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject camera = new GameObject("Main Camera");
        camera.tag = "MainCamera";
        Camera view = camera.AddComponent<Camera>();
        view.orthographic = true;
        view.orthographicSize = RunnerCamera.DefaultSize;
        view.clearFlags = CameraClearFlags.SolidColor;
        camera.AddComponent<AudioListener>();
        camera.transform.position = new Vector3(5, 3, -10);
        GameObject root = new GameObject("Runner Game");
        RunnerGame game = root.AddComponent<RunnerGame>();
        GameObject world = new GameObject("Generated world");
        game.Generator = world.AddComponent<RunnerGenerator>();
        game.PlayerPrefab = AssetDatabase.LoadAssetAtPath<Player>("Assets/Runner/Prefabs/RunnerPlayer.prefab");
        game.Generator.EnemyPrefab = AssetDatabase.LoadAssetAtPath<Enemy_Skeleton>("Assets/Runner/Prefabs/RunnerSkeleton.prefab");
        game.Generator.SurfaceSprite = SpriteAt("Assets/Graphics/Surroundings/Medieval_Castle/Tiles/floor_tile_2.png");
        game.Generator.BodySprite = SpriteAt("Assets/Graphics/Surroundings/Medieval_Castle/Tiles/floor_tile_3.png");
        game.Generator.LampSprite = SpriteAt("Assets/Graphics/Surroundings/Valley_Decor/lamp.png");
        game.BackgroundSprite = SpriteAt("Assets/Graphics/Surroundings/Medieval_Castle/Background/layer_1.png");
        game.CityBackgroundSprite = SpriteAt("Assets/Graphics/Surroundings/Medieval_Castle/Background/layer_2.png");
        game.ShadowSheet = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Graphics/Enemies/Briner of Death/Bringer-of-Death-SpritSheet_no-Effect.png");
        if (game.Generator.BodySprite == null || game.ShadowSheet == null) throw new System.Exception("Runner art assets missing.");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/RunnerScene.unity");
        EditorBuildSettings.scenes = new[] {
            new EditorBuildSettingsScene("Assets/Scenes/RunnerScene.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity", true)
        };
        AssetDatabase.SaveAssets();
        RunnerDemoAssets.Export();
        Debug.Log("RUNNER SCENE BUILT");
    }
    private static Sprite SpriteAt(string path)
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path)) if (asset is Sprite sprite) return sprite;
        return null;
    }
}
