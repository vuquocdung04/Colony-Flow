using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public partial class GridTop
{
    [BoxGroup("Booster"), Range(0f, 0.9f)] public float clearStagger = 0.55f;

    readonly List<int> _clearTargets = new List<int>();

    public string ColorAt(int index) =>
        _colors != null && index >= 0 && index < _colors.Length ? _colors[index] : null;

    public bool CanBoosterPick(int index) =>
        !IsBlocked(index) && !string.IsNullOrEmpty(ColorAt(index));

    public void SetBoosterPicking(bool value)
    {
        if (_cells == null) return;

        for (int index = 0; index < _cells.Length; index++)
        {
            FoodObj cell = _cells[index];
            if (cell == null) continue;

            HighlightObject.Set(cell.gameObject, value && CanBoosterPick(index));
        }
    }

    public int ClearColor(string hex, float duration)
    {
        if (_colors == null || string.IsNullOrEmpty(hex)) return 0;

        EnsureLayout();

        Vector3 center = suckPoint != null ? suckPoint.position : Holder.position;

        _clearTargets.Clear();
        float farthest = 0f;

        for (int index = 0; index < _colors.Length; index++)
        {
            if (!SameColor(_colors[index], hex)) continue;

            _clearTargets.Add(index);

            float distance = FlatDistance(index, center);
            if (distance > farthest) farthest = distance;
        }

        if (_clearTargets.Count == 0) return 0;

        float travel = duration * (1f - clearStagger);
        float spread = duration * clearStagger;

        foreach (int index in _clearTargets)
        {
            _colors[index] = null;
            _reserved[index] = false;
            _remaining--;

            if (_hidden[index])
            {
                _hidden[index] = false;
                _hiddenRemaining--;
            }

            FoodObj cell = _cells[index];
            _cells[index] = null;
            if (cell == null) continue;

            float t = farthest > Mathf.Epsilon ? FlatDistance(index, center) / farthest : 0f;
            cell.Clear(center, t * spread, travel);
        }

        RebuildOpen();
        return _clearTargets.Count;
    }

    float FlatDistance(int index, Vector3 center)
    {
        FoodObj cell = _cells[index];
        Vector3 from = cell != null
            ? cell.transform.position
            : CellWorld(ColonyGridIndex.X(index, gridX), ColonyGridIndex.Y(index, gridX));

        return new Vector2(from.x - center.x, from.z - center.z).magnitude;
    }
}
