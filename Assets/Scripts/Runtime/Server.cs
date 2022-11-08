using System;
using System.Threading;
using System.Net;
using System.Collections;
using System.Net.Sockets;
using System.Collections.Generic;
using UnityEngine;

public class Server : MonoBehaviour {
  private const int PORT = 5001;
  private readonly string LOCAL_ADDR = GetLocalIPAddress();
  private bool _isFull;
  private int _buffIndex;
  private ArrayList _buffer;

  // Start is called before the first frame update
  void Start() {
    _isFull = false;
    _buffIndex = 0;
    _buffer = new ArrayList();
    Thread thread = new Thread(AcceptRobotStateData);
    thread.Start();
  }

  // Update is called once per frame
  void Update() {
    // if there is data, read it
    if (_buffIndex < _buffer.Count) {
      var data = _buffer[_buffIndex];
      print(data);
      transform.GetComponent<ArticulationBody>().TeleportRoot(
          Vector3.forward * 0.5f, transform.rotation); //(double) data);
      _buffIndex++;
    }

    // Flush the _buffer when it's full and we finished reading from it
    if (_isFull && _buffIndex >= _buffer.Count) {
      print("Flushing the _buffer");
      _buffer.Clear();
      _buffIndex = 0;
      _isFull = false;
    }
  }

  // Runs a server, connects a client, gets data from the client,
  // writes to the _buffer (global ArrayList)
  private void AcceptRobotStateData() {
    TcpListener server = null;
    try {
      server = new TcpListener(IPAddress.Parse(LOCAL_ADDR), PORT);

      server.Start();
      int nextByte;
      print("Server has started on " + LOCAL_ADDR + ":" + PORT);

      while (true) {
        print("Waiting for a client connection");
        TcpClient client = server.AcceptTcpClient();
        print("A client is connected.");

        if (!_isFull) {
          // Get a stream object for reading and writing
          NetworkStream stream = client.GetStream();
          string coordinate = "";
          while ((nextByte = stream.ReadByte()) != -1) {
            if (nextByte == ' ') {
              _buffer.Add(double.Parse(coordinate));
              coordinate = "";
            } else {
              coordinate += (char)nextByte;
            }
          }
          // Parsing the last coordinate
          _buffer.Add(double.Parse(coordinate));
          _isFull = true;
        }
        client.Close();
        print("Client is disconnected");
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
    throw new System.Exception(
        "No network adapters with an IPv4 address in the system!");
  }
}
