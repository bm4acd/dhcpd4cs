using System;
using System.Collections.Generic;
using System.Net;

namespace DotNetProjects.DhcpServer
{
    /// <summary>Reply options</summary>
    public class DHCPReplyOptions
    {
        /// <summary>IP address</summary>
        public IPAddress SubnetMask = null;
        /// <summary>Next Server IP address (bootp)</summary>
        public IPAddress ServerIpAddress = null;
        /// <summary>IP address lease time (seconds)</summary>
        public UInt32 IPAddressLeaseTime = 60 * 60 * 24;
        /// <summary>Renewal time (seconds)</summary>
        public UInt32? RenewalTimeValue_T1 = 60 * 60 * 24;
        /// <summary>Rebinding time (seconds)</summary>
        public UInt32? RebindingTimeValue_T2 = 60 * 60 * 24;
        /// <summary>Domain name</summary>
        public string DomainName = null;
        /// <summary>IP address of DHCP server</summary>
        public IPAddress ServerIdentifier = null;
        /// <summary>Router (gateway) IP</summary>
        public IPAddress RouterIP = null;
        /// <summary>Domain name servers (DNS)</summary>
        public IPAddress[] DomainNameServers = null;
        /// <summary>Log server IP</summary>
        public IPAddress LogServerIP = null;
        /// <summary>Static routes</summary>
        public NetworkRoute[] StaticRoutes = null;
        /// <summary>Other options which will be sent on request</summary>
        public Dictionary<DHCPOption, byte[]> OtherRequestedOptions = new Dictionary<DHCPOption, byte[]>();

        public DHCPReplyOptions clone()
        {
            DHCPReplyOptions opt = new DHCPReplyOptions();
            opt.SubnetMask = SubnetMask == null ? null : new IPAddress(SubnetMask.GetAddressBytes());
            opt.ServerIpAddress = ServerIpAddress == null ? null : new IPAddress(ServerIpAddress.GetAddressBytes());
            opt.IPAddressLeaseTime = IPAddressLeaseTime;
            opt.RenewalTimeValue_T1 = RenewalTimeValue_T1;
            opt.RebindingTimeValue_T2 = RebindingTimeValue_T2;
            opt.DomainName = DomainName == null ? null : String.Copy(DomainName);
            opt.ServerIdentifier = ServerIdentifier == null ? null : new IPAddress(ServerIdentifier.GetAddressBytes());
            opt.RouterIP = RouterIP == null ? null : new IPAddress(RouterIP.GetAddressBytes());
            IPAddress[] dnss = DomainNameServers == null ? null : new IPAddress[DomainNameServers.Length];
            if (DomainNameServers == null)
            {
                opt.DomainNameServers = null;
            }
            else
            {
                for (int i = 0; i < DomainNameServers.Length; i++)
                {
                    dnss[i] = DomainNameServers[i] == null ? null : new IPAddress(DomainNameServers[i].GetAddressBytes());
                }
                opt.DomainNameServers = dnss;
            }
            opt.LogServerIP = LogServerIP == null ? null : new IPAddress(LogServerIP.GetAddressBytes());
            opt.StaticRoutes = StaticRoutes; //TODO: deep clone
            opt.OtherRequestedOptions = OtherRequestedOptions; //TODO: deep clone

            return opt;
        }
    }
}