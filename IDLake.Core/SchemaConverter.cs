using IDLake.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Data.Entity.Design.PluralizationServices;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace IDLake.Core
{
    public class SchemaConverter
    {
        public static dynamic JsonToExpando(string JsonStr)
        {
            return JsonConvert.DeserializeObject<ExpandoObject>(JsonStr, new ExpandoObjectConverter());
        }
        public static SchemaEntity ExpandoToSchema(dynamic obj, string SchemaName)
        {
            var node = new SchemaEntity();
            node.SchemaName = SchemaName;
            node.JsonStructure = JsonConvert.SerializeObject(obj);
            XElement el = ExpandoToXML(obj, SchemaName);
            node.XmlStructure = el.ToString();
            node.Fields = GenerateFieldsFromExpando(obj);
            return node;

        }
        public static SchemaEntity JsonToSchema(string JsonStr, string SchemaName)
        {
            dynamic obj = JsonConvert.DeserializeObject<ExpandoObject>(JsonStr, new ExpandoObjectConverter());
            var node = new SchemaEntity();
            node.SchemaName = SchemaName;
            node.JsonStructure = JsonStr;
            node.XmlStructure = ExpandoToXML(obj, SchemaName);
            node.Fields = GenerateFieldsFromExpando(obj);
            return node;
        }

        public static string SchemaToJson(SchemaEntity entity)
        {
            return entity.JsonStructure;
        }

        public static string SchemaToXml(SchemaEntity entity)
        {
            return entity.XmlStructure;
        }

        public static SchemaEntity XmlToSchema(string XmlStr, string SchemaName)
        {
            dynamic obj = XMLtoExpando(null, XElement.Parse(XmlStr));
            var node = new SchemaEntity();
            node.SchemaName = SchemaName;
            node.JsonStructure = JsonConvert.SerializeObject(obj); ;
            node.XmlStructure = XmlStr;
            node.Fields = GenerateFieldsFromExpando(obj);
            return node;
        }
        private static Dictionary<string, IDField> GenerateFieldsFromExpando(dynamic node)
        {
            Dictionary<string, IDField> root = new Dictionary<string, IDField>();

            foreach (var property in (IDictionary<String, Object>)node)
            {
                if (property.Value.GetType() == typeof(List<dynamic>))
                    foreach (var element in (List<dynamic>)property.Value)
                        root.Add(property.Key, new IDField() { Name = property.Key, NativeType = property.Value.GetType(), Children = GenerateChild(element), Desc = "", FieldType = FieldTypes.MultiField, IsMandatory = true, RegexValidation = "" });
                else
                    root.Add(property.Key, new IDField() { Name = property.Key, NativeType = property.Value.GetType(), Children = null, Desc = "", FieldType = FieldTypes.SingleField, IsMandatory = true, RegexValidation = "" });
            }

            return root;
        }
        private static List<IDField> GenerateChild(dynamic node)
        {
            List<IDField> root = new List<IDField>();

            foreach (var property in (IDictionary<String, Object>)node)
            {

                if (property.Value.GetType() == typeof(List<dynamic>))
                    foreach (var element in (List<dynamic>)property.Value)
                        root.Add(new IDField() { Name = property.Key, NativeType = property.Value.GetType(), Children = GenerateChild(element), Desc = "", FieldType = FieldTypes.MultiField, IsMandatory = true, RegexValidation = "" });
                else
                    root.Add(new IDField() { Name = property.Key, NativeType = property.Value.GetType(), Children = null, Desc = "", FieldType = FieldTypes.SingleField, IsMandatory = true, RegexValidation = "" });
            }

            return root;
        }
        private static XElement ExpandoToXML(dynamic node, String nodeName)
        {
            XElement xmlNode = new XElement(nodeName);
            if (node is List<dynamic>)
            {
                foreach (var element in (List<dynamic>)node)
                    xmlNode.Add(ExpandoToXML(element, "row"));
            }
            else
            {
                foreach (var property in (IDictionary<String, Object>)node)
                {

                    if (property.Value.GetType() == typeof(ExpandoObject))
                        xmlNode.Add(ExpandoToXML(property.Value, property.Key));

                    else
                        if (property.Value.GetType() == typeof(List<dynamic>))
                        foreach (var element in (List<dynamic>)property.Value)
                            xmlNode.Add(ExpandoToXML(element, property.Key));
                    else
                        xmlNode.Add(new XElement(property.Key, property.Value));
                }
            }
            return xmlNode;
        }
        private static dynamic XMLtoExpando(String file, XElement node = null)
        {
            if (String.IsNullOrWhiteSpace(file) && node == null) return null;

            // If a file is not empty then load the xml and overwrite node with the
            // root element of the loaded document
            node = !String.IsNullOrWhiteSpace(file) ? XDocument.Load(file).Root : node;

            IDictionary<String, dynamic> result = new ExpandoObject();

            // implement fix as suggested by [ndinges]
            var pluralizationService =
                PluralizationService.CreateService(CultureInfo.CreateSpecificCulture("en-us"));

            // use parallel as we dont really care of the order of our properties
            node.Elements().AsParallel().ForAll(gn =>
            {
                // Determine if node is a collection container
                var isCollection = gn.HasElements &&
                    (
                        // if multiple child elements and all the node names are the same
                        gn.Elements().Count() > 1 &&
                        gn.Elements().All(
                            e => e.Name.LocalName.ToLower() == gn.Elements().First().Name.LocalName) ||

                        // if there's only one child element then determine using the PluralizationService if
                        // the pluralization of the child elements name matches the parent node. 
                        gn.Name.LocalName.ToLower() == pluralizationService.Pluralize(
                            gn.Elements().First().Name.LocalName).ToLower()
                    );

                // If the current node is a container node then we want to skip adding
                // the container node itself, but instead we load the children elements
                // of the current node. If the current node has child elements then load
                // those child elements recursively
                var items = isCollection ? gn.Elements().ToList() : new List<XElement>() { gn };

                var values = new List<dynamic>();

                // use parallel as we dont really care of the order of our properties
                // and it will help processing larger XMLs
                items.AsParallel().ForAll(i => values.Add((i.HasElements) ?
                   XMLtoExpando(null, i) : i.Value.Trim()));

                // Add the object name + value or value collection to the dictionary
                result[gn.Name.LocalName] = isCollection ? values : values.FirstOrDefault();
            });
            return result;
        }
        public static bool AreExpandoStructureEquals(ExpandoObject obj1, ExpandoObject obj2)
        {
            var obj1AsColl = (ICollection<KeyValuePair<string, object>>)obj1;
            var obj2AsDict = (IDictionary<string, object>)obj2;

            // Make sure they have the same number of properties
            if (obj1AsColl.Count != obj2AsDict.Count)
                return false;

            foreach (var pair in obj1AsColl)
            {
                if (!obj2AsDict.ContainsKey(pair.Key))
                {
                    return false;
                }
                else
                {
                    //if (obj2AsDict[pair.Key].GetType() != pair.Value.GetType())
                    //    return false;
                }

            }

            // Everything matches
            return true;
        }
    }
}
