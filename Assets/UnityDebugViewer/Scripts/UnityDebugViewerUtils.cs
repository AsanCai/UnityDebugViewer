using System;
using System.Text;
using System.Runtime.InteropServices;

namespace UnityDebugViewer
{
    public static class UnityDebugViewerUtils
    {
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

            byte[] infoBytes = new byte[512];
            Array.Copy(bytes, 4, infoBytes, 0, 512);
            string info = Encoding.UTF8.GetString(infoBytes);

            IntPtr structPtr = Marshal.AllocHGlobal(size);
            Marshal.Copy(bytes, 0, structPtr, size);
            object obj = Marshal.PtrToStructure(structPtr, type);

            Marshal.FreeHGlobal(structPtr);
            return (T)obj;
        }
    }
}
