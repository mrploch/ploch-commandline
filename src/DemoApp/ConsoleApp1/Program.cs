// See https://aka.ms/new-console-template for more information

using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ploch.Common.CommandLine;
using Ploch.Common.CommandLine.Autofac;

Console.WriteLine("Hello, World!");

// Demonstrates verbs with verbs using commands
await AppBuilder.CreateDefault(cfg => cfg.AddJsonFile("appsettings.json"))
    .UseAutofac()
    .Configure(container =>
    {
        container.ServiceCollection.AddSingleton<RootCommand1>()
            .AddSingleton<ChildCommand1>()
            .AddSingleton<ISomeInterface, SomeClass>();
        container.Application.Command<RootCommand1>(app =>
        {
            app.Command<ChildCommand1>();
        });
    })
    .Build()
    .ExecuteAsync(args);
