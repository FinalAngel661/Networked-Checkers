using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using UnityEngine;

public class Server : MonoBehaviour
{
    private List<ServerClient> clients;
    private List<ServerClient> disconnectList;

    public int port = 1234;
    private TcpListener server;
    private bool serverStarted;

    public void Init()
    {
        DontDestroyOnLoad(gameObject);

        clients = new List<ServerClient>();
        disconnectList = new List<ServerClient>();
        try
        {
            server = new TcpListener(System.Net.IPAddress.Any, port);
            server.Start();

            StartListening();
            serverStarted = true;
            Debug.Log("Server has been activated " + port.ToString());
        }
        catch (Exception e)
        {
            Debug.Log("Socket error: " + e.Message);
        }
    }

    private void Update()
    {
        if (!serverStarted)
            return;

        foreach (ServerClient c in clients)
        {
            if (!IsConnected(c.tcp))
            {
                c.tcp.Close();
                disconnectList.Add(c);
                continue;
            }
            else
            {
                NetworkStream s = c.tcp.GetStream();
                if (s.DataAvailable)
                {
                    StreamReader reader = new StreamReader(s, true);
                    string data = reader.ReadLine();

                    if (data != null)
                    {
                        OnIncomingData(c, data);
                    }
                }
            }

            for (int i = 0; i < disconnectList.Count - 1; i++)
            {
                BroadCast(disconnectList[i].clientName + " has disconnected", clients);

                clients.Remove(disconnectList[i]);
                disconnectList.RemoveAt(i);

            }
        }

    }

    private void StartListening()
    {
        server.BeginAcceptTcpClient(AcceptTcpClient,server);
    }



    private void AcceptTcpClient(IAsyncResult ar)
    {
        TcpListener listener = (TcpListener)ar.AsyncState;

        string allUsers = "";
        foreach (ServerClient i in clients)
        {
            allUsers += i.clientName + '|';
        }

        ServerClient sc = new ServerClient(listener.EndAcceptTcpClient(ar));
        clients.Add(sc);

        StartListening();

        Broadcast2("HostUsername |" + allUsers, clients[clients.Count - 1]);
    }

    private bool IsConnected(TcpClient c)
    {
        try
        {
            if (c != null && c.Client != null && c.Client.Connected)
            {
                if (c.Client.Poll(0, SelectMode.SelectRead))
                {
                    return !(c.Client.Receive(new byte[1], SocketFlags.Peek) == 0);
                }
                return true;
            }
            else
            {
                return false;
            }

        }
        catch
        {
            return false;
        }
    }

    private void OnIncomingData(ServerClient c, string data)
    {
        Debug.Log("Server: " + data);
        string[] aData = data.Split('|');

        switch (aData[0])
        {
            case "Username":
                c.clientName = aData[1];
                c.isHost = (aData[2] == "0") ? false : true;
                BroadCast("UserDisconnected |" + c.clientName, clients);
                break;

            case "Movement":
                BroadCast("Movement|" + aData[1] + "|" + aData[2] + "|" + aData[3] + "|" + aData[4], clients);
                break;
        }
    }

    private void BroadCast(string data, List<ServerClient> cl)
    {
        foreach(ServerClient c in cl)
        {
            try
            {
                StreamWriter writer = new StreamWriter(c.tcp.GetStream());
                writer.WriteLine(data);
                writer.Flush();
            }
            catch (Exception e)
            {
                Debug.Log("Write error : " + e.Message + "to client " + c.clientName);
            }
        }
    }

    private void Broadcast2(string data, ServerClient c)
    {
        List<ServerClient> sc = new List<ServerClient> { c };
        BroadCast(data, sc);
    }
}

public class ServerClient
{
    public TcpClient tcp;
    public string clientName;
    public bool isHost;

    public ServerClient(TcpClient clientSocket)
    {
        clientName = "Guest";
        tcp = clientSocket;
    }

}