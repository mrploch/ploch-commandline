using Moq;
using Ploch.CommandLine.Spectre.Tests.Testing;
using Spectre.Console.Cli;

namespace Ploch.CommandLine.Spectre.Tests;

/// <summary>
///     Cover for the thin configurator that adapts <see cref="ICommandApp" /> to
///     <see cref="ICommandAppConfigurator" />.
/// </summary>
[Collection(GlobalConsoleState.Name)]
public sealed class CommandAppConfiguratorTests : IDisposable
{
    public void Dispose()
    {
        EnvironmentSettings.Reset();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Configure_should_forward_the_configuration_action_to_the_command_app()
    {
        var commandApp = new Mock<ICommandApp>();
        Action<IConfigurator> configuration = _ => { };

        new CommandAppConfigurator(commandApp.Object, CancellationToken.None).Configure(configuration);

        commandApp.Verify(app => app.Configure(configuration), Times.Once);
    }

    [Fact]
    public void Configure_should_return_an_executor_bound_to_the_same_command_app()
    {
        var commandApp = new Mock<ICommandApp>();
        commandApp.Setup(app => app.Run(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).Returns(11);
        EnvironmentSettings.Current = new EnvironmentSettings(isDebugging: false, pauseBeforeExit: false, new Dictionary<string, string?>());

        var executor = new CommandAppConfigurator(commandApp.Object, CancellationToken.None).Configure(_ => { });

        executor.Run("anything").Should().Be(11, "the executor must run the command app that was configured");
    }

    [Fact]
    public void Configure_should_hand_the_executor_the_token_it_was_built_with()
    {
        var commandApp = new Mock<ICommandApp>();
        CancellationToken received = default;
        commandApp.Setup(app => app.Run(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                  .Callback<IEnumerable<string>, CancellationToken>((_, token) => received = token)
                  .Returns(0);
        EnvironmentSettings.Current = new EnvironmentSettings(isDebugging: false, pauseBeforeExit: false, new Dictionary<string, string?>());
        using var cancellationTokenSource = new CancellationTokenSource();

        new CommandAppConfigurator(commandApp.Object, cancellationTokenSource.Token).Configure(_ => { }).Run("build");

        // Asserting on the token's own behaviour rather than on It.IsAny<CancellationToken>(): a default token
        // satisfies that matcher just as well, so a configurator that dropped its token and built the executor
        // with CancellationToken.None would pass every other test in this class.
        received.CanBeCanceled.Should().BeTrue("an executor built with a default token makes cancellation inert");
        received.IsCancellationRequested.Should().BeFalse();

        cancellationTokenSource.Cancel();

        received.IsCancellationRequested
                .Should()
                .BeTrue("the configurator must pass the token it was constructed with to the executor it builds");
    }

    [Fact]
    public void Configure_should_reject_a_null_configuration_action()
    {
        var configurator = new CommandAppConfigurator(Mock.Of<ICommandApp>(), CancellationToken.None);

        var act = () => configurator.Configure(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
