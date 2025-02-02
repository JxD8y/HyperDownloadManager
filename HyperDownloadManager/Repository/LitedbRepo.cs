using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using LiteDB;

namespace HyperDownloadManager.Repository
{
    public static class LitedbRepo<T>
    {
        public const string DownloadsColName = "Downloads";
        public const string ContainerColName = "Containers";
        public const string ThemeSettingsColName = "ThemeSettings";
        public const string NetSettingsColName = "NetSettings";
        public const string GeneralSettingsColName = "GeneralSettings";

        static LiteDatabase? db;
        static LiteCollection<T>? coll;
        static bool Connected = false;
        private static void Connect(string Collection)
        {
            string path = System.IO.Path.Combine(PathManager.GetPathDirectoryInfo("DataBase"), "DeepDownloads.db");
            db = new LiteDatabase(path);
            coll = (LiteCollection<T>)db.GetCollection<T>(Collection);
            Connected = true;
        }


        private static void Disconnect()
        {
            if (Connected)
            {
                db.Dispose();
                coll = null;
                Connected = false;
            }
        }

        public static BsonValue? Add(T entity, string ColName)
        {
            lock (entity)
            {
                Connect(ColName);
                coll.EnsureIndex("id");
                var bsv = coll.Insert(entity);
                Disconnect();
                return bsv;
            }
        }
        public static List<T> Get(string ColName)
        {
            Connect(ColName);
            var result = coll.FindAll().ToList<T>();
            Disconnect();
            return result;
        }

        public static T? Get(string ColName, int id)
        {
            Connect(ColName);
            var result = coll.FindById(id);
            Disconnect();
            return result;
        }

        public static void Remove(BsonValue id, string ColName)
        {
            Connect(ColName);
            coll.Delete(id);
            Disconnect();
        }
        public static void RemoveAll(string ColName)
        {
            Connect(ColName);
            db.DropCollection(ColName);
            Disconnect();
        }

        public static void Update(BsonValue id, T entity, string ColName)
        {
            Connect(ColName);
            coll.Update(id, entity);
            Disconnect();
        }
    }
}
