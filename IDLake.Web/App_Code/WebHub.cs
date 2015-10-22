using System;
using System.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using IDLake.Core;
using IDLake.Entities;
using System.Configuration;
using System.Linq;

namespace IDLake.Web
{
    [HubName("WebHub")]
    public class WebHub : Hub
    {
        static LibraryContainer _container;
        public LibraryContainer container
        {
            set
            {
                _container = value;
            }
            get
            {
                return _container;
            }
        }


        public WebHub()
        {
            //singleton + container pattern
            if (container == null)
            {
                /* # INVERSION OF CONTROL SCANNER/CONTAINER # uncomment this when u deploy in production area

                 Gravicode.Transformer.LibraryScanner scanner = new Gravicode.Transformer.LibraryScanner();
                 scanner.ScanLibrary<IDataContext>(ConfigurationManager.AppSettings["LibraryPath"]);
                 foreach (string a in scanner.GetLibraryList())
                 {
                     Console.WriteLine("Nama fungsi :" + a);
                 }
                 IDataContext redis = scanner.getInstance<IDataContext>(ConfigurationManager.AppSettings["RedisLibPath"]);
                 IDataContext mongo = scanner.getInstance<IDataContext>(ConfigurationManager.AppSettings["MongoLibPath"]);
                 */
                container = new LibraryContainer();
                //database aplikasi dan desain schema
                container.RegisterLibrary<SchemaDb>(new SchemaDb());
                //database untuk relatime data
                container.RegisterLibrary<InMemoryDb>(new InMemoryDb("lake", container.Get<SchemaDb>()));
                //database untuk relational data
                container.RegisterLibrary<DocumentDb>(new DocumentDb("lake", container.Get<SchemaDb>()));
                //databse untuk Big Data (historikal) - casandra

            }
        }
        #region Authentication
        [HubMethodName("Login")]
        public OutputCls Login(string username, string password)
        {
            OutputCls res = new OutputCls() { Result = false, Comment = "Username & password invalid." };
            var db = container.Get<SchemaDb>();
            var datas = from c in db.GetAllData<User>()
                        where c.UserName == username
                        select c;
            foreach (var item in datas)
            {
                if (item.Password == password)
                {
                    res.Comment = "Login succeed.";
                    res.Result = true;
                    return res;
                }
            }
            return res;
            // Call the broadcastMessage method to update clients.
            //Clients.All.displayData(datas);
        }

        [HubMethodName("CreateUser")]
        public OutputCls CreateUser(string username, string password, string Email)
        {
            OutputCls res = new OutputCls() { Result = false, Comment = "Create user failed." };
            var db = container.Get<SchemaDb>();
            var datas = from c in db.GetAllData<User>()
                        where c.UserName == username
                        select c;
            if (datas.Count() > 0)
            {
                res.Comment = "User is already exists.";
                return res;
            }
            db.InsertData<User>(new User() { UserName = username, Password = password, Email = Email, IsLocked = false, Id = db.GetSequence<User>() });
            res.Result = true;
            res.Comment = "User registered.";
            return res;
        }
        #endregion


    }
}