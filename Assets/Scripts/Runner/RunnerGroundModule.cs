using UnityEngine;
using UnityEngine.Tilemaps;

// The tile layout is copied from an authored block in SampleScene, including
// its carpet, sides, underside, tile colours and transforms.
public class RunnerGroundModule : MonoBehaviour
{
    public Tilemap Tiles;
    public int NativeWidth;
    public int Height;
    public int PreserveLeft;
    public int PreserveRight;
    public RectInt SourceCells;

    public void SetWidth(int width)
    {
        if (Tiles == null || width == NativeWidth) return;
        TileBase[] originals = Tiles.GetTilesBlock(new BoundsInt(0, -Height, 0, NativeWidth, Height, 1));
        var colors = new Color[originals.Length];
        var matrices = new Matrix4x4[originals.Length];
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < NativeWidth; x++)
            {
                int index = y * NativeWidth + x;
                Vector3Int cell = new Vector3Int(x, y - Height, 0);
                colors[index] = Tiles.GetColor(cell);
                matrices[index] = Tiles.GetTransformMatrix(cell);
            }
        Tiles.ClearAllTiles();
        int middle = NativeWidth - PreserveLeft - PreserveRight;
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < width; x++)
            {
                int sourceX = x < PreserveLeft ? x : x >= width - PreserveRight ?
                    NativeWidth - (width - x) : PreserveLeft + (x - PreserveLeft) % middle;
                int index = y * NativeWidth + sourceX;
                if (originals[index] == null) continue;
                Vector3Int cell = new Vector3Int(x, y - Height, 0);
                Tiles.SetTile(cell, originals[index]);
                Tiles.SetTileFlags(cell, TileFlags.None);
                Tiles.SetColor(cell, colors[index]);
                Tiles.SetTransformMatrix(cell, matrices[index]);
            }
        Tiles.CompressBounds();
    }
}
