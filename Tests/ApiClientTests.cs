
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
        const string baseUrl = "https://api.example.com/resource";
        [Get(baseUrl)]
        public void SimulatedGetMethod(int param0, string param1)
        {
        }
        [Test]
        public void HandleGet_ShouldFormCorrectQueryString()
        {
            // Arrange
            var requestHandlerMock = new Mock<IRequestHandler>();
            var httpClient = new HttpClient();
            var apiClient = new ApiClient(requestHandlerMock.Object, httpClient);

            var arguments = new object[] { 123, "testValue" };

            // Створення MethodInfo для симуляції Get методу
            MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(SimulatedGetMethod));
            var parameters = methodInfo.GetParameters();
            // Act
            HttpRequestMessage request = apiClient.CreateRequest(methodInfo, parameters, arguments);

            // Assert
            ClassicAssert.AreEqual(HttpMethod.Get, request.Method);
            ClassicAssert.AreEqual($"{baseUrl}?param0=123&param1=testValue", request.RequestUri.ToString());
        }

        [Get(baseUrl)]
        public void SimulatedGetMethodWithAttribute([Query("quaery")]int param0, string param1)
        {
        }
        [Test]
        public void HandleGet_ShouldFormCorrectQueryStringWithAtribute()
        {
            // Arrange
            var requestHandlerMock = new Mock<IRequestHandler>();
            var httpClient = new HttpClient();
            var apiClient = new ApiClient(requestHandlerMock.Object, httpClient);

            var arguments = new object[] { 123, "testValue" };

            // Створення MethodInfo для симуляції Get методу
            MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(SimulatedGetMethodWithAttribute));
            var parameters = methodInfo.GetParameters();
            // Act
            HttpRequestMessage request = apiClient.CreateRequest(methodInfo, parameters, arguments);

            // Assert
            ClassicAssert.AreEqual(HttpMethod.Get, request.Method);
            ClassicAssert.AreEqual($"{baseUrl}?quaery=123&param1=testValue", request.RequestUri.ToString());
        }






        [Delete(baseUrl)]
        public void SimulatedDeleteMethod(int param0, string param1)
        {
        }
        [Test]
        public void HandleDelete_ShouldFormCorrectQueryString()
        {
            // Arrange
            var requestHandlerMock = new Mock<IRequestHandler>();
            var httpClient = new HttpClient();
            var apiClient = new ApiClient(requestHandlerMock.Object, httpClient);

            var arguments = new object[] { 123, "testValue" };

            // Створення MethodInfo для симуляції Get методу
            MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(SimulatedDeleteMethod));
            var parameters = methodInfo.GetParameters();
            // Act
            HttpRequestMessage request = apiClient.CreateRequest(methodInfo, parameters, arguments);

            // Assert
            ClassicAssert.AreEqual(HttpMethod.Delete, request.Method);
            ClassicAssert.AreEqual($"{baseUrl}?param0=123&param1=testValue", request.RequestUri.ToString());
        }

        [Delete(baseUrl)]
        public void SimulatedDeleteMethodWithAttribute([Query("quaery")] int param0, string param1)
        {
        }
        [Test]
        public void HandleDelete_ShouldFormCorrectQueryStringWithAtribute()
        {
            // Arrange
            var requestHandlerMock = new Mock<IRequestHandler>();
            var httpClient = new HttpClient();
            var apiClient = new ApiClient(requestHandlerMock.Object, httpClient);

            var arguments = new object[] { 123, "testValue" };

            // Створення MethodInfo для симуляції Get методу
            MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(SimulatedDeleteMethodWithAttribute));
            var parameters = methodInfo.GetParameters();
            // Act
            HttpRequestMessage request = apiClient.CreateRequest(methodInfo, parameters, arguments);

            // Assert
            ClassicAssert.AreEqual(HttpMethod.Delete, request.Method);
            ClassicAssert.AreEqual($"{baseUrl}?quaery=123&param1=testValue", request.RequestUri.ToString());
        }



    }
}
