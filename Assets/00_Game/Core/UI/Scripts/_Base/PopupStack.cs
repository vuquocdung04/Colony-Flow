using System.Collections.Generic;

public interface IPopupBox
{
    void Suspend();
    void Resume();
    float BackdropAlpha { get; }
}

public interface IPopupBackdrop
{
    void ShowBackdrop(float alpha, bool fade);
    void HideBackdrop(bool fade);
}

public static class PopupStack
{
    private class Entry
    {
        public IPopupBox Box;
        public IPopupBox Covered;
    }

    private static readonly List<Entry> _stack = new();
    private static IPopupBackdrop _backdrop;

    public static bool IsEmpty => _stack.Count == 0;

    public static void RegisterBackdrop(IPopupBackdrop backdrop)
    {
        _backdrop = backdrop;
        RefreshBackdrop(false);
    }

    public static void UnregisterBackdrop(IPopupBackdrop backdrop)
    {
        if (ReferenceEquals(_backdrop, backdrop)) _backdrop = null;
    }

    public static void Push(IPopupBox box, bool cover)
    {
        RemoveOwn(box);

        bool wasEmpty = _stack.Count == 0;
        IPopupBox covered = null;
        if (cover && _stack.Count > 0)
        {
            covered = _stack[_stack.Count - 1].Box;
            covered.Suspend();
        }

        _stack.Add(new Entry { Box = box, Covered = covered });
        RefreshBackdrop(wasEmpty);
    }

    public static IPopupBox Pop(IPopupBox box)
    {
        int idx = FindLast(box);
        if (idx < 0) return null;

        var covered = _stack[idx].Covered;
        _stack.RemoveAt(idx);

        return covered is UnityEngine.Object o && o != null ? covered : null;
    }

    public static void Remove(IPopupBox box)
    {
        RemoveOwn(box);
        foreach (var e in _stack)
            if (ReferenceEquals(e.Covered, box)) e.Covered = null;
        RefreshBackdrop(false);
    }

    public static void RefreshBackdrop(bool fade)
    {
        if (_backdrop == null) return;

        if (_stack.Count == 0)
        {
            _backdrop.HideBackdrop(fade);
            return;
        }

        var top = _stack[_stack.Count - 1].Box;
        _backdrop.ShowBackdrop(top.BackdropAlpha, fade);
    }

    private static void RemoveOwn(IPopupBox box)
    {
        int idx = FindLast(box);
        if (idx >= 0) _stack.RemoveAt(idx);
    }

    private static int FindLast(IPopupBox box)
    {
        for (int i = _stack.Count - 1; i >= 0; i--)
            if (ReferenceEquals(_stack[i].Box, box)) return i;
        return -1;
    }
}
