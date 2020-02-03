using System;
using System.IO;
using System.Diagnostics;
using System.Text;
using UnityEditor;

namespace UnityDebugViewer
{
    public static class UnityDebugViewerADB
    {
        private static Process logCatProcess;
        private static string deviceID;
        public const string DEFAULT_PC_PORT = "50000";
        public const string DEFAULT_PHONE_PORT = "50000";

        private const string LOGCAT_CLEAR = "logcat -c";
        //private const string LOGCAT_ARGUMENTS_WHOLE_UNITY = "logcat -s Unity";
        private const string LOGCAT_ARGUMENTS = "logcat -v time";
        private const string LOGCAT_ARGUMENTS_WITH_FILTER = "logcat -v time -s {0}";
        private const string ADB_DEVICE_CHECK = "devices";
        private const string START_ADB_FORWARD = "forward tcp:{0} tcp:{1}";
        private const string STOP_ADB_FORWARD = "forward --remove-all";

        public static void RunClearCommand()
        {
            // 使用`adb logcat -c`清理log buffer
            ProcessStartInfo clearProcessInfo = CreateProcessStartInfo(LOGCAT_CLEAR);
            if(clearProcessInfo == null)
            {
                return;
            }

            Process clearProcess = Process.Start(clearProcessInfo);
            clearProcess.WaitForExit();
        }

        public static bool StartLogCatProcess(string commands, DataReceivedEventHandler processDataHandler)
        {
            // 创建`adb logcat`进程
            ProcessStartInfo logProcessInfo = CreateProcessStartInfo(commands);
            if(logProcessInfo == null)
            {
                return false;
            }

            /// 执行adb进程
            StopLogCatProcess();
            logCatProcess = Process.Start(logProcessInfo);
            logCatProcess.ErrorDataReceived += processDataHandler;
            logCatProcess.OutputDataReceived += processDataHandler;
            logCatProcess.BeginErrorReadLine();
            logCatProcess.BeginOutputReadLine();

            return true;
        }

        public static bool StartLogCatProcess(DataReceivedEventHandler processDataHandler, string filter = null)
        {
            if (CheckDevice())
            {
                string commands = string.IsNullOrEmpty(filter) ? LOGCAT_ARGUMENTS : string.Format(LOGCAT_ARGUMENTS_WITH_FILTER, filter);
                ProcessStartInfo logProcessInfo = CreateProcessStartInfo(commands);
                if (logProcessInfo != null)
                {
                    /// 执行adb进程
                    logCatProcess = Process.Start(logProcessInfo);
                    logCatProcess.ErrorDataReceived += processDataHandler;
                    logCatProcess.OutputDataReceived += processDataHandler;
                    logCatProcess.BeginErrorReadLine();
                    logCatProcess.BeginOutputReadLine();
                    return true;
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Unity Debug Viewer", "Cannot detect any android device", "ok");
            }

            return false;
        }


        public static void StopLogCatProcess()
        {
            if (logCatProcess != null)
            {
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
        }

        public static bool StartForwardProcess(string pcPort, string phonePort)
        {
            if (CheckDevice())
            {
                if (String.IsNullOrEmpty(pcPort))
                {
                    pcPort = DEFAULT_PC_PORT;
                }

                if (String.IsNullOrEmpty(phonePort))
                {
                    phonePort = DEFAULT_PHONE_PORT;
                }

                string command = String.Format(START_ADB_FORWARD, pcPort, phonePort);
                ProcessStartInfo forwardInfo = CreateProcessStartInfo(command);
                if(forwardInfo != null)
                {
                    Process forwardProcess = Process.Start(forwardInfo);
                    forwardProcess.WaitForExit();
                    return true;
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Unity Debug Viewer", "Cannot detect any android device", "ok");
            }

            return false;
        }

        public static void StopForwardProcess()
        {
            ProcessStartInfo stopForwardInfo = CreateProcessStartInfo(STOP_ADB_FORWARD);
            if (stopForwardInfo == null)
            {
                return;
            }

            Process stopForwardProcess = Process.Start(stopForwardInfo);
            stopForwardProcess.WaitForExit();
        }

        public static bool CheckDevice()
        {
            ProcessStartInfo checkInfo = CreateProcessStartInfo(ADB_DEVICE_CHECK);
            if(checkInfo == null)
            {
                return false;
            }

            Process checkProcess = Process.Start(checkInfo);
            checkProcess.WaitForExit();

            StreamReader stdOutput = checkProcess.StandardOutput;
            stdOutput.ReadLine();
            if (!stdOutput.EndOfStream)
            {
                string deviceChecked = stdOutput.ReadLine();
                if (String.IsNullOrEmpty(deviceChecked))
                {
                    return false;
                }
                else
                {
                    deviceID = deviceChecked.Split('\t')[0];
                    return true;
                }
            }
            else
            {
                return false;
            }
        }

        private static ProcessStartInfo CreateProcessStartInfo(string command)
        {
            var adbPath = GetAdbPath();
            if (String.IsNullOrEmpty(adbPath))
            {
                EditorUtility.DisplayDialog("Unity Debug Viewer", "Cannot find adb", "ok");
                return null;
            }

            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                FileName = adbPath,
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = command
            };

            return processStartInfo;
        }

        private static string GetAdbPath()
        {
            string adbPath = string.Empty;
#if UNITY_2019_1_OR_NEWER
            ADB adb = ADB.GetInstance();
            if(abd != null)
            {
                adbPath = adb.GetADBPath();
            }
#else
            string androidSdkRoot = EditorPrefs.GetString("AndroidSdkRoot");
            if (!string.IsNullOrEmpty(androidSdkRoot))
            {
                adbPath = Path.Combine(androidSdkRoot, Path.Combine("platform-tools", "adb"));
            }
#endif
            return adbPath;
        }
    }
}
