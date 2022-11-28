using SimParser;
using System;
using System.Threading;
using System.Net;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Runtime {
public class Server : MonoBehaviour {
  public int port;

  public static event Action<string> TriggerPopupWindow;

  private readonly string _localAddr = Utils.GetLocalIPAddress();
  private IEnumerator<RobotState> _states;
  private ConcurrentQueue<RobotState> _buffer;
  private ConcurrentQueue<Exception> _threadException;
  private RobotControl _control;
  private TcpListener _server;
  private IPAddress _responder;

  // Start is called before the first frame update
  public void Start() {
    print("[+] Server start called");
    _control = GetComponent<RobotControl>();
    _buffer = new ConcurrentQueue<RobotState>();
    _threadException = new ConcurrentQueue<Exception>();

    Thread thread = new(ServerLoop);
    thread.Start();
  }

  // Update is called once per frame
  void FixedUpdate() {
    if (OVRInput.GetUp(OVRInput.Button.One)) {
      Respond();
    }

    if (_threadException.TryDequeue(out Exception exc)) {
      TriggerPopupWindow?.Invoke("Error: " + exc.Message);
    }

    // if there is data, read it
    if (!_buffer.TryDequeue(out RobotState data)) {
      _control.Grabbable = true;
      return;
    }

    // Make robot not grabbable.
    _control.Grabbable = false;

    // Double-check that the data is valid, in case any joint positions are out
    // of range.
    switch (data.IsProbablyValid()) {
    case Left<FormatException, RobotState> l:
      _buffer.Clear();
      _threadException.Enqueue(l.FromLeft());
      break;
    case Right<FormatException, RobotState> r:
      _control.SetState(r.FromRight());
      break;
    default:
      throw new InvalidCastException("Not an Either.");
    }
  }

  private bool ReadIPAddress(TcpListener server) {
    TcpClient currentClient = server.AcceptTcpClient();
    NetworkStream currentStream = currentClient.GetStream();
    print("[+] A client is sending its IP to the server.");

    try {
      _responder = null;

      byte[] buf = new byte[512];
      int read;
      if ((read = currentStream.Read(buf, 0, 512)) > 0) {
        string maybeIp = Encoding.UTF8.GetString(buf, 0, read);
        if (IPAddress.TryParse(maybeIp, out IPAddress addr)) {
          print("[+] IP read: " + maybeIp);
          _responder = addr;

          return true;
        }

        print("[+] Invalid IP: " + maybeIp);
        return false;
      } else {
        print("[+] Read unsuccessful. Disconnecting.");
        return false;
      }
    } catch (Exception e) {
      _threadException.Enqueue(e);
      return false;
    } finally {
      currentStream.Close();
      currentClient.Close();
    }
  }

  private void ReceiveStates(TcpListener server) {
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
        if (!hasRead)
          continue;

        RobotState st = states.Current;
        st.IsFirst = true;
      }

      // XXX: See scanner for description of hack.
      do
        _buffer.Enqueue(states.Current);
      while (states.MoveNext());
    } catch (Exception e) {
      _threadException.Enqueue(e);
    }

    states?.Dispose();
    currentClient.Close();
    print("[+] A movement client connection is closed.");
  }

  // Runs a server, connects a client, gets data from the client,
  // writes to the _buffer (global ArrayList)
  private void ServerLoop() {
    _server = new TcpListener(IPAddress.Parse(_localAddr), port);
    try {
      _server.Start();
      print("[+] Movement Server has started on " + _localAddr + ":" + port);

      while (true) {
        print("Waiting for a client connection");
        // The first connection that receives an IP address.
        do {
          print("[+] Waiting for IP.");
        } while (!ReadIPAddress(_server));
        TriggerPopupWindow?.Invoke(
            "Client IP found. Please press 'A' to start streaming simulated movement data.");

        // The second connection that receives the states.
        ReceiveStates(_server);
      }
    } catch (SocketException e) {
      print("SocketException: " + e);
    } finally {
      print("[+] Server thread ending");
      StopServer();
    }
  }

  private void Respond() {
    IPAddress responder = _responder;
    _responder = null;

    if (responder == null) {
      print("[+] No IP to respond to. Returning.");
      return;
    }

    print("[+] Connecting to " + responder +
          " to ask for data. Clearing cached IP.");
    using Socket client =
        new(responder.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

    client.Connect(responder, 5001);
    print("[+] Success.");
  }

  public void StopServer() {
    _server?.Stop();
    _server = null;
  }

  private void OnDestroy() { StopServer(); }
}
}
