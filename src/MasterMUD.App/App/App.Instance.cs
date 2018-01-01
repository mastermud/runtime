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

            foreach (var dll in (System.IO.Directory.Exists(this.PluginsPath) ? new System.IO.DirectoryInfo(this.PluginsPath) : System.IO.Directory.CreateDirectory(this.PluginsPath)).GetFiles("*.dll", System.IO.SearchOption.AllDirectories))
                foreach (var feature in System.Reflection.Assembly.LoadFrom(dll.FullName).GetTypes().Where(type => false == type.IsInterface && type.GetInterface(nameof(App.IPlugin)) != null).Select(type => (App.IPlugin)System.Activator.CreateInstance(type)).Where(feature => true == this.Plugins.TryAdd(feature.Name, feature)))
                    continue;

            this.ActivationSubscription = System.Reactive.Linq.Observable.Interval(System.TimeSpan.FromMilliseconds(App.ApplicationTickRateMilliseconds)).Subscribe(this.Tick);
            this.Activated = System.DateTime.Now;
        }

        private async void ConnectAsync(System.Net.Sockets.TcpClient connection)
        {
            using (var oConnection = connection)
            {
                await Task.Yield();

                try
                {
                    var sRemoteEndPoint = oConnection.Client.RemoteEndPoint.ToString();
                    var sAddress = sRemoteEndPoint.Substring(0, sRemoteEndPoint.IndexOf(':'));
                    var iPort = int.Parse(sRemoteEndPoint.Substring(sAddress.Length + 1));

                    if (this.BannedAddresses.Contains(sAddress))
                        throw new System.Security.SecurityException($"{sAddress} ({iPort}) is banned.");

                    var r = 0;
                    var i = 0;
                    var b = default(byte);
                    var c = default(char);
                    var input = new byte[80];
                    var prompt = (byte)62;

                    App.Log($"+++ {sAddress} ({iPort})");

                    using (var stream = oConnection.GetStream())
                    {
                        stream.WriteByte(0xFF);
                        stream.WriteByte(0xFB);
                        stream.WriteByte(0x01);

                        await stream.ReadAsync(input, 0, input.Length, this.CancellationToken);                        

                        do
                        {
                            stream.WriteByte(prompt);

                            do
                            {
                                try
                                {
                                    if ((r = stream.ReadByte()) < 1)
                                        break;

                                    if (r == 13)
                                    {
                                        if (i > 0)
                                        {
                                            var cmd = System.Text.Encoding.ASCII.GetString(input, 0, i + 1).Trim();
                                            i = 0;

                                            if (string.IsNullOrEmpty(cmd))
                                                continue;
                                            
                                            stream.WriteByte(10);
                                            stream.WriteByte(13);

                                            App.Log($"{sAddress} ({iPort})> {cmd}");

                                            await Task.Delay(33, this.CancellationToken);
                                            break;
                                        }

                                        continue;
                                    }

                                    if (r == 08)
                                    {
                                        if (input[i] != 0)
                                        {
                                            input[i] = 0;

                                            if (i > 0)
                                            {
                                                i -= 1;
                                                stream.WriteByte(08);
                                            }
                                        }
                                        continue;
                                    }

                                    if (i + 1 < input.Length && false == Char.IsControl(c = (char)(b = (byte)r)))
                                    {
                                        stream.WriteByte(input[i] = b);
                                        i += 1;
                                        continue;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    r = -1;
                                    App.Log(ex);                                    
                                }
                            } while (true);
                        } while (r > 0);
                    }

                    App.Log($"--- {sAddress} ({iPort})");
                }
                catch (Exception ex)
                {
                    App.Log(ex);
                }
            }
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

                this.CancellationTokenSource.Cancel(throwOnFirstException: true);
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