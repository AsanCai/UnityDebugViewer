/// Copyright (C) 2020 AsanCai   
/// All rights reserved
/// Email: 969850420@qq.com

using System;
using UnityEngine;

namespace UnityDebugViewer
{
    public enum AnalysisDataSortType
    {
        TotalCount,
        LogCount,
        WarningCount,
        ErrorCount
    }

    [Serializable]
    public class UnityDebugViewerAnalysisData
    {
        public string className { get; private set; }
        public string methodName { get; private set; }
        public string fullStackMessage { get; private set; }

        public int logCount { get; private set; }
        public int warningCount { get; private set; }
        public int errorCount { get; private set; }
        public int totalCount
        {
            get
            {
                return logCount + warningCount + errorCount;
            }
        }

        public bool isExpanded { get; set; }
        public bool isVisible { get; set; }
        public bool isSearchedStatus { get; set; }

        public UnityDebugViewerAnalysisData(LogStackData stackData, LogType type, bool isExpanded)
        {
            if (stackData == null)
            {
                this.className = "UnknowClass";
                this.methodName = "UnknowMethod";
            }
            else
            {
                this.className = stackData.className;
                this.methodName = stackData.methodName;
            }
            this.fullStackMessage = string.Format("{0}:{1}", this.className, this.methodName);

            this.errorCount = 0;
            this.warningCount = 0;
            this.errorCount = 0;
            AddLogCount(type);

            this.isExpanded = isExpanded;
            this.isSearchedStatus = false;
            this.isVisible = true;
        }

        public static bool IsNullOrEmpty(UnityDebugViewerAnalysisData data)
        {
            return data == null || string.IsNullOrEmpty(data.fullStackMessage);
        }

        public string[] getColumnArray()
        {
            string[] columnArray = new string[]
            {
                totalCount.ToString(),
                logCount.ToString(),
                warningCount.ToString(),
                errorCount.ToString()
            };

            return columnArray;
        }

        public void AddLogCount(LogType type, int count = 1)
        {
            switch (type)
            {
                case LogType.Log:
                    logCount += count;
                    break;
                case LogType.Warning:
                    warningCount += count;
                    break;
                case LogType.Error:
                case LogType.Assert:
                case LogType.Exception:
                    errorCount += count;
                    break;
                default:
                    break;
            }
        }

        public void AddLogCount(UnityDebugViewerAnalysisData data)
        {
            this.logCount += data.logCount;
            this.warningCount += data.warningCount;
            this.errorCount += data.errorCount;
        }

        public int CompareTo(AnalysisDataSortType sortType, UnityDebugViewerAnalysisData data)
        {
            int result = 0;
            switch (sortType)
            {
                case AnalysisDataSortType.TotalCount:
                    result = this.totalCount - data.totalCount;
                    break;
                case AnalysisDataSortType.LogCount:
                    result = this.logCount - data.logCount;
                    break;
                case AnalysisDataSortType.WarningCount:
                    result = this.warningCount - data.warningCount;
                    break;
                case AnalysisDataSortType.ErrorCount:
                    result = this.errorCount - data.errorCount;
                    break;
            }

            /// 降序排列
            if (result > 0)
            {
                result = -1;
            }
            else if (result < 0)
            {
                result = 1;
            }

            return result;
        }

        public override string ToString()
        {
            return this.fullStackMessage;
        }

        public override int GetHashCode()
        {
            return fullStackMessage.GetHashCode() + 10;
        }

        public override bool Equals(object obj)
        {
            UnityDebugViewerAnalysisData node = obj as UnityDebugViewerAnalysisData;
            return node != null && this.Equals(node);
        }

        public bool Equals(UnityDebugViewerAnalysisData node)
        {
            return node.fullStackMessage.Equals(this.fullStackMessage);
        }


    }
}
