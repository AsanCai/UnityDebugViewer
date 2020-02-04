using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace UnityDebugViewer
{
    /// <summary>
    /// 负责绘制窗口
    /// </summary>
    public class UnityDebugViewerWindow : EditorWindow
    {
        private double lastClickTime = 0;
        private const double DOUBLE_CLICK_INTERVAL = 0.3;
        private int selectedLogIndex;
        private int selectedStackIndex;

        private bool isPlaying;

        private Rect upperPanel;
        private Rect lowerPanel;
        private Rect resizer;
        private Rect menuBar;

        private float sizeRatio = 0.5f;
        private bool isResizing;

        private float resizerHeight = 5f;
        private float splitHeight = 2f;
        private float menuBarHeight = 20f;

        private const string ShowLogPref = "LOGGER_EDITOR_SHOW_LOG";
        private const string ShowWarningPref = "LOGGER_EDITOR_SHOW_WARNING";
        private const string ShowErrorPref = "LOGGER_EDITOR_SHOW_ERROR";

        private bool collapse = false;
        private bool clearOnPlay = false;
        private bool errorPause = false;
        private bool autoScroll = false;
        private bool showLog = false;
        private bool showWarning = false;
        private bool showError = false;

        [SerializeField]
        private UnityDebugViewerEditorManager editorManager;

        private string pcPort = string.Empty;
        private string phonePort = string.Empty;
        private bool startForwardProcess = false;
        private bool onlyShowUnityLog = true;
        private bool startLogcatProcess = false;
        private int preLogNum = 0;

        private Vector2 upperPanelScroll;
        private Vector2 lowerPanelScroll;

        private GUIStyle resizerStyle = new GUIStyle();
        private GUIStyle logBoxStyle = new GUIStyle();
        private GUIStyle stackBoxStyle = new GUIStyle();
        private GUIStyle textAreaStyle = new GUIStyle();

        private Texture2D _bgLogBoxOdd;
        private Texture2D boxLogBgOdd
        {
            get
            {
                if(_bgLogBoxOdd == null)
                {
                    _bgLogBoxOdd = GUI.skin.GetStyle("OL EntryBackOdd").normal.background;
                }

                return _bgLogBoxOdd;
            }
        }
        private Texture2D _boxLogBgEven;
        private Texture2D boxLogBgEven
        {
            get
            {
                if(_boxLogBgEven == null)
                {
                    _boxLogBgEven = GUI.skin.GetStyle("OL EntryBackEven").normal.background;
                }

                return _boxLogBgEven;
            }
        }
        private Texture2D _boxLogBgSelected;
        private Texture2D boxLogBgSelected
        {
            get
            {
                if(_boxLogBgSelected == null)
                {
                    _boxLogBgSelected = GUI.skin.GetStyle("OL SelectedRow").normal.background;
                }

                return _boxLogBgSelected;
            }
        }
        private Texture2D _bgResizer;
        private Texture2D bgResizer
        {
            get
            {
                if(_bgResizer == null)
                {
                    _bgResizer = EditorGUIUtility.Load("icons/d_AvatarBlendBackground.png") as Texture2D;
                }

                return _bgResizer;
            }
        }
        private Texture2D _bgTextArea;
        private Texture2D bgTextArea
        {
            get
            {
                if(_bgTextArea == null)
                {
                    _bgTextArea = GUI.skin.GetStyle("ProjectBrowserIconAreaBg").normal.background;
                }

                return _bgTextArea;
            }
        }
        private Texture2D _bgStackBoxOdd;
        private Texture2D boxgStackBgOdd
        {
            get
            {
                if (_bgStackBoxOdd == null)
                {
                    _bgStackBoxOdd = GUI.skin.GetStyle("CN EntryBackOdd").normal.background;
                }

                return _bgStackBoxOdd;
            }
        }
        private Texture2D _boxStackBgEven;
        private Texture2D boxStackBgEven
        {
            get
            {
                if (_boxStackBgEven == null)
                {
                    _boxStackBgEven = GUI.skin.GetStyle("CN EntryBackEven").normal.background;
                }

                return _boxStackBgEven;
            }
        }

        private Texture2D icon;
        private Texture2D errorIcon;
        private Texture2D errorIconSmall;
        private Texture2D warningIcon;
        private Texture2D warningIconSmall;
        private Texture2D infoIcon;
        private Texture2D infoIconSmall;

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

        private void Awake()
        {
            errorIcon = EditorGUIUtility.Load("icons/console.erroricon.png") as Texture2D;
            warningIcon = EditorGUIUtility.Load("icons/console.warnicon.png") as Texture2D;
            infoIcon = EditorGUIUtility.Load("icons/console.infoicon.png") as Texture2D;

            errorIconSmall = EditorGUIUtility.Load("icons/console.erroricon.sml.png") as Texture2D;
            warningIconSmall = EditorGUIUtility.Load("icons/console.warnicon.sml.png") as Texture2D;
            infoIconSmall = EditorGUIUtility.Load("icons/console.infoicon.sml.png") as Texture2D;

            /// 确保只被赋值一次
            editorManager = UnityDebugViewerEditorManager.GetInstance();
        }

        /// <summary>
        /// 序列化结束时会被调用,因此静态数据的赋值需要放在这里执行
        /// </summary>
        private void OnEnable()
        {
            showLog = PlayerPrefs.GetInt(ShowLogPref, 0) == 1;
            showWarning = PlayerPrefs.GetInt(ShowWarningPref, 0) == 1;
            showError = PlayerPrefs.GetInt(ShowErrorPref, 0) == 1;

            Application.logMessageReceivedThreaded += LogMessageReceivedHandler;
#if UNITY_2017_2_OR_NEWER
            EditorApplication.playModeStateChanged += PlayModeStateChangeHandler;
#else
            EditorApplication.playmodeStateChanged += PlayModeStateChangeHandler;
#endif
        }

        private void OnDestroy()
        {
            Application.logMessageReceivedThreaded -= LogMessageReceivedHandler;
#if UNITY_2017_2_OR_NEWER
            EditorApplication.playModeStateChanged -= PlayModeStateChangeHandler;
#else
            EditorApplication.playmodeStateChanged -= PlayModeStateChangeHandler;
#endif
        }

        private void OnInspectorUpdate()
        {
            // Call Repaint on OnInspectorUpdate as it repaints the windows
            // less times as if it was OnGUI/Update
            Repaint();
        }

        private void OnGUI()
        {
            DrawMenuBar();
            DrawUpperPanel();
            DrawLowerPanel();
            DrawResizer();

            ProcessEvents(Event.current);
        }

        private void DrawMenuBar()
        {
            menuBar = new Rect(0, 0, position.width, menuBarHeight);

            GUILayout.BeginArea(menuBar, EditorStyles.toolbar);
            {
                GUILayout.BeginHorizontal();
                {
                    if(GUILayout.Button(new GUIContent("Clear"), EditorStyles.toolbarButton, GUILayout.Width(40)))
                    {
                        this.editorManager.activeEditor.ClearLog();
                    }

                    GUILayout.Space(5);

                    collapse = GUILayout.Toggle(collapse, new GUIContent("Collapse"), EditorStyles.toolbarButton);
                    clearOnPlay = GUILayout.Toggle(clearOnPlay, new GUIContent("Clear On Play"), EditorStyles.toolbarButton);
                    errorPause = GUILayout.Toggle(errorPause, new GUIContent("Error Pause"), EditorStyles.toolbarButton);
                    autoScroll = GUILayout.Toggle(autoScroll, new GUIContent("Auto Scroll"), EditorStyles.toolbarButton);

                    GUILayout.Space(5);

                    Vector2 size = EditorStyles.toolbarPopup.CalcSize(new GUIContent(this.editorManager.activeEditorTypeStr));
                    this.editorManager.activeEditorType = (UnityDebugViewerEditorType)EditorGUILayout.EnumPopup(this.editorManager.activeEditorType, EditorStyles.toolbarPopup, GUILayout.Width(size.x));
                    switch (this.editorManager.activeEditorType)
                    {
                        case UnityDebugViewerEditorType.Editor:
                            break;
                        case UnityDebugViewerEditorType.ADBForward:
                            GUILayout.Label(new GUIContent("PC Port:"), EditorStyles.label);
                            pcPort = GUILayout.TextField(pcPort, 5, EditorStyles.toolbarTextField, GUILayout.MinWidth(50f));
                            if (string.IsNullOrEmpty(pcPort))
                            {
                                pcPort = UnityDebugViewerADB.DEFAULT_PC_PORT;
                            }
                            else
                            {
                                pcPort = Regex.Replace(pcPort, @"[^0-9]", "");
                            }

                            GUILayout.Label(new GUIContent("Phone Port:"), EditorStyles.label);
                            phonePort = GUILayout.TextField(phonePort, 5, EditorStyles.toolbarTextField, GUILayout.MinWidth(50f));
                            if (string.IsNullOrEmpty(phonePort))
                            {
                                phonePort = UnityDebugViewerADB.DEFAULT_PHONE_PORT;
                            }
                            else
                            {
                                phonePort = Regex.Replace(phonePort, @"[^0-9]", "");
                            }

                            GUI.enabled = !startForwardProcess;
                            if (GUILayout.Button(new GUIContent("Start"), EditorStyles.toolbarButton))
                            {
                                startForwardProcess = UnityDebugViewerADB.StartForwardProcess(pcPort, phonePort);
                                if (startForwardProcess)
                                {
                                    int port = 0;
                                    if(int.TryParse(pcPort, out port))
                                    {
                                        UnityDebugViewerTcp.ConnectToServer("127.0.0.1", port);
                                        UnityDebugViewerLogger.Log(string.Format("Connect to 127.0.0.1:{0} successfully!", port));
                                    }
                                }
                            }

                            GUI.enabled = startForwardProcess;
                            if (GUILayout.Button(new GUIContent("Stop"), EditorStyles.toolbarButton))
                            {
                                UnityDebugViewerTcp.Disconnect();
                                UnityDebugViewerADB.StopForwardProcess();
                                startForwardProcess = false;
                            }

                            GUI.enabled = true;
                            break;
                        case UnityDebugViewerEditorType.ADBLogcat:
                            onlyShowUnityLog = GUILayout.Toggle(onlyShowUnityLog, new GUIContent("Only Unity"), EditorStyles.toolbarButton);

                            GUI.enabled = !startLogcatProcess;
                            if (GUILayout.Button(new GUIContent("Start"), EditorStyles.toolbarButton))
                            {
                                startLogcatProcess = UnityDebugViewerADB.StartLogCatProcess(LogcatDataHandler/*, "Unity"*/);
                                if (startLogcatProcess)
                                {
                                    UnityDebugViewerLogger.Log("Start logcat process successfully!");
                                }
                            }

                            GUI.enabled = startLogcatProcess;
                            if (GUILayout.Button(new GUIContent("Stop"), EditorStyles.toolbarButton))
                            {
                                UnityDebugViewerADB.StopLogCatProcess();
                                startLogcatProcess = false;
                            }

                            GUI.enabled = true;
                            break;
                        case UnityDebugViewerEditorType.LogFile:
                            break;
                    }

                    GUILayout.FlexibleSpace();

                    var _showLog = this.showLog;
                    var _showWarning = this.showWarning;
                    var _showError = this.showError;

                    string logNum = this.editorManager.activeEditor.logNum.ToString();
                    string warningNum = this.editorManager.activeEditor.warningNum.ToString();
                    string errorNum = this.editorManager.activeEditor.errorNum.ToString();

                    this.showLog = GUILayout.Toggle(this.showLog, new GUIContent(logNum, infoIconSmall), EditorStyles.toolbarButton);
                    this.showWarning = GUILayout.Toggle(this.showWarning, new GUIContent(warningNum, warningIconSmall), EditorStyles.toolbarButton);
                    this.showError = GUILayout.Toggle(this.showError, new GUIContent(errorNum, errorIconSmall), EditorStyles.toolbarButton);

                    if (_showLog != this.showLog)
                    {
                        PlayerPrefs.SetInt(ShowLogPref, this.showLog ? 1 : 0);
                    }
                    if (_showWarning != this.showWarning)
                    {
                        PlayerPrefs.SetInt(ShowWarningPref, this.showWarning ? 1 : 0);
                    }
                    if (_showError != this.showError)
                    {
                        PlayerPrefs.SetInt(ShowErrorPref, this.showError ? 1 : 0);
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndArea();
        }

        private void DrawUpperPanel()
        {
            upperPanel = new Rect(0, menuBarHeight, position.width, (position.height * sizeRatio) - menuBarHeight);

            GUILayout.BeginArea(upperPanel);
            {
                upperPanelScroll = GUILayout.BeginScrollView(upperPanelScroll);
                {
                    var logList = this.collapse ? this.editorManager.activeEditor.collapsedLogList : this.editorManager.activeEditor.logList;

                    if (logList != null)
                    {
                        for (int i = 0; i < logList.Count; i++)
                        {
                            var log = logList[i];
                            int num = this.editorManager.activeEditor.GetLogNum(log);
                            if (ShouldDisplay(log.type) && DrawLogBox(log, i % 2 == 0, num, this.collapse))
                            {
                                if (this.editorManager.activeEditor.selectedLog != null)
                                {
                                    this.editorManager.activeEditor.selectedLog.isSelected = false;
                                }

                                log.isSelected = true;
                                this.editorManager.activeEditor.selectedLog = log;

                                this.selectedLogIndex = i;
                            }
                        }

                        /// 有新的log，并且开启了"Auto Scroll"
                        /// 需要强制滚动至底部
                        if (this.preLogNum != logList.Count && this.autoScroll)
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
            lowerPanel = new Rect(0, (position.height * sizeRatio) + resizerHeight, position.width, (position.height * (1 - sizeRatio)) - resizerHeight);

            GUILayout.BeginArea(lowerPanel);
            {
                if (this.editorManager.activeEditor.selectedLog != null)
                {
                    var log = this.editorManager.activeEditor.selectedLog;
                    textAreaStyle.normal.background = bgTextArea;
                    string textStr = string.Format("{0}\n{1}\n", log.info, log.extraInfo);
                    GUILayout.TextArea(textStr, textAreaStyle, GUILayout.ExpandWidth(true));

                    GUILayout.Box("", GUILayout.Height(splitHeight), GUILayout.ExpandWidth(true));

                    lowerPanelScroll = GUILayout.BeginScrollView(lowerPanelScroll);
                    {
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
                                if (selectedStackIndex == i)
                                {
                                    if (EditorApplication.timeSinceStartup - lastClickTime < DOUBLE_CLICK_INTERVAL)
                                    {
                                        UnityDebugViewerEditorUtility.JumpToSource(stack.filePath, stack.lineNumber);
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
                            }
                        }
                    }
                    GUILayout.EndScrollView();
                }
            }
            GUILayout.EndArea();
        }

        private void DrawResizer()
        {
            resizer = new Rect(0, (position.height * sizeRatio) - resizerHeight, position.width, resizerHeight * 2);

            resizerStyle.normal.background = bgResizer;
            GUILayout.BeginArea(new Rect(resizer.position + (Vector2.up * resizerHeight), new Vector2(position.width, 2)), resizerStyle);
            GUILayout.EndArea();

            EditorGUIUtility.AddCursorRect(resizer, MouseCursor.ResizeVertical);
        }

        private bool DrawLogBox(LogData log, bool isOdd, int num, bool isCollapsed = false)
        {
            string content = log.info;
            LogType boxType = log.type;
            bool isSelected = log.isSelected;

            if (isSelected)
            {
                logBoxStyle.normal.background = boxLogBgSelected;
            }
            else
            {
                logBoxStyle.normal.background = isOdd ? boxLogBgOdd : boxLogBgEven;
            }

            switch (boxType)
            {
                case LogType.Error: icon = errorIcon; break;
                case LogType.Exception: icon = errorIcon; break;
                case LogType.Assert: icon = errorIcon; break;
                case LogType.Warning: icon = warningIcon; break;
                case LogType.Log: icon = infoIcon; break;
            }

            bool click;
            GUILayout.BeginHorizontal(logBoxStyle);
            {
                click = GUILayout.Button(new GUIContent(content, icon), logBoxStyle, GUILayout.ExpandWidth(true), GUILayout.Height(30));

                if (isCollapsed)
                {
                    Rect buttonRect = GUILayoutUtility.GetLastRect();
                    GUIContent numContent = new GUIContent(num.ToString());
                    GUIStyle numStyle = GUI.skin.GetStyle("CN CountBadge");

                    Vector2 size = numStyle.CalcSize(numContent);
                    Rect labelRect = new Rect(buttonRect.width - size.x - 20, buttonRect.y + buttonRect.height / 2 - size.y / 2, size.x, size.y);

                    GUI.Label(labelRect, numContent, numStyle);
                }
            }
            GUILayout.EndHorizontal();

            return click;
        }


        private bool DrawStackBox(LogStackData stack, bool isOdd)
        {
            string content = string.Format("\n{0}\n{1}", stack.fullStackMessage, stack.sourceContent);
            stackBoxStyle.normal.background = isOdd ? boxgStackBgOdd : boxStackBgEven;
            return GUILayout.Button(new GUIContent(content), stackBoxStyle, GUILayout.ExpandWidth(true));
        }

        private bool ShouldDisplay(LogType type)
        {
            switch (type)
            {
                case LogType.Log:
                    return this.showLog;

                case LogType.Warning:
                    return this.showWarning;

                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    return this.showError;

                default:
                    return false;
            }
        }


        private void ProcessEvents(Event e)
        {
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0 && resizer.Contains(e.mousePosition))
                    {
                        isResizing = true;
                    }
                    break;

                case EventType.MouseUp:
                    isResizing = false;
                    break;
            }

            Resize(e);
        }
        private void Resize(Event e)
        {
            if (isResizing)
            {
                sizeRatio = e.mousePosition.y / position.height;
                Repaint();
            }
        }

        private void LogMessageReceivedHandler(string info, string stackTrace, LogType type)
        {
            UnityDebugViewerLogger.AddEditorLog(info, stackTrace, type);
        }

        private void PlayModeStateChangeHandler(PlayModeStateChange state)
        {
            if (!isPlaying && EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (this.clearOnPlay)
                {
                    this.editorManager.activeEditor.ClearLog();
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