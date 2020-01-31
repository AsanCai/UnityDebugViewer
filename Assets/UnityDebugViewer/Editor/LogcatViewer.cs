using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading;

namespace UnityDebugViewer
{
    enum LogLevel
    {
        INFO = 1,
        WARNING = 2,
        ERROR = 4,
        ALL = 8,
        UNKNOWN = 16
    }

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
        public string stackMessage = "";
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

    public class LogcatViewer : EditorWindow
    {
        //[MenuItem("Window/Android - LogCatViewer")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(LogcatViewer), false, "LogcatViewer");
        }


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

        // Android adb logcat进程
        private Process logCatProcess;
        private List<ADBLogParse> adbLogs;
        private List<ADBLogParse> adbFilteredLogs;

        #region Tcp Client
        private IPAddress ip;
        private IPEndPoint ipEnd;
        private Socket serverSocket;

        private byte[] receiveData;
        private byte[] sendData;
        private int receiveLength;
        private string receiveStr;
        private string sendStr;
        private Thread connectThread;

        private void ConnectToServer()
        {
            ip = IPAddress.Parse("127.0.0.1");
            ipEnd = new IPEndPoint(ip, 5000);

            connectThread = new Thread(new ThreadStart(SocketReceive));
            connectThread.Start();
        }

        private void SocketReceive()
        {
            SocketConnect();
            while (true)
            {
                receiveData = new byte[1024];
                receiveLength = serverSocket.Receive(receiveData);
                if (receiveLength == 0)
                {
                    SocketConnect();
                    continue;
                }

                receiveStr = Encoding.UTF8.GetString(receiveData, 0, receiveLength);
                UnityEngine.Debug.LogError(receiveStr);
            }
        }

        private void SocketConnect()
        {
            if (serverSocket != null)
            {
                serverSocket.Close();
                serverSocket = null;
            }

            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Connect(ipEnd);

            //输出初次连接收到的字符串
            receiveLength = serverSocket.Receive(receiveData);
            if(receiveLength == 0)
            {
                return;
            }

            receiveStr = Encoding.UTF8.GetString(receiveData, 0, receiveLength);
            UnityEngine.Debug.LogError(receiveStr);
        }

        private void SocketSend(string data)
        {
            if (serverSocket == null)
            {
                UnityEngine.Debug.LogError("Server socket is null");
                return;
            }

            sendData = new byte[1024];
            sendData = Encoding.UTF8.GetBytes(data);
            serverSocket.Send(sendData);
        }

        private void SocketQuit()
        {
            //先关闭线程
            if (connectThread != null)
            {
                connectThread.Interrupt();
                connectThread.Abort();
            }

            //最后关闭服务器
            if(serverSocket != null)
            {
                serverSocket.Close();
                serverSocket = null;
            }

            UnityEngine.Debug.LogError("Disconnect");
        }
        #endregion

        private void OnEnable()
        {
            adbLogs = new List<ADBLogParse>();
            adbFilteredLogs = new List<ADBLogParse>();
        }

        void OnDestroy()
        {
            StopLogCatProcess();
        }

        // Filters
        private bool prefilterOnlyUnity = true;
        private bool filterOnlyError = false;
        private bool filterOnlyWarning = false;
        private bool filterOnlyDebug = false;
        private bool filterOnlyInfo = false;
        private bool filterOnlyVerbose = false;
        private string filterByString = String.Empty;
        private Vector2 scrollPosition = Vector2.zero;
        private int showLimit = 200;
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
                RunClearCommond();

                string adbPath = GetAdbPath();

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
                logProcessInfo.Arguments = LOGCAT_ARGUMENTS_WHOLE_UNITY;

                /// 执行adb进程
                logCatProcess = Process.Start(logProcessInfo);
                logCatProcess.ErrorDataReceived += AdbProcessOutputDataReceived;
                logCatProcess.OutputDataReceived += AdbProcessOutputDataReceived;
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
                ClearLog();
            }

            GUI.enabled = serverSocket == null;
            if (GUILayout.Button("Connect", GUILayout.Width(100)))
            {
                ConnectToServer();
            }

            GUI.enabled = serverSocket != null;
            if (GUILayout.Button("Disconnect", GUILayout.Width(100)))
            {
                SocketQuit();
            }

            //GUILayout.Label(filteredList.Count + " matching logs", GUILayout.Height(20));

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

            //// 确保显示log的数量不超过`showLimit`
            //int fromIndex = filteredList.Count - showLimit;
            //if (fromIndex < 0)
            //{
            //    fromIndex = 0;
            //}

            //for (int i = fromIndex; i < filteredList.Count; i++)
            //{
            //    LogCatLog log = filteredList[i];
            //    GUI.backgroundColor = log.GetBgColor();
            //    GUILayout.BeginHorizontal(lineStyle);
            //    GUILayout.Label(log.CreationDate + " | " + log.Message);
            //    GUILayout.EndHorizontal();
            //}

            // 确保显示log的数量不超过`showLimit`
            int fromIndex = adbLogs.Count - showLimit;
            if (fromIndex < 0)
            {
                fromIndex = 0;
            }

            for (int i = fromIndex; i < adbLogs.Count; i++)
            {
                ADBLogParse log = adbLogs[i];
                //GUI.backgroundColor = log.GetBgColor();
                GUILayout.BeginHorizontal(lineStyle);
                GUILayout.Label(string.Format("{0} {1} {2}\n{3}", log.adbLogLevel, log.adbLogDateTime, log.adbLogMessage, log.stackMessage));
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
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

        private void ClearLog()
        {
            RunClearCommond();

            lock (adbLogs)
            {
                adbLogs.Clear();
                adbFilteredLogs.Clear();
            }
        }

        private void RunClearCommond()
        {
            // 使用`adb logcat -c`清理log buffer
            ProcessStartInfo clearProcessInfo = new ProcessStartInfo();
            clearProcessInfo.WindowStyle = ProcessWindowStyle.Hidden;
            clearProcessInfo.CreateNoWindow = true;
            clearProcessInfo.UseShellExecute = false;
            clearProcessInfo.FileName = GetAdbPath();
            clearProcessInfo.Arguments = LOGCAT_CLEAR;
            Process clearProcess = Process.Start(clearProcessInfo);
            clearProcess.WaitForExit();
        }

        private bool startLogData = true;
        private bool completeLogData = false;
        private ADBLogParse parseLogData = new ADBLogParse();
        private const string filePathPattern = @"(\(at)(.+):([0-9]{0,})\)";
        private void AdbProcessOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            List<string> parseLog = new List<string>(e.Data.Split(' '));
            parseLog.RemoveAll(item => item == string.Empty);

            if(parseLog.Count > 5)
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

                        lock (parseLogData)
                        {
                            if (parseLogData.adbLogLevel != LogLevel.UNKNOWN && (parseLogData.stackMessage.Trim() != string.Empty
                                || (parseLogData.stackMessage.Trim() == string.Empty && parseLogData.adbLogLevel != LogLevel.INFO)))
                            {
                                foreach (Match regMatch in Regex.Matches(parseLogData.stackMessage, filePathPattern))
                                {
                                    string source = regMatch.Groups[2].Value;
                                    int line = int.Parse(regMatch.Groups[3].Value);

                                    parseLogData.adbLogCodePath.Add(new ADBLogParse.LogCodePath() { codeLine = line, codePath = source });
                                }

                                //bool noCollapseLog = false;
                                //for (int i = 0; i < adbCollapseLogs.Count; ++i)
                                //{
                                //    if (adbCollapseLogs[i].Log.adbLogMessage == parseLogData.adbLogMessage)
                                //        if (adbCollapseLogs[i].Log.adbLogCodePath[0].codeLine == parseLogData.adbLogCodePath[0].codeLine &&
                                //            adbCollapseLogs[i].Log.adbLogCodePath[0].codePath == parseLogData.adbLogCodePath[0].codePath)
                                //        {
                                //            adbCollapseLogs[i].Count++;
                                //            noCollapseLog = true;
                                //            break;
                                //        }
                                //}
                                //if (!noCollapseLog)
                                //    adbCollapseLogs.Add(new CollapseLogPair(parseLogData));

                                //switch (parseLogData.adbLogLevel)
                                //{
                                //    case LogLevel.INFO:
                                //        infoLogCount++;
                                //        break;
                                //    case LogLevel.ERROR:
                                //        errorLogCount++;
                                //        break;
                                //    case LogLevel.WARNING:
                                //        warningLogCount++;
                                //        break;
                                //}

                                adbLogs.Add(parseLogData);
                                parseLogData = new ADBLogParse();
                            }
                        }
                    }
                    else
                    {
                        parseLogData.stackMessage += (message + "\n");
                    }
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
}
