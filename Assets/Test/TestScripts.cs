using System.Text;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using System;

public class TestScripts : MonoBehaviour {

    int port = 5000;
    string host = "127.0.0.1";

    private void Awake()
    {
        Application.logMessageReceivedThreaded += CaptureLogThread;
    }

    private void OnDestroy()
    {
        Application.logMessageReceivedThreaded -= CaptureLogThread;
        //if(socket != null)
        //{
        //    socket.Close();
        //}
    }

    string info = string.Empty;
    float timer = 0f;
	void Update () {
        timer += Time.deltaTime;
        if(timer >= 1)
        {
            Debug.Log("pass 1s;");

            timer = 0f;
        }
	}

    private bool send = false;
    private void OnGUI()
    {
        //if (GUILayout.Button("打开链接"))
        //{
        //    CreateSocket();
        //}

        //if (GUILayout.Button("断开链接"))
        //{
        //    if(socket != null)
        //    {
        //        socket.Close(); 
        //    }
        //}

        if (GUILayout.Button("开始发送数据"))
        {
            send = true;
        }

        if (GUILayout.Button("停止发送数据"))
        {
            send = false;
            info = string.Empty;
        }

        GUILayout.Label(info);
    }

    //private Socket socket;
    //private void CreateSocket()
    //{
    //    if(socket != null)
    //    {
    //        return;
    //    }

    //    IPAddress ip = IPAddress.Parse(host);

    //    IPEndPoint ipe = new IPEndPoint(ip, port);

    //    socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    //    socket.Connect(ipe);
    //}

    void CaptureLogThread(string condition, string stacktrace, UnityEngine.LogType type)
    {
        if (type == LogType.Error || !send)
        {
            return;
        }

        IPAddress ip = IPAddress.Parse(host);

        IPEndPoint ipe = new IPEndPoint(ip, port);

        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            socket.Connect(ipe);

            info = condition + stacktrace;
            byte[] bs = Encoding.UTF8.GetBytes(info);
            socket.Send(bs);
        }
        catch (Exception e)
        {
            
        }
        finally
        {
            socket.Close();
        }

        


        //TcpClient client = new TcpClient(host, port);

        //// Translate the passed message into ASCII and store it as a Byte array.
        //info = condition + stacktrace;
        //Byte[] data = System.Text.Encoding.UTF8.GetBytes(info);

        //// Get a client stream for reading and writing.
        ////  Stream stream = client.GetStream();

        //NetworkStream stream = client.GetStream();

        //// Send the message to the connected TcpServer.
        //stream.Write(data, 0, data.Length);
        //stream.Close();
        //client.Close();
    }
}
