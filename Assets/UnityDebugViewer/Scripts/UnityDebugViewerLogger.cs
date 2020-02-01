using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace UnityDebugViewer
{
    public class LogData
    {
        public string info;
        public string stack;
        public LogType type;
        public bool isSelected;


        public LogData(string info, string stack, LogType type, bool isSelected = false)
        {
            this.isSelected = isSelected;
            this.info = info;
            this.stack = stack;
            this.type = type;
        }
    }

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

        public static LogData selectedLog = null;

        public static void ClearLog()
        {
            logList.Clear();
            selectedLog = null;
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
            logList.Add(data);
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
