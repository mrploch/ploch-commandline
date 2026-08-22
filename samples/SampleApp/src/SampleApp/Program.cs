using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ploch.CommandLine.Spectre;
using Ploch.CommandLine.Spectre.FluentValidation;
using Ploch.CommandLine.Spectre.SampleApp.Commands.Common;
using Ploch.CommandLine.Spectre.SampleApp.Commands.Config;
using Ploch.CommandLine.Spectre.SampleApp.Commands.Files;
using Ploch.CommandLine.Spectre.SampleApp.Commands.Projects;
using Ploch.CommandLine.Spectre.SampleApp.Commands.Projects.UseCases;
using Ploch.CommandLine.Spectre.SampleApp.Commands.Users;
using Ploch.CommandLine.Spectre.SampleApp.Services;
using Ploch.CommandLine.Spectre.Serilog;
using Spectre.Console.Cli;

// 1. Create and configure the AppBuilder
var appBuilder = AppBuilder.Create(args)
                           .WithName("Ploch.CommandLine.Spectre Sample CLI")
                           .WithVersion(new Version(1, 0, 0))
                           .WithDescription("Showcase application demonstrating Ploch.CommandLine.Spectre features, " +
                                            "multi-level sub-commands, FluentValidation, tokens, and Clean Architecture use cases.")

                           // The host resolves relative configuration file paths against the current working
                           // directory. A CLI is invoked from wherever the user happens to be, so appsettings.json
                           // is loaded from the directory the application was deployed to instead.
                           .ConfigureAppConfiguration(configuration => configuration.SetBasePath(AppContext.BaseDirectory)
                                                                                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true))
                           .ConfigureServices((context, services) =>
                           {
                               // Serilog reads its minimum level from the "Serilog" section of appsettings.json and
                               // writes to the console plus a rolling log file under the application's log directory.
                               services.AddSerilog(context.Configuration,
                                                   logName: "sample",
                                                   logPath: Path.Combine(AppContext.BaseDirectory, "logs"));

                               // Register domain services & repositories
                               services.AddSingleton<IUserService, UserService>();
                               services.AddSingleton<IProjectRepository, InMemoryProjectRepository>();

                               // Register Clean Architecture use cases
                               services.AddTransient<CreateProjectUseCase>();
                               services.AddTransient<ExportProjectUseCase>();

                               // Register FluentValidation validators
                               services.AddCommandLineSettingsFluentValidation(builder =>
                                   builder.AddAssembly(typeof(Program).Assembly));
                           });

// 2. Configure the composite command structure with multi-level commands and branches
var executor = appBuilder.ConfigureCommandApp(config =>
{
    config.SetApplicationName("sample");

    // Root-level command
    config.AddCommand<InfoCommand>("info")
          .WithDescription("Display system, application, and host runtime information.")
          .WithExample("info")
          .WithExample("info", "-d");

    // Branch: 'user' commands
    config.AddBranch("user", user =>
    {
        user.SetDescription("Manage user accounts and profile data.");

        user.AddCommand<UserAddCommand>("add")
            .WithDescription("Create a new user account with validation.")
            .WithExample("user", "add", "Alice Smith", "-e", "alice@example.com", "-r", "Administrator");

        user.AddCommand<UserListCommand>("list")
            .WithDescription("List registered user accounts in a rich table.")
            .WithExample("user", "list")
            .WithExample("user", "list", "-a", "-f", "compact");

        user.AddCommand<UserDeleteCommand>("delete")
            .WithDescription("Delete a user account by ID.")
            .WithExample("user", "delete", "1", "--force");
    });

    // Branch: 'config' commands
    config.AddBranch("config", cfg =>
    {
        cfg.SetDescription("Inspect and manage application configuration settings.");

        cfg.AddCommand<ConfigGetCommand>("get")
           .WithDescription("Retrieve a configuration value by key.")
           .WithExample("config", "get", "SampleAppSettings:Environment");

        cfg.AddCommand<ConfigSetCommand>("set")
           .WithDescription("Update a configuration value in memory.")
           .WithExample("config", "set", "SampleAppSettings:MaxBatchSize", "250");

        cfg.AddCommand<ConfigShowCommand>("show")
           .WithDescription("Display configuration hierarchy as a formatted tree.")
           .WithExample("config", "show")
           .WithExample("config", "show", "-s", "SampleAppSettings");
    });

    // Branch: 'file' commands (with token replacement)
    config.AddBranch("file", file =>
    {
        file.SetDescription("File processing and report generation utilities.");

        file.AddCommand<FileProcessCommand>("process")
            .WithDescription("Process a file with automatic '{date}' and '{datetime}' token resolution.")
            .WithExample("file", "process", "input.csv", "-o", "./output-{date}/data.dat");

        file.AddCommand<FileReportCommand>("report")
            .WithDescription("Generate analysis report for a file.")
            .WithExample("file", "report", "input.csv", "-t", "Daily Report - {date}");
    });

    // Branch: 'project' commands (with Clean Architecture UseCases)
    config.AddBranch("project", proj =>
    {
        proj.SetDescription("Project operations powered by Clean Architecture use cases and Ardalis.Result.");

        proj.AddCommand<ProjectCreateCommand>("create")
            .WithDescription("Create a new project using the CreateProject use case.")
            .WithExample("project", "create", "NewApp", "-d", "My new application", "-t", "Console");

        proj.AddCommand<ProjectExportCommand>("export")
            .WithDescription("Export a project bundle using the ExportProject use case.")
            .WithExample("project", "export", "SpectreDemo", "-o", "./exports-{date}");
    });
});

// 3. Execute the command-line application
return executor.Run(args);
