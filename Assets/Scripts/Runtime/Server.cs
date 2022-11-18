using SimParser;
using System;
using System.Threading;
using System.Net;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Collections.Generic;
using UnityEngine;

namespace Runtime {
public class Server : MonoBehaviour {
  public int port;
  public GameObject errorPopup;
  public ErrorController errorController;

  private readonly string LOCAL_ADDR = GetLocalIPAddress();
  private IEnumerator<RobotState> _states;
  private ConcurrentQueue<RobotState> _buffer;
  private ConcurrentQueue<Exception> _threadException;
  private RobotControl _control;

  // Start is called before the first frame update
  void Start() {
    _control = GetComponent<RobotControl>();
    _buffer = new ConcurrentQueue<RobotState>();
    _threadException = new ConcurrentQueue<Exception>();

    Thread thread = new(AcceptRobotStateData);
    thread.Start();
  }

  // Update is called once per frame
  void FixedUpdate() {
    if (_threadException.TryDequeue(out Exception exc)) {
      var ec = errorController.textField;
      ec.text = exc.Message;
      errorPopup.SetActive(true);
    }

    // if there is data, read it
    if (!_buffer.TryDequeue(out RobotState data))
      return;

    // Double-check that the data is valid, in case any joint positions are out
    // of range.
    switch (data.IsProbablyValid()) {
    case Left<FormatException, RobotState> l:
      _buffer.Clear();
      throw l.FromLeft();
    case Right<FormatException, RobotState> r:
      _control.SetState(r.FromRight());
      break;
    default:
      throw new InvalidCastException("Not an Either.");
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
        print("[+] A client is connected to the movement server.");

        IEnumerator<RobotState> states = null;
        try {
          states = new SimulationParser(_control.jointCount, currentStream)
                       .GetEnumerator();
          bool hasRead = false;

          // Keep the connection alive until a state is read.
          while (!hasRead) {
            hasRead = states.MoveNext();
            if (hasRead) {
              RobotState st = states.Current;
              st.IsFirst = true;
            }
          }

          // XXX: See scanner for description of hack.
          do {
            _buffer.Enqueue(states.Current);
          } while (states.MoveNext());
        } catch (Exception e) {
          _threadException.Enqueue(e);
        }

        states?.Dispose();
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
