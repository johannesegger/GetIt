using MirrorSharp.Advanced;
using MirrorSharp.Internal;

namespace MirrorSharp.Extensions
{
    public static class WorkSessionExtensions
    {
        public static GenericLanguageSession<T> TryGetGenericLanguageSession<T>(this IWorkSession session)
        {
            return ((WorkSession)session).LanguageSession as GenericLanguageSession<T>;
        }
    }
}