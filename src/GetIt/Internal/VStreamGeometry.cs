using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Avalonia;
using Avalonia.Media;
using Elmish.Net;
using Elmish.Net.Utils;
using Elmish.Net.VDom;
using LanguageExt;
using static LanguageExt.Prelude;
using Unit = System.Reactive.Unit;

namespace GetIt.Internal
{
    internal class VStreamGeometry<TMessage> : IVDomNode<StreamGeometry, TMessage>
    {
        public VStreamGeometry(string data)
        {
            Data = data;
        }

        public IReadOnlyCollection<IVDomNodeProperty> Properties { get; } = ImmutableList<IVDomNodeProperty>.Empty;
        public string Data { get; }

        public IVDomNode<StreamGeometry, TMessage> AddProperty(IVDomNodeProperty<StreamGeometry, TMessage> property)
        {
            throw new NotSupportedException();
        }

        public IVDomNode<StreamGeometry, TMessage> AddSubscription(Func<StreamGeometry, ISub<TMessage>> subscribe)
        {
            throw new NotSupportedException();
        }

        public MergeResult<TMessage> MergeWith(Option<IVDomNode<TMessage>> node)
        {
            return node
                .TryCast<VStreamGeometry<TMessage>>()
                .Bind(p => p.Data == Data ? Some(Unit.Default) : None)
                .Some(_ => new MergeResult<TMessage>(o => (o, Sub.None<TMessage>())))
                .None(() => new MergeResult<TMessage>(o => (Some<object>(PathGeometry.Parse(Data)), Sub.None<TMessage>())));
        }
    }
}
