using System;
using System.Threading;
using System.Net;
using System.Collections;
using System.Net.Sockets;
using System.Collections.Generic;
using UnityEngine;

public class Server : MonoBehaviour {
  // Start is called before the first frame update
  void Start() {
    Thread thread = new Thread(WaitForClient);
    thread.Start();
  }

  // Update is called once per frame
  void Update() {}

  void WaitForClient() {
    TcpListener server = new TcpListener(IPAddress.Parse("127.0.0.1"), 5001);

    server.Start();
    print("Server has started on 127.0.0.1:5001.\nWaiting for a connection…");

    TcpClient client = server.AcceptTcpClient();

    print("A client connected.");
  }
}
