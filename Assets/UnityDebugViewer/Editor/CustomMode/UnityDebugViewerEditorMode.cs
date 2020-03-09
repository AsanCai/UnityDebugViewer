/// Copyright (C) 2020 AsanCai   
/// All rights reserved
/// Email: 969850420@qq.com

using UnityEngine;
using UnityEditor;

namespace UnityDebugViewer
{
    public class UnityDebugViewerEditorMode : UnityDebugViewerIntermediaryEditor
    {
        /// <summary>
        /// 标记入口方法
        /// </summary>
        [InitializeOnLoadMethod]
        private static void InitializeEditorMode()
        {
            UnityDebugViewerEditorManager.RegisterMode<UnityDebugViewerEditorMode>(UnityDebugViewerDefaultMode.Editor, 0);

            Application.logMessageReceivedThreaded -= LogMessageReceivedHandler;
            Application.logMessageReceivedThreaded += LogMessageReceivedHandler;
        }

        public override void Clear()
        {
            UnityDebugViewerWindowUtility.ClearNativeConsoleWindow();
        }

        private static void LogMessageReceivedHandler(string info, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Assert || type == LogType.Exception)
            {
                UnityDebugViewerEditorManager.ForceActiveEditor(UnityDebugViewerDefaultMode.Editor);
                if (UnityDebugViewerWindow.errorPause)
                {
                    UnityEngine.Debug.Break();
                }
            }

            AddEditorLog(info, stackTrace, type);
        }

        /// <summary>
        /// 将log输出至UnityDebugViewerDefaultMode.Editor对应的UnityDebugViewerEditor
        /// </summary>
        /// <param name="transferLogData"></param>
        public static void AddEditorLog(string info, string stack, LogType type)
        {
            string editorType = UnityDebugViewerDefaultMode.Editor;
            UnityDebugViewerLogger.AddLog(info, stack, type, editorType);
        }
    }
}
