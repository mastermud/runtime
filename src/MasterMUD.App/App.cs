using System;

namespace MasterMUD
{
    public sealed partial class App
    {
        /// <summary>
        ///     Initializes the environment and performs all required work before Main is allowed to run.
        /// </summary>
        /// <remarks>If anything goes wrong in here, we need to do our very best to shutdown the running application.</remarks>
        static App()
        {
            if (System.Environment.UserInteractive)
                try
                {
                    // TODO: Environment configuration and initialization.
                    System.Console.Title = Properties.Resources.Title;
                    System.Console.Clear();
                    System.Console.CursorVisible = false;
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

        private static void Main() => new App.Exception("Main Not Yet Implemented.", new NotImplementedException(nameof(Main)));
    }
}
