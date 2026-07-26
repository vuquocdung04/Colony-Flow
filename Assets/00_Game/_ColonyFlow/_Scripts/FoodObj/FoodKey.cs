using DG.Tweening;
using UnityEngine;

public class FoodKey : MonoBehaviour
{
    public MeshRenderer meshRenderer;

    [Header("Spin")]
    public float spinSpeed = 120f;

    [Header("Fly")]
    public float jumpPower = 2f;
    public float jumpDuration = 0.6f;
    public Ease jumpEase = Ease.OutQuad;

    Color _color = Color.white;
    bool _locked;
    Tween _spin;

    public bool IsLocked => _locked;
    public string ColorHex { get; private set; }

    void OnEnable() => StartSpin();

    void OnDisable() => StopSpin();

    public void SetColor(string hex)
    {
        ColorHex = hex;
        SetColor(ColonyPalette.ToColor(hex));
    }

    public void SetColor(Color color)
    {
        _color = color;
        ColonyPalette.Tint(meshRenderer, _color);
    }

    public void SetLocked(bool value) => _locked = value;

    public void Unlock() => SetLocked(false);

    public void Fly(Vector3 worldTarget, TweenCallback onArrive)
    {
        StopSpin();
        transform.SetParent(null, true);

        transform.DOJump(worldTarget, jumpPower, 1, jumpDuration)
                 .SetEase(jumpEase)
                 .SetLink(gameObject)
                 .OnComplete(() =>
                 {
                     onArrive?.Invoke();
                     Destroy(gameObject);
                 });
    }

    void StartSpin()
    {
        StopSpin();
        if (spinSpeed <= 0f) return;

        Vector3 baseEuler = transform.localEulerAngles;

        _spin = transform.DOLocalRotate(baseEuler + new Vector3(0f, 360f, 0f), 360f / spinSpeed,
                                        RotateMode.FastBeyond360)
                         .SetEase(Ease.Linear)
                         .SetLoops(-1, LoopType.Restart)
                         .SetLink(gameObject);
    }

    void StopSpin()
    {
        _spin?.Kill();
        _spin = null;
    }
}
