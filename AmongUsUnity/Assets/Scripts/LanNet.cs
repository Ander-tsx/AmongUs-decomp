using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace AmongUsLan
{
    /// <summary>Line-based TCP transport. Host accepts clients; clients talk only to the host.</summary>
    public class LanPeer : IDisposable
    {
        public const int TcpPort = 47777;

        public readonly ConcurrentQueue<KeyValuePair<int, string>> Inbox = new ConcurrentQueue<KeyValuePair<int, string>>();
        public bool IsHost { get; private set; }

        readonly Dictionary<int, TcpClient> _clients = new Dictionary<int, TcpClient>();
        readonly Dictionary<int, StreamWriter> _writers = new Dictionary<int, StreamWriter>();
        readonly object _lock = new object();
        TcpListener _listener;
        TcpClient _server;
        StreamWriter _serverWriter;
        int _nextId = 1;
        volatile bool _running;

        public static LanPeer StartHost()
        {
            var p = new LanPeer { IsHost = true, _running = true };
            p._listener = new TcpListener(IPAddress.Any, TcpPort);
            p._listener.Start();
            new Thread(p.AcceptLoop) { IsBackground = true }.Start();
            return p;
        }

        public static LanPeer Connect(string host)
        {
            var p = new LanPeer { IsHost = false, _running = true };
            p._server = new TcpClient { NoDelay = true };
            var ar = p._server.BeginConnect(host, TcpPort, null, null);
            if (!ar.AsyncWaitHandle.WaitOne(3000) || !p._server.Connected)
            {
                p._server.Close();
                throw new IOException("No se pudo conectar a " + host);
            }
            p._server.EndConnect(ar);
            p._serverWriter = new StreamWriter(p._server.GetStream(), new UTF8Encoding(false)) { AutoFlush = true };
            new Thread(() => p.ReadLoop(0, p._server)) { IsBackground = true }.Start();
            return p;
        }

        void AcceptLoop()
        {
            while (_running)
            {
                try
                {
                    var c = _listener.AcceptTcpClient();
                    c.NoDelay = true;
                    int id;
                    lock (_lock)
                    {
                        id = _nextId++;
                        _clients[id] = c;
                        _writers[id] = new StreamWriter(c.GetStream(), new UTF8Encoding(false)) { AutoFlush = true };
                    }
                    new Thread(() => ReadLoop(id, c)) { IsBackground = true }.Start();
                    Inbox.Enqueue(new KeyValuePair<int, string>(id, "CONNECT"));
                }
                catch { if (!_running) return; }
            }
        }

        void ReadLoop(int id, TcpClient c)
        {
            try
            {
                var r = new StreamReader(c.GetStream(), Encoding.UTF8);
                string line;
                while (_running && (line = r.ReadLine()) != null)
                    Inbox.Enqueue(new KeyValuePair<int, string>(id, line));
            }
            catch { }
            Inbox.Enqueue(new KeyValuePair<int, string>(id, "DISCONNECT"));
            if (IsHost) Drop(id);
        }

        void Drop(int id)
        {
            lock (_lock)
            {
                if (_clients.TryGetValue(id, out var c)) { try { c.Close(); } catch { } }
                _clients.Remove(id);
                _writers.Remove(id);
            }
        }

        /// <summary>Host: send to one client id. Client: send to host (id ignored).</summary>
        public void Send(int id, string msg)
        {
            try
            {
                if (IsHost)
                {
                    StreamWriter w;
                    lock (_lock) _writers.TryGetValue(id, out w);
                    w?.WriteLine(msg);
                }
                else _serverWriter?.WriteLine(msg);
            }
            catch { if (IsHost) Drop(id); }
        }

        public void Broadcast(string msg)
        {
            int[] ids;
            lock (_lock) { ids = new int[_writers.Count]; _writers.Keys.CopyTo(ids, 0); }
            foreach (var id in ids) Send(id, msg);
        }

        public void Dispose()
        {
            _running = false;
            try { _listener?.Stop(); } catch { }
            try { _server?.Close(); } catch { }
            lock (_lock) foreach (var c in _clients.Values) { try { c.Close(); } catch { } }
        }
    }

    /// <summary>UDP broadcast discovery so clients can list games on the LAN.</summary>
    public class LanDiscovery : IDisposable
    {
        public const int UdpPort = 47778;
        const string Magic = "AMONGLAN1";

        public class Entry { public string Ip, Name; public int Players; public DateTime Seen; }

        public readonly Dictionary<string, Entry> Found = new Dictionary<string, Entry>();
        readonly object _lock = new object();
        volatile bool _running = true;
        UdpClient _listen, _send;
        string _hostName = "";
        int _hostPlayers;

        public void StartListening()
        {
            try
            {
                _listen = new UdpClient(AddressFamily.InterNetwork);
                _listen.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _listen.Client.Bind(new IPEndPoint(IPAddress.Any, UdpPort));
            }
            catch { return; }
            new Thread(() =>
            {
                var ep = new IPEndPoint(IPAddress.Any, 0);
                while (_running)
                {
                    try
                    {
                        var data = _listen.Receive(ref ep);
                        var parts = Encoding.UTF8.GetString(data).Split('|');
                        if (parts.Length < 3 || parts[0] != Magic) continue;
                        lock (_lock)
                            Found[ep.Address.ToString()] = new Entry
                            { Ip = ep.Address.ToString(), Name = parts[1], Players = int.Parse(parts[2]), Seen = DateTime.UtcNow };
                    }
                    catch { if (!_running) return; }
                }
            }) { IsBackground = true }.Start();
        }

        public void StartAdvertising(string hostName)
        {
            _hostName = hostName;
            if (_send != null) return;      // already broadcasting; just the name changed
            _send = new UdpClient { EnableBroadcast = true };
            new Thread(() =>
            {
                var targets = new[] { new IPEndPoint(IPAddress.Broadcast, UdpPort), new IPEndPoint(IPAddress.Loopback, UdpPort) };
                while (_running)
                {
                    var bytes = Encoding.UTF8.GetBytes(Magic + "|" + _hostName + "|" + _hostPlayers);
                    if (_advertise) foreach (var t in targets) { try { _send.Send(bytes, bytes.Length, t); } catch { } }
                    Thread.Sleep(1000);
                }
            }) { IsBackground = true }.Start();
        }

        volatile bool _advertise = true;
        public void SetPlayers(int n) { _hostPlayers = n; }
        public void StopAdvertising() { _advertise = false; }
        public void ResumeAdvertising() { _advertise = true; }

        public List<Entry> Snapshot()
        {
            var list = new List<Entry>();
            lock (_lock)
            {
                var stale = new List<string>();
                foreach (var kv in Found)
                {
                    if ((DateTime.UtcNow - kv.Value.Seen).TotalSeconds > 4) stale.Add(kv.Key);
                    else list.Add(kv.Value);
                }
                foreach (var k in stale) Found.Remove(k);
            }
            return list;
        }

        public void Dispose()
        {
            _running = false;
            try { _listen?.Close(); } catch { }
            try { _send?.Close(); } catch { }
        }
    }
}
