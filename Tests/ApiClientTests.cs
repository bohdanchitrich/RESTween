using Moq.Protected;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using System.Net;
using RESTween.Handlers;
using RESTween;
using System.Reflection;
using RESTween.Attributes;

namespace Tests
{
    public class ApiClientTests
    {
        [Fact]
        public void HandleGet_ShouldFormCorrectQueryString()
        {
            // Arrange
            var requestHandlerMock = new Mock<IRequestHandler>();
            var httpClient = new HttpClient();
            var apiClient = new ApiClient(requestHandlerMock.Object, httpClient);

            var url = "https://api.example.com/resource";
            var parameters = new object[] { 123, "testValue" };

            // Створення MethodInfo для симуляції Get методу
            MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(SimulatedGetMethod));

            // Act
            HttpRequestMessage request = apiClient.CreateRequest(methodInfo, parameters);

            // Assert
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal($"{url}?param0=123&param1=testValue", request.RequestUri.ToString());
        }

        [Get("https://api.example.com/resource")]
        public void SimulatedGetMethod(int param0, string param1)
        {
            // Цей метод лише симулює реальний метод для тестування
        }
    }
}
