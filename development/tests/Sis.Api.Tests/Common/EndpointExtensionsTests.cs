using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Sis.Api.Common;

namespace Sis.Api.Tests.Common;

public sealed class EndpointExtensionsTests
{
    [Fact]
    public void AddEndpoints_registers_every_concrete_endpoint_in_the_assembly()
    {
        var services = new ServiceCollection();

        services.AddEndpoints(typeof(EndpointExtensionsTests).Assembly);

        services.Where(descriptor => descriptor.ServiceType == typeof(IEndpoint))
            .Select(descriptor => descriptor.ImplementationType)
            .Should().ContainSingle()
            .Which.Should().Be<PingEndpoint>();
    }

    private sealed class PingEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app) => app.MapGet("/ping", () => "pong");
    }

    // Must be skipped by the scan: it cannot be constructed.
    private abstract class AbstractEndpoint : IEndpoint
    {
        public abstract void MapEndpoint(IEndpointRouteBuilder app);
    }
}
