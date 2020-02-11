using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UnityDebugViewer
{
    public class UnityDebugViewerAnalysisDataTreeView
    {
        private GUIContent[] _columnTitleGUIContentArray;
        public GUIContent[] columnTitleGUIContentArray
        {
            get
            {
                if (_columnTitleGUIContentArray == null)
                {
                    _columnTitleGUIContentArray = new GUIContent[]
                    {
                        new GUIContent("Total Count"),
                        new GUIContent("Log Count", UnityDebugViewerWindowConstant.infoIconStyle.normal.background),
                        new GUIContent("Waring Count", UnityDebugViewerWindowConstant.warningIconStyle.normal.background),
                        new GUIContent("Error Count", UnityDebugViewerWindowConstant.errorIconStyle.normal.background)
                    };
                }

                return _columnTitleGUIContentArray;
            }
        }

        protected const int indentScaler = 14;

        [SerializeField]
        private readonly UnityDebugViewerAnalysisDataTreeItem _root;

        private Rect _controlRect;
        private float _drawY;
        private float _height;

        [SerializeField]
        private UnityDebugViewerAnalysisDataTreeItem _selected;
        private int _controlID;

        public UnityDebugViewerAnalysisDataTreeView(UnityDebugViewerAnalysisDataTreeItem root)
        {
            _root = root;
        }

        public void DrawColumnTitle(Rect titleRect)
        {
            var titleStyle = GUI.skin.GetStyle("Wizard Box");
            titleStyle.alignment = TextAnchor.MiddleLeft;
            titleStyle.padding = new RectOffset(indentScaler, 0, 0, 0);

            var stackGUIContent = new GUIContent("Stack Message");
            EditorGUI.LabelField(titleRect, stackGUIContent, titleStyle);

            DrawColumn(columnTitleGUIContentArray, titleStyle, titleRect);
        }

        private void DrawColumn(GUIContent[] columnGUIContentArray, GUIStyle columnStyle, Rect rowRect)
        {
            columnStyle.padding = new RectOffset(0, 0, 0, 0);
            columnStyle.alignment = TextAnchor.MiddleCenter;

            var columnWidth = Mathf.Max(50, rowRect.width * 0.5f / columnGUIContentArray.Length);
            var columnBegin = Mathf.Max(rowRect.width * 0.5f, rowRect.width - columnWidth * columnGUIContentArray.Length);

            GUIContent columnGUIContent;
            Rect columnRect;
            for (int i = 0; i < columnGUIContentArray.Length; i++)
            {
                columnGUIContent = columnGUIContentArray[i];
                columnRect = new Rect(
                    columnBegin,
                    rowRect.y,
                    columnWidth,
                    rowRect.height
                    );
                EditorGUI.LabelField(columnRect, columnGUIContent, columnStyle);

                columnBegin += columnWidth;
            }
        }

        public virtual void DrawTreeLayout()
        {
            _height = 0;
            _drawY = 0;
            _root.Traverse(OnGetLayoutHeight);

            _controlRect = EditorGUILayout.GetControlRect(false, _height);
            _controlID = GUIUtility.GetControlID(FocusType.Passive, _controlRect);
            _root.Traverse(OnDrawRow);
        }

        protected virtual float GetRowHeight(UnityDebugViewerAnalysisDataTreeItem node)
        {
            return 1.5f * EditorGUIUtility.singleLineHeight;
        }

        protected virtual bool OnGetLayoutHeight(UnityDebugViewerAnalysisDataTreeItem node)
        {
            if (UnityDebugViewerAnalysisData.IsNullOrEmpty(node.Data) || node.Data.isVisible == false)
            {
                return true;
            }

            _height += GetRowHeight(node);
            return node.Data.isExpanded;
        }

        protected virtual bool OnDrawRow(UnityDebugViewerAnalysisDataTreeItem node)
        {
            if (UnityDebugViewerAnalysisData.IsNullOrEmpty(node.Data) || node.Data.isVisible == false)
            {
                return true;
            }

            float rowHeight = GetRowHeight(node);

            Rect rowRect = new Rect(0, _controlRect.y + _drawY, _controlRect.width, rowHeight);

            node.Row = (int)(_drawY / rowHeight);
            OnDrawTreeNode(rowRect, node, _selected == node, false);

            EventType eventType = Event.current.GetTypeForControl(_controlID);
            if (eventType == EventType.MouseUp && rowRect.Contains(Event.current.mousePosition))
            {
                _selected = node;

                GUI.changed = true;
                Event.current.Use();
            }

            _drawY += rowHeight;

            return node.Data.isExpanded;
        }

        protected virtual void OnDrawTreeNode(Rect rowRect, UnityDebugViewerAnalysisDataTreeItem node, bool selected, bool focus)
        {
            if (UnityDebugViewerAnalysisData.IsNullOrEmpty(node.Data) || node.Data == null)
            {
                return;
            }

            var rowStyle = new GUIStyle();
            rowStyle.alignment = TextAnchor.MiddleLeft;
            if (selected)
            {
                rowStyle.normal.background = UnityDebugViewerWindowConstant.boxBgSelected;
            }
            else
            {
                rowStyle.normal.background = node.Row % 2 == 0 ? UnityDebugViewerWindowConstant.boxBgOdd : UnityDebugViewerWindowConstant.boxBgEven;
            }

            EditorGUI.LabelField(rowRect, GUIContent.none, rowStyle);

            float rowIndent = indentScaler * node.Level;
            Rect indentRect = new Rect(rowIndent + rowRect.x, rowRect.y, rowRect.width, rowRect.height);

            if (!node.IsLeaf)
            {
                var foldOutSize = EditorStyles.foldout.CalcSize(GUIContent.none);

                var foldOutRect = new Rect(
                    indentRect.x - 12,
                    indentRect.y + rowRect.height * 0.5f - foldOutSize.y * 0.5f,
                    12,
                    indentRect.height);
                node.Data.isExpanded = EditorGUI.Foldout(foldOutRect, node.Data.isExpanded, GUIContent.none, EditorStyles.foldout);
            }

            GUIContent labelContent = new GUIContent(node.Data.ToString());
            EditorGUI.LabelField(indentRect, labelContent, rowStyle);

            var columnArray = node.Data.getColumnArray();
            if (columnArray == null)
            {
                return;
            }

            GUIContent[] columnGUIContentArray = new GUIContent[columnArray.Length];
            for(int i = 0;i < columnArray.Length; i++)
            {
                columnGUIContentArray[i] = new GUIContent(columnArray[i]);
            }
            DrawColumn(columnGUIContentArray, rowStyle, rowRect);
        }
    }
}