using System;
using System.Net;
using System.Net.Sockets;

namespace Runtime {
internal static class Utils {
  /// <summary>
  /// Get the local IP of the current machine in the network.
  /// </summary>
  /// <returns>Local IP address of current machine.</returns>
  /// <exception cref="Exception">If the machine does not have an internal IP
  /// address.</exception>
  public static string GetLocalIPAddress() {
    IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
    foreach (IPAddress ip in host.AddressList) {
      if (ip.AddressFamily == AddressFamily.InterNetwork) {
        return ip.ToString();
      }
    }

    throw new Exception(
        "No network adapters with an IPv4 address in the system!");
  }
}
}
