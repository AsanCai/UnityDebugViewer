using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditorInternal;

namespace UnityDebugViewer
{
    /// <summary>
    /// 为UnityDebugViewerWindow提供与Editor相关的操作
    /// </summary>
    public static class UnityDebugViewerEditorUtility
    {
        public const char UnityInternalDirectorySeparator = '/';
        public const string EllipsisStr = "........";
        public const int DisplayLineNumber = 8;

        public static bool JumpToSource(string filePath, int lineNumber)
        {
            var validFilePath = GetSystemFilePath(filePath);
            if (File.Exists(validFilePath))
            {
                if(InternalEditorUtility.OpenFileAtLineExternal(validFilePath, lineNumber))
                {
                    return true;
                }
            }

            return false;
        }

        public static string GetSourceContent(string filePath, int lineNumber)
        {
            var validFilePath = GetSystemFilePath(filePath);
            if (!File.Exists(validFilePath))
            {
                return string.Empty;
            }

            var lineArray = File.ReadAllLines(validFilePath);

            int fileLineNumber = lineNumber - 1;
            int firstLine = Mathf.Max(fileLineNumber - DisplayLineNumber / 2, 0);
            int lastLine = Mathf.Min(fileLineNumber + DisplayLineNumber / 2 + 1, lineArray.Count());

            string souceContent = string.Empty;
            if(firstLine != 0)
            {
                souceContent = string.Format("{0}\n{1}", EllipsisStr, souceContent);
            }
            for(int index = firstLine;index < lastLine;index++)
            {
                string str = ReplaceTabWithSpace(lineArray[index]) + "\n";
                if(index == fileLineNumber)
                {
                    str = string.Format("<color=#ff0000ff>{0}</color>", str);
                }

                souceContent += str;
            }
            if(lastLine != lineArray.Count())
            {
                souceContent = string.Format("{0}\n{1}", souceContent, EllipsisStr);
            }

            return souceContent;
        }

        public static string GetSystemFilePath(string filePath)
        {
            string systemFilePath = filePath.Replace(UnityInternalDirectorySeparator, Path.DirectorySeparatorChar);
            systemFilePath = Path.Combine(Directory.GetCurrentDirectory(), systemFilePath);
            return systemFilePath;
        }

        /// <summary>
        /// 使用四个空格代替Tab
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static string ReplaceTabWithSpace(string str)
        {
            return str.Replace("\t", "\b\b\b\b");
        }
    }
}
