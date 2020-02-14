using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityDebugViewer;

public class TestTcp : MonoBehaviour
{
    private UnityDebugViewerTransfer transfer;
    private void Awake()
    {
        transfer = new UnityDebugViewerTransfer();
        transfer.CreateServerSocket(50000);

        Application.logMessageReceivedThreaded += CaptureLogThread;
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
            /// 连接成功则发送数据
            var logData = new TransferLogData(info, stacktrace, type);

            byte[] sendData = UnityDebugViewerTransferUtility.StructToBytes(logData);
            transfer.SendData(sendData);
        }
    }
}
