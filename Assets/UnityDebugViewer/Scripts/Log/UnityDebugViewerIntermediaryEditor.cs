/// Copyright (C) 2020 AsanCai   
/// All rights reserved
/// Email: 969850420@qq.com

using System;
using UnityEngine;

namespace UnityDebugViewer
{
    [Serializable]
    public class UnityDebugViewerIntermediaryEditor : ScriptableObject
    {
        protected void OnEnable()
        {
            /// 确保在序列化时，可序列化的数据成员不会被重置
            hideFlags = HideFlags.HideAndDontSave;
        }

        /// <summary>
        /// 打开Window的时候被调用
        /// </summary>
        public virtual void OnEditorEnable() { }
        /// <summary>
        /// 关闭Window的时候被调用
        /// </summary>
        public virtual void OnEditorDisable() { }
        /// <summary>
        /// 被选中的时候调用
        /// </summary>
        public virtual void Active() { }
        /// <summary>
        /// 选中时被切换的时候调用
        /// </summary>
        public virtual void Inactive() { }
        /// <summary>
        /// 选中时被调用，用于绘制菜单
        /// </summary>
        public virtual void OnGUI() { }
        /// <summary>
        /// 点击清理按钮时被调用
        /// </summary>
        public virtual void Clear() { }
        /// <summary>
        /// 开始编译时开始调用
        /// </summary>
        public virtual void StartCompiling() { }
    }
}
