using Microsoft.Extensions.DependencyInjection;
using Sis.Contracts.Students;
using Sis.Web.Pages.Students;
using Sis.Web.Services;
using Sis.Web.Tests.Support;
using TailwindUi.Blazor.Extensions;

namespace Sis.Web.Tests.Pages;

public sealed class StudentListTests : BunitContext
{
    public StudentListTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddTailwindUi();
    }

    [Fact]
    public void Lists_the_students_returned_by_the_api()
    {
        UseApi(_ => StubHttp.Json(new[]
        {
            new StudentResponse(Guid.NewGuid(), "Ada", "Lovelace", null, "ada@example.com", "Grade 5"),
        }));

        var cut = Render<StudentList>();

        cut.WaitForAssertion(() =>
            cut.Find("[data-testid=student-list]").TextContent
                .Should().Contain("Ada Lovelace").And.Contain("Grade 5").And.Contain("ada@example.com"));
    }

    [Fact]
    public void Shows_an_empty_state_when_there_are_no_students()
    {
        UseApi(_ => StubHttp.Json(Array.Empty<StudentResponse>()));

        var cut = Render<StudentList>();

        cut.WaitForAssertion(() =>
            cut.Find("[data-testid=student-list]").TextContent.Should().Contain("No students yet"));
    }

    [Fact]
    public void Shows_an_error_when_the_api_is_unreachable()
    {
        UseApi(_ => throw new HttpRequestException("connection refused"));

        var cut = Render<StudentList>();

        cut.WaitForAssertion(() =>
            cut.Find("[data-testid=student-list]").TextContent.Should().Contain("Couldn't load students"));
    }

    private void UseApi(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        Services.AddSingleton(new StudentsClient(StubHttp.Create(respond)));
}
