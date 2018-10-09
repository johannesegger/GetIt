using System;
using System.Collections.Generic;
using System.Reactive.Disposables;

namespace PlayAndLearn.Utils
{
    public static class DictionaryExtensions
    {
        public static IDisposable AddUndoable<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            dictionary.Add(key, value);
            return Disposable.Create(() => dictionary.Remove(key));
        }
    }
}