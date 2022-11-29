using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

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

  /// <summary>
  /// Run something, and if it doesn't complete within a certain time frame,
  /// throws an exception.
  /// </summary>
  /// <param name="action">The thing to run and get the result of.</param>
  /// <param name="timeLimit">How long to wait before timing out.</param>
  /// <typeparam name="T">The type of the result.</typeparam>
  /// <returns>The result if the action succeeds and returns without an
  /// exception.</returns> <exception cref="Exception">The generic exception if
  /// the action throws.</exception> <exception cref="TimeoutException">Thrown
  /// if the task takes too long.</exception>
  public static T RunTaskWithTimeout<T>(Func<T> action, TimeSpan timeLimit) {
    // Attempt to run the task, recording the result and catching any
    // exceptions.
    T result = default;
    Exception caught = null;

            Thread worker = new(() =>
            {
                try
                {
                    result = action();
  }
  catch (Exception e) {
    caught = e;
  }
});

// If the join was successful and no exception was caught, return the result.
if (worker.Join(timeLimit) && caught == null) {
  return result;
}

// If an exception was caught, rethrow it.
if (caught != null) {
  throw caught;
}

// Otherwise, the task timed out, and throw a TimeoutException.
throw new TimeoutException(
    "Task did not complete within the given time limit.");
}
}
}
