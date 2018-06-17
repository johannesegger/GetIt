using Avalonia;
using Elmish.Net;

namespace PlayAndLearn.Utils
{
    public static class VNodeExtensions
    {
        public static IVNode<T> Attach<T, TProp>(
            this IVNode<T> vNode,
            AvaloniaProperty<TProp> property,
            TProp value)
            where T : AvaloniaObject
        {
            return new VNode<T>(node =>
            {
                var o = vNode.Materialize(node);
                o.Resource.SetValue(property, value);
                return o;
            });
        }
    }
}