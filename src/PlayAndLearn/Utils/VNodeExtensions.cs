using Avalonia;
using Elmish.Net;

namespace PlayAndLearn.Utils
{
    public static class VNodeExtensions
    {
        public static IVNode<T> Attach<T, TProp>(
            this IVNode<T> vNode,
            AttachedProperty<TProp> attachedProperty,
            TProp value)
            where T : AvaloniaObject
        {
            return new VNode<T>(node =>
            {
                var o = vNode.Materialize(node);
                o.Resource.SetValue(attachedProperty, value);
                return o;
            });
        }
    }
}