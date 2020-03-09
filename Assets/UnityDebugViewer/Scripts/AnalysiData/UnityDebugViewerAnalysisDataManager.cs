/// Copyright (C) 2020 AsanCai   
/// All rights reserved
/// Email: 969850420@qq.com

using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityDebugViewer {
    [Serializable]
	public class UnityDebugViewerAnalysisDataManager : ISerializationCallbackReceiver
    {
        [SerializeField]
        private UnityDebugViewerAnalysisDataTreeItemPool poolInstance;

        [SerializeField]
        private UnityDebugViewerAnalysisDataTreeItem _root;
        public UnityDebugViewerAnalysisDataTreeItem root
        {
            get
            {
                if (_root == null)
                {
                    _root = new UnityDebugViewerAnalysisDataTreeItem(null);
                }

                return _root;
            }
        }

        public AnalysisDataSortType sortType { get; private set; }
        public string searchText { get; private set; }

        public UnityDebugViewerAnalysisDataManager()
        {
            _root = new UnityDebugViewerAnalysisDataTreeItem(null);
            sortType = AnalysisDataSortType.TotalCount;
        }

        public void Clear()
		{
            UnityDebugViewerAnalysisDataTreeItemPool.Instance.Clear();
            root.Clear();
        }

        public void Sort()
        {
            root.Traverse(OnSortChildren);
        }

        public void Sort(AnalysisDataSortType type)
        {
            this.sortType = type;
            root.Traverse(OnSortChildren);
        }

        public void Search(string searchText)
        {
            if(string.IsNullOrEmpty(this.searchText) == false && this.searchText.Equals(searchText))
            {
                return;
            }

            this.searchText = searchText;
            root.Traverse(OnSearchChildren);
        }

        private bool OnSortChildren(UnityDebugViewerAnalysisDataTreeItem node)
        {
            node.SortChildren(SortComparison);

            return true;
        }

        private bool OnSearchChildren(UnityDebugViewerAnalysisDataTreeItem node)
        {
            if (node.Data != null)
            {
                if (string.IsNullOrEmpty(searchText))
                {
                    node.Data.isSearchedStatus = false;
                    node.Data.isVisible = true;
                }
                else
                {
                    node.Data.isSearchedStatus = true;

                    try
                    {
                        if (Regex.IsMatch(node.Data.fullStackMessage, this.searchText))
                        {
                            node.Data.isVisible = true;
                        }
                        else
                        {
                            string str = this.searchText.ToLower();
                            string input = node.Data.fullStackMessage.ToLower();
                            if (Regex.IsMatch(input, str))
                            {
                                node.Data.isVisible = true;
                            }
                            else
                            {
                                node.Data.isVisible = input.Contains(str);
                            }
                        }
                    }
                    catch
                    {
                        string str = this.searchText.ToLower();
                        string input = node.Data.fullStackMessage.ToLower();
                        node.Data.isVisible = input.Contains(str);
                    }
                }
            }

            return true;
        }

        private bool OnResetData(UnityDebugViewerAnalysisDataTreeItem node)
        {
            node.ResetData();
            return true;
        }

        private int SortComparison(UnityDebugViewerAnalysisDataTreeItem x, UnityDebugViewerAnalysisDataTreeItem y)
        {
            var xData = x.Data as UnityDebugViewerAnalysisData;
            var yData = y.Data as UnityDebugViewerAnalysisData;
            if(xData == null || yData == null)
            {
                return 0;
            }

            return xData.CompareTo(this.sortType, yData);
        }
        
		public void AddAnalysisData(LogData log)
		{
            if (log == null)
            {
                return;
            }

			UnityDebugViewerAnalysisDataTreeItem node = _root;
            if(log.stackList.Count == 0)
            {
                UnityDebugViewerAnalysisData stackNode = new UnityDebugViewerAnalysisData(null, log.type, false);
                UnityDebugViewerAnalysisDataTreeItem child = node.FindInChildren(stackNode);
                if (child == null)
                {
                    child = node.AddChild(stackNode);
                }
                else
                {
                    var data = child.Data as UnityDebugViewerAnalysisData;
                    if(data != null)
                    {
                        data.AddLogCount(stackNode);
                    }
                }
            }
            else
            {
                for (int i = log.stackList.Count - 1; i >= 0; i--)
                {
                    UnityDebugViewerAnalysisData stackNode = new UnityDebugViewerAnalysisData(log.stackList[i], log.type, false);
                    UnityDebugViewerAnalysisDataTreeItem child = node.FindInChildren(stackNode);
                    if (child == null)
                    {
                        child = node.AddChild(stackNode);
                    }
                    else
                    {
                        var data = child.Data as UnityDebugViewerAnalysisData;
                        if (data != null)
                        {
                            data.AddLogCount(stackNode);
                        }
                    }

                    node = child;
                }
            }
		}

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            poolInstance = UnityDebugViewerAnalysisDataTreeItemPool.Instance;
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            UnityDebugViewerAnalysisDataTreeItemPool.Instance.ResetInstance(poolInstance);
            root.Traverse(OnResetData);
        }
    }
}