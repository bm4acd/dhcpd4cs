using System;
using System.Net;

namespace dhcp.rtdhcp
{
    class IpPoolItem
    {
        public long tick { get; set; } //unit: ms
        public IPAddress ip { get; }
        public String mac { get; set; }

        public Status status = Status.NULL;
        public enum Status { NULL, OFFERED, ACKED };

        public IpPoolItem(IPAddress ip) : this(null, ip) { }

        public IpPoolItem(String mac, IPAddress ip) : this(mac, ip, 0) { this.tick = (long) Util.currentUnixTimeMs(); }
        public IpPoolItem(String mac, IPAddress ip, long tick)
        {
            this.mac = mac;
            this.ip = ip;
            this.tick = tick;
            //tick = new DateTime().Ticks;
        }

        public void clearTick()
        {
            tick = 0;
        }
        public void setTick()
        {
            tick = (long) Util.currentUnixTimeMs();
        }

        public bool expired(double leaseTime)
        {
            return (Util.currentUnixTimeMs() - tick) > (status == Status.ACKED ? leaseTime : 60000);
        }

        public override string ToString()
        {
            return String.Format("{0};{1};{2}", mac, ip.ToString(), tick);
        }

        public static IpPoolItem valueOf(String s)
        {
            String[] ss = s.Split(';');
            IpPoolItem item = new IpPoolItem(ss[0], IPAddress.Parse(ss[1]), long.Parse(ss[2]));
            return item;
        }
    }
}
