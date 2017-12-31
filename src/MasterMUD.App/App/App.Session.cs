using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace MasterMUD
{
    public sealed partial class App
    {
        public sealed class Session
        {
            internal static Session Connect(string address, int port, System.Net.Sockets.TcpClient connection) => new Session(address, port, connection);

            public string Address { get; private set; }

            public int Port { get; private set; }

            private volatile System.Net.Sockets.TcpClient Connection;

            private Session(string address, int port, System.Net.Sockets.TcpClient connection)
            {
                this.Address = address;
                this.Port = port;
                this.Connection = connection;
            }

            protected internal void Disconnect()
            {
                var conn = this.Connection;

                this.Address = null;
                this.Port = 0;
                this.Connection = null;

                conn.Dispose();
            }
        }
    }
}