using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace NPhies_FHIR_Integration.Tests.Nphies.Rcm;

[TestClass]
public class RouteUniquenessTests
{
    [TestMethod]
    public void ControllerRoutes_AreNotAmbiguous()
    {
        var assembly = typeof(NPhies_FHIR_Integration.ApiService.Controllers.AuthController).Assembly;
        var controllers = assembly.GetTypes()
            .Where(t => !t.IsAbstract && typeof(ControllerBase).IsAssignableFrom(t));

        var seen = new Dictionary<string, string>();
        var duplicates = new List<string>();

        foreach (var controller in controllers)
        {
            var name = controller.Name.EndsWith("Controller") ? controller.Name[..^"Controller".Length] : controller.Name;
            var prefixes = controller.GetCustomAttributes<RouteAttribute>(true).Select(r => r.Template).DefaultIfEmpty("").ToList();

            foreach (var action in controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                foreach (var http in action.GetCustomAttributes<HttpMethodAttribute>(true))
                {
                    foreach (var prefix in prefixes)
                    {
                        var template = http.Template is { } t && t.StartsWith('/') ? t : $"{prefix}/{http.Template}";
                        template = Normalize(template.Replace("[controller]", name).Replace("[action]", action.Name));
                        foreach (var method in http.HttpMethods)
                        {
                            var key = $"{method} {template}";
                            var owner = $"{controller.Name}.{action.Name}";
                            if (seen.TryGetValue(key, out var existing))
                                duplicates.Add($"{key}: {existing} vs {owner}");
                            else
                                seen[key] = owner;
                        }
                    }
                }
            }
        }

        Assert.AreEqual(0, duplicates.Count, "Ambiguous routes:\n" + string.Join("\n", duplicates));
    }

    private static string Normalize(string template)
    {
        template = Regex.Replace(template, @"\{[^}]*\}", "{}");
        return "/" + string.Join('/', template.ToLowerInvariant().Split('/', StringSplitOptions.RemoveEmptyEntries));
    }
}
