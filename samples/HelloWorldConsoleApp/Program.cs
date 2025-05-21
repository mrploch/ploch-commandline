using HelloWorldConsoleApp;
using Ploch.Common.CommandLine;

var app = AppBuilder.CreateDefault("Hello World App", "A sample app").Build();
app.Command<HellowWorldCommand>();