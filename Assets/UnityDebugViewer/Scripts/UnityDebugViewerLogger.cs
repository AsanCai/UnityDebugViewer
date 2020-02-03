using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using UnityEngine;

namespace UnityDebugViewer
{
    /// <summary>
    /// socket用于传递log数据的structure
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)] //按1字节对齐
    public struct TransferLogData
    {
        public int logType;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]
        public string info;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
        public string stack;

        public TransferLogData(string _info, string _stack, LogType type)
        {
            var infoLength = _info.Length > 512 ? 512 : _info.Length;
            info = _info.Substring(0, infoLength);
            var stackLength = _stack.Length > 1024 ? 1024 : _stack.Length;
            stack = _stack.Substring(0, stackLength);
            logType = (int)type;
        }
    }

    [Serializable]
    public struct CollapsedLogData
    {
        public LogData log;
        public int count;
    }

    /// <summary>
    /// 保存log数据
    /// </summary>
    [Serializable]
    public class LogData  
    {
        public const string LOGCAT_REGEX = @"(?<time>[\d]+-[\d]+[\s]*[\d]+:[\d]+:[\d]+.[\d]+)[\s]*(?<logType>\w)/(?<filter>[\w]*)[\s]*\([\s\d]*\)[\s:]*";

        public bool isSelected;

        public string info { get; private set; }
        public string extraInfo { get; private set; }
        public LogType type { get; private set; }

        private List<StackData> _stackList;
        public List<StackData> stackList
        {
            get
            {
                if(_stackList == null)
                {
                    _stackList = new List<StackData>();
                }

                return _stackList;
            }
        }

        private string _stack;
        public string stack
        {
            get
            {
                return _stack;
            }
            private set
            {
                _stack = value;
                if (string.IsNullOrEmpty(value))
                {
                    return;
                }

                MatchCollection matchList = Regex.Matches(_stack, StackData.STACK_REGEX);
                this.stackList.Clear();
                foreach (Match match in matchList)
                {
                    if(match == null)
                    {
                        continue;
                    }
                    this.stackList.Add(new StackData(match));
                }

                /// 获取额外信息
                this.extraInfo = Regex.Replace(value, StackData.STACK_REGEX, "").Trim();
            }
        }
        

        public LogData(string info, string stack, LogType type, bool isSelected = false)
        {
            this.stack = stack;

            this.isSelected = isSelected;
            this.info = info;
            this.type = type;
        }

        public string GetKey()
        {
            string key = string.Format("{0}{1}{2}", info, stack, type);
            return key;
        }

        public bool Equals(LogData data)
        {
            if (data == null)
            {
                return false;
            }

            return this.info.Equals(data.info) && this.stack.Equals(data.stack) && this.type == data.type;
        }

        public LogData Clone()
        {
            LogData log = new LogData(this.info, this.stack, this.type);
            return log;
        }
    }

    /// <summary>
    /// 保存堆栈信息
    /// </summary>
    [Serializable]
    public class StackData
    {
        /// <summary>
        /// 匹配堆栈信息的正则表达式
        /// </summary>
        public const string STACK_REGEX = @"(?<className>[\w]+):(?<functionName>[\w]+)\(\)\s\([at]+\s(?<fileDirectory>([a-zA-Z]:[\\/])([\s\.\-\w]+[\\/])*)(?<fileName>[\w]+.[\w]+):(?<lineNumber>[\d]+)\)";

        public string className { get; private set; }
        public string functionName { get; private set; }
        public string fileDirectory { get; private set; }
        public string fileName { get; private set; }
        public string filePath { get; private set; }
        public int lineNumber { get; private set; }

        public string fullStackMessage { get; private set; }
        public string sourceContent;

        public StackData(Match match)
        {
            this.fullStackMessage = match.Value;
            this.className = match.Result("${className}");
            this.functionName = match.Result("${functionName}");

            this.fileDirectory = match.Result("${fileDirectory}");
            this.fileName = match.Result("${fileName}");
            this.filePath = Path.Combine(fileDirectory, fileName);

            string lineNumberStr = match.Result("${lineNumber}");
            int lineNumber;
            this.lineNumber = int.TryParse(lineNumberStr, out lineNumber) ? lineNumber : -1;

            this.sourceContent = String.Empty;
        }

        public bool Equals(StackData data)
        {
            if(data == null)
            {
                return false;
            }

            return fullStackMessage.Equals(data.fullStackMessage);
        }
    }

    public class UnityDebugViewerLogger
    {
        /// <summary>
        /// 输出editor log
        /// </summary>
        /// <param name="transferLogData"></param>
        public static void AddEditorLog(string info, string stack, LogType type)
        {
            /// 默认输出至logcat editor
            UnityDebugViewerEditorType editorType = UnityDebugViewerEditorType.Editor;
            AddLog(info, stack, type, editorType);
        }

        /// <summary>
        /// 输出tcp log
        /// </summary>
        /// <param name="transferLogData"></param>
        public static void AddTransferLog(TransferLogData transferLogData)
        {
            /// 默认输出至logcat editor
            UnityDebugViewerEditorType editorType = UnityDebugViewerEditorType.ADBForward;
            LogType type = (LogType)transferLogData.logType;
            string info = transferLogData.info;
            string stack = transferLogData.stack;
            AddLog(info, stack, type, editorType);
        }


        /// <summary>
        /// 输出logcat log
        /// </summary>
        /// <param name="logcat"></param>
        public static void AddLogcatLog(string logcat)
        {
            if (Regex.IsMatch(logcat, LogData.LOGCAT_REGEX))
            {
                /// 默认输出至logcat editor
                UnityDebugViewerEditorType editorType = UnityDebugViewerEditorType.ADBLogcat;
                var match = Regex.Match(logcat, LogData.LOGCAT_REGEX);
                string logType = match.Result("${logType}").ToUpper();
                string tag = match.Result("${tag}");
                string time = match.Result("${time}");
                string message = Regex.Replace(logcat, LogData.LOGCAT_REGEX, "");

                switch (logType)
                {
                    case "I":
                        AddLog(message, string.Empty, LogType.Log, editorType);
                        break;
                    case "W":
                        AddLog(message, string.Empty, LogType.Warning, editorType);
                        break;
                    case "E":
                        AddLog(message, string.Empty, LogType.Error, editorType);
                        break;
                    default:
                        break;
                }
            }
        }

        public static void AddLog(string info, string stack, LogType type, UnityDebugViewerEditorType editorType)
        {
            var logData = new LogData(info, stack, type);
            AddLog(logData, editorType);
        }

        public static void AddLog(LogData data, UnityDebugViewerEditorType editorType)
        {
            /// 将log输出至指定的editor
            UnityDebugViewerEditorManager.GetEditor(editorType).AddLog(data);
        }

        public static void Log(string str, UnityDebugViewerEditorType editorType = UnityDebugViewerEditorType.Editor)
        {
            string stack = new StackTrace().ToString();
            AddLog(str, stack, LogType.Log, editorType);
        }

        public static void LogWarning(string str, UnityDebugViewerEditorType editorType = UnityDebugViewerEditorType.Editor)
        {
            string stack = new StackTrace().ToString();
            AddLog(str, stack, LogType.Warning, editorType);
        }

        public static void LogError(string str, UnityDebugViewerEditorType editorType = UnityDebugViewerEditorType.Editor)
        {
            string stack = new StackTrace().ToString();
            AddLog(str, stack, LogType.Error, editorType);
        }
    }
}
