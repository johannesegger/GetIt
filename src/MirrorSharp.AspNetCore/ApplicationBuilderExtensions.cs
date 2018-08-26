using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.CodeAnalysis;
using MirrorSharp.Extensions;

namespace MirrorSharp.AspNetCore
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseMirrorSharp(
            this IApplicationBuilder app,
            MirrorSharpOptions options = null)
        {
            return app.UseMirrorSharp<object>(null, options);
        }

        public static IApplicationBuilder UseMirrorSharp<TSessionData>(
            this IApplicationBuilder app,
            GenericLanguage<TSessionData> language,
            MirrorSharpOptions options = null)
        {
            options = options ?? new MirrorSharpOptions();
            if (language != null)
            {
                options.Languages.Clear();
                // Name must be `LanguageNames.CSharp`, because it's MirrorSharp's default language
                options.Languages.Add(LanguageNames.CSharp, () => language);
            }
            return app.UseMiddleware<Middleware>(options);
        }
    }
}