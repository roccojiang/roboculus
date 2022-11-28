using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using System.IO;
using System.IO.Compression;
using System.Text;
using TMPro;

namespace Runtime {
public class UrdfServer : MonoBehaviour {
  private const int Port = 5002;
  private readonly string _localAddr = Utils.GetLocalIPAddress();

  private ConcurrentQueue<Exception> _threadException;
  public GameObject errorPopup;
  public ErrorController errorController;

  // Start is called before the first frame update
  void Start() {
    string applicationDataStore = Application.persistentDataPath;
    _threadException = new ConcurrentQueue<Exception>();
    Thread thread = new(() => AcceptUrdfData(applicationDataStore));
    thread.Start();
  }

  private void Update() {
    // can either do this just after client connection or check every frame
    // not sure doing it every frame is great...
    if (!_threadException.TryDequeue(out Exception exc))
      return;

    TextMeshProUGUI ec = errorController.textField;
    ec.text = exc.Message;
    errorPopup.SetActive(true);
  }

  private void AcceptUrdfData(string applicationDataStore) {
    TcpListener server = null;

    try {
      server = new TcpListener(IPAddress.Parse(_localAddr), Port);

      server.Start();
      print("[+] Server has started on " + _localAddr + ":" + Port);

      while (true) {
        print("[+] Waiting for a client connection");
        TcpClient client = server.AcceptTcpClient();
        print("[+] A client is connected.");

        NetworkStream stream = client.GetStream();
        List<byte> buffer = new();

        try {
          int nextByte;

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

          string tempZip =
              Path.Combine(applicationDataStore, Guid.NewGuid() + ".zip");
          string dest = Path.Combine(applicationDataStore, fileName);

          File.WriteAllBytes(tempZip, fileBuffer);

          if (!Directory.Exists(dest)) {
            Directory.CreateDirectory(dest);
            ZipFile.ExtractToDirectory(tempZip, dest);
          }

          File.Delete(tempZip);
        } catch (Exception e) {
          print("[+] EXCEPTION: " + e);
          _threadException.Enqueue(e);
        }

        client.Close();
        print("[+] Client is disconnected");
      }
    } catch (SocketException e) {
      print("SocketException: " + e);
      _threadException.Enqueue(e);
    } finally {
      server?.Stop();
    }
  }
}
}
