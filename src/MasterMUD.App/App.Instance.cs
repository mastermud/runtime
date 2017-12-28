using System;
using System.Linq;

namespace MasterMUD
{
    public sealed partial class App
    {
        public System.DateTime Activated { get; } = System.DateTime.Now;

        private readonly System.IDisposable ActivationSubscription;

        /// <summary>
        ///     Ensures only one running instance is allowed.
        /// </summary>
        private readonly System.Threading.Mutex Mutex;

        /// <summary>
        ///     Keeps the application alive forever.
        /// </summary>
        private readonly System.Threading.EventWaitHandle EventWaitHandle;

        private readonly TcpListener Listener;

        internal System.Collections.Concurrent.ConcurrentDictionary<string, App.IPlugin> Plugins { get; }

        /// <summary>
        ///     Loads plugins and initializes the runtime.
        /// </summary>
        /// <exception cref="System.InvalidProgramException">Only one running instance is allowed.</exception>
        private App(string pluginsPath = null)
        {
            if (true == string.IsNullOrEmpty(pluginsPath))
                pluginsPath = System.IO.Path.Combine(new System.IO.DirectoryInfo(new System.IO.FileInfo(System.Environment.GetCommandLineArgs()[0]).DirectoryName).FullName, nameof(App.Plugins));

            System.Console.Title = Properties.Resources.Title;
            System.Console.Clear();
            System.Console.CursorVisible = false;
            System.Console.TreatControlCAsInput = false;

            this.EventWaitHandle = new System.Threading.EventWaitHandle(initialState: false, mode: System.Threading.EventResetMode.ManualReset);
            this.Plugins = new System.Collections.Concurrent.ConcurrentDictionary<string, App.IPlugin>(System.StringComparer.OrdinalIgnoreCase);
            this.Mutex = new System.Threading.Mutex(initiallyOwned: true, name: nameof(MasterMUD), createdNew: out var createdNew);

            if (!createdNew)
                throw new System.InvalidProgramException(Properties.Resources.StaticConstructorDuplicated);

            foreach (var dll in (System.IO.Directory.Exists(pluginsPath) ? new System.IO.DirectoryInfo(pluginsPath) : System.IO.Directory.CreateDirectory(pluginsPath)).GetFiles("*.dll", System.IO.SearchOption.AllDirectories))
                foreach (var feature in System.Reflection.Assembly.LoadFrom(dll.FullName).GetTypes().Where(type => false == type.IsInterface && type.GetInterface(nameof(App.IPlugin)) != null).Select(type => (App.IPlugin)System.Activator.CreateInstance(type)).Where(feature => true == this.Plugins.TryAdd(feature.Name, feature)))
                    continue;

            this.Listener = new TcpListener(localaddr: System.Net.IPAddress.Loopback, port: 23);
            this.ActivationSubscription = System.Reactive.Linq.Observable.Interval(System.TimeSpan.FromSeconds(6)).Subscribe(this.Tick);
            this.Listener.Start();
        }

        /// <summary>
        ///     Signals the <see cref="App.EventWaitHandle"/> to set and reset.
        /// </summary>
        protected internal void Shutdown()
        {
            App.Current.Listener.Stop();
            App.Current.ActivationSubscription.Dispose();

            foreach (var feature in App.Current.Plugins)
                try
                {
                    feature.Value.Stop();
                }
                catch (Exception ex)
                {
                    App.Log(ex);
                }
        }

        private async void Tick(long tick)
        {
            await System.Threading.Tasks.Task.Yield();

            System.Diagnostics.Debug.WriteLine("Tick {0}", tick + 1);
        }
    }
}