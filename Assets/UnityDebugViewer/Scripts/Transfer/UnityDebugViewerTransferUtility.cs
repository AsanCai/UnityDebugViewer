/// Copyright (C) 2020 AsanCai   
/// All rights reserved
/// Email: 969850420@qq.com

using System;
using System.Runtime.InteropServices;

namespace UnityDebugViewer
{
    public static class UnityDebugViewerTransferUtility
    {
        public static event DisconnectHandler disconnectToServerEvent;
        public static event DisconnectHandler disconnectToClientrEvent;
        public static event ReceiveDataHandler receiveDaraFromServerEvent;
        public static event ReceiveDataHandler receiveDaraFromClientEvent;

        private static UnityDebugViewerTransfer transferInstance = null;

        public static void ConnectToServer(string ip, int port)
        {
            if(transferInstance == null)
            {
                transferInstance = new UnityDebugViewerTransfer();
                transferInstance.disconnectToServerEvent += disconnectToServerEvent;
                transferInstance.disconnectToClientrEvent += disconnectToClientrEvent;
                transferInstance.receiveDaraFromServerEvent += receiveDaraFromServerEvent;
                transferInstance.receiveDaraFromClientEvent += receiveDaraFromClientEvent;
            }

            transferInstance.ConnectToServer(ip, port);
        }

        public static void CreateServerSocket(int port)
        {
            if (transferInstance == null)
            {
                transferInstance = new UnityDebugViewerTransfer();
                transferInstance.disconnectToServerEvent += disconnectToServerEvent;
                transferInstance.disconnectToClientrEvent += disconnectToClientrEvent;
                transferInstance.receiveDaraFromServerEvent += receiveDaraFromServerEvent;
                transferInstance.receiveDaraFromClientEvent += receiveDaraFromClientEvent;
            }

            transferInstance.CreateServerSocket(port);
        }

        public static void SendData(byte[] data)
        {
            if(transferInstance != null)
            {
                transferInstance.SendData(data);
            }
        }

        public static void Clear()
        {
            if(transferInstance != null)
            {
                transferInstance.Clear();
            }
        }

        public static byte[] StructToBytes(object data)
        {
            /// 得到结构体的大小
            int size = Marshal.SizeOf(data);
            /// 分配结构体大小的空间
            IntPtr structPtr = Marshal.AllocHGlobal(size);
            /// 将结构体存储到分配好的空间
            Marshal.StructureToPtr(data, structPtr, false);

            byte[] bytes = new byte[size];

            /// 从内存空间拷贝到byte数组
            Marshal.Copy(structPtr, bytes, 0, size);
            /// 释放内存空间
            Marshal.FreeHGlobal(structPtr);

            return bytes;
        }


        public static T BytesToStruct<T>(byte[] bytes)
        {
            Type type = typeof(T);
            int size = Marshal.SizeOf(type);
            if(size > bytes.Length)
            {
                return default(T);
            }

            IntPtr structPtr = Marshal.AllocHGlobal(size);
            Marshal.Copy(bytes, 0, structPtr, size);
            object obj = Marshal.PtrToStructure(structPtr, type);

            Marshal.FreeHGlobal(structPtr);
            return (T)obj;
        }
    }
}
