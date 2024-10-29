using ConsoleApp1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ploch.Common.CommandLine;
using Ploch.Common.CommandLine.Autofac;

// Demonstrates verbs with verbs using commands
await AppBuilder.CreateDefault(new CommandAppProperties("MyTestApp", "My App Description"), cfg => cfg.AddJsonFile("appsettings.json"))
                .UseAutofac()
                .Configure(container =>
                           {
                               container.Services.AddSingleton<RootCommand1>()
                                        .AddSingleton<ChildCommand1>()
                                        .AddSingleton<ISomeInterface, SomeClass>();
                               container.Application.Command<RootCommand1>(app =>
                                                                           {
                                                                               app.Command<ChildCommand1>();
                                                                           });
                           })
                .Build()
                .ExecuteAsync(args);