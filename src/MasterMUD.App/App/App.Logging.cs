﻿using System.Linq;
using MasterMUD.Interfaces;

namespace MasterMUD
{
    public sealed partial class App
    {
        private static void Log(object data, System.ConsoleColor foregroundColor)
        {
            if (System.Environment.UserInteractive)
                lock (App.Lock)
                    try
                    {
                        var oldFgColor = System.Console.ForegroundColor;
                        System.Console.ForegroundColor = System.ConsoleColor.Cyan;
                        System.Console.Write("{0} ", (System.DateTime.Now - App.Current.Activated));
                        System.Console.ForegroundColor = foregroundColor;
                        System.Console.WriteLine(data);
                        System.Console.ForegroundColor = oldFgColor;
                    }
                    catch (System.Exception ex2)
                    {
                        System.Diagnostics.Debug.WriteLine(ex2);
                    }
        }

        public static void Log(System.Exception exception) => App.Log(data: exception, foregroundColor: System.ConsoleColor.Red);
        public static void Log(string message) => App.Log(data: message, foregroundColor: System.ConsoleColor.White);
        public static void Log(string message, System.ConsoleColor foregroundColor) => App.Log(data: message, foregroundColor: foregroundColor);
    }
}