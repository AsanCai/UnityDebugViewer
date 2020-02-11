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

        private Rect logBoxControlRect;
        private int logBoxControlID;

        private Rect upperPanelRect;
        private Rect lowerPanelRect;
        private Rect resizerRecr;
        private Rect menuBarRect;

        private float sizeRatio = 0.5f;
        private bool isResizing;

        private float resizerHeight = 5f;
        private float menuBarHeight = 20f;
        private float logBoxHeight
        {
            get
            {
                return (logLineCount + 1) * EditorGUIUtility.singleLineHeight;
            }
        }

        private const string LogLineCountPref = "UNITY_DEBUG_VIEWER_WINDOW_LOG_LINE_COUNT";
        private const string CollapsePref = "UNITY_DEBUG_VIEWER_WINDOW_COLLAPSE";
        private const string ClearOnPlayPref = "UNITY_DEBUG_VIEWER_WINDOW_CLEAR_ON_PLAY";
        private const string ErrorPausePref = "UNITY_DEBUG_VIEWER_WINDOW_ERROR_PAUSE";
        private const string AutoScrollPref = "UNITY_DEBUG_VIEWER_WINDOW_AUTO_SCROLL";
        private const string ShowAnalysisPref = "UNITY_DEBUG_VIEWER_SHOW_LOG_ANALYSIS";
        private const string ShowLogPref = "UNITY_DEBUG_VIEWER_WINDOW_SHOW_LOG";
        private const string ShowWarningPref = "UNITY_DEBUG_VIEWER_WINDOW_SHOW_WARNING";
        private const string ShowErrorPref = "UNITY_DEBUG_VIEWER_WINDOW_SHOW_ERROR";

        private static int logLineCount = 1;
        private static bool collapse = false;
        private static bool clearOnPlay = false;
        private static bool errorPause = false;
        private static bool autoScroll = false;
        private static bool showlogAnalysis = false;
        private static bool showLog = false;
        private static bool showWarning = false;
        private static bool showError = false;

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

        private string pcPort = string.Empty;
        private string phonePort = string.Empty;
        private bool startForwardProcess = false;
        private string logcatTagFilterStr = "Unity";
        private bool startLogcatProcess = false;
        private int preLogNum = 0;
        private string logFilePath;
        private string searchText = string.Empty;

        private Vector2 upperPanelScrollPos;
        private Vector2 lowerPanelScroll;

        private GUIStyle resizerStyle = new GUIStyle();
        private GUIStyle logBoxStyle = new GUIStyle();
        private GUIStyle stackBoxStyle = new GUIStyle();

        [MenuItem("Window/Debug Viewer")]
        private static void OpenWindow()
        {
            UnityDebugViewerWindow window = GetWindow<UnityDebugViewerWindow>();
#if UNITY_5 || UNITY_5_3_OR_NEWER
            window.titleContent = new GUIContent("Debug Viewer");
#else
            window.title = "Debug Viewer";
#endif
        }

        [InitializeOnLoadMethod]
        private static void StartCompilingListener()
        {
            Application.logMessageReceivedThreaded -= LogMessageReceivedHandler;
            Application.logMessageReceivedThreaded += LogMessageReceivedHandler;
        }

        void IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
        {
            for (int i = 1; i <= 10; ++i)
            {
                var lineString = i == 1 ? "Line" : "Lines";
                menu.AddItem(new GUIContent(string.Format("Log Entry/{0} {1}", i, lineString)), i == logLineCount, SetLogLineCount, i);
            }
        }


        private void Awake()
        {
            /// 确保只被赋值一次
            editorManager = UnityDebugViewerEditorManager.GetInstance();
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

            UnityDebugViewerTransferUtility.disconnectToServerEvent += DisconnectToServerHandler;
#if UNITY_2017_2_OR_NEWER
            EditorApplication.playModeStateChanged += PlayModeStateChangeHandler;
#else
            EditorApplication.playmodeStateChanged += PlayModeStateChangeHandler;
#endif
        }

        private void OnDestroy()
        {
            UnityDebugViewerTransferUtility.disconnectToServerEvent -= DisconnectToServerHandler;
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
                StartCompiling();
            }
            isCompiling = EditorApplication.isCompiling;

            if(logList != null && logList.Count != this.editorManager.activeEditor.totalLogNum)
            {
                GUI.changed = true;
            }

            if (GUI.changed)
            {
                // Call Repaint on OnInspectorUpdate as it repaints the windows
                // less times as if it was OnGUI/Update
                Repaint();
            }
        }

        private void OnGUI()
        {
            DrawMenuBar();
            DrawUpperPanel();
            DrawResizer();
            DrawLowerPanel();

            ProcessEvents(Event.current);
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
                        if (this.editorManager.activeEditorType == UnityDebugViewerEditorType.Editor)
                        {
                            UnityDebugViewerWindowUtility.ClearNativeConsoleWindow();
                        }
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

                    Vector2 size = EditorStyles.toolbarPopup.CalcSize(new GUIContent(this.editorManager.activeEditorTypeStr));
                    EditorGUI.BeginChangeCheck();
                    this.editorManager.activeEditorType = (UnityDebugViewerEditorType)EditorGUILayout.EnumPopup(this.editorManager.activeEditorType, EditorStyles.toolbarPopup, GUILayout.Width(size.x));
                    if (EditorGUI.EndChangeCheck())
                    {
                        this.analysisDataTreeView = new UnityDebugViewerAnalysisDataTreeView(this.editorManager.activeEditor.analysisDataManager.root);
                        this.shouldUpdateLogFilter = true;
                    }

                    switch (this.editorManager.activeEditorType)
                    {
                        case UnityDebugViewerEditorType.Editor:
                            break;
                        case UnityDebugViewerEditorType.ADBForward:
                            GUILayout.Label(new GUIContent("PC Port:"), EditorStyles.label);
                            pcPort = GUILayout.TextField(pcPort, 5, EditorStyles.toolbarTextField, GUILayout.MinWidth(50f));
                            if (string.IsNullOrEmpty(pcPort))
                            {
                                pcPort = UnityDebugViewerADBUtility.DEFAULT_FORWARD_PC_PORT;
                            }
                            else
                            {
                                pcPort = Regex.Replace(pcPort, @"[^0-9]", "");
                            }

                            GUILayout.Label(new GUIContent("Phone Port:"), EditorStyles.label);
                            phonePort = GUILayout.TextField(phonePort, 5, EditorStyles.toolbarTextField, GUILayout.MinWidth(50f));
                            if (string.IsNullOrEmpty(phonePort))
                            {
                                phonePort = UnityDebugViewerADBUtility.DEFAULT_FORWARD_PHONE_PORT;
                            }
                            else
                            {
                                phonePort = Regex.Replace(phonePort, @"[^0-9]", "");
                            }

                            GUI.enabled = !startForwardProcess;
                            if (GUILayout.Button(new GUIContent("Start"), EditorStyles.toolbarButton))
                            {
                                StartADBForward();
                            }

                            GUI.enabled = startForwardProcess;
                            if (GUILayout.Button(new GUIContent("Stop"), EditorStyles.toolbarButton))
                            {
                                StopADBForward();
                            }

                            GUI.enabled = true;
                            break;
                        case UnityDebugViewerEditorType.ADBLogcat:

                            GUILayout.Label(new GUIContent("Tag Filter: "), EditorStyles.label);
                            this.logcatTagFilterStr = GUILayout.TextField(this.logcatTagFilterStr, EditorStyles.toolbarTextField, GUILayout.MinWidth(50f), GUILayout.MaxWidth(100f));

                            GUI.enabled = !startLogcatProcess;
                            if (GUILayout.Button(new GUIContent("Start"), EditorStyles.toolbarButton))
                            {
                                StartADBLogcat();
                            }

                            GUI.enabled = startLogcatProcess;
                            if (GUILayout.Button(new GUIContent("Stop"), EditorStyles.toolbarButton))
                            {
                                StopADBLogcat();
                            }

                            GUI.enabled = true;
                            break;
                        case UnityDebugViewerEditorType.LogFile:
                            GUILayout.Label(new GUIContent("Log File Path:"), EditorStyles.label);

                            this.logFilePath = EditorGUILayout.TextField(this.logFilePath, EditorStyles.toolbarTextField);
                            if (GUILayout.Button(new GUIContent("Browser"), EditorStyles.toolbarButton))
                            {
                                this.logFilePath = EditorUtility.OpenFilePanel("Select log file", this.logFilePath, "txt,log");
                            }
                            if (GUILayout.Button(new GUIContent("Load"), EditorStyles.toolbarButton))
                            {
                                UnityDebugViewerEditorUtility.ParseLogFile(this.logFilePath);
                            }
                            break;
                    }

                    GUILayout.FlexibleSpace();

                    string tempSearchText = UnityDebugViewerWindowUtility.CopyPasteTextField(this.searchText, UnityDebugViewerWindowConstant.toolbarSearchTextStyle, GUILayout.MinWidth(180f), GUILayout.MaxWidth(300f));
                    if (GUILayout.Button("", UnityDebugViewerWindowConstant.toolbarCancelButtonStyle))
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
                    showLog = GUILayout.Toggle(showLog, new GUIContent(logNum, UnityDebugViewerWindowConstant.infoIconSmallStyle.normal.background), EditorStyles.toolbarButton);
                    showWarning = GUILayout.Toggle(showWarning, new GUIContent(warningNum, UnityDebugViewerWindowConstant.warningIconSmallStyle.normal.background), EditorStyles.toolbarButton);
                    showError = GUILayout.Toggle(showError, new GUIContent(errorNum, UnityDebugViewerWindowConstant.errorIconSmallStyle.normal.background), EditorStyles.toolbarButton);
                    if (EditorGUI.EndChangeCheck())
                    {
                        PlayerPrefs.SetInt(ShowLogPref, showLog ? 1 : 0);
                        PlayerPrefs.SetInt(ShowWarningPref, showWarning ? 1 : 0);
                        PlayerPrefs.SetInt(ShowErrorPref, showError ? 1 : 0);

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

                            if(ShouldLogBoxDisplay(i))
                            {
                                DrawLogBox(log, logBoxRect, i % 2 == 0, i, collapse);
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

        private bool ShouldLogBoxDisplay(int logIndex, bool displayComplete = false)
        {
            float top = (logIndex + 1) * logBoxHeight;
            float bottom = logIndex * logBoxHeight;

            float displayTop = this.logBoxControlRect.y + this.upperPanelScrollPos.y + this.upperPanelRect.height;
            float displayBottom = this.logBoxControlRect.y + this.upperPanelScrollPos.y;
            if (displayComplete)
            {
                displayTop -= this.logBoxHeight;
                displayBottom += this.logBoxHeight;
            }

            if(top <= displayBottom || bottom >= displayTop)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private void MoveToSpecificLogBox(int logIndex)
        {
            if (ShouldLogBoxDisplay(logIndex, true))
            {
                return;
            }

            float top = (logIndex + 1) * logBoxHeight;
            float bottom = logIndex * logBoxHeight;

            float displayTop = this.logBoxControlRect.y + this.upperPanelScrollPos.y + this.upperPanelRect.height;
            float displayBottom = this.logBoxControlRect.y + this.upperPanelScrollPos.y;

            float topDistance = displayTop - top;
            float bottomDistance = displayBottom - bottom;
            float moveDistacne = Mathf.Abs(topDistance) > Mathf.Abs(bottomDistance) ? bottomDistance : topDistance;

            this.upperPanelScrollPos.y -= moveDistacne;
        }

        private void DrawLogBox(LogData log, Rect logBoxRect, bool isOdd, int index, bool isCollapsed = false)
        {
            GUIStyle iconStyle;
            switch (log.type)
            {
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    iconStyle = UnityDebugViewerWindowConstant.errorIconStyle;
                    break;
                case LogType.Warning:
                    iconStyle = UnityDebugViewerWindowConstant.warningIconStyle;
                    break;
                case LogType.Log:
                    iconStyle = UnityDebugViewerWindowConstant.infoIconStyle;
                    break;
                default:
                    iconStyle = null;
                    break;
            }
            if (iconStyle == null)
            {
                return;
            }

            logBoxStyle.wordWrap = true;
            logBoxStyle.clipping = TextClipping.Clip;
            logBoxStyle.padding = new RectOffset(30, 10, 5, 5);
            if (index == this.editorManager.activeEditor.selectedLogIndex)
            {
                logBoxStyle.normal.background = UnityDebugViewerWindowConstant.boxBgSelected;
            }
            else
            {
                logBoxStyle.normal.background = isOdd ? UnityDebugViewerWindowConstant.boxBgOdd : UnityDebugViewerWindowConstant.boxBgEven;
            }

            GUI.DrawTexture(logBoxRect, logBoxStyle.normal.background);

            var logBoxGUIContent = new GUIContent(log.info);
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
                var labelSize = UnityDebugViewerWindowConstant.collapsedNumLabelStyle.CalcSize(labelGUIContent);
                Rect labelRect = new Rect(position.width - labelSize.x - 20, logBoxRect.y + logBoxRect.height / 2 - labelSize.y / 2, labelSize.x, labelSize.y);

                EditorGUI.LabelField(labelRect, labelGUIContent, UnityDebugViewerWindowConstant.collapsedNumLabelStyle);
            }

            /// process event
            if (logBoxRect.Contains(Event.current.mousePosition))
            {
                EventType eventType = Event.current.GetTypeForControl(this.logBoxControlID);
                if (eventType == EventType.MouseDown)
                {
                    this.editorManager.activeEditor.selectedLogIndex = index;

                    if (Event.current.button == 0 && Event.current.clickCount == 2)
                    {
                        UnityDebugViewerWindowUtility.JumpToSource(log);
                    }
                }
                else if (eventType == EventType.MouseUp && Event.current.button == 1)
                {
                    ShowCopyMenu(log.info);
                }
                else if(eventType == EventType.KeyUp)
                {
                    bool changeSelectedLog = false;
                    if(Event.current.keyCode == KeyCode.UpArrow)
                    {
                        this.editorManager.activeEditor.selectedLogIndex--;
                        changeSelectedLog = true;
                    }
                    else if(Event.current.keyCode == KeyCode.DownArrow)
                    {
                        this.editorManager.activeEditor.selectedLogIndex++;
                        changeSelectedLog = true;
                    }

                    this.editorManager.activeEditor.selectedLogIndex = Mathf.Clamp(this.editorManager.activeEditor.selectedLogIndex, 0, this.logList.Count - 1);
                    if (changeSelectedLog)
                    {
                        MoveToSpecificLogBox(this.editorManager.activeEditor.selectedLogIndex);
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
            Rect analysisRect = new Rect(lowerPanelRect.x, titleRect.y + titleRect.height, lowerPanelRect.width, lowerPanelRect.height - titleRect.height);

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

                    string tempSearchText = UnityDebugViewerWindowUtility.CopyPasteTextField(this.analysisSearchText, UnityDebugViewerWindowConstant.toolbarSearchTextStyle, GUILayout.MinWidth(180f), GUILayout.MaxWidth(300f));
                    if (GUILayout.Button("", UnityDebugViewerWindowConstant.toolbarCancelButtonStyle))
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
                lowerPanelScroll = GUILayout.BeginScrollView(lowerPanelScroll);
                {
                    analysisDataTreeView.DrawTreeLayout();
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndArea();

            /// draw title at last to make sure title is always on the top
            analysisDataTreeView.DrawColumnTitle(titleRect);
        }

        private void DrawStackMessage()
        {
            GUILayout.BeginArea(lowerPanelRect);
            {
                lowerPanelScroll = GUILayout.BeginScrollView(lowerPanelScroll);
                {
                    var log = this.editorManager.activeEditor.selectedLog;
                    if (log != null && this.logFilter.ShouldDisplay(log))
                    {
                        string logFullMessage = string.Format("{0}\n{1}\n", log.info, log.extraInfo);
                        var logFullMessageAreaGUIContent = new GUIContent(logFullMessage);
                        var logFullMessageAreaHeight = UnityDebugViewerWindowConstant.logFullMessageAreaStyle.CalcHeight(logFullMessageAreaGUIContent, lowerPanelRect.width);
                        EditorGUILayout.SelectableLabel(logFullMessage, UnityDebugViewerWindowConstant.logFullMessageAreaStyle, GUILayout.ExpandWidth(true), GUILayout.Height(logFullMessageAreaHeight));

                        for (int i = 0; i < log.stackList.Count; i++)
                        {
                            var stack = log.stackList[i];
                            if (stack == null)
                            {
                                continue;
                            }

                            DrawStackBox(stack, i % 2 == 0, i);
                        }
                    }
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndArea();
        }

        private void DrawStackBox(LogStackData stack, bool isOdd, int index)
        {
            if (string.IsNullOrEmpty(stack.sourceContent))
            {
                stack.sourceContent = UnityDebugViewerEditorUtility.GetSourceContent(stack.filePath, stack.lineNumber);
            }

            string content = string.Format("\n{0}\n{1}", stack.fullStackMessage, stack.sourceContent);
            stackBoxStyle.wordWrap = true;
            if (this.selectedStackIndex == index)
            {
                stackBoxStyle.normal.background = UnityDebugViewerWindowConstant.boxBgSelected;
            }
            else
            {
                stackBoxStyle.normal.background = isOdd ? UnityDebugViewerWindowConstant.boxBgOdd : UnityDebugViewerWindowConstant.boxBgEven;
            }

            GUILayout.Label(new GUIContent(content), stackBoxStyle, GUILayout.ExpandWidth(true));
            Rect stackBoxRect = GUILayoutUtility.GetLastRect();

            if (stackBoxRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.type == EventType.MouseDown)
                {
                    if (Event.current.button == 0 && Event.current.clickCount == 2)
                    {
                        UnityDebugViewerWindowUtility.JumpToSource(stack);
                    }

                    this.selectedStackIndex = index;
                }
                else if (Event.current.button == 1 && Event.current.type == EventType.MouseUp)
                {
                    ShowCopyMenu(stack.fullStackMessage);
                }
            }
        }

        private void DrawResizer()
        {
            resizerRecr = new Rect(0, (position.height * sizeRatio) - resizerHeight, position.width, resizerHeight * 2);

            resizerStyle.normal.background = UnityDebugViewerWindowConstant.bgResizer;
            GUILayout.BeginArea(new Rect(resizerRecr.position + (Vector2.up * resizerHeight), new Vector2(position.width, 2)), resizerStyle);
            GUILayout.EndArea();

            EditorGUIUtility.AddCursorRect(resizerRecr, MouseCursor.ResizeVertical);
        }


        private void ProcessEvents(Event e)
        {
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0 && resizerRecr.Contains(e.mousePosition))
                    {
                        isResizing = true;
                    }
                    break;
                case EventType.MouseDrag:
                    if (isResizing)
                    {
                        sizeRatio = e.mousePosition.y / position.height;
                        sizeRatio = Mathf.Clamp(sizeRatio, 0.1f, 0.9f);
                        Repaint();
                    }
                    break;

                case EventType.Ignore:
                case EventType.MouseUp:
                    isResizing = false;
                    break;
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

        private bool CheckADBStatus(string adbPath)
        {
            if (string.IsNullOrEmpty(adbPath))
            {
                EditorUtility.DisplayDialog("Unity Debug Viewer", "Cannot find adb path", "OK");
                return false;
            }

            if (UnityDebugViewerADBUtility.CheckDevice(adbPath) == false)
            {
                EditorUtility.DisplayDialog("Unity Debug Viewer", "Cannot detect any connected devices", "OK");
                return false;
            }

            return true;
        }

        private void StartADBForward()
        {
            string adbPath = UnityDebugViewerWindowUtility.GetAdbPath();
            if(CheckADBStatus(adbPath) == false)
            {
                return;
            }

            startForwardProcess = UnityDebugViewerADBUtility.StartForwardProcess(pcPort, phonePort, adbPath);
            if (startForwardProcess)
            {
                int port = 0;
                if (int.TryParse(pcPort, out port))
                {
                    UnityDebugViewerTransferUtility.ConnectToServer("127.0.0.1", port);
                }
            }
        }

        private void DisconnectToServerHandler()
        {
            string adbPath = UnityDebugViewerWindowUtility.GetAdbPath();
            if (UnityDebugViewerADBUtility.CheckDevice(adbPath) == false)
            {
                UnityDebugViewerLogger.LogError("No devices connect, adb forward process should be restart!", UnityDebugViewerEditorType.ADBForward);

                StopADBForward();
            }
        }

        private void StopADBForward()
        {
            string adbPath = UnityDebugViewerWindowUtility.GetAdbPath();

            UnityDebugViewerADBUtility.StopForwardProcess(adbPath);
            startForwardProcess = false;

            /// will abort process, should excute at last
            UnityDebugViewerTransferUtility.Clear();
        }

        private void StartADBLogcat()
        {
            string adbPath = UnityDebugViewerWindowUtility.GetAdbPath();
            if (CheckADBStatus(adbPath) == false)
            {
                return;
            }

            startLogcatProcess = UnityDebugViewerADBUtility.StartLogcatProcess(LogcatDataHandler, logcatTagFilterStr, adbPath);
        }

        private void StopADBLogcat()
        {
            UnityDebugViewerADBUtility.StopLogCatProcess();
            startLogcatProcess = false;
        }

        private void StartCompiling()
        {
            if (startForwardProcess)
            {
                StopADBForward();
            }

            if (startLogcatProcess)
            {
                StopADBLogcat();
            }
        }


        private static void LogMessageReceivedHandler(string info, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Assert || type == LogType.Exception)
            {
                UnityDebugViewerEditorManager.ForceActiveEditor(UnityDebugViewerEditorType.Editor);
                if (errorPause)
                {
                    UnityEngine.Debug.Break();
                }
            }

            UnityDebugViewerLogger.AddEditorLog(info, stackTrace, type);
        }

        private static void SetLogLineCount(object obj)
        {
            int count = (int)obj;
            logLineCount = count;
            PlayerPrefs.SetInt(LogLineCountPref, count);
        }

        private void PlayModeStateChangeHandler(PlayModeStateChange state)
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

        private void LogcatDataHandler(object sender, DataReceivedEventArgs outputLine)
        {
            UnityDebugViewerLogger.AddLogcatLog(outputLine.Data);
        }
    }
}