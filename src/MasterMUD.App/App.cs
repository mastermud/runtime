using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace MasterMUD
{
    public sealed partial class App
    {
        /// <summary>
        ///     The tickrate for the application.
        /// </summary>
        private const int ApplicationTickRateMilliseconds = 1000 * 6;

        /// <summary>
        ///     Ensures only one running instance is allowed.
        /// </summary>
        private static readonly System.Threading.Mutex Mutex;

        /// <summary>
        ///     Keeps the application alive forever.
        /// </summary>
        private static readonly System.Threading.EventWaitHandle EventWaitHandle;

        /// <summary>
        ///     The actual object instance referenced by <see cref="App.Current"/>.
        /// </summary>
        private static readonly Lazy<App> Instance;

        /// <summary>
        ///     Singleton instance of the application at runtime.
        /// </summary>
        public static App.IApp Current => App.Instance.Value;

        /// <summary>
        ///     Initializes the environment and performs all required work before Main is allowed to run.
        /// </summary>
        /// <remarks>If anything goes wrong in here, we need to do our very best to shutdown the running application.</remarks>
        static App()
        {
            try
            {
                System.Console.Title = Properties.Resources.Title;
                System.Console.Clear();
                System.Console.CursorVisible = false;
                System.Console.TreatControlCAsInput = false;
                App.EventWaitHandle = new System.Threading.EventWaitHandle(initialState: false, mode: System.Threading.EventResetMode.ManualReset);
                App.Instance = new Lazy<App>(() => new App());
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
            // Prevent multiple calls to Main
            if (false == App.Instance.IsValueCreated)
                using (App.Mutex)
                using (App.EventWaitHandle)
                    try
                    {
                        App.Instance.Value.Start();
                    }
                    catch (Exception ex)
                    {
                        App.Log(ex);
                    }
                    finally
                    {
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
                            App.Instance.Value.Stop();
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