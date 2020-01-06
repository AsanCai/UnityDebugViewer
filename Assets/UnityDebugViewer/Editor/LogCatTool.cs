#if PLATFORM_ANDROID
using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
#if UNITY_2017_3_OR_NEWER
using UnityEditor.Compilation;
#endif

namespace UnityDebugViewer
{
    /// <summary>
    /// 存储adb logcat输出的信息
    /// </summary>
    class ADBLogParse : IComparable
    {
        public struct LogCodePath
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

    public class LogCatTool : EditorWindow
    {

        // 最大内存限制
        private const int memoryLimit = 2000;

        // 最大log显示条数
        private const int showLimit = 200;

        // Filters
        private bool prefilterOnlyUnity = true;
        private bool filterOnlyError = false;
        private bool filterOnlyWarning = false;
        private bool filterOnlyDebug = false;
        private bool filterOnlyInfo = false;
        private bool filterOnlyVerbose = false;
        private string filterByString = String.Empty;

        // Android adb logcat进程
        private Process logCatProcess;

        // Log entries
        private List<LogCatLog> logsList = new List<LogCatLog>();
        private List<LogCatLog> filteredList = new List<LogCatLog>(memoryLimit);
        private List<ADBLogParse> adbLogs = new List<ADBLogParse>();
        private List<CollapseLogPair> adbCollapseLogs = new List<CollapseLogPair>();

        /// <summary>
        /// 用来匹配log的正则表达式
        /// </summary>
        private const string LogcatPattern = @"([0-1][0-9]-[0-3][0-9] [0-2][0-9]:[0-5][0-9]:[0-5][0-9]\.[0-9]{3}) ([WIEDV])/(.*)";

        #region 命令行指令
        private const string LOGCAT_ARGUMENTS_WHOLE_UNITY = "logcat -s Unity";
        

        #endregion

        //private static readonly Regex LogcatRegex = new Regex(LogcatPattern, RegexOptions.Compiled);
        private static readonly Regex LogcatRegex = new Regex(LogcatPattern);

        // Filtered GUI list scroll position
        private Vector2 scrollPosition = new Vector2(0, 0);

        // Add menu item named "LogCat" to the Window menu
        [MenuItem("Window/Android - LogCatTool")]
        public static void ShowWindow()
        {
            // 显示窗口
            EditorWindow.GetWindow(typeof(LogCatTool), false, "LogCatTool");
        }

        void Update()
        {
            if (logsList.Count == 0)
                return;

            lock (logsList)
            {
                // 过滤器
                filteredList = logsList.Where(log => (filterByString.Length <= 2 || log.Message.ToLower().Contains(filterByString.ToLower())) &&
                                              ((!filterOnlyError && !filterOnlyWarning && !filterOnlyDebug && !filterOnlyInfo && !filterOnlyVerbose)
                 || filterOnlyError && log.Type == 'E'
                 || filterOnlyWarning && log.Type == 'W'
                 || filterOnlyDebug && log.Type == 'D'
                 || filterOnlyInfo && log.Type == 'I'
                 || filterOnlyVerbose && log.Type == 'V')).ToList();
            }

            if (logCatProcess != null)
            {
                Repaint();
            }
        }

        void OnGUI()
        {
            GUILayout.BeginHorizontal();

            // Enable pre-filter if process is not started
            GUI.enabled = logCatProcess == null;
            prefilterOnlyUnity = GUILayout.Toggle(prefilterOnlyUnity, "Only Unity", "Button", GUILayout.Width(80));

            // Enable button if process is not started
            GUI.enabled = logCatProcess == null;
            if (GUILayout.Button("Start", GUILayout.Width(60)))
            {
                string adbPath = GetAdbPath();

                // 使用`adb logcat -c`清理log buffer
                ProcessStartInfo clearProcessInfo = new ProcessStartInfo();
                clearProcessInfo.WindowStyle = ProcessWindowStyle.Hidden;
                clearProcessInfo.CreateNoWindow = true;
                clearProcessInfo.UseShellExecute = false;
                clearProcessInfo.FileName = adbPath;
                clearProcessInfo.Arguments = @"logcat -c";
                Process.Start(clearProcessInfo);

                // 创建`adb logcat`进程
                ProcessStartInfo logProcessInfo = new ProcessStartInfo();
                logProcessInfo.CreateNoWindow = true;
                logProcessInfo.UseShellExecute = false;
                logProcessInfo.RedirectStandardOutput = true;
                logProcessInfo.RedirectStandardError = true;
                logProcessInfo.StandardOutputEncoding = Encoding.UTF8;
                logProcessInfo.FileName = adbPath;
                logProcessInfo.WindowStyle = ProcessWindowStyle.Hidden;
                /// 过滤Unity的log
                //logProcessInfo.Arguments = "logcat -v time" + (prefilterOnlyUnity ? " -s  \"Unity\"" : "");
                logProcessInfo.Arguments = LOGCAT_ARGUMENTS_WHOLE_UNITY;

                /// 执行adb进程
                logCatProcess = Process.Start(logProcessInfo);
                logCatProcess.ErrorDataReceived += AddLogData;
                logCatProcess.OutputDataReceived += AddLogData;
                logCatProcess.BeginErrorReadLine();
                logCatProcess.BeginOutputReadLine();
            }

            // Disable button if process is already started
            GUI.enabled = logCatProcess != null;
            if (GUILayout.Button("Stop", GUILayout.Width(60)))
            {
                StopLogCatProcess();
            }

            GUI.enabled = true;
            if (GUILayout.Button("Clear", GUILayout.Width(60)))
            {
                lock (logsList)
                {
                    logsList.Clear();
                    filteredList.Clear();
                }
            }

            GUILayout.Label(filteredList.Count + " matching logs", GUILayout.Height(20));

            // Create filters
            filterByString = GUILayout.TextField(filterByString, GUILayout.Height(20));
            GUI.color = new Color(0.75f, 0.5f, 0.5f, 1f);
            filterOnlyError = GUILayout.Toggle(filterOnlyError, "Error", "Button", GUILayout.Width(80));
            GUI.color = new Color(0.95f, 0.95f, 0.3f, 1f);
            filterOnlyWarning = GUILayout.Toggle(filterOnlyWarning, "Warning", "Button", GUILayout.Width(80));
            GUI.color = new Color(0.5f, 0.5f, 0.75f, 1f);
            filterOnlyDebug = GUILayout.Toggle(filterOnlyDebug, "Debug", "Button", GUILayout.Width(80));
            GUI.color = new Color(0.5f, 0.75f, 0.5f, 1f);
            filterOnlyInfo = GUILayout.Toggle(filterOnlyInfo, "Info", "Button", GUILayout.Width(80));
            GUI.color = Color.white;
            filterOnlyVerbose = GUILayout.Toggle(filterOnlyVerbose, "Verbose", "Button", GUILayout.Width(80));

            GUILayout.EndHorizontal();

            GUIStyle lineStyle = new GUIStyle();
            lineStyle.normal.background = MakeTexture(600, 1, new Color(1.0f, 1.0f, 1.0f, 0.1f));

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(Screen.height - 45));

            // 确保显示log的数量不超过`showLimit`
            int fromIndex = filteredList.Count - showLimit;
            if (fromIndex < 0)
            {
                fromIndex = 0;
            }

            for (int i = fromIndex; i < filteredList.Count; i++)
            {
                LogCatLog log = filteredList[i];
                GUI.backgroundColor = log.GetBgColor();
                GUILayout.BeginHorizontal(lineStyle);
                GUILayout.Label(log.CreationDate + " | " + log.Message);
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
        }

        private bool startLogData = true;
        private bool completeLogData = false;
        private ADBLogParse parseLogData;
        private object lockLogs = new object();
        private const string filePathPattern = @"(\(at)(.+):([0-9]{0,})\)";
        int infoLogCount = 0;
        int errorLogCount = 0;
        int warningLogCount = 0;

        private void AddLogData(object sender, DataReceivedEventArgs outputLine)
        {
            //if (outputLine.Data != null && outputLine.Data.Length > 2)
            //{
            //    AddLog(new LogCatLog(outputLine.Data));
            //}
            List<string> parseLog = new List<string>(outputLine.Data.Split(' '));
            parseLog.RemoveAll(item => item == string.Empty);
            if (parseLog.Count > 5)
            {
                string message = outputLine.Data.Substring(outputLine.Data.IndexOf("Unity") + 10).Trim();

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
                    /// log结束
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

                                    parseLogData.adbLogCodePath.Add(new ADBLogParse.LogCodePath() {
                                        codeLine = line,
                                        codePath = source
                                    });
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

        private Texture2D MakeTexture(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];

            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();

            return result;
        }

        private void AddLog(LogCatLog log)
        {
            lock (logsList)
            {
                if (logsList.Count > memoryLimit + 1)
                {
                    logsList.RemoveRange(0, logsList.Count - memoryLimit + 1);
                }

                logsList.Add(log);
            }
        }

        void OnEnable()
        {
#if UNITY_2017_3_OR_NEWER
            CompilationPipeline.assemblyCompilationStarted += OnAssemblyCompilationStarted;
#endif
        }

        void OnDisable()
        {
#if UNITY_2017_3_OR_NEWER
            CompilationPipeline.assemblyCompilationStarted -= OnAssemblyCompilationStarted;
#endif
        }

        void OnDestroy()
        {
            StopLogCatProcess();
        }

        private void StopLogCatProcess()
        {
            if (logCatProcess == null)
            {
                return;
            }
            try
            {
                if (!logCatProcess.HasExited)
                {
                    logCatProcess.Kill();
                }
            }
            catch (InvalidOperationException)
            {
                // Just ignore it.
            }
            finally
            {
                logCatProcess.Dispose();
                logCatProcess = null;
            }
        }

        private void OnAssemblyCompilationStarted(string _)
        {
            StopLogCatProcess();
        }

        private class LogCatLog
        {
            public LogCatLog(string data)
            {
                UnityEngine.Debug.Log(data);

                // First char indicates error type:
                // W - warning
                // E - error
                // D - debug
                // I - info
                // V - verbose
                Match match = LogcatRegex.Match(data);
                if (match.Success)
                {
                    Type = match.Groups[2].Value[0];

                    Message = match.Groups[3].Value;
                    CreationDate = match.Groups[1].Value;
                }
                else
                {
                    Type = 'V';

                    Message = data;
                    CreationDate = DateTime.Now.ToString("MM-dd HH:mm:ss.fff");
                }
            }

            public string CreationDate
            {
                get;
                set;
            }

            public char Type
            {
                get;
                set;
            }

            public string Message
            {
                get;
                set;
            }

            public Color GetBgColor()
            {
                switch (Type)
                {
                    case 'W':
                        return Color.yellow;

                    case 'I':
                        return Color.green;

                    case 'E':
                        return Color.red;

                    case 'D':
                        return Color.blue;

                    case 'V':
                    default:
                        return Color.grey;
                }
            }
        }

        private static string GetAdbPath()
        {
#if UNITY_2019_1_OR_NEWER
        ADB adb = ADB.GetInstance();
        return adb == null ? string.Empty : adb.GetADBPath();
#else
            string androidSdkRoot = EditorPrefs.GetString("AndroidSdkRoot");
            if (string.IsNullOrEmpty(androidSdkRoot))
            {
                return string.Empty;
            }
            return Path.Combine(androidSdkRoot, Path.Combine("platform-tools", "adb"));
#endif
        }
    }
#endif
}