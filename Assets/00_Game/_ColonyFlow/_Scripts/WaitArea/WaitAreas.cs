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

    private Transform Holder => holder != null ? holder : transform;

    private Anthill[] _occupants;

    public bool TryPlace(Anthill anthill, out Vector3 slotPosition)
    {
        slotPosition = Vector3.zero;
        if (anthill == null) return false;

        Transform parent = Holder;
        EnsureOccupants(parent.childCount);

        for (int i = 0; i < _occupants.Length; i++)
        {
            if (_occupants[i] != null) continue;

            _occupants[i] = anthill;
            slotPosition = parent.GetChild(i).position;
            return true;
        }

        return false;
    }

    public void Release(Anthill anthill)
    {
        if (_occupants == null) return;

        for (int i = 0; i < _occupants.Length; i++)
            if (_occupants[i] == anthill) _occupants[i] = null;
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

        float startX = -(slotCount - 1) * spacingX * 0.5f;

        for (int i = 0; i < slotCount; i++)
        {
            GameObject slot = InstantiateSlot();
            slot.name = $"{slotPrefab.name}_{i}";
            slot.transform.SetParent(Holder, false);
            slot.transform.localPosition = new Vector3(startX + i * spacingX, 0f, 0f);
            slot.transform.localRotation = Quaternion.identity;
            slot.transform.localScale = slotPrefab.transform.localScale;
        }

        _occupants = new Anthill[slotCount];
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
}
