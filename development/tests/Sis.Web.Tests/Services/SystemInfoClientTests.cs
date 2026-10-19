using Sis.Contracts.SystemInfo;
using Sis.Web.Services;
using Sis.Web.Tests.Support;

namespace Sis.Web.Tests.Services;

public sealed class SystemInfoClientTests
{
    [Fact]
    public async Task GetAsync_reads_system_info_from_the_api()
    {
        var expected = new SystemInfoResponse("Student Information System", "1.0.0", "Testing", DateTimeOffset.UnixEpoch);
        HttpRequestMessage? sent = null;
        var http = StubHttp.Create(request =>
        {
            sent = request;
            return StubHttp.Json(expected);
        });

        var actual = await new SystemInfoClient(http).GetAsync();

        actual.Should().Be(expected);
        sent!.RequestUri.Should().Be(new Uri(StubHttp.BaseAddress, "api/system/info"));
    }
}
