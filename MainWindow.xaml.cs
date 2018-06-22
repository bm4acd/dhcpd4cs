using dhcp.rtdhcp;
using System.Windows;
using System;
using System.Windows.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;
using log4net;

namespace dhcp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IRTDhcpListener
    {
        IPAddress localIp = null;
        ILog log = null;

        public MainWindow()
        {
            InitializeComponent();

            log4net.Config.XmlConfigurator.ConfigureAndWatch(new FileInfo("log4net.xml"));
            log = LogManager.GetLogger("root");

            localIp = getLocalIPAddress();
        }

        IPAddress getLocalIPAddress()
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

        private void bStart_Click(object sender, RoutedEventArgs e)
        {
            startDHCPServer();
            bStart.IsEnabled = false;
            bStop.IsEnabled = true;
        }

        private void bStop_Click(object sender, RoutedEventArgs e)
        {
            stopDHCPServer();
            bStart.IsEnabled = true;
            bStop.IsEnabled = false;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            stopDHCPServer();
        }

        void appendStatus(String status)
        {
            tbStatus.AppendText(status + "\n");
            tbStatus.ScrollToEnd();
        }

        void appendStatusAcrossThread(String status)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action<String>(appendStatus), status);
            }
            else
            {
                appendStatus(status);
            }
        }

        // ----- for RTDhcpSvr -----
        RTDhcpSvr dhcpsvr = null;
        void IRTDhcpListener.notify(DhcpNotify notify)
        {
            if (notify.type == DhcpNotify.NotifyType.CONTROL)
            {
                appendStatusAcrossThread("***** " + notify.message + " *****");
            }
            else
            {
                appendStatusAcrossThread("[" + notify.type.ToString() + "] " + notify.message);
            }
            log.Info(String.Format("[{0}] {1}", notify.type.ToString(), notify.message));
        }

        void startDHCPServer()
        {
            if (dhcpsvr == null || dhcpsvr.isTerminated())
            {
                DhcpParams prms = new DhcpParams();
                prms.ipStart = tIpStart.Text;
                prms.ipEnd = tIpEnd.Text;
                prms.mask = tMask.Text;
                prms.defaultGateway = tGateway.Text;
                prms.dnsSvr = tDNS.Text;
                prms.domainName = tDomainName.Text;
                prms.leaseTime = UInt32.Parse(tLeaseTime.Text);

                dhcpsvr = new RTDhcpSvr(prms, this, localIp);

                dhcpsvr.start();
                log.Info("DHCP server is running.");
            } else
            {
                appendStatus("DHCP server is running already!");
                log.Info("DHCP server is already running.");
            }
        }

        void stopDHCPServer()
        {
            if (dhcpsvr!=null && dhcpsvr.isRunning())
            {
                dhcpsvr.stop();
                dhcpsvr = null;
                log.Info("DHCP server terminated.");
            }
        }

        // ----- for RTDhcpSvr -----
    }
}
