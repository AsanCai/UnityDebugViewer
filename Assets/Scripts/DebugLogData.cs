using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityDebugViewer
{
    public class DebugLogData
    {
        public int count = 1;
        public LogType logType;
        public string condition;
        public string stacktrace;
        public int sampleId;

        public DebugLogData Clone()
        {
            return (DebugLogData)this.MemberwiseClone();
        }

        public float MemorySize()
        {
            return (float)(sizeof(int) +
                    sizeof(LogType) +
                    condition.Length * sizeof(char) +
                    stacktrace.Length * sizeof(char) +
                    sizeof(int));
        }
    }
}
