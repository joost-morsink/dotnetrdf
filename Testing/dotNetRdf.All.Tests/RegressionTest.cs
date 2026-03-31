using FluentAssertions;
using System.Reflection;
using VDS.RDF;
using VDS.RDF.Data.DataTables;
using VDS.RDF.Dynamic;
using VDS.RDF.LDF;
using VDS.RDF.Ontology;
using VDS.RDF.Query;
using VDS.RDF.Query.Pull;
using VDS.RDF.Query.Spin;
using VDS.RDF.Shacl;
using VDS.RDF.Skos;
using VDS.RDF.Storage;
using VDS.RDF.Writing;

namespace dotNetRdf.All.Tests;

public class RegressionTest
{
    [Fact(DisplayName = "`System.Uri` should not be used in public APIs.")]
    public void SystemUriShouldNotBeUsedInPublicApis()
    {
        var allAssemblies = new[]
        {
            typeof(FusekiConnector),
            typeof(StringExtensions),
            typeof(DataTableHandler),
            typeof(DynamicGraph),
            typeof(InferencingTripleStore),
            typeof(LdfException),
            typeof(Ontology),
            typeof(FullTextHelper),
            typeof(PullQueryOptions),
            typeof(SpinWrappedDataset),
            typeof(ShapesGraph),
            typeof(SkosGraph),
            typeof(HtmlSchemaWriter)
        }.Select(t => t.Assembly).Distinct().ToArray();

        var uris = from a in allAssemblies
            from t in a.ExportedTypes
            where t != typeof(VDS.RDF.Uri)
            from m in t.GetMembers()
            from reft in RefTypes(m)
            where reft == typeof(System.Uri) || reft.IsGenericType && reft.GetGenericArguments().Any(t => t == typeof(System.Uri))
            select $"{t.Namespace}.{t.Name}.{m.Name}";

        uris.Should().HaveCount(0);

        IEnumerable<Type> RefTypes(MemberInfo mi)
        {
            return mi switch
            {
                MethodInfo meth => meth.GetParameters().Select(p => p.ParameterType).Append(meth.ReturnType),
                PropertyInfo prop => [prop.PropertyType],
                _ => []
            };
        }
    }
}