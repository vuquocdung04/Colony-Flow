using System;
using System.Collections.Generic;
using EventDispatcher;
using TMPro;
using UnityEngine;

public abstract class BlockerBase : MonoBehaviour
{
    [Header("Refs")]
    public Transform visual;
    public TMP_Text capacityLabel;

    [Space, Header("Size - khớp với vùng che")]
    public Vector3 baseSize = Vector3.one;
    public float sizeMultiplier = 1f;

    public event Action<BlockerBase> Emptied;

    public int Capacity { get; private set; }

    public bool IsEmpty => Capacity <= 0;

    public Transform Root => visual != null ? visual : transform;

    readonly List<GameObject> _covered = new List<GameObject>();

    Vector3 _baseScale = Vector3.one;
    bool _hasBaseScale;

    protected virtual void Awake() => CacheBaseScale();

    protected virtual void OnEnable() => this.RegisterListener(EventID.BLOCK_DESTROYED, OnBlockDestroyed);

    protected virtual void OnDisable() => this.RemoveListener(EventID.BLOCK_DESTROYED, OnBlockDestroyed);

    public void Setup(int capacity, List<GameObject> covered)
    {
        Capacity = Mathf.Max(0, capacity);
        RefreshLabel();

        _covered.Clear();
        if (covered == null) return;

        foreach (GameObject target in covered)
        {
            if (target == null) continue;

            _covered.Add(target);
            target.SetActive(false);
        }
    }

    public void SetSize(Vector3 target) =>
        SetScale(Mathf.Min(Ratio(target.x, baseSize.x), Ratio(target.z, baseSize.z)));

    public void SetScale(float factor)
    {
        CacheBaseScale();
        Root.localScale = _baseScale * (factor * sizeMultiplier);
    }

    static float Ratio(float target, float natural) => natural > Mathf.Epsilon ? target / natural : 1f;

    public bool Consume(int amount = 1)
    {
        if (IsEmpty || amount <= 0) return false;

        Capacity = Mathf.Max(0, Capacity - amount);
        RefreshLabel();

        if (!IsEmpty) return false;

        OnEmpty();
        return true;
    }

    protected abstract bool Accepts(object param);

    void OnBlockDestroyed(object param)
    {
        if (Accepts(param)) Consume();
    }

    void OnEmpty()
    {
        RevealCovered();
        Emptied?.Invoke(this);
        PlayEmpty(Remove);
    }

    protected virtual void PlayEmpty(Action onComplete) => onComplete?.Invoke();

    void Remove() => Destroy(gameObject);

    void RevealCovered()
    {
        foreach (GameObject target in _covered)
            if (target != null) target.SetActive(true);

        _covered.Clear();
    }

    void RefreshLabel()
    {
        if (capacityLabel == null) return;

        capacityLabel.text = Capacity.ToString();
        capacityLabel.gameObject.SetActive(Capacity > 0);
    }

    void CacheBaseScale()
    {
        if (_hasBaseScale) return;

        _baseScale = Root.localScale;
        _hasBaseScale = true;
    }
}
