using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;

namespace MasterMUD
{
    public sealed partial class App
    {
        public sealed class Session : System.IDisposable
        {
            private static readonly ConcurrentStack<Session> Pool;
            internal static readonly HashSet<Session> Connections;

            static Session()
            {
                var pool = new Session[byte.MaxValue];
                for (var i = 0; i < pool.Length; i++)
                    pool[i] = new Session();

                Session.Pool = new ConcurrentStack<Session>(pool);
                Session.Connections = new HashSet<Session>();
            }

            internal static void Connect(string address, int port, System.Net.Sockets.TcpClient connection)
            {
                if (!Session.Pool.TryPop(out var client))
                    client = new Session();

                client.Init(address, port, connection);
            }

            private readonly byte[] InputBuffer;
            private volatile int InputBufferIndex;
            private volatile byte InputByte;
            private volatile char InputChar;
            private readonly ISubject<string> CommandSubject;
            private readonly IObservable<string> CommandObservable;

            public string Address { get; private set; }

            public int Port { get; private set; }

            private volatile bool IsDisposed;

            private volatile System.Net.Sockets.NetworkStream Stream;

            private volatile System.IObservable<byte> InputObservable;

            private System.IDisposable InputObservableSubscription;

            private volatile System.Text.StringBuilder InputStringBuilder;

            private Session()
            {
                this.InputBuffer = new byte[80];
                this.InputBufferIndex = 0;
                this.InputByte = default(byte);
                this.InputChar = default(char);
                this.InputStringBuilder = new StringBuilder();
                this.CommandSubject = new Subject<string>();
                this.CommandObservable = this.CommandSubject.AsObservable();
            }

            private void Init(string address, int port, System.Net.Sockets.TcpClient connection)
            {
                this.IsDisposed = false;
                this.Address = address;
                this.Port = port;
                this.Stream = connection.GetStream();
                this.Stream.WriteByte(0xFF);
                this.Stream.WriteByte(0xFB);
                this.Stream.WriteByte(0x01);
                this.Stream.ReadAsync(this.InputBuffer, 0, this.InputBuffer.Length);
                this.Stream.WriteByte((byte)'>');
                this.InputObservable = System.Reactive.Linq.Observable.ToObservable<byte>(this.Read());
                Session.Connections.Add(this);
                App.Log($"+++ {this.Address} ({this.Port})");
                this.InputObservableSubscription = this.InputObservable.Subscribe(this.Read);
            }

            private IEnumerable<byte> Read()
            {
                int? i = -1;

                for (; ; )
                {
                    if (true == this.IsDisposed)
                        break;

                    i = this.Stream?.ReadByte();

                    if (i == null || i < 1)
                    {
                        this.Dispose(disposing: true);
                        break;
                    }
                    else
                    {
                        yield return (byte)i;
                    }
                }

                yield break;
            }

            private void Read(byte data)
            {
                switch (data)
                {
                    case 13:
                        if (this.InputBufferIndex > 0)
                        {
                            if (this.InputBufferIndex > 0)
                            {
                                this.InputStringBuilder = this.InputStringBuilder.Append(System.Text.Encoding.ASCII.GetString(this.InputBuffer, 0, this.InputBufferIndex + 1).Trim());
                                this.InputBufferIndex = 0;

                                if (this.InputStringBuilder.Length == 0)
                                    return;

                                this.Stream?.WriteByte(10);
                                this.Stream?.WriteByte(13);

                                App.Current.Process(this, this.InputStringBuilder.ToString());
                                this.InputStringBuilder = this.InputStringBuilder.Clear();
                                break;
                            }
                        }
                        break;
                    case 08:
                        if (this.InputBuffer[this.InputBufferIndex] != 0)
                        {
                            this.InputBuffer[this.InputBufferIndex] = 0;

                            if (this.InputBufferIndex > 0)
                            {
                                this.InputBufferIndex -= 1;
                                this.Stream?.WriteByte(data);
                            }
                        }
                        break;
                    default:
                        if (this.InputBufferIndex + 1 < this.InputBuffer.Length && false == Char.IsControl(this.InputChar = (char)(this.InputByte = (byte)data)))
                        {
                            this.Stream?.WriteByte(this.InputBuffer[this.InputBufferIndex] = this.InputByte);
                            this.InputBufferIndex += 1;
                        }
                        break;
                }
            }

            public void Send(byte[] data)
            {
                if (this.InputBufferIndex == 0)
                    for (var i = 0; i < data.Length; i++)
                        this.Stream?.WriteByte(data[i]);                                
            }

            private void Dispose(bool disposing)
            {
                if (false == this.IsDisposed)
                {
                    this.IsDisposed = true;

                    if (disposing)
                        try
                        {
                            App.Log($"--- {this.Address} ({this.Port})");
                            this.Address = null;
                            this.Port = -1;
                            this.InputObservable = null;
                            this.Stream?.Dispose();
                        }
                        finally
                        {
                            this.Stream = null;
                            this.InputObservable = null;

                            for (var i = 0; i < this.InputBufferIndex; i++)
                                this.InputBuffer[i] = 0;

                            this.InputBufferIndex = 0;
                            this.InputByte = default(byte);
                            this.InputChar = default(char);
                            this.InputStringBuilder = this.InputStringBuilder.Clear();
                            Session.Connections.Remove(this);
                            Session.Pool.Push(this);
                        }
                }
            }

            public void Dispose()
            {
                this.Dispose(disposing: true);
                System.GC.SuppressFinalize(this);
            }

            ~Session()
            {
                this.Dispose(disposing: false);
            }
        }
    }
}
