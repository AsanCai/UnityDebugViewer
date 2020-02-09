using System;
using System.Collections.Generic;
using UnityEngine;            

namespace UnityDebugViewer
{
    [Serializable]
    public class UnityDebugViewerAnalysisDataTreeItem
    {
		public delegate bool TraversalDataDelegate(UnityDebugViewerAnalysisData data);
		public delegate bool TraversalNodeDelegate(UnityDebugViewerAnalysisDataTreeItem node);

        [SerializeField]
		protected UnityDebugViewerAnalysisData _data;
        [SerializeField]
        protected UnityDebugViewerAnalysisDataTreeItem _parent;
        protected int _level;
        [SerializeField]
        protected List<UnityDebugViewerAnalysisDataTreeItem> _children;

		public UnityDebugViewerAnalysisDataTreeItem(UnityDebugViewerAnalysisData data)
		{
			_data = data;
			_children = new List<UnityDebugViewerAnalysisDataTreeItem>();
			_level = 0;
		}

		public UnityDebugViewerAnalysisDataTreeItem(UnityDebugViewerAnalysisData data, UnityDebugViewerAnalysisDataTreeItem parent) : this(data)
		{
			_parent = parent;
            _level = _parent != null ? _parent.Level + 1 : 0;
		}

        public int Row;
		public int Level
        {
            get
            {
                /// enter search status
                if (this.Data != null && this.Data.isSearchedStatus && this.Data.isVisible)
                {
                    return 1;
                }

                return _level;
            }
        }
		public int ChildrenCount { get { return _children.Count; }}
		public bool IsRoot { get { return _parent==null; }}
		public bool IsLeaf
        {
            get
            {
                /// enter search status
                if (this.Data != null && this.Data.isSearchedStatus && this.Data.isVisible)
                {
                    return true;
                }

                return _children.Count == 0;
            }
        }
		public UnityDebugViewerAnalysisData Data { get { return _data; }}
		public UnityDebugViewerAnalysisDataTreeItem Parent { get { return _parent; }}

		public UnityDebugViewerAnalysisDataTreeItem this[int key]
		{
			get { return _children[key]; }
		}

		public void Clear()
		{
			_children.Clear();
		}

		public UnityDebugViewerAnalysisDataTreeItem AddChild(UnityDebugViewerAnalysisData value)
		{
            UnityDebugViewerAnalysisDataTreeItem node = new UnityDebugViewerAnalysisDataTreeItem(value, this);
			_children.Add(node);

			return node;
		}

        public UnityDebugViewerAnalysisDataTreeItem GetChild(int index)
        {
            if(index < 0 || index >= ChildrenCount)
            {
                return null;
            }

            return _children[index];
        }

        public bool HasChild(UnityDebugViewerAnalysisData data)
		{
            return FindInChildren(data) != null;
		}

		public UnityDebugViewerAnalysisDataTreeItem FindInChildren(UnityDebugViewerAnalysisData data)
		{
			for(int i = 0; i < ChildrenCount; ++i)
            { 
				UnityDebugViewerAnalysisDataTreeItem child = _children[i];
                if (child.Data.Equals(data))
                {
                    return child;
                }
			}

			return null;
		}

		public bool RemoveChild(UnityDebugViewerAnalysisDataTreeItem node)
		{
			return _children.Remove(node);
		}

        public void SortChildren(Comparison<UnityDebugViewerAnalysisDataTreeItem> comparison)
        {
            _children.Sort(comparison);
        }

		public void Traverse(TraversalDataDelegate handler)
		{
            if (handler(_data))
            { 
				for(int i = 0; i < ChildrenCount; ++i)
                {
                    _children[i].Traverse(handler);
                }
			}
		}

		public void Traverse(TraversalNodeDelegate handler)
		{
            if (handler(this))
            { 
				for(int i = 0; i < ChildrenCount; ++i)
                {
                    _children[i].Traverse(handler);
                }
			}
		}
    }
}