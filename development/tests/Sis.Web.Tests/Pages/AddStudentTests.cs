using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Sis.Contracts.Students;
using Sis.Web.Pages.Students;
using Sis.Web.Services;
using Sis.Web.Tests.Support;
using TailwindUi.Blazor.Components;
using TailwindUi.Blazor.Extensions;

namespace Sis.Web.Tests.Pages;

public sealed class AddStudentTests : BunitContext
{
    private readonly List<HttpRequestMessage> _requests = [];

    public AddStudentTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddTailwindUi();
        Services.AddSingleton(TimeProvider.System);
        Services.AddSingleton(new StudentsClient(StubHttp.Create(request =>
        {
            _requests.Add(request);
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = JsonContent.Create(new StudentResponse(Guid.NewGuid(), "Ada", "Lovelace", null, null, "Grade 5")),
            };
        })));
    }

    [Fact]
    public void Submitting_an_empty_form_shows_required_errors_and_calls_no_api()
    {
        var cut = Render<AddStudent>();

        cut.Find("[data-testid=student-form]").Submit();

        cut.Markup.Should().Contain("First name is required.")
            .And.Contain("Last name is required.")
            .And.Contain("Class is required.");
        _requests.Should().BeEmpty();
    }

    [Fact]
    public void Submitting_a_valid_form_posts_the_student_and_returns_to_the_list()
    {
        var cut = Render<AddStudent>();

        var texts = cut.FindComponents<TextField<string>>();
        cut.InvokeAsync(() => texts[0].Instance.ValueChanged.InvokeAsync("Ada"));
        cut.InvokeAsync(() => texts[1].Instance.ValueChanged.InvokeAsync("Lovelace"));
        cut.InvokeAsync(() => cut.FindComponent<Select<string>>().Instance.ValueChanged.InvokeAsync("Grade 5"));
        cut.Find("[data-testid=student-form]").Submit();

        cut.WaitForAssertion(() => _requests.Should().ContainSingle(r => r.Method == HttpMethod.Post));
        Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>()
            .Uri.Should().EndWith("/students");
    }
}
