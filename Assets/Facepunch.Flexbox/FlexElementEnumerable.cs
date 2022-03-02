using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal struct FlexElementEnumerable : IEnumerable<FlexElement>
{
    private readonly FlexElement _parent;
    private readonly bool _reversed;

    public FlexElementEnumerable(FlexElement parent, bool reversed)
    {
        _parent = parent;
        _reversed = reversed;
    }

    public FlexElementEnumerator GetEnumerator()
    {
        return new FlexElementEnumerator(_parent, _reversed);
    }

    IEnumerator<FlexElement> IEnumerable<FlexElement>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

internal struct FlexElementEnumerator : IEnumerator<FlexElement>
{
    private readonly Transform _parent;
    private readonly int _childCount;
    private readonly bool _reversed;
    private int _index;

    public FlexElement Current { get; private set; }

    public FlexElementEnumerator(FlexElement parent, bool reversed)
    {
        _parent = parent.transform;
        _childCount = _parent.childCount;
        _reversed = reversed;
        _index = reversed ? _childCount - 1 : 0;
        Current = null;
    }

    public bool MoveNext()
    {
        while (true)
        {
            var complete = _reversed
                ? _index < 0
                : _index >= _childCount;
            if (complete)
            {
                Current = null;
                return false;
            }

            var obj = _parent.GetChild(_index).gameObject;
            if (!obj.TryGetComponent<FlexElement>(out var elem) || !elem.IsActive() || elem.IsAbsolute)
            {
                _index += _reversed ? -1 : 1;
                continue;
            }

            Current = elem;
            _index += _reversed ? -1 : 1;
            return true;
        }
    }

    object IEnumerator.Current => Current;

    public void Reset() { }

    public void Dispose() { }
}
