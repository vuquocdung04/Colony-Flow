using UnityEngine;

public partial class GridBottom
{
    public Vector3 CellSize => anthill != null ? anthill.Size : Vector3.one;

    public Vector3 CellCenter(int x, int y) => CellCenter(x, y, CellSize);

    public Vector3 SlotCenter(int index) => SlotCenter(index, CellSize);

    Vector3 SlotCenter(int index, Vector3 cell) =>
        CellCenter(ColonyGridIndex.X(index, gridX), ColonyGridIndex.Y(index, gridX), cell);

    Vector3 CellCenter(int x, int y, Vector3 cell)
    {
        float totalWidth = gridX * cell.x + (gridX - 1) * spacingX;
        float originX = -totalWidth * 0.5f + cell.x * 0.5f;

        Vector3 local = new Vector3(
            originX + x * (cell.x + spacingX),
            0f,
            -y * (cell.z + spacingZ));

        return Holder.TransformPoint(local);
    }
}
