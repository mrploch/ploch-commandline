using System.Globalization;
using Ardalis.Result;
using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Output;
using Ploch.TestingSupport.XUnit3.AutoMoq;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;

namespace Ploch.CommandLine.UseCases.Tests;

/// <summary>
///     Cover for the command base that bridges Spectre.Console.Cli to a use case. Its job is to build the request
///     from the validated settings, forward the cancellation token, and map the <see cref="Result{T}" /> the use
///     case returns onto an exit code.
/// </summary>
public class UseCaseAsyncCommandTests
{
    [Theory]
    [AutoMockData]
    public async Task ExecuteAsync_should_return_success_and_report_completion_when_the_use_case_succeeds(CommandContext context)
    {
        var output = new RecordingOutput();
        var command = CreateCommand(output, _ => Result<string>.Success("done"));

        var result = await command.ExecuteAsync(context, new StubSettings(), CancellationToken.None);

        result.Should().Be((int)ExitCode.Success);
        output.Written.Should().ContainMatch("*Use case completed successfully*");
    }

    [Theory]
    [AutoMockData]
    public async Task ExecuteAsync_should_return_error_and_report_the_errors_when_the_use_case_fails(CommandContext context)
    {
        var output = new RecordingOutput();
        var command = CreateCommand(output, _ => Result<string>.Error("the widget jammed"));

        var result = await command.ExecuteAsync(context, new StubSettings(), CancellationToken.None);

        result.Should().Be((int)ExitCode.Error);
        output.Written.Should().ContainMatch("*Use case failed*the widget jammed*");
        output.Written.Should().NotContainMatch("*completed successfully*");
    }

    [Theory]
    [AutoMockData]
    public async Task ExecuteAsync_should_return_error_when_the_use_case_reports_not_found(CommandContext context)
    {
        var output = new RecordingOutput();
        var command = CreateCommand(output, _ => Result<string>.NotFound());

        var result = await command.ExecuteAsync(context, new StubSettings(), CancellationToken.None);

        result.Should().Be((int)ExitCode.Error, "any unsuccessful result maps onto the failure path");
    }

    [Theory]
    [AutoMockData]
    public async Task ExecuteAsync_should_pass_the_request_built_from_the_settings_to_the_use_case(CommandContext context)
    {
        string? receivedRequest = null;
        var command = CreateCommand(new RecordingOutput(),
                                    request =>
                                    {
                                        receivedRequest = request;

                                        return Result<string>.Success("done");
                                    });

        await command.ExecuteAsync(context, new StubSettings { Target = "widget-42" }, CancellationToken.None);

        receivedRequest.Should().Be("request-for-widget-42", "CreateRequest turns the settings into the use case request");
    }

    [Theory]
    [AutoMockData]
    public async Task ExecuteAsync_should_forward_the_cancellation_token_to_the_use_case(CommandContext context)
    {
        var useCase = new StubUseCase(_ => Result<string>.Success("done"));
        var command = CreateCommand(new RecordingOutput(), useCase);
        using var cancellationTokenSource = new CancellationTokenSource();

        await command.ExecuteAsync(context, new StubSettings(), cancellationTokenSource.Token);

        useCase.ReceivedToken.Should().Be(cancellationTokenSource.Token, "the use case has to be able to honour cancellation");
    }

    [Theory]
    [AutoMockData]
    public async Task ExecuteAsync_should_name_the_use_case_before_running_it(CommandContext context)
    {
        var output = new RecordingOutput();
        var command = CreateCommand(output, _ => Result<string>.Success("done"));

        await command.ExecuteAsync(context, new StubSettings { Target = "widget-42" }, CancellationToken.None);

        output.Written.Should().ContainMatch($"*Starting use case*{nameof(StubUseCase)}*", "the progress line names the use case being run");
    }

    [Theory]
    [AutoMockData]
    public async Task ExecuteAsync_should_let_an_overridden_success_handler_choose_the_exit_code(CommandContext context)
    {
        var command = new CustomExitCodeCommand(new RecordingOutput(), new StubUseCase(_ => Result<string>.Success("done")));

        var result = await command.ExecuteAsync(context, new StubSettings(), CancellationToken.None);

        result.Should().Be((int)ExitCode.InvalidInput, "ProcessSuccessResponse is a virtual extension point");
    }

    /// <summary>
    ///     The echo prints every public settings property indiscriminately, and a derived command is free to add a
    ///     password or API token as an option. Since this is a library base class the consumer never opted in, so the
    ///     default has to be silence.
    /// </summary>
    [Theory]
    [AutoMockData]
    public async Task ExecuteAsync_should_not_echo_the_settings_by_default(CommandContext context)
    {
        var output = new RecordingOutput();
        var command = CreateCommand(output, _ => Result<string>.Success("done"));

        await command.ExecuteAsync(context, new StubSettings { Target = "s3cr3t-token" }, CancellationToken.None);

        output.Written.Should().NotContainMatch("*s3cr3t-token*", "settings values must not be disclosed unless the command opts in");
        output.Written.Should().NotContainMatch("*Settings:*");
    }

    [Theory]
    [AutoMockData]
    public async Task ExecuteAsync_should_echo_the_settings_when_the_command_opts_in(CommandContext context)
    {
        var output = new RecordingOutput();
        var command = new EchoingCommand(output, new StubUseCase(_ => Result<string>.Success("done")));

        await command.ExecuteAsync(context, new StubSettings { Target = "widget-42" }, CancellationToken.None);

        output.Written.Should().ContainMatch("*Settings:*");
        output.Written.Should().ContainMatch("*Target*widget-42*", "a command that opts in still gets the diagnostic");
    }

    // Spectre.Console escapes only holes whose runtime value is a string. The echo passed each value as it was, so
    // a value of any other type whose text contained '[' - here a value object - was parsed as markup and threw; a
    // string value was already safe. This goes through the real output adapter and a real console, so the escaping
    // is exercised rather than simulated.
    [Theory]
    [AutoMockData]
    public async Task ExecuteAsync_should_echo_a_non_string_settings_value_containing_a_bracket_verbatim_when_the_command_opts_in(CommandContext context)
    {
        using var writer = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
                                         {
                                             Ansi = AnsiSupport.No,
                                             ColorSystem = ColorSystemSupport.NoColors,
                                             Out = new AnsiConsoleOutput(writer)
                                         });
        var output = new AnsiConsoleMarkupOutput(console, new MessageFormatterProcessor([], []));
        var command = new EchoingCommand(output, new StubUseCase(_ => Result<string>.Success("done")));

        var settings = new StubSettings { Target = "Widget [Dev]", Tag = new BracketedTag() };
        settings.Tag!.ToString().Should().Be("[Dev]", "the value object's text must look like a style tag for the test to mean anything");

        var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

        result.Should().Be((int)ExitCode.Success);

        // Compared line by line after whitespace normalisation: the console renderer pads and wraps per platform,
        // and the character codes in the failure message tell an invisible difference from a missing line.
        var lines = RenderedLines(writer.ToString());
        lines.Should().Contain("Tag: [Dev]", "a non-string value is converted to a string hole, which Spectre escapes; rendered: {0}", DescribeCodes(lines));
        lines.Should().Contain("Target: Widget [Dev]", "a string value stays escaped as before; rendered: {0}", DescribeCodes(lines));
    }

    /// <summary>
    ///     Result.Invalid puts its messages in ValidationErrors and leaves Errors empty, so rendering only Errors
    ///     printed "Use case failed:" with nothing after it - the user was told it failed but not why.
    /// </summary>
    [Theory]
    [AutoMockData]
    public async Task ExecuteAsync_should_report_validation_errors_from_an_invalid_result(CommandContext context)
    {
        var output = new RecordingOutput();
        var command = CreateCommand(output,
                                    _ => Result<string>.Invalid(new ValidationError { Identifier = "Name", ErrorMessage = "Name is unusable" }));

        var result = await command.ExecuteAsync(context, new StubSettings(), CancellationToken.None);

        result.Should().Be((int)ExitCode.Error);
        output.Written.Should().ContainMatch("*Use case failed*Name is unusable*", "an Invalid result carries its reason in ValidationErrors");
    }

    /// <summary>
    ///     A ValidationError is not obliged to carry an ErrorMessage. Naming the offending field beats dropping the
    ///     error and falling back to the bare status.
    /// </summary>
    [Theory]
    [AutoMockData]
    public async Task ExecuteAsync_should_fall_back_to_the_identifier_when_a_validation_error_has_no_message(CommandContext context)
    {
        var output = new RecordingOutput();
        var command = CreateCommand(output, _ => Result<string>.Invalid(new ValidationError { Identifier = "DestinationPath" }));

        await command.ExecuteAsync(context, new StubSettings(), CancellationToken.None);

        output.Written.Should().ContainMatch("*DestinationPath*", "the identifier is the only thing the error carries");
        output.Written.Should().NotContainMatch("*Use case failed: Invalid*", "the status is a last resort, not a substitute for a known field");
    }

    [Theory]
    [AutoMockData]
    public async Task ExecuteAsync_should_report_the_status_when_a_failure_carries_no_message(CommandContext context)
    {
        var output = new RecordingOutput();
        var command = CreateCommand(output, _ => Result<string>.Invalid());

        var result = await command.ExecuteAsync(context, new StubSettings(), CancellationToken.None);

        result.Should().Be((int)ExitCode.Error);
        output.Written.Should().ContainMatch("*Use case failed*Invalid*", "a failure with no message must still say what happened");
        output.Written.Should().NotContainMatch("*Use case failed: [/]*", "an empty reason is not an explanation");
    }

    private static StubUseCaseCommand CreateCommand(RecordingOutput output, Func<string, Result<string>> execute) =>
        CreateCommand(output, new StubUseCase(execute));

    private static StubUseCaseCommand CreateCommand(RecordingOutput output, StubUseCase useCase) => new(output, useCase);

    /// <summary>Splits console output into lines with surrounding and repeated whitespace collapsed.</summary>
    private static List<string> RenderedLines(string rendered) =>
        rendered.Split('\n')
                .Select(line => string.Join(' ', line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)))
                .Where(line => line.Length > 0)
                .ToList();

    /// <summary>Lists each line with the code of every character, so an invisible difference shows up in a failure message.</summary>
    private static string DescribeCodes(IEnumerable<string> lines) =>
        string.Join(" | ", lines.Select(line => line + " = " + string.Join(" ", line.Select(c => ((int)c).ToString("x4", CultureInfo.InvariantCulture)))));

    private sealed class StubSettings : CommandSettings
    {
        public string Target { get; init; } = "default-target";

        /// <summary>A non-string value, the kind Spectre.Console's interpolation does not escape on its own.</summary>
        public BracketedTag? Tag { get; init; }
    }

    /// <summary>A value object whose text looks like a markup tag.</summary>
    private sealed class BracketedTag
    {
        public override string ToString() => "[Dev]";
    }

    private sealed class StubUseCase(Func<string, Result<string>> execute) : IResultUseCase<string, string>
    {
        public CancellationToken ReceivedToken { get; private set; }

        public Task<Result<string>> ExecuteAsync(string request, CancellationToken cancellationToken = default)
        {
            ReceivedToken = cancellationToken;

            return Task.FromResult(execute(request));
        }
    }

    private class StubUseCaseCommand(IOutput output, StubUseCase useCase)
        : UseCaseAsyncCommand<StubSettings, StubUseCase, string, string>(output,
                                                                         useCase,
                                                                         new CommandArgumentsRootProcessor([]),
                                                                         new PassThroughValidator(),
                                                                         new ThrowingExceptionHandler())
    {
        protected override string CreateRequest(StubSettings commandSettings) => $"request-for-{commandSettings.Target}";
    }

    /// <summary>A command that opts the settings echo back on, the way a consumer with non-sensitive settings would.</summary>
    private sealed class EchoingCommand(IOutput output, StubUseCase useCase) : StubUseCaseCommand(output, useCase)
    {
        protected override bool EchoSettings => true;
    }

    private sealed class CustomExitCodeCommand(RecordingOutput output, StubUseCase useCase) : StubUseCaseCommand(output, useCase)
    {
        protected override ExitCode ProcessSuccessResponse(Result<string> result) => ExitCode.InvalidInput;
    }

    private sealed class PassThroughValidator : ICommandSettingsValidator<StubSettings>
    {
        public ValidationResult Validate(CommandContext context, StubSettings settings) => ValidationResult.Success();
    }

    /// <summary>Fails the test loudly rather than swallowing an unexpected exception into an exit code.</summary>
    private sealed class ThrowingExceptionHandler : IExceptionHandler
    {
        public int HandleException(Exception ex) => throw new InvalidOperationException("The command raised an unexpected exception.", ex);
    }

    /// <summary>Captures the rendered text so the command's reporting can be asserted without a console.</summary>
    private sealed class RecordingOutput : IOutput
    {
        public List<string> Written { get; } = [];

        public IOutput EndLine() => this;

        public IOutput Markup(string value) => Record(value);

        public IOutput MarkupLine(string value) => Record(value);

        public IOutput MarkupInterpolated(FormattableString value) => Record(value.ToString(CultureInfo.InvariantCulture));

        public IOutput MarkupLineInterpolated(FormattableString value) => Record(value.ToString(CultureInfo.InvariantCulture));

        public IOutput Write(IRenderable renderable) => this;

        public IOutput Write<TMessage>(TMessage message, IFormatProvider? format = null) => Record(message?.ToString());

        public IOutput WriteBold<TMessage>(TMessage? message) => Record(message?.ToString());

        public IOutput WriteBoldLine<TMessage>(TMessage? message) => Record(message?.ToString());

        public IOutput WriteError<TMessage>(TMessage? message) => Record(message?.ToString());

        public IOutput WriteErrorLine<TMessage>(TMessage? message) => Record(message?.ToString());

        public IOutput WriteException<TException>(TException? exception) where TException : Exception => Record(exception?.Message);

        public IOutput WriteLine() => this;

        public IOutput WriteLine<TMessage>(TMessage message) => Record(message?.ToString());

        private RecordingOutput Record(string? text)
        {
            if (text is not null)
            {
                Written.Add(text);
            }

            return this;
        }
    }
}
