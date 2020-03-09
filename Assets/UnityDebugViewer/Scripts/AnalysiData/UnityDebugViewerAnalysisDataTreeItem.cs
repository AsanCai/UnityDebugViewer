/// Copyright (C) 2020 AsanCai   
/// All rights reserved
/// Email: 969850420@qq.com

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityDebugViewer
{
    [Serializable]
    public class UnityDebugViewerAnalysisDataTreeItemPool
    {
        private static UnityDebugViewerAnalysisDataTreeItemPool instance;
        public static UnityDebugViewerAnalysisDataTreeItemPool Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new UnityDebugViewerAnalysisDataTreeItemPool();
                }

                return instance;
            }
        }

        public void ResetInstance(UnityDebugViewerAnalysisDataTreeItemPool pool)
        {
            instance = pool;
        }

        [SerializeField]
        public List<UnityDebugViewerAnalysisDataTreeItem> _itemPool;
        private List<UnityDebugViewerAnalysisDataTreeItem> itemPool
        {
            get
            {
                if(_itemPool == null)
                {
                    _itemPool = new List<UnityDebugViewerAnalysisDataTreeItem>();
                }

                return _itemPool;
            }
        }


        public int AddItem(UnityDebugViewerAnalysisDataTreeItem item)
        {
            int id = 0;
            lock (itemPool)
            {
                itemPool.Add(item);
                id = itemPool.Count;
            }

            return id;
        }

        public UnityDebugViewerAnalysisDataTreeItem GetItem(int id)
        {
            int index = id - 1;
            if(index < 0 || index >= itemPool.Count)
            {
                return null;
            }

            return itemPool[index];
        }

        public void Clear()
        {
            itemPool.Clear();
        }
    }


    [Serializable]
    public class UnityDebugViewerAnalysisDataTreeItem
    {
		public delegate bool TraversalDataDelegate(UnityDebugViewerAnalysisData data);
		public delegate bool TraversalNodeDelegate(UnityDebugViewerAnalysisDataTreeItem node);

        /// <summary>
        /// Serializable custom classes behave like structs
        /// and it will never be null after serializeation
        /// </summary>
        [SerializeField]
		protected UnityDebugViewerAnalysisData _data = null;

        /// <summary>
        /// Avoiding Unity's Infinite Depth Warning
        /// </summary>
        [NonSerialized]
        protected UnityDebugViewerAnalysisDataTreeItem _parent;
        [NonSerialized]
        private List<UnityDebugViewerAnalysisDataTreeItem> _children;
        protected List<UnityDebugViewerAnalysisDataTreeItem> children
        {
            get
            {
                if (_children == null)
                {
                    _children = new List<UnityDebugViewerAnalysisDataTreeItem>();
                }

                return _children;
            }
        }

        public int id { get; private set; }
        private int _parentID;
        private List<int> _childrenID;
        private List<int> childrenID
        {
            get
            {
                if(_childrenID == null)
                {
                    _childrenID = new List<int>();
                }

                return _childrenID;
            }
        }

        public UnityDebugViewerAnalysisDataTreeItem(UnityDebugViewerAnalysisData data)
        {
            _data = data;
            _level = 0;
            id = UnityDebugViewerAnalysisDataTreeItemPool.Instance.AddItem(this);
        }

        public UnityDebugViewerAnalysisDataTreeItem(UnityDebugViewerAnalysisData data, UnityDebugViewerAnalysisDataTreeItem parent) : this(data)
        {
            _parent = parent;
            _parentID = parent.id;
            _level = _parent != null ? _parent.Level + 1 : 0;
        }

        public int Row;

        protected int _level;
        public int Level
        {
            get
            {
                /// enter search status
                if (UnityDebugViewerAnalysisData.IsNullOrEmpty(this.Data) == false && this.Data.isSearchedStatus && this.Data.isVisible)
                {
                    return 1;
                }

                return _level;
            }
        }

		public int ChildrenCount { get { return children.Count; }}
		public bool IsRoot { get { return _parent==null; }}
		public bool IsLeaf
        {
            get
            {
                /// enter search status
                if (UnityDebugViewerAnalysisData.IsNullOrEmpty(this.Data) == false && this.Data.isSearchedStatus && this.Data.isVisible)
                {
                    return true;
                }

                return children.Count == 0;
            }
        }
		public UnityDebugViewerAnalysisData Data { get { return _data; }}
		public UnityDebugViewerAnalysisDataTreeItem Parent { get { return _parent; }}

		public UnityDebugViewerAnalysisDataTreeItem this[int key]
		{
			get { return children[key]; }
		}

		public void Clear()
		{
            childrenID.Clear();
			children.Clear();

            _parent = null;
            _parentID = 0;

            _data = null;

        }

		public UnityDebugViewerAnalysisDataTreeItem AddChild(UnityDebugViewerAnalysisData value)
		{
            UnityDebugViewerAnalysisDataTreeItem node = new UnityDebugViewerAnalysisDataTreeItem(value, this);
			children.Add(node);
            childrenID.Add(node.id);
			return node;
		}

        public UnityDebugViewerAnalysisDataTreeItem GetChild(int index)
        {
            if(index < 0 || index >= ChildrenCount)
            {
                return null;
            }

            return children[index];
        }

        public bool HasChild(UnityDebugViewerAnalysisData data)
		{
            return FindInChildren(data) != null;
		}

		public UnityDebugViewerAnalysisDataTreeItem FindInChildren(UnityDebugViewerAnalysisData data)
		{
			for(int i = 0; i < ChildrenCount; ++i)
            { 
				UnityDebugViewerAnalysisDataTreeItem child = children[i];
                if (child.Data.Equals(data))
                {
                    return child;
                }
			}

			return null;
		}

		public bool RemoveChild(UnityDebugViewerAnalysisDataTreeItem node)
		{
			return children.Remove(node) && childrenID.Remove(node.id); 
		}

        public void SortChildren(Comparison<UnityDebugViewerAnalysisDataTreeItem> comparison)
        {
            children.Sort(comparison);
            /// update id
            childrenID.Clear();
            for (int i = 0;i < children.Count; i++)
            {
                childrenID.Add(children[i].id);
            }
        }

		public void Traverse(TraversalDataDelegate handler)
		{
            if (handler(_data))
            { 
				for(int i = 0; i < ChildrenCount; ++i)
                {
                    children[i].Traverse(handler);
                }
			}
		}

		public void Traverse(TraversalNodeDelegate handler)
		{
            if (handler(this))
            { 
				for(int i = 0; i < ChildrenCount; ++i)
                {
                    children[i].Traverse(handler);
                }
			}
		}

        public void ResetData()
        {
            children.Clear();

            for (int i = 0;i < childrenID.Count; i++)
            {
                var item = UnityDebugViewerAnalysisDataTreeItemPool.Instance.GetItem(childrenID[i]);
                if(item == null)
                {
                    continue;
                }

                children.Add(item);
            }

            _parent = UnityDebugViewerAnalysisDataTreeItemPool.Instance.GetItem(_parentID);
        }
    }
}