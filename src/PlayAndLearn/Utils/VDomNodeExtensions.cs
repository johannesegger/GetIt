using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using Avalonia;
using Elmish.Net;
using Elmish.Net.Utils;
using Elmish.Net.VDom;
using LanguageExt;
using static LanguageExt.Prelude;

namespace PlayAndLearn.Utils
{
    public static class VDomNodeExtensions
    {
        public static IVDomNode<T, TMessage> Attach<T, TMessage, TProp>(
            this IVDomNode<T, TMessage> node,
            AvaloniaProperty<TProp> property,
            TProp value,
            IEqualityComparer<TProp> equalityComparer)
            where T : AvaloniaObject
        {
            return node.AddProperty(new VDomNodeAttachedProperty<T, TMessage, TProp>(property, value, equalityComparer));
        }

        public static IVDomNode<T, TMessage> Attach<T, TMessage, TProp>(
            this IVDomNode<T, TMessage> node,
            AvaloniaProperty<TProp> dependencyProperty,
            TProp value)
            where T : AvaloniaObject
        {
            return node.Attach(dependencyProperty, value, EqualityComparer<TProp>.Default);
        }

        private class VDomNodeAttachedProperty<TParent, TMessage, TValue>
            : IVDomNodeProperty<TParent, TMessage, TValue>
            where TParent : AvaloniaObject
        {
            private readonly AvaloniaProperty<TValue> avaloniaProperty;
            private readonly IEqualityComparer<TValue> equalityComparer;

            public VDomNodeAttachedProperty(
                AvaloniaProperty<TValue> avaloniaProperty,
                TValue value,
                IEqualityComparer<TValue> equalityComparer)
            {
                this.avaloniaProperty = avaloniaProperty;
                Value = value;
                this.equalityComparer = equalityComparer;
            }

            public TValue Value { get; }

            public Func<TParent, ISub<TMessage>> MergeWith(IVDomNodeProperty property)
            {
                return Optional(property)
                    .TryCast<IVDomNodeProperty<TParent, TMessage, TValue>>()
                    .Bind(p =>
                        equalityComparer.Equals(p.Value, Value)
                        ? Some(Unit.Default)
                        : None
                    )
                    .Some(_ => new Func<TParent, ISub<TMessage>>(o => Sub.None<TMessage>()))
                    .None(() => new Func<TParent, ISub<TMessage>>(o =>
                    {
                        o.SetValue(avaloniaProperty, Value);
                        return Sub.None<TMessage>();
                    }));
            }

            public bool CanMergeWith(IVDomNodeProperty property)
            {
                return property is VDomNodeAttachedProperty<TParent, TMessage, TValue> p
                    && Equals(avaloniaProperty, p.avaloniaProperty);
            }
        }
    }
}