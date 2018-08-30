using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.CodeAnalysis;

namespace MirrorSharp.AspNetCore
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseMirrorSharp(
            this IApplicationBuilder app,
            MirrorSharpOptions options)
        {
            return app.UseMiddleware<Middleware>(options);
        }
    }
}