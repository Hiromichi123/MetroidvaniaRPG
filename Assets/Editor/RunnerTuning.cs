using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RunnerTuning
{
    // Patch only the relevant serialized settings; retain authored scene and prefab edits.
    public static void Apply()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/RunnerScene.unity");
        RunnerGame game = Object.FindObjectOfType<RunnerGame>();
        game.Generator.Ahead = 130;
        game.Generator.SanctuarySpacing = 500;
        game.Generator.SurfaceSprite = SpriteAt("Assets/Graphics/Surroundings/Medieval_Castle/Tiles/floor_tile_2.png");
        game.Generator.BodySprite = SpriteAt("Assets/Graphics/Surroundings/Medieval_Castle/Tiles/floor_tile_3.png");
        game.BackgroundSprite = SpriteAt("Assets/Graphics/Surroundings/Medieval_Castle/Background/layer_1.png");
        game.CityBackgroundSprite = SpriteAt("Assets/Graphics/Surroundings/Medieval_Castle/Background/layer_2.png");
        Camera.main.orthographicSize = RunnerCamera.DefaultSize;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("RUNNER TUNING APPLIED");
    }
    private static Sprite SpriteAt(string path)
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path)) if (asset is Sprite sprite) return sprite;
        return null;
    }
}
