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
        /// <summary>
        /// 匹配Unity堆栈信息
        /// </summary>
        public const string UNITY_STACK_REGEX = @"(?<className>[\w]+):(?<functionName>[\w]+\(.*\))[\s]*\([at]*\s*(?<filePath>([\w]:/)?([\w]+/)*[\w]+.[\w]+)\:(?<lineNumber>[\d]+)\)";

        public const string LOGCAT_REGEX = @"(?<time>[\d]+-[\d]+[\s]*[\d]+:[\d]+:[\d]+.[\d]+)[\s]*(?<logType>\w)/(?<filter>[\w]*)[\s]*\([\s\d]*\)[\s:]*";

        public bool isSelected;

        public string info { get; private set; }
        public string extraInfo { get; private set; }
        public LogType type { get; private set; }
        public string stackMessage { get; private set; }

        private List<LogStackData> _stackList;
        public List<LogStackData> stackList
        {
            get
            {
                if(_stackList == null)
                {
                    _stackList = new List<LogStackData>();
                }

                return _stackList;
            }
        }

        public LogData(string info, string stack, LogType type, bool isSelected = false)
        {
            this.isSelected = isSelected;
            this.info = info;
            this.type = type;
            this.stackMessage = stack;

            if (string.IsNullOrEmpty(stack))
            {
                return;
            }
            MatchCollection matchList = Regex.Matches(stack, UNITY_STACK_REGEX);
            this.stackList.Clear();
            foreach (Match match in matchList)
            {
                if (match == null)
                {
                    continue;
                }
                this.stackList.Add(new LogStackData(match));
            }

            /// 获取额外信息
            this.extraInfo = Regex.Replace(stack, UNITY_STACK_REGEX, "").Trim();
        }


        public LogData(string info, string extraInfo, List<StackFrame> stackFrameList, LogType logType, bool isSelected = false)
        {
            this.isSelected = isSelected;
            this.info = info;
            this.type = logType;
            this.extraInfo = extraInfo;
            this.stackMessage = extraInfo;

            if (stackFrameList == null)
            {
                return;
            }

            for(int i = 0; i < stackFrameList.Count; i++)
            {
                var logStackData = new LogStackData(stackFrameList[i]);
                this.stackMessage = string.Format("{0}\n{1}", this.stackMessage, logStackData.fullStackMessage);
                this.stackList.Add(logStackData);
            }
        }

        public LogData(string info, string extraInfo, string stack, List<LogStackData> stackList, LogType logType, bool isSelected = false)
        {
            this.info = info;
            this.extraInfo = extraInfo;
            this.stackMessage = stack;
            this.stackList.AddRange(stackList);
            this.type = logType;
            this.isSelected = isSelected;
        }

        public string GetKey()
        {
            string key = string.Format("{0}{1}{2}", info, stackMessage, type);
            return key;
        }

        public bool Equals(LogData data)
        {
            if (data == null)
            {
                return false;
            }

            return this.info.Equals(data.info) && this.stackMessage.Equals(data.stackMessage) && this.type == data.type;
        }

        public LogData Clone()
        {
            LogData log = new LogData(this.info, this.extraInfo, this.stackMessage, this.stackList, this.type, this.isSelected);
            return log;
        }
    }

    /// <summary>
    /// 保存堆栈信息
    /// </summary>
    [Serializable]
    public class LogStackData
    {
        public string className { get; private set; }
        public string functionName { get; private set; }
        public string filePath { get; private set; }
        public int lineNumber { get; private set; }

        public string fullStackMessage { get; private set; }
        public string sourceContent;

        public LogStackData(Match match)
        {
            this.className = match.Result("${className}");
            this.functionName = match.Result("${functionName}");
            this.filePath = UnityDebugViewerEditorUtility.GetSystemFilePath(match.Result("${filePath}"));

            string lineNumberStr = match.Result("${lineNumber}");
            int lineNumber;
            this.lineNumber = int.TryParse(lineNumberStr, out lineNumber) ? lineNumber : -1;

            this.fullStackMessage = string.Format("{0}:{1} (at {2}:{3})", this.className, this.functionName, this.filePath, this.lineNumber);
            this.sourceContent = String.Empty;
        }

        public LogStackData(StackFrame stackFrame)
        {
            var method = stackFrame.GetMethod();

            string methodParam = string.Empty;
            var paramArray = method.GetParameters();
            if (paramArray != null)
            {
                string[] paramType = new string[paramArray.Length];
                for (int index = 0; index < paramArray.Length; index++)
                {
                    paramType[index] = paramArray[index].ParameterType.Name;
                }
                methodParam = string.Join(", ", paramType);
            }

            this.className = method.DeclaringType.Name;
            this.functionName = string.Format("{0}({1})", method.Name, methodParam); ;
            this.filePath = stackFrame.GetFileName();
            this.lineNumber = stackFrame.GetFileLineNumber();

            this.fullStackMessage = string.Format("{0}:{1} (at {2}:{3})", this.className, this.functionName, this.filePath, this.lineNumber);
            this.sourceContent = String.Empty;
        }

        public bool Equals(LogStackData data)
        {
            if(data == null)
            {
                return false;
            }

            return fullStackMessage.Equals(data.fullStackMessage);
        }
    }

    /// <summary>
    /// 标记需要被忽略堆栈信息的方法
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class IgnoreStackTrace : Attribute
    {
        public bool showAsExtraMessage { get; private set; }
        public IgnoreStackTrace(bool show)
        {
            showAsExtraMessage = show;
        }

        /// <summary>
        /// 默认不作为堆栈的extra message显示
        /// </summary>
        public IgnoreStackTrace()
        {
            showAsExtraMessage = false;
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

        public static void AddLog(string info, string extraMessage, List<StackFrame> stackFrameList, LogType type, UnityDebugViewerEditorType editorType)
        {
            var logData = new LogData(info, extraMessage, stackFrameList, type);
            AddLog(logData, editorType);
        }

        public static void AddLog(LogData data, UnityDebugViewerEditorType editorType)
        {
            /// 将log输出至指定的editor
            UnityDebugViewerEditorManager.GetEditor(editorType).AddLog(data);
        }

        [IgnoreStackTrace(true)]
        public static void Log(string str, UnityDebugViewerEditorType editorType = UnityDebugViewerEditorType.Editor)
        {
            AddSystemLog(str, LogType.Log, editorType);
        }

        [IgnoreStackTrace(true)]
        public static void LogWarning(string str, UnityDebugViewerEditorType editorType = UnityDebugViewerEditorType.Editor)
        {
            AddSystemLog(str, LogType.Warning, editorType);
        }

        [IgnoreStackTrace(true)]
        public static void LogError(string str, UnityDebugViewerEditorType editorType = UnityDebugViewerEditorType.Editor)
        {
            AddSystemLog(str, LogType.Error, editorType);
        }

        [IgnoreStackTrace]
        private static void AddSystemLog(string str, LogType logType, UnityDebugViewerEditorType editorType)
        {
            string extraInfo = string.Empty;
            var stackList = ParseSystemStackTrace(ref extraInfo);
            AddLog(str, extraInfo, stackList, logType, editorType);
        }

        [IgnoreStackTrace]
        private static List<StackFrame> ParseSystemStackTrace(ref string extraInfo)
        {
            List<StackFrame> stackFrameList = new List<StackFrame>();

            StackTrace stackTrace = new StackTrace(true);
            StackFrame[] stackFrames = stackTrace.GetFrames();

            for (int i = 0; i < stackFrames.Length; i++)
            {
                StackFrame stackFrame = stackFrames[i];
                var method = stackFrame.GetMethod();

                if (!method.IsDefined(typeof(IgnoreStackTrace), true))
                {
                    /// 过滤掉所有Unity内部调用的方法
                    if (method.Name.Equals("InternalInvoke"))
                    {
                        break;
                    }

                    stackFrameList.Add(stackFrame);
                }
                else
                {
                    foreach (object attributes in method.GetCustomAttributes(false))
                    {
                        IgnoreStackTrace ignoreAttr = (IgnoreStackTrace)attributes;
                        if (ignoreAttr != null && ignoreAttr.showAsExtraMessage)
                        {
                            string methodParam = string.Empty;
                            var paramArray = method.GetParameters();
                            if (paramArray != null)
                            {
                                string[] paramType = new string[paramArray.Length];
                                for (int index = 0; index < paramArray.Length; index++)
                                {
                                    paramType[index] = paramArray[index].ParameterType.Name;
                                }
                                methodParam = string.Join(", ", paramType);
                            }

                            extraInfo = string.Format("{0}:{1}({2})", method.DeclaringType.FullName, method.Name, methodParam);
                        }
                    }
                }
            }

            return stackFrameList;
        }
    }
}
