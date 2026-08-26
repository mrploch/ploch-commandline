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

    private static StubUseCaseCommand CreateCommand(RecordingOutput output, Func<string, Result<string>> execute) =>
        CreateCommand(output, new StubUseCase(execute));

    private static StubUseCaseCommand CreateCommand(RecordingOutput output, StubUseCase useCase) => new(output, useCase);

    private sealed class StubSettings : CommandSettings
    {
        public string Target { get; init; } = "default-target";
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

    private class StubUseCaseCommand(RecordingOutput output, StubUseCase useCase)
        : UseCaseAsyncCommand<StubSettings, StubUseCase, string, string>(output,
                                                                         useCase,
                                                                         new CommandArgumentsRootProcessor([]),
                                                                         new PassThroughValidator(),
                                                                         new ThrowingExceptionHandler())
    {
        protected override string CreateRequest(StubSettings commandSettings) => $"request-for-{commandSettings.Target}";
    }

    /// <summary>A command that opts the settings echo back on, the way a consumer with non-sensitive settings would.</summary>
    private sealed class EchoingCommand(RecordingOutput output, StubUseCase useCase) : StubUseCaseCommand(output, useCase)
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
