/// Copyright (C) 2020 AsanCai   
/// All rights reserved
/// Email: 969850420@qq.com

using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

namespace UnityDebugViewer
{
    public class UnityDebugViewerLogFileMode : UnityDebugViewerIntermediaryEditor
    {
        private string logFilePath = string.Empty;

        [InitializeOnLoadMethod]
        private static void InitializeLogFileMode()
        {
            UnityDebugViewerEditorManager.RegisterMode<UnityDebugViewerLogFileMode>(UnityDebugViewerDefaultMode.LogFile, 3);
        }

        public override void OnGUI()
        {
            GUILayout.Label(new GUIContent("Log File Path:"), EditorStyles.label);

            logFilePath = EditorGUILayout.TextField(logFilePath, EditorStyles.toolbarTextField);
            if (GUILayout.Button(new GUIContent("Browser"), EditorStyles.toolbarButton))
            {
                logFilePath = EditorUtility.OpenFilePanel("Select log file", logFilePath, "txt,log");
            }
            if (GUILayout.Button(new GUIContent("Load"), EditorStyles.toolbarButton))
            {
                ParseLogFile(logFilePath);
            }
        }

        private void ParseLogFile(string logFilePath)
        {
            if (!string.IsNullOrEmpty(logFilePath) && File.Exists(logFilePath))
            {
                var logFineLineArray = File.ReadAllLines(logFilePath);
                if (logFineLineArray == null || logFineLineArray.Length == 0)
                {
                    return;
                }

                var logContent = string.Empty;
                for (int i = 0; i < logFineLineArray.Length; i++)
                {
                    string line = logFineLineArray[i].Trim();
                    if (string.IsNullOrEmpty(line))
                    {
                        AddLogFileLog(logContent);
                        logContent = string.Empty;
                    }
                    else
                    {
                        logContent = string.Format("{0}{1}\n", logContent, line);
                    }
                }

                if (!string.IsNullOrEmpty(logContent))
                {
                    AddLogFileLog(logContent);
                }
            }
        }

        /// <summary>
        /// Regular expression for the log from log file
        /// </summary>
        private const string LOG_FILE_TYPE_AND_TIME_REGEX = @"(?m)(^\[(?<logType>[\w]+)\][\s]*(?<time>[\d]+:[\d]+:[\d]+\.[\d]+))(\|[\d]+)?";
        //private const string LOG_FILE_STACK_REGEX = @"(?m)^(?<className>[\w]+(\.[\<\>\w\s\,\`]+)*)[\s]*:[\s]*(?<methodName>[\<\>\w\s\,\`\.]+\([\w\s\,\[\]\<\>\&\*\`]*\))\r*$";

        /// <summary>
        /// Add log to the UnityDebugViewerEditor correspond to 'ADBLogcat'
        /// </summary>
        /// <param name="logcat"></param>
        private void AddLogFileLog(string logStr)
        {
            string editorMode = UnityDebugViewerDefaultMode.LogFile;
            var match = Regex.Match(logStr, LOG_FILE_TYPE_AND_TIME_REGEX);
            if (match.Success)
            {
                string logType = match.Result("${logType}").ToLower();
                string time = match.Result("${time}");
                LogType type;
                switch (logType)
                {
                    case "log":
                        type = LogType.Log;
                        break;
                    case "warning":
                        type = LogType.Warning;
                        break;
                    case "error":
                        type = LogType.Error;
                        break;
                    default:
                        type = LogType.Error;
                        break;

                }

                string preProcessedStr = Regex.Replace(logStr, LOG_FILE_TYPE_AND_TIME_REGEX, "").Trim();
                string info = Regex.Replace(preProcessedStr, LogData.UNITY_STACK_REGEX, "").Trim();
                string extraInfo = string.Empty;
                string stackMessage = string.Empty;
                List<LogStackData> stackList = new List<LogStackData>();

                if (string.IsNullOrEmpty(info) == false)
                {
                    stackMessage = preProcessedStr.Replace(info, "").Trim();
                    match = Regex.Match(stackMessage, LogData.UNITY_STACK_REGEX);
                    while (match.Success)
                    {
                        stackList.Add(new LogStackData(match));
                        match = match.NextMatch();
                    }

                    if(stackList.Count > 0 && stackList[0].lineNumber == -1)
                    {
                        var stackData = stackList[0];
                        stackList.RemoveAt(0);
                        extraInfo = stackData.ToString();
                    }
                }

                var log = new LogData(info, extraInfo, stackMessage, stackList, time, type);
                UnityDebugViewerLogger.AddLog(log, editorMode);
            }
        }
    }
}
