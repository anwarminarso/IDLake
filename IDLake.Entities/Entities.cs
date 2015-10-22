using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IDLake.Entities
{
    public class SchemaEntity
    {
        public string SchemaName { set; get; }
        public long Id { set; get; }
        public string JsonStructure { set; get; }
        public string XmlStructure { set; get; }
        public Dictionary<string,IDField> Fields { set; get; }
        public SchemaTypes SchemaType { set; get; }
    }
    public enum SchemaTypes { StreamData=0, RelationalData, HistoricalData }
    public enum FieldTypes { SingleField=0, MultiField }
    //public enum IDType { Teks, Desimal, AngkaBulat, Tanggal, Karakter, Bit }
    public class IDField
    {
        public string Name { set; get; }
        public string Desc { set; get; }
        public Type NativeType { set; get; }
        public FieldTypes FieldType { set; get; }
        public bool IsMandatory { set; get; }
        public string RegexValidation { set; get; }
        public List<IDField> Children { set; get; }

    }
  
}
