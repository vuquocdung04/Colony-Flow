using DG.Tweening;
using UnityEngine;

public class Booster2InputMode : InputMode
{
    static readonly Vector3 CamPickPosition = new Vector3(0f, 27.1f, -22.4f);
    const float CamMoveDuration = 0.35f;
    const Ease CamMoveEase = Ease.OutCubic;

    Vector3 _camDefaultPosition;
    bool _camCached;
    Tween _camTween;

    public override void OnEnter(InputController controller)
    {
        base.OnEnter(controller);
        SetPicking(true);
        GameScene.EnableDarkPanel(true);
        MoveCamera(CamPickPosition);
    }

    public override void OnExit()
    {
        SetPicking(false);
        GameScene.EnableDarkPanel(false);
        if (_camCached) MoveCamera(_camDefaultPosition);
    }

    public override void HandleClick(RaycastHit hit)
    {
        if (hit.collider == null) return;

        Anthill anthill = hit.collider.GetComponentInParent<Anthill>();
        if (anthill == null || !anthill.BoosterSelect()) return;

        BoosterController.Instance.OnBoosterActionSuccess();
    }

    static void SetPicking(bool value)
    {
        GridBottom bottom = Colony.Instance != null ? Colony.Instance.Bottom : null;
        if (bottom != null) bottom.SetBoosterPicking(value);
    }

    void MoveCamera(Vector3 position)
    {
        Transform cam = ResolveCamera();
        if (cam == null) return;

        _camTween?.Kill();
        _camTween = cam.DOMove(position, CamMoveDuration)
                       .SetEase(CamMoveEase)
                       .SetUpdate(true);
    }

    Transform ResolveCamera()
    {
        Camera cam = GamePlayController.Instance != null ? GamePlayController.Instance.cameraGameplay : null;
        if (cam == null) return null;

        if (!_camCached)
        {
            _camDefaultPosition = cam.transform.position;
            _camCached = true;
        }

        return cam.transform;
    }
}
