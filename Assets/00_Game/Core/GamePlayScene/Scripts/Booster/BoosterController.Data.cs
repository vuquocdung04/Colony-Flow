using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public partial class BoosterController
{
    [System.Serializable]
    public class BoosterConfig
    {
        [TableColumnWidth(90, resizable: false)]
        public BoosterType type;
        public int levelUnlock;
        public int quantity;
    }

    [Title("Items")]
    [SerializeField] private List<BoosterItem> items;

    [TableList(AlwaysExpanded = true, DrawScrollView = false)]
    [SerializeField] private List<BoosterConfig> configs;

    private int CurrentLevel => UseProfile.Level.Value;

    private BoosterConfig GetConfig(BoosterType type) => configs.Find(c => c.type == type);

    private void SeedData()
    {
        foreach (var cfg in configs)
        {
            if (BoosterData.Has(cfg.type)) continue;
            BoosterData.Get(cfg.type).Amount = cfg.quantity;
            BoosterData.MarkDirty();
        }
    }

    private void ApplyConfigToItems()
    {
        foreach (var item in items)
        {
            var cfg = GetConfig(item.Type);
            item.SetUnlockLevel(cfg.levelUnlock);

            bool unlocked = CurrentLevel >= cfg.levelUnlock;

            if (unlocked)
            {
                item.ChangeState(BoosterState.Available, force: true);
                item.SetData(GetQuantity(item.Type));
            }
            else
            {
                item.ChangeState(BoosterState.Locked, force: true);
            }
        }
    }

    private int GetQuantity(BoosterType type) => BoosterData.Get(type).Amount;

    private void SetQuantity(BoosterType type, int amount)
    {
        BoosterData.Get(type).Amount = Mathf.Max(0, amount);
        BoosterData.MarkDirty();
    }

    private void Consume(BoosterType type) => SetQuantity(type, GetQuantity(type) - 1);

    public void AddQuantity(BoosterType type, int amount)
    {
        SetQuantity(type, GetQuantity(type) + amount);
        FindItem(type)?.SetData(GetQuantity(type));
    }

    private bool IsTutorialDone(BoosterType type) => BoosterData.Get(type).TutorialDone;

    private void SetTutorialDone(BoosterType type, bool done)
    {
        BoosterData.Get(type).TutorialDone = done;
        BoosterData.MarkDirty();
    }

    public BoosterItem FindItem(BoosterType type) => items.Find(i => i.Type == type);

    public BoosterItem GetItemByIndex(int index) =>
        (items != null && index >= 0 && index < items.Count) ? items[index] : null;
}
