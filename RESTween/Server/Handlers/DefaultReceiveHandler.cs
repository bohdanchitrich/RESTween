using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RESTween.Server.Handlers
{
    internal class DefaultReceiveHandler : IReceiveHandler
    {
        public Task<bool> HandleReceiveAsync(HttpContext context, ApiDispatcher apiDispatcher)
        {
          return apiDispatcher.Handle(context);
        }
    }
}
