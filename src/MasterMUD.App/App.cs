using System.Linq;
using MasterMUD.Interfaces;

namespace MasterMUD
{
    public sealed partial class App
    {
        private static readonly object Lock = new object();

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

        private static void Main()
        {
            using (App.Current.Mutex)
            using (App.Current.EventWaitHandle)
                if (App.Current.Features.Count > 0)
                    try
                    {
                        foreach (var feature in App.Current.Features.Values)
                            feature.Start();

                        System.Console.CancelKeyPress += Console_CancelKeyPress;

                        App.Log(Properties.Resources.Ready);

                        try
                        {
                            App.Current.EventWaitHandle.WaitOne();
                        }
                        catch (System.Exception ex)
                        {
                            App.Log(ex);
                        }

                        System.Console.CancelKeyPress -= Console_CancelKeyPress;

                        foreach (var feature in App.Current.Features.Values)
                            feature.Stop();
                    }
                    catch (System.Exception ex)
                    {
                        App.Log(ex);
                    }
        }

        private static void Console_CancelKeyPress(object sender, System.ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            App.Current.Terminate();
        }
    }
}