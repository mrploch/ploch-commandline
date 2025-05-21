using BasicConsoleApp;
using AdvancedFeaturesSample;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ploch.Common.CommandLine;
using Ploch.Common.CommandLine.Autofac;

// Demonstrates verbs with verbs using commands
await AppBuilder.CreateDefault(new CommandAppProperties("MyTestApp", "My App Description"), cfg => cfg.AddJsonFile("appsettings.json"))
                .UseAutofac()
                .Configure(container =>
                           {
                               container.Services.AddSingleton<SampleRootCommand>()
                                        .AddSingleton<SampleChildCommand>()
                                        .AddSingleton<ISampleService, SampleHelloWorldService>();
                               container.Application.Command<SampleRootCommand>(app =>
                                                                           {
                                                                               app.Command<SampleChildCommand>();
                                                                           });
                           })
                .Build()
                .ExecuteAsync(args);