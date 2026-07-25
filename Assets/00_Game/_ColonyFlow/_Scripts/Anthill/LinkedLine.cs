using System.Collections.Generic;
using UnityEngine;

public class LinkedLine : MonoBehaviour
{
    [SerializeField] private GameObject linkVisual;
    [SerializeField] private MeshRenderer ropeForAnthill;
    [SerializeField] private MeshRenderer cylinderRope;
    [SerializeField] private Transform ropePoint;
    [SerializeField] private Transform connectionPivot;

    [Space(10), Header("Rope Anchor")]
    [SerializeField] private Transform waitPoint;
    [SerializeField] private Transform sleepPoint;

    [Space(10), Header("Rope Material")]
    [SerializeField] private Material normalMaterial;
    [SerializeField] private Material hiddenMaterial;

    [Space(10), Header("Rope Config")]
    [SerializeField] private float ropeThickness = 0.1f;
    [SerializeField] private float ropeNativeLength = 1f;
    [SerializeField] private float loweredTiltX = 10f;

    public List<Anthill> partners = new List<Anthill>();
    public Anthill ownedPartner;
    public LinkGroup group;

    public Transform RopePoint => ropePoint;
    public bool IsOwner => ownedPartner != null;
    public bool IsInGroup => group != null;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private MaterialPropertyBlock _mpb;

    private Anthill _owner;

    private Color OwnerColor => ColonyPalette.ToColor(_owner.ColorHex);

    public void Init(Anthill owner)
    {
        _owner = owner;
        if (linkVisual != null) linkVisual.SetActive(false);
    }

    public void ApplyState(bool lowered)
    {
        if (ropeForAnthill == null) return;

        Transform target = lowered ? sleepPoint : waitPoint;
        if (target != null) ropeForAnthill.transform.position = target.position;
    }

    public void Detach()
    {
        if (linkVisual != null) linkVisual.SetActive(false);
        if (connectionPivot != null) connectionPivot.gameObject.SetActive(false);
        partners.Clear();
        ownedPartner = null;
        group = null;
    }

    public void Setup(Anthill partner, bool owner)
    {
        if (partner == null) return;

        if (!partners.Contains(partner))
            partners.Add(partner);

        if (owner)
            ownedPartner = partner;

        if (linkVisual != null) linkVisual.SetActive(true);

        RefreshRopeForAnthill();

        if (owner)
        {
            connectionPivot.gameObject.SetActive(true);
            RefreshRopeLook();
            RefreshLink();
        }
        else
        {
            connectionPivot.gameObject.SetActive(false);
        }
    }

    public void RefreshRopeForAnthill()
    {
        bool hidden = _owner != null && _owner.IsHiddenNow;
        ApplyLook(ropeForAnthill, 0, hidden, OwnerColor);
    }

    public void RefreshRopeLook()
    {
        if (ownedPartner == null) return;

        bool ownerHidden = _owner != null && _owner.IsHiddenNow;
        bool partnerHidden = ownedPartner.IsHiddenNow;

        ApplyLook(cylinderRope, 0, ownerHidden, OwnerColor);
        ApplyLook(cylinderRope, 1, partnerHidden, ColonyPalette.ToColor(ownedPartner.ColorHex));
    }

    public void RefreshAllLooks()
    {
        RefreshRopeForAnthill();
        RefreshRopeLook();

        foreach (var partner in partners)
        {
            if (partner == null) continue;
            LinkedLine pl = partner.Link;
            if (pl != null && pl.ownedPartner == _owner)
                pl.RefreshRopeLook();
        }
    }

    private void ApplyLook(MeshRenderer renderer, int slot, bool hidden, Color color)
    {
        if (renderer == null) return;

        Material[] mats = renderer.sharedMaterials;
        if (slot >= mats.Length) return;

        mats[slot] = hidden ? hiddenMaterial : normalMaterial;
        renderer.sharedMaterials = mats;

        if (hidden) return;

        _mpb ??= new MaterialPropertyBlock();
        renderer.GetPropertyBlock(_mpb, slot);
        _mpb.SetColor(BaseColorId, color);
        renderer.SetPropertyBlock(_mpb, slot);
    }

    public void RefreshLink()
    {
        if (ownedPartner == null) return;

        LinkedLine partnerLink = ownedPartner.Link;
        if (partnerLink == null || partnerLink.ropePoint == null) return;
        if (ropePoint == null || connectionPivot == null) return;

        Vector3 a = ropePoint.position;
        Vector3 b = partnerLink.ropePoint.position;
        Vector3 dir = b - a;
        float dist = dir.magnitude;

        connectionPivot.position = a;
        if (dist > 0.0001f)
        {
            connectionPivot.forward = dir / dist;
            if (ownedPartner.IsLowered) connectionPivot.Rotate(loweredTiltX, 0f, 0f, Space.Self);
        }

        connectionPivot.localScale = new Vector3(
            ropeThickness,
            ropeThickness,
            dist / ropeNativeLength
        );
    }

    public void RefreshAllLinks()
    {
        RefreshLink();

        foreach (var partner in partners)
        {
            if (partner == null) continue;
            LinkedLine pl = partner.Link;
            if (pl != null && pl.ownedPartner == _owner)
                pl.RefreshLink();
        }
    }
}
