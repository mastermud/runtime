using System.Linq;

namespace MasterMUD
{
    public sealed partial class App
    {
        /// <summary>
        ///     Used for locking console logging so there aren't multiple threads overlapping writes.
        /// </summary>
        /// <remarks>This should be removed at some point for something with more finesse.</remarks>
        private static readonly object Lock = new object();

        private static void Log(object data, System.ConsoleColor foregroundColor)
        {
            if (System.Environment.UserInteractive)
                lock (App.Lock)
                    try
                    {
                        var oldFgColor = System.Console.ForegroundColor;
                        System.Console.ForegroundColor = System.ConsoleColor.Cyan;
                        System.Console.Write("{0:HH:hh:ss} ", System.DateTime.Now);
                        System.Console.ForegroundColor = foregroundColor;
                        System.Console.WriteLine(data);
                        System.Console.ForegroundColor = oldFgColor;
                    }
                    catch (System.Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex);
                    }
        }

        public static void Log(System.Exception exception) => App.Log(data: exception, foregroundColor: System.ConsoleColor.Red);
        public static void Log(string message) => App.Log(data: message, foregroundColor: System.ConsoleColor.White);        
    }
}