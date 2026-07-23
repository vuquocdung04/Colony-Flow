using System.Collections;
using TMPro;
using UnityEngine;

public class Anthill : MonoBehaviour
{
    public MeshRenderer body;
    public MeshRenderer lid;
    public TMP_Text capacityLabel;

    public Ant antPrefab;
    public Transform spawnPoint;

    public float spawnInterval = 0.35f;
    public float moveDuration = 0.3f;

    public string ColorHex { get; private set; }
    public int Capacity { get; private set; }
    public bool IsTaken { get; private set; }

    MaterialPropertyBlock _block;
    GridTop _grid;
    WaitAreas _waitAreas;
    bool _spawning;
    float _timer;

    public Vector3 Size
    {
        get
        {
            if (lid == null) return Vector3.one;
            Vector3 size = lid.bounds.size;
            return new Vector3(
                size.x > Mathf.Epsilon ? size.x : 1f,
                size.y,
                size.z > Mathf.Epsilon ? size.z : 1f);
        }
    }

    public void Setup(string hex, int capacity, GridTop grid, WaitAreas areas)
    {
        _grid = grid;
        _waitAreas = areas;
        Capacity = Mathf.Max(0, capacity);
        SetColor(hex);
        RefreshLabel();
    }

    public void SetColor(string hex)
    {
        ColorHex = hex;
        SetColor(ColonyPalette.ToColor(hex));
    }

    public void SetColor(Color color)
    {
        _block ??= new MaterialPropertyBlock();
        ColonyPalette.Tint(body, _block, color);
        ColonyPalette.Tint(lid, _block, color);
    }

    public bool TrySelect()
    {
        if (IsTaken || _waitAreas == null) return false;
        if (!_waitAreas.TryPlace(this, out Vector3 slotPosition)) return false;

        IsTaken = true;
        StartCoroutine(MoveThenSpawn(slotPosition));
        return true;
    }

    public void OnEmpty()
    {
        if (_waitAreas != null) _waitAreas.Release(this);
        Destroy(gameObject);
    }

    IEnumerator MoveThenSpawn(Vector3 target)
    {
        Vector3 start = transform.position;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(start, target, moveDuration > 0f ? elapsed / moveDuration : 1f);
            yield return null;
        }

        transform.position = target;
        _spawning = true;
    }

    void Update()
    {
        if (!_spawning || Capacity <= 0 || _grid == null || antPrefab == null) return;

        _timer -= Time.deltaTime;
        if (_timer > 0f) return;
        _timer = spawnInterval;

        if (!_grid.TryReserve(ColorHex, out GridTarget target)) return;

        Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
        Ant ant = Instantiate(antPrefab, position, antPrefab.transform.rotation);
        ant.SetColor(ColorHex);
        ant.Init(_grid, target);

        Capacity--;
        RefreshLabel();

        if (Capacity > 0) return;

        _spawning = false;
        OnEmpty();
    }

    void RefreshLabel()
    {
        if (capacityLabel != null) capacityLabel.text = Capacity.ToString();
    }
}
