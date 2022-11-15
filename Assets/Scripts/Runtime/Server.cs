using SimParser;
using System;
using System.Threading;
using System.Net;
using System.Collections;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Runtime {
public class Server : MonoBehaviour {
  public int port = 5001;

  private readonly string LOCAL_ADDR = "127.0.0.1"; // GetLocalIPAddress();
  private IEnumerator<RobotState> _states;
  private ConcurrentQueue<RobotState> _buffer;
  private RobotControl _control;

  // Start is called before the first frame update
  void Start() {
    _control = GetComponent<RobotControl>();
    _buffer = new ConcurrentQueue<RobotState>();

    Thread thread = new(AcceptRobotStateData);
    thread.Start();
  }

  // Update is called once per frame
  void FixedUpdate() {
    // if there is data, read it
    if (_buffer.TryDequeue(out RobotState data)) {
      // print(data);
      _control.SetState(data);
    }
  }

  // Runs a server, connects a client, gets data from the client,
  // writes to the _buffer (global ArrayList)
  private void AcceptRobotStateData() {
    TcpListener server = new(IPAddress.Parse(LOCAL_ADDR), port);
    try {
      server.Start();
      // int nextByte;
      print("Server has started on " + LOCAL_ADDR + ":" + port);

      while (true) {
        print("Waiting for a client connection");
        TcpClient currentClient = server.AcceptTcpClient();
        NetworkStream currentStream = currentClient.GetStream();
        print("A client is connected.");

        IEnumerator<RobotState> states =
            new SimulationParser(_control.jointCount, currentStream)
                .GetEnumerator();
        bool hasRead = false;

        // Keep the connection alive until a state is read.
        while (!hasRead) {
          hasRead = states.MoveNext();
        }

        // TODO: Need a way to exit out of this loop when states are exhausted.
        //       Issue is that at the moment, the Scanner blocks if no
        //       characters remain, even if
        do {
          _buffer.Enqueue(states.Current);
        } while (states.MoveNext());

        states.Dispose();
        currentClient.Close();
        print("A connection is closed.");
      }
    } catch (SocketException e) {
      print("SocketException: " + e);
    } finally {
      server.Stop();
    }
  }

  private static string GetLocalIPAddress() {
    var host = Dns.GetHostEntry(Dns.GetHostName());
    foreach (var ip in host.AddressList) {
      if (ip.AddressFamily == AddressFamily.InterNetwork) {
        return ip.ToString();
      }
    }

    throw new Exception(
        "No network adapters with an IPv4 address in the system!");
  }
}
}
