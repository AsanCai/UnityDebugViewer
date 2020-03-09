/// Copyright (C) 2020 AsanCai   
/// All rights reserved
/// Email: 969850420@qq.com

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
                    logCatProcess.OutputDataReceived += processDataHandler;
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
                    using (Process forwardProcess = Process.Start(forwardInfo))
                    {
                        using (StreamReader stdOutput = forwardProcess.StandardOutput)
                        {
                            if (!stdOutput.EndOfStream)
                            {
                                string output = stdOutput.ReadLine();
                                if (!String.IsNullOrEmpty(output))
                                {
                                    int port = 0;
                                    if (int.TryParse(output, out port))
                                    {
                                        UnityDebugViewerLogger.AddLog(string.Format("Start adb forward successfully at port: {0}", port), String.Empty, LogType.Log, UnityDebugViewerDefaultMode.ADBForward);
                                    }
                                    else
                                    {
                                        UnityDebugViewerLogger.AddLog(String.Format("[{0}] {1}", "Start adb forward process faild", output), String.Empty, LogType.Error, UnityDebugViewerDefaultMode.ADBForward);
                                        return false;
                                    }
                                }
                            }
                        }

                        using (StreamReader errorOutPut = forwardProcess.StandardError)
                        {
                            if (!errorOutPut.EndOfStream)
                            {
                                string output = errorOutPut.ReadLine();
                                if (!string.IsNullOrEmpty(output))
                                {
                                    UnityDebugViewerLogger.AddLog(String.Format("[{0}] {1}", "Start adb forward process faild", output), String.Empty, LogType.Error, UnityDebugViewerDefaultMode.ADBForward);
                                }

                                return false;
                            }
                        }
                    }
                    
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
            stopForwardProcess.Dispose();
        }

        public bool CheckDevice(string adbPath)
        {
            ProcessStartInfo checkInfo = CreateProcessStartInfo(UnityDebugViewerADBUtility.ADB_DEVICE_CHECK, adbPath);
            if(checkInfo == null)
            {
                return false;
            }

            using (Process checkProcess = Process.Start(checkInfo))
            {
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
                StandardErrorEncoding = Encoding.UTF8,
                FileName = adbPath,
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = command
            };

            return processStartInfo;
        }
    }
}
