using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace VMoiChat
{
    public class ConnectedClient
    {
        public TcpClient TcpClient { get; set; }
        public string Username { get; set; }
    }
}
