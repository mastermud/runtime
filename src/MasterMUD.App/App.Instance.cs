using System;
using System.Linq;

namespace MasterMUD
{
    public sealed partial class App : App.IApp
    {
        /// <summary>
        ///     The <see cref="System.DateTime"/> this object instance was initialized via its private constructor.
        /// </summary>
        public System.DateTime Activated { get; }

        /// <summary>
        ///     Retains a reactive timer for the lifetime of the application.
        /// </summary>
        private readonly System.IDisposable ActivationSubscription;

        /// <summary>
        ///     The full file system path to the directory containing implementations of <see cref="MasterMUD.App.IPlugin"/>.
        /// </summary>
        private readonly string PluginsPath;

        /// <summary>
        ///     The plugins identified during initialization.
        /// </summary>
        private System.Collections.Concurrent.ConcurrentDictionary<string, App.IPlugin> Plugins { get; }

        /// <summary>
        ///     Loads plugins and initializes the runtime.
        /// </summary>
        /// <exception cref="System.InvalidProgramException">Only one running instance is allowed.</exception>
        private App()
        {
            this.Plugins = new System.Collections.Concurrent.ConcurrentDictionary<string, App.IPlugin>(System.StringComparer.OrdinalIgnoreCase);
            this.PluginsPath = System.IO.Path.Combine(new System.IO.DirectoryInfo(new System.IO.FileInfo(System.Environment.GetCommandLineArgs()[0]).DirectoryName).FullName, nameof(App.Plugins));

            foreach (var dll in (System.IO.Directory.Exists(this.PluginsPath) ? new System.IO.DirectoryInfo(this.PluginsPath) : System.IO.Directory.CreateDirectory(this.PluginsPath)).GetFiles("*.dll", System.IO.SearchOption.AllDirectories))
                foreach (var feature in System.Reflection.Assembly.LoadFrom(dll.FullName).GetTypes().Where(type => false == type.IsInterface && type.GetInterface(nameof(App.IPlugin)) != null).Select(type => (App.IPlugin)System.Activator.CreateInstance(type)).Where(feature => true == this.Plugins.TryAdd(feature.Name, feature)))
                    continue;

            this.Activated = System.DateTime.Now;
            this.ActivationSubscription = System.Reactive.Linq.Observable.Interval(System.TimeSpan.FromMilliseconds(App.ApplicationTickRateMilliseconds)).Subscribe(this.Tick);
        }

        private void Start()
        {
            foreach (var feature in this.Plugins)
                try
                {
                    feature.Value.Start();
                }
                catch (Exception ex)
                {
                    App.Log(ex);
                }

            System.Console.CancelKeyPress += App.Console_CancelKeyPress;

            App.Log(Properties.Resources.Ready);
        }

        private void Stop()
        {
            System.Console.CancelKeyPress -= App.Console_CancelKeyPress;

            this.ActivationSubscription.Dispose();

            foreach (var feature in this.Plugins)
                try
                {
                    feature.Value.Stop();
                }
                catch (Exception ex)
                {
                    App.Log(ex);
                }
        }

        /// <summary>
        ///     
        /// </summary>
        /// <param name="tick"></param>
        private async void Tick(long tick)
        {
            var msg = $"Tick {tick + 1}";

            await System.Threading.Tasks.Task.Yield();

            App.Log(msg);
        }

        public interface IApp
        {
            System.DateTime Activated { get; }
        }
    }
}