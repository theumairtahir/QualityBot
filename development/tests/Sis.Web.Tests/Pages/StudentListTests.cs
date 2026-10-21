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

public sealed class StudentListTests : BunitContext, IAsyncLifetime
{
    private static readonly StudentResponse Ada =
        new(Guid.Parse("11111111-1111-1111-1111-111111111111"), 7, "Ada", "Lovelace", null, "ada@example.com", "Grade 5");

    private readonly List<HttpRequestMessage> _requests = [];

    public StudentListTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddTailwindUi();
    }

    // The Modal's JS service is async-disposable only, which bUnit's synchronous teardown rejects.
    public Task InitializeAsync() => Task.CompletedTask;

    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void Lists_the_students_with_roll_number_name_and_class()
    {
        UseApi(_ => StubHttp.Json(new[] { Ada }));

        var cut = Render<StudentList>();

        cut.WaitForAssertion(() =>
            cut.Find("[data-testid=student-list]").TextContent
                .Should().Contain("7").And.Contain("Ada Lovelace").And.Contain("Grade 5").And.Contain("ada@example.com"));
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

    [Fact]
    public void Searching_asks_the_api_for_matching_students()
    {
        UseApi(request => StubHttp.Json(request.RequestUri!.Query.Contains("search=love")
            ? new[] { Ada }
            : Array.Empty<StudentResponse>()));
        var cut = Render<StudentList>();
        cut.WaitForAssertion(() => cut.Find("[data-testid=student-list]").TextContent.Should().Contain("No students yet"));

        Search(cut, "love");

        cut.WaitForAssertion(() =>
            cut.Find("[data-testid=student-list]").TextContent.Should().Contain("Ada Lovelace"));
        _requests.Last().RequestUri!.Query.Should().Be("?search=love");
    }

    [Fact]
    public void Shows_no_students_found_when_the_search_matches_nothing()
    {
        UseApi(_ => StubHttp.Json(Array.Empty<StudentResponse>()));
        var cut = Render<StudentList>();

        Search(cut, "zzz");

        cut.WaitForAssertion(() =>
            cut.Find("[data-testid=student-list]").TextContent.Should().Contain("No students found"));
    }

    [Fact]
    public void Removing_a_student_asks_for_confirmation_and_sends_nothing_until_confirmed()
    {
        UseApi(_ => StubHttp.Json(new[] { Ada }));
        var cut = Render<StudentList>();
        cut.WaitForElement("[data-testid=remove-student]").Click();

        cut.Find("[data-testid=remove-confirmation]").TextContent.Should().Contain("Ada Lovelace");
        _requests.Should().NotContain(r => r.Method == HttpMethod.Delete);
    }

    [Fact]
    public void Confirming_the_removal_deletes_the_student_and_refreshes_the_list()
    {
        var removed = false;
        UseApi(request =>
        {
            if (request.Method == HttpMethod.Delete)
            {
                removed = true;
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            return StubHttp.Json(removed ? Array.Empty<StudentResponse>() : new[] { Ada });
        });
        var cut = Render<StudentList>();
        cut.WaitForElement("[data-testid=remove-student]").Click();

        cut.Find("[data-testid=confirm-remove]").Click();

        cut.WaitForAssertion(() =>
        {
            _requests.Should().ContainSingle(r => r.Method == HttpMethod.Delete)
                .Which.RequestUri!.AbsolutePath.Should().EndWith($"/api/students/{Ada.Id}");
            cut.Find("[data-testid=student-list]").TextContent.Should().Contain("No students yet");
        });
    }

    [Fact]
    public void Cancelling_the_removal_keeps_the_student()
    {
        UseApi(_ => StubHttp.Json(new[] { Ada }));
        var cut = Render<StudentList>();
        cut.WaitForElement("[data-testid=remove-student]").Click();

        cut.Find("[data-testid=cancel-remove]").Click();

        _requests.Should().NotContain(r => r.Method == HttpMethod.Delete);
        cut.Find("[data-testid=student-list]").TextContent.Should().Contain("Ada Lovelace");
    }

    // The library repeats the Id on a wrapper div, so target the input itself.
    private static void Search(IRenderedComponent<StudentList> cut, string text) =>
        cut.Find("input#student-search").Input(text);

    private void UseApi(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        Services.AddSingleton(new StudentsClient(StubHttp.Create(request =>
        {
            _requests.Add(request);
            return respond(request);
        })));
}
