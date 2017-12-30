using System;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;

namespace MasterMUD
{
    public sealed partial class App
    {
        private sealed class TcpListener : System.Net.Sockets.TcpListener
        {
            public new bool Active => base.Active;

            public IPAddress LocalAddress { get; }

            public int LocalPort { get; }

            public TcpListener(IPAddress localaddr, int port) : base(localaddr, port)
            {
                this.LocalAddress = localaddr;
                this.LocalPort = port;
            }

            public new void Start() => this.Start(backlog: -1);

            public new void Start(int backlog)
            {
                if (true == this.Active)
                    return;

                try
                {
                    base.Start(backlog: backlog < 1 ? 1 : backlog);
                    App.Log($"Started listening on {this.LocalAddress} ({this.LocalPort})");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }

            public new void Stop()
            {
                if (false == this.Active)
                    return;

                try
                {
                    base.Stop();
                    App.Log($"Stopped listening on {this.LocalAddress} ({this.LocalPort})");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }
        }
    }
}