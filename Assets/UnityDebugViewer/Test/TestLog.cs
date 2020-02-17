using System.Text;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityDebugViewer;

public class TestLog : MonoBehaviour
{
    string info = string.Empty;
    float timer = 0f;
    private void Awake()
    {
        for (int i = 0; i < 5000; i++)
        {
            Debug.LogFormat("pass {0}s;", i);
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
