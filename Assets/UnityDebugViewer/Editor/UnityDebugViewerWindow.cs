using System.Diagnostics;
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
        private double lastClickTime = 0;
        private const double DOUBLE_CLICK_INTERVAL = 0.3;
        private int selectedLogIndex;
        private int selectedStackIndex;

        private bool isPlaying = false;
        private bool isCompiling = false;

        private Rect upperPanelRect;
        private Rect lowerPanelRect;
        private Rect resizerRecr;
        private Rect menuBarRect;

        private float sizeRatio = 0.5f;
        private bool isResizing;

        private float resizerHeight = 5f;
        private float splitHeight = 2f;
        private float menuBarHeight = 20f;

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
        private bool onlyShowUnityLog = true;
        private bool startLogcatProcess = false;
        private int preLogNum = 0;
        private string logFilePath;
        private string searchText = string.Empty;

        private Vector2 upperPanelScroll;
        private Vector2 lowerPanelScroll;

        private GUIStyle resizerStyle = new GUIStyle();
        private GUIStyle logBoxStyle = new GUIStyle();
        private GUIStyle stackBoxStyle = new GUIStyle();
        private GUIStyle textAreaStyle = new GUIStyle();

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
                            onlyShowUnityLog = GUILayout.Toggle(onlyShowUnityLog, new GUIContent("Only Unity"), EditorStyles.toolbarButton);

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
                upperPanelScroll = GUILayout.BeginScrollView(upperPanelScroll);
                {
                    var logList = this.editorManager.activeEditor.GetFilteredLogList(this.logFilter, this.shouldUpdateLogFilter);
                    this.shouldUpdateLogFilter = false;

                    if (logList != null)
                    {
                        for (int i = 0; i < logList.Count; i++)
                        {
                            var log = logList[i];
                            if(log == null)
                            {
                                continue;
                            }

                            /// update selected state
                            if (DrawLogBox(log, i % 2 == 0, i, collapse))
                            {
                                if(Event.current.button == 0)
                                {
                                    this.editorManager.activeEditor.selectedLogIndex = i;

                                    /// try to open source file of the log
                                    if (this.selectedLogIndex == i && Event.current.button == 0)
                                    {
                                        if (EditorApplication.timeSinceStartup - lastClickTime < DOUBLE_CLICK_INTERVAL)
                                        {
                                            UnityDebugViewerWindowUtility.JumpToSource(log);
                                            lastClickTime = 0;
                                        }
                                        else
                                        {
                                            lastClickTime = EditorApplication.timeSinceStartup;
                                        }
                                    }
                                    else
                                    {
                                        this.selectedLogIndex = i;
                                        lastClickTime = EditorApplication.timeSinceStartup;
                                    }
                                }
                                else if (Event.current.button == 1)
                                {
                                    ShowCopyMenu(log.info);
                                }
                            }
                        }

                        /// if "Auto Scroll" is selected, then force scroll to the bottom when new log is added
                        if (this.preLogNum != logList.Count && autoScroll)
                        {
                            upperPanelScroll.y = Mathf.Infinity;
                        }
                        this.preLogNum = logList.Count;
                    }
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndArea();
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

        private void DrawStackMessage()
        {
            GUILayout.BeginArea(lowerPanelRect);
            {
                lowerPanelScroll = GUILayout.BeginScrollView(lowerPanelScroll);
                {
                    var log = this.editorManager.activeEditor.selectedLog;
                    if (log != null && this.logFilter.ShouldDisplay(log))
                    {
                        textAreaStyle.normal.background = UnityDebugViewerWindowConstant.bgTextArea;
                        textAreaStyle.wordWrap = true;

                        string textStr = string.Format("{0}\n{1}\n", log.info, log.extraInfo);
                        var textAreaGUIContent = new GUIContent(textStr);
                        var textAreaSize = textAreaStyle.CalcSize(textAreaGUIContent);

                        EditorGUILayout.SelectableLabel(textStr, textAreaStyle, GUILayout.ExpandWidth(true), GUILayout.Height(textAreaSize.y));

                        GUILayout.Box("", GUILayout.Height(splitHeight), GUILayout.ExpandWidth(true));

                        for (int i = 0; i < log.stackList.Count; i++)
                        {
                            var stack = log.stackList[i];
                            if (stack == null)
                            {
                                continue;
                            }

                            if (string.IsNullOrEmpty(stack.sourceContent))
                            {
                                stack.sourceContent = UnityDebugViewerEditorUtility.GetSourceContent(stack.filePath, stack.lineNumber);
                            }

                            if (DrawStackBox(stack, i % 2 == 0))
                            {
                                /// try to open the source file of logStack
                                if (selectedStackIndex == i && Event.current.button == 0)
                                {
                                    if (EditorApplication.timeSinceStartup - lastClickTime < DOUBLE_CLICK_INTERVAL)
                                    {
                                        UnityDebugViewerWindowUtility.JumpToSource(stack.filePath, stack.lineNumber);
                                        lastClickTime = 0;
                                    }
                                    else
                                    {
                                        lastClickTime = EditorApplication.timeSinceStartup;
                                    }
                                }
                                else
                                {
                                    selectedStackIndex = i;
                                    lastClickTime = EditorApplication.timeSinceStartup;
                                }

                                if (Event.current.button == 1)
                                {
                                    ShowCopyMenu(stack.fullStackMessage);
                                }
                            }
                        }
                    }
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndArea();
        }

        private void DrawAnalysisMessage()
        {
            if(analysisDataTreeView == null)
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

        private void DrawResizer()
        {
            resizerRecr = new Rect(0, (position.height * sizeRatio) - resizerHeight, position.width, resizerHeight * 2);

            resizerStyle.normal.background = UnityDebugViewerWindowConstant.bgResizer;
            GUILayout.BeginArea(new Rect(resizerRecr.position + (Vector2.up * resizerHeight), new Vector2(position.width, 2)), resizerStyle);
            GUILayout.EndArea();

            EditorGUIUtility.AddCursorRect(resizerRecr, MouseCursor.ResizeVertical);
        }

        private bool DrawLogBox(LogData log, bool isOdd, int index, bool isCollapsed = false)
        {
            LogType boxType = log.type;

            GUIStyle iconStyle;
            switch (boxType)
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
            if(iconStyle == null)
            {
                return false;
            }

            logBoxStyle.wordWrap = true;
            logBoxStyle.clipping = TextClipping.Clip;
            logBoxStyle.padding = new RectOffset(20, 10, 5, 5);
            if (index == this.editorManager.activeEditor.selectedLogIndex)
            {
                logBoxStyle.normal.background = UnityDebugViewerWindowConstant.boxLogBgSelected;
            }
            else
            {
                logBoxStyle.normal.background = isOdd ? UnityDebugViewerWindowConstant.boxLogBgOdd : UnityDebugViewerWindowConstant.boxLogBgEven;
            }

            bool click;
            GUILayout.BeginHorizontal(logBoxStyle);
            {
                float logBoxHeight = (logLineCount + 1) * EditorGUIUtility.singleLineHeight;

                string content = log.info;
                var buttonGuiContent = new GUIContent(content);
                var buttonSize = logBoxStyle.CalcSize(buttonGuiContent);
                if(buttonSize.y > logBoxHeight)
                {
                    logBoxStyle.alignment = TextAnchor.UpperLeft;
                }
                else
                {
                    logBoxStyle.alignment = TextAnchor.MiddleLeft;
                }
                
                click = GUILayout.Button(buttonGuiContent, logBoxStyle, GUILayout.ExpandWidth(true), GUILayout.Height(logBoxHeight));
                Rect buttonRect = GUILayoutUtility.GetLastRect();
                var buttonMidHeight = buttonRect.y + buttonRect.height / 2;

                var iconGUIContent = new GUIContent("");
                var iconSize = iconStyle.CalcSize(iconGUIContent);
                var iconRect = new Rect(5, buttonMidHeight - iconSize.y / 2, iconSize.x, iconSize.y);
                GUI.Button(iconRect, iconGUIContent, iconStyle);

                if (isCollapsed)
                {
                    /// make sure the number label display in a fixed relative position of the window
                    int num = this.editorManager.activeEditor.GetLogNum(log);
                    GUIContent labelGUIContent = new GUIContent(num.ToString());
                    var labelSize = UnityDebugViewerWindowConstant.collapsedNumLabelStyle.CalcSize(labelGUIContent);
                    Rect labelRect = new Rect(position.width - labelSize.x - 20, buttonRect.y + buttonRect.height / 2 - labelSize.y / 2, labelSize.x, labelSize.y);

                    GUI.Label(labelRect, labelGUIContent, UnityDebugViewerWindowConstant.collapsedNumLabelStyle);
                }
            }
            GUILayout.EndHorizontal();

            return click;
        }


        private bool DrawStackBox(LogStackData stack, bool isOdd)
        {
            string content = string.Format("\n{0}\n{1}", stack.fullStackMessage, stack.sourceContent);
            stackBoxStyle.normal.background = isOdd ? UnityDebugViewerWindowConstant.boxgStackBgOdd : UnityDebugViewerWindowConstant.boxStackBgEven;
            return GUILayout.Button(new GUIContent(content), stackBoxStyle, GUILayout.ExpandWidth(true));
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

            startLogcatProcess = UnityDebugViewerADBUtility.StartLogcatProcess(LogcatDataHandler, "Unity", adbPath);
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