using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using Avalonia;
using Elmish.Net.Utils;
using Elmish.Net.VDom;
using LanguageExt;
using static LanguageExt.Prelude;

namespace PlayAndLearn.Utils
{
    public static class VDomNodeExtensions
    {
        public static IVDomNode<T> Attach<T, TProp>(
            this IVDomNode<T> node,
            AvaloniaProperty<TProp> property,
            TProp value,
            IEqualityComparer<TProp> equalityComparer)
            where T : AvaloniaObject
        {
            return node.AddProperty(new VDomNodeAttachedProperty<T, TProp>(property, value, equalityComparer));
        }

        public static IVDomNode<T> Attach<T, TProp>(
            this IVDomNode<T> node,
            AvaloniaProperty<TProp> dependencyProperty,
            TProp value)
            where T : AvaloniaObject
        {
            return node.Attach(dependencyProperty, value, EqualityComparer<TProp>.Default);
        }

        private class VDomNodeAttachedProperty<TParent, TValue>
            : IVDomNodeProperty<TParent, TValue>
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

            public Func<TParent, IDisposable> MergeWith(IVDomNodeProperty property)
            {
                return Optional(property)
                    .TryCast<IVDomNodeProperty<TParent, TValue>>()
                    .Bind(p =>
                        equalityComparer.Equals(p.Value, Value)
                        ? Some(Unit.Default)
                        : None
                    )
                    .Some(_ => new Func<TParent, IDisposable>(o => Disposable.Empty))
                    .None(() => new Func<TParent, IDisposable>(o =>
                    {
                        o.SetValue(avaloniaProperty, Value);
                        return Disposable.Empty;
                    }));
            }

            public bool CanMergeWith(IVDomNodeProperty property)
            {
                return property is VDomNodeAttachedProperty<TParent, TValue> p
                    && Equals(avaloniaProperty, p.avaloniaProperty);
            }
        }
    }
}