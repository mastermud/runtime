using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace MasterMUD
{
    public sealed partial class App
    {
        private sealed class TcpListener : System.Net.Sockets.TcpListener
        {
            public new bool Active => true == (this.ListenerSubscription != null) && true == base.Active;

            private volatile System.IDisposable ListenerSubscription;

            protected internal System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Concurrent.ConcurrentDictionary<int, System.Net.Sockets.TcpClient>> Connections { get; }
            
            public TcpListener(System.Net.IPAddress localaddr, int port) : base(localaddr, port)
            {
                this.Connections = new System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Concurrent.ConcurrentDictionary<int, System.Net.Sockets.TcpClient>>();
            }

            private async void Connect(System.Net.Sockets.TcpClient connection)
            {
                // Set a locally scoped reference due to potential closure scope-encroaching on our stack (I think it's a bug)
                var oConn = connection;

                // Signal the caller to continue doing whatever is needed next because an infinite loop is going to keep running on this stack forever
                await System.Threading.Tasks.Task.Yield();

                var sRemoteEndPoint = string.Empty;
                var sAddress = string.Empty;
                var iPort = default(int);

                // Try accessing the remote endpoint object to parse the address and port of the incoming connection
                try
                {
                    sRemoteEndPoint = oConn.Client.RemoteEndPoint.ToString();
                }
                catch (Exception ex)
                {
                    App.Log(ex);

                    try
                    {
                        oConn?.Dispose();
                    }
                    finally
                    {
                        oConn = null;
                    }
                }

                if (oConn == null || sRemoteEndPoint == null)
                    return;

                // Parse IPv4 or IPv6 the lazy way
                try
                {                   
                    sAddress = sRemoteEndPoint.Substring(0, sRemoteEndPoint.IndexOf(':'));
                    iPort = int.Parse(sRemoteEndPoint.Substring(sAddress.Length + 1));
                }
                catch (Exception)
                {
                    // Probably IPv6, if not then just disconnect for now
                    try
                    {
                        var ipEndPoint = (System.Net.IPEndPoint)oConn.Client.RemoteEndPoint;
                        sAddress = ipEndPoint.Address.ToString();
                        iPort = ipEndPoint.Port;
                    }
                    catch (Exception ex)
                    {
                        App.Log(ex);

                        try
                        {
                            oConn?.Dispose();
                        }
                        finally
                        {
                            oConn = null;
                        }
                    }
                }

                if (oConn == null)
                    return;
                
                using (oConn)
                    try
                    {
                        App.Current.Listener.Connections.TryAdd(sAddress, new System.Collections.Concurrent.ConcurrentDictionary<int, System.Net.Sockets.TcpClient>());
                        
                        if (App.Current.Listener.Connections[sAddress].TryAdd(iPort, oConn))
                        {
                            using (var stream = oConn.GetStream())
                            {
                                App.Log(message: $"+++ {sAddress} ({iPort})", foregroundColor: ConsoleColor.Green);

                                do
                                {
                                    var r = stream.ReadByte();

                                    if (r < 1)
                                        break;

                                    stream.WriteByte((byte)r);
                                } while (true);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        App.Log($"!!! {sAddress} ({iPort}) -> {ex}");
                    }
                    finally
                    {
                        App.Log(message: $"--- {sAddress} ({iPort})", foregroundColor: ConsoleColor.Green);
                    }
            }

            public new void Start() => this.Start(backlog: -1);

            public new void Start(int backlog)
            {
                if (true == this.Active)
                    return;

                if (backlog <= 1)
                    backlog = 1;

                try
                {
                    base.Start(backlog: backlog);                    
                }
                catch (Exception ex)
                {
                    App.Log(ex);
                }
                finally
                {
                    if (true == base.Active)
                    {
                        this.ListenerSubscription = Observable.While(() => true == base.Active, Observable.FromAsync(base.AcceptTcpClientAsync)).Subscribe(this.Connect);
                    }
                }
            }

            public new void Stop()
            {
                if (false == base.Active)
                    return;

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
                    if (false == base.Active && this.ListenerSubscription != null)
                    {
                        try
                        {
                            this.ListenerSubscription.Dispose();
                        }
                        finally
                        {
                            this.ListenerSubscription = null;
                        }                        
                    }
                }
            }
        }
    }
}