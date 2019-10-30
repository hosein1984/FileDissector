using System;
using System.IO;
using FileDissector.Domain.Infrastructure;
using StructureMap.Configuration.DSL;

namespace FileDissector.Infrastructure
{
    public class AppRegistry : Registry
    {
        public AppRegistry()
        {
            // setup loggin
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log4Net.config");
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("The log4net.config file was not found");
            }

            log4net.Config.XmlConfigurator.ConfigureAndWatch(new FileInfo(path));
            For<ILogger>().Use<Log4NetLogger>().Ctor<Type>("type").Is(x => x.RootType).AlwaysUnique();

            Scan(scanner =>
            {
                scanner.ExcludeType<ILogger>();
                scanner.LookForRegistries();
                scanner.Convention<AppConventions>();
                scanner.AssemblyContainingType<AppRegistry>();
            });
        }
    }
}
