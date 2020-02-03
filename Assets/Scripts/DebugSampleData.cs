using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityDebugViewer
{
    public class DebugSampleData
    {
        public float time;
        public float memory;
        public float fps;
        public string fpsText;
        public static float MemorySize()
        {
            float s = sizeof(float) + sizeof(byte) + sizeof(float) + sizeof(float);
            return s;
        }
    }
}
