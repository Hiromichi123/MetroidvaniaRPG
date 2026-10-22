using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class RunnerLiveReload
{
    private const string Key = "Runner.ReloadAfterScriptChange";
    static RunnerLiveReload()
    {
        AssemblyReloadEvents.beforeAssemblyReload += BeforeReload;
        Player existing = Application.isPlaying ? Object.FindObjectOfType<Player>() : null;
        bool staleRun = EditorApplication.isPlaying && SceneManager.GetActiveScene().name == "RunnerScene" &&
            existing != null && existing.stateMachine == null;
        if (!SessionState.GetBool(Key, false) && !staleRun) return;
        SessionState.SetBool(Key, false);
        EditorApplication.delayCall += RestoreRun;
    }
    private static void RestoreRun()
    {
        if (!EditorApplication.isPlaying || SceneManager.GetActiveScene().name != "RunnerScene") return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        { EditorApplication.delayCall += RestoreRun; return; }
        // Runtime FSMs and generator caches are not serializable across a
        // code reload. Wait for all prefab imports, then load a coherent run.
        SceneManager.LoadScene("RunnerScene");
        Debug.Log("[Runner] Restarted the run after script changes to restore runtime state and asset references.");
    }
    private static void BeforeReload()
    {
        SessionState.SetBool(Key, EditorApplication.isPlaying && Application.isPlaying &&
            SceneManager.GetActiveScene().name == "RunnerScene");
    }
}
