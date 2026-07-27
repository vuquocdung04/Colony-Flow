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

    public void Rebuild()
    {
        members.RemoveAll(m => m == null);

        foreach (var s in members)
            if (s.Link != null) s.Link.Detach();

        if (members.Count < 2) return;

        foreach (var s in members)
            if (s.Link != null) s.Link.group = this;

        for (int i = 0; i < members.Count - 1; i++)
        {
            members[i].Connect(members[i + 1], true);
            members[i + 1].Connect(members[i], false);
        }
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
