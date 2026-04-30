using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using RESTween.Server;
using System;
using System.IO;
using System.Linq;

namespace RESTween.Server.Tests
{
    [TestFixture]
    public sealed class RestweenControllerGeneratorTests
    {
        [Test]
        public void GeneratesControllerThatDelegatesToSameInterfaceHandler()
        {
            var source = """
                using RESTween.Attributes;
                using RESTween.Server;
                using System.Threading.Tasks;

                namespace Demo;

                public sealed class UserDto
                {
                    public int Id { get; set; }
                }

                public sealed class CreateUserDto
                {
                    public string? Name { get; set; }
                }

                [RestweenController]
                public interface IUserApi
                {
                    [Get("/users/{id}")]
                    Task<UserDto> GetUserAsync([Route] int id, [Query("includeDeleted")] bool includeDeleted, [Header("x-tenant")] string tenant);

                    [Post("/users")]
                    Task<UserDto> CreateUserAsync([Body] CreateUserDto dto);
                }

                public sealed class UserApiHandler : IUserApi
                {
                    public Task<UserDto> GetUserAsync(int id, bool includeDeleted, string tenant)
                    {
                        return Task.FromResult(new UserDto { Id = id });
                    }

                    public Task<UserDto> CreateUserAsync(CreateUserDto dto)
                    {
                        return Task.FromResult(new UserDto());
                    }
                }
                """;

            var result = RunGenerator(source);
            var generatedController = result.GeneratedSources.Single(text => text.Contains("class UserApiController"));

            Assert.That(generatedController, Does.Contain("[global::Microsoft.AspNetCore.Mvc.HttpGet(\"/users/{id}\")]"));
            Assert.That(generatedController, Does.Contain("private readonly global::Demo.IUserApi _handler;"));
            Assert.That(generatedController, Does.Contain("[global::Microsoft.AspNetCore.Mvc.FromRoute(Name = \"id\")] global::System.Int32 id"));
            Assert.That(generatedController, Does.Contain("[global::Microsoft.AspNetCore.Mvc.FromQuery(Name = \"includeDeleted\")] global::System.Boolean includeDeleted"));
            Assert.That(generatedController, Does.Contain("[global::Microsoft.AspNetCore.Mvc.FromHeader(Name = \"x-tenant\")] global::System.String tenant"));
            Assert.That(generatedController, Does.Contain("[global::Microsoft.AspNetCore.Mvc.FromBody] global::Demo.CreateUserDto dto"));
            Assert.That(generatedController, Does.Contain("=> _handler.GetUserAsync(id, includeDeleted, tenant);"));
            Assert.That(generatedController, Does.Contain("=> _handler.CreateUserAsync(dto);"));

            ClassicAssert.IsEmpty(result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray());
        }

        [Test]
        public void MapsParametersWithoutAttributesUsingRestweenClientConventions()
        {
            var source = """
                using RESTween.Attributes;
                using RESTween.Server;

                namespace Demo;

                public sealed class SearchFilter
                {
                    public string? Term { get; set; }
                }

                [RestweenController]
                public interface ISearchApi
                {
                    [Get("/users/{id}")]
                    string GetUser(int id, string culture);

                    [Post("/search")]
                    string Search(SearchFilter filter);
                }
                """;

            var result = RunGenerator(source);
            var generatedController = result.GeneratedSources.Single(text => text.Contains("class SearchApiController"));

            Assert.That(generatedController, Does.Contain("[global::Microsoft.AspNetCore.Mvc.FromRoute(Name = \"id\")] global::System.Int32 id"));
            Assert.That(generatedController, Does.Contain("[global::Microsoft.AspNetCore.Mvc.FromQuery(Name = \"culture\")] global::System.String culture"));
            Assert.That(generatedController, Does.Contain("[global::Microsoft.AspNetCore.Mvc.FromBody] global::Demo.SearchFilter filter"));
            ClassicAssert.IsEmpty(result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray());
        }

        [Test]
        public void GeneratesControllerFromAspNetCoreHttpAttributes()
        {
            var source = """
                using Microsoft.AspNetCore.Authorization;
                using Microsoft.AspNetCore.Mvc;
                using RESTween.Attributes;
                using RESTween.Server;
                using System.Threading.Tasks;
                using RestweenRouteAttribute = RESTween.Attributes.RouteAttribute;

                namespace Demo;

                public sealed class LoginDto
                {
                    public string? Email { get; set; }
                }

                public sealed class LoginResult
                {
                    public string? Token { get; set; }
                }

                [RestweenController]
                public interface IUserApi
                {
                    [AllowAnonymous]
                    [HttpPost("/login")]
                    Task<LoginResult> LoginAsync(LoginDto dto);

                    [Authorize(Roles = "Admin", AuthenticationSchemes = "Bearer")]
                    [HttpGet("/users/{id}")]
                    Task<string> GetUserAsync([RestweenRoute] int id);

                    [Authorize(Policy = "Users.Write")]
                    [HttpPut("/users/{id}")]
                    Task<string> UpdateUserAsync([RestweenRoute] int id, LoginDto dto);

                    [HttpDelete("/users/{id}")]
                    Task DeleteUserAsync([RestweenRoute] int id);
                }
                """;

            var result = RunGenerator(source);
            var generatedController = result.GeneratedSources.Single(text => text.Contains("class UserApiController"));

            Assert.That(generatedController, Does.Contain("[global::Microsoft.AspNetCore.Authorization.AllowAnonymous]"));
            Assert.That(generatedController, Does.Contain("[global::Microsoft.AspNetCore.Authorization.Authorize(Roles = \"Admin\", AuthenticationSchemes = \"Bearer\")]"));
            Assert.That(generatedController, Does.Contain("[global::Microsoft.AspNetCore.Authorization.Authorize(Policy = \"Users.Write\")]"));
            Assert.That(generatedController, Does.Contain("[global::Microsoft.AspNetCore.Mvc.HttpPost(\"/login\")]"));
            Assert.That(generatedController, Does.Contain("[global::Microsoft.AspNetCore.Mvc.HttpGet(\"/users/{id}\")]"));
            Assert.That(generatedController, Does.Contain("[global::Microsoft.AspNetCore.Mvc.HttpPut(\"/users/{id}\")]"));
            Assert.That(generatedController, Does.Contain("[global::Microsoft.AspNetCore.Mvc.HttpDelete(\"/users/{id}\")]"));
            Assert.That(generatedController, Does.Contain("[global::Microsoft.AspNetCore.Mvc.FromBody] global::Demo.LoginDto dto"));
            ClassicAssert.IsEmpty(result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray());
        }

        [Test]
        public void GeneratesParameterlessAspNetCoreHttpAttributeWhenTemplateIsMissing()
        {
            var source = """
                using Microsoft.AspNetCore.Mvc;
                using RESTween.Server;

                namespace Demo;

                [RestweenController]
                public interface IHealthApi
                {
                    [HttpGet]
                    string Ping();
                }
                """;

            var result = RunGenerator(source);
            var generatedController = result.GeneratedSources.Single(text => text.Contains("class HealthApiController"));

            Assert.That(generatedController, Does.Contain("[global::Microsoft.AspNetCore.Mvc.HttpGet]"));
            ClassicAssert.IsEmpty(result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray());
        }

        [Test]
        public void ReportsDiagnosticWhenMethodHasMultipleHttpAttributes()
        {
            var source = """
                using Microsoft.AspNetCore.Mvc;
                using RESTween.Attributes;
                using RESTween.Server;

                namespace Demo;

                [RestweenController]
                public interface IUserApi
                {
                    [Get("/users")]
                    [HttpGet("/mvc-users")]
                    string GetUsers();
                }
                """;

            var result = RunGenerator(source);
            var diagnostic = result.Diagnostics.SingleOrDefault(d => d.Id == "RESTWEEN001");

            ClassicAssert.IsNotNull(diagnostic);
            ClassicAssert.AreEqual(DiagnosticSeverity.Error, diagnostic!.Severity);
        }

        private static GeneratorRunResult RunGenerator(string source)
        {
            var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
            var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
            var compilation = CSharpCompilation.Create(
                "RESTween.Server.Generator.Tests",
                new[] { syntaxTree },
                GetReferences(),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var generator = new RestweenControllerGenerator();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            var emitDiagnostics = outputCompilation.GetDiagnostics();
            var runResult = driver.GetRunResult();
            var generatedSources = runResult.Results.SelectMany(result => result.GeneratedSources)
                .Select(sourceText => sourceText.SourceText.ToString())
                .ToArray();

            return new GeneratorRunResult(
                generatedSources,
                diagnostics.Concat(emitDiagnostics).ToArray());
        }

        private static MetadataReference[] GetReferences()
        {
            var trustedPlatformAssemblies = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
                ?.Split(Path.PathSeparator)
                ?? Array.Empty<string>();

            return trustedPlatformAssemblies
                .Where(File.Exists)
                .Select(path => MetadataReference.CreateFromFile(path))
                .Concat(new[]
                {
                    MetadataReference.CreateFromFile(typeof(RESTween.Attributes.GetAttribute).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Microsoft.AspNetCore.Mvc.ControllerBase).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute).Assembly.Location)
                })
                .GroupBy(reference => reference.Display)
                .Select(group => group.First())
                .ToArray();
        }

        private sealed class GeneratorRunResult
        {
            public GeneratorRunResult(string[] generatedSources, Diagnostic[] diagnostics)
            {
                GeneratedSources = generatedSources;
                Diagnostics = diagnostics;
            }

            public string[] GeneratedSources { get; }
            public Diagnostic[] Diagnostics { get; }
        }
    }
}
