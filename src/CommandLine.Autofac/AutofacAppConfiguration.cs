using Autofac;
using Autofac.Extensions.DependencyInjection;
using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.Conventions;
using Microsoft.Extensions.DependencyInjection;

namespace Ploch.Common.CommandLine.Autofac
{
    public static class AutofacAppConfiguration
    {
        public static AppBuilder UseAutofac(this AppBuilder builder, ContainerBuilder? containerBuilder = null)
        {
            builder.Configure(container => container.ServiceProviderFactory = () =>
                                                                              {
                                                                                               containerBuilder ??= new ContainerBuilder();
                                                                                               containerBuilder.Populate(container.ServiceCollection);
                                                                                               return new AutofacServiceProvider(containerBuilder.Build());
                                                                                           });
             
            return builder;
        }
    }
}