using System.IO;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;

namespace TheGrid
{
    public class Connection
    {
        public EndPoint EndPoint { get; }
        public TcpClient Client { get; set; }
        public StreamWriter Writer { get; set; }
        public StreamReader Reader { get; set; }

        public Connection(TcpClient client) {
            EndPoint = client.Client.RemoteEndPoint;
            Writer = new StreamWriter(client.GetStream());
            Reader = new StreamReader(client.GetStream());
        }

        public void Write(object obj) {
            var s = JsonConvert.SerializeObject(obj);
            Writer.WriteLine(s);
            Writer.Flush();
        }

        public TOut Read<TOut>() {
            var r = Reader.ReadLine();
            return JsonConvert.DeserializeObject<TOut>(r);
        }
    }
}
