using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Arfilon.TaaS.Providers
{

     class ApplicationPartManagerProvider : ITenantServiceProvider<ApplicationPartManager>
    {
        private readonly ApplicationPartManager parts;

        public ApplicationPartManagerProvider(IWebHostEnvironment environment)
        {

            parts = new ApplicationPartManager();

          
            var entryAssemblyName = environment?.ApplicationName;



            var assemblies = GetApplicationPartAssemblies(entryAssemblyName);

            var seenAssemblies = new HashSet<Assembly>();

            foreach (var assembly in assemblies)
            {
                if (!seenAssemblies.Add(assembly))
                {
                    // "assemblies" may contain duplicate values, but we want unique ApplicationPart instances.
                    // Note that we prefer using a HashSet over Distinct since the latter isn't
                    // guaranteed to preserve the original ordering.
                    continue;
                }

                var partProvider = ApplicationPartFactory.GetApplicationPartFactory(assembly);
                foreach (var applicationPart in partProvider.GetApplicationParts(assembly))
                {
                   parts.ApplicationParts.Add(applicationPart);
                }
            }
        }

        ApplicationPartManager ITenantServiceProvider<ApplicationPartManager>.GetService(TenantKey tenant)
        {
            return parts;
        }

        private static IEnumerable<Assembly> GetApplicationPartAssemblies(string entryAssemblyName)
        {
            var entryAssembly = Assembly.Load(new AssemblyName(entryAssemblyName));

            var assembliesFromAttributes = entryAssembly.GetCustomAttributes<ApplicationPartAttribute>()
                .Select(name => Assembly.Load(name.AssemblyName))
                .OrderBy(assembly => assembly.FullName, StringComparer.Ordinal)
                .SelectMany(GetAsemblyClosure);

            return GetAsemblyClosure(entryAssembly)
                .Concat(assembliesFromAttributes);
        }

        private static IEnumerable<Assembly> GetAsemblyClosure(Assembly assembly)
        {
            yield return assembly;

            var relatedAssemblies = RelatedAssemblyAttribute.GetRelatedAssemblies(assembly, throwOnError: false)
                .OrderBy(assembly => assembly.FullName, StringComparer.Ordinal);

            foreach (var relatedAssembly in relatedAssemblies)
            {
                yield return relatedAssembly;
            }
        }

    }
}
