using System.Collections.Generic;

public partial class GridBottom
{
    public void ClearColor(string hex)
    {
        if (_slots == null || string.IsNullOrEmpty(hex)) return;

        List<Anthill> victims = new List<Anthill>();

        foreach (Anthill item in _slots)
            if (item != null && ColonyPalette.SameColor(item.ColorHex, hex)) victims.Add(item);

        foreach (Anthill item in victims) item.ClearByColor();
    }
}
