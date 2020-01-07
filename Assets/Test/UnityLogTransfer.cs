using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class UnityLogTransfer : MonoBehaviour
{

    private void Awake()
    {
        CreateSocket();
        Application.logMessageReceivedThreaded += CaptureLogThread;
    }

    private void OnDestroy()
    {
        Application.logMessageReceivedThreaded -= CaptureLogThread;
        socket.Close();
    }

    string info = string.Empty;
    private void OnGUI()
    {
        GUILayout.Label(info);
    }

    private Socket socket;
    private void CreateSocket()
    {
        int port = 5000;
        string host = "127.0.0.1";
        IPAddress ip = IPAddress.Parse(host);

        IPEndPoint ipe = new IPEndPoint(ip, port);

        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Connect(ipe);
    }

    void CaptureLogThread(string condition, string stacktrace, UnityEngine.LogType type)
    {
        if(type == LogType.Error)
        {
            return;
        }


        lock (socket)
        {
            info = condition + stacktrace;
            byte[] bs = Encoding.UTF8.GetBytes(info);
            socket.Send(bs);
        }
    }
}
