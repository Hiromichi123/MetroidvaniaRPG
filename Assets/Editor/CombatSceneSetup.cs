using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Apply the single-player migration in the editor's loaded scene rather than
// replacing its YAML on disk, preserving any unsaved level edits.
[InitializeOnLoad]
public static class CombatSceneSetup
{
    static CombatSceneSetup()
    {
        EditorApplication.delayCall += ApplyToLoadedScenes;
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode) { Apply(scene); }

    private static void ApplyToLoadedScenes()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++) Apply(SceneManager.GetSceneAt(i));
    }

    private static void Apply(Scene scene)
    {
        if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode || !scene.isLoaded ||
            scene.path != "Assets/Scenes/SampleScene.unity") return;
        Player primary = null;
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Player candidate in root.GetComponentsInChildren<Player>(true))
                if (candidate.name == "Player") primary = candidate;
        if (primary == null) return;
        bool wasDirty = scene.isDirty;
        bool changed = false;
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Player candidate in root.GetComponentsInChildren<Player>(true))
                if (candidate != primary && candidate.name == "fox")
                {
                    Undo.DestroyObjectImmediate(candidate.gameObject);
                    changed = true;
                }
        if (!changed) return;
        EditorSceneManager.MarkSceneDirty(scene);
        if (!wasDirty) EditorSceneManager.SaveScene(scene);
        Debug.Log("[Combat] Removed the obsolete fox player. Existing scene edits were preserved.");
    }
}
