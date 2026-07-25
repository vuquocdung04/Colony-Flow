using System.Collections.Generic;

public class LinkGroup
{
    public List<Anthill> members = new List<Anthill>();

    readonly GridBottom _board;

    public LinkGroup(GridBottom board) => _board = board;

    public Anthill Leader => members.Count > 0 ? members[0] : null;
    public int Count => members.Count;

    public bool AllEmpty()
    {
        foreach (var s in members)
            if (s != null && s.Capacity > 0) return false;
        return true;
    }

    public bool CanClick(Anthill clicked, int gridX)
    {
        foreach (var s in members)
            if (s != null && s.IsLocked) return false;

        if (clicked.Index / gridX != 0) return false;

        var memberSet = new HashSet<Anthill>(members);
        foreach (var s in members)
        {
            if (s == null || s == clicked) continue;

            int sRow = s.Index / gridX;
            if (sRow == 0) continue;

            int aboveIdx = s.Index - gridX;
            Anthill above = _board != null ? _board.SlotAt(aboveIdx) : null;
            if (above == null) return false;
            if (!memberSet.Contains(above)) return false;
        }

        return true;
    }
}
