using UnityEngine;

namespace UnityDebugViewer
{
    public class TestTcp : MonoBehaviour
    {
        private UnityDebugViewerTransfer transfer;
        private void Awake()
        {
            /// 创建一个tcp传输实例
            transfer = new UnityDebugViewerTransfer();
            /// 创建一个tcp server socket并侦听50000端口
            transfer.CreateServerSocket(50000);

            /// 开始收集log信息
            Application.logMessageReceivedThreaded += CaptureLogThread;

            DontDestroyOnLoad(this.gameObject);
        }

        private void OnDestroy()
        {
            Application.logMessageReceivedThreaded -= CaptureLogThread;
            transfer.Clear();
        }

        private void CaptureLogThread(string info, string stacktrace, UnityEngine.LogType type)
        {
            if (transfer == null)
            {
                return;
            }
            lock (transfer)
            {
                /// 将收集到的log数据序列化成byte[]
                /// 并转发至连接到指定端口的tcp client socket
                var logData = new TransferLogData(info, stacktrace, type);
                byte[] sendData = UnityDebugViewerTransferUtility.StructToBytes(logData);
                transfer.SendData(sendData);
            }
        }
    }
}