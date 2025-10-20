using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RESTween.Server
{
    internal static class ApiDispatcherFactory
    {

        public static ApiDispatcher CreateApiDispatcher<TInterface>(TInterface implementation) where TInterface : class
        {
           return ApiDispatcher.Create(implementation);
        }
    }
}
