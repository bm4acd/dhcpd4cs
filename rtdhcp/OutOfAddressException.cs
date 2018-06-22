using System;

namespace dhcp.rtdhcp
{
    class OutOfAddressException : Exception
    {
        public OutOfAddressException(): base() { }
        public OutOfAddressException(string message) : base(message)
        {
        }
    }
}
