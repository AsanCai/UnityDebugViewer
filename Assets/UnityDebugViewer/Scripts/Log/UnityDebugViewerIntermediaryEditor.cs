/// Copyright (C) 2020 AsanCai   
/// All rights reserved
/// Email: 969850420@qq.com

using UnityEngine;

namespace UnityDebugViewer
{
    public class UnityDebugViewerIntermediaryEditor : ScriptableObject
    {
        protected void OnEnable()
        {
            /// 确保在序列化时，可序列化的数据成员不会被重置
            hideFlags = HideFlags.HideAndDontSave;
        }


        public virtual void OnGUI() { }

        public virtual void Clear() { }

        public virtual void StartCompiling() { }
    }
}
