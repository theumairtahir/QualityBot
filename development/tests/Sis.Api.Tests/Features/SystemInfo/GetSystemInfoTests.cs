using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;
using Sis.Api.Features.SystemInfo;

namespace Sis.Api.Tests.Features.SystemInfo;

public sealed class GetSystemInfoTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 19, 9, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Handle_reports_the_environment_and_server_time()
    {
        var response = GetSystemInfo.Handle(new FakeTimeProvider(Now), new StubHostEnvironment("Staging"));

        response.Environment.Should().Be("Staging");
        response.ServerTimeUtc.Should().Be(Now);
    }

    [Fact]
    public void Handle_reports_the_application_name_and_a_semantic_version()
    {
        var response = GetSystemInfo.Handle(new FakeTimeProvider(Now), new StubHostEnvironment("Development"));

        response.ApplicationName.Should().Be("Student Information System");
        response.Version.Should().MatchRegex(@"^\d+\.\d+\.\d+$");
    }

    private sealed class StubHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Sis.Api";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
