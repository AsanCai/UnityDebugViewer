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

        private void LogcatDataHandler(object sender, DataReceivedEventArgs outputLine)
        {
            AddLogcatLog(outputLine.Data);
        }

        /// <summary>
        /// Regular expression for the stack message gathered from logcat process
        /// </summary>
        private const string LOGCAT_REGEX = @"(?<date>[\d]+-[\d]+)[\s]*(?<time>[\d]+:[\d]+:[\d]+.[\d]+)[\s]*(?<logType>\w)/(?<filter>[\w]*)[\s]*\([\s\d]*\)[\s:]*";

        private const string LOGCAT_STACK_REGEX = @"(?<className>[\w]+(\.[\<\>\w\s\,\`]+)*)[\s]*:[\s]*(?<methodName>[\<\>\w\s\,\`\.]+\([\w\s\,\[\]\<\>\&\*\`]*\))\s*";

        private bool newLogStart = false;
        private bool logStackStart = false;
        
        private LogType type;
        private string time;
        private string info;
        private string stack;

        /// <summary>
        /// Add log to the UnityDebugViewerEditor correspond to 'ADBLogcat'
        /// </summary>
        /// <param name="logcat"></param>
        private void AddLogcatLog(string logcat)
        {
            if (string.IsNullOrEmpty(logcat))
            {
                return;
            }

            var match = Regex.Match(logcat, LOGCAT_REGEX);
            if (match.Success == false)
            {
                return;
            }

            string editorMode = UnityDebugViewerDefaultMode.ADBLogcat;
            
            string message = Regex.Replace(logcat, LOGCAT_REGEX, "").Trim();

            if (newLogStart)
            {
                /// 避免出现中间有多个空行的情况
                if (string.IsNullOrEmpty(message) == false)
                {
                    newLogStart = false;

                    string logType = match.Result("${logType}").ToUpper();
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

                    time = match.Result("${time}");
                    info = message;
                    stack = string.Empty;
                }
            }
            else
            {
                /// log结束
                if (string.IsNullOrEmpty(message))
                {
                    newLogStart = true;
                    logStackStart = false;

                    var log = new LogData(info, stack, time, type);
                    UnityDebugViewerLogger.AddLog(log, editorMode);
                }
                else 
                {
                    /// 读到堆栈信息
                    if(Regex.IsMatch(message, LOGCAT_STACK_REGEX))
                    {
                        if(logStackStart == false)
                        {
                            stack = string.Format("{0}\n{1}", stack, message);
                        }
                        else
                        {
                            logStackStart = true;
                            stack = message;
                        }
                    }
                    else
                    {
                        if(logStackStart == false)
                        {
                            info = string.Format("{0}\n{1}", info, message);
                        }
                    }
                }
            }
        }

    }
}
