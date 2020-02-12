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
        private UnityDebugViewerAnalysisDataTreeItem _selectedNode;
        private int _selectedRow;
        private bool _changeSelectedRow;
        private Rect _panelRect;
        private Vector2 _scrollPos;
        private int _controlID;

        public UnityDebugViewerAnalysisDataTreeView(UnityDebugViewerAnalysisDataTreeItem root)
        {
            _root = root;
            _selectedRow = 0;
            _changeSelectedRow = false;
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

        public virtual void DrawTreeLayout(Rect panelRect, ref Vector2 scrollPos)
        {
            _panelRect = panelRect;
            _scrollPos = scrollPos;

            _height = 0;
            _drawY = 0;
            _root.Traverse(OnGetLayoutHeight);

            _controlRect = EditorGUILayout.GetControlRect(false, _height);
            _controlID = GUIUtility.GetControlID(FocusType.Passive, _controlRect);
            _root.Traverse(OnDrawRow);

            scrollPos = _scrollPos;
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
            if (_changeSelectedRow && _selectedRow == node.Row)
            {
                _selectedNode = node;
                _changeSelectedRow = false;
                float showTop = _controlRect.y + _scrollPos.y + _panelRect.height;
                float showBottom = _controlRect.y + _scrollPos.y;
                float rectTop = rowRect.y + rowRect.height;
                float rectBottom = rowRect.y;
                UnityDebugViewerWindowUtility.MoveToSpecificRect(showTop, showBottom, rectTop, rectBottom, ref _scrollPos);
            }

            OnDrawTreeNode(rowRect, node, _selectedNode == node, false);

            if (rowRect.Contains(Event.current.mousePosition))
            {
                EventType eventType = Event.current.GetTypeForControl(_controlID);
                if (eventType == EventType.MouseUp)
                {
                    _selectedNode = node;
                    _selectedRow = node.Row;

                    GUI.changed = true;
                    Event.current.Use();
                }
                else if(eventType == EventType.KeyUp)
                {
                    if(Event.current.keyCode == KeyCode.UpArrow)
                    {
                        _selectedRow = _selectedNode.Row - 1;
                        if(_selectedRow < 0)
                        {
                            _selectedRow = 0;
                        }
                        _changeSelectedRow = true;
                    }
                    else if(Event.current.keyCode == KeyCode.DownArrow)
                    {
                        _selectedRow = _selectedNode.Row + 1;
                        int maxRow = (int)(_controlRect.height / rowHeight) - 1;
                        if (_selectedRow > maxRow)
                        {
                            _selectedRow = maxRow;
                        }
                        _changeSelectedRow = true;
                    }

                    if (_changeSelectedRow)
                    {
                        GUI.changed = true;
                        Event.current.Use();
                    }
                }
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