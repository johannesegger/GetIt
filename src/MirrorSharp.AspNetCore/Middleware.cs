using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using MirrorSharp.Internal;

namespace MirrorSharp.AspNetCore
{
    internal class Middleware : MiddlewareBase
    {
        private readonly RequestDelegate next;

        public Middleware(MirrorSharpOptions options, RequestDelegate next)
            : base(options)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext ctx)
        {
            if(ctx.Request.Path == new PathString("/mirrorsharp"))
            {
                if(ctx.WebSockets.IsWebSocketRequest)
                {
                    var webSocket = await ctx.WebSockets.AcceptWebSocketAsync();
                    await WebSocketLoopAsync(webSocket, CancellationToken.None);

                }
                else
                {
                    ctx.Response.StatusCode = 400;
                }
            }
            else
            {
                await next(ctx);
            }
        }
    }
}
