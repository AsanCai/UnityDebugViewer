/// Copyright (C) 2020 AsanCai   
/// All rights reserved
/// Email: 969850420@qq.com

using System.Diagnostics;
using UnityEngine;

namespace UnityDebugViewer
{
    public static class UnityDebugViewerADBUtility
    {
        public const string DEFAULT_FORWARD_PC_PORT = "50000";
        public const string DEFAULT_FORWARD_PHONE_PORT = "50000";
        public const string DEFAULT_ADB_PATH = "ADB";

        public const string LOGCAT_CLEAR = "logcat -c";
        public const string LOGCAT_ARGUMENTS = "logcat -v time";
        public const string LOGCAT_ARGUMENTS_WITH_FILTER = "logcat -v time -s {0}";
        public const string ADB_DEVICE_CHECK = "devices";
        public const string START_ADB_FORWARD = "forward tcp:{0} tcp:{1}";
        public const string STOP_ADB_FORWARD = "forward --remove-all";

        private static UnityDebugViewerADB adbInstance;
        public static UnityDebugViewerADB GetADBInstance()
        {
            if(adbInstance == null)
            {
                adbInstance = ScriptableObject.CreateInstance<UnityDebugViewerADB>();
            }

            return adbInstance;
        }

        public static void RunClearCommand(string adbPath)
        {
            GetADBInstance().RunClearCommand(adbPath);
        }

        public static bool StartLogcatProcess(DataReceivedEventHandler processDataHandler, string filter, string adbPath)
        {
            return GetADBInstance().StartLogcatProcess(processDataHandler, filter, adbPath);
        }

        public static void StopLogCatProcess()
        {
            GetADBInstance().StopLogcatProcess();
        }

        public static bool StartForwardProcess(string pcPort, string phonePort, string adbPath)
        {
            return GetADBInstance().StartForwardProcess(pcPort, phonePort, adbPath);
        }

        public static void StopForwardProcess(string adbPath)
        {
            GetADBInstance().StopForwardProcess(adbPath);
        }

        public static bool CheckDevice(string adbPath)
        {
            return GetADBInstance().CheckDevice(adbPath);
        }
    }
}
