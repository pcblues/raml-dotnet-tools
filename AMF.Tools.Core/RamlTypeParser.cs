using System.Collections.Generic;
using AMF.Parser.Model;

namespace AMF.Tools.Core
{
    public class RamlTypeParser
    {
        private readonly IEnumerable<Shape> ramlTypes;
        private readonly IDictionary<string, ApiObject> schemaObjects;
        private readonly string targetNamespace;

        private readonly IDictionary<string, string> warnings;
        private readonly IDictionary<string, ApiEnum> enums;

        public RamlTypeParser(IEnumerable<Shape> ramlTypes, IDictionary<string, ApiObject> schemaObjects, 
            string targetNamespace, IDictionary<string, ApiEnum> enums, IDictionary<string, string> warnings)
        {
            this.ramlTypes = ramlTypes;
            this.schemaObjects = schemaObjects;
            this.targetNamespace = targetNamespace;
            this.enums = enums;
            this.warnings = warnings;
        }

        public void Parse()
        {
            foreach (var type in ramlTypes)
            {
                var apiObject = ParseRamlType(type.Name, type);
                if(apiObject != null && !schemaObjects.ContainsKey(type.Name))
                    schemaObjects.Add(type.Name, apiObject);
            }
        }

        public ApiObject ParseInline(string key, Shape inline, IDictionary<string, ApiObject> objects)
        {
            return ParseRamlType(key, inline);
        }

        private ApiObject ParseRamlType(string key, Shape ramlType)
        {
            return new ObjectParser().ParseObject(key, new string[0], ramlType, new Dictionary<string, ApiObject>(), 
                new Dictionary<string, string>(), new Dictionary<string, ApiEnum>(), new Dictionary<string, ApiObject>(), 
                new Dictionary<string, ApiObject>(), targetNamespace);
        }
    }
}