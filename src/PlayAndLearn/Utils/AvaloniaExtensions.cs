using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace PlayAndLearn.Utils
{
    public static class AvaloniaExtensions
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
    }
}