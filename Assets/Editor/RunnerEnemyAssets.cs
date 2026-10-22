using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RunnerEnemyAssets
{
    private const string Art = "Assets/Runner/Enemies";
    private const string Prefabs = "Assets/Resources/Runner/Enemies";
    private struct State
    {
        public string Name; public int Row, Count;
        public State(string name, int row, int count) { Name = name; Row = row; Count = count; }
    }

    [MenuItem("Tools/Runner/Extract enemy assets")]
    public static void Build()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(Art + "/Textures");
        Directory.CreateDirectory(Art + "/Animations");
        Directory.CreateDirectory(Prefabs);
        AssetDatabase.Refresh();
        Sprite[] energy = ImportSheet("Assets/Graphics/FX/Arcane/Arcane_Ball/EnergyBall.png", "ArcaneBolt", 128, 128, 1, 64,
            new Vector2(.65f, .5f), new[] { new State("Fly", 0, 9) })["Fly"];
        GameObject bolt = new GameObject("RunnerArcaneBolt");
        SpriteRenderer visual = bolt.AddComponent<SpriteRenderer>();
        visual.sprite = energy[0]; visual.sortingLayerName = "Player"; visual.sortingOrder = 6;
        RunnerProjectile shot = bolt.AddComponent<RunnerProjectile>(); shot.Frames = energy;
        PrefabUtility.SaveAsPrefabAsset(bolt, Prefabs + "/RunnerArcaneBolt.prefab");
        Object.DestroyImmediate(bolt);
        RunnerProjectile projectile = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/RunnerArcaneBolt.prefab").GetComponent<RunnerProjectile>();

        CreateEnemy("RunnerSlime", RunnerEnemyKind.Slime, "Assets/Graphics/Enemies/Slime/slime-Sheet.png",
            32, 25, 3, 20, new Vector2(.5f, .04f), new Vector2(1.15f, .65f), 1.1f,
            new[] { new State("Idle", 0, 8), new State("Move", 0, 8), new State("Attack", 1, 8), new State("Hit", 1, 2), new State("Dead", 2, 6) }, projectile);
        CreateEnemy("RunnerReaver", RunnerEnemyKind.Reaver, "Assets/Graphics/Enemies/Night born/NightBorne.png",
            80, 80, 5, 16, new Vector2(.5f, .2f), new Vector2(.95f, 1.7f), 1.2f,
            new[] { new State("Idle", 0, 9), new State("Move", 1, 6), new State("Attack", 2, 12), new State("Hit", 3, 5), new State("Dead", 4, 23) }, projectile);
        CreateEnemy("RunnerNecromancer", RunnerEnemyKind.Necromancer, "Assets/Graphics/Enemies/Necro/Necromancer_creativekind-Sheet.png",
            160, 128, 7, 24, new Vector2(.5f, 13f / 128), new Vector2(.95f, 1.95f), 1.1f,
            new[] { new State("Idle", 0, 8), new State("Move", 1, 8), new State("Attack", 2, 13), new State("Hit", 5, 5), new State("Dead", 6, 10) }, projectile);
        AssetDatabase.SaveAssets();
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/RunnerScene.unity");
        RunnerGenerator generator = Object.FindObjectOfType<RunnerGenerator>();
        generator.EnemyVariants = new[] {
            EnemyAt("RunnerSlime"), generator.EnemyPrefab, EnemyAt("RunnerNecromancer"), EnemyAt("RunnerReaver")
        };
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("THREE ENEMY SPECIES EXTRACTED AND REGISTERED");
    }

    private static Enemy EnemyAt(string name) => AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/" + name + ".prefab").GetComponent<Enemy>();

    private static Dictionary<string, Sprite[]> ImportSheet(string source, string name, int width, int height, int rows,
        float pixelsPerUnit, Vector2 pivot, State[] states)
    {
        string path = Art + "/Textures/" + name + ".png";
        File.Copy(source, path, true);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.maxTextureSize = 4096;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        var slices = new List<SpriteMetaData>();
        foreach (State state in states)
            for (int i = 0; i < state.Count; i++) slices.Add(new SpriteMetaData {
                name = name + "_" + state.Name + "_" + i.ToString("00"),
                rect = new Rect(i * width, (rows - state.Row - 1) * height, width, height), alignment = 9, pivot = pivot
            });
        importer.spritesheet = slices.ToArray();
        importer.SaveAndReimport();
        Dictionary<string, Sprite> sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(sprite => sprite.name);
        var result = new Dictionary<string, Sprite[]>();
        foreach (State state in states)
            result[state.Name] = Enumerable.Range(0, state.Count).Select(i => sprites[name + "_" + state.Name + "_" + i.ToString("00")]).ToArray();
        return result;
    }

    private static void CreateEnemy(string name, RunnerEnemyKind kind, string source, int width, int height, int rows,
        float ppu, Vector2 pivot, Vector2 bodySize, float scale, State[] states, RunnerProjectile projectile)
    {
        Dictionary<string, Sprite[]> sprites = ImportSheet(source, name, width, height, rows, ppu, pivot, states);
        string folder = Art + "/Animations/" + name;
        Directory.CreateDirectory(folder);
        AssetDatabase.Refresh();
        string controllerPath = folder + "/" + name + ".controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        foreach (State state in states)
        {
            string clipPath = folder + "/" + state.Name + ".anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, clipPath); }
            clip.frameRate = 12;
            bool loop = state.Name == "Idle" || state.Name == "Move";
            Sprite[] frames = sprites[state.Name];
            var keys = new ObjectReferenceKeyframe[frames.Length + 1];
            for (int i = 0; i < frames.Length; i++) keys[i] = new ObjectReferenceKeyframe { time = i / 12f, value = frames[i] };
            keys[frames.Length] = new ObjectReferenceKeyframe { time = frames.Length / 12f, value = loop ? frames[0] : frames[frames.Length - 1] };
            AnimationUtility.SetObjectReferenceCurve(clip, new EditorCurveBinding { path = "", type = typeof(SpriteRenderer), propertyName = "m_Sprite" }, keys);
            var clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
            clipSettings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, clipSettings);
            AnimatorState node = machine.states.Select(child => child.state).FirstOrDefault(candidate => candidate.name == state.Name);
            if (node == null) node = machine.AddState(state.Name);
            node.motion = clip;
            if (state.Name == "Idle") machine.defaultState = node;
            EditorUtility.SetDirty(clip);
        }
        GameObject root = new GameObject(name);
        root.layer = LayerMask.NameToLayer("Enemy");
        root.transform.localScale = Vector3.one * scale;
        Rigidbody2D rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3.5f;
        rb.mass = kind == RunnerEnemyKind.Reaver ? 3 : kind == RunnerEnemyKind.Slime ? .6f : 1.2f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        CapsuleCollider2D collider = root.AddComponent<CapsuleCollider2D>(); collider.size = bodySize;
        GameObject actor = new GameObject("Animator"); actor.transform.SetParent(root.transform, false);
        actor.transform.localPosition = new Vector3(0, -bodySize.y * .5f, 0);
        SpriteRenderer visual = actor.AddComponent<SpriteRenderer>();
        visual.sprite = sprites["Idle"][0]; visual.sortingLayerName = "Enemy"; visual.flipX = true;
        actor.AddComponent<Animator>().runtimeAnimatorController = controller;
        GameObject ground = new GameObject("GroundCheck"); ground.transform.SetParent(root.transform, false);
        ground.transform.localPosition = new Vector3(0, -bodySize.y * .5f - .02f, 0);
        GameObject wall = new GameObject("WallCheck"); wall.transform.SetParent(root.transform, false); wall.transform.localPosition = new Vector3(.6f, 0, 0);
        RunnerEnemy enemy = root.AddComponent<RunnerEnemy>();
        enemy.Kind = kind; enemy.ProjectilePrefab = projectile;
        enemy.moveSpeed = kind == RunnerEnemyKind.Reaver ? 2.2f : kind == RunnerEnemyKind.Necromancer ? 1.3f : 0;
        enemy.detectionRange = kind == RunnerEnemyKind.Necromancer ? 15 : 10;
        enemy.AttackRange = kind == RunnerEnemyKind.Necromancer ? 12 : kind == RunnerEnemyKind.Reaver ? 2.8f : 3.5f;
        enemy.Windup = kind == RunnerEnemyKind.Slime ? .28f : .75f;
        enemy.ActiveTime = kind == RunnerEnemyKind.Slime ? .65f : .3f;
        enemy.Cooldown = kind == RunnerEnemyKind.Necromancer ? 1.3f : 1f;
        enemy.Damage = kind == RunnerEnemyKind.Slime ? 10 : kind == RunnerEnemyKind.Reaver ? 26 : 14;
        enemy.CoinReward = kind == RunnerEnemyKind.Slime ? 6 : kind == RunnerEnemyKind.Reaver ? 16 : 12;
        enemy.ScoreReward = kind == RunnerEnemyKind.Slime ? 70 : kind == RunnerEnemyKind.Reaver ? 180 : 130;
        SerializedObject data = new SerializedObject(enemy);
        data.FindProperty("maxHealth").intValue = kind == RunnerEnemyKind.Slime ? 90 : kind == RunnerEnemyKind.Reaver ? 240 : 120;
        data.FindProperty("groundCheck").objectReferenceValue = ground.transform;
        data.FindProperty("groundCheckDistance").floatValue = .18f;
        data.FindProperty("wallCheck").objectReferenceValue = wall.transform;
        data.FindProperty("wallCheckDistance").floatValue = .5f;
        data.FindProperty("whatIsGround").intValue = LayerMask.GetMask("Ground", "Wall");
        data.FindProperty("whatIsWall").intValue = LayerMask.GetMask("Ground", "Wall");
        data.FindProperty("whatIsPlayer").intValue = LayerMask.GetMask("Player");
        data.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(root, Prefabs + "/" + name + ".prefab");
        Object.DestroyImmediate(root);
    }
}
