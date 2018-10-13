using System;
using System.Reactive.Linq;
using LanguageExt;

namespace GetIt.Utils
{
    public static class ObservableExtensions
    {
        public static IObservable<TOut> Choose<TIn, TOut>(this IObservable<TIn> obs, Func<TIn, Option<TOut>> fn)
        {
            return obs
                .SelectMany(p => fn(p).Some(Observable.Return).None(Observable.Empty<TOut>));
        }
    }
}