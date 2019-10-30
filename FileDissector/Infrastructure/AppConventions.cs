using System;
using System.Collections.Generic;
using System.Linq;
using StructureMap.Configuration.DSL;
using StructureMap.Graph;
using StructureMap.TypeRules;

namespace FileDissector.Infrastructure
{
    public class AppConventions : IRegistrationConvention
    {
        public void Process(Type type, Registry registry)
        {
            // only work on concrete types
            if (!type.IsConcrete() || type.IsGenericType) return;
            
            // register against all interfaces implemented by this concrete class
            type.GetInterfaces()
                .Where(@interface => @interface.Name == $"I{type.Name}")
                .ForEach(@interface => registry.For(@interface).Use(type).Singleton());
        }
    }
}
