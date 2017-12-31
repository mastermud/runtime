using System;
using System.Linq;
using System.Reactive.Linq;

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
        private volatile System.IDisposable ActivationSubscription;

        private readonly TcpListener Listener;

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
            System.Console.CancelKeyPress += App.Console_CancelKeyPress;
            System.Console.Title = Properties.Resources.Title;
            System.Console.Clear();
            System.Console.CursorVisible = false;
            System.Console.TreatControlCAsInput = false;

            this.Listener = new TcpListener(System.Net.IPAddress.Any, 23);
            this.Plugins = new System.Collections.Concurrent.ConcurrentDictionary<string, App.IPlugin>(System.StringComparer.OrdinalIgnoreCase);
            this.PluginsPath = System.IO.Path.Combine(new System.IO.DirectoryInfo(new System.IO.FileInfo(System.Environment.GetCommandLineArgs()[0]).DirectoryName).FullName, nameof(App.Plugins));

            foreach (var dll in (System.IO.Directory.Exists(this.PluginsPath) ? new System.IO.DirectoryInfo(this.PluginsPath) : System.IO.Directory.CreateDirectory(this.PluginsPath)).GetFiles("*.dll", System.IO.SearchOption.AllDirectories))
                foreach (var feature in System.Reflection.Assembly.LoadFrom(dll.FullName).GetTypes().Where(type => false == type.IsInterface && type.GetInterface(nameof(App.IPlugin)) != null).Select(type => (App.IPlugin)System.Activator.CreateInstance(type)).Where(feature => true == this.Plugins.TryAdd(feature.Name, feature)))
                    continue;

            this.ActivationSubscription = System.Reactive.Linq.Observable.Interval(System.TimeSpan.FromMilliseconds(App.ApplicationTickRateMilliseconds)).Subscribe(this.Tick);
            this.Activated = System.DateTime.Now;
        }

        private void Start()
        {
            foreach (var plugin in this.Plugins.Values.Where(plugin => false == plugin.Active).Select(plugin => plugin.Name).ToArray())
                try
                {
                    this.Plugins[plugin].Start();
                }
                catch (Exception ex)
                {
                    App.Log(ex);
                }

            this.Listener.Start();

            System.Console.CancelKeyPress += App.Console_CancelKeyPress;
        }

        private void Stop()
        {
            System.Console.CancelKeyPress -= App.Console_CancelKeyPress;

            this.Listener.Stop();

            foreach (var feature in this.Plugins.Values.Where(plugin => true == plugin.Active).Select(plugin => plugin.Name).ToArray())
                try
                {
                    this.Plugins[feature].Stop();
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

        private sealed class TcpListener : System.Net.Sockets.TcpListener
        {
            public new bool Active => base.Active;

            public System.Net.IPAddress Address { get; }

            public int Port { get; }

            public TcpListener(System.Net.IPAddress localaddr, int port) : base(localaddr, port)
            {
                this.Address = localaddr;
                this.Port = port;
            }

            private async void Connect(System.Net.Sockets.TcpClient connection)
            {
                var conn = connection;

                await System.Threading.Tasks.Task.Yield();

                var ipendpoint = (System.Net.IPEndPoint)conn.Client.RemoteEndPoint;

                App.Log($"+++ {ipendpoint.Address} ({ipendpoint.Port})");
                
                conn.Close();

                App.Log($"--- {ipendpoint.Address} ({ipendpoint.Port})");
            }

            public new void Start()
            {
                this.Start(backlog: -1);
            }

            public new void Start(int backlog)
            {
                if (true == this.Active)
                    return;

                try
                {
                    base.Start(backlog: backlog < 1 ? 1 : backlog);

                    Observable.While(() => true == this.Active, Observable.FromAsync(base.AcceptTcpClientAsync)).Subscribe(this.Connect);
                }
                catch (System.Exception ex)
                {
                    App.Log(ex);
                }
            }

            public new void Stop()
            {
                if (false == this.Active)
                    return;

                try
                {
                    base.Stop();
                }
                catch (System.Exception ex)
                {
                    App.Log(ex);
                }
            }
        }
    }
}