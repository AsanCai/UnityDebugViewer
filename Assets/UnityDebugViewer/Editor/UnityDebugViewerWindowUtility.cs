﻿using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace UnityDebugViewer
{
    public static class UnityDebugViewerWindowUtility 
    {
        private static string adbPath = string.Empty;

        public static bool JumpToSource(LogData log)
        {
            if (log != null)
            {
                for (int i = 0; i < log.stackList.Count; i++)
                {
                    var stack = log.stackList[i];
                    if (stack == null)
                    {
                        continue;
                    }

                    if (JumpToSource(stack))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool JumpToSource(LogStackData stack)
        {
            if (stack == null)
            {
                return false;
            }
            else
            {
                return JumpToSource(stack.filePath, stack.lineNumber);
            }
        }

        public static bool JumpToSource(string filePath, int lineNumber)
        {
            var validFilePath = UnityDebugViewerEditorUtility.ConvertToSystemFilePath(filePath);
            if (File.Exists(validFilePath))
            {
                if (InternalEditorUtility.OpenFileAtLineExternal(validFilePath, lineNumber))
                {
                    return true;
                }
            }

            return false;
        }

        public static void ClearNativeConsoleWindow()
        {
            Assembly unityEditorAssembly = Assembly.GetAssembly(typeof(EditorWindow));
            Type logEntriesType = unityEditorAssembly.GetType("UnityEditor.LogEntries");
            if (logEntriesType == null)
            {
                logEntriesType = unityEditorAssembly.GetType("UnityEditorInternal.LogEntries");
                if (logEntriesType == null)
                {
                    return;
                }
            }

            object logEntriesInstance = Activator.CreateInstance(logEntriesType);
            var clearMethod = logEntriesType.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (clearMethod != null)
            {
                clearMethod.Invoke(logEntriesInstance, null);
            }

            var getCountMethod = logEntriesType.GetMethod("GetCount", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (getCountMethod == null)
            {
                return;
            }

            int count = (int)getCountMethod.Invoke(logEntriesInstance, null);
            if (count > 0)
            {
                Type logEntryType = unityEditorAssembly.GetType("UnityEditor.LogEntry");
                if (logEntryType == null)
                {
                    logEntryType = unityEditorAssembly.GetType("UnityEditorInternal.LogEntry");
                    if (logEntryType == null)
                    {
                        return;
                    }
                }
                object logEntryInstacne = Activator.CreateInstance(logEntryType);

                var startGettingEntriesMethod = logEntriesType.GetMethod("StartGettingEntries", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                var endGettingEntriesMethod = logEntriesType.GetMethod("EndGettingEntries", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                var getEntryInternalMethod = logEntriesType.GetMethod("GetEntryInternal", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (startGettingEntriesMethod == null || endGettingEntriesMethod == null || getEntryInternalMethod == null)
                {
                    return;
                }

                var infoFieldInfo = logEntryType.GetField("message", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (infoFieldInfo == null)
                {
                    infoFieldInfo = logEntryType.GetField("condition", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (infoFieldInfo == null)
                    {
                        return;
                    }
                }

                string info;
                startGettingEntriesMethod.Invoke(logEntriesInstance, null);
                for (int i = 0; i < count; i++)
                {
                    getEntryInternalMethod.Invoke(logEntriesInstance, new object[] { i, logEntryInstacne });
                    if (logEntryInstacne == null)
                    {
                        continue;
                    }

                    info = infoFieldInfo.GetValue(logEntryInstacne).ToString();
                    UnityDebugViewerLogger.AddLog(info, string.Empty, LogType.Error, UnityDebugViewerEditorType.Editor);
                }
                endGettingEntriesMethod.Invoke(logEntriesInstance, null);
            }
        }

        public static string GetAdbPath()
        {
            if (!String.IsNullOrEmpty(adbPath))
            {
                return adbPath;
            }

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

            if (string.IsNullOrEmpty(adbPath))
            {
                MonoScript ms = MonoScript.FromScriptableObject(UnityDebugViewerADBUtility.GetADBInstance());
                string filePath = AssetDatabase.GetAssetPath(ms);
                filePath = UnityDebugViewerEditorUtility.ConvertToSystemFilePath(filePath);

                string currentScriptDirectory = Path.GetDirectoryName(filePath);
                string parentDirectory = Directory.GetParent(currentScriptDirectory).FullName;

                string defaultAdbPath = UnityDebugViewerEditorUtility.ConvertToSystemFilePath(UnityDebugViewerADBUtility.DEFAULT_ADB_PATH);

                adbPath = Path.Combine(Path.Combine(parentDirectory, defaultAdbPath), "adb");
            }

            return adbPath;
        }
    }
}