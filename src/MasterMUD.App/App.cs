using System;

namespace MasterMUD
{
    public sealed partial class App
    {
        /// <summary>
        ///     Ensures only one running instance is allowed.
        /// </summary>
        private static readonly System.Threading.Mutex Mutex;

        /// <summary>
        ///     Keeps the application alive forever.
        /// </summary>
        private static readonly System.Threading.EventWaitHandle EventWaitHandle;

        /// <summary>
        ///     Initializes the environment and performs all required work before Main is allowed to run.
        /// </summary>
        /// <remarks>If anything goes wrong in here, we need to do our very best to shutdown the running application.</remarks>
        static App()
        {
            try
            {
                App.Mutex = new System.Threading.Mutex(initiallyOwned: true, name: nameof(MasterMUD), out var createdNew);

                if (false == createdNew)
                {
                    System.Console.Error.WriteLine(Properties.Resources.StaticConstructorDuplicated);
                    System.Threading.Thread.Sleep(3 * (333 * 3));
                    System.Environment.Exit(0);
                }
                else
                {
                    if (System.Environment.UserInteractive)
                    {
                        // TODO: Environment configuration and initialization.
                        System.Console.Title = Properties.Resources.Title;
                        System.Console.Clear();
                        System.Console.CursorVisible = false;
                        System.Console.TreatControlCAsInput = false;
                    }

                    App.EventWaitHandle = new System.Threading.EventWaitHandle(initialState: false, mode: System.Threading.EventResetMode.ManualReset);

                    var args = System.Environment.GetCommandLineArgs();
                    var fi = new System.IO.FileInfo(args[0]);
                    var dir = new System.IO.DirectoryInfo(fi.DirectoryName);
                    Console.WriteLine($"Initializing from {dir.FullName}");
                    foreach (var dll in dir.GetFiles("*.dll", System.IO.SearchOption.AllDirectories))
                        Console.WriteLine($"\t {dll.Name}");
                }

                System.Console.WriteLine("Ready.");
            }
            catch (System.Exception ex)
            {
                try
                {
                    System.Console.Error.WriteLine(ex);
                }
                catch (System.Exception ex2)
                {
                    System.Diagnostics.Debug.WriteLine(ex2);
                }
                finally
                {
                    var failedToExit = false;

                    try
                    {
                        System.Environment.Exit(ex.HResult);
                    }
                    catch (System.Security.SecurityException se)
                    {
                        failedToExit = true;
                        System.Diagnostics.Debug.WriteLine(se);
                    }
                    finally
                    {
                        if (failedToExit)
                        {
                            // TODO: What do if couldn't exit?
                            throw new App.Exception(Properties.Resources.StaticConstructorCatastrophic);
                        }
                    }
                }
            }
        }

        private static void Main()
        {
            using (App.Mutex)
            using (App.EventWaitHandle)
                try
                {                    
                    System.Console.CancelKeyPress += Console_CancelKeyPress;

                    try
                    {
                        App.EventWaitHandle.WaitOne();
                    }
                    catch (System.Exception ex)
                    {
                        System.Console.Error.WriteLine(ex);
                    }

                    System.Console.CancelKeyPress -= Console_CancelKeyPress;
                }
                catch (System.Exception ex)
                {
                    System.Console.Error.WriteLine(ex);
                }
        }

        private static void Terminate()
        {
            try
            {
                App.EventWaitHandle.Set();
            }
            finally
            {
                try
                {
                    App.EventWaitHandle.Reset();
                }
                finally
                {
                    System.Console.WriteLine("Terminated.");
                }
            }
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            Terminate();
        }
    }
}
