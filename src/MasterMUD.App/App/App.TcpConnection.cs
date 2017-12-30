using System;
using System.Linq;

namespace MasterMUD
{
    public sealed partial class App
    {
        public sealed class TcpConnection
        {
            public bool IsConnected => this.Connection != null;

            private volatile System.Net.Sockets.TcpClient Connection;

            private readonly App.TcpListener Listener;

            internal static TcpConnection Create(App.TcpListener listener) => new TcpConnection(listener);

            private TcpConnection(App.TcpListener listener)
            {
                this.Listener = listener;
            }

            protected internal TcpConnection Connect(System.Net.Sockets.TcpClient connection)
            {
                if (true == this.IsConnected)
                    throw new InvalidOperationException();

                this.Connection = connection;

                return this;
            }
            
            protected internal void Disconnect()
            {
                if (this.IsConnected)
                {
                    var oldConnection = this.Connection;

                    this.Connection = null;

                    try
                    {
                        oldConnection.Dispose();
                    }
                    catch (Exception ex)
                    {
                        App.Log(ex);
                    }
                }
            }            
        }
    }
}
