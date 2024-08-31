
using RESTween.Attributes;
using NUnit.Framework;
using System.Reflection;
using Moq;
using RESTween;
using RESTween.Handlers;
using NUnit.Framework.Legacy;
using System.Text.Json;
using System.Net.Http;
namespace Tests
{
    [TestFixture]
    public class ApiClientTests
    {
        const string baseUrl = "https://api.example.com/resource";
        ApiClient _apiClient;

        [SetUp]
        public void SetUp()
        {
            var requestHandlerMock = new Mock<IRequestHandler>();
            var httpClient = new HttpClient();
            _apiClient = new ApiClient(requestHandlerMock.Object, httpClient);
        }
        #region GetTests
        [Get(baseUrl)]
        public void GetEmpty()
        {
        }
        [Test]
        public void GetEmptyTest()
        {
            // Arrange
            var arguments = new object[0];
            MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(GetEmpty));
            var parameters = methodInfo.GetParameters();
            // Act
            HttpRequestMessage request = _apiClient.CreateRequest(methodInfo, parameters, arguments);

            // ClassicAssert
            ClassicAssert.AreEqual(HttpMethod.Get, request.Method);
            ClassicAssert.AreEqual($"{baseUrl}", request.RequestUri.ToString());
        }

        [Get(baseUrl)]
        public void GetQuery(int parameter)
        {
        }
        [Test]
        public void GetQueryTest()
        {
            // Arrange
            var arguments = new object[] { 123};
            MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(GetQuery));
            var parameters = methodInfo.GetParameters();
            // Act
            HttpRequestMessage request = _apiClient.CreateRequest(methodInfo, parameters, arguments);
            // ClassicAssert
            ClassicAssert.AreEqual(HttpMethod.Get, request.Method);
            ClassicAssert.AreEqual($"{baseUrl}?parameter=123", request.RequestUri.ToString());

        }

        [Get(baseUrl)]
        public void GetQueryTwo(int parameter1, string parameter2)
        {
        }
        [Test]
        public void GetQueryTwoTest()
        {
            // Arrange
            var arguments = new object[] { 123 ,"testParameter"};
            MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(GetQueryTwo));
            var parameters = methodInfo.GetParameters();
            // Act
            HttpRequestMessage request = _apiClient.CreateRequest(methodInfo, parameters, arguments);
            // ClassicAssert
            ClassicAssert.AreEqual(HttpMethod.Get, request.Method);
            ClassicAssert.AreEqual($"{baseUrl}?parameter1=123&parameter2=testParameter", request.RequestUri.ToString());

        }



        [Get(baseUrl)]
        public void GetQueryAttribute([Query("parameterQuery")]int parameter)
        {
        }
        [Test]
        public void GetQueryAttributeTest()
        {
            // Arrange
            var arguments = new object[] { 123 };
            MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(GetQueryAttribute));
            var parameters = methodInfo.GetParameters();
            // Act
            HttpRequestMessage request = _apiClient.CreateRequest(methodInfo, parameters, arguments);
            // ClassicAssert
            ClassicAssert.AreEqual(HttpMethod.Get, request.Method);
            ClassicAssert.AreEqual($"{baseUrl}?parameterQuery=123", request.RequestUri.ToString());

        }

        [Get(baseUrl)]
        public void GetQueryTwoAttribute([Query("queryAttribute1")]int parameter1, [Query("queryAttribute2")] string parameter2)
        {
        }
        [Test]
        public void GetQueryTwoAttributeTest()
        {
            // Arrange
            var arguments = new object[] { 123, "testParameter" };
            MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(GetQueryTwoAttribute));
            var parameters = methodInfo.GetParameters();
            // Act
            HttpRequestMessage request = _apiClient.CreateRequest(methodInfo, parameters, arguments);
            // ClassicAssert
            ClassicAssert.AreEqual(HttpMethod.Get, request.Method);
            ClassicAssert.AreEqual($"{baseUrl}?queryAttribute1=123&queryAttribute2=testParameter", request.RequestUri.ToString());

        }

        [Get(baseUrl)]
        public void GetQueryTwoParametersOneWithAttribute( int parameter1, [Query("queryAttribute2")] string parameter2)
        {
        }
        [Test]
        public void GetQueryTwoParametersOneWithAttributeTest()
        {
            // Arrange
            var arguments = new object[] { 123, "testParameter" };
            MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(GetQueryTwoParametersOneWithAttribute));
            var parameters = methodInfo.GetParameters();
            // Act
            HttpRequestMessage request = _apiClient.CreateRequest(methodInfo, parameters, arguments);
            // ClassicAssert
            ClassicAssert.AreEqual(HttpMethod.Get, request.Method);
            ClassicAssert.AreEqual($"{baseUrl}?parameter1=123&queryAttribute2=testParameter", request.RequestUri.ToString());

        }




        [Get(baseUrl+"/{parameter}")]
        public void GetUrlParameter(int parameter)
        {
        }
        [Test]
        public void GetUrlParameterTest()
        {
            // Arrange
            var arguments = new object[] { 123 };
            MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(GetUrlParameter));
            var parameters = methodInfo.GetParameters();
            // Act
            HttpRequestMessage request = _apiClient.CreateRequest(methodInfo, parameters, arguments);
            // ClassicAssert
            ClassicAssert.AreEqual(HttpMethod.Get, request.Method);
            ClassicAssert.AreEqual($"{baseUrl}/123", request.RequestUri.ToString());

        }



        [Get(baseUrl + "/{parameter1}/{parameter2}")]
        public void GetTwoUrlParameter(int parameter1, int parameter2)
        {
        }
        [Test]
        public void GetTwoUrlParameterTest()
        {
            // Arrange
            var arguments = new object[] { 1, 2 };
            MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(GetTwoUrlParameter));
            var parameters = methodInfo.GetParameters();
            // Act
            HttpRequestMessage request = _apiClient.CreateRequest(methodInfo, parameters, arguments);
            // ClassicAssert
            ClassicAssert.AreEqual(HttpMethod.Get, request.Method);
            ClassicAssert.AreEqual($"{baseUrl}/1/2", request.RequestUri.ToString());
        }



        [Get(baseUrl + "/{parameter2}/{parameter1}")]
        public void GetTwoReverseUrlParameter(int parameter1,int parameter2)
        {
        }
        [Test]
        public void GetTwoReversUrlParameterTest()
        {
            // Arrange
            var arguments = new object[] { 1,2 };
            MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(GetTwoReverseUrlParameter));
            var parameters = methodInfo.GetParameters();
            // Act
            HttpRequestMessage request = _apiClient.CreateRequest(methodInfo, parameters, arguments);
            // ClassicAssert
            ClassicAssert.AreEqual(HttpMethod.Get, request.Method);
            ClassicAssert.AreEqual($"{baseUrl}/2/1", request.RequestUri.ToString());
        }



        [Get(baseUrl + "/{parameter}")]
        public void GetUrlParameterWithQuery(int parameter,string query)
        {
        }
        [Test]
        public void GetUrlParameterWithQueryTest()
        {
            // Arrange
            var arguments = new object[] { 123,"queryTest" };
            MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(GetUrlParameterWithQuery));
            var parameters = methodInfo.GetParameters();
            // Act
            HttpRequestMessage request = _apiClient.CreateRequest(methodInfo, parameters, arguments);
            // ClassicAssert
            ClassicAssert.AreEqual(HttpMethod.Get, request.Method);
            ClassicAssert.AreEqual($"{baseUrl}/123?query=queryTest", request.RequestUri.ToString());

        }


        [Get(baseUrl + "/{parameter}")]
        public void GetReversUrlParameterWithQuery(string query,int parameter)
        {
        }
        [Test]
        public void GetReversUrlParameterWithQueryTest()
        {
            // Arrange
            var arguments = new object[] { "queryTest",123};
            MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(GetReversUrlParameterWithQuery));
            var parameters = methodInfo.GetParameters();
            // Act
            HttpRequestMessage request = _apiClient.CreateRequest(methodInfo, parameters, arguments);
            // ClassicAssert
            ClassicAssert.AreEqual(HttpMethod.Get, request.Method);
            ClassicAssert.AreEqual($"{baseUrl}/123?query=queryTest", request.RequestUri.ToString());

        }


        [Get(baseUrl + "/{parameter}")]
        public void GetUrlParameterWithTwoQuery(string query2,int parameter, string query1)
        {
        }
        [Test]
        public void GetUrlParameterWithTwoQueryTest()
        {
            // Arrange
            var arguments = new object[] {"query2Test" ,123, "query1Test" };
            MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(GetUrlParameterWithTwoQuery));
            var parameters = methodInfo.GetParameters();
            // Act
            HttpRequestMessage request = _apiClient.CreateRequest(methodInfo, parameters, arguments);
            // ClassicAssert
            ClassicAssert.AreEqual(HttpMethod.Get, request.Method);
            ClassicAssert.AreEqual($"{baseUrl}/123?query2=query2Test&query1=query1Test", request.RequestUri.ToString());

        }

        [Get(baseUrl + "/{parameter}")]
        public void GetUrlParameterWithTwoQueryAndWithAttribute([Query("queryAttribute")]string query2, int parameter, string query1)
        {
        }
        [Test]
        public void GetUrlParameterWithTwoQueryAndWithAttributeTest()
        {
            // Arrange
            var arguments = new object[] { "query2Test", 123, "query1Test" };
            MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(GetUrlParameterWithTwoQueryAndWithAttribute));
            var parameters = methodInfo.GetParameters();
            // Act
            HttpRequestMessage request = _apiClient.CreateRequest(methodInfo, parameters, arguments);
            // ClassicAssert
            ClassicAssert.AreEqual(HttpMethod.Get, request.Method);
            ClassicAssert.AreEqual($"{baseUrl}/123?queryAttribute=query2Test&query1=query1Test", request.RequestUri.ToString());

        }

        #endregion
        #region PostTests
        [Post(baseUrl)]
        public void PostEmpty()
        {
        }
        [Test]
        public void PostEmptyTest()
        {
            // Arrange
            var arguments = new object[0];
            MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(PostEmpty));
            var parameters = methodInfo.GetParameters();
            // Act
            HttpRequestMessage request = _apiClient.CreateRequest(methodInfo, parameters, arguments);

            // ClassicAssert
            ClassicAssert.AreEqual(HttpMethod.Post, request.Method);
            ClassicAssert.AreEqual($"{baseUrl}", request.RequestUri.ToString());
        }



        [Post(baseUrl)]
        public void PostBody([Body] object body)
        {
        }

        [Test]
        public void PostBodyTest()
        {
            // Arrange
          

            var bodyContent = new { Name = "John", Age = 30 };
            var arguments = new object[] { bodyContent};

            MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(PostBody));
            var parameters = methodInfo.GetParameters();

            // Act
            HttpRequestMessage request = _apiClient.CreateRequest(methodInfo, parameters, arguments);

            // ClassicAssert
            ClassicAssert.AreEqual(HttpMethod.Post, request.Method);
            ClassicAssert.AreEqual(baseUrl, request.RequestUri.ToString());
            var jsonContent = request.Content.ReadAsStringAsync().Result;
            ClassicAssert.AreEqual(JsonSerializer.Serialize(bodyContent), jsonContent);
        }





        [Post(baseUrl)]
        public void PostBodyWithQuery([Body] object body, int param1)
        {
        }

        [Test]
        public void PostBodyWithQueryTest()
        {
            // Arrange


            var bodyContent = new { Name = "John", Age = 30 };
            var arguments = new object[] { bodyContent, 123 };

            MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(PostBodyWithQuery));
            var parameters = methodInfo.GetParameters();

            // Act
            HttpRequestMessage request = _apiClient.CreateRequest(methodInfo, parameters, arguments);

            // ClassicAssert
            ClassicAssert.AreEqual(HttpMethod.Post, request.Method);
            ClassicAssert.AreEqual(baseUrl + "?param1=123", request.RequestUri.ToString());
            var jsonContent = request.Content.ReadAsStringAsync().Result;
            ClassicAssert.AreEqual(JsonSerializer.Serialize(bodyContent), jsonContent);
        }
        [Post(baseUrl+"/{param1}")]
        public void PostBodyWithUrlParameter([Body] object body, int param1)
        {
        }

        [Test]
        public void PostBodyWithUrlParameterTest()
        {
            // Arrange


            var bodyContent = new { Name = "John", Age = 30 };
            var arguments = new object[] { bodyContent, 123 };

            MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(PostBodyWithUrlParameter));
            var parameters = methodInfo.GetParameters();

            // Act
            HttpRequestMessage request = _apiClient.CreateRequest(methodInfo, parameters, arguments);

            // ClassicAssert
            ClassicAssert.AreEqual(HttpMethod.Post, request.Method);
            ClassicAssert.AreEqual(baseUrl + "/123", request.RequestUri.ToString());
            var jsonContent = request.Content.ReadAsStringAsync().Result;
            ClassicAssert.AreEqual(JsonSerializer.Serialize(bodyContent), jsonContent);
        }



        [Post(baseUrl + "/{param2}")]
        public void PostBodyWithUrlParameterAndQuery([Query("queryParam")]string param1,[Body] object body, int param2)
        {
        }

        [Test]
        public void PostBodyWithUrlParameterAndQueryTest()
        {
            // Arrange


            var bodyContent = new { Name = "John", Age = 30 };
            var arguments = new object[] { "query",bodyContent, 123 };

            MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(PostBodyWithUrlParameterAndQuery));
            var parameters = methodInfo.GetParameters();

            // Act
            HttpRequestMessage request = _apiClient.CreateRequest(methodInfo, parameters, arguments);

            // ClassicAssert
            ClassicAssert.AreEqual(HttpMethod.Post, request.Method);
            ClassicAssert.AreEqual(baseUrl + "/123?queryParam=query", request.RequestUri.ToString());
            var jsonContent = request.Content.ReadAsStringAsync().Result;
            ClassicAssert.AreEqual(JsonSerializer.Serialize(bodyContent), jsonContent);
        }


        [Post(baseUrl)]
        public void PostQuery(int parameter)
        {
        }
        [Test]
        public void PostQueryTest()
        {
            // Arrange
            var arguments = new object[] { 123 };
            MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(PostQuery));
            var parameters = methodInfo.GetParameters();
            // Act
            HttpRequestMessage request = _apiClient.CreateRequest(methodInfo, parameters, arguments);
            // ClassicAssert
            ClassicAssert.AreEqual(HttpMethod.Post, request.Method);
            ClassicAssert.AreEqual($"{baseUrl}?parameter=123", request.RequestUri.ToString());

        }

        [Post(baseUrl)]
        public void PostQueryTwo(int parameter1, string parameter2)
        {
        }
        [Test]
        public void PostQueryTwoTest()
        {
            // Arrange
            var arguments = new object[] { 123, "testParameter" };
            MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(PostQueryTwo));
            var parameters = methodInfo.GetParameters();
            // Act
            HttpRequestMessage request = _apiClient.CreateRequest(methodInfo, parameters, arguments);
            // ClassicAssert
            ClassicAssert.AreEqual(HttpMethod.Post, request.Method);
            ClassicAssert.AreEqual($"{baseUrl}?parameter1=123&parameter2=testParameter", request.RequestUri.ToString());

        }



        [Post(baseUrl)]
        public void PostQueryAttribute([Query("parameterQuery")] int parameter)
        {
        }
        [Test]
        public void PostQueryAttributeTest()
        {
            // Arrange
            var arguments = new object[] { 123 };
            MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(PostQueryAttribute));
            var parameters = methodInfo.GetParameters();
            // Act
            HttpRequestMessage request = _apiClient.CreateRequest(methodInfo, parameters, arguments);
            // ClassicAssert
            ClassicAssert.AreEqual(HttpMethod.Post, request.Method);
            ClassicAssert.AreEqual($"{baseUrl}?parameterQuery=123", request.RequestUri.ToString());

        }

        [Post(baseUrl)]
        public void PostQueryTwoAttribute([Query("queryAttribute1")] int parameter1, [Query("queryAttribute2")] string parameter2)
        {
        }
        [Test]
        public void PostQueryTwoAttributeTest()
        {
            // Arrange
            var arguments = new object[] { 123, "testParameter" };
            MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(PostQueryTwoAttribute));
            var parameters = methodInfo.GetParameters();
            // Act
            HttpRequestMessage request = _apiClient.CreateRequest(methodInfo, parameters, arguments);
            // ClassicAssert
            ClassicAssert.AreEqual(HttpMethod.Post, request.Method);
            ClassicAssert.AreEqual($"{baseUrl}?queryAttribute1=123&queryAttribute2=testParameter", request.RequestUri.ToString());

        }

        [Post(baseUrl)]
        public void PostQueryTwoParametersOneWithAttribute(int parameter1, [Query("queryAttribute2")] string parameter2)
        {
        }
        [Test]
        public void PostQueryTwoParametersOneWithAttributeTest()
        {
            // Arrange
            var arguments = new object[] { 123, "testParameter" };
            MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(PostQueryTwoParametersOneWithAttribute));
            var parameters = methodInfo.GetParameters();
            // Act
            HttpRequestMessage request = _apiClient.CreateRequest(methodInfo, parameters, arguments);
            // ClassicAssert
            ClassicAssert.AreEqual(HttpMethod.Post, request.Method);
            ClassicAssert.AreEqual($"{baseUrl}?parameter1=123&queryAttribute2=testParameter", request.RequestUri.ToString());

        }




        [Post(baseUrl + "/{parameter}")]
        public void PostUrlParameter(int parameter)
        {
        }
        [Test]
        public void PostUrlParameterTest()
        {
            // Arrange
            var arguments = new object[] { 123 };
            MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(PostUrlParameter));
            var parameters = methodInfo.GetParameters();
            // Act
            HttpRequestMessage request = _apiClient.CreateRequest(methodInfo, parameters, arguments);
            // ClassicAssert
            ClassicAssert.AreEqual(HttpMethod.Post, request.Method);
            ClassicAssert.AreEqual($"{baseUrl}/123", request.RequestUri.ToString());

        }



        [Post(baseUrl + "/{parameter1}/{parameter2}")]
        public void PostTwoUrlParameter(int parameter1, int parameter2)
        {
        }
        [Test]
        public void PostTwoUrlParameterTest()
        {
            // Arrange
            var arguments = new object[] { 1, 2 };
            MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(PostTwoUrlParameter));
            var parameters = methodInfo.GetParameters();
            // Act
            HttpRequestMessage request = _apiClient.CreateRequest(methodInfo, parameters, arguments);
            // ClassicAssert
            ClassicAssert.AreEqual(HttpMethod.Post, request.Method);
            ClassicAssert.AreEqual($"{baseUrl}/1/2", request.RequestUri.ToString());
        }



        [Post(baseUrl + "/{parameter2}/{parameter1}")]
        public void PostTwoReverseUrlParameter(int parameter1, int parameter2)
        {
        }
        [Test]
        public void PostTwoReversUrlParameterTest()
        {
            // Arrange
            var arguments = new object[] { 1, 2 };
            MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(PostTwoReverseUrlParameter));
            var parameters = methodInfo.GetParameters();
            // Act
            HttpRequestMessage request = _apiClient.CreateRequest(methodInfo, parameters, arguments);
            // ClassicAssert
            ClassicAssert.AreEqual(HttpMethod.Post, request.Method);
            ClassicAssert.AreEqual($"{baseUrl}/2/1", request.RequestUri.ToString());
        }



        [Post(baseUrl + "/{parameter}")]
        public void PostUrlParameterWithQuery(int parameter, string query)
        {
        }
        [Test]
        public void PostUrlParameterWithQueryTest()
        {
            // Arrange
            var arguments = new object[] { 123, "queryTest" };
            MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(PostUrlParameterWithQuery));
            var parameters = methodInfo.GetParameters();
            // Act
            HttpRequestMessage request = _apiClient.CreateRequest(methodInfo, parameters, arguments);
            // ClassicAssert
            ClassicAssert.AreEqual(HttpMethod.Post, request.Method);
            ClassicAssert.AreEqual($"{baseUrl}/123?query=queryTest", request.RequestUri.ToString());

        }


        [Post(baseUrl + "/{parameter}")]
        public void PostReversUrlParameterWithQuery(string query, int parameter)
        {
        }
        [Test]
        public void PostReversUrlParameterWithQueryTest()
        {
            // Arrange
            var arguments = new object[] { "queryTest", 123 };
            MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(PostReversUrlParameterWithQuery));
            var parameters = methodInfo.GetParameters();
            // Act
            HttpRequestMessage request = _apiClient.CreateRequest(methodInfo, parameters, arguments);
            // ClassicAssert
            ClassicAssert.AreEqual(HttpMethod.Post, request.Method);
            ClassicAssert.AreEqual($"{baseUrl}/123?query=queryTest", request.RequestUri.ToString());

        }


        [Post(baseUrl + "/{parameter}")]
        public void PostUrlParameterWithTwoQuery(string query2, int parameter, string query1)
        {
        }
        [Test]
        public void PostUrlParameterWithTwoQueryTest()
        {
            // Arrange
            var arguments = new object[] { "query2Test", 123, "query1Test" };
            MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(PostUrlParameterWithTwoQuery));
            var parameters = methodInfo.GetParameters();
            // Act
            HttpRequestMessage request = _apiClient.CreateRequest(methodInfo, parameters, arguments);
            // ClassicAssert
            ClassicAssert.AreEqual(HttpMethod.Post, request.Method);
            ClassicAssert.AreEqual($"{baseUrl}/123?query2=query2Test&query1=query1Test", request.RequestUri.ToString());

        }

        [Post(baseUrl + "/{parameter}")]
        public void PostUrlParameterWithTwoQueryAndWithAttribute([Query("queryAttribute")] string query2, int parameter, string query1)
        {
        }
        [Test]
        public void PostUrlParameterWithTwoQueryAndWithAttributeTest()
        {
            // Arrange
            var arguments = new object[] { "query2Test", 123, "query1Test" };
            MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(PostUrlParameterWithTwoQueryAndWithAttribute));
            var parameters = methodInfo.GetParameters();
            // Act
            HttpRequestMessage request = _apiClient.CreateRequest(methodInfo, parameters, arguments);
            // ClassicAssert
            ClassicAssert.AreEqual(HttpMethod.Post, request.Method);
            ClassicAssert.AreEqual($"{baseUrl}/123?queryAttribute=query2Test&query1=query1Test", request.RequestUri.ToString());

        }



        #endregion
        //    [Post(baseUrl)]
        //    public void SimulatedPostMethod([Body] object body, int param1)
        //    {
        //    }

        //    [Test]
        //    public void HandlePost_ShouldCreateCorrectRequestWithBody()
        //    {
        //        // Arrange
        //        var requestHandlerMock = new Mock<IRequestHandler>();
        //        var httpClient = new HttpClient();
        //        var apiClient = new ApiClient(requestHandlerMock.Object, httpClient);

        //        var bodyContent = new { Name = "John", Age = 30 };
        //        var arguments = new object[] { bodyContent, 123 };

        //        MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(SimulatedPostMethod));
        //        var parameters = methodInfo.GetParameters();

        //        // Act
        //        HttpRequestMessage request = apiClient.CreateRequest(methodInfo, parameters, arguments);

        //        // ClassicAssert
        //        ClassicAssert.AreEqual(HttpMethod.Post, request.Method);
        //        ClassicAssert.AreEqual(baseUrl + "?param1=123", request.RequestUri.ToString());
        //        var jsonContent = request.Content.ReadAsStringAsync().Result;
        //        ClassicAssert.AreEqual(JsonSerializer.Serialize(bodyContent), jsonContent);
        //    }

        //    [Post(baseUrl)]
        //    public void SimulatedPostMethodWithoutBody(int param1, [Query("extraParam")] string param2)
        //    {
        //    }

        //    [Test]
        //    public void HandlePost_ShouldCreateCorrectRequestWithoutBody()
        //    {
        //        // Arrange
        //        var requestHandlerMock = new Mock<IRequestHandler>();
        //        var httpClient = new HttpClient();
        //        var apiClient = new ApiClient(requestHandlerMock.Object, httpClient);

        //        var arguments = new object[] { 123, "test" };

        //        MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(SimulatedPostMethodWithoutBody));
        //        var parameters = methodInfo.GetParameters();

        //        // Act
        //        HttpRequestMessage request = apiClient.CreateRequest(methodInfo, parameters, arguments);

        //        // ClassicAssert
        //        ClassicAssert.AreEqual(HttpMethod.Post, request.Method);
        //        ClassicAssert.AreEqual($"{baseUrl}?param1=123&extraParam=test", request.RequestUri.ToString());
        //        ClassicAssert.IsNull(request.Content); // Content should be null since no body is specified
        //    }

        //    [Put(baseUrl)]
        //    public void SimulatedPutMethod([Body] object body)
        //    {
        //    }

        //    [Test]
        //    public void HandlePut_ShouldCreateCorrectRequestWithBody()
        //    {
        //        // Arrange
        //        var requestHandlerMock = new Mock<IRequestHandler>();
        //        var httpClient = new HttpClient();
        //        var apiClient = new ApiClient(requestHandlerMock.Object, httpClient);

        //        var bodyContent = new { Name = "Jane", Age = 25 };
        //        var arguments = new object[] { bodyContent };

        //        MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(SimulatedPutMethod));
        //        var parameters = methodInfo.GetParameters();

        //        // Act
        //        HttpRequestMessage request = apiClient.CreateRequest(methodInfo, parameters, arguments);

        //        // ClassicAssert
        //        ClassicAssert.AreEqual(HttpMethod.Put, request.Method);
        //        ClassicAssert.AreEqual(baseUrl, request.RequestUri.ToString());
        //        var jsonContent = request.Content.ReadAsStringAsync().Result;
        //        ClassicAssert.AreEqual(JsonSerializer.Serialize(bodyContent), jsonContent);
        //    }

        //    [Put(baseUrl)]
        //    public void SimulatedPutMethodWithoutBody(int param1, [Query("extraParam")] string param2)
        //    {
        //    }

        //    [Test]
        //    public void HandlePut_ShouldCreateCorrectRequestWithoutBody()
        //    {
        //        // Arrange
        //        var requestHandlerMock = new Mock<IRequestHandler>();
        //        var httpClient = new HttpClient();
        //        var apiClient = new ApiClient(requestHandlerMock.Object, httpClient);

        //        var arguments = new object[] { 456, "anotherTest" };

        //        MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(SimulatedPutMethodWithoutBody));
        //        var parameters = methodInfo.GetParameters();

        //        // Act
        //        HttpRequestMessage request = apiClient.CreateRequest(methodInfo, parameters, arguments);

        //        // ClassicAssert
        //        ClassicAssert.AreEqual(HttpMethod.Put, request.Method);
        //        ClassicAssert.AreEqual($"{baseUrl}?param1=456&extraParam=anotherTest", request.RequestUri.ToString());
        //        ClassicAssert.IsNull(request.Content); // Content should be null since no body is specified
        //    }

        //    [Post(baseUrl)]
        //    public void SimulatedPostMethodWithMultipleQueryParams([Body] object body, int param1, [Query("customName")] string param2)
        //    {
        //    }

        //    [Test]
        //    public void HandlePost_ShouldHandleMultipleQueryParamsCorrectly()
        //    {
        //        // Arrange
        //        var requestHandlerMock = new Mock<IRequestHandler>();
        //        var httpClient = new HttpClient();
        //        var apiClient = new ApiClient(requestHandlerMock.Object, httpClient);

        //        var bodyContent = new { Name = "Doe", Age = 40 };
        //        var arguments = new object[] { bodyContent, 789, "customValue" };

        //        MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(SimulatedPostMethodWithMultipleQueryParams));
        //        var parameters = methodInfo.GetParameters();

        //        // Act
        //        HttpRequestMessage request = apiClient.CreateRequest(methodInfo, parameters, arguments);

        //        // ClassicAssert
        //        ClassicAssert.AreEqual(HttpMethod.Post, request.Method);
        //        ClassicAssert.AreEqual($"{baseUrl}?param1=789&customName=customValue", request.RequestUri.ToString());
        //        var jsonContent = request.Content.ReadAsStringAsync().Result;
        //        ClassicAssert.AreEqual(JsonSerializer.Serialize(bodyContent), jsonContent);
        //    }


        //    // Метод з маршрутом, що містить параметр {id}
        //    [Get("/users/{id}")]
        //    public Task GetUserAsync(int id)
        //    {
        //        return Task.CompletedTask;
        //    }

        //    [Test]
        //    public void ReplaceRouteParameters_ShouldReplaceRouteParameterInUrl()
        //    {
        //        // Arrange
        //        var requestHandlerMock = new Mock<IRequestHandler>();
        //        var httpClient = new HttpClient();
        //        var apiClient = new ApiClient(requestHandlerMock.Object, httpClient);

        //        var arguments = new object[] { 42 };
        //        MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(GetUserAsync));
        //        var parameters = methodInfo.GetParameters();

        //        // Act
        //        var request = apiClient.CreateRequest(methodInfo, parameters, arguments);

        //        // ClassicAssert
        //        ClassicAssert.AreEqual(HttpMethod.Get, request.Method);
        //        ClassicAssert.AreEqual($"{baseUrl}/users/42", request.RequestUri.ToString());
        //    }

        //    // Метод з маршрутом, що містить декілька параметрів {id} і {action}
        //    [Get("/users/{id}/{action}")]
        //    public Task GetUserActionAsync(int id, string action)
        //    {
        //        return Task.CompletedTask;
        //    }

        //    [Test]
        //    public void ReplaceRouteParameters_ShouldReplaceMultipleRouteParametersInUrl()
        //    {
        //        // Arrange
        //        var requestHandlerMock = new Mock<IRequestHandler>();
        //        var httpClient = new HttpClient();
        //        var apiClient = new ApiClient(requestHandlerMock.Object, httpClient);

        //        var arguments = new object[] { 42, "edit" };
        //        MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(GetUserActionAsync));
        //        var parameters = methodInfo.GetParameters();

        //        // Act
        //        var request = apiClient.CreateRequest(methodInfo, parameters, arguments);

        //        // ClassicAssert
        //        ClassicAssert.AreEqual(HttpMethod.Get, request.Method);
        //        ClassicAssert.AreEqual($"{baseUrl}/users/42/edit", request.RequestUri.ToString());
        //    }

        //    // Метод з маршрутом, що містить параметр і query параметри
        //    [Get("/users/{id}")]
        //    public Task GetUserWithQueryAsync(int id, [Query("filter")] string filter)
        //    {
        //        return Task.CompletedTask;
        //    }

        //    [Test]
        //    public void ReplaceRouteParameters_ShouldReplaceRouteParameterAndAppendQuery()
        //    {
        //        // Arrange
        //        var requestHandlerMock = new Mock<IRequestHandler>();
        //        var httpClient = new HttpClient();
        //        var apiClient = new ApiClient(requestHandlerMock.Object, httpClient);

        //        var arguments = new object[] { 42, "active" };
        //        MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(GetUserWithQueryAsync));
        //        var parameters = methodInfo.GetParameters();

        //        // Act
        //        var request = apiClient.CreateRequest(methodInfo, parameters, arguments);

        //        // ClassicAssert
        //        ClassicAssert.AreEqual(HttpMethod.Get, request.Method);
        //        ClassicAssert.AreEqual($"{baseUrl}/users/42?filter=active", request.RequestUri.ToString());
        //    }

        //    // Метод з маршрутом, що містить необов'язковий параметр, який може бути не вказаний
        //    [Get("/users/{id}/{optionalAction?}")]
        //    public Task GetUserWithOptionalActionAsync(int id, string optionalAction = "view")
        //    {
        //        return Task.CompletedTask;
        //    }

        //    [Test]
        //    public void ReplaceRouteParameters_ShouldReplaceOptionalRouteParameterInUrl()
        //    {
        //        // Arrange
        //        var requestHandlerMock = new Mock<IRequestHandler>();
        //        var httpClient = new HttpClient();
        //        var apiClient = new ApiClient(requestHandlerMock.Object, httpClient);

        //        var arguments = new object[] { 42 };
        //        MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(GetUserWithOptionalActionAsync));
        //        var parameters = methodInfo.GetParameters();

        //        // Act
        //        var request = apiClient.CreateRequest(methodInfo, parameters, arguments);

        //        // ClassicAssert
        //        ClassicAssert.AreEqual(HttpMethod.Get, request.Method);
        //        ClassicAssert.AreEqual($"{baseUrl}/users/42/view", request.RequestUri.ToString());
        //    }

        //    [Test]
        //    public void ReplaceRouteParameters_ShouldReplaceOptionalRouteParameterWithProvidedValue()
        //    {
        //        // Arrange
        //        var requestHandlerMock = new Mock<IRequestHandler>();
        //        var httpClient = new HttpClient();
        //        var apiClient = new ApiClient(requestHandlerMock.Object, httpClient);

        //        var arguments = new object[] { 42, "edit" };
        //        MethodInfo methodInfo = typeof(ApiClientTests).GetMethod(nameof(GetUserWithOptionalActionAsync));
        //        var parameters = methodInfo.GetParameters();

        //        // Act
        //        var request = apiClient.CreateRequest(methodInfo, parameters, arguments);

        //        // ClassicAssert
        //        ClassicAssert.AreEqual(HttpMethod.Get, request.Method);
        //        ClassicAssert.AreEqual($"{baseUrl}/users/42/edit", request.RequestUri.ToString());
        //    }
    }
}
