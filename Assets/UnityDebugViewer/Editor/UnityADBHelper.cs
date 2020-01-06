using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System;
using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;

enum LogLevel
{
    INFO = 1,
    WARNING = 2,
    ERROR = 4,
    ALL = 8,
    UNKNOWN = 16
}

class ADBLogParse : IComparable
{
    public class LogCodePath
    {
        public string codePath;
        public int codeLine;
    }

    public DateTime adbLogDateTime;
    public LogLevel adbLogLevel;
    public string adbLogMessage;
    public string rawLogMessage = "";
    public List<LogCodePath> adbLogCodePath = new List<LogCodePath>();

    public int CompareTo(object obj)
    {
        ADBLogParse tmp = obj as ADBLogParse;
        return adbLogDateTime.CompareTo(tmp.adbLogDateTime);
    }
}

class CollapseLogPair
{
    public ADBLogParse Log { get; private set; }
    public int Count { get; set; }
    public CollapseLogPair(ADBLogParse log)
    {
        Log = log;
        Count = 1;
    }
}

public class UnityADBHelper : EditorWindow
{
    #region ADB command
    private string ADB_EXECUTABLE = "{0}/platform-tools/adb.exe";

    private const string ADB_DEVICE_CHECK = "devices";

    private const string LOGCAT_ARGUMENTS_WHOLE_UNITY = "logcat -s Unity";
    private const string LOGCAT_ARGUMENTS_WHOLE_UNITY_LOG = "logcat Unity:I Native:I *:S";
    private const string LOGCAT_CLEAR = "logcat -c";

    private string REMOTE_ADB = "tcpip {0}";
    private string REMOTE_ADB_CONNECT = "connect {0}:{1}";
    private const string REMOTE_ADB_DISCONNECT = "disconnect";
    #endregion

    // only supports for Visual studio
    private string openEditorPath;
    private string OPEN_SCRIPT_COMMAND = "\"{0}\" /edit \"{1}\" /command \"Edit.Goto {2}\"";

    System.Threading.Thread adbProcessBackground;
    object lockLogs = new object();

    private Process adbProcess;
    private List<ADBLogParse> adbLogs;
    private List<ADBLogParse> adbFilteredLogs;
    private List<CollapseLogPair> adbCollapseLogs;
    private List<CollapseLogPair> adbFilteredCollapseLog;
    private List<Rect> adbLogRects;

    private string clickedItemLog;
    private List<ADBLogParse.LogCodePath> clickedItemCode;
    private ADBLogParse parseLogData;
    private bool completeLogData = false;
    private bool startLogData = true;

    private string deviceID;
    private string IPAddress = "";
    private string port = "5555";
    private bool useRemoteADB;
    private Vector2 scrollPos = Vector2.zero;
    private Vector2 infoScrollPos = Vector2.zero;
    bool scrollFixed = false;

    private LogLevel filterLogLevel = LogLevel.ALL;
    Texture infoLogLevel;
    Texture errorLogLevel;
    Texture warningLogLevel;
    int infoLogCount = 0;
    int errorLogCount = 0;
    int warningLogCount = 0;

    bool filterInfo = true;
    bool filterError = true;
    bool filterWarning = true;
    bool clearOnPlay = false;
    bool collapseLog = false;

    // regex pattern
    string filePathPattern = @"(\(at)(.+):([0-9]{0,})\)";

    // splitter
    private float splitterCurrentPos = 200.0f;
    private Vector2 prevMousePosition;
    private bool splitterResizeFlag = false;

    // Add menu named "ADB for Unity" to the Window menu
    [MenuItem("Window/ADB - Unity")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        UnityADBHelper window = (UnityADBHelper)EditorWindow.GetWindow(typeof(UnityADBHelper));
        window.Show();
    }

    private void OnEnable()
    {
        adbLogs = new List<ADBLogParse>();
        adbFilteredLogs = new List<ADBLogParse>();
        adbLogRects = new List<Rect>();
        clickedItemCode = new List<ADBLogParse.LogCodePath>();
        adbCollapseLogs = new List<CollapseLogPair>();
        adbFilteredCollapseLog = new List<CollapseLogPair>();
        parseLogData = new ADBLogParse();
        EditorApplication.playmodeStateChanged = HandleOnPlayModeChanged;

        infoLogLevel = Resources.Load("info") as Texture;
        errorLogLevel = Resources.Load("error") as Texture;
        warningLogLevel = Resources.Load("warning") as Texture;

        ADB_EXECUTABLE = string.Format(ADB_EXECUTABLE, EditorPrefs.GetString("AndroidSdkRoot"));
        openEditorPath = EditorPrefs.GetString("kScriptsDefaultApp");
    }

    private void OnDisable()
    {
        DestroyProcess();
    }

    private void OnDestroy()
    {
        DestroyProcess();
    }

    void DestroyProcess()
    {
        if (adbProcessBackground.ThreadState == System.Threading.ThreadState.Running)
            adbProcessBackground.Abort();

        if (adbProcess != null && !adbProcess.HasExited)
            adbProcess.Kill();
    }

    void OnGUI()
    {
        Rect settingRect = EditorGUILayout.BeginHorizontal();
        useRemoteADB = EditorGUILayout.BeginToggleGroup("Use Remote ADB", useRemoteADB);
        EditorGUILayout.BeginVertical();
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("IP address");
        IPAddress = GUILayout.TextField(IPAddress, GUILayout.Width(150));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Port");
        port = GUILayout.TextField(port, GUILayout.Width(150));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndToggleGroup();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Connect"))
            RemoteADBSetting();
        if (GUILayout.Button("Disconnect"))
            RunCommand(REMOTE_ADB_DISCONNECT);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Raw log"))
            ; // open other window
        EditorGUILayout.EndHorizontal();
        
        // indicator
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginVertical();
        Rect buttonRect = EditorGUILayout.BeginHorizontal();
        // buttons
        if (GUILayout.Button("Start"))
        {
            init();

            RunCommand(LOGCAT_CLEAR);
            adbLogs.Clear();
            adbFilteredLogs.Clear();
            adbFilteredCollapseLog.Clear();

            RunLogcat();
        }
        if (GUILayout.Button("Stop"))
            DestroyProcess();
        if (GUILayout.Button("Clear"))
        {
            RunCommand(LOGCAT_CLEAR);
            adbLogs.Clear();
            adbFilteredLogs.Clear();
            adbFilteredCollapseLog.Clear();
        }
        collapseLog = GUILayout.Toggle(collapseLog, "Collapse", "Button");
        clearOnPlay = GUILayout.Toggle(clearOnPlay, "Clear on Play", "Button");
        GUILayout.FlexibleSpace();
        scrollFixed = GUILayout.Toggle(scrollFixed, "Scroll to end", "Button");

        // just trigger when clicking button (edit)
        bool prevState = filterInfo;
        filterInfo = GUILayout.Toggle(filterInfo, "Info " + infoLogCount.ToString(), "Button");
        if (prevState != filterInfo)
            filterLogLevel ^= LogLevel.INFO;

        prevState = filterWarning;
        filterWarning = GUILayout.Toggle(filterWarning, "Warning " + warningLogCount.ToString(), "Button");
        if (prevState != filterWarning)
            filterLogLevel ^= LogLevel.WARNING;

        prevState = filterError;
        filterError = GUILayout.Toggle(filterError, "Error " + errorLogCount.ToString(), "Button");
        if (prevState != filterError)
            filterLogLevel ^= LogLevel.ERROR;
        EditorGUILayout.EndHorizontal();

        // repaint <> layout event 
        //if (Event.current.type == EventType.layout)
        //    FilteredLog();
        if (Event.current.type == EventType.Layout)
            FilteredLog();

        adbLogRects.Clear();
        float upperMargin = splitterCurrentPos + settingRect.height + buttonRect.height + 10;
        if (scrollFixed)
            scrollPos = EditorGUILayout.BeginScrollView(new Vector2(0, (adbFilteredLogs.Count * 44 + 4) - splitterCurrentPos), false, true, GUILayout.Height(splitterCurrentPos), GUILayout.Width(position.width));
        else
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, true, GUILayout.Height(splitterCurrentPos), GUILayout.Width(position.width));

        if (collapseLog)
        {
            for (int i = 0; i < adbFilteredCollapseLog.Count; ++i)
            {
                Rect logItem = EditorGUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.Width(position.width - 50));
                adbLogRects.Add(logItem);

                switch (adbFilteredCollapseLog[i].Log.adbLogLevel)
                {
                    case LogLevel.ERROR:
                        GUILayout.Label(errorLogLevel, GUILayout.Width(32), GUILayout.Height(32));
                        break;
                    case LogLevel.INFO:
                        GUILayout.Label(infoLogLevel, GUILayout.Width(32), GUILayout.Height(32));
                        break;
                    case LogLevel.WARNING:
                        GUILayout.Label(warningLogLevel, GUILayout.Width(32), GUILayout.Height(32));
                        break;
                }
                EditorGUILayout.BeginVertical();
                GUILayout.Label(adbFilteredCollapseLog[i].Log.adbLogMessage, GUILayout.Width(position.width - 120));
                GUILayout.Label(adbFilteredCollapseLog[i].Log.adbLogDateTime.ToString(), GUILayout.Width(position.width - 120));
                EditorGUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                // collapse log
                GUILayout.Label(adbFilteredCollapseLog[i].Count.ToString(), EditorStyles.helpBox);
                EditorGUILayout.EndHorizontal();
            }
        }
        else
        {
            for (int i = 0; i < adbFilteredLogs.Count; ++i)
            {
                Rect logItem = EditorGUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.Width(position.width - 50));
                adbLogRects.Add(logItem);

                switch (adbFilteredLogs[i].adbLogLevel)
                {
                    case LogLevel.ERROR:
                        GUILayout.Label(errorLogLevel, GUILayout.Width(32), GUILayout.Height(32));
                        break;
                    case LogLevel.INFO:
                        GUILayout.Label(infoLogLevel, GUILayout.Width(32), GUILayout.Height(32));
                        break;
                    case LogLevel.WARNING:
                        GUILayout.Label(warningLogLevel, GUILayout.Width(32), GUILayout.Height(32));
                        break;
                }
                EditorGUILayout.BeginVertical();
                GUILayout.Label(adbFilteredLogs[i].adbLogMessage, GUILayout.Width(position.width - 70));
                GUILayout.Label(adbFilteredLogs[i].adbLogDateTime.ToString(), GUILayout.Width(position.width - 70));
                EditorGUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndScrollView();
        ResizeSplitter(new Rect(0, upperMargin, position.width, 10));

        GUILayout.FlexibleSpace();
        infoScrollPos = EditorGUILayout.BeginScrollView(infoScrollPos, false, true, GUILayout.Height(position.height - upperMargin - 100), GUILayout.Width(position.width));
        EditorGUILayout.LabelField(clickedItemLog, EditorStyles.wordWrappedLabel, GUILayout.Width(position.width));
        EditorGUILayout.EndScrollView();
        GUILayout.FlexibleSpace();

        if (clickedItemCode.Count != 0)
            foreach (ADBLogParse.LogCodePath path in clickedItemCode)
                if (GUILayout.Button(path.codePath))
                    OpenScript(path.codePath, path.codeLine);

        EditorGUILayout.EndVertical();

        // **** height margin 10
        EditorGUI.DrawRect(new Rect(0, upperMargin, position.width, 5), new Color(0.9f, 0.9f, 0.9f));
        EditorGUIUtility.AddCursorRect(new Rect(0, upperMargin, position.width, 10), MouseCursor.SplitResizeUpDown);

        //if (Event.current.type == EventType.mouseDown)
        if (Event.current.type == EventType.MouseDown)
        {
            // click item
            int clickedIdx = Mathf.FloorToInt((Event.current.mousePosition.y - (upperMargin - splitterCurrentPos - scrollPos.y)) / 44);
            if (collapseLog)
            {
                if (clickedIdx < adbFilteredCollapseLog.Count && clickedIdx >= 0)
                {
                    clickedItemLog = adbFilteredCollapseLog[clickedIdx].Log.adbLogMessage + "\n" + adbFilteredCollapseLog[clickedIdx].Log.rawLogMessage;
                    clickedItemCode = adbFilteredCollapseLog[clickedIdx].Log.adbLogCodePath;
                }
            }
            else
            {
                if (clickedIdx < adbFilteredLogs.Count && clickedIdx >= 0)
                {
                    clickedItemLog = adbFilteredLogs[clickedIdx].adbLogMessage + "\n" + adbFilteredLogs[clickedIdx].rawLogMessage;
                    clickedItemCode = adbFilteredLogs[clickedIdx].adbLogCodePath;
                }
            }
        }

        Repaint();
    }

    private void OpenScript(string path, int line)
    {
        string cmd = string.Format(OPEN_SCRIPT_COMMAND, openEditorPath, path, line.ToString());
        Process open = new Process();

        open.StartInfo.FileName = cmd;
        open.StartInfo.RedirectStandardOutput = true;
        open.StartInfo.CreateNoWindow = true;
        open.StartInfo.UseShellExecute = false;
        open.Start();
    }

    private void FilteredLog()
    {
        if (collapseLog)
        {
            adbFilteredCollapseLog.Clear();
            adbFilteredCollapseLog = new List<CollapseLogPair>(adbCollapseLogs);
            adbFilteredCollapseLog.RemoveAll(item => ((item.Log.adbLogLevel & filterLogLevel) == item.Log.adbLogLevel));
        }
        else
        {
            if (filterLogLevel == 0)
            {
                adbFilteredLogs.Clear();
                return;
            }

            if (filterLogLevel == LogLevel.ALL)
            {
                adbFilteredLogs = new List<ADBLogParse>(adbLogs);
                return;
            }
            else
            {
                // critical section
                adbFilteredLogs.Clear();
                adbFilteredLogs = new List<ADBLogParse>(adbLogs);
                adbFilteredLogs.RemoveAll(item => ((item.adbLogLevel & filterLogLevel) == item.adbLogLevel));
            }
        }
    }

    private void ResizeSplitter(Rect splitterRect)
    {
        //if (Event.current.type == EventType.mouseDown && splitterRect.Contains(Event.current.mousePosition))
        if (Event.current.type == EventType.MouseDown && splitterRect.Contains(Event.current.mousePosition))
        {
            splitterResizeFlag = true;
            prevMousePosition = Event.current.mousePosition;
        }

        if (splitterResizeFlag)
        {
            float val = Event.current.mousePosition.y - prevMousePosition.y;
            if ((splitterCurrentPos + val) > 40 && (splitterCurrentPos + val) < position.height - 70)
                splitterCurrentPos += val;
            prevMousePosition = Event.current.mousePosition;
            Repaint();
        }

        //if (Event.current.type == EventType.mouseUp)
        if (Event.current.type == EventType.MouseUp)
            splitterResizeFlag = false;
    }

    public void init()
    {
        adbProcess = new Process();

        adbProcess.StartInfo.FileName = ADB_EXECUTABLE;
        adbProcess.StartInfo.RedirectStandardOutput = true;
        adbProcess.StartInfo.CreateNoWindow = true;
        adbProcess.StartInfo.UseShellExecute = false;
    }

    public void RemoteADBSetting()
    {
        if (DeviceCheck())
        {
            RunCommand(string.Format(REMOTE_ADB, port));
            RunCommand(string.Format(REMOTE_ADB_CONNECT, IPAddress, port));
        }
        else
            EditorUtility.DisplayDialog("Remote ADB", "connect android device", "ok");
    }

    public void RunLogcat()
    {
        adbProcessBackground = new System.Threading.Thread(() =>
        {
            adbProcess.StartInfo.Arguments = LOGCAT_ARGUMENTS_WHOLE_UNITY;
            adbProcess.OutputDataReceived += AdbProcessOutputDataReceived;
            adbProcess.Start();
            adbProcess.BeginOutputReadLine();

            adbProcess.WaitForExit();
        });

        adbProcessBackground.Start();
    }

    private bool DeviceCheck()
    {
        RunCommand(ADB_DEVICE_CHECK);

        StreamReader stdOutput = adbProcess.StandardOutput;
        // bypass the first line ("List of devices attached")
        stdOutput.ReadLine();

        if (!stdOutput.EndOfStream)
        {
            string deviceCheck = stdOutput.ReadLine();
            if (deviceCheck == string.Empty)
                return false;
            else
                deviceID = deviceCheck.Split('\t')[0];

            return true;
        }

        return false;
    }

    private void RunCommand(string arg)
    {
        if (adbProcess == null)
            init();

        adbProcess.StartInfo.Arguments = arg;
        adbProcess.Start();
        adbProcess.WaitForExit();
    }

    private void AdbProcessOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        List<string> parseLog = new List<string>(e.Data.Split(' '));
        parseLog.RemoveAll(item => item == string.Empty);
        if (parseLog.Count > 5)
        {
            string message = e.Data.Substring(e.Data.IndexOf("Unity") + 10).Trim();
            if (startLogData)
            {
                startLogData = false;
                completeLogData = true;
                parseLogData.adbLogMessage = message;
                parseLogData.adbLogDateTime = DateTime.ParseExact(parseLog[0] + parseLog[1], "MM-ddHH:mm:ss.fff", System.Globalization.CultureInfo.CurrentCulture);
                switch (parseLog[4].ToCharArray()[0])
                {
                    case 'I':
                        parseLogData.adbLogLevel = LogLevel.INFO;
                        break;
                    case 'E':
                        parseLogData.adbLogLevel = LogLevel.ERROR;
                        break;
                    case 'W':
                        parseLogData.adbLogLevel = LogLevel.WARNING;
                        break;
                    default:
                        parseLogData.adbLogLevel = LogLevel.UNKNOWN;
                        break;
                }
            }
            else if (completeLogData)
            {
                if (message == string.Empty)
                {
                    completeLogData = false;
                    startLogData = true;
                    lock (lockLogs)
                    {
                        if (parseLogData.adbLogLevel != LogLevel.UNKNOWN && (parseLogData.rawLogMessage.Trim() != string.Empty 
                            || (parseLogData.rawLogMessage.Trim() == string.Empty && parseLogData.adbLogLevel != LogLevel.INFO)))
                        {
                            foreach (Match regMatch in Regex.Matches(parseLogData.rawLogMessage, filePathPattern))
                            {
                                string source = regMatch.Groups[2].Value;
                                int line = int.Parse(regMatch.Groups[3].Value);

                                parseLogData.adbLogCodePath.Add(new ADBLogParse.LogCodePath() { codeLine = line, codePath = source });
                            }

                            bool noCollapseLog = false;
                            for (int i = 0; i < adbCollapseLogs.Count; ++i)
                            {
                                if (adbCollapseLogs[i].Log.adbLogMessage == parseLogData.adbLogMessage)
                                    if (adbCollapseLogs[i].Log.adbLogCodePath[0].codeLine == parseLogData.adbLogCodePath[0].codeLine &&
                                        adbCollapseLogs[i].Log.adbLogCodePath[0].codePath == parseLogData.adbLogCodePath[0].codePath)
                                    {
                                        adbCollapseLogs[i].Count++;
                                        noCollapseLog = true;
                                        break;
                                    }
                            }
                            if (!noCollapseLog)
                                adbCollapseLogs.Add(new CollapseLogPair(parseLogData));

                            switch (parseLogData.adbLogLevel)
                            {
                                case LogLevel.INFO:
                                    infoLogCount++;
                                    break;
                                case LogLevel.ERROR:
                                    errorLogCount++;
                                    break;
                                case LogLevel.WARNING:
                                    warningLogCount++;
                                    break;
                            }

                            adbLogs.Add(parseLogData);
                            parseLogData = new ADBLogParse();
                        }
                    }
                }
                else
                    parseLogData.rawLogMessage += (message + "\n");
            }
        }
    }

    private void HandleOnPlayModeChanged()
    {
        if (EditorApplication.isPlaying && clearOnPlay)
        {
            RunCommand(LOGCAT_CLEAR);

            adbLogs.Clear();
            adbFilteredLogs.Clear();
        }
    }
}
