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

    public struct CollapsedLogData
    {
        public LogData log;
        public int count;
    }

    /// <summary>
    /// 保存log数据
    /// </summary>
    public class LogData  
    {
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

    public static class UnityDebugViewerLogger
    {
        private static List<LogData> _logList = null;
        public static List<LogData> logList
        {
            get
            {
                if(_logList == null)
                {
                    _logList = new List<LogData>();
                }

                return _logList;
            }
        }

        private static List<LogData> _collapsedLogList = null;
        public static List<LogData> collapsedLogList
        {
            get
            {
                if (_collapsedLogList == null)
                {
                    _collapsedLogList = new List<LogData>();
                }

                return _collapsedLogList;
            }
        }

        private static Dictionary<string, CollapsedLogData> _collapsedLogDataDic = null;
        private static Dictionary<string, CollapsedLogData> collapsedLogDic
        {
            get
            {
                if(_collapsedLogDataDic == null)
                {
                    _collapsedLogDataDic = new Dictionary<string, CollapsedLogData>();
                }

                return _collapsedLogDataDic;
            }
        }

        private const int MAX_DISPLAY_NUM = 99;

        private static int _logNum = 0;
        public static int logNum
        {
            get
            {
                return _logNum;
            }
            private set
            {
                int num = value > 99 ? 99 : value;
                _logNum = num;
            }
        }
        private static int _warningNum = 0;
        public static int warningNum
        {
            get
            {
                return _warningNum;
            }
            private set
            {
                int num = value > 99 ? 99 : value;
                _warningNum = num;
            }
        }
        private static int _errorNum = 0;
        public static int errorNum
        {
            get
            {
                return _errorNum;
            }
            private set
            {
                int num = value > 99 ? 99 : value;
                _errorNum = num;
            }
        }

        public static LogData selectedLog = null;

        public static void ClearLog()
        {
            logList.Clear();
            collapsedLogList.Clear();
            collapsedLogDic.Clear();

            selectedLog = null;
            logNum = 0;
            warningNum = 0;
            errorNum = 0;
        }

        public static int GetLogNum(LogData data)
        {
            int num = 1;
            string key = data.GetKey();
            if (collapsedLogDic.ContainsKey(key))
            {
                num = collapsedLogDic[key].count;
            }

            return num;
        }

        public static void AddLog(TransferLogData transferLogData)
        {
            LogType type = (LogType)transferLogData.logType;
            string info = transferLogData.info;
            string stack = transferLogData.stack;
            AddLog(info, stack, type);
        }

        public static void AddLog(string info, string stack, LogType type)
        {
            var logData = new LogData(info, stack, type);
            AddLog(logData);
        }

        public static void AddLog(LogData data)
        {
            switch (data.type)
            {
                case LogType.Log:
                    logNum++;
                    break;
                case LogType.Warning:
                    warningNum++;
                    break;
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    errorNum++;
                    break;
            }
            logList.Add(data);

            CollapsedLogData collapsedLogData;
            string key = data.GetKey();
            var cloneLog = data.Clone();
            if (collapsedLogDic.ContainsKey(key))
            {
                collapsedLogData.count = collapsedLogDic[key].count + 1;
                collapsedLogData.log = collapsedLogDic[key].log;
                collapsedLogDic[key] = collapsedLogData;
            }
            else
            {
                collapsedLogData.log = cloneLog;
                collapsedLogData.count = 1;
                collapsedLogDic.Add(key, collapsedLogData);
                collapsedLogList.Add(cloneLog);
            }
        }

        public static TransferLogData Log(string str)
        {
            string stack = new StackTrace().ToString();
            AddLog(str, stack, LogType.Log);

            TransferLogData logData = new TransferLogData
            {
                info = str,
                stack = stack,
                logType = (int)LogType.Log
            };

            return logData;
        }

        public static TransferLogData LogWarning(string str)
        {
            string stack = new StackTrace().ToString();
            AddLog(str, stack, LogType.Warning);

            TransferLogData logData = new TransferLogData
            {
                info = str,
                stack = stack,
                logType = (int)LogType.Warning
            };

            return logData;
        }

        public static TransferLogData LogError(string str)
        {
            string stack = new StackTrace().ToString();
            AddLog(str, stack, LogType.Error);

            TransferLogData logData = new TransferLogData
            {
                info = str,
                stack = stack,
                logType = (int)LogType.Warning
            };

            return logData;
        }
    }
}
