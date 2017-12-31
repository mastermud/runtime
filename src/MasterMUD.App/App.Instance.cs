using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace MasterMUD
{
    public sealed partial class App : System.Net.Sockets.TcpListener, App.IApp
    {
        /// <summary>
        ///     The <see cref="System.DateTime"/> this object instance was initialized via its private constructor.
        /// </summary>
        public System.DateTime Activated { get; }

        /// <summary>
        ///     Whether the application is currently active.
        /// </summary>
        public new bool Active => base.Active;

        /// <summary>
        ///     The address accompanying <see cref="App.Port"/> on which to listen for connections.
        /// </summary>
        public System.Net.IPAddress Address { get; }

        public System.Threading.CancellationToken CancellationToken => this.CancellationTokenSource.Token;

        /// <summary>
        ///     The port accompanying <see cref="App.Address"/> on which to listen for connections.
        /// </summary>
        public int Port { get; }

        /// <summary>
        ///     Retains a reactive timer for the lifetime of the application.
        /// </summary>
        private volatile System.IDisposable ActivationSubscription;

        private readonly System.Collections.Generic.HashSet<string> BannedAddresses;

        /// <summary>
        ///     Provides task cancellation capabilities.
        /// </summary>
        private readonly System.Threading.CancellationTokenSource CancellationTokenSource;

        /// <summary>
        ///     The full file system path to the directory containing implementations of <see cref="MasterMUD.App.IPlugin"/>.
        /// </summary>
        private readonly string PluginsPath;

        /// <summary>
        ///     The plugins identified during initialization.
        /// </summary>
        private System.Collections.Concurrent.ConcurrentDictionary<string, App.IPlugin> Plugins { get; }

        private readonly System.Collections.Generic.HashSet<App.Session> Sessions;

        /// <summary>
        ///     Loads plugins and initializes the runtime.
        /// </summary>
        /// <exception cref="System.InvalidProgramException">Only one running instance is allowed.</exception>
        private App(System.Net.IPAddress localaddr, int port) : base(localaddr: localaddr, port: port)
        {
            System.Console.CancelKeyPress += App.Console_CancelKeyPress;
            System.Console.Title = Properties.Resources.Title;
            System.Console.Clear();
            System.Console.CursorVisible = false;
            System.Console.TreatControlCAsInput = false;

            this.BannedAddresses = new System.Collections.Generic.HashSet<string>();
            this.CancellationTokenSource = new System.Threading.CancellationTokenSource();
            this.Plugins = new System.Collections.Concurrent.ConcurrentDictionary<string, App.IPlugin>(System.StringComparer.OrdinalIgnoreCase);
            this.PluginsPath = System.IO.Path.Combine(new System.IO.DirectoryInfo(new System.IO.FileInfo(System.Environment.GetCommandLineArgs()[0]).DirectoryName).FullName, nameof(App.Plugins));
            this.Sessions = new System.Collections.Generic.HashSet<Session>();

            foreach (var dll in (System.IO.Directory.Exists(this.PluginsPath) ? new System.IO.DirectoryInfo(this.PluginsPath) : System.IO.Directory.CreateDirectory(this.PluginsPath)).GetFiles("*.dll", System.IO.SearchOption.AllDirectories))
                foreach (var feature in System.Reflection.Assembly.LoadFrom(dll.FullName).GetTypes().Where(type => false == type.IsInterface && type.GetInterface(nameof(App.IPlugin)) != null).Select(type => (App.IPlugin)System.Activator.CreateInstance(type)).Where(feature => true == this.Plugins.TryAdd(feature.Name, feature)))
                    continue;

            this.ActivationSubscription = System.Reactive.Linq.Observable.Interval(System.TimeSpan.FromMilliseconds(App.ApplicationTickRateMilliseconds)).Subscribe(this.Tick);
            this.Activated = System.DateTime.Now;
        }

        private async void ConnectAsync(System.Net.Sockets.TcpClient connection)
        {
            string sAddress;
            int iPort;
            var oConnection = connection;

            await Task.Yield();

            try
            {
                var sRemoteEndPoint = oConnection.Client.RemoteEndPoint.ToString();
                sAddress = sRemoteEndPoint.Substring(0, sRemoteEndPoint.IndexOf(':'));
                iPort = int.Parse(sRemoteEndPoint.Substring(sAddress.Length + 1));

                if (this.BannedAddresses.Contains(sAddress))
                    throw new System.Security.SecurityException($"{sAddress} ({iPort}) is banned.");
            }
            catch (Exception ex)
            {
                App.Log(ex);

                try
                {
                    oConnection.Dispose();
                }
                catch (Exception ex2)
                {
                    App.Log(ex2);
                }

                return;
            }

            var session = App.Session.Connect(address: sAddress, port: iPort, connection: oConnection);

            App.Log($"+++ {session.Address} ({session.Port})");

            await Task.Delay(333, this.CancellationToken);

            App.Log($"--- {session.Address} ({session.Port})");

            session.Disconnect();
        }

        private new void Start() => this.Start(backlog: -1);

        private new void Start(int backlog)
        {
            if (true == this.Active)
                return;

            foreach (var plugin in this.Plugins.Values.Where(plugin => false == plugin.Active).ToArray())
                try
                {
                    plugin.Start();
                }
                catch (Exception ex)
                {
                    App.Log(ex);
                }

            try
            {
                base.Start(backlog: backlog < 1 ? 1 : backlog);
                Observable.While(() => true == this.Active, Observable.FromAsync(base.AcceptTcpClientAsync)).Subscribe(this.ConnectAsync);
            }
            catch (Exception ex)
            {
                App.Log(ex);
            }

            System.Console.CancelKeyPress += App.Console_CancelKeyPress;
        }

        private new void Stop()
        {
            if (false == this.Active)
                return;

            System.Console.CancelKeyPress -= App.Console_CancelKeyPress;

            try
            {
                base.Stop();
            }
            catch (Exception ex)
            {
                App.Log(ex);
            }
            finally
            {
                foreach (var plugin in this.Plugins.Values.Where(plugin => true == plugin.Active).ToArray())
                    try
                    {
                        plugin.Stop();
                    }
                    catch (Exception ex)
                    {
                        App.Log(ex);
                    }
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
    }
}