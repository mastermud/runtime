using System;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;

namespace MasterMUD
{
    public sealed partial class App
    {
        internal sealed class TcpListener : System.Net.Sockets.TcpListener, IObservable<App.TcpConnection>
        {
            public new bool Active => base.Active;

            public IPAddress LocalAddress { get; }

            public int LocalPort { get; }

            protected internal System.Collections.Concurrent.ConcurrentStack<App.TcpConnection> ConnectionPool { get; }

            private readonly System.Reactive.Subjects.ISubject<App.TcpConnection> ConnectionSubject;

            private readonly System.IObservable<App.TcpConnection> ConnectionObservable;

            public TcpListener(IPAddress localaddr, int port) : base(localaddr, port)
            {
                this.LocalAddress = localaddr;
                this.LocalPort = port;

                var arr = new App.TcpConnection[sbyte.MaxValue];
                for (var i = 0; i < arr.Length; i++)
                    arr[i] = App.TcpConnection.Create(this);

                this.ConnectionPool = new System.Collections.Concurrent.ConcurrentStack<TcpConnection>(arr);
                this.ConnectionSubject = new System.Reactive.Subjects.Subject<App.TcpConnection>();
                this.ConnectionObservable = this.ConnectionSubject.AsObservable();
            }

            private App.TcpConnection Connect(System.Net.Sockets.TcpClient tcpClient)
            {
                App.TcpConnection connection = null;

                if (false == this.ConnectionPool.TryPop(out connection) || connection == null)
                    connection = App.TcpConnection.Create(this);

                return connection.Connect(tcpClient);
            }
            
            public new void Start() => this.Start(backlog: -1);

            public new void Start(int backlog)
            {
                if (true == this.Active)
                    return;

                try
                {
                    base.Start(backlog: backlog < 1 ? 1 : backlog);
                    Observable.While(() => true == this.Active, Observable.FromAsync(base.AcceptTcpClientAsync)).Select(this.Connect).Subscribe(this.ConnectionSubject.OnNext);
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

            public IDisposable Subscribe(IObserver<TcpConnection> observer) => this.ConnectionObservable.Subscribe(observer);
        }
    }
}