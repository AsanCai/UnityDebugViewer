/// Copyright (C) 2020 AsanCai   
/// All rights reserved
/// Email: 969850420@qq.com

using System.Diagnostics;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using System.Text;

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
            if (UnityDebugViewerWindowUtility.CheckADBStatus() == false)
            {
                return;
            }

            string adbPath = UnityDebugViewerWindowUtility.GetAdbPath();
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
        private const string LOGCAT_REGEX = @"(?<date>[\d]+-[\d]+)[\s]*(?<time>[\d]+:[\d]+:[\d]+.[\d]+)[\s]*(?<logType>\w)/(?<filter>[\w]*)[\s]*\((?<pid>[\s\d]*)\)[\s:]*";

        private const string LOGCAT_UNITY_STACK_REGEX = @"(?<className>[\w]+(\.[\<\>\w\s\,\`]+)*)[\s]*:[\s]*(?<methodName>[\<\>\w\s\,\`\.]+\([\w\s\,\[\]\<\>\&\*\`]*\))\s*";

        private bool isCollectingInfo = false;
        private bool isCollectingStack = false;

        private string collectedInfo = string.Empty;
        private string collectedStack = string.Empty;
        private string collectedTime = string.Empty;
        private LogType collectingType;

        private string preLogLevel = string.Empty;
        private string preLogFilter = string.Empty;
        private string preTime = string.Empty;
        private string prePID = string.Empty;
        private bool encounterEmpty = false;

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
            string message = Regex.Replace(logcat, LOGCAT_REGEX, "").Trim();
            /// 读到空信息，不进行任何处理
            if (string.IsNullOrEmpty(message))
            {
                encounterEmpty = true;
                return;
            }

            /// 解析logcat的信息
            string time = match.Result("${time}");
            string filter = match.Result("${filter}");
            string logLevel = match.Result("${logType}").ToUpper();
            string pid = match.Result("${pid}");
            LogType type;
            switch (logLevel)
            {
                case "V":
                case "D":
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

            /// logLevel、filter或者时间不一致，说明是不同的log
            bool newLogStart = filter.Equals(preLogFilter) == false
                || logLevel.Equals(preLogLevel) == false
                || time.Equals(preTime) == false
                || pid.Equals(prePID) == false
                || encounterEmpty;
            if (newLogStart)
            {
                FinishCollectingLog();

                /// 开始记录下一条log
                BeginCollectingLog(message, string.Empty, time, type);

                preLogLevel = logLevel;
                preLogFilter = filter;
                preTime = time;
                prePID = pid;
                encounterEmpty = false;
                return;
            }

            var stackMatch = Regex.Match(message, LOGCAT_UNITY_STACK_REGEX);
            if (stackMatch.Success)
            {
                collectedStack = string.Format("{0}\n{1}", collectedStack, message);
                isCollectingStack = true;

                return;
            }

            stackMatch = Regex.Match(message, LogData.ANDROID_STACK_REGEX);
            if (stackMatch.Success)
            {
                collectedStack = string.Format("{0}\n{1}", collectedStack, message);
                isCollectingStack = true;

                return;
            }

            stackMatch = Regex.Match(message, LogData.ANDROID_STACK_REGEX_WITH_PARAM);
            if (stackMatch.Success)
            {
                collectedStack = string.Format("{0}\n{1}", collectedStack, message);
                isCollectingStack = true;

                return;
            }

            if (isCollectingStack)
            {
                /// 在收集stack的时候遇到了log，说明正在收集的log结束
                FinishCollectingLog();

                /// 开始记录下一条log
                BeginCollectingLog(message, string.Empty, time, type);

                preLogLevel = logLevel;
                preLogFilter = filter;
                preTime = time;
                prePID = pid;
                encounterEmpty = false;
            }
            else
            {
                collectedInfo = string.Format("{0}\n{1}", collectedInfo, message);
            }
        }


        private void FinishCollectingLog()
        {
            if(isCollectingInfo == false)
            {
                return;
            }

            isCollectingInfo = false;
            isCollectingStack = false;

            Encoding CURRENT_CODE_PAGE = Encoding.Default;
            Encoding TARGET_CODE_PAGE = Encoding.UTF8;
            /// 使用gb2312对utf8进行编码，获取utf8字节
            byte[] raw = CURRENT_CODE_PAGE.GetBytes(collectedInfo);
            collectedInfo = TARGET_CODE_PAGE.GetString(raw);

            /// 输出收集的信息
            var log = new LogData(collectedInfo, collectedStack, collectedTime, collectingType);
            UnityDebugViewerLogger.AddLog(log, UnityDebugViewerDefaultMode.ADBLogcat);
        }

        private void BeginCollectingLog(string info, string stack, string time, LogType type)
        {
            if (string.IsNullOrEmpty(info))
            {
                return;
            }

            isCollectingInfo = true;
            isCollectingStack = false;

            collectedInfo = info ?? string.Empty;
            collectedStack = stack ?? string.Empty;
            collectedTime = time ?? string.Empty;
            collectingType = type;
        }
    }
}
