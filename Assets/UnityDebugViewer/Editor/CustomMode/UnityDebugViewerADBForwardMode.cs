/// Copyright (C) 2020 AsanCai   
/// All rights reserved
/// Email: 969850420@qq.com

using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

namespace UnityDebugViewer
{
    public class UnityDebugViewerADBForwardMode : UnityDebugViewerIntermediaryEditor
    {
        private string pcPort = string.Empty;
        private string phonePort = string.Empty;
        private bool startForwardProcess = false;

        [InitializeOnLoadMethod]
        private static void InitializeADBForwardMode()
        {
            UnityDebugViewerEditorManager.RegisterMode<UnityDebugViewerADBForwardMode>(UnityDebugViewerDefaultMode.ADBForward, 1);
        }

        private void Awake()
        {
            UnityDebugViewerTransferUtility.disconnectToServerEvent += DisconnectToServerHandler;
            UnityDebugViewerTransferUtility.receiveDaraFromServerEvent += ReceiveDataFromServerHandler;
        }

        private void OnDestroy()
        {
            UnityDebugViewerTransferUtility.disconnectToServerEvent -= DisconnectToServerHandler;
            UnityDebugViewerTransferUtility.receiveDaraFromServerEvent -= ReceiveDataFromServerHandler;
        }

        public override void OnGUI()
        {
            GUILayout.Label(new GUIContent("PC Port:"), EditorStyles.label);
            pcPort = GUILayout.TextField(pcPort, 5, EditorStyles.toolbarTextField, GUILayout.MinWidth(50f));
            if (string.IsNullOrEmpty(pcPort))
            {
                pcPort = UnityDebugViewerADBUtility.DEFAULT_FORWARD_PC_PORT;
            }
            else
            {
                pcPort = Regex.Replace(pcPort, @"[^0-9]", "");
            }

            GUILayout.Label(new GUIContent("Phone Port:"), EditorStyles.label);
            phonePort = GUILayout.TextField(phonePort, 5, EditorStyles.toolbarTextField, GUILayout.MinWidth(50f));
            if (string.IsNullOrEmpty(phonePort))
            {
                phonePort = UnityDebugViewerADBUtility.DEFAULT_FORWARD_PHONE_PORT;
            }
            else
            {
                phonePort = Regex.Replace(phonePort, @"[^0-9]", "");
            }

            GUI.enabled = !startForwardProcess;
            if (GUILayout.Button(new GUIContent("Start"), EditorStyles.toolbarButton))
            {
                StartADBForward();
            }

            GUI.enabled = startForwardProcess;
            if (GUILayout.Button(new GUIContent("Stop"), EditorStyles.toolbarButton))
            {
                StopADBForward();
            }

            GUI.enabled = true;
        }

        public override void StartCompiling()
        {
            if (startForwardProcess)
            {
                StopADBForward();
            }
        } 

        private void StartADBForward()
        {
            if (UnityDebugViewerWindowUtility.CheckADBStatus() == false)
            {
                return;
            }

            string adbPath = UnityDebugViewerWindowUtility.GetAdbPath();
            startForwardProcess = UnityDebugViewerADBUtility.StartForwardProcess(pcPort, phonePort, adbPath);
            if (startForwardProcess)
            {
                int port = 0;
                if (int.TryParse(pcPort, out port))
                {
                    UnityDebugViewerTransferUtility.ConnectToServer("127.0.0.1", port);
                }
            }
        }

        private void StopADBForward()
        {
            string adbPath = UnityDebugViewerWindowUtility.GetAdbPath();

            UnityDebugViewerADBUtility.StopForwardProcess(adbPath);
            startForwardProcess = false;

            /// will abort process, should excute at last
            UnityDebugViewerTransferUtility.Clear();
        }


        private void DisconnectToServerHandler()
        {
            string adbPath = UnityDebugViewerWindowUtility.GetAdbPath();
            if (UnityDebugViewerADBUtility.CheckDevice(adbPath) == false)
            {
                UnityDebugViewerLogger.LogError("No devices connect, adb forward process should be restart!", UnityDebugViewerDefaultMode.ADBForward);

                StopADBForward();
            }
        }

        private void ReceiveDataFromServerHandler(byte[] data)
        {
            TransferLogData logData = UnityDebugViewerTransferUtility.BytesToStruct<TransferLogData>(data);
            AddTransferLog(logData);
        }

        /// <summary>
        /// Add log to the UnityDebugViewerEditor correspond to 'ADBForward'
        /// </summary>
        /// <param name="transferLogData"></param>
        private void AddTransferLog(TransferLogData transferLogData)
        {
            string editorMode = UnityDebugViewerDefaultMode.ADBForward;

            LogType type = (LogType)transferLogData.logType;
            string info = transferLogData.info;
            string stack = transferLogData.stack;
            UnityDebugViewerLogger.AddLog(info, stack, type, editorMode);
        }
    }
}
