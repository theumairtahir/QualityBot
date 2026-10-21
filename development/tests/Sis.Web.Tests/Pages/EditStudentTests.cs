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

public sealed class EditStudentTests : BunitContext
{
    private static readonly StudentResponse Ada =
        new(Guid.Parse("11111111-1111-1111-1111-111111111111"), 7, "Ada", "Lovelace", null, null, "Grade 5");

    private readonly List<HttpRequestMessage> _requests = [];

    public EditStudentTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddTailwindUi();
        Services.AddSingleton(TimeProvider.System);
    }

    [Fact]
    public void Prefills_the_form_with_the_students_current_details()
    {
        UseApi(_ => StubHttp.Json(Ada));

        var cut = Render<EditStudent>(p => p.Add(c => c.Id, Ada.Id));

        cut.WaitForAssertion(() =>
        {
            var texts = cut.FindComponents<TextField<string>>();
            texts[0].Instance.Value.Should().Be("Ada");
            texts[1].Instance.Value.Should().Be("Lovelace");
            cut.FindComponent<Select<string>>().Instance.Value.Should().Be("Grade 5");
        });
    }

    [Fact]
    public void Saving_sends_the_changes_to_the_students_record()
    {
        UseApi(request => request.Method == HttpMethod.Put ? StubHttp.Json(Ada with { LastName = "Byron" }) : StubHttp.Json(Ada));
        var cut = Render<EditStudent>(p => p.Add(c => c.Id, Ada.Id));
        cut.WaitForElement("[data-testid=student-form]");
        cut.InvokeAsync(() => cut.FindComponents<TextField<string>>()[1].Instance.ValueChanged.InvokeAsync("Byron"));

        cut.Find("[data-testid=student-form]").Submit();

        cut.WaitForAssertion(() =>
        {
            var put = _requests.Should().ContainSingle(r => r.Method == HttpMethod.Put).Subject;
            put.RequestUri!.AbsolutePath.Should().EndWith($"/api/students/{Ada.Id}");
        });
    }

    [Fact]
    public void Applies_the_same_validation_as_adding_a_student()
    {
        UseApi(_ => StubHttp.Json(Ada));
        var cut = Render<EditStudent>(p => p.Add(c => c.Id, Ada.Id));
        cut.WaitForElement("[data-testid=student-form]");
        cut.InvokeAsync(() => cut.FindComponents<TextField<string>>()[0].Instance.ValueChanged.InvokeAsync("Ad4"));

        cut.Find("[data-testid=student-form]").Submit();

        cut.Markup.Should().Contain("First name can only contain letters");
        _requests.Should().NotContain(r => r.Method == HttpMethod.Put);
    }

    [Fact]
    public void Shows_student_not_found_when_the_student_no_longer_exists()
    {
        UseApi(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var cut = Render<EditStudent>(p => p.Add(c => c.Id, Ada.Id));

        cut.WaitForAssertion(() =>
            cut.Find("[data-testid=student-not-found]").TextContent.Should().Contain("Student not found"));
    }

    private void UseApi(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        Services.AddSingleton(new StudentsClient(StubHttp.Create(request =>
        {
            _requests.Add(request);
            return respond(request);
        })));
}
