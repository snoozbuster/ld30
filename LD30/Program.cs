using Accelerated_Delivery_Win;
using BEPUutilities;
using System;
using System.IO;

namespace LD30
{
#if WINDOWS || XBOX
    static class Program
    {
        public static string SavePath { get; private set; }
        public static BoxCutter Cutter { get; private set; }
        public static BaseGame Game { get; private set; }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
#if !DEBUG
            try
            {
#endif
#if WINDOWS
                SavePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\LD30\\";
                if(!Directory.Exists(SavePath))
                    Directory.CreateDirectory(SavePath);
#endif
                Cutter = new BoxCutter(false, false, SavePath);
                using(Game = new BaseGame())
                    Game.Run();
#if !DEBUG
            }
            catch(Exception ex)
            {
                using(CrashDebugGame game = new CrashDebugGame(ex, Cutter))
                    game.Run();
            }
#endif
        }

        public static BEPUutilities.Vector3 Abs(this BEPUutilities.Vector3 v)
        {
            return new BEPUutilities.Vector3(Math.Abs(v.X), Math.Abs(v.Y), Math.Abs(v.Z));
        }

        public static Microsoft.Xna.Framework.Vector3 Abs(this Microsoft.Xna.Framework.Vector3 v)
        {
            return new Microsoft.Xna.Framework.Vector3(Math.Abs(v.X), Math.Abs(v.Y), Math.Abs(v.Z));
        }

        public static bool Contains(this BoundingBox b, Vector3 v)
        {
            return b.Min.X <= v.X && b.Min.Y <= v.Y && b.Min.Z <= v.Z &&
                   b.Max.X >= v.X && b.Max.Y >= v.Y && b.Max.Z >= v.Z;
        }
    }
#endif
}

