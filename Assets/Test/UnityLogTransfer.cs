using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using UnityDebugViewer;

/// <summary>
/// 手机端，建立tcp server
/// </summary>
public class UnityLogTransfer : MonoBehaviour
{
    private Socket serverSocket;
    private Socket clientSocket;
    private IPEndPoint ipEnd;
    private string receiveStr;
    private byte[] receiveData = new byte[1024];
    private int receiveLength;
    private Thread connectThread;

    private void Awake()
    {
        CreateServerSocket();
        Application.logMessageReceivedThreaded += CaptureLogThread;
    }

    private void OnApplicationQuit()
    {
        SocketQuit();
        Application.logMessageReceivedThreaded -= CaptureLogThread;
    }

    private List<string> infoList = new List<string>();
    private void OnGUI()
    {
        foreach(var info in infoList)
        {
            GUILayout.Label(info);
        }
    }

    private void CreateServerSocket()
    {
        ipEnd = new IPEndPoint(IPAddress.Any, 50000);

        serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        serverSocket.Bind(ipEnd);
        serverSocket.Listen(10);

        connectThread = new Thread(new ThreadStart(SocketReceive));
        connectThread.Start();
    }

    private void SocketReceive()
    {
        /// 连接
        SocketConnect();
        while (true)
        {
            receiveData = new byte[1024];
            receiveLength = clientSocket.Receive(receiveData);
            if(receiveLength == 0)
            {
                SocketConnect();
                continue;
            }

            receiveStr = Encoding.UTF8.GetString(receiveData, 0, receiveLength);
            infoList.Add(receiveStr);
        }
    }

    private void SocketConnect()
    {
        if(clientSocket != null)
        {
            clientSocket.Close();
        }

        //控制台输出侦听状态
        infoList.Add("Waiting for a client");
        //一旦接受连接，创建一个客户端
        clientSocket = serverSocket.Accept();

        /// 获取客户端的IP和端口
        IPEndPoint ipEndClient = (IPEndPoint)clientSocket.RemoteEndPoint;
        /// 输出客户端的IP和端口
        infoList.Add("Connect with " + ipEndClient.Address.ToString() + ":" + ipEndClient.Port.ToString());
        Debug.Log("Connect to server successfully!");
    }

    private void SocketSend(byte[] data)
    {
        if (clientSocket == null)
        {
            infoList.Add("Client socket is null");
            return;
        }
        
        clientSocket.Send(data);
    }

    private void SocketQuit()
    {
        //先关闭客户端
        if (clientSocket != null)
        {
            clientSocket.Close();
        }

        //再关闭线程
        if (connectThread != null)
        {
            connectThread.Interrupt();
            connectThread.Abort();
        }
        //最后关闭服务器
        if(serverSocket != null)
        {
            serverSocket.Close();
        }
        infoList.Add("Server Close");
    }

    private void CaptureLogThread(string info, string stacktrace, UnityEngine.LogType type)
    {
        /// 连接成功则发送数据
        var logData = new TransferLogData(info, stacktrace, type);

        byte[] sendData = UnityDebugViewerUtils.StructToBytes(logData);
        SocketSend(sendData);
    }
}
