using System;
using System.Collections.Generic;
using System.Linq;
using Raml.Common;

using AMF.Tools.Core.XML;
using AMF.Parser.Model;

namespace AMF.Tools.Core
{
    public class ObjectParser
    {
        //private readonly JsonSchemaParser jsonSchemaParser = new JsonSchemaParser();

        public ApiObject ParseObject(string key, IEnumerable<string> value, Shape shape, IDictionary<string, ApiObject> objects, IDictionary<string, string> warnings, IDictionary<string, ApiEnum> enums, IDictionary<string, ApiObject> otherObjects, IDictionary<string, ApiObject> schemaObjects, string targetNamespace)
        {
            var apiObj = new ApiObject
            {
                BaseClass = shape.Inherits.First().Name, //TODO: check
                IsArray = shape is ArrayShape,
                IsScalar = shape is ScalarShape,
                IsUnionType = shape is UnionShape,
                Name = shape.Name,
                Type = MapShapeType(shape),
                Description = shape.Description,
                Properties = MapProperties(shape),
                Example = MapExample(shape)
            };

            return apiObj;
        }

        public static string MapExample(Shape shape)
        {
            if (shape is AnyShape)
                return string.Join("\r\n", ((AnyShape)shape).Examples.Select(e => e.Name));

            return null;
        }

        private IList<Property> MapProperties(Shape shape)
        {
            if(shape is NodeShape)
            {
                return ((NodeShape)shape).Properties.Select(p => MapProperty(p)).ToList();
            }

            return new Property[0];
        }

        private Property MapProperty(PropertyShape p)
        {
            var prop = new Property
            {
                Name = p.Path, // p.Range.Name,
                Required = p.Required,
                Type = p.Path //TODO: check
            };

            if (p.Range == null)
                return prop;

            prop.Description = p.Range.Description;
            prop.Example = MapExample(p.Range);
            prop.Type = MapShapeType(p.Range);

            if (p.Range is ScalarShape scalar)
            {
                prop.MaxLength = scalar.MaxLength;
                prop.MinLength = scalar.MinLength;
                prop.Maximum = string.IsNullOrWhiteSpace(scalar.Maximum) ? (double?)null : Convert.ToDouble(scalar.Maximum);
                prop.Minimum = string.IsNullOrWhiteSpace(scalar.Minimum) ? (double?)null : Convert.ToDouble(scalar.Minimum);
            }
            return prop;
        }

        public static string MapShapeType(Shape shape) //TODO: check
        {
            if (shape is ScalarShape)
                return ((ScalarShape)shape).DataType;

            return shape.Name;
        }

        public ApiObject ParseObject(string key, string value, IDictionary<string, ApiObject> objects, IDictionary<string, string> warnings, IDictionary<string, ApiEnum> enums, IDictionary<string, ApiObject> otherObjects, IDictionary<string, ApiObject> schemaObjects, string targetNamespace)
        {
            var obj = ParseSchema(key, value, objects, warnings, enums, otherObjects, schemaObjects, targetNamespace);
            if (obj == null)
                return null;

            obj.Name = NetNamingMapper.GetObjectName(key);

            if (schemaObjects.Values.Any(o => o.Name == obj.Name) || objects.Values.Any(o => o.Name == obj.Name) ||
                otherObjects.Values.Any(o => o.Name == obj.Name))
            {
                if(UniquenessHelper.HasSameProperties(obj, objects, key, otherObjects, schemaObjects))
                    return null;

                obj.Name = UniquenessHelper.GetUniqueName(objects, obj.Name, otherObjects, schemaObjects);
            }

            return obj;
        }

        private ApiObject ParseSchema(string key, string schema, IDictionary<string, ApiObject> objects, IDictionary<string, string> warnings, IDictionary<string, ApiEnum> enums, IDictionary<string, ApiObject> otherObjects, IDictionary<string, ApiObject> schemaObjects, string targetNamespace)
		{
   			if (schema == null)
				return null;

            // is a reference, should then be defined elsewhere
            if (schema.Contains("<<") && schema.Contains(">>"))
                return null;

            if (schema.Trim().StartsWith("<"))
                return ParseXmlSchema(key, schema, objects, targetNamespace, otherObjects, schemaObjects);

            if (!schema.Contains("{"))
                return null;

            // return jsonSchemaParser.Parse(key, schema, objects, warnings, enums, otherObjects, schemaObjects);
            return null;
        }

        private ApiObject ParseXmlSchema(string key, string schema, IDictionary<string, ApiObject> objects, string targetNamespace, IDictionary<string, ApiObject> otherObjects, IDictionary<string, ApiObject> schemaObjects)
		{
            if(objects.ContainsKey(key))
                return null;

		    var xmlSchemaParser = new XmlSchemaParser();
            var  obj = xmlSchemaParser.Parse(key, schema, objects, targetNamespace);

		    if (obj != null && !objects.ContainsKey(key) && !UniquenessHelper.HasSameProperties(obj, objects, otherObjects, schemaObjects))
		        objects.Add(key, obj); // to associate that key with the main XML Schema object

		    return obj;
		}

    }
}