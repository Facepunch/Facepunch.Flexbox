using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Facepunch.Flexbox
{
    internal struct FlexChildEnumerable : IEnumerable<IFlexNode>
    {
        private readonly FlexElement _parent;
        private readonly bool _reversed;

        public FlexChildEnumerable(FlexElement parent, bool reversed)
        {
            _parent = parent;
            _reversed = reversed;
        }

        public FlexChildEnumerator GetEnumerator() => new FlexChildEnumerator(_parent, _reversed);

        IEnumerator<IFlexNode> IEnumerable<IFlexNode>.GetEnumerator() => throw new NotSupportedException();

        IEnumerator IEnumerable.GetEnumerator() => throw new NotSupportedException();
    }

    internal struct FlexChildEnumerator : IEnumerator<IFlexNode>
    {
        private readonly Transform _parent;
        private readonly int _childCount;
        private readonly bool _reversed;
        private int _index;

        public IFlexNode Current { get; private set; }

        public FlexChildEnumerator(FlexElement parent, bool reversed)
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
                if (!obj.TryGetComponent<IFlexNode>(out var child) || !child.IsActive || child.IsAbsolute)
                {
                    _index += _reversed ? -1 : 1;
                    continue;
                }

                Current = child;
                _index += _reversed ? -1 : 1;
                return true;
            }
        }

        object IEnumerator.Current => Current;

        public void Reset()
        {
        }

        public void Dispose()
        {
        }
    }
}
