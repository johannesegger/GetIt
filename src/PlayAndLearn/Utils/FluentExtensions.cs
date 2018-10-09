using System;
using System.Reactive.Disposables;

namespace PlayAndLearn.Utils
{
    public static class FluentExtensions
    {
        public static T Do<T>(this T obj, Action<T> setter)
        {
            setter(obj);
            return obj;
        }

        public static TObj Subscribe<TObj, TValue>(this TObj obj, IObservable<TValue> value, Action<TObj, TValue> setter, CompositeDisposable d)
        {
            value
                .Subscribe(v => setter(obj, v))
                .DisposeWith(d);
            return obj;
        }
    }
}