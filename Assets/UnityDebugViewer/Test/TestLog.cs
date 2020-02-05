using System.Text;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityDebugViewer;

public class TestLog : MonoBehaviour
{
    string info = string.Empty;
    float timer = 0f;
    void Update()
    {
        GenerateLog(Time.deltaTime);
    }

    void GenerateLog(float deltaTime)
    {
        timer += deltaTime;
        if (timer >= 1)
        {
            UnityDebugViewerLogger.Log("pass 1s;");
            Debug.LogFormat("pass {0}s;", 1);

            timer = 0f;
        }
    }
}
