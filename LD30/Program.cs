using Accelerated_Delivery_Win;
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
    }
#endif
}

