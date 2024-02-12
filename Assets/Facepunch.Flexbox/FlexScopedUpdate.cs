using System;

namespace Facepunch.Flexbox
{
    public readonly struct FlexScopedUpdate : IDisposable
    {
        private readonly FlexElementBase _element;

        public FlexScopedUpdate(FlexElementBase element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            if (!element.IsAbsolute)
            {
                throw new ArgumentException("Scoped updates can only be started for absolute flex elements.");
            }

            if (!FlexLayoutManager.ActiveScopedUpdates.Add(element))
            {
                throw new InvalidOperationException("A scoped update is already active for this flex element.");
            }

            _element = element;
        }

        public void Dispose()
        {
            FlexLayoutManager.ActiveScopedUpdates.Remove(_element);
            FlexLayoutManager.LayoutImmediate(_element);
        }
    }
}
