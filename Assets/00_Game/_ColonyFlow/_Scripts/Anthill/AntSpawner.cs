using System;
using UnityEngine;

public class AntSpawner
{
    readonly Ant _prefab;
    readonly Transform _spawnPoint;
    readonly Transform _fallbackPoint;
    readonly float _interval;
    readonly Action<int> _onCapacityChanged;
    readonly Action _onDepleted;

    GridTop _grid;
    string _colorHex;
    int _remaining;
    bool _active;
    float _timer;

    public int Remaining => _remaining;

    public AntSpawner(Ant prefab, Transform spawnPoint, Transform fallbackPoint, float interval,
                      Action<int> onCapacityChanged, Action onDepleted)
    {
        _prefab = prefab;
        _spawnPoint = spawnPoint;
        _fallbackPoint = fallbackPoint;
        _interval = interval;
        _onCapacityChanged = onCapacityChanged;
        _onDepleted = onDepleted;
    }

    public void Setup(GridTop grid, string colorHex, int capacity)
    {
        _grid = grid;
        _colorHex = colorHex;
        _remaining = Mathf.Max(0, capacity);
        _onCapacityChanged?.Invoke(_remaining);
    }

    public void SetColor(string colorHex) => _colorHex = colorHex;

    public void Begin() => _active = true;

    public void Tick(float delta)
    {
        if (!_active || _remaining <= 0 || _grid == null || _prefab == null) return;

        _timer -= delta;
        if (_timer > 0f) return;
        _timer = _interval;

        Vector3 position = _spawnPoint != null ? _spawnPoint.position : _fallbackPoint.position;
        if (!_grid.TryReserve(_colorHex, position, out GridTarget target)) return;

        Ant ant = UnityEngine.Object.Instantiate(_prefab, position, _prefab.transform.rotation);
        ant.SetColor(_colorHex);
        ant.Init(_grid, target);

        _remaining--;
        _onCapacityChanged?.Invoke(_remaining);

        if (_remaining > 0) return;

        _active = false;
        _onDepleted?.Invoke();
    }
}
