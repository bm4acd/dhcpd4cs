namespace dhcp.rtdhcp
{
    /// <summary>
    /// This defines the interface of the listener of RTDhcpSvr.
    /// </summary>
    interface IRTDhcpListener
    {
        /// <summary>
        /// notify the listener from RTDhcpSvr
        /// </summary>
        /// <param name="notify"></param>
        void notify(DhcpNotify notify);
    }
}
