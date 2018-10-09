using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace PlayAndLearn.Utils
{
    internal static class AvaloniaExtensions
    {
        public static IEnumerable<T> FindVisualChildren<T>(this IVisual control)
        {
            foreach (var child in control.VisualChildren)
            {
                if (child is T tChild)
                {
                    yield return tChild;
                }
                foreach (var subChild in FindVisualChildren<T>(child))
                {
                    yield return subChild;
                }
            }
        }

        public static TElement AttachProperty<TElement, TValue>(this TElement element, AvaloniaProperty<TValue> property, TValue value)
            where TElement : AvaloniaObject
        {
            element.SetValue(property, value);
            return element;
        }

        public static IDisposable AddChild(this Panel panel, IControl child)
        {
            panel.Children.Add(child);
            return Disposable.Create(() => panel.Children.Remove(child));
        }
    }
}