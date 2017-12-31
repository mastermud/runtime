using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace MasterMUD
{
    public sealed partial class App
    {
        public interface IApp
        {
            System.DateTime Activated { get; }

            bool Active { get; }

            System.Net.IPAddress Address { get; }

            int Port { get; }
        }

        /// <summary>
        ///     The tickrate for the application.
        /// </summary>
        private const int ApplicationTickRateMilliseconds = 1000 * 6;

        /// <summary>
        ///     Used to hide the implementation details of <see cref="App.Current"/> and to prevent multiple calls to <see cref="App.Main"/>.
        /// </summary>
        private static readonly Lazy<App> Context;

        /// <summary>
        ///     Ensures only one running instance is allowed.
        /// </summary>
        private static readonly System.Threading.Mutex Mutex;

        /// <summary>
        ///     Keeps the application alive forever.
        /// </summary>
        private static readonly System.Threading.EventWaitHandle EventWaitHandle;

        /// <summary>
        ///     Singleton instance of the application at runtime.
        /// </summary>
        public static App.IApp Current => App.Context.Value;

        /// <summary>
        ///     Ensures only one instance of the application is permitted to run.
        /// </summary>
        /// <remarks>If anything goes wrong in here, we need to do our very best to shutdown the running application.</remarks>
        static App()
        {
            try
            {
                App.Context = new Lazy<App>(() => new App(localaddr: System.Net.IPAddress.Any, port: 23));
                App.EventWaitHandle = new System.Threading.EventWaitHandle(initialState: false, mode: System.Threading.EventResetMode.ManualReset);
                App.Mutex = new System.Threading.Mutex(initiallyOwned: true, name: nameof(MasterMUD), createdNew: out var createdNew);

                if (!createdNew)
                    throw new System.InvalidProgramException(Properties.Resources.StaticConstructorDuplicated);
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine(ex.Message);
                System.Environment.Exit(ex.HResult);
            }
        }

        /// <summary>
        ///     Entry-point for the application is invoked prior to the static constructor.
        /// </summary>
        private static void Main()
        {
            // Disallow multiple calls into the main thread.
            if (false == App.Context.IsValueCreated)
                using (App.Mutex)
                using (App.EventWaitHandle)
                    try
                    {
                        App.Context.Value.Start();
                    }
                    catch (Exception ex)
                    {
                        App.Log(ex);
                    }
                    finally
                    {
                        App.Log(Properties.Resources.Ready);

                        try
                        {
                            App.EventWaitHandle.WaitOne();
                        }
                        catch (Exception ex)
                        {
                            App.Log(ex);
                        }
                        finally
                        {
                            App.Context.Value.Stop();
                        }
                    }
        }

        private static void Console_CancelKeyPress(object sender, System.ConsoleCancelEventArgs e)
        {
            try
            {
                App.EventWaitHandle.Set();
            }
            catch (System.Exception ex)
            {
                App.Log(ex);
            }
            finally
            {
                try
                {
                    App.EventWaitHandle.Reset();
                }
                catch (System.Exception ex2)
                {
                    App.Log(ex2);
                }
                finally
                {
                    e.Cancel = true;
                }
            }
        }
    }
}