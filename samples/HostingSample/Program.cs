using Autofac.Core;
using Autofac.Core.Registration;
using HostingSample;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
//builder.ConfigureContainer(new AutofacServiceProviderFactory(), collection => collection.RegisterModule<Worker>);

var host = builder.Build();
host.Run();

public class AutofacBuilder : IModule
{
    public AutofacBuilder()
    {
        throw new NotImplementedException();
    }

    public void Configure(IComponentRegistryBuilder componentRegistry)
    {
        // componentRegistry.AddRegistrationSource(new Se);
    }
}