using System.Linq;
using MasterMUD.Interfaces;

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

        internal static System.Collections.Concurrent.ConcurrentDictionary<string, MasterMUD.Interfaces.IFeature> Features { get; }

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
                    var args = System.Environment.GetCommandLineArgs();
                    var fi = new System.IO.FileInfo(args[0]);
                    var dir = new System.IO.DirectoryInfo(fi.DirectoryName);
                    var dlls = dir.GetFiles("*.dll", System.IO.SearchOption.AllDirectories);

                    if (System.Environment.UserInteractive)
                    {
                        // TODO: Environment configuration and initialization.
                        System.Console.Title = Properties.Resources.Title;
                        System.Console.Clear();
                        System.Console.CursorVisible = false;
                        System.Console.TreatControlCAsInput = false;
                        System.Console.WriteLine($"Found {dlls.Length} DLLs in {dir.FullName}");
                    }

                    App.EventWaitHandle = new System.Threading.EventWaitHandle(initialState: false, mode: System.Threading.EventResetMode.ManualReset);
                    App.Features = new System.Collections.Concurrent.ConcurrentDictionary<string, MasterMUD.Interfaces.IFeature>(System.StringComparer.OrdinalIgnoreCase);

                    foreach (var dll in dlls)
                        foreach (var feature in System.Reflection.Assembly.LoadFrom(dll.FullName).GetTypes().Where(type => false == type.IsInterface && type.GetInterface(nameof(IFeature)) != null).Select(type => (IFeature)System.Activator.CreateInstance(type)))
                            if (App.Features.TryAdd(feature.Name, feature))
                                System.Console.WriteLine($"Feature '{feature.Name}' added.");
                }
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
                if (App.Features.Count > 0)
                    try
                    {
                        foreach (var feature in App.Features.Values)
                        {
                            feature.Start();
                        }

                        System.Console.CancelKeyPress += Console_CancelKeyPress;

                        try
                        {
                            System.Console.WriteLine("Ready. Press CTRL+C to terminate.");
                            App.EventWaitHandle.WaitOne();
                        }
                        catch (System.Exception ex)
                        {
                            System.Console.Error.WriteLine(ex);
                        }

                        System.Console.CancelKeyPress -= Console_CancelKeyPress;

                        foreach (var feature in App.Features.Values)
                        {
                            feature.Stop();
                        }
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
                    System.Console.WriteLine("Terminating.");
                }
            }
        }

        private static void Console_CancelKeyPress(object sender, System.ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            Terminate();
        }
    }
}