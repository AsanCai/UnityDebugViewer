using UnityEngine;
using UnityEditor;

namespace UnityDebugViewer
{
    public class UnityDebugViewerEditorMode : UnityDebugViewerIntermediaryEditor
    {
        [InitializeOnLoadMethod]
        private static void InitializeEditorMode()
        {
            var intermediaryEditor = UnityDebugViewerEditorUtility.GetScriptableObjectInstance<UnityDebugViewerEditorMode>();

            UnityDebugViewerEditorManager.RegisterMode(UnityDebugViewerDefaultMode.Editor, intermediaryEditor, 0);

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
        /// Add log to the UnityDebugViewerEditor correspond to 'Editor'
        /// </summary>
        /// <param name="transferLogData"></param>
        public static void AddEditorLog(string info, string stack, LogType type)
        {
            string editorType = UnityDebugViewerDefaultMode.Editor;
            UnityDebugViewerLogger.AddLog(info, stack, type, editorType);
        }
    }
}
