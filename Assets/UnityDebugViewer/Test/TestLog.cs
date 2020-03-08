using UnityEngine;

namespace UnityDebugViewer
{
    public class TestLog : MonoBehaviour
    {
        float timer = 0f;
        private void Awake()
        {
            for (int i = 0; i < 5000; i++)
            {
                Debug.LogFormat("Test Log {0};", i);
            }
        }

        void Update()
        {
            GenerateLog(Time.deltaTime);
        }

        void GenerateLog(float deltaTime)
        {
            timer += deltaTime;
            if (timer >= 1)
            {
                UnityDebugViewerLogger.LogWarning("pass 1s;");
                Debug.LogWarningFormat("pass {0}s;", 1);

                timer = 0f;
            }
        }
    }
}
