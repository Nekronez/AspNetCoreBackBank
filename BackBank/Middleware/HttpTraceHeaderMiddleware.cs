using BackBank.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackBank.Middleware
{
    /// <summary>
    /// Middleware used to add Http Header to response for trace logs
    /// </summary>
    public class HttpTraceHeaderMiddleware
    {
        private const string TraceHeaderName = "X-Trace-Id";

        private readonly RequestDelegate _next;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="next"></param>
        public HttpTraceHeaderMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Adds X-Trace-Id Http Header to response
        /// </summary>
        /// <param name = "context" ></ param >
        /// < returns ></ returns >
        public async Task Invoke(HttpContext context, TraceIdentifierProviderExtension traceIdentifierProviderExtension)
        {
            var traceHeader = context.Request.Headers[TraceHeaderName];

            context.TraceIdentifier = traceHeader == StringValues.Empty ? TraceIdentifierProvider.GenerateTraceId() : traceHeader[0];

            using (traceIdentifierProviderExtension.SetTraceIdScope(context.TraceIdentifier))
            {
                context.Response.Headers.Append(TraceHeaderName, context.TraceIdentifier);
                await _next(context);
            }
        }
    }
}
