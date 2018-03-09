using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Raml.Common;
using AMF.Parser.Model;
using AMF.Tools.Core.WebApiGenerator;

namespace AMF.Tools.Core
{
    public class WebApiMethodsGenerator : MethodsGeneratorBase
    {
        private readonly QueryParametersParser queryParametersParser;

        public WebApiMethodsGenerator(AmfModel raml, IDictionary<string, ApiObject> schemaResponseObjects, 
            IDictionary<string, ApiObject> schemaRequestObjects, IDictionary<string, string> linkKeysWithObjectNames, 
            IDictionary<string, ApiObject> schemaObjects)
            : base(raml, schemaResponseObjects, schemaRequestObjects, linkKeysWithObjectNames, schemaObjects)
        {
            queryParametersParser = new QueryParametersParser(schemaObjects);
        }

        public IEnumerable<ControllerMethod> GetMethods(EndPoint resource, string url, ControllerObject parent, string objectName, IDictionary<string, Parameter> parentUriParameters)
        {
            var methodsNames = new List<string>();
            if (parent != null && parent.Methods != null)
                methodsNames = parent.Methods.Select(m => m.Name).ToList();

            var generatorMethods = new Collection<ControllerMethod>();
            if (resource.Operations == null)
                return generatorMethods;

            foreach (var method in resource.Operations)
            {
                var generatedMethod = BuildControllerMethod(url, method, resource, parent, parentUriParameters);

                if (IsVerbForMethod(method))
                {
                    if (methodsNames.Contains(generatedMethod.Name))
                        generatedMethod.Name = GetUniqueName(methodsNames, generatedMethod.Name, resource.Path);

                    if (method.Request.QueryParameters != null && method.Request.QueryParameters.Any())
                    {
                        var queryParameters = queryParametersParser.ParseParameters(method);
                        generatedMethod.QueryParameters = queryParameters;
                    }

                    generatorMethods.Add(generatedMethod);
                    methodsNames.Add(generatedMethod.Name);
                }
            }

            return generatorMethods;
        }

        private ControllerMethod BuildControllerMethod(string url, Operation method, EndPoint resource, ControllerObject parent, IDictionary<string, Parameter> parentUriParameters)
        {
            var relativeUri = UrlGeneratorHelper.GetRelativeUri(url, parent.PrefixUri);

            var parentUrl = UrlGeneratorHelper.GetParentUri(url, resource.Path);

            var securedBy = resource.Operations.FirstOrDefault(m => m.Method == method.Method && m.Security != null
                                && m.Security.Any()).Security.Select(s => s.Name); //TODO: check

            return new ControllerMethod
            {
                Name = NetNamingMapper.GetMethodName(method.Method ?? "Get" + resource.Path),
                Parameter = GetParameter(GeneratorServiceHelper.GetKeyForResource(method, resource, parentUrl), method, resource, url),
                UriParameters = uriParametersGenerator.GetUriParameters(resource, url, parentUriParameters),
                ReturnType = GetReturnType(GeneratorServiceHelper.GetKeyForResource(method, resource, parentUrl), method, resource, url),
                Comment = GetComment(resource, method, url),
                Url = relativeUri,
                Verb = NetNamingMapper.Capitalize(method.Method),
                Parent = null,
                UseSecurity = resource.Operations.Any(m => m.Method == method.Method && m.Security != null && m.Security.Any()),
                SecuredBy = securedBy,
                SecurityParameters = GetSecurityParameters(method)
            };
        }

        private IEnumerable<Property> GetSecurityParameters(Operation method)
        {
            var securityParams = new Collection<Property>();

            var securedBy = method.Security != null && method.Security.Any() ? method.Security : null;

            if (securedBy == null)
                return securityParams;

            var secured = securedBy.First(); //TODO: check, how to choose ?

            return queryParametersParser.ConvertParametersToProperties(secured.QueryParameters);
        }
    }
}