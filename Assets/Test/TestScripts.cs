using System.Text;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityDebugViewer;

public class TestScripts : MonoBehaviour
{
    string info = string.Empty;
    float timer = 0f;
    void Update()
    {
        TestLog(Time.deltaTime);
    }

    void TestLog(float deltaTime)
    {
        timer += deltaTime;
        if (timer >= 1)
        {
            UnityDebugViewerLogger.Log("logger: pass 1s");
            Debug.LogFormat("pass {0}s;", 1);

            timer = 0f;
        }
    }
}
