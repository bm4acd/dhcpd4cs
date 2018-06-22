using System;

namespace dhcp.rtdhcp
{
    /// <summary>
    /// The notification which to notify the listener.
    /// </summary>
    class DhcpNotify
    {
        public NotifyType type { get; set; }
        public String message { get; set; }

        /// <summary>
        /// Constructs a DhcpNotify by setting the type and message.
        /// </summary>
        /// <param name="type">type of the notification</param>
        /// <param name="message">message body of this notification</param>
        public DhcpNotify(NotifyType type, String message)
        {
            this.type = type;
            this.message = message;
        }

        /// <summary>
        /// Type of this notification.
        /// </summary>
        public enum NotifyType
        {
            CONTROL, DISCOVER, REQUEST, RELEASE, DECLINE, INFORM
        }
    }
}
