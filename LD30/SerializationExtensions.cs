using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LD30
{
    static class SerializationExtensions
    {
        public static void Write(this BinaryWriter writer, Vector3 v)
        {
            writer.Write(v.X);
            writer.Write(v.Y);
            writer.Write(v.Z);
        }

        public static void Write(this BinaryWriter writer, Color c)
        {
            writer.Write(c.R);
            writer.Write(c.G);
            writer.Write(c.B);
            writer.Write(c.A);
        }

        public static void Write(this BinaryWriter writer, PropInstance i)
        {
            writer.Write(i.BaseProp.ID);
            writer.Write(i.Scale);
            writer.Write(i.Position);
            writer.Write(i.RotationAngle);
            writer.Write(i.Color);
        }

        public static Vector3 ReadVector3(this BinaryReader reader)
        {
            return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }

        public static Color ReadColor(this BinaryReader reader)
        {
            return new Color(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
        }

        public static PropInstance ReadPropInstance(this BinaryReader reader, World w)
        {
            return Prop.CreateInstance(reader.ReadInt32(), reader.ReadVector3(), reader.ReadVector3(), reader.ReadSingle(), reader.ReadColor(), w);
        }
    }
}
