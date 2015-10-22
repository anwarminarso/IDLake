using IDLake.Core;
using IDLake.Entities;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IDLake.Test
{
    class Program
    {
        static void Main(string[] args)
        {

            ISchemaContext ctx = new SchemaDb();
            dynamic contacts = new System.Dynamic.ExpandoObject();
            (contacts as IDictionary<string, object>).Add("Usia", 40);
            contacts.Name = "Patrick Hines";
            contacts.Phone = "206-555-0144";
            (contacts as IDictionary<string, object>).Add("House", new List<dynamic>());
            contacts.House.Add(new System.Dynamic.ExpandoObject());
            contacts.House[0].Address = "Jln. Klumeten";
            contacts.House[0].POBox = "7658";

            dynamic contacts2 = new ExpandoObject();
            (contacts2 as IDictionary<string, object>).Add("Usia", 39);
            contacts2.Name = "Ellen Adams";
            contacts2.Phone = "206-555-0155";
            contacts2.House = new List<dynamic>();
            contacts2.House.Add(new ExpandoObject());
            contacts2.House[0].Address = "Jln. Klumet";
            contacts2.House[0].POBox = "23456";
           
            Console.WriteLine(SchemaConverter.AreExpandoStructureEquals(contacts, contacts2));
            //GetData();
            //dtx.InsertData<dynamic>(contacts, "contacts");
            SchemaEntity item = SchemaConverter.ExpandoToSchema(contacts, nameof(contacts));
            item.Id = ctx.GetSequence<SchemaEntity>();
            ctx.InsertData<SchemaEntity>(item);
            /*
            var data = ctx.GetAllData<SchemaEntity>();
            foreach(var item in data)
            {
                dynamic obj = SchemaConverter.JsonToExpando(item.JsonStructure);
                if (obj is ExpandoObject)
                {
                    foreach (var property in (IDictionary<String, Object>)obj)
                    {
                        if (property.Value is ExpandoObject)
                        {
                            //do nothing

                        }
                        else if (property.Value is List<dynamic>)
                        {
                            foreach (var element in (List<dynamic>)property.Value)
                            {
                                if (element is ExpandoObject)
                                {
                                    foreach (var pr in (IDictionary<String, Object>)element)
                                    {
                                        Console.WriteLine($"{pr.Key} as {pr.Value.GetType().ToString()} = {pr.Value}");
                                    }
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine($"{property.Key} as {property.Value.GetType().ToString()} = {property.Value}");
                        }

                    }
                }
                
            }*/
            Console.ReadLine();
        }

        async static void GetData()
        {
            ISchemaContext ctx = new SchemaDb();
            dynamic contacts2 = new ExpandoObject();
            (contacts2 as IDictionary<string, object>).Add("Usia", 39);
            contacts2.Name = "Asep XX";
            contacts2.Phone = "206-555-0155";
            contacts2.House = new List<dynamic>();
            contacts2.House.Add(new ExpandoObject());
            contacts2.House[0].Address = "Jln. Klumet";
            contacts2.House[0].POBox = "23456";
            contacts2._id = 1;
           

            IDataContext dtx = new InMemoryDb("lake",ctx);

            dtx.InsertData(contacts2, "contacts");
            //dtx.InsertData(contacts2, "contacts");
            var datas = await dtx.GetAllData("contacts");
            //var datas = await dtx.GetDataByStartId(2,1,"contacts");
            foreach (dynamic item in datas)
            {
                Console.WriteLine($"{item.House[0].Address}");
                //Console.WriteLine(SchemaConverter.AreExpandoStructureEquals(item, contacts2));
                //(item as IDictionary<string, object>)["Usia"]= 31;
               
                //dtx.UpdateData(item, "contacts");
            }
        }
    }
}
