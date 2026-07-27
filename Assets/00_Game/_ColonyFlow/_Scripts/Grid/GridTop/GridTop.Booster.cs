public partial class GridTop
{
    public string ColorAt(int index) =>
        _colors != null && index >= 0 && index < _colors.Length ? _colors[index] : null;

    public int ClearColor(string hex)
    {
        if (_colors == null || string.IsNullOrEmpty(hex)) return 0;

        int cleared = 0;

        for (int index = 0; index < _colors.Length; index++)
        {
            if (!SameColor(_colors[index], hex)) continue;

            _colors[index] = null;
            _reserved[index] = false;
            _remaining--;
            cleared++;

            if (_hidden[index])
            {
                _hidden[index] = false;
                _hiddenRemaining--;
            }

            FoodObj cell = _cells[index];
            _cells[index] = null;
            if (cell != null) cell.Clear();
        }

        if (cleared > 0) RebuildOpen();
        return cleared;
    }
}
