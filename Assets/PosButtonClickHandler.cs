// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public class PosButtonClickHandler : MonoBehaviour
// {
//     // Start is called before the first frame update
//     void Start()
//     {
        
//     }

//     // Update is called once per frame
//     void Update()
//     {
        
//     }

//     public void HandleButtonClick()
//     {
//         Debug.Log("Button Clicked!");
//     }
// }


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System;
using TMPro;

public class PosButtonClickHandler : MonoBehaviour
{
    public TextMeshProUGUI textObject;
    public int port = 8888;
    private TcpListener listener;
    private List<TcpClient> clients = new List<TcpClient>();

    // Start is called before the first frame update
    void Start()
    {
        StartServer();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void HandleButtonClick()
    {
        string textToSend = textObject.text;
        SendTextToClients(textToSend);
    }

    void StartServer()
    {
        // Start TCP server on specified port
        listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Debug.Log("Server started on port " + port);

        // Begin accepting client connections in background thread
        listener.BeginAcceptTcpClient(HandleNewConnection, listener);
    }

    void HandleNewConnection(IAsyncResult result)
    {
        // Accept new client connection and add to list of clients
        TcpClient client = listener.EndAcceptTcpClient(result);
        clients.Add(client);

        // Continue accepting new client connections in background thread
        listener.BeginAcceptTcpClient(HandleNewConnection, listener);
    }

    void SendTextToClients(string textToSend)
    {
        // Encode text as byte array
        byte[] data = Encoding.UTF8.GetBytes(textToSend);

        // Send text to all connected clients
        foreach (TcpClient client in clients)
        {
            NetworkStream stream = client.GetStream();
            stream.Write(data, 0, data.Length);
        }
    }

    void OnDestroy()
    {
        // Clean up resources when script is destroyed
        listener.Stop();
        foreach (TcpClient client in clients)
        {
            client.Close();
        }
    }
}
