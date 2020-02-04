using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityDebugViewer
{
    public enum UnityDebugViewerEditorType
    {
        Editor,
        ADBForward,
        ADBLogcat,
        LogFile
    }

    /// <summary>
    /// Manage all UnityDebugViewerEditor binded to UnityDebugViewerWindow
    /// </summary>
    [Serializable]
    public class UnityDebugViewerEditorManager : ScriptableObject, ISerializationCallbackReceiver
    {
        public UnityDebugViewerEditorType activeEditorType = UnityDebugViewerEditorType.Editor;
        public string activeEditorTypeStr
        {
            get
            {
                string str;
                switch (activeEditorType)
                {
                    case UnityDebugViewerEditorType.Editor:
                        str = "Editor";
                        break;
                    case UnityDebugViewerEditorType.ADBForward:
                        str = "ADB Forward";
                        break;
                    case UnityDebugViewerEditorType.ADBLogcat:
                        str = "ADB Logcat";
                        break;
                    case UnityDebugViewerEditorType.LogFile:
                        str = "Log File";
                        break;
                    default:
                        str = string.Empty;
                        break;
                }

                return str;
            }
        }

        private static UnityDebugViewerEditor _editorForceToActive = null;
        [SerializeField]
        public UnityDebugViewerEditor activeEditor
        {
            get
            {
                if(_editorForceToActive != null)
                {
                    activeEditorType = _editorForceToActive.type;
                    _editorForceToActive = null;
                }
                return GetEditor(activeEditorType);
            }
        }

        /// <summary>
        /// dictionary cannot be serilized
        /// </summary>
        private static Dictionary<int, UnityDebugViewerEditor> _editorDic;
        private static Dictionary<int, UnityDebugViewerEditor> editorDic
        {
            get
            {
                if (_editorDic == null)
                {
                    _editorDic = new Dictionary<int, UnityDebugViewerEditor>();
                }

                return _editorDic;
            }
        }
        [SerializeField]
        private List<int> serializeKeyList = new List<int>();
        [SerializeField]
        private List<UnityDebugViewerEditor> serializeValueList = new List<UnityDebugViewerEditor>();


        public static void ForceActiveEditor(UnityDebugViewerEditorType type)
        {
            _editorForceToActive = GetEditor(type);
        }

        public static UnityDebugViewerEditor GetEditor(UnityDebugViewerEditorType type)
        {
            UnityDebugViewerEditor editor = null;
            int key = (int)type;
            if (editorDic.ContainsKey(key))
            {
                editor = editorDic[key];
            }
            else
            {
                editor = UnityDebugViewerEditor.CreateInstance(type);
                editorDic.Add(key, editor);
            }

            return editor;
        }

        public static UnityDebugViewerEditorManager GetInstance()
        {
            var manager = ScriptableObject.FindObjectOfType<UnityDebugViewerEditorManager>();
            if (manager == null)
            {
                manager = ScriptableObject.CreateInstance<UnityDebugViewerEditorManager>();
            }

            return manager;
        }

        void OnEnable()
        {
            hideFlags = HideFlags.HideAndDontSave;
        }

        
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            this.serializeKeyList.Clear();
            this.serializeValueList.Clear();

            foreach (var pair in editorDic)
            {
                this.serializeKeyList.Add(pair.Key);
                this.serializeValueList.Add(pair.Value);
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            int count = Mathf.Min(this.serializeKeyList.Count, this.serializeValueList.Count);

            editorDic.Clear();
            for (int i = 0; i < count; ++i)
            {
                editorDic.Add(this.serializeKeyList[i], this.serializeValueList[i]);
            }
        }
    }
}
