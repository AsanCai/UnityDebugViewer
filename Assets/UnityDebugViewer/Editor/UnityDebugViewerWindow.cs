using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

namespace UnityDebugViewer
{
    /// <summary>
    /// The frontend of UnityDebugViewer
    /// UnityDebugViewerWindow can bind multiple UnityDebugViewerEditor, but only one of them can be actived at a time
    /// </summary>
    public class UnityDebugViewerWindow : EditorWindow, IHasCustomMenu
    {
        private bool isPlaying = false;
        private bool isCompiling = false;
        private int selectedStackIndex = -1;
        private List<Rect> stackRectList = new List<Rect>();

        private int logBoxControlID;
        private int stackBoxControlID;
        private int resizerControlID;

        private Rect logBoxControlRect;
        private Rect upperPanelRect;
        private Rect lowerPanelRect;
        private Rect resizerRect;
        private Rect menuBarRect;

        private float sizeRatio = 0.5f;
        private bool isResizing;

        private float resizerHeight = 5f;
        private float menuBarHeight = 20f;
        private float splitHeight = 3f;
        private float logFullMessageAreaHeight;
        private float logBoxHeight
        {
            get
            {
                return (logLineCount + 2) * EditorGUIUtility.singleLineHeight;
            }
        }

        private const string LogLineCountPref = "UNITY_DEBUG_VIEWER_WINDOW_LOG_LINE_COUNT";
        private const string CollapsePref = "UNITY_DEBUG_VIEWER_WINDOW_COLLAPSE";
        private const string ClearOnPlayPref = "UNITY_DEBUG_VIEWER_WINDOW_CLEAR_ON_PLAY";
        private const string ErrorPausePref = "UNITY_DEBUG_VIEWER_WINDOW_ERROR_PAUSE";
        private const string AutoScrollPref = "UNITY_DEBUG_VIEWER_WINDOW_AUTO_SCROLL";
        private const string ShowAnalysisPref = "UNITY_DEBUG_VIEWER_SHOW_LOG_ANALYSIS";
        private const string ShowTimePref = "UNITY_DEBUG_VIEWER_WINDOW_SHOW_TIME";
        private const string ShowLogPref = "UNITY_DEBUG_VIEWER_WINDOW_SHOW_LOG";
        private const string ShowWarningPref = "UNITY_DEBUG_VIEWER_WINDOW_SHOW_WARNING";
        private const string ShowErrorPref = "UNITY_DEBUG_VIEWER_WINDOW_SHOW_ERROR";

        private static int logLineCount = 1;
        public static bool collapse = false;
        public static bool clearOnPlay = false;
        public static bool errorPause = false;
        public static bool autoScroll = false;
        public static bool showlogAnalysis = false;
        public static bool showTime = false;
        public static bool showLog = false;
        public static bool showWarning = false;
        public static bool showError = false;

        private List<LogData> logList = null;

        [SerializeField]
        private UnityDebugViewerAnalysisDataTreeView analysisDataTreeView;

        [SerializeField]
        private UnityDebugViewerEditorManager editorManager;
        [SerializeField]
        private LogFilter logFilter;
        private bool shouldUpdateLogFilter;

        private AnalysisDataSortType analysisDataSortType = AnalysisDataSortType.TotalCount;
        private string analysisSearchText = string.Empty;

        private int preLogNum = 0;
        private string searchText = string.Empty;

        private Vector2 upperPanelScrollPos;
        private Vector2 stackPanelScrollPos;
        private Vector2 analysisPanelScrollPos;

        private GUIStyle resizerStyle = new GUIStyle();
        private GUIStyle logBoxStyle;
        private GUIStyle stackBoxStyle;

        [MenuItem("Window/Debug Viewer", false, 100)]
        private static void OpenWindow()
        {
            UnityDebugViewerWindow window = GetWindow<UnityDebugViewerWindow>();
#if UNITY_5 || UNITY_5_3_OR_NEWER
            window.titleContent = new GUIContent("Debug Viewer");
#else
            window.title = "Debug Viewer";
#endif
        }

        void IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
        {
            for (int i = 1; i <= 10; ++i)
            {
                var lineString = i == 1 ? "Line" : "Lines";
                menu.AddItem(new GUIContent(string.Format("Log Entry/{0} {1}", i, lineString)), i == logLineCount, SetLogLineCount, i);
            }

            menu.AddItem(new GUIContent("About UnityDebugViewer"), false, ClickHelpBtn, null);
        }

        private static void SetLogLineCount(object obj)
        {
            int count = (int)obj;
            logLineCount = count;
            PlayerPrefs.SetInt(LogLineCountPref, count);
        }

        private static void ClickHelpBtn(object obj)
        {
            Application.OpenURL("https://github.com/AsanCai/UnityDebugViewer");
        }

        private void Awake()
        {
            /// 确保只被赋值一次，否则序列化时log数据将会丢失
            editorManager = new UnityDebugViewerEditorManager();
        }

        /// <summary>
        /// 序列化结束时会被调用,因此静态数据的赋值需要放在这里执行
        /// </summary>
        private void OnEnable()
        {
            logLineCount = PlayerPrefs.GetInt(LogLineCountPref, 1);
            collapse = PlayerPrefs.GetInt(CollapsePref, 0) == 1;
            clearOnPlay = PlayerPrefs.GetInt(ClearOnPlayPref, 0) == 1;
            errorPause = PlayerPrefs.GetInt(ErrorPausePref, 0) == 1;
            autoScroll = PlayerPrefs.GetInt(AutoScrollPref, 0) == 1;
            showlogAnalysis = PlayerPrefs.GetInt(ShowAnalysisPref, 0) == 1;

            showTime = PlayerPrefs.GetInt(ShowTimePref, 0) == 1;
            showLog = PlayerPrefs.GetInt(ShowLogPref, 0) == 1;
            showWarning = PlayerPrefs.GetInt(ShowWarningPref, 0) == 1;
            showError = PlayerPrefs.GetInt(ShowErrorPref, 0) == 1;

            logFilter.showLog = showLog;
            logFilter.showWarning = showWarning;
            logFilter.showError = showError;
            logFilter.collapse = collapse;
            logFilter.searchText = searchText;
            shouldUpdateLogFilter = true;

            analysisDataTreeView = new UnityDebugViewerAnalysisDataTreeView(this.editorManager.activeEditor.analysisDataManager.root);

#if UNITY_2017_2_OR_NEWER
            EditorApplication.playModeStateChanged += PlayModeStateChangeHandler;
#else
            EditorApplication.playmodeStateChanged += PlayModeStateChangeHandler;
#endif
        }

        private void OnDestroy()
        {
#if UNITY_2017_2_OR_NEWER
            EditorApplication.playModeStateChanged -= PlayModeStateChangeHandler;
#else
            EditorApplication.playmodeStateChanged -= PlayModeStateChangeHandler;
#endif
        }

        private void OnInspectorUpdate()
        {
            if(isCompiling == false && EditorApplication.isCompiling)
            {
                this.editorManager.activeEditor.StartCompiling();
            }
            isCompiling = EditorApplication.isCompiling;

            if(logList != null && logList.Count != this.editorManager.activeEditor.totalLogNum)
            {
                GUI.changed = true;
            }

            // Call Repaint on OnInspectorUpdate as it repaints the windows
            // less times as if it was OnGUI/Update
            Repaint();
        }

        private void OnGUI()
        {
            DrawMenuBar();
            DrawUpperPanel();
            DrawResizer();
            DrawLowerPanel();
        }

        private void DrawMenuBar()
        {
            menuBarRect = new Rect(0, 0, position.width, menuBarHeight);

            GUILayout.BeginArea(menuBarRect, EditorStyles.toolbar);
            {
                GUILayout.BeginHorizontal();
                {
                    if(GUILayout.Button(new GUIContent("Clear"), EditorStyles.toolbarButton, GUILayout.Width(40)))
                    {
                        this.editorManager.activeEditor.Clear();
                    }

                    GUILayout.Space(5);

                    EditorGUI.BeginChangeCheck();
                    collapse = GUILayout.Toggle(collapse, new GUIContent("Collapse"), EditorStyles.toolbarButton);
                    if (EditorGUI.EndChangeCheck())
                    {
                        this.logFilter.collapse = collapse;
                        this.shouldUpdateLogFilter = true;
                        PlayerPrefs.SetInt(CollapsePref, collapse ? 1 : 0);
                    }

                    EditorGUI.BeginChangeCheck();
                    clearOnPlay = GUILayout.Toggle(clearOnPlay, new GUIContent("Clear On Play"), EditorStyles.toolbarButton);
                    errorPause = GUILayout.Toggle(errorPause, new GUIContent("Error Pause"), EditorStyles.toolbarButton);
                    autoScroll = GUILayout.Toggle(autoScroll, new GUIContent("Auto Scroll"), EditorStyles.toolbarButton);

                    GUILayout.Space(5);

                    showlogAnalysis = GUILayout.Toggle(showlogAnalysis, new GUIContent("Show Analysis"), EditorStyles.toolbarButton);

                    if (EditorGUI.EndChangeCheck())
                    {
                        PlayerPrefs.SetInt(ClearOnPlayPref, clearOnPlay ? 1 : 0);
                        PlayerPrefs.SetInt(ErrorPausePref, errorPause ? 1 : 0);
                        PlayerPrefs.SetInt(AutoScrollPref, autoScroll ? 1 : 0);
                        PlayerPrefs.SetInt(ShowAnalysisPref, showlogAnalysis ? 1 : 0);
                    }

                    GUILayout.Space(5);

                    Vector2 size = EditorStyles.toolbarPopup.CalcSize(new GUIContent(this.editorManager.activeMode));
                    EditorGUI.BeginChangeCheck();
                    this.editorManager.activeModeIndex = EditorGUILayout.Popup(this.editorManager.activeModeIndex, this.editorManager.modeArray, EditorStyles.toolbarPopup, GUILayout.Width(size.x));
                    if (EditorGUI.EndChangeCheck())
                    {
                        this.analysisDataTreeView = new UnityDebugViewerAnalysisDataTreeView(this.editorManager.activeEditor.analysisDataManager.root);
                        this.shouldUpdateLogFilter = true;
                    }

                    this.editorManager.activeEditor.OnGUI();

                    GUILayout.FlexibleSpace();

                    string tempSearchText = UnityDebugViewerWindowUtility.CopyPasteTextField(this.searchText, UnityDebugViewerWindowStyleUtility.toolbarSearchTextStyle, GUILayout.MinWidth(180f), GUILayout.MaxWidth(300f));
                    if (GUILayout.Button("", UnityDebugViewerWindowStyleUtility.toolbarCancelButtonStyle))
                    {
                        tempSearchText = string.Empty;
                    }
                    if (tempSearchText.Equals(this.searchText) == false)
                    {
                        this.searchText = tempSearchText;
                        this.logFilter.searchText = this.searchText;
                        this.shouldUpdateLogFilter = true;
                    }

                    string logNum = this.editorManager.activeEditor.logNum.ToString();
                    string warningNum = this.editorManager.activeEditor.warningNum.ToString();
                    string errorNum = this.editorManager.activeEditor.errorNum.ToString();

                    EditorGUI.BeginChangeCheck();
                    showTime = GUILayout.Toggle(showTime, new GUIContent("Time"), EditorStyles.toolbarButton);
                    showLog = GUILayout.Toggle(showLog, new GUIContent(logNum, UnityDebugViewerWindowStyleUtility.infoIconSmallTexture), EditorStyles.toolbarButton);
                    showWarning = GUILayout.Toggle(showWarning, new GUIContent(warningNum, UnityDebugViewerWindowStyleUtility.warningIconSmallTexture), EditorStyles.toolbarButton);
                    showError = GUILayout.Toggle(showError, new GUIContent(errorNum, UnityDebugViewerWindowStyleUtility.errorIconSmallTexture), EditorStyles.toolbarButton);
                    if (EditorGUI.EndChangeCheck())
                    {
                        PlayerPrefs.SetInt(ShowTimePref, showTime ? 1 : 0);
                        PlayerPrefs.SetInt(ShowLogPref, showLog ? 1 : 0);
                        PlayerPrefs.SetInt(ShowWarningPref, showWarning ? 1 : 0);
                        PlayerPrefs.SetInt(ShowErrorPref, showError ? 1 : 0);

                        this.logFilter.showTime = showTime;
                        this.logFilter.showLog = showLog;
                        this.logFilter.showWarning = showWarning;
                        this.logFilter.showError = showError;
                        this.shouldUpdateLogFilter = true;
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndArea();
        }

        private void DrawUpperPanel()
        {
            upperPanelRect = new Rect(0, menuBarHeight, position.width, (position.height * sizeRatio) - menuBarHeight);

            GUILayout.BeginArea(upperPanelRect);
            {
                upperPanelScrollPos = GUILayout.BeginScrollView(upperPanelScrollPos);
                {
                    this.logList = this.editorManager.activeEditor.GetFilteredLogList(this.logFilter, this.shouldUpdateLogFilter);
                    this.shouldUpdateLogFilter = false;

                    if (this.logList != null)
                    {
                        this.logBoxControlRect = EditorGUILayout.GetControlRect(false, logList.Count * logBoxHeight);
                        this.logBoxControlID = GUIUtility.GetControlID(FocusType.Passive, logBoxControlRect);

                        for (int i = 0; i < this.logList.Count; i++)
                        {
                            var log = this.logList[i];
                            if (log == null)
                            {
                                continue;
                            }

                            Rect logBoxRect = new Rect(
                                this.upperPanelRect.x, 
                                this.logBoxControlRect.y + i * this.logBoxHeight,
                                this.upperPanelRect.width, 
                                this.logBoxHeight
                                );

                            if(ShouldLogBoxShow(i))
                            {
                                DrawLogBox(log, logBoxRect, i, collapse);
                            }
                        }

                        /// if "Auto Scroll" is selected, then force scroll to the bottom when new log is added
                        if (this.preLogNum != logList.Count && autoScroll)
                        {
                            upperPanelScrollPos.y = Mathf.Infinity;
                        }

                        this.preLogNum = logList.Count;
                    }
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndArea();
        }

        private bool ShouldLogBoxShow(int logIndex)
        {
            float rectTop = (logIndex + 1) * logBoxHeight;
            float rectBottom = logIndex * logBoxHeight;

            float showTop = this.logBoxControlRect.y + this.upperPanelScrollPos.y + this.upperPanelRect.height;
            float showBottom = this.logBoxControlRect.y + this.upperPanelScrollPos.y;

            return UnityDebugViewerWindowUtility.ShouldRectShow(showTop, showBottom, rectTop, rectBottom, false);
        }

        private void MoveToSpecificLogBox(int logIndex)
        {
            float rectTop = (logIndex + 1) * logBoxHeight;
            float rectBottom = logIndex * logBoxHeight;

            float showTop = this.logBoxControlRect.y + this.upperPanelScrollPos.y + this.upperPanelRect.height;
            float showBottom = this.logBoxControlRect.y + this.upperPanelScrollPos.y;

            UnityDebugViewerWindowUtility.MoveToSpecificRect(showTop, showBottom, rectTop, rectBottom, ref this.upperPanelScrollPos);
        }

        private void DrawLogBox(LogData log, Rect logBoxRect, int index, bool isCollapsed = false)
        {
            GUIStyle iconStyle;
            switch (log.type)
            {
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    iconStyle = UnityDebugViewerWindowStyleUtility.errorIconStyle;
                    break;
                case LogType.Warning:
                    iconStyle = UnityDebugViewerWindowStyleUtility.warningIconStyle;
                    break;
                case LogType.Log:
                    iconStyle = UnityDebugViewerWindowStyleUtility.infoIconStyle;
                    break;
                default:
                    iconStyle = null;
                    break;
            }
            if (iconStyle == null)
            {
                return;
            }

            
            if (index == this.editorManager.activeEditor.selectedLogIndex)
            {
                logBoxStyle = UnityDebugViewerWindowUtility.activeControlID == this.logBoxControlID ? logBoxStyle = UnityDebugViewerWindowStyleUtility.selectedLogBoxStyle : UnityDebugViewerWindowStyleUtility.inactiveLogBoxStyle;
            }
            else
            {
                logBoxStyle = index % 2 == 0 ? UnityDebugViewerWindowStyleUtility.oddLogBoxtyle : UnityDebugViewerWindowStyleUtility.evenLogBoxtyle;
            }
            GUI.DrawTexture(logBoxRect, logBoxStyle.normal.background);


            string logContent = log.GetContent(showTime);
            var logBoxGUIContent = new GUIContent(logContent);
            var logContentHeight = logBoxStyle.CalcHeight(logBoxGUIContent, logBoxRect.width);
            logBoxStyle.alignment = logContentHeight > logBoxHeight ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft;
            EditorGUI.LabelField(logBoxRect, logBoxGUIContent, logBoxStyle);

            var logBoxMidHeight = logBoxRect.y + logBoxRect.height / 2;
            var iconSize = iconStyle.CalcSize(GUIContent.none);
            var iconRect = new Rect(5, logBoxMidHeight - iconSize.y / 2, iconSize.x, iconSize.y);
            EditorGUI.LabelField(iconRect, GUIContent.none, iconStyle);

            if (collapse)
            {
                /// make sure the number label display in a fixed relative position of the window
                int num = this.editorManager.activeEditor.GetLogNum(log);
                GUIContent labelGUIContent = new GUIContent(num.ToString());
                var labelSize = UnityDebugViewerWindowStyleUtility.collapsedNumLabelStyle.CalcSize(labelGUIContent);
                Rect labelRect = new Rect(position.width - labelSize.x - 20, logBoxRect.y + logBoxRect.height / 2 - labelSize.y / 2, labelSize.x, labelSize.y);

                EditorGUI.LabelField(labelRect, labelGUIContent, UnityDebugViewerWindowStyleUtility.collapsedNumLabelStyle);
            }

            EventType eventType = Event.current.GetTypeForControl(this.logBoxControlID);
#if UNITY_5 || UNITY_5_3_OR_NEWER
            if (eventType == EventType.MouseDown)
#else
            if (eventType == EventType.mouseDown)
#endif
            {
                /// process event
                if (logBoxRect.Contains(Event.current.mousePosition))
                {
                    this.editorManager.activeEditor.selectedLogIndex = index;

                    if (Event.current.button == 0 && Event.current.clickCount == 2)
                    {
                        UnityDebugViewerWindowUtility.JumpToSource(log);
                    }

                    UnityDebugViewerWindowUtility.activeControlID = this.logBoxControlID;
                    Event.current.Use();
                }
                
                if(Event.current.button == 2)
                {
                    MoveToSpecificLogBox(this.editorManager.activeEditor.selectedLogIndex);

                    Event.current.Use();
                }
            }
#if UNITY_5 || UNITY_5_3_OR_NEWER
            else if (eventType == EventType.MouseUp)
#else
            else if (eventType == EventType.mouseUp)
#endif
            {
                if (logBoxRect.Contains(Event.current.mousePosition) && Event.current.button == 1)
                {
                    ShowCopyMenu(logContent);

                    UnityDebugViewerWindowUtility.activeControlID = this.logBoxControlID;
                    Event.current.Use();
                }
            }
#if UNITY_5 || UNITY_5_3_OR_NEWER
            else if (eventType == EventType.KeyUp)
#else
            else if (eventType == EventType.keyUp)
#endif
            {
                if(this.logBoxControlID == UnityDebugViewerWindowUtility.activeControlID)
                {
                    bool changeSelectedLog = false;
                    int selectedIndex = this.editorManager.activeEditor.selectedLogIndex;
                    if (Event.current.keyCode == KeyCode.UpArrow)
                    {
                        selectedIndex--;
                        changeSelectedLog = true;
                    }
                    else if (Event.current.keyCode == KeyCode.DownArrow)
                    {
                        selectedIndex++;
                        changeSelectedLog = true;
                    }

                    if (changeSelectedLog)
                    {
                        this.editorManager.activeEditor.selectedLogIndex = Mathf.Clamp(selectedIndex, 0, this.logList.Count - 1);

                        Event.current.Use();
                    }
                }
            }
        }

        private void DrawLowerPanel()
        {
            lowerPanelRect = new Rect(0, (position.height * sizeRatio) + resizerHeight, position.width, (position.height * (1 - sizeRatio)) - resizerHeight);

            if (showlogAnalysis)
            {
                DrawAnalysisMessage();
            }
            else
            {
                DrawStackMessage();
            }
        }

        private void DrawAnalysisMessage()
        {
            if (analysisDataTreeView == null)
            {
                return;
            }

            Rect analysisMenuBarRect = new Rect(lowerPanelRect.x, lowerPanelRect.y, lowerPanelRect.width, menuBarHeight);
            Rect titleRect = new Rect(lowerPanelRect.x, analysisMenuBarRect.y + analysisMenuBarRect.height, lowerPanelRect.width, 1.5f * EditorGUIUtility.singleLineHeight);
            Rect analysisRect = new Rect(lowerPanelRect.x, titleRect.y + titleRect.height, lowerPanelRect.width, lowerPanelRect.height - titleRect.height - analysisMenuBarRect.height);

            GUILayout.BeginArea(analysisMenuBarRect, EditorStyles.toolbar);
            {
                GUILayout.BeginHorizontal();
                {
                    EditorGUI.BeginChangeCheck();
                    GUILayout.Label(new GUIContent("Sort Type: "), EditorStyles.toolbarButton);
                    analysisDataSortType = (AnalysisDataSortType)EditorGUILayout.EnumPopup(analysisDataSortType, EditorStyles.toolbarPopup);
                    if (EditorGUI.EndChangeCheck())
                    {
                        this.editorManager.activeEditor.analysisDataManager.Sort(analysisDataSortType);
                    }

                    GUILayout.FlexibleSpace();

                    string tempSearchText = UnityDebugViewerWindowUtility.CopyPasteTextField(this.analysisSearchText, UnityDebugViewerWindowStyleUtility.toolbarSearchTextStyle, GUILayout.MinWidth(180f), GUILayout.MaxWidth(300f));
                    if (GUILayout.Button("", UnityDebugViewerWindowStyleUtility.toolbarCancelButtonStyle))
                    {
                        tempSearchText = string.Empty;
                    }
                    if (tempSearchText.Equals(this.analysisSearchText) == false)
                    {
                        this.analysisSearchText = tempSearchText;
                        this.editorManager.activeEditor.analysisDataManager.Search(this.analysisSearchText);
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndArea();

            GUILayout.BeginArea(analysisRect);
            {
                this.analysisPanelScrollPos = GUILayout.BeginScrollView(this.analysisPanelScrollPos);
                {
                    analysisDataTreeView.DrawTreeLayout(analysisRect, ref this.analysisPanelScrollPos);
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndArea();

            /// draw title at last to make sure title is always on the top
            analysisDataTreeView.DrawColumnTitle(titleRect);
        }

        private void DrawStackMessage()
        {
            GUILayout.BeginArea(this.lowerPanelRect);
            {
                this.stackBoxControlID = GUIUtility.GetControlID(FocusType.Passive, lowerPanelRect);

                this.stackPanelScrollPos = GUILayout.BeginScrollView(this.stackPanelScrollPos);
                {
                    var log = this.editorManager.activeEditor.selectedLog;
                    if (log != null && this.logFilter.ShouldDisplay(log))
                    {
                        string logFullMessage = string.Format("{0}\n{1}\n", log.info, log.extraInfo);
                        var logFullMessageAreaGUIContent = new GUIContent(logFullMessage);
                        this.logFullMessageAreaHeight = UnityDebugViewerWindowStyleUtility.logFullMessageAreaStyle.CalcHeight(logFullMessageAreaGUIContent, this.lowerPanelRect.width);

                        EditorGUILayout.SelectableLabel(logFullMessage, UnityDebugViewerWindowStyleUtility.logFullMessageAreaStyle, GUILayout.ExpandWidth(true), GUILayout.Height(this.logFullMessageAreaHeight));

                        GUILayout.Label(GUIContent.none, GUI.skin.GetStyle("Wizard Box"), GUILayout.Height(this.splitHeight), GUILayout.ExpandWidth(true));

                        if(this.stackRectList == null)
                        {
                            this.stackRectList = new List<Rect>();
                        }
                        if (this.stackRectList.Count < log.stackList.Count)
                        {
                            this.stackRectList.Clear();
                            for (int i = 0; i < log.stackList.Count; i++)
                            {
                                this.stackRectList.Add(Rect.zero);
                            }
                        }


                        Vector2 startPos = new Vector2(0, this.logFullMessageAreaHeight + this.splitHeight);

                        for (int i = 0; i < log.stackList.Count; i++)
                        {
                            var stack = log.stackList[i];
                            if (stack == null)
                            {
                                continue;
                            }

                            DrawStackBox(stack, i, ref startPos);
                        }

                        EventType eventType = Event.current.GetTypeForControl(this.stackBoxControlID);
#if UNITY_5 || UNITY_5_3_OR_NEWER
                        if (eventType == EventType.KeyUp)
#else
                        if (eventType == EventType.keyUp)
#endif
                        {
                            if(this.stackBoxControlID == UnityDebugViewerWindowUtility.activeControlID)
                            {
                                bool changeSeletedStack = false;

                                if (Event.current.keyCode == KeyCode.UpArrow)
                                {
                                    this.selectedStackIndex--;
                                    if (this.selectedStackIndex < 0)
                                    {
                                        this.selectedStackIndex = 0;
                                        this.stackPanelScrollPos.y = 0;
                                    }
                                    changeSeletedStack = true;
                                }
                                else if (Event.current.keyCode == KeyCode.DownArrow)
                                {
                                    this.selectedStackIndex++;
                                    if (this.selectedStackIndex > log.stackList.Count - 1)
                                    {
                                        this.selectedStackIndex = log.stackList.Count - 1;
                                    }
                                    changeSeletedStack = true;
                                }

                                if (changeSeletedStack)
                                {
                                    float showRectTop = this.stackPanelScrollPos.y + this.lowerPanelRect.height;
                                    float showRectBottom = this.stackPanelScrollPos.y;
                                    float rectTop = stackRectList[this.selectedStackIndex].y + stackRectList[this.selectedStackIndex].height;
                                    float rectBottom = stackRectList[this.selectedStackIndex].y;

                                    UnityDebugViewerWindowUtility.MoveToSpecificRect(showRectTop, showRectBottom, rectTop, rectBottom, ref this.stackPanelScrollPos);

                                    GUI.changed = true;
                                    Event.current.Use();
                                }
                            }
                        }
                    }
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndArea();
        }


        private void DrawStackBox(LogStackData stack, int index, ref Vector2 drawPos)
        {
            if (string.IsNullOrEmpty(stack.sourceContent))
            {
                stack.sourceContent = UnityDebugViewerEditorUtility.GetSourceContent(stack.filePath, stack.lineNumber);
            }

            var stackBoxGUIContent = new GUIContent(string.Format("\n{0}\n{1}", stack.fullStackMessage, stack.sourceContent));

            if (this.selectedStackIndex == index)
            {
                stackBoxStyle = UnityDebugViewerWindowUtility.activeControlID == this.stackBoxControlID ? stackBoxStyle = UnityDebugViewerWindowStyleUtility.selectedStackBoxStyle : UnityDebugViewerWindowStyleUtility.inactiveStackBoxStyle;
            }
            else
            {
                stackBoxStyle = index % 2 == 0 ? UnityDebugViewerWindowStyleUtility.oddStackBoxStyle : UnityDebugViewerWindowStyleUtility.evenStackBoxStyle;
            }

            var height = stackBoxStyle.CalcHeight(stackBoxGUIContent, this.lowerPanelRect.width);
            var stackBoxRect = new Rect(drawPos.x, drawPos.y, this.lowerPanelRect.width, height);
            EditorGUI.LabelField(stackBoxRect, stackBoxGUIContent, stackBoxStyle);
            EditorGUILayout.GetControlRect(false, height);

            this.stackRectList[index] = stackBoxRect;
            drawPos.y += height;

            if (stackBoxRect.Contains(Event.current.mousePosition))
            {
                EventType eventType = Event.current.GetTypeForControl(this.stackBoxControlID);
#if UNITY_5 || UNITY_5_3_OR_NEWER
                if (eventType == EventType.MouseDown)
#else
                if (eventType == EventType.mouseDown)
#endif
                {
                    if (Event.current.button == 0 && Event.current.clickCount == 2)
                    {
                        UnityDebugViewerWindowUtility.JumpToSource(stack);
                    }

                    this.selectedStackIndex = index;

                    UnityDebugViewerWindowUtility.activeControlID = this.stackBoxControlID;
                    Event.current.Use();
                }
#if UNITY_5 || UNITY_5_3_OR_NEWER
                if (eventType == EventType.MouseUp && Event.current.button == 1)
#else
                if (eventType == EventType.mouseUp && Event.current.button == 1)
#endif
                {
                    ShowCopyMenu(stack.fullStackMessage);

                    UnityDebugViewerWindowUtility.activeControlID = this.stackBoxControlID;
                    Event.current.Use();
                }
            }
        }

        private void DrawResizer()
        {
            resizerRect = new Rect(0, (position.height * sizeRatio) - resizerHeight, position.width, resizerHeight * 2);

            resizerStyle.normal.background = UnityDebugViewerWindowStyleUtility.bgResizer;
            GUILayout.BeginArea(new Rect(resizerRect.position + (Vector2.up * resizerHeight), new Vector2(position.width, 2)), resizerStyle);
            GUILayout.EndArea();

            EditorGUIUtility.AddCursorRect(resizerRect, MouseCursor.ResizeVertical);

            this.resizerControlID = GUIUtility.GetControlID(FocusType.Passive, resizerRect);
            EventType eventType = Event.current.GetTypeForControl(this.resizerControlID);
#if UNITY_5 || UNITY_5_3_OR_NEWER
            if (eventType == EventType.MouseDown)
#else
            if (eventType == EventType.mouseDown)
#endif
            {
                if (Event.current.button == 0 && resizerRect.Contains(Event.current.mousePosition))
                {
                    isResizing = true;
                    Event.current.Use();
                }
            }
#if UNITY_5 || UNITY_5_3_OR_NEWER
            if (eventType == EventType.MouseDrag)
#else
            if (eventType == EventType.mouseDrag)
#endif
            {
                if (isResizing)
                {
                    sizeRatio = Event.current.mousePosition.y / position.height;
                    sizeRatio = Mathf.Clamp(sizeRatio, 0.1f, 0.9f);
                    Repaint();

                    Event.current.Use();
                }
            }
#if UNITY_5 || UNITY_5_3_OR_NEWER
            if (eventType == EventType.Ignore || eventType == EventType.MouseUp)
#else
            if (eventType == EventType.ignore || eventType == EventType.mouseUp)
#endif
            {
                if (isResizing)
                {
                    isResizing = false;

                    Event.current.Use();
                }
            }
        }

        private void ShowCopyMenu(object data)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Copy"), false, CopyData, data);
            menu.ShowAsContext();
        }

        private void CopyData(object data)
        {
            string str = data as string;
            if(string.IsNullOrEmpty(str))
            {
                return;
            }

            EditorGUIUtility.systemCopyBuffer = str;
        }

#if UNITY_2017_2_OR_NEWER
        private void PlayModeStateChangeHandler(PlayModeStateChange state)
#else
        private void PlayModeStateChangeHandler()
#endif
        {
            if (!isPlaying && EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (clearOnPlay)
                {
                    this.editorManager.activeEditor.Clear();
                }
            }

            isPlaying = EditorApplication.isPlayingOrWillChangePlaymode;
        }
    }
}