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
