using System.Collections.Generic;
using EventDispatcher;
using Sirenix.OdinInspector;
using Spine.Unity;
using UnityEngine;

public class BoosterFxPanel : MonoBehaviour
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

    private void Awake()
    {
        Hide();
        this.RegisterListener(EventID.BOOSTER_USED, OnBoosterUsed);
    }

    private void OnDestroy()
    {
        this.RemoveListener(EventID.BOOSTER_USED, OnBoosterUsed);
    }

    private void OnBoosterUsed(object param)
    {
        BoosterType type = (BoosterType)param;
        FxConfig cfg = configs.Find(c => c.type == type);

        if (cfg == null || cfg.spine == null || string.IsNullOrEmpty(cfg.animKey))
        {
            this.PostEvent(EventID.BOOSTER_ACTION, new BoosterActionInfo(type, 0f));
            return;
        }

        Run(cfg, type).Forget();
    }

    private async Awaitable Run(FxConfig cfg, BoosterType type)
    {
        Spine.TrackEntry entry = cfg.spine.AnimationState.SetAnimation(0, cfg.animKey, false);
        float length = entry != null && entry.Animation != null ? entry.Animation.Duration : 0f;
        float actionDuration = Mathf.Max(0f, length * cfg.actionPercent);

        Show(cfg.spine);

        if (cfg.actionDelay > 0f)
            await Awaitable.WaitForSecondsAsync(cfg.actionDelay, destroyCancellationToken);

        this.PostEvent(EventID.BOOSTER_ACTION, new BoosterActionInfo(type, actionDuration));

        float remain = length - cfg.actionDelay;
        if (remain > 0f)
            await Awaitable.WaitForSecondsAsync(remain, destroyCancellationToken);

        Hide();
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
