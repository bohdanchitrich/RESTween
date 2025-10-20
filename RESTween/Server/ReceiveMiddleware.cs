using Microsoft.AspNetCore.Http;
using RESTween.Server.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RESTween.Server
{
    internal class ReceiveMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IReceiveHandler _receiver;
        private readonly IEnumerable<ApiDispatcher> _dispatchers;

        public ReceiveMiddleware(
            RequestDelegate next,
            IReceiveHandler receiver,
            IEnumerable<ApiDispatcher> dispatchers)
        {
            _next = next;
            _receiver = receiver;
            _dispatchers = dispatchers;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            foreach (var dispatcher in _dispatchers)
            {
                var handled = await _receiver.HandleReceiveAsync(context, dispatcher);
                if (handled)
                    return; 
            }

            await _next(context);
        }
    }

}
