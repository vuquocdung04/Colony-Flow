using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class WaitAreas : MonoBehaviour
{
    public Transform holder;
    public GameObject slotPrefab;
    [Min(0)] public int slotCount = 5;
    public float spacingX = 1f;

    public bool showAimGizmos = true;
    public Color aimColor = new Color(0.2f, 1f, 0.5f, 0.9f);

    public List<Transform> slots = new List<Transform>();

    private Transform Holder => holder != null ? holder : transform;

    private Anthill[] _occupants;

    public int FreeSlots
    {
        get
        {
            EnsureOccupants(slots.Count);

            int free = 0;
            for (int i = 0; i < slots.Count; i++)
                if (slots[i] != null && _occupants[i] == null) free++;
            return free;
        }
    }

    public bool TryPlace(Anthill anthill, out Vector3 slotPosition)
    {
        slotPosition = Vector3.zero;
        if (anthill == null) return false;

        EnsureOccupants(slots.Count);

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null || _occupants[i] != null) continue;

            _occupants[i] = anthill;
            slotPosition = slots[i].position;
            return true;
        }

        return false;
    }

    public void ClearColor(string hex)
    {
        if (_occupants == null || string.IsNullOrEmpty(hex)) return;

        for (int i = _occupants.Length - 1; i >= 0; i--)
        {
            Anthill occupant = _occupants[i];
            if (occupant == null) continue;
            if (!ColonyPalette.SameColor(occupant.ColorHex, hex)) continue;

            occupant.ClearByColor();
        }
    }

    public void Release(Anthill anthill)
    {
        if (_occupants == null) return;

        for (int i = 0; i < _occupants.Length; i++)
            if (_occupants[i] == anthill) _occupants[i] = null;
    }

    private void Update()
    {
        if (_occupants == null) return;

        float delta = Time.deltaTime;

        for (int i = 0; i < _occupants.Length; i++)
        {
            Anthill occupant = _occupants[i];
            if (occupant == null) continue;

            occupant.Tick(delta);
        }
    }

    private void EnsureOccupants(int count)
    {
        if (_occupants != null && _occupants.Length == count) return;

        Anthill[] next = new Anthill[count];
        if (_occupants != null)
            for (int i = 0; i < _occupants.Length && i < count; i++) next[i] = _occupants[i];

        _occupants = next;
    }

    [Button(ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]
    public void Generate()
    {
        ClearSlots();

        if (slotPrefab == null || slotCount <= 0) return;

        for (int i = 0; i < slotCount; i++) slots.Add(SpawnSlot(i).transform);

        ApplyLayout();
        _occupants = new Anthill[slots.Count];
    }

    [Button(ButtonSizes.Medium)]
    public bool AddSlot()
    {
        if (slotPrefab == null) return false;

        slots.Add(SpawnSlot(slots.Count).transform);
        slotCount = slots.Count;

        EnsureOccupants(slots.Count);
        ApplyLayout();
        RefreshOccupants();
        return true;
    }

    private GameObject SpawnSlot(int index)
    {
        GameObject slot = InstantiateSlot();
        slot.name = $"{slotPrefab.name}_{index}";
        slot.transform.SetParent(Holder, false);
        slot.transform.localRotation = Quaternion.identity;
        slot.transform.localScale = slotPrefab.transform.localScale;
        return slot;
    }

    private void ApplyLayout()
    {
        float startX = -(slots.Count - 1) * spacingX * 0.5f;

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null) continue;
            slots[i].localPosition = new Vector3(startX + i * spacingX, 0f, 0f);
        }
    }

    private void RefreshOccupants()
    {
        if (_occupants == null) return;

        for (int i = 0; i < _occupants.Length && i < slots.Count; i++)
        {
            Anthill occupant = _occupants[i];
            if (occupant == null || slots[i] == null) continue;

            occupant.MoveToSlot(slots[i].position);
        }
    }

    [Button(ButtonSizes.Medium)]
    public void ClearSlots()
    {
        Transform parent = Holder;
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            GameObject child = parent.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }

        slots.Clear();
        _occupants = null;
    }

    private GameObject InstantiateSlot()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            return (GameObject)PrefabUtility.InstantiatePrefab(slotPrefab, Holder);
#endif
        return Instantiate(slotPrefab, Holder);
    }

    private void OnDrawGizmos()
    {
        if (!showAimGizmos || slots == null) return;

        GridTop gridTop = Colony.Instance != null ? Colony.Instance.Top : null;
        if (gridTop == null) return;

        float radius = gridTop.CellSize.x * 0.15f;

        Gizmos.color = aimColor;
        foreach (Transform slot in slots)
        {
            if (slot == null) continue;

            Vector3 hit = gridTop.StraightHit(slot.position);
            Gizmos.DrawLine(slot.position, hit);
            Gizmos.DrawWireSphere(slot.position, radius);
        }
    }
}
