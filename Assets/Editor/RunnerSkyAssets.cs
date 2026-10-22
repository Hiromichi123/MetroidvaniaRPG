using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class RunnerSkyAssets
{
    [MenuItem("Tools/Runner/Extract sky ghost assets")]
    public static void Build()
    {
        const string path = "Assets/Runner/Enemies/Textures/RunnerSkyGhost.png";
        File.Copy("Assets/Graphics/Enemies/Briner of Death/Bringer-of-Death-SpritSheet_no-Effect.png", path, true);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 24; importer.maxTextureSize = 2048;
        importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        int w = texture.width / 8, h = texture.height / 8;
        var slices = new System.Collections.Generic.List<SpriteMetaData>();
        string[] states = { "Hover", "Cast", "Hurt", "Death" };
        int[] rows = { 0, 5, 3, 4 }, counts = { 8, 8, 3, 5 };
        for (int s = 0; s < states.Length; s++)
            for (int i = 0; i < counts[s]; i++) slices.Add(new SpriteMetaData {
                name = states[s] + i, rect = new Rect(i * w + 76, (7 - rows[s]) * h + 4, 62, 85),
                alignment = 9, pivot = new Vector2(.5f, .5f)
            });
        importer.spritesheet = slices.ToArray(); importer.SaveAndReimport();
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
        GameObject root = new GameObject("RunnerSkyGhost"); root.layer = LayerMask.NameToLayer("Enemy");
        Rigidbody2D rb = root.AddComponent<Rigidbody2D>(); rb.bodyType = RigidbodyType2D.Kinematic; rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        CapsuleCollider2D body = root.AddComponent<CapsuleCollider2D>(); body.size = new Vector2(1.3f, 2.3f); body.isTrigger = true;
        GameObject art = new GameObject("Ghost"); art.transform.SetParent(root.transform, false);
        SpriteRenderer visual = art.AddComponent<SpriteRenderer>(); visual.sortingLayerName = "Enemy"; visual.sortingOrder = 6;
        RunnerSkyEnemy enemy = root.AddComponent<RunnerSkyEnemy>();
        enemy.HoverFrames = Frames(sprites, "Hover", 8); enemy.CastFrames = Frames(sprites, "Cast", 8);
        enemy.HurtFrames = Frames(sprites, "Hurt", 3); enemy.DeathFrames = Frames(sprites, "Death", 5);
        visual.sprite = enemy.HoverFrames[0];
        enemy.ProjectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Runner/Enemies/RunnerArcaneBolt.prefab").GetComponent<RunnerProjectile>();
        SerializedObject data = new SerializedObject(enemy); data.FindProperty("maxHealth").intValue = 160; data.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(root, "Assets/Resources/Runner/Enemies/RunnerSkyGhost.prefab");
        Object.DestroyImmediate(root); AssetDatabase.SaveAssets();
        Debug.Log("SKY GHOST ASSETS BUILT");
        if (Application.isBatchMode) EditorApplication.Exit(0);
    }
    private static Sprite[] Frames(Sprite[] sprites, string prefix, int count) =>
        Enumerable.Range(0, count).Select(i => sprites.First(s => s.name == prefix + i)).ToArray();
}
