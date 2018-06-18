using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace PlayAndLearn.Utils
{
    public static class TypeExtensions
    {
        public static string GetFullName(this Type type)
        {
            if (type.IsGenericParameter)
            {
                return type.Name;
            }
            if (!type.IsGenericType)
            {
                return type.FullName;
            }
            var nameParts = type.Namespace
                .Split(".")
                .Concat(new[] { Regex.Replace(type.Name, @"`\d+$", "") });
            var genericArguments = string.Join(", ", type.GetGenericArguments().Select(GetFullName));
            return $"{string.Join(".", nameParts)}<{genericArguments}>";
        }
    } 
}