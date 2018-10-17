using System;
using System.Reactive.Linq;
using Avalonia.Interactivity;

namespace GetIt.Utils
{
    internal static class AvaloniaExtensions
    {
        public static IObservable<TEventArgs> ObserveEvent<TEventArgs>(
            this Interactive element,
            RoutedEvent<TEventArgs> @event)
            where TEventArgs : RoutedEventArgs
        {
            return Observable
                .Create<TEventArgs>(observer => element
                    .AddHandler(
                        @event,
                        new EventHandler<TEventArgs>((s, e) => observer.OnNext(e)),
                        handledEventsToo: true));
        }
    }
}
