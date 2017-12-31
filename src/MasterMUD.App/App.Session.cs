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
            public string Address { get; }

            public int Port { get; }

            protected internal Session(string address, int port)
            {
                this.Address = address;
                this.Port = port;
            }
        }
    }
}