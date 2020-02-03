using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace UnityDebugViewer
{
    public static class UnityDebugViewerTcp
    {
        private static IPAddress ipAddress;
        private static IPEndPoint ipEndPoint;
        private static Socket serverSocket;

        private static byte[] receiveBuffer = new byte[2048];
        private static int receiveLength;
        private static Thread connectThread;

        public static void ConnectToServer(string ip, int port)
        {
            ipAddress = IPAddress.Parse(ip);
            ipEndPoint = new IPEndPoint(ipAddress, port);

            connectThread = new Thread(new ThreadStart(SocketReceive));
            connectThread.Start();
        }

        private static void SocketReceive()
        {
            SocketConnect();
            while (true)
            {
                receiveLength = serverSocket.Receive(receiveBuffer);
                if (receiveLength == 0)
                {
                    SocketConnect();
                    continue;
                }

                byte[] receivedBytes = new byte[receiveLength];
                Array.Copy(receiveBuffer, receivedBytes, receiveLength);

                TransferLogData data = UnityDebugViewerUtils.BytesToStruct<TransferLogData>(receivedBytes);
                UnityDebugViewerLogger.AddTransferLog(data);
            }
        }

        private static void SocketConnect()
        {
            if (serverSocket != null)
            {
                serverSocket.Close();
                serverSocket = null;
            }

            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Connect(ipEndPoint);

            ///// 输出初次连接收到的字符串
            //receiveLength = serverSocket.Receive(receiveBuffer);
            //if (receiveLength == 0)
            //{
            //    return;
            //}

            //byte[] receivedBytes = new byte[receiveLength];
            //Array.Copy(receiveBuffer, receivedBytes, receiveLength);

            //TransferLogData data = UnityDebugViewerUtils.BytesToStruct<TransferLogData>(receivedBytes);
            //UnityDebugViewerLogger.AddLog(data);
        }

        private static void SocketSend(string data)
        {
            if (serverSocket == null)
            {
                return;
            }

            byte[] sendData = Encoding.UTF8.GetBytes(data);
            serverSocket.Send(sendData);
        }

        public static void Disconnect()
        {
            //先关闭线程
            if (connectThread != null)
            {
                connectThread.Interrupt();
                connectThread.Abort();
                connectThread = null;
            }

            //最后关闭服务器
            if (serverSocket != null)
            {
                serverSocket.Close();
                serverSocket = null;
            }
        }
    }
}
