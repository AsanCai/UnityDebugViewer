using System;
using System.IO;
using System.Diagnostics;
using System.Text;
using UnityEngine;

namespace UnityDebugViewer
{
    public class UnityDebugViewerADB : ScriptableObject
    {
        public string deviceID { get; private set; }
        private Process logCatProcess;

        public void RunClearCommand(string adbPath)
        {
            // 使用`adb logcat -c`清理log buffer
            ProcessStartInfo clearProcessInfo = CreateProcessStartInfo(UnityDebugViewerADBUtility.LOGCAT_CLEAR, adbPath);
            if(clearProcessInfo == null)
            {
                return;
            }

            Process clearProcess = Process.Start(clearProcessInfo);
            clearProcess.WaitForExit();
        }

        public bool StartLogcatProcess(DataReceivedEventHandler processDataHandler, string filter, string adbPath)
        {
            /// stop first
            StopLogcatProcess();

            if (CheckDevice(adbPath))
            {
                string commands = string.IsNullOrEmpty(filter) ? UnityDebugViewerADBUtility.LOGCAT_ARGUMENTS : string.Format(UnityDebugViewerADBUtility.LOGCAT_ARGUMENTS_WITH_FILTER, filter);

                ProcessStartInfo logProcessInfo = CreateProcessStartInfo(commands, adbPath);
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

            return false;
        }


        public void StopLogcatProcess()
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
                finally
                {
                    logCatProcess.Dispose();
                    logCatProcess = null;
                }
            }
        }

        public bool StartForwardProcess(string pcPort, string phonePort, string adbPath)
        {
            /// stop first
            StopForwardProcess(adbPath);

            if (CheckDevice(adbPath))
            {
                if (String.IsNullOrEmpty(pcPort))
                {
                    pcPort = UnityDebugViewerADBUtility.DEFAULT_FORWARD_PC_PORT;
                }

                if (String.IsNullOrEmpty(phonePort))
                {
                    phonePort = UnityDebugViewerADBUtility.DEFAULT_FORWARD_PHONE_PORT;
                }

                string command = String.Format(UnityDebugViewerADBUtility.START_ADB_FORWARD, pcPort, phonePort);
                ProcessStartInfo forwardInfo = CreateProcessStartInfo(command, adbPath);
                if(forwardInfo != null)
                {
                    Process forwardProcess = Process.Start(forwardInfo);
                    forwardProcess.WaitForExit();
                    return true;
                }
            }

            return false;
        }

        public void StopForwardProcess(string adbPath)
        {
            ProcessStartInfo stopForwardInfo = CreateProcessStartInfo(UnityDebugViewerADBUtility.STOP_ADB_FORWARD, adbPath);
            if (stopForwardInfo == null)
            {
                return;
            }

            Process stopForwardProcess = Process.Start(stopForwardInfo);
            stopForwardProcess.WaitForExit();
        }

        public bool CheckDevice(string adbPath)
        {
            ProcessStartInfo checkInfo = CreateProcessStartInfo(UnityDebugViewerADBUtility.ADB_DEVICE_CHECK, adbPath);
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


        private ProcessStartInfo CreateProcessStartInfo(string command, string adbPath)
        {
            if (String.IsNullOrEmpty(adbPath))
            {
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
    }
}
