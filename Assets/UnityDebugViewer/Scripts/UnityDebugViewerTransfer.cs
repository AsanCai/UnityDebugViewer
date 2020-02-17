using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace UnityDebugViewer
{
    public delegate void DisconnectHandler();

    public class UnityDebugViewerTransfer
    {
        private IPAddress ipAddress;
        private IPEndPoint ipEndPoint;
        private Socket serverSocket;
        private Socket clientSocket;

        private byte[] receiveBuffer = new byte[2048];
        private int receiveLength;
        private Thread connectThread;

        public event DisconnectHandler disconnectToServerEvent;
        public event DisconnectHandler disconnectToClientrEvent;

        public void ConnectToServer(string ip, int port)
        {
            Clear();

            ipAddress = IPAddress.Parse(ip);
            ipEndPoint = new IPEndPoint(ipAddress, port);

            connectThread = new Thread(new ThreadStart(ReceiveFromServerSocket));
            connectThread.Start();
        }

        private void ReceiveFromServerSocket()
        {
            ConnectToServerSocket();
            while (true)
            {
                try
                {
                    receiveLength = serverSocket.Receive(receiveBuffer);
                }
                catch
                {
                    if(disconnectToServerEvent != null)
                    {
                        disconnectToServerEvent();
                    }
                }
                if (receiveLength == 0)
                {
                    ConnectToServerSocket();
                    continue;
                }

                byte[] receivedBytes = new byte[receiveLength];
                Array.Copy(receiveBuffer, receivedBytes, receiveLength);

                TransferLogData data = UnityDebugViewerTransferUtility.BytesToStruct<TransferLogData>(receivedBytes);
                UnityDebugViewerLogger.AddTransferLog(data);
            }
        }

        private void ConnectToServerSocket()
        {
            if (serverSocket != null)
            {
                serverSocket.Close();
                serverSocket = null;
            }

            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                serverSocket.Connect(ipEndPoint);
            }
            catch(Exception e)
            {
                if (disconnectToServerEvent != null)
                {
                    disconnectToServerEvent();
                }

                UnityDebugViewerLogger.LogError(e.ToString(), UnityDebugViewerEditorType.ADBForward);
            }
        }


        public void CreateServerSocket(int port)
        {
            Clear();

            ipAddress = IPAddress.Any;
            ipEndPoint = new IPEndPoint(IPAddress.Any, port);

            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(ipEndPoint);
            serverSocket.Listen(10);

            connectThread = new Thread(new ThreadStart(ReceiveFromClientSocket));
            connectThread.Start();
        }

        private void ReceiveFromClientSocket()
        {
            /// 连接
            ConnectToClientSocket();
            while (true)
            {
                try
                {
                    receiveLength = clientSocket.Receive(receiveBuffer);
                }
                catch(Exception e)
                {
                    if(disconnectToClientrEvent != null)
                    {
                        disconnectToClientrEvent();
                    }
                }
                if (receiveLength == 0)
                {
                    ConnectToClientSocket();
                    continue;
                }
            }
        }

        private void ConnectToClientSocket()
        {
            if (clientSocket != null)
            {
                clientSocket.Close();
            }

            try
            {
                clientSocket = serverSocket.Accept();
            }
            catch(Exception e)
            {
                UnityDebugViewerLogger.LogError(e.ToString(), UnityDebugViewerEditorType.ADBForward);
            }
        }

        public void SendData(byte[] data)
        {
            if (clientSocket == null)
            {
                return;
            }

            clientSocket.Send(data);
        }


        public void Clear()
        {
            /// close in order
            if (clientSocket != null)
            {
                clientSocket.Close();
                clientSocket = null;
            }

            if (connectThread != null)
            {
                connectThread.Interrupt();
                connectThread.Abort();
                connectThread = null;
            }

            if (serverSocket != null)
            {
                serverSocket.Close();
                serverSocket = null;
            }
        }
    }
}
