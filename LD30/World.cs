using BEPUutilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace LD30
{
    public class World
    {
        public const int MaxSideLength = 50;

        public readonly string OwnerName;
        public List<PropInstance> Objects { get { return new List<PropInstance>(objects); } }
        private List<PropInstance> objects;

        public Vector3 WorldPosition { get; private set; }

        private World(string name, string data)
        {
            WorldPosition = Vector3.Zero;
            OwnerName = name;
            objects = new List<PropInstance>();
            createWorld(new BinaryReader(new MemoryStream(Convert.FromBase64String(data))));
        }

        public World(string name)
        {
            OwnerName = name;
            objects = new List<PropInstance>();
            WorldPosition = Vector3.Zero;
            createWorld();
        }

        /// <summary>
        /// Warning: may throw exceptions
        /// </summary>
        /// <param name="paste"></param>
        public World(paste paste, Vector3 worldPos)
        {
            WorldPosition = worldPos;
            OwnerName = paste.paste_title;
            string data = null;
            using(WebClient c = new WebClient())
                data = c.DownloadString("http://pastebin.com/raw.php?i=" + paste.paste_key);
            createWorld(new BinaryReader(new MemoryStream(Convert.FromBase64String(data))));
        }

        private void createWorld()
        {
            // generate a world
        }

        private void createWorld(BinaryReader reader)
        {
            int num = reader.ReadInt32();
            for(int i = 0; i < num; i++)
                objects.Add(reader.ReadPropInstance(this));
        }

        public string SaveToFile()
        {
            using(MemoryStream stream = new MemoryStream())
                using(BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(objects.Count);
                    foreach(PropInstance p in objects)
                        writer.Write(p);
                    stream.Seek(0, SeekOrigin.Begin);
                    byte[] temp = new byte[stream.Length];
                    stream.Read(temp, 0, temp.Length);
                    string data = Convert.ToBase64String(temp);
                    string path = Program.SavePath;
                    path = path + OwnerName + ".wld";
                    using(StreamWriter w = new StreamWriter(path))
                        w.Write(data);
                    return path;
                }
        }

        public static World FromFile(string path)
        {
            using(StreamReader reader = new StreamReader(path))
                return new World(Path.GetFileNameWithoutExtension(path), reader.ReadToEnd());
        }
    }
}
