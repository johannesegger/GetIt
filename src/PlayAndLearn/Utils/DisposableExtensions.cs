using System;
using System.Reactive.Disposables;

namespace PlayAndLearn.Utils
{
    internal static class DisposableExtensions
    {
        public static T DisposeWith<T>(this T d, CompositeDisposable cd)
            where T : IDisposable
        {
            cd.Add(d);
            return d;
        }
    }
}