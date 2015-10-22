using Gravicode.Transformer;
using IDLake.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ServiceStack.Redis;
using ServiceStack.Redis.Generic;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IDLake.Core
{
    [Keterangan("InMemoryDb", "Storage menggunakan redis")]
    public class InMemoryDb : IDataContext
    {
        ISchemaContext ctx { set; get; }
        public string DBName { private set; get; }
        public InMemoryDb(string DBName, ISchemaContext ctx)
        {
            this.DBName = DBName;
            this.ctx = ctx;
        }
        public Task<bool> DeleteAllData(string CollectionName)
        {
            try
            {
                using (var redisManager = new PooledRedisClientManager())
                using (var redis = redisManager.GetClient())
                {
                    var datas = redis.ScanAllKeys($"{DBName}:{CollectionName}:*");
                    foreach (var item in datas)
                    {
                        redis.Remove(item);
                    }
                    return Task.FromResult(true);
                }
            }
            catch
            {
                //print ke log
                //throw;
                return Task.FromResult(false); ;
            }
        }

        public Task<bool> DeleteData(dynamic id, string CollectionName)
        {
            try
            {
                using (var redisManager = new PooledRedisClientManager())
                using (var redis = redisManager.GetClient())
                {
                    var datas = redis.ScanAllKeys($"{DBName}:{CollectionName}:{id}");
                    foreach (var item in datas)
                    {
                        redis.Remove(item);
                    }
                    return Task.FromResult(true);
                }
            }
            catch
            {
                //print ke log
                //throw;
                return Task.FromResult(false); ;
            }
        }

        public Task<bool> DeleteDataBulk(IEnumerable<dynamic> Ids, string CollectionName)
        {
            try
            {
                using (var redisManager = new PooledRedisClientManager())
                using (var redis = redisManager.GetClient())
                {
                    foreach (var Fid in Ids)
                    {
                        var datas = redis.ScanAllKeys($"{DBName}:{CollectionName}:{Fid}");
                        foreach (var item in datas)
                        {
                            redis.Remove(item);
                        }
                    }
                    return Task.FromResult(true);
                }
            }
            catch
            {
                //print ke log
                //throw;
                return Task.FromResult(false); ;
            }
        }

        public Task<List<dynamic>> GetAllData(string CollectionName)
        {
            var datas = new List<dynamic>();
            using (var redisManager = new PooledRedisClientManager())
            using (var redis = redisManager.GetClient())
            {
                using (var redisNative = redisManager.GetCacheClient())
                {
                    var items = redis.ScanAllKeys($"{DBName}:{CollectionName}:*");
                    foreach (var item in items)
                    {
                        /*
                        dynamic node = redisNative.Get<ExpandoObject>(item);
                        if (node != null)
                            datas.Add(node);
                        */
                        dynamic node = redisNative.Get<string>(item);
                        if (!string.IsNullOrEmpty(node))
                            datas.Add(JsonConvert.DeserializeObject<ExpandoObject>(node, new ExpandoObjectConverter()));
                    }
                }
                return Task.FromResult(datas);
            }
        }

        public Task<List<dynamic>> GetAllData(int Limit, string CollectionName)
        {
            var counter = 0;
            var datas = new List<dynamic>();
            using (var redisManager = new PooledRedisClientManager())
            using (var redis = redisManager.GetClient())
            {
                var items = redis.ScanAllKeys($"{DBName}:{CollectionName}:*");
                foreach (var item in items)
                {
                    string node = redis.Get<string>(item);
                    if (!string.IsNullOrEmpty(node))
                        datas.Add(JsonConvert.DeserializeObject<ExpandoObject>(node, new ExpandoObjectConverter()));
                    counter++;
                    if (counter >= Limit) break;
                }
                return Task.FromResult(datas);
            }
        }

        public Task<dynamic> GetDataById(dynamic Id, string CollectionName)
        {
            dynamic node = null;
            using (var redisManager = new PooledRedisClientManager())
            using (var redis = redisManager.GetClient())
            {
                var item = $"{DBName}:{CollectionName}:{Id}";
                string itemstr = redis.Get<string>(item);
                if (!string.IsNullOrEmpty(itemstr))
                    node = JsonConvert.DeserializeObject<ExpandoObject>(itemstr, new ExpandoObjectConverter());
                return Task.FromResult(node);
            }
        }

        public Task<List<dynamic>> GetDataByStartId(int Limit, long StartId, string CollectionName)
        {
            
            var datas = new List<dynamic>();
            using (var redisManager = new PooledRedisClientManager())
            using (var redis = redisManager.GetClient())
            {
                for (long i = StartId; i <= StartId + Limit; i++)
                {
                    var item = $"{DBName}:{CollectionName}:{i}";
                    string itemstr = redis.Get<string>(item);
                    if (!string.IsNullOrEmpty(itemstr))
                    {
                        var node = JsonConvert.DeserializeObject<ExpandoObject>(itemstr, new ExpandoObjectConverter());

                        datas.Add(node);

                    }
                }
                return Task.FromResult(datas);
            }
        }

        public long GetSequence(string CollectionName)
        {
            return ctx.GetSchemaSequence($"redis_counter:{DBName}:{CollectionName}");
        }

        public Task<bool> InsertBulkData(IEnumerable<dynamic> data, string CollectionName)
        {
            using (var redisManager = new PooledRedisClientManager())
            using (var redis = redisManager.GetClient())
            {
               foreach(dynamic item in data)
                {
                    var counter = this.GetSequence(CollectionName);
                    item._id = counter;
                    var keyItem = $"{DBName}:{CollectionName}:{counter}";
                    redis.Set<string>(keyItem, JsonConvert.SerializeObject(item));
                }
            }
            return Task.FromResult(true);
        }

        public Task<bool> InsertData(dynamic data, string CollectionName)
        {
            using (var redisManager = new PooledRedisClientManager())
            using (var redis = redisManager.GetCacheClient())
            {
                
                var counter = this.GetSequence(CollectionName);
                data._id = counter;
                var keyItem = $"{DBName}:{CollectionName}:{counter}";
                //redis.Set<ExpandoObject>(keyItem,data);
                redis.Set<string>(keyItem, JsonConvert.SerializeObject(data));
            }
            return Task.FromResult(true);
        }

        public Task<bool> UpdateData(dynamic data, string CollectionName)
        {
            using (var redisManager = new PooledRedisClientManager())
            using (var redis = redisManager.GetClient())
            {
                var keyItem = $"{DBName}:{CollectionName}:{data._id}";
                redis.Set<string>(keyItem, JsonConvert.SerializeObject(data));
            }
            return Task.FromResult(true);
        }
    }
}
