using dhcp.rtdhcp;
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;

namespace DotNetProjects.DhcpServer
{
    /// <summary>
    /// DHCP Server
    /// </summary>
    public class DHCPServer : IDisposable
    {
        /// <summary>Delegate for DHCP message</summary>
        public delegate void DHCPDataReceivedEventHandler(DHCPRequest dhcpRequest);

        /// <summary>Will be called on any DHCP message</summary>
        public event DHCPDataReceivedEventHandler OnDataReceived = delegate { };
        /// <summary>Will be called on any DISCOVER message</summary>
        public event DHCPDataReceivedEventHandler OnDiscover = delegate { };
        /// <summary>Will be called on any REQUEST message</summary>
        public event DHCPDataReceivedEventHandler OnRequest = delegate { };
        /// <summary>Will be called on any DECLINE message</summary>
        public event DHCPDataReceivedEventHandler OnDecline = delegate { };
        /// <summary>Will be called on any DECLINE released</summary>
        public event DHCPDataReceivedEventHandler OnReleased = delegate { };
        /// <summary>Will be called on any DECLINE inform</summary>
        public event DHCPDataReceivedEventHandler OnInform = delegate { };

        /// <summary>Server name (optional)</summary>
        public string ServerName { get; set; }

        //private Socket socket = null;
        private UdpClient udp = null;
        private Thread receiveDataThread = null;
        private const int PORT_TO_LISTEN_TO = 67;
        private IPAddress _bindIp;

        public event Action<Exception> UnhandledException;

        public IPAddress BroadcastAddress { get; set; }

		public NetworkInterface SendDhcpAnswerNetworkInterface { get; set; }

		/// <summary>
		/// Creates DHCP server, it will be started instantly
		/// </summary>
		/// <param name="bindIp">IP address to bind</param>
		public DHCPServer(IPAddress bindIp)
        {
            _bindIp = bindIp;
        }

        /// <summary>Creates DHCP server, it will be started instantly</summary>
        public DHCPServer() : this(IPAddress.Any)
        {
            BroadcastAddress = IPAddress.Broadcast;
        }

        public void Start()
        {
            try
            {
                var ipLocalEndPoint = new IPEndPoint(_bindIp, PORT_TO_LISTEN_TO);
                udp = new UdpClient(ipLocalEndPoint);
                udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                receiveDataThread = new Thread(ReceiveDataThread);
                receiveDataThread.Start();
            } catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
        }

        /// <summary>Disposes DHCP server</summary>
        public void Dispose()
        {
            /*
            if (socket != null)
            {
                socket.Close();
                socket = null;
            }
            */
            if (udp != null)
            {
                udp.Close();
                udp = null;
            }

            if (receiveDataThread != null)
            {
                receiveDataThread.Abort();
                receiveDataThread = null;
            }
        }

        private void ReceiveDataThread()
        {
            while (true)
            {
                try
                {
                    /*
                    IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                    EndPoint remote = (EndPoint)(sender);
                    var buffer = new byte[1024];
                    int len = socket.ReceiveFrom(buffer, ref remote);

                    if (len > 0)
                    {
                        Array.Resize(ref buffer, len);
                        var dataReceivedThread = new Thread(DataReceived);
                        dataReceivedThread.Start(buffer);
                    }
                    */
                    IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                    byte[] buffer = udp.Receive(ref remote);
                    var dataReceivedThread = new Thread(DataReceived);
                    dataReceivedThread.Start(buffer);
                }
                catch (Exception ex)
                {
                    if (UnhandledException != null)
                        UnhandledException(ex);
                }
            }
        }

        private void DataReceived(object o)
        {
            var data = (byte[])o;
            try
            {
                //var dhcpRequest = new DHCPRequest(data, socket, this);
                //ccDHCP = new clsDHCP();
                DHCPRequest dhcpRequest = new DHCPRequest(data, udp, this);


                //data is now in the structure
                //get the msg type
                OnDataReceived(dhcpRequest);
                var msgType = dhcpRequest.GetMsgType();
                Util.log(String.Format("Message type: {0}", msgType.ToString()));
                switch (msgType)
                {
                    case DHCPMsgType.DHCPDISCOVER:
                        OnDiscover(dhcpRequest);
                        break;
                    case DHCPMsgType.DHCPREQUEST:
                        OnRequest(dhcpRequest);
                        break;
                    case DHCPMsgType.DHCPDECLINE:
                        OnDecline(dhcpRequest);
                        break;
                    case DHCPMsgType.DHCPRELEASE:
                        OnReleased(dhcpRequest);
                        break;
                    case DHCPMsgType.DHCPINFORM:
                        OnInform(dhcpRequest);
                        break;
                    //default:
                    //    Console.WriteLine("Unknown DHCP message: " + (int)MsgTyp + " (" + MsgTyp.ToString() + ")");
                    //    break;
                }
            }
            catch (Exception ex)
            {
                Util.log(ex.Message);
                Util.log(ex.StackTrace);
                if (UnhandledException != null)
                    UnhandledException(ex);                    
            }
        }
    }
}