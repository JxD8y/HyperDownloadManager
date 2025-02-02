using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperDownloadManager
{
    public static class PathManager
    {
        public static string ApplicationPath = "";
        public static List<string> InnerPathNames = new List<string>();
        public static bool CheckPathExistDirs(string foldername)
        {
            if (ApplicationPath == null)
            {
                throw new NullReferenceException("ApplicationPath was Null");
            }
            string _path = Path.Combine(ApplicationPath, foldername);
            return Directory.Exists(_path);
        }
        public static string GetPathDirectoryInfo(string foldername)
        {
            if (ApplicationPath == null)
            {
                throw new NullReferenceException("ApplicationPath was Null");
            }
            string _path = Path.Combine(ApplicationPath, foldername);
            DirectoryInfo _di = new DirectoryInfo(_path);
            return _di.FullName;
        }
        public static void CreateDirs()
        {
            InnerPathNames = new List<string>();
            ApplicationPath = Directory.GetCurrentDirectory();
            InnerPathNames.Add(Path.Combine(ApplicationPath, "Logs"));
            InnerPathNames.Add(Path.Combine(ApplicationPath, "DataBase"));
            foreach (string path in InnerPathNames)
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
