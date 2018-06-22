using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;

namespace dhcp.rtdhcp
{
    /// <summary>
    /// The pool of IP address which DHCP server can utilize.
    /// </summary>
    class IpPool
    {
        Dictionary<String, IpPoolItem> lease = new Dictionary<String, IpPoolItem>(); //mac, IpPoolItem
        private Queue<IpPoolItem> released = new Queue<IpPoolItem>();
        private static int CHECK_LEASE_TIME_PERIOD = 15000;
        IPAddress ipS, ipE;
        byte[] nextIp = new byte[4];
        byte[] endIp = new byte[4];
        double leaseTime = 86400 * 1000; //unit: ms
        bool running { get; set; } = false;

        public IpPool(String start, String end, double leaseTime)
        {
            ipS = IPAddress.Parse(start);
            ipE = IPAddress.Parse(end);
            nextIp = ipS.GetAddressBytes();
            endIp = ipE.GetAddressBytes();
            this.leaseTime = leaseTime * 1000L;

            retriveDb();

            Thread leaseTimeThread = new Thread(checkLeaseTimeRunner);
            leaseTimeThread.Start();

            running = true;
        }

        public IpPoolItem getPoolItem(String mac)
        {
            IpPoolItem item = null;
            if (!lease.TryGetValue(mac, out item))
            {
                return null;
            }
            return item;
        }

        public IPAddress offerIP(String mac) {
            return offerIP(mac, null);
        }
        public IPAddress offerIP(String mac, IPAddress preferIp)
        {
            IpPoolItem item = null;
            if (!lease.TryGetValue(mac, out item)) //already allocate address for this mac
            {
                if (preferIp != null)
                {
                    if (!containsIp(preferIp))
                    {
                        item = new IpPoolItem(preferIp);
                    }
                }
                if (item==null && released.Count > 0) //is there released address to be reused
                {
                    lock (released)
                    {
                        item = released.Dequeue();
                    }
                }
                if (item == null) //find an available address
                {
                    IPAddress ip = null;
                    ip = getAvailableAddress();
                    item = new IpPoolItem(ip);
                }
                
                item.mac = mac;
                item.setTick();
                item.status = IpPoolItem.Status.OFFERED;
                lease[mac] = item;
                //storeDb();
            }

            return item.ip;
        }

        public IPAddress ackIP(String mac)
        {
            IpPoolItem item = null;
            if (!lease.TryGetValue(mac, out item)) //already allocate address for this mac
            {
                return null;
            }
            item.status = IpPoolItem.Status.ACKED;

            return item.ip;
        }

        private static byte IP_OCTET_MIN = 1;
        private static byte IP_OCTET_MAX = 254;
        private IPAddress getAvailableAddress()
        {
            while (true)
            {
                //check next IP larger than end IP
                for (int i=0; i<4; i++)
                {
                    if (nextIp[i]>endIp[i])
                    {
                        throw new OutOfAddressException("next IP larger than end IP");
                    }
                }

                IPAddress address = new IPAddress(nextIp);
                bool inUse = containsIp(address) || addressInUse(address);

                if (inUse) // calculate next available address
                {
                    nextIp[3]++;
                    for (int i=3; i>0; i--)
                    {
                        if (nextIp[i]> IP_OCTET_MAX)
                        {
                            nextIp[i] = IP_OCTET_MIN;
                            nextIp[i - 1]++;
                        }
                    }
                    continue;
                }

                return address;                
            }
        }

        private static Int32 PINT_TIMEOUT = 1500;
        private bool addressInUse(IPAddress address)
        {
            Ping ping = new Ping();
            PingReply reply = ping.Send(address, PINT_TIMEOUT);
            return (reply.Status == IPStatus.Success);
        }
        public bool containsIp(IPAddress ip)
        {
            foreach (KeyValuePair<String, IpPoolItem> item in lease)
            {
                if (item.Value.ip.Equals(ip))
                    return true;
            }
            return false;
        }
        public bool containsIp(String ip)
        {
            return containsIp(IPAddress.Parse(ip));
        }

        public void release(String mac)
        {
            IpPoolItem item = null;
            if (lease.TryGetValue(mac, out item))
            {
                lease.Remove(mac);
                item.clearTick();
                item.mac = null;
                item.status = IpPoolItem.Status.NULL;
                released.Enqueue(item);
                //storeDb();
            }
        }

        public void checkLeaseTimeRunner()
        {
            LinkedList<String> toRelease = new LinkedList<string>();
            while (running)
            {
                Thread.Sleep(CHECK_LEASE_TIME_PERIOD);
                toRelease.Clear();
                foreach (KeyValuePair<String, IpPoolItem> item in lease)
                {
                    if (item.Value.expired(leaseTime))
                    {
                        toRelease.AddLast(item.Key);
                    }
                }
                Util.log("*** check lease time, expired: " + toRelease.Count);

                if (toRelease.Count > 0)
                {
                    lock (lease)
                    {
                        foreach (String mac in toRelease)
                        {
                            release(mac);
                        }
                    }
                }
            }
        }

        public void stop()
        {
            //save current status
            storeDb();

            running = false;
        }

        private static String DB_FILE = @"db\dhcp.db";
        private static String DB_PATH = @"db";
        private void retriveDb()
        {
            if (!Directory.Exists(DB_PATH))
            {
                Directory.CreateDirectory(DB_PATH);
            }
            if (File.Exists(DB_FILE))
            {
                String[] lines = File.ReadAllLines(DB_FILE);
                foreach (String line in lines)
                {
                    try
                    {
                        IpPoolItem item = IpPoolItem.valueOf(line);
                        item.status = IpPoolItem.Status.ACKED;
                        lease.Add(item.mac, item);
                    }
                    catch
                    {
                        Util.log("retrieve dhcp data fail: " + line);
                    }
                }
                Util.log("retrieve dhcp data, count " + lease.Count);
            }
            else
            {
                Util.log("no dhcp data file");
                File.WriteAllText(DB_FILE, "");
            }
        }

        public void storeDb()
        {
            int count = lease.Count;
            String[] lines = new String[count];
            int i = 0;
            foreach (KeyValuePair<String, IpPoolItem> item in lease)
            {
                if (item.Value.status == IpPoolItem.Status.ACKED)
                {
                    lines[i] = item.Value.ToString();
                    i++;
                }
            }
            try
            {
                File.WriteAllLines(DB_FILE, lines);
            } catch(Exception ex)
            {
                Util.log("store dhcp data fail:");
                Util.log(ex.StackTrace);
            }
        }
    }

}
