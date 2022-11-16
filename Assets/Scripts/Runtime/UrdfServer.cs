using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Runtime {
public class UrdfServer : MonoBehaviour {
  private const int PORT = 5002;
  private readonly string LOCAL_ADDR = GetLocalIPAddress();

  private bool _isFull;
  private string applicationDataStore;

  // Start is called before the first frame update
  void Start() {
    applicationDataStore = Application.persistentDataPath;
    Thread thread = new Thread(AcceptUrdfData);
    thread.Start();
  }

  // Update is called once per frame
  void Update() {}

  private void AcceptUrdfData() {
    TcpListener server = null;
    try {
      server = new TcpListener(IPAddress.Parse(LOCAL_ADDR), PORT);

      server.Start();
      print("[+] Server has started on " + LOCAL_ADDR + ":" + PORT);

      while (true) {
        int nextByte;

        print("[+] Waiting for a client connection");
        TcpClient client = server.AcceptTcpClient();
        print("[+] A client is connected.");

        NetworkStream stream = client.GetStream();
        List<byte> buffer = new();

        while (Convert.ToChar(nextByte = stream.ReadByte()) != '$') {
          buffer.Add((byte)nextByte);
        }

        // File information sent in the form of "<filename>:<filesize>$"
        string filePair = Encoding.UTF8.GetString(buffer.ToArray());
        string[] nameSize = filePair.Split(':');
        string fileName = nameSize[0];
        int fileSize = int.Parse(nameSize[1]);

        print("[+] Writing " + fileSize + " bytes to " + fileName + " at " +
              applicationDataStore);

        byte[] fileBuffer = new byte[fileSize];
        int currentOffset = 0;

        // Read the zipped directory in increments
        // Attempting to read the whole file at once will most definitely be
        // unsuccessful
        while (currentOffset < fileSize) {
          currentOffset +=
              stream.Read(fileBuffer, currentOffset,
                          Math.Min(fileSize - currentOffset, 1024));
        }

        string tempZip = Path.Combine(
            applicationDataStore, Path.GetTempPath() + Guid.NewGuid() + ".zip");
        string dest = Path.Combine(applicationDataStore, fileName);

        File.WriteAllBytes(tempZip, fileBuffer);

        if (!Directory.Exists(dest)) {
          Directory.CreateDirectory(dest);
        }

        ZipFile.ExtractToDirectory(tempZip, dest);
        File.Delete(tempZip);

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
}