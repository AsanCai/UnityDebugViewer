using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UnityDebugViewer
{
    public class UnityDebugViewerADBLogcatMode : UnityDebugViewerIntermediaryEditor
    {
        private string logcatTagFilterStr = "Unity";
        private bool startLogcatProcess = false;

        [InitializeOnLoadMethod]
        private static void InitializeADBLogcatMode()
        {
            UnityDebugViewerEditorManager.RegisterMode<UnityDebugViewerADBLogcatMode>(UnityDebugViewerDefaultMode.ADBLogcat, 2);
        }

        public override void OnGUI()
        {
            GUILayout.Label(new GUIContent("Tag Filter: "), EditorStyles.label);
            logcatTagFilterStr = GUILayout.TextField(logcatTagFilterStr, EditorStyles.toolbarTextField, GUILayout.MinWidth(50f), GUILayout.MaxWidth(100f));

            GUI.enabled = !startLogcatProcess;
            if (GUILayout.Button(new GUIContent("Start"), EditorStyles.toolbarButton))
            {
                StartADBLogcat();
            }

            GUI.enabled = startLogcatProcess;
            if (GUILayout.Button(new GUIContent("Stop"), EditorStyles.toolbarButton))
            {
                StopADBLogcat();
            }

            GUI.enabled = true;
        }

        public override void StartCompiling()
        {
            if (startLogcatProcess)
            {
                StopADBLogcat();
            }
        }

        private void StartADBLogcat()
        {
            string adbPath = UnityDebugViewerWindowUtility.GetAdbPath();
            if (UnityDebugViewerWindowUtility.CheckADBStatus(adbPath) == false)
            {
                return;
            }

            startLogcatProcess = UnityDebugViewerADBUtility.StartLogcatProcess(LogcatDataHandler, logcatTagFilterStr, adbPath);
        }

        private void StopADBLogcat()
        {
            UnityDebugViewerADBUtility.StopLogCatProcess();
            startLogcatProcess = false;
        }

        private static void LogcatDataHandler(object sender, DataReceivedEventArgs outputLine)
        {
            AddLogcatLog(outputLine.Data);
        }

        /// <summary>
        /// Regular expression for the stack message gathered from logcat process
        /// </summary>
        private const string LOGCAT_REGEX = @"(?<date>[\d]+-[\d]+)[\s]*(?<time>[\d]+:[\d]+:[\d]+.[\d]+)[\s]*(?<logType>\w)/(?<filter>[\w]*)[\s]*\([\s\d]*\)[\s:]*";
        /// <summary>
        /// Add log to the UnityDebugViewerEditor correspond to 'ADBLogcat'
        /// </summary>
        /// <param name="logcat"></param>
        private static void AddLogcatLog(string logcat)
        {
            if (Regex.IsMatch(logcat, LOGCAT_REGEX))
            {
                string editorMode = UnityDebugViewerDefaultMode.ADBLogcat;

                var match = Regex.Match(logcat, LOGCAT_REGEX);
                string logType = match.Result("${logType}").ToUpper();
                string time = match.Result("${time}");
                string info = Regex.Replace(logcat, LOGCAT_REGEX, "");
                string extraInfo = string.Empty;
                string stackMessage = string.Empty;
                List<LogStackData> stackList = new List<LogStackData>();

                LogType type;
                switch (logType)
                {
                    case "I":
                        type = LogType.Log;
                        break;
                    case "W":
                        type = LogType.Warning;
                        break;
                    case "E":
                        type = LogType.Error;
                        break;
                    default:
                        type = LogType.Error;
                        break;
                }

                var log = new LogData(info, extraInfo, stackMessage, stackList, time, type);
                UnityDebugViewerLogger.AddLog(log, editorMode);
            }
        }

    }
}
