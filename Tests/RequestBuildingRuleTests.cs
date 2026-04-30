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
    public class RequestBuildingRuleTests
    {
        private const string BaseUrl = "https://api.example.com/resource";
        private ApiClient _apiClient = null!;

        [SetUp]
        public void SetUp()
        {
            _apiClient = new ApiClient(new CaptureRequestHandler(), new HttpClient());
        }

        [Test]
        public void Metadata_ThrowsWhenHttpMethodAttributeIsMissing()
        {
            ClassicAssert.Throws<RestweenRequestBuildException>(() => CreateRequest(nameof(NoHttpMethodAttribute)));
        }

        [Test]
        public void Metadata_ThrowsWhenArgumentCountDoesNotMatchParameterCount()
        {
            var method = GetType().GetMethod(nameof(ExplicitQueryName))!;

            ClassicAssert.Throws<RestweenRequestBuildException>(() =>
                _apiClient.CreateRequest(method, method.GetParameters(), Array.Empty<object>()));
        }

        [Test]
        public void Metadata_BuildsGetMethod()
        {
            var request = CreateRequest(nameof(GetMethod));

            ClassicAssert.AreEqual(HttpMethod.Get, request.Method);
            ClassicAssert.AreEqual(BaseUrl, request.RequestUri!.ToString());
        }

        [Test]
        public void Metadata_BuildsPostMethod()
        {
            var request = CreateRequest(nameof(PostMethod));

            ClassicAssert.AreEqual(HttpMethod.Post, request.Method);
            ClassicAssert.AreEqual(BaseUrl, request.RequestUri!.ToString());
        }

        [Test]
        public void Metadata_BuildsPutMethod()
        {
            var request = CreateRequest(nameof(PutMethod));

            ClassicAssert.AreEqual(HttpMethod.Put, request.Method);
            ClassicAssert.AreEqual(BaseUrl, request.RequestUri!.ToString());
        }

        [Test]
        public void Metadata_BuildsDeleteMethod()
        {
            var request = CreateRequest(nameof(DeleteMethod));

            ClassicAssert.AreEqual(HttpMethod.Delete, request.Method);
            ClassicAssert.AreEqual(BaseUrl, request.RequestUri!.ToString());
        }

        [Test]
        public void Route_ExplicitNameUsesAttributeName()
        {
            var request = CreateRequest(nameof(ExplicitRouteName), 15);

            ClassicAssert.AreEqual(BaseUrl + "/15", request.RequestUri!.ToString());
        }

        [Test]
        public void Route_ImplicitNameUsesParameterName()
        {
            var request = CreateRequest(nameof(ImplicitRouteName), 16);

            ClassicAssert.AreEqual(BaseUrl + "/16", request.RequestUri!.ToString());
        }

        [Test]
        public void Route_EncodesFormattedValue()
        {
            var request = CreateRequest(nameof(RouteWithSpecialCharacters), "john doe");

            ClassicAssert.AreEqual(BaseUrl + "/john+doe", request.RequestUri!.ToString());
        }

        [Test]
        public void Route_FormatsEnumMemberValue()
        {
            var request = CreateRequest(nameof(RouteWithEnum), SortOrder.Descending);

            ClassicAssert.AreEqual(BaseUrl + "/desc", request.RequestUri!.ToString());
        }

        [Test]
        public void Route_FormatsDateTimeAsUtcQueryStyleValue()
        {
            var request = CreateRequest(nameof(RouteWithDateTime), new DateTime(2026, 4, 30, 10, 15, 0, DateTimeKind.Utc));

            ClassicAssert.AreEqual(BaseUrl + "/2026-04-30T10%3a15%3a00Z", request.RequestUri!.ToString());
        }

        [Test]
        public void Route_ThrowsWhenValueIsNull()
        {
            ClassicAssert.Throws<RestweenRequestBuildException>(() => CreateRequest(nameof(NullableRoute), new object?[] { null }));
        }

        [Test]
        public void Route_ThrowsWhenDuplicateRouteIsBound()
        {
            ClassicAssert.Throws<RestweenRequestBuildException>(() => CreateRequest(nameof(DuplicateRoute), 1, 2));
        }

        [Test]
        public void Route_ThrowsWhenTemplateDoesNotContainExplicitRoute()
        {
            ClassicAssert.Throws<RestweenRequestBuildException>(() => CreateRequest(nameof(MissingExplicitRouteTemplate), 1));
        }

        [Test]
        public void Query_ExplicitNameUsesAttributeName()
        {
            var request = CreateRequest(nameof(ExplicitQueryName), 20);

            ClassicAssert.AreEqual(BaseUrl + "?page_size=20", request.RequestUri!.ToString());
        }

        [Test]
        public void Query_NullValueIsSkipped()
        {
            var request = CreateRequest(nameof(NullableQuery), null, "active");

            ClassicAssert.AreEqual(BaseUrl + "?status=active", request.RequestUri!.ToString());
        }

        [Test]
        public void Query_EmptyStringIsIncluded()
        {
            var request = CreateRequest(nameof(NullableQuery), string.Empty, "active");

            ClassicAssert.AreEqual(BaseUrl + "?search=&status=active", request.RequestUri!.ToString());
        }

        [Test]
        public void Query_AppendsToExistingQueryString()
        {
            var request = CreateRequest(nameof(ExistingQueryString), 2);

            ClassicAssert.AreEqual(BaseUrl + "?existing=1&page=2", request.RequestUri!.ToString());
        }

        [Test]
        public void Query_BoolIsLowercase()
        {
            var request = CreateRequest(nameof(BoolQuery), false);

            ClassicAssert.AreEqual(BaseUrl + "?enabled=false", request.RequestUri!.ToString());
        }

        [Test]
        public void Query_DecimalUsesInvariantCulture()
        {
            var request = CreateRequest(nameof(DecimalQuery), 1234.56m);

            ClassicAssert.AreEqual(BaseUrl + "?amount=1234.56", request.RequestUri!.ToString());
        }

        [Test]
        public void Query_EnumUsesEnumMemberValue()
        {
            var request = CreateRequest(nameof(EnumQuery), SortOrder.Descending);

            ClassicAssert.AreEqual(BaseUrl + "?sort=desc", request.RequestUri!.ToString());
        }

        [Test]
        public void Query_UnspecifiedDateTimeHasNoUtcSuffix()
        {
            var request = CreateRequest(nameof(DateTimeQuery), new DateTime(2026, 4, 30, 10, 15, 0, DateTimeKind.Unspecified));

            ClassicAssert.AreEqual(BaseUrl + "?from=2026-04-30T10%3a15%3a00", request.RequestUri!.ToString());
        }

        [Test]
        public void Query_UtcDateTimeHasUtcSuffix()
        {
            var request = CreateRequest(nameof(DateTimeQuery), new DateTime(2026, 4, 30, 10, 15, 0, DateTimeKind.Utc));

            ClassicAssert.AreEqual(BaseUrl + "?from=2026-04-30T10%3a15%3a00Z", request.RequestUri!.ToString());
        }

        [Test]
        public void Query_DefaultCollectionRepeatsSameName()
        {
            var request = CreateRequest(nameof(DefaultCollectionQuery), (object)new[] { "red", "blue" });

            ClassicAssert.AreEqual(BaseUrl + "?tags=red&tags=blue", request.RequestUri!.ToString());
        }

        [Test]
        public void Query_MultiCollectionAddsArraySuffix()
        {
            var request = CreateRequest(nameof(MultiCollectionQuery), (object)new[] { "UA", "PL" });

            ClassicAssert.AreEqual(BaseUrl + "?countryCodes[]=UA&countryCodes[]=PL", request.RequestUri!.ToString());
        }

        [Test]
        public void Query_CollectionSkipsNullItems()
        {
            var request = CreateRequest(nameof(NullableCollectionQuery), (object)new string?[] { "one", null, "three" });

            ClassicAssert.AreEqual(BaseUrl + "?values=one&values=three", request.RequestUri!.ToString());
        }

        [Test]
        public void Query_ComplexObjectExpandsPropertiesAndJsonNames()
        {
            var request = CreateRequest(nameof(ExplicitComplexQuery), new SearchQuery { PageSize = 50, Term = "hello" });

            ClassicAssert.AreEqual(BaseUrl + "?page_size=50&Term=hello", request.RequestUri!.ToString());
        }

        [Test]
        public void Query_ComplexObjectSkipsNullProperties()
        {
            var request = CreateRequest(nameof(ExplicitComplexQuery), new SearchQuery { PageSize = 10, Term = null });

            ClassicAssert.AreEqual(BaseUrl + "?page_size=10", request.RequestUri!.ToString());
        }

        [Test]
        public void Query_ComplexObjectCanIncludeInternalGetter()
        {
            var request = CreateRequest(nameof(InternalGetterQuery), new InternalGetterFilter(3, "secret"));

            ClassicAssert.AreEqual(BaseUrl + "?Page=3&InternalCode=secret", request.RequestUri!.ToString());
        }

        [Test]
        public void Query_DuplicateExplicitNameThrows()
        {
            ClassicAssert.Throws<RestweenRequestBuildException>(() => CreateRequest(nameof(DuplicateExplicitQuery), 1, 2));
        }

        [Test]
        public void Query_DuplicateNameFromComplexObjectThrows()
        {
            ClassicAssert.Throws<RestweenRequestBuildException>(() =>
                CreateRequest(nameof(DuplicateComplexQuery), new SearchQuery { PageSize = 1 }, 2));
        }

        [Test]
        public void Query_MultiDuplicateNameIsAllowed()
        {
            var request = CreateRequest(nameof(DuplicateMultiQuery), new[] { "a" }, new[] { "b" });

            ClassicAssert.AreEqual(BaseUrl + "?tag[]=a&tag[]=b", request.RequestUri!.ToString());
        }

        [Test]
        public async Task Body_ExplicitBodySerializesJson()
        {
            var request = CreateRequest(nameof(ExplicitPostBody), new SearchQuery { PageSize = 4, Term = "body" });

            ClassicAssert.AreEqual("{\"page_size\":4,\"Term\":\"body\"}", await request.Content!.ReadAsStringAsync());
        }

        [Test]
        public void Body_NullBodyDoesNotCreateContentForPost()
        {
            var request = CreateRequest(nameof(ExplicitPostBody), new object?[] { null });

            ClassicAssert.IsNull(request.Content);
        }

        [Test]
        public async Task Body_PostImplicitComplexObjectBecomesBody()
        {
            var request = CreateRequest(nameof(ImplicitPostBody), new SearchQuery { PageSize = 5, Term = "implicit" });

            ClassicAssert.AreEqual("{\"page_size\":5,\"Term\":\"implicit\"}", await request.Content!.ReadAsStringAsync());
        }

        [Test]
        public async Task Body_PutImplicitComplexObjectBecomesBody()
        {
            var request = CreateRequest(nameof(ImplicitPutBody), new SearchQuery { PageSize = 6, Term = "put" });

            ClassicAssert.AreEqual("{\"page_size\":6,\"Term\":\"put\"}", await request.Content!.ReadAsStringAsync());
        }

        [Test]
        public void Body_PostSimpleParameterStaysQuery()
        {
            var request = CreateRequest(nameof(PostSimpleQuery), true);

            ClassicAssert.AreEqual(BaseUrl + "?notify=true", request.RequestUri!.ToString());
            ClassicAssert.IsNull(request.Content);
        }

        [Test]
        public void Body_PostCollectionParameterStaysQuery()
        {
            var request = CreateRequest(nameof(PostCollectionQuery), (object)new[] { "x", "y" });

            ClassicAssert.AreEqual(BaseUrl + "?tags=x&tags=y", request.RequestUri!.ToString());
            ClassicAssert.IsNull(request.Content);
        }

        [Test]
        public void Body_GetImplicitComplexObjectExpandsToQuery()
        {
            var request = CreateRequest(nameof(ImplicitGetComplexQuery), new SearchQuery { PageSize = 7, Term = "get" });

            ClassicAssert.AreEqual(BaseUrl + "?page_size=7&Term=get", request.RequestUri!.ToString());
        }

        [Test]
        public void Body_DeleteWithExplicitBodyThrows()
        {
            ClassicAssert.Throws<RestweenRequestBuildException>(() =>
                CreateRequest(nameof(DeleteWithBody), new SearchQuery { PageSize = 1 }));
        }

        [Test]
        public void Body_GetWithExplicitBodyThrows()
        {
            ClassicAssert.Throws<RestweenRequestBuildException>(() =>
                CreateRequest(nameof(GetWithBody), new SearchQuery { PageSize = 1 }));
        }

        [Test]
        public void Body_ExplicitAndImplicitBodyTogetherThrow()
        {
            ClassicAssert.Throws<RestweenRequestBuildException>(() =>
                CreateRequest(nameof(ExplicitAndImplicitBody), new SearchQuery(), new SearchQuery()));
        }

        [Test]
        public void Header_MethodHeaderIsAdded()
        {
            var request = CreateRequest(nameof(MethodHeader));

            ClassicAssert.AreEqual("restween", string.Join("", request.Headers.GetValues("X-Client")));
        }

        [Test]
        public void Header_ParameterHeaderIsAdded()
        {
            var request = CreateRequest(nameof(ParameterHeader), "trace-1");

            ClassicAssert.AreEqual("trace-1", string.Join("", request.Headers.GetValues("X-Trace")));
        }

        [Test]
        public void Header_NullParameterHeaderIsSkipped()
        {
            var request = CreateRequest(nameof(ParameterHeader), new object?[] { null });

            ClassicAssert.IsFalse(request.Headers.Contains("X-Trace"));
        }

        [Test]
        public void Header_ParameterHeaderOverridesMethodHeader()
        {
            var request = CreateRequest(nameof(HeaderOverride), "parameter");

            ClassicAssert.AreEqual("parameter", string.Join("", request.Headers.GetValues("X-Mode")));
        }

        [Test]
        public async Task Header_ContentTypeHeaderAppliesToBodyContent()
        {
            var request = CreateRequest(nameof(ContentTypeWithBody), new SearchQuery { PageSize = 8 });

            ClassicAssert.AreEqual("application/vnd.restween+json", request.Content!.Headers.ContentType!.MediaType);
            ClassicAssert.AreEqual("{\"page_size\":8,\"Term\":null}", await request.Content.ReadAsStringAsync());
        }

        [Test]
        public void Header_ContentHeaderWithoutContentThrows()
        {
            ClassicAssert.Throws<RestweenRequestBuildException>(() => CreateRequest(nameof(ContentHeaderWithoutBody)));
        }

        [Test]
        public void Header_InvalidMethodHeaderThrows()
        {
            ClassicAssert.Throws<RestweenRequestBuildException>(() => CreateRequest(nameof(InvalidMethodHeader)));
        }

        [Test]
        public void Header_DuplicateMethodHeaderThrows()
        {
            ClassicAssert.Throws<RestweenRequestBuildException>(() => CreateRequest(nameof(DuplicateMethodHeader)));
        }

        [Test]
        public void Header_DateTimeUsesRfc1123Format()
        {
            var request = CreateRequest(nameof(DateTimeHeader), new DateTime(2026, 4, 30, 10, 15, 0, DateTimeKind.Utc));

            ClassicAssert.AreEqual("Thu, 30 Apr 2026 10:15:00 GMT", string.Join("", request.Headers.GetValues("Date")));
        }

        [Test]
        public void Multipart_CreatesMultipartFormDataContent()
        {
            var request = CreateRequest(nameof(MultipartSimple), "name");

            ClassicAssert.IsInstanceOf<MultipartFormDataContent>(request.Content);
        }

        [Test]
        public async Task Multipart_ByteArrayAddsFilePart()
        {
            var request = CreateRequest(nameof(MultipartBytes), Encoding.UTF8.GetBytes("bytes"));
            var content = await request.Content!.ReadAsStringAsync();

            StringAssert.Contains("name=file", content);
            StringAssert.Contains("filename=file", content);
            StringAssert.Contains("bytes", content);
        }

        [Test]
        public async Task Multipart_StreamAddsFilePart()
        {
            var request = CreateRequest(nameof(MultipartStream), new MemoryStream(Encoding.UTF8.GetBytes("stream")));
            var content = await request.Content!.ReadAsStringAsync();

            StringAssert.Contains("name=file", content);
            StringAssert.Contains("filename=file", content);
            StringAssert.Contains("stream", content);
        }

        [Test]
        public async Task Multipart_FileInfoUsesFileName()
        {
            var path = Path.Combine(Path.GetTempPath(), "restween-" + Guid.NewGuid().ToString("N") + ".txt");
            File.WriteAllText(path, "file-info");
            HttpRequestMessage? request = null;

            try
            {
                request = CreateRequest(nameof(MultipartFileInfo), new FileInfo(path));
                var content = await request.Content!.ReadAsStringAsync();

                StringAssert.Contains("name=file", content);
                StringAssert.Contains("filename=" + Path.GetFileName(path), content);
                StringAssert.Contains("file-info", content);
            }
            finally
            {
                request?.Dispose();
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Test]
        public async Task Multipart_SimpleValueAddsStringPart()
        {
            var request = CreateRequest(nameof(MultipartSimple), "display-name");
            var content = await request.Content!.ReadAsStringAsync();

            StringAssert.Contains("name=name", content);
            StringAssert.Contains("display-name", content);
        }

        [Test]
        public async Task Multipart_ComplexValueAddsJsonPart()
        {
            var request = CreateRequest(nameof(MultipartComplex), new SearchQuery { PageSize = 9, Term = "multi" });
            var content = await request.Content!.ReadAsStringAsync();

            StringAssert.Contains("name=payload", content);
            StringAssert.Contains("application/json", content);
            StringAssert.Contains("{\"page_size\":9,\"Term\":\"multi\"}", content);
        }

        [Test]
        public async Task Multipart_NullValueIsSkipped()
        {
            var request = CreateRequest(nameof(MultipartNullable), new object?[] { null, "second" });
            var content = await request.Content!.ReadAsStringAsync();

            ClassicAssert.IsFalse(content.Contains("name=first"));
            StringAssert.Contains("name=second", content);
        }

        [Test]
        public async Task Multipart_UsesCustomSerializerForComplexParts()
        {
            var serializer = new FixedSerializer();
            var formatter = new DefaultRestweenValueFormatter();
            var builder = new DefaultRestweenRequestBuilder(
                new HttpMethodMetadataReader(),
                new IRestweenParameterBinder[]
                {
                    new MultipartParameterBinder(serializer),
                    new HeaderParameterBinder(formatter),
                    new RouteParameterBinder(),
                    new QueryParameterBinder(),
                    new BodyParameterBinder()
                },
                serializer,
                formatter);

            var apiClient = new ApiClient(new CaptureRequestHandler(), new HttpClient(), builder);
            var method = GetType().GetMethod(nameof(MultipartComplex))!;
            var request = apiClient.CreateRequest(method, method.GetParameters(), new object[] { new SearchQuery() });
            var content = await request.Content!.ReadAsStringAsync();

            StringAssert.Contains("custom-multipart-json", content);
        }

        [Test]
        public async Task Extensibility_CustomParameterBinderRunsBeforeDefaultBinders()
        {
            var handler = new CaptureRequestHandler();
            var services = new ServiceCollection();

            services.AddSingleton<IRequestHandler>(handler);
            services.AddSingleton<IRestweenParameterBinder, TokenHeaderBinder>();
            services.AddApiClient<ICustomBinderApi>(new Uri("https://api.example.com"));

            var client = services.BuildServiceProvider().GetRequiredService<ICustomBinderApi>();
            await client.GetAsync("token-value");

            var request = handler.LastRequest!;
            ClassicAssert.AreEqual("https://api.example.com/custom", request.RequestUri!.ToString());
            ClassicAssert.AreEqual("token-value", string.Join("", request.Headers.GetValues("X-Custom-Token")));
        }

        [Test]
        public async Task Extensibility_CustomFormatterIsUsedByDefaultBinders()
        {
            var handler = new CaptureRequestHandler();
            var services = new ServiceCollection();

            services.AddSingleton<IRequestHandler>(handler);
            services.AddSingleton<IRestweenValueFormatter, PrefixFormatter>();
            services.AddApiClient<IFormattedApi>(new Uri("https://api.example.com"));

            var client = services.BuildServiceProvider().GetRequiredService<IFormattedApi>();
            await client.GetAsync(10, 20, 30);

            var request = handler.LastRequest!;
            ClassicAssert.AreEqual("https://api.example.com/items/value-10?page=value-20", request.RequestUri!.ToString());
            ClassicAssert.AreEqual("value-30", string.Join("", request.Headers.GetValues("X-Mode")));
        }

        [Test]
        public async Task Extensibility_CustomRequestBuilderReceivesInvocationMetadata()
        {
            var handler = new CaptureRequestHandler();
            var builder = new MetadataCaptureRequestBuilder();
            var client = ApiClientFactory.CreateClient<IMetadataApi>(new HttpClient(), handler, builder);

            await client.GetAsync(123, "abc");

            ClassicAssert.AreEqual(nameof(IMetadataApi.GetAsync), builder.MethodName);
            ClassicAssert.AreEqual(2, builder.ParameterCount);
            ClassicAssert.AreEqual(2, builder.ArgumentCount);
        }

        public void NoHttpMethodAttribute()
        {
        }

        [Get(BaseUrl)]
        public void GetMethod()
        {
        }

        [Post(BaseUrl)]
        public void PostMethod()
        {
        }

        [Put(BaseUrl)]
        public void PutMethod()
        {
        }

        [Delete(BaseUrl)]
        public void DeleteMethod()
        {
        }

        [Get(BaseUrl + "/{id}")]
        public void ExplicitRouteName([Route("id")] int value)
        {
        }

        [Get(BaseUrl + "/{id}")]
        public void ImplicitRouteName(int id)
        {
        }

        [Get(BaseUrl + "/{slug}")]
        public void RouteWithSpecialCharacters(string slug)
        {
        }

        [Get(BaseUrl + "/{sort}")]
        public void RouteWithEnum(SortOrder sort)
        {
        }

        [Get(BaseUrl + "/{from}")]
        public void RouteWithDateTime(DateTime from)
        {
        }

        [Get(BaseUrl + "/{id}")]
        public void NullableRoute(string id)
        {
        }

        [Get(BaseUrl + "/{id}")]
        public void DuplicateRoute([Route("id")] int first, [Route("id")] int second)
        {
        }

        [Get(BaseUrl)]
        public void MissingExplicitRouteTemplate([Route("id")] int id)
        {
        }

        [Get(BaseUrl)]
        public void ExplicitQueryName([Query("page_size")] int pageSize)
        {
        }

        [Get(BaseUrl)]
        public void NullableQuery(string? search, string status)
        {
        }

        [Get(BaseUrl + "?existing=1")]
        public void ExistingQueryString(int page)
        {
        }

        [Get(BaseUrl)]
        public void BoolQuery(bool enabled)
        {
        }

        [Get(BaseUrl)]
        public void DecimalQuery(decimal amount)
        {
        }

        [Get(BaseUrl)]
        public void EnumQuery(SortOrder sort)
        {
        }

        [Get(BaseUrl)]
        public void DateTimeQuery(DateTime from)
        {
        }

        [Get(BaseUrl)]
        public void DefaultCollectionQuery([Query] string[] tags)
        {
        }

        [Get(BaseUrl)]
        public void MultiCollectionQuery([Query(collectionFormat: CollectionFormat.Multi)] string[] countryCodes)
        {
        }

        [Get(BaseUrl)]
        public void NullableCollectionQuery([Query] string?[] values)
        {
        }

        [Get(BaseUrl)]
        public void ExplicitComplexQuery([Query] SearchQuery query)
        {
        }

        [Get(BaseUrl)]
        public void InternalGetterQuery([Query] InternalGetterFilter filter)
        {
        }

        [Get(BaseUrl)]
        public void DuplicateExplicitQuery([Query("x")] int first, [Query("x")] int second)
        {
        }

        [Get(BaseUrl)]
        public void DuplicateComplexQuery([Query] SearchQuery query, [Query("page_size")] int pageSize)
        {
        }

        [Get(BaseUrl)]
        public void DuplicateMultiQuery(
            [Query("tag", collectionFormat: CollectionFormat.Multi)] string[] first,
            [Query("tag", collectionFormat: CollectionFormat.Multi)] string[] second)
        {
        }

        [Post(BaseUrl)]
        public void ExplicitPostBody([Body] SearchQuery body)
        {
        }

        [Post(BaseUrl)]
        public void ImplicitPostBody(SearchQuery body)
        {
        }

        [Put(BaseUrl)]
        public void ImplicitPutBody(SearchQuery body)
        {
        }

        [Post(BaseUrl)]
        public void PostSimpleQuery(bool notify)
        {
        }

        [Post(BaseUrl)]
        public void PostCollectionQuery(string[] tags)
        {
        }

        [Get(BaseUrl)]
        public void ImplicitGetComplexQuery(SearchQuery query)
        {
        }

        [Delete(BaseUrl)]
        public void DeleteWithBody([Body] SearchQuery body)
        {
        }

        [Get(BaseUrl)]
        public void GetWithBody([Body] SearchQuery body)
        {
        }

        [Post(BaseUrl)]
        public void ExplicitAndImplicitBody([Body] SearchQuery first, SearchQuery second)
        {
        }

        [Headers("X-Client: restween")]
        [Get(BaseUrl)]
        public void MethodHeader()
        {
        }

        [Get(BaseUrl)]
        public void ParameterHeader([Header("X-Trace")] string trace)
        {
        }

        [Headers("X-Mode: method")]
        [Get(BaseUrl)]
        public void HeaderOverride([Header("X-Mode")] string mode)
        {
        }

        [Headers("Content-Type: application/vnd.restween+json")]
        [Post(BaseUrl)]
        public void ContentTypeWithBody([Body] SearchQuery body)
        {
        }

        [Headers("Content-Type: application/json")]
        [Get(BaseUrl)]
        public void ContentHeaderWithoutBody()
        {
        }

        [Headers("InvalidHeaderLine")]
        [Get(BaseUrl)]
        public void InvalidMethodHeader()
        {
        }

        [Headers("X-Duplicate: one")]
        [Headers("X-Duplicate: two")]
        [Get(BaseUrl)]
        public void DuplicateMethodHeader()
        {
        }

        [Get(BaseUrl)]
        public void DateTimeHeader([Header("Date")] DateTime date)
        {
        }

        [Multipart]
        [Post(BaseUrl)]
        public void MultipartSimple(string name)
        {
        }

        [Multipart]
        [Post(BaseUrl)]
        public void MultipartBytes(byte[] file)
        {
        }

        [Multipart]
        [Post(BaseUrl)]
        public void MultipartStream(Stream file)
        {
        }

        [Multipart]
        [Post(BaseUrl)]
        public void MultipartFileInfo(FileInfo file)
        {
        }

        [Multipart]
        [Post(BaseUrl)]
        public void MultipartComplex(SearchQuery payload)
        {
        }

        [Multipart]
        [Post(BaseUrl)]
        public void MultipartNullable(string? first, string second)
        {
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

        public sealed class InternalGetterFilter
        {
            public InternalGetterFilter(int page, string internalCode)
            {
                Page = page;
                InternalCode = internalCode;
            }

            public int Page { get; }

            internal string InternalCode { get; }
        }

        public enum SortOrder
        {
            Ascending,

            [EnumMember(Value = "desc")]
            Descending
        }

        public interface ICustomBinderApi
        {
            [Get("https://api.example.com/custom")]
            Task GetAsync(string token);
        }

        public interface IFormattedApi
        {
            [Get("https://api.example.com/items/{id}")]
            Task GetAsync(int id, int page, [Header("X-Mode")] int mode);
        }

        public interface IMetadataApi
        {
            [Get("https://api.example.com/metadata")]
            Task GetAsync(int id, string name);
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

        private sealed class TokenHeaderBinder : IRestweenParameterBinder
        {
            public bool TryBind(RestweenParameterContext context)
            {
                if (!string.Equals(context.Parameter.Name, "token", StringComparison.Ordinal))
                    return false;

                if (context.Value != null)
                    context.State.AddHeader("X-Custom-Token", context.Value.ToString() ?? string.Empty);

                return true;
            }
        }

        private sealed class PrefixFormatter : IRestweenValueFormatter
        {
            public string FormatRouteValue(object value)
            {
                return "value-" + value;
            }

            public string FormatQueryValue(object value)
            {
                return "value-" + value;
            }

            public string FormatHeaderValue(object value)
            {
                return "value-" + value;
            }
        }

        private sealed class MetadataCaptureRequestBuilder : IRestweenRequestBuilder
        {
            public string? MethodName { get; private set; }

            public int ParameterCount { get; private set; }

            public int ArgumentCount { get; private set; }

            public HttpRequestMessage Build(MethodInfo method, ParameterInfo[] parameterInfos, object?[] parameters)
            {
                MethodName = method.Name;
                ParameterCount = parameterInfos.Length;
                ArgumentCount = parameters.Length;

                return new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/captured");
            }
        }
    }
}
