using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using MasterMUD.Interfaces;

namespace MasterMUD
{
    public sealed partial class App
    {
        /// <summary>
        ///     Singleton instance of the application at runtime.
        /// </summary>
        public static readonly App Current;

        /// <summary>
        ///     Initializes the environment and performs all required work before Main is allowed to run.
        /// </summary>
        /// <remarks>If anything goes wrong in here, we need to do our very best to shutdown the running application.</remarks>
        static App()
        {
            try
            {
                App.Current = new App();
            }
            catch (System.Exception ex)
            {
                App.Log(ex);
            }
        }

        /// <summary>
        ///     Entry-point for the application is invoked prior to the static constructor.
        /// </summary>
        private static void Main()
        {
            using (App.Current.Mutex)
            using (App.Current.EventWaitHandle)
            {
                foreach (var feature in App.Current.Features)
                    try
                    {
                        feature.Value.Start();
                    }
                    catch (Exception ex)
                    {
                        App.Log(ex);
                    }

                App.Log(Properties.Resources.Ready);

                System.Console.CancelKeyPress += Console_CancelKeyPress;

                // TODO: Allow for different modes of execution where the application isn't simply waiting for user-input CTRL+C to terminate.

                try
                {                    
                    App.Current.EventWaitHandle.WaitOne();
                }
                catch (System.Exception ex)
                {
                    App.Log(ex);
                }
                finally
                {
                    System.Console.CancelKeyPress -= Console_CancelKeyPress;

                    App.Current.Terminate();
                }
            }
        }

        private static void Console_CancelKeyPress(object sender, System.ConsoleCancelEventArgs e)
        {
            try
            {
                App.Current.EventWaitHandle.Set();
            }
            catch (System.Exception ex)
            {
                App.Log(ex);
            }
            finally
            {
                try
                {
                    App.Current.EventWaitHandle.Reset();
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