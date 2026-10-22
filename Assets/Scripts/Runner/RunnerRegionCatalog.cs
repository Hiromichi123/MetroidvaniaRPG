using UnityEngine;

[System.Serializable]
public class RunnerRegionDefinition
{
    public string Name;
    public RunnerBackdrop Backdrop;
    public Enemy[] Enemies;
    public Color GroundTint = Color.white;
    public Sprite Decoration;
    public bool ForestLayout;
}

public class RunnerRegionCatalog : ScriptableObject
{
    public RunnerRegionDefinition[] Regions;
}
