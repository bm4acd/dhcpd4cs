/*
 * This project depends on dotnetprojects/sharp-dhcp-server-lib in github.
 * https://github.com/dotnetprojects/sharp-dhcp-server-lib
 */

using DotNetProjects.DhcpServer;
using System;
using System.Net;
using System.Net.NetworkInformation;

namespace dhcp.rtdhcp
{
    class RTDhcpSvr
    {
        private static int STATE_READY = 0;
        private static int STATE_RUNNING = 1;
        private static int STATE_TERMINATED = -1;
        int state = STATE_READY;

        //NetworkInterface intf = null;

        static DHCPServer dhcpsvr = null;
        static DHCPReplyOptions defaultOptions = null;
        static IpPool pool = null;

        static IRTDhcpListener listener = null;

        static IPAddress localAddress = null;

        /// <summary>Ruby DHCP server. The server will bind to 0.0.0.0. Suggest providing local IP address if there are multiple network interface on your machine.</summary>
        /// <param name="prms">initialization parameters</param>
        /// <param name="listener">event listener</param>
        public RTDhcpSvr(DhcpParams prms, IRTDhcpListener listener) : this(prms, listener, null) { }
        /// <summary>Ruby DHCP server</summary>
        /// <param name="prms">initialization parameters</param>
        /// <param name="listener">event listener</param>
        /// <param name="localIp">the local IP address to bind to and will be the server identifier</param>
        public RTDhcpSvr(DhcpParams prms, IRTDhcpListener listener, IPAddress localIp)
        {
            /*
            intf = findNetworkInterface();
            if (intf!=null)
            {
                Util.log(string.Format("network interface: {0}({1})", intf.Name, intf.Id));
            }*/
            
            localAddress = localIp == null ? Util.getLocalIPAddress() : localIp;

            defaultOptions = new DHCPReplyOptions();
            defaultOptions.DomainName = prms.domainName;
            defaultOptions.DomainNameServers = new IPAddress[] { IPAddress.Parse(prms.dnsSvr) };
            defaultOptions.IPAddressLeaseTime = prms.leaseTime;
            defaultOptions.RouterIP = IPAddress.Parse(prms.defaultGateway);
            defaultOptions.SubnetMask = IPAddress.Parse(prms.mask);
            if (localAddress != null)
            {
                defaultOptions.ServerIdentifier = localAddress;
            }

            pool = new IpPool(prms.ipStart, prms.ipEnd, prms.leaseTime);

            RTDhcpSvr.listener = listener;

            state = STATE_READY;
        }

        public RTDhcpSvr(DhcpParams prms): this(prms, null) { }
        /*
        private NetworkInterface findNetworkInterface()
        {
            NetworkInterface[] intfs = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface intf in intfs)
            {
                //Util.log(string.Format("===== {0}:{1} =====", intf.Name, intf.NetworkInterfaceType));

                //bypass some interfaces
                if (intf.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                {
                    Util.log(string.Format("bypass loopback interface: {0}({1})", intf.Name, intf.Id));
                    continue;
                }
                else if (intf.Name.StartsWith("VMware Network Adapter"))
                {
                    Util.log(string.Format("bypass VMWare virtual interface: {0}({1})", intf.Name, intf.Id));
                    continue;
                }
                else if (intf.OperationalStatus.Equals(OperationalStatus.Down))
                {
                    Util.log(string.Format("bypass link down interface: {0}({1})", intf.Name, intf.Id));
                    continue;
                } else if (Util.hasLinkLocalIp(intf))
                {
                    Util.log(string.Format("bypass linklocal interface: {0}({1})", intf.Name, intf.Id));
                    continue;
                }

                return intf;                
            }
            return null;
        }   */     

        /// <summary>Start the DHCP server.</summary>
        public void start()
        {
            if (localAddress == null)
            {
                dhcpsvr = new DHCPServer();
                sendNotify(DhcpNotify.NotifyType.CONTROL, "RTDhcpServer binds to 0.0.0.0");
            }
            else
            {
                dhcpsvr = new DHCPServer(localAddress);
                sendNotify(DhcpNotify.NotifyType.CONTROL, String.Format("RTDhcpServer binds to {0}", localAddress.ToString()));
            }
            dhcpsvr.OnDiscover += discoverHandler;
            dhcpsvr.OnRequest += requestHandler;
            dhcpsvr.OnReleased += releaseHandler;
            dhcpsvr.OnDecline += declineHandler;
            dhcpsvr.OnInform += informHandler;
            //dhcpsvr.SendDhcpAnswerNetworkInterface = intf;
            dhcpsvr.BroadcastAddress = IPAddress.Broadcast;

            dhcpsvr.Start();

            state = STATE_RUNNING;

            sendNotify(DhcpNotify.NotifyType.CONTROL, "RTDhcpServer is running.");
        }

        static void discoverHandler(DHCPRequest dhcpRequest)
        {
            DHCPMsgType type = dhcpRequest.GetMsgType();
            String mac = Util.bytesToMac(dhcpRequest.GetChaddr());
            IPAddress ip = pool.offerIP(mac, dhcpRequest.GetRequestedIP());
            DHCPReplyOptions options = defaultOptions.clone();
            options.ServerIdentifier = localAddress;
            dhcpRequest.SendDHCPReply(DHCPMsgType.DHCPOFFER, ip, defaultOptions);
            sendNotify(DhcpNotify.NotifyType.DISCOVER, String.Format("RTDhcpServer receives DHCP discover message from {0}, offer {1}", mac, ip));
        }
        static void requestHandler(DHCPRequest dhcpRequest)
        {
            String mac = Util.bytesToMac(dhcpRequest.GetChaddr());
            IPAddress si = new IPAddress(dhcpRequest.GetOptionData(DHCPOption.ServerIdentifier));
            if (si != null && si.Equals(localAddress)) //client select this server
            {
                IPAddress requestIp = new IPAddress(dhcpRequest.GetOptionData(DHCPOption.RequestedIPAddress));

                IPAddress ip = pool.ackIP(mac);
                if (ip != null)
                {
                    pool.storeDb();
                    dhcpRequest.SendDHCPReply(DHCPMsgType.DHCPACK, ip, defaultOptions);
                    sendNotify(DhcpNotify.NotifyType.REQUEST, String.Format("RTDhcpServer receives DHCP request message from {0}, ack {1}", mac, ip));
                }
                /*
                IPAddress ip = pool.getIP(mac);
                if (requestIp!=null && ip.Equals(requestIp))
                {
                    pool.storeDb();
                    dhcpRequest.SendDHCPReply(DHCPMsgType.DHCPACK, ip, options);
                    sendNotify(DhcpNotify.NotifyType.REQUEST, String.Format("RTDhcpServer receives DHCP request message from {0}, ack {1}", mac, ip));
                } else
                {
                    dhcpRequest.SendDHCPReply(DHCPMsgType.DHCPNAK, requestIp, options);
                    pool.release(mac);
                    sendNotify(DhcpNotify.NotifyType.REQUEST, String.Format("RTDhcpServer receives DHCP request message from {0}, nack {1}", mac, requestIp));
                }
                */
            } else 
            {
                IpPoolItem item = pool.getPoolItem(mac);
                if (item != null && item.status == IpPoolItem.Status.ACKED) //reuse previous allocated address
                {
                    IPAddress ip = item.ip;
                    pool.storeDb();
                    dhcpRequest.SendDHCPReply(DHCPMsgType.DHCPACK, ip, defaultOptions);
                    sendNotify(DhcpNotify.NotifyType.REQUEST, String.Format("RTDhcpServer receives DHCP request(reuse) message from {0}, ack {1}", mac, ip));

                }
                else //not selected by client
                {
                    pool.release(mac);
                    sendNotify(DhcpNotify.NotifyType.REQUEST, String.Format("RTDhcpServer receives DHCP request message from {0}, not selected by this client", mac));
                }
            }
        }

        static void releaseHandler(DHCPRequest dhcpRequest)
        {
            String mac = Util.bytesToMac(dhcpRequest.GetChaddr());
            pool.release(mac);
            pool.storeDb();
            sendNotify(DhcpNotify.NotifyType.DISCOVER, String.Format("RTDhcpServer receives DHCP release message from {0}", mac));
        }

        static void declineHandler(DHCPRequest dhcpRequest)
        {
            String mac = Util.bytesToMac(dhcpRequest.GetChaddr());
            pool.release(mac);
            pool.storeDb();
            sendNotify(DhcpNotify.NotifyType.DECLINE, String.Format("RTDhcpServer receives DHCP decline from {0}", mac));
        }
        static void informHandler(DHCPRequest dhcpRequest)
        {
            String mac = Util.bytesToMac(dhcpRequest.GetChaddr());
            //TODO: implement this method
            sendNotify(DhcpNotify.NotifyType.INFORM, String.Format("RTDhcpServer receives DHCP inform message from {0}", mac));
        }

        static void sendNotify(DhcpNotify.NotifyType type, String message)
        {
            if (listener != null)
            {
                DhcpNotify o = new DhcpNotify(type, message);
                listener.notify(o);
            }
        }

        /// <summary>Stop the DHCP server.</summary>
        public void stop()
        {
            dhcpsvr.Dispose();
            state = STATE_TERMINATED;
            pool.stop();

            sendNotify(DhcpNotify.NotifyType.CONTROL, "RTDhcpServer is terminated.");
        }

        /// <summary>Check is the DHCP server running</summary>
        /// <returns>true if the server running</returns>
        public bool isRunning()
        {
            return state == STATE_RUNNING;
        }

        /// <summary>Check is the DHCP server terminated</summary>
        /// <returns>true if the server be terminated</returns>
        public bool isTerminated()
        {
            return state == STATE_TERMINATED;
        }
    }
}
