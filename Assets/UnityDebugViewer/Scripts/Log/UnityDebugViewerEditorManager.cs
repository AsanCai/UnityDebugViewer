/// Copyright (C) 2020 AsanCai   
/// All rights reserved
/// Email: 969850420@qq.com

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityDebugViewer
{
    /// <summary>
    /// Manage all UnityDebugViewerEditor binded to UnityDebugViewerWindow
    /// </summary>
    [Serializable]
    public class UnityDebugViewerEditorManager : ISerializationCallbackReceiver
    {
        private static List<int> _modeOrderList;
        private static List<int> modeOrderList
        {
            get
            {
                if(_modeOrderList == null)
                {
                    _modeOrderList = new List<int>();
                }

                return _modeOrderList;
            }
        }

        private static List<string> _modeList;
        private static List<string> modeList
        {
            get
            {
                if (_modeList == null)
                {
                    _modeList = new List<string>();
                }

                return _modeList;
            }
        }

        private static Dictionary<string, UnityDebugViewerIntermediaryEditor> _intermediaryEditorDic;
        private static Dictionary<string, UnityDebugViewerIntermediaryEditor> intermediaryEditorDic
        {
            get
            {
                if(_intermediaryEditorDic == null)
                {
                    _intermediaryEditorDic = new Dictionary<string, UnityDebugViewerIntermediaryEditor>();
                }

                return _intermediaryEditorDic;
            }
        }
        [SerializeField]
        private List<string> serializeintermediaryEditorDicKeyList = new List<string>();
        [SerializeField]
        private List<UnityDebugViewerIntermediaryEditor> serializeintermediaryEditorDicValueList = new List<UnityDebugViewerIntermediaryEditor>();

        /// <summary>
        /// 用于绘制mode下拉框的数据
        /// </summary>
        public int activeModeIndex = 0;
        private string[] _modeArray = null;
        public string[] modeArray
        {
            get
            {
                if(_modeArray == null || _modeArray.Length != modeList.Count)
                {
                    _modeArray = modeList.ToArray();
                }

                return _modeArray;
            }
        }

        /// <summary>
        /// 在使用activeMode之前，必须先注册
        /// </summary>
        public string activeMode
        {
            get
            {
                string defaultMode = UnityDebugViewerDefaultMode.Editor;

                if (0 <= activeModeIndex && activeModeIndex <= modeList.Count - 1)
                {
                    defaultMode = modeList[activeModeIndex];
                }
                else
                {
                    activeModeIndex = 0;
                }

                return defaultMode;
            }
        }

        #region 生命周期
        public void OnEnable()
        {
            for(int i = 0; i < modeList.Count; i++)
            {
                var editor = GetEditor(modeList[i]).intermediaryEditor;
                if(editor != null)
                {
                    editor.OnEditorEnable();
                }
            }
        }
        
        public void OnDisable()
        {
            for (int i = 0; i < modeList.Count; i++)
            {
                var editor = GetEditor(modeList[i]).intermediaryEditor;
                if (editor != null)
                {
                    editor.OnEditorDisable();
                }
            }
        }
        #endregion

        private static UnityDebugViewerEditor _activeEditor;
        /// <summary>
        /// May return null
        /// </summary>
        /// <returns></returns>
        public static UnityDebugViewerEditor GetActiveEditor()
        {
            return _activeEditor;
        }

        private static UnityDebugViewerEditor _editorForceToActive = null;
        public UnityDebugViewerEditor activeEditor
        {
            get
            {
                /// 默认激活activeMode对应的UnityDebugViewerEditor
                string mode = activeMode;

                /// 查看是否需要强制激活其他的mode
                if(_editorForceToActive != null && string.IsNullOrEmpty(_editorForceToActive.mode) == false)
                {
                    for(int i = 0;i < modeList.Count; i++)
                    {
                        if(_editorForceToActive.mode.Equals(modeList[i]))
                        {
                            mode = _editorForceToActive.mode;
                            activeModeIndex = i;
                            break;
                        }
                    }
                    _editorForceToActive = null;
                }

                _activeEditor = GetEditor(mode);
                return _activeEditor;
            }
        }

        /// <summary>
        /// dictionary cannot be serilized
        /// </summary>
        private static Dictionary<string, UnityDebugViewerEditor> _editorDic;
        private static Dictionary<string, UnityDebugViewerEditor> editorDic
        {
            get
            {
                if (_editorDic == null)
                {
                    _editorDic = new Dictionary<string, UnityDebugViewerEditor>();
                }

                return _editorDic;
            }
        }
        [SerializeField]
        private List<string> serializeEditorDicKeyList = new List<string>();
        [SerializeField]
        private List<UnityDebugViewerEditor> serializeEditorDicValueList = new List<UnityDebugViewerEditor>();

        /// <summary>
        /// 使用string注册mode，并返回对应的UnityDebugViewerEditor实例
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        public static void RegisterMode<T>(string mode, int order = int.MaxValue) where T : UnityDebugViewerIntermediaryEditor
        {
            /// 避免重复注册同名Mode
            if(modeList.Contains(mode))
            {
                return;
            }

            int index = -1;
            for (int i = 0; i < modeOrderList.Count; i++)
            {
                if (order < modeOrderList[i])
                {
                    index = i;
                    break;
                }
            }

            if (index >= 0)
            {
                modeOrderList.Insert(index, order);
                modeList.Insert(index, mode);
            }
            else
            {
                modeOrderList.Add(order);
                modeList.Add(mode);
            }

            UnityDebugViewerIntermediaryEditor intermediaryEditor = UnityDebugViewerEditorUtility.GetScriptableObjectInstance<T>();
            if (intermediaryEditorDic.ContainsKey(mode))
            {
                intermediaryEditorDic[mode] = intermediaryEditor;
            }
            else
            {
                intermediaryEditorDic.Add(mode, intermediaryEditor);
            }
        }

        /// <summary>
        /// 激活modeList里某个指定的mode
        /// </summary>
        /// <param name="mode"></param>
        public static void ForceActiveEditor(string mode)
        {
            _editorForceToActive = modeList.Contains(mode) ? GetEditor(mode) : null;
        }

        public static UnityDebugViewerEditor GetEditor(string mode)
        {
            UnityDebugViewerEditor editor;
            if (editorDic.ContainsKey(mode))
            {
                editor = editorDic[mode];
            }
            else
            {
                editor = new UnityDebugViewerEditor(mode);
                editorDic.Add(mode, editor);
            }

            if (intermediaryEditorDic.ContainsKey(mode) && editor.intermediaryEditor == null)
            {
                editor.intermediaryEditor = intermediaryEditorDic[mode];
            }

            return editor;
        }

        /// <summary>
        /// 在RegisterMode方法之前执行
        /// </summary>
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            this.serializeEditorDicKeyList.Clear();
            this.serializeEditorDicValueList.Clear();
            foreach (var pair in editorDic)
            {
                this.serializeEditorDicKeyList.Add(pair.Key);
                this.serializeEditorDicValueList.Add(pair.Value);
            }


            this.serializeintermediaryEditorDicKeyList.Clear();
            this.serializeintermediaryEditorDicValueList.Clear();
            foreach(var pair in intermediaryEditorDic)
            {
                this.serializeintermediaryEditorDicKeyList.Add(pair.Key);
                this.serializeintermediaryEditorDicValueList.Add(pair.Value);
            }
        }

        /// <summary>
        /// 在RegisterMode方法之后执行
        /// </summary>
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            int count = Mathf.Min(this.serializeEditorDicKeyList.Count, this.serializeEditorDicValueList.Count);
            editorDic.Clear();
            for (int i = 0; i < count; ++i)
            {
                editorDic.Add(this.serializeEditorDicKeyList[i], this.serializeEditorDicValueList[i]);
            }

            /// intermediaryEditorDic是在OnBeforeSerialize之后赋值的，因此不能清空
            count = Mathf.Min(this.serializeintermediaryEditorDicKeyList.Count, this.serializeintermediaryEditorDicValueList.Count);
            for(int i = 0;i < count; i++)
            {
                var key = this.serializeintermediaryEditorDicKeyList[i];
                var value = this.serializeintermediaryEditorDicValueList[i];
                if (intermediaryEditorDic.ContainsKey(key))
                {
                    intermediaryEditorDic[key] = value;
                }
            }
        }
    }
}
