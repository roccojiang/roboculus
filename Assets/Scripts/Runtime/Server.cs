using System;
using System.Threading;
using System.Net;
using System.Collections;
using System.Net.Sockets;
using System.Collections.Generic;
using UnityEngine;

public class Server : MonoBehaviour {

  bool isFull;
  int buffIndex;
  ArrayList buffer;

  // Start is called before the first frame update
  void Start() {
    isFull = false;
    buffIndex = 0;
    buffer = new ArrayList(20);
    Thread thread = new Thread(WaitForClient);
    thread.Start();
  }

  // Update is called once per frame
  void Update() {
    // if there is data, read it
    if (buffIndex < buffer.Count) {
      var data = buffer[buffIndex];
      // transform.Translate(Vector3.forward * (double) data);
      buffIndex++;
    }

    // Flush the buffer when it's full and we finished reading from it
    if (isFull && buffIndex >= buffer.Count) {
      print("Flushing the buffer");
      buffer = new ArrayList(20);
      buffIndex = 0;
      isFull = false;
    }
  }

  // Runs a server, connects a client, gets data from the client,
  // writes to the buffer (global ArrayList), sends data back to the client,
  // disconnects the client, repeat.
  void WaitForClient() {
    string localAddr = "127.0.0.1";
    int port = 5001;
    TcpListener server = null;
    try {
      server = new TcpListener(IPAddress.Parse(localAddr), port);

      server.Start();
      print("Server has started on " + localAddr + ":" + port);
      Byte[] readBuffer = new Byte[1];

      while (true) {

        print("Waiting for a client connection");
        TcpClient client = server.AcceptTcpClient();
        print("A client is connected.");

        if (!isFull) {
          // Get a stream object for reading and writing
          NetworkStream stream = client.GetStream();
          // i to track number of chars read
          int i;
          // word will be appended with number until space, then parsed into
          // double
          string word = "";
          // Loop to receive all the data sent by the client.
          while ((i = stream.Read(readBuffer, 0, readBuffer.Length)) != 0) {
            // Translate data bytes to an ASCII string.
            string ch = System.Text.Encoding.ASCII.GetString(readBuffer, 0, i);
            // Parsing of the char
            if (ch == " ") {
              buffer.Add(double.Parse(word));
              word = "";
            } else {
              word += ch;
            }
            // reply with received data
            byte[] msg = System.Text.Encoding.ASCII.GetBytes(ch);
            stream.Write(msg, 0, msg.Length);
          }
          isFull = true;
          // Parsing the last word
          buffer.Add(double.Parse(word));
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
}
