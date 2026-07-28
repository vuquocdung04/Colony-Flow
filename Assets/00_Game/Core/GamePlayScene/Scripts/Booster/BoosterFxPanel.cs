using System.Collections.Generic;
using EventDispatcher;
using Sirenix.OdinInspector;
using Spine.Unity;
using UnityEngine;

public class BoosterFxPanel : Singleton<BoosterFxPanel>
{
    [System.Serializable]
    public class FxConfig
    {
        [TableColumnWidth(90, resizable: false)]
        public BoosterType type;
        public SkeletonGraphic spine;
        public string animKey;
        public float actionDelay;
        [Range(0f, 1f)] public float actionPercent = 1f;
    }

    [SerializeField] private CanvasGroup group;

    [TableList(AlwaysExpanded = true, DrawScrollView = false)]
    [SerializeField] private List<FxConfig> configs = new List<FxConfig>();

    protected override void OnAwake()
    {
        Hide();
        this.RegisterListener(EventID.BOOSTER_USED, OnBoosterUsed);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        this.RemoveListener(EventID.BOOSTER_USED, OnBoosterUsed);
    }

    public float GetActionDuration(BoosterType type)
    {
        FxConfig cfg = configs.Find(c => c.type == type);
        if (cfg == null) return 0f;

        return Mathf.Max(0f, AnimLength(cfg) * cfg.actionPercent);
    }

    private void OnBoosterUsed(object param)
    {
        BoosterType type = (BoosterType)param;
        FxConfig cfg = configs.Find(c => c.type == type);

        if (cfg == null || cfg.spine == null || string.IsNullOrEmpty(cfg.animKey))
        {
            this.PostEvent(EventID.BOOSTER_ACTION, type);
            return;
        }

        Run(cfg, type).Forget();
    }

    private async Awaitable Run(FxConfig cfg, BoosterType type)
    {
        Spine.TrackEntry entry = cfg.spine.AnimationState.SetAnimation(0, cfg.animKey, false);
        float length = entry != null && entry.Animation != null ? entry.Animation.Duration : 0f;

        Show(cfg.spine);

        if (cfg.actionDelay > 0f)
            await Awaitable.WaitForSecondsAsync(cfg.actionDelay, destroyCancellationToken);

        this.PostEvent(EventID.BOOSTER_ACTION, type);

        float remain = length - cfg.actionDelay;
        if (remain > 0f)
            await Awaitable.WaitForSecondsAsync(remain, destroyCancellationToken);

        Hide();
    }

    private static float AnimLength(FxConfig cfg)
    {
        if (cfg.spine == null || cfg.spine.skeletonDataAsset == null || string.IsNullOrEmpty(cfg.animKey))
            return 0f;

        Spine.SkeletonData data = cfg.spine.skeletonDataAsset.GetSkeletonData(true);
        Spine.Animation anim = data != null ? data.FindAnimation(cfg.animKey) : null;
        return anim != null ? anim.Duration : 0f;
    }

    private void Show(SkeletonGraphic target)
    {
        foreach (FxConfig cfg in configs)
        {
            if (cfg == null || cfg.spine == null) continue;
            SetAlpha(cfg.spine, cfg.spine == target ? 1f : 0f);
        }

        SetGroup(1f, true);
    }

    private void Hide() => SetGroup(0f, false);

    private void SetGroup(float alpha, bool block)
    {
        if (group == null) return;

        group.alpha = alpha;
        group.blocksRaycasts = block;
        group.interactable = block;
    }

    private static void SetAlpha(SkeletonGraphic spine, float alpha)
    {
        Color color = spine.color;
        color.a = alpha;
        spine.color = color;
    }
}
