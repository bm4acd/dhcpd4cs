using System;

namespace dhcp.rtdhcp
{
    /// <summary>
    /// The value object to contain parameters to initialize the DHCP server.
    /// </summary>
    class DhcpParams
    {
        public String ipStart { get; set; } = "192.168.1.1";
        public String ipEnd { get; set; } = "192.168.1.254";
        public String domainName { get; set; } = null;
        public String mask { get; set; } = "255.255.255.0";
        public String dnsSvr { get; set; } = "1.1.1.1";
        public UInt32 leaseTime { get; set; } = 60 * 60 * 24;
        public String defaultGateway { get; set; } = "192.168.1.254";

    }
}
