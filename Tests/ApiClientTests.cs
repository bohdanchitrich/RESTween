
using RESTween.Attributes;
using NUnit.Framework;
using System.Reflection;
using Moq;
using RESTween;
using RESTween.Handlers;
using NUnit.Framework.Legacy;
namespace Tests
{
    [TestFixture]
    public class ApiClientTests
    {
        [Test]
        public void HandleGet_ShouldFormCorrectQueryString()
        {
            // Arrange
            var requestHandlerMock = new Mock<IRequestHandler>();
            var httpClient = new HttpClient();
            var apiClient = new ApiClient(requestHandlerMock.Object, httpClient);

            var url = "https://api.example.com/resource";
            var arguments = new object[] { 123, "testValue" };

            // Створення MethodInfo для симуляції Get методу
            MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(SimulatedGetMethod));
            var parameters = methodInfo.GetParameters();
            // Act
            HttpRequestMessage request = apiClient.CreateRequest(methodInfo, parameters, arguments);

            // Assert
            ClassicAssert.AreEqual(HttpMethod.Get, request.Method);
            ClassicAssert.AreEqual($"{url}?param0=123&param1=testValue", request.RequestUri.ToString());
        }

        [Get("https://api.example.com/resource")]
        public void SimulatedGetMethod(int param0, string param1)
        {
            // Цей метод лише симулює реальний метод для тестування
        }
    }
}
