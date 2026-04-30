using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using RESTween;
using RESTween.Attributes;
using RESTween.Building;
using RESTween.Handlers;
using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tests
{
    [TestFixture]
    public class RequestBuildingTests
    {
        private const string BaseUrl = "https://api.example.com/resource";
        private ApiClient _apiClient = null!;

        [SetUp]
        public void SetUp()
        {
            _apiClient = new ApiClient(new CaptureRequestHandler(), new HttpClient());
        }

        [Get(BaseUrl + "/{id}")]
        public void ExplicitRoute([Route("id")] int value)
        {
        }

        [Test]
        public void RouteBinder_BindsExplicitRoute()
        {
            var request = CreateRequest(nameof(ExplicitRoute), 42);

            ClassicAssert.AreEqual(HttpMethod.Get, request.Method);
            ClassicAssert.AreEqual(BaseUrl + "/42", request.RequestUri!.ToString());
        }

        [Get(BaseUrl + "/{id}")]
        public void ImplicitRoute(int id)
        {
        }

        [Test]
        public void RouteBinder_BindsImplicitRouteByTemplateName()
        {
            var request = CreateRequest(nameof(ImplicitRoute), 7);

            ClassicAssert.AreEqual(BaseUrl + "/7", request.RequestUri!.ToString());
        }

        [Get(BaseUrl)]
        public void ComplexQuery([Query] SearchQuery query)
        {
        }

        [Test]
        public void QueryBinder_ExpandsComplexDtoAndJsonPropertyName()
        {
            var request = CreateRequest(nameof(ComplexQuery), new SearchQuery { PageSize = 25, Term = "hello world" });

            ClassicAssert.AreEqual(BaseUrl + "?page_size=25&Term=hello+world", request.RequestUri!.ToString());
        }

        [Get(BaseUrl)]
        public void QueryFormatting(SortOrder sort, bool includeInactive, DateTime from)
        {
        }

        [Test]
        public void QueryBinder_UsesStableValueFormatting()
        {
            var request = CreateRequest(
                nameof(QueryFormatting),
                SortOrder.Descending,
                true,
                new DateTime(2026, 4, 30, 10, 15, 0, DateTimeKind.Utc));

            ClassicAssert.AreEqual(
                BaseUrl + "?sort=desc&includeInactive=true&from=2026-04-30T10%3a15%3a00Z",
                request.RequestUri!.ToString());
        }

        [Headers("X-Client: restween")]
        [Get(BaseUrl)]
        public void Headers([Header("X-Trace")] Guid traceId)
        {
        }

        [Test]
        public void HeaderBinder_BindsMethodAndParameterHeaders()
        {
            var traceId = Guid.Parse("5df50730-5817-44d0-a29b-8f8f333d1f67");
            var request = CreateRequest(nameof(Headers), traceId);

            ClassicAssert.AreEqual("restween", string.Join("", request.Headers.GetValues("X-Client")));
            ClassicAssert.AreEqual(traceId.ToString(), string.Join("", request.Headers.GetValues("X-Trace")));
        }

        [Put(BaseUrl)]
        public void PutBody([Body] SearchQuery body)
        {
        }

        [Test]
        public async Task BodyBinder_BindsPutBody()
        {
            var body = new SearchQuery { PageSize = 3, Term = "x" };
            var request = CreateRequest(nameof(PutBody), body);

            ClassicAssert.AreEqual(HttpMethod.Put, request.Method);
            ClassicAssert.AreEqual("{\"page_size\":3,\"Term\":\"x\"}", await request.Content!.ReadAsStringAsync());
        }

        [Delete(BaseUrl)]
        public void DeleteQuery([Query] int id)
        {
        }

        [Test]
        public void MetadataReader_BindsDeleteMethod()
        {
            var request = CreateRequest(nameof(DeleteQuery), 12);

            ClassicAssert.AreEqual(HttpMethod.Delete, request.Method);
            ClassicAssert.AreEqual(BaseUrl + "?id=12", request.RequestUri!.ToString());
        }

        [Post(BaseUrl)]
        public void DuplicateBody([Body] SearchQuery first, [Body] SearchQuery second)
        {
        }

        [Test]
        public void BodyBinder_ThrowsForDuplicateBody()
        {
            ClassicAssert.Throws<RestweenRequestBuildException>(() =>
                CreateRequest(
                    nameof(DuplicateBody),
                    new SearchQuery(),
                    new SearchQuery()));
        }

        [Multipart]
        [Post(BaseUrl)]
        public void MultipartUpload(byte[] file, int id, SearchQuery payload)
        {
        }

        [Test]
        public async Task MultipartBinder_BindsFileSimpleAndComplexParts()
        {
            var request = CreateRequest(
                nameof(MultipartUpload),
                Encoding.UTF8.GetBytes("file-content"),
                10,
                new SearchQuery { PageSize = 2, Term = "abc" });

            ClassicAssert.IsInstanceOf<MultipartFormDataContent>(request.Content);

            var content = await request.Content!.ReadAsStringAsync();
            StringAssert.Contains("name=file", content);
            StringAssert.Contains("name=id", content);
            StringAssert.Contains("name=payload", content);
            StringAssert.Contains("{\"page_size\":2,\"Term\":\"abc\"}", content);
        }

        [Post(BaseUrl)]
        public void CustomSerializedBody([Body] SearchQuery body)
        {
        }

        [Test]
        public async Task RequestBuilder_UsesCustomSerializer()
        {
            var serializer = new FixedSerializer();
            var formatter = new DefaultRestweenValueFormatter();
            var builder = new DefaultRestweenRequestBuilder(
                new HttpMethodMetadataReader(),
                new IRestweenParameterBinder[]
                {
                    new HeaderParameterBinder(formatter),
                    new RouteParameterBinder(),
                    new QueryParameterBinder(),
                    new BodyParameterBinder()
                },
                serializer,
                formatter);

            var apiClient = new ApiClient(new CaptureRequestHandler(), new HttpClient(), builder);
            var method = GetType().GetMethod(nameof(CustomSerializedBody))!;
            var request = apiClient.CreateRequest(method, method.GetParameters(), new object[] { new SearchQuery() });

            ClassicAssert.AreEqual("custom-json", await request.Content!.ReadAsStringAsync());
        }

        [Test]
        public async Task AddApiClient_UsesRegisteredRequestBuilder()
        {
            var handler = new CaptureRequestHandler();
            var builder = new FixedRequestBuilder();
            var services = new ServiceCollection();

            services.AddSingleton<IRequestHandler>(handler);
            services.AddSingleton<IRestweenRequestBuilder>(builder);
            services.AddApiClient<IProxyApi>(new Uri("https://api.example.com"));

            var provider = services.BuildServiceProvider();
            var client = provider.GetRequiredService<IProxyApi>();

            await client.GetAsync();

            ClassicAssert.IsTrue(builder.WasCalled);
            ClassicAssert.AreEqual("https://custom.example.com/from-builder", handler.LastRequest!.RequestUri!.ToString());
        }

        [Test]
        public async Task ApiClientFactory_WorksWithoutManualRequestBuilder()
        {
            var handler = new CaptureRequestHandler();
            var client = ApiClientFactory.CreateClient<IProxyApi>(new HttpClient(), handler);

            await client.GetAsync();

            ClassicAssert.AreEqual("https://api.example.com/factory", handler.LastRequest!.RequestUri!.ToString());
        }

        private HttpRequestMessage CreateRequest(string methodName, params object?[] arguments)
        {
            var method = GetType().GetMethod(methodName)!;
            return _apiClient.CreateRequest(method, method.GetParameters(), arguments);
        }

        public sealed class SearchQuery
        {
            [JsonPropertyName("page_size")]
            public int PageSize { get; set; }

            public string? Term { get; set; }
        }

        public enum SortOrder
        {
            Ascending,

            [EnumMember(Value = "desc")]
            Descending
        }

        public interface IProxyApi
        {
            [Get("https://api.example.com/factory")]
            Task GetAsync();
        }

        private sealed class CaptureRequestHandler : IRequestHandler
        {
            public HttpRequestMessage? LastRequest { get; private set; }

            public Task<T> HandleRequestAsync<T>(RequestContext context, HttpClient httpClient)
            {
                LastRequest = context.Request;
                return Task.FromResult(default(T)!);
            }

            public Task HandleRequestAsync(RequestContext context, HttpClient httpClient)
            {
                LastRequest = context.Request;
                return Task.CompletedTask;
            }
        }

        private sealed class FixedSerializer : IRestweenContentSerializer
        {
            public HttpContent SerializeJsonContent(object value)
            {
                return new StringContent("custom-json");
            }

            public HttpContent SerializeMultipartJsonContent(object value)
            {
                return new StringContent("custom-multipart-json");
            }
        }

        private sealed class FixedRequestBuilder : IRestweenRequestBuilder
        {
            public bool WasCalled { get; private set; }

            public HttpRequestMessage Build(MethodInfo method, ParameterInfo[] parameterInfos, object?[] parameters)
            {
                WasCalled = true;
                return new HttpRequestMessage(HttpMethod.Get, "https://custom.example.com/from-builder");
            }
        }
    }
}
