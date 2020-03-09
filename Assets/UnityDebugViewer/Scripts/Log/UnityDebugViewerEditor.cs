/// Copyright (C) 2020 AsanCai   
/// All rights reserved
/// Email: 969850420@qq.com


using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityDebugViewer
{
    [Serializable]
    public struct LogFilter
    {
        public bool collapse;
        public bool showTime;
        public bool showLog;
        public bool showWarning;
        public bool showError;

        public bool searchWithRegex;
        public string searchText;

        public bool Equals(LogFilter filter)
        {
            return this.collapse == filter.collapse
                && this.showTime == filter.showTime
                && this.showLog == filter.showLog
                && this.showWarning == filter.showWarning
                && this.showError == filter.showError
                && this.searchWithRegex == filter.searchWithRegex
                && this.searchText.Equals(filter.searchText);
        }

        public bool ShouldDisplay(LogData log)
        {
            bool canDisplayInType;
            switch (log.type)
            {
                case LogType.Log:
                    canDisplayInType = this.showLog;
                    break;

                case LogType.Warning:
                    canDisplayInType = this.showWarning;
                    break;
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    canDisplayInType = this.showError;
                    break;
                default:
                    canDisplayInType = false;
                    break;
            }

            if (canDisplayInType)
            {
                if (string.IsNullOrEmpty(searchText))
                {
                    return true;
                }
                else
                {
                    string logContent = log.GetContent(showTime);
                    string input = logContent.ToLower();
                    string pattern = searchText.ToLower();
                    if(searchWithRegex)
                    {
                        try
                        {
                            if (Regex.IsMatch(logContent, searchText))
                            {
                                return true;
                            }
                            else
                            {
                                if (Regex.IsMatch(input, pattern))
                                {
                                    return true;
                                }
                                else
                                {
                                    return input.Contains(pattern);
                                }
                            }
                        }
                        catch
                        {
                            /// 正则表达式匹配出现错误，则使用普通匹配
                            return input.Contains(pattern);
                        }
                    }
                    else
                    {
                        return input.Contains(pattern);
                    }
                }
            }
            else
            {
                return false;
            }
        }
    }

    /// <summary>
    /// The backend of UnityDebugViewer and provide data for UnityDebugViewerWindow
    /// </summary>
    [Serializable]
    public class UnityDebugViewerEditor : ISerializationCallbackReceiver
    {
        #region 用于保存log数据
        protected int _logNum = 0;
        public int logNum
        {
            get
            {
                return _logNum;
            }
            private set
            {
                _logNum = value;
            }
        }
        protected int _warningNum = 0;
        public int warningNum
        {
            get
            {
                return _warningNum;
            }
            private set
            {
                _warningNum = value;
            }
        }
        protected int _errorNum = 0;
        public int errorNum
        {
            get
            {
                return _errorNum;
            }
            private set
            {
                _errorNum = value;
            }
        }

        public int totalLogNum
        {
            get
            {
                return logList.Count;
            }
        }

        protected List<LogData> _logList = null;
        protected List<LogData> logList
        {
            get
            {
                if (_logList == null)
                {
                    _logList = new List<LogData>();
                }

                return _logList;
            }
        }

        protected List<LogData> _collapsedLogList = null;
        private List<LogData> collapsedLogList
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

        /// <summary>
        /// Dictionary序列化时会丢失数据，需要手动序列化
        /// </summary>
        protected Dictionary<string, CollapsedLogData> _collapsedLogDataDic = null;
        protected Dictionary<string, CollapsedLogData> collapsedLogDic
        {
            get
            {
                if (_collapsedLogDataDic == null)
                {
                    _collapsedLogDataDic = new Dictionary<string, CollapsedLogData>();
                }

                return _collapsedLogDataDic;
            }
        }
        [SerializeField]
        private List<string> serializeKeyList = new List<string>();
        [SerializeField]
        private List<CollapsedLogData> serializeValueList = new List<CollapsedLogData>();

        /// <summary>
        /// the log list used to display by UnityDebugViewerWindow
        /// </summary>
        private List<LogData> _filteredLogList = null;
        private List<LogData> filteredLogList
        {
            get
            {
                if(_filteredLogList == null)
                {
                    _filteredLogList = new List<LogData>();
                }

                return _filteredLogList;
            }
        }
        [SerializeField]
        private LogFilter logFilter;

        public int selectedLogIndex = -1;
        public LogData selectedLog
        {
            get
            {
                if(selectedLogIndex < 0 || selectedLogIndex >= filteredLogList.Count)
                {
                    return null;
                }

                return filteredLogList[selectedLogIndex];
            }
        }
        #endregion

        private string _mode;
        public string mode
        {
            get
            {
                return _mode;
            }
        }
        public UnityDebugViewerIntermediaryEditor intermediaryEditor;

        [SerializeField]
        private UnityDebugViewerAnalysisDataManager _analysisDataManager = null;
        public UnityDebugViewerAnalysisDataManager analysisDataManager
        {
            get
            {
                if(_analysisDataManager == null)
                {
                    _analysisDataManager = new UnityDebugViewerAnalysisDataManager();
                }

                return _analysisDataManager;
            }
        }

        public UnityDebugViewerEditor(string mode)
        {
            _mode = mode;
        }

        /// <summary>
        /// clear log
        /// </summary>
        /// <param name="clearNativeConsoleWindow"></param>
        public void Clear()
        {
            logList.Clear();
            collapsedLogList.Clear();
            collapsedLogDic.Clear();
            filteredLogList.Clear();

            analysisDataManager.Clear();

            selectedLogIndex = -1;
            logNum = 0;
            warningNum = 0;
            errorNum = 0;

            if (intermediaryEditor != null)
            {
                intermediaryEditor.Clear();
            }
        }

        public void OnGUI()
        {
            if (intermediaryEditor != null)
            {
                intermediaryEditor.OnGUI();
            }
        }

        public void StartCompiling()
        {
            if(intermediaryEditor != null)
            {
                intermediaryEditor.StartCompiling();
            }
        }

        public int GetLogNum(LogData data)
        {
            int num = 1;
            
            if(logFilter.showTime == false)
            {
                string key = data.GetKey();
                if (collapsedLogDic.ContainsKey(key))
                {
                    num = collapsedLogDic[key].count;
                }
            }

            return num;
        }

        public List<LogData> GetFilteredLogList(LogFilter filter, bool forceUpdate = false)
        {
            if (forceUpdate || this.logFilter.Equals(filter) == false)
            {
                var selectedLog = this.selectedLog;
                this.filteredLogList.Clear();
                this.selectedLogIndex = -1;
                var logList = (filter.collapse && !filter.showTime) ? this.collapsedLogList : this.logList;
                for(int i = 0; i < logList.Count; i++)
                {
                    var log = logList[i];
                    if(log == null)
                    {
                        continue;
                    }

                    if (filter.ShouldDisplay(log))
                    {
                        this.filteredLogList.Add(log);

                        if (filter.collapse)
                        {
                            if (log.Equals(selectedLog))
                            {
                                this.selectedLogIndex = this.filteredLogList.Count - 1;
                            }
                        }
                        else
                        {
                            if (log == selectedLog)
                            {
                                this.selectedLogIndex = this.filteredLogList.Count - 1;
                            }
                        }
                    }
                }

                this.logFilter = filter;
            }

            return filteredLogList;
        }

        public void AddLog(LogData log)
        {
            switch (log.type)
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
            logList.Add(log);
            analysisDataManager.AddAnalysisData(log);
            analysisDataManager.Sort();

            bool addFilterLog = true;

            /// add collapsed log data
            CollapsedLogData collapsedLogData;
            string key = log.GetKey();
            var cloneLog = log.Clone();
            if (collapsedLogDic.ContainsKey(key))
            {
                collapsedLogData.count = collapsedLogDic[key].count + 1;
                collapsedLogData.log = collapsedLogDic[key].log;
                collapsedLogDic[key] = collapsedLogData;

                /// if not collapse, then should add filter log although the log is collapsed
                addFilterLog = !logFilter.collapse;
            }
            else
            {
                collapsedLogData.log = cloneLog;
                collapsedLogData.count = 1;
                collapsedLogDic.Add(key, collapsedLogData);
                collapsedLogList.Add(cloneLog);
            }

            if (addFilterLog && logFilter.ShouldDisplay(log))
            {
                filteredLogList.Add(log);
            }
        }

        public bool SaveLogToFile(string filePath, bool saveFilteredLog)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return false;
            }

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            string content = string.Empty;
            for (int i = 0; i < logList.Count; i++)
            {
                var log = logList[i];
                if (log == null)
                {
                    continue;
                }

                if (saveFilteredLog && logFilter.ShouldDisplay(log) == false)
                {
                    continue;
                }

                content = string.Format("{0}\n{1}\n", content, log.ToString());
            }

            File.WriteAllText(filePath, content);

            return true;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            this.serializeKeyList.Clear();
            this.serializeValueList.Clear();

            foreach (var pair in this.collapsedLogDic)
            {
                this.serializeKeyList.Add(pair.Key);
                this.serializeValueList.Add(pair.Value);
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            int count = Mathf.Min(this.serializeKeyList.Count, this.serializeValueList.Count);
            for (int i = 0; i < count; ++i)
            {
                this.collapsedLogDic.Add(this.serializeKeyList[i], this.serializeValueList[i]);
            }
        }
    }
}
