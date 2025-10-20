using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace RESTween.Server.Handlers
{
    internal interface IReceiveHandler
    {
        Task<bool> HandleReceiveAsync(HttpContext context, ApiDispatcher apiDispatcher);
    }
}
