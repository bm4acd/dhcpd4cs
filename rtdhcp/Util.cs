using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace dhcp.rtdhcp
{
    class Util
    {
        public static String bytesToString(byte[] ar)
        {
            return bytesToString(ar, "");
        }
        public static String bytesToString(byte[] ar, String delimiter)
        {
            StringBuilder sb = new StringBuilder();
            bool first = true;
            foreach (var b in ar)
            {
                if(first)
                {
                    first = false;
                } else
                {
                    sb.Append(delimiter);
                }
                sb.Append(b.ToString("X2"));
            }
            return sb.ToString();
        }

        public static String bytesToMac(byte[]ar)
        {
            return bytesToString(ar, "-");
        }

        public static void log(Object obj)
        {
            if (obj != null)
            {
                DateTime now = DateTime.Now;
                Console.WriteLine(string.Format("{0} {1}", now.ToString("MM/dd HH:mm:ss.fff"), obj.ToString()));
            }
        }

        public static IPAddress getLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }


        public static bool hasLinkLocalIp(NetworkInterface intf)
        {
            bool hasLinkLocal = false;

            foreach (UnicastIPAddressInformation ip in intf.GetIPProperties().UnicastAddresses)
            {
                if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) //check is IPv4
                {

                    if (ip.Address.ToString().StartsWith("169.254."))
                    {
                        UdpClient client = null;
                        Util.log(string.Format("check {0}: {1}", intf.Name, ip.Address.ToString()));
                        try
                        {
                            IPEndPoint srcPoint = new IPEndPoint(ip.Address, 0);
                            client = new UdpClient(srcPoint);
                            if (((IPEndPoint)client.Client.LocalEndPoint).Port != 0)
                            {
                                hasLinkLocal = true;
                                Util.log(string.Format("add {0}({1}):{2}", intf.Name, intf.Id, ip.Address.ToString()));
                                break;
                            }
                        }
                        catch
                        {
                            Util.log("link-local IP not ready, waiting...");
                        }
                        finally
                        {
                            if (client != null)
                            {
                                client.Close();
                            }
                        }
                    }
                }
            }

            return hasLinkLocal;
        }

        public static double currentUnixTimeMs()
        {
            DateTime dt1970 = new DateTime(1970, 1, 1);
            DateTime now = DateTime.Now;
            TimeSpan span = now - dt1970;
            return span.TotalMilliseconds;
        }
    }
}
