using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityDebugViewer {
    [Serializable]
	public class UnityDebugViewerAnalysisDataManager
	{
        [SerializeField]
        private UnityDebugViewerAnalysisDataTreeItem _root;
        public UnityDebugViewerAnalysisDataTreeItem Root
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
			Root.Clear();
		}

        public void Sort()
        {
            Root.Traverse(OnSortChildren);
        }

        public void Sort(AnalysisDataSortType type)
        {
            this.sortType = type;
            Root.Traverse(OnSortChildren);
        }

        public void Search(string searchText)
        {
            if(this.searchText.Equals(searchText))
            {
                return;
            }

            this.searchText = searchText;
            Root.Traverse(OnSearchChildren);
        }

        private bool OnSortChildren(UnityDebugViewerAnalysisDataTreeItem node)
        {
            node.SortChildren(SortComparison);

            return true;
        }

        private bool OnSearchChildren(UnityDebugViewerAnalysisDataTreeItem node)
        {
            var analysisData = node.Data as UnityDebugViewerAnalysisData;

            if (analysisData != null)
            {
                if (string.IsNullOrEmpty(searchText))
                {
                    analysisData.isSearchedStatus = false;
                    analysisData.isVisible = true;
                }
                else
                {
                    analysisData.isSearchedStatus = true;

                    if (Regex.IsMatch(analysisData.fullStackMessage, this.searchText))
                    {
                        node.Data.isVisible = true;
                    }
                    else
                    {
                        string str = this.searchText.ToLower();
                        string input = analysisData.fullStackMessage.ToLower();
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
            }

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
                UnityDebugViewerAnalysisData stackNode = new UnityDebugViewerAnalysisData(null, log.type, node.Level == 0);
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
                    UnityDebugViewerAnalysisData stackNode = new UnityDebugViewerAnalysisData(log.stackList[i], log.type, node.Level == 0);
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

	}
}