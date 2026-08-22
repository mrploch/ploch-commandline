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

        new CommandAppConfigurator(commandApp.Object).Configure(configuration);

        commandApp.Verify(app => app.Configure(configuration), Times.Once);
    }

    [Fact]
    public void Configure_should_return_an_executor_bound_to_the_same_command_app()
    {
        var commandApp = new Mock<ICommandApp>();
        commandApp.Setup(app => app.Run(It.IsAny<IEnumerable<string>>())).Returns(11);
        EnvironmentSettings.Current = new EnvironmentSettings(isDebugging: false, pauseBeforeExit: false, new Dictionary<string, string?>());

        var executor = new CommandAppConfigurator(commandApp.Object).Configure(_ => { });

        executor.Run("anything").Should().Be(11, "the executor must run the command app that was configured");
    }

    [Fact]
    public void Configure_should_reject_a_null_configuration_action()
    {
        var configurator = new CommandAppConfigurator(Mock.Of<ICommandApp>());

        var act = () => configurator.Configure(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
