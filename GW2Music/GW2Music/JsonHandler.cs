using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GW2Music
{
    class JsonHandler
    {
        private static string path = Directory.GetCurrentDirectory() + "\\songs.json";
        
        public static SongLibrary LoadLibrary()
        {
            if(File.Exists(path))
            {
                string json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<SongLibrary>(json);
            } else
            {
                return new SongLibrary();
            }
        }

        public static void SaveLibrary(SongLibrary lib)
        {
            string output = JsonConvert.SerializeObject(lib);
            if(File.Exists(path))
            {
                File.Delete(path);
            }
            File.WriteAllText(path, output);
        }
    }
}
