using System.Text;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class TestScripts : MonoBehaviour {

    private void Awake()
    {
        Application.logMessageReceivedThreaded += CaptureLogThread;
    }

    private void OnDestroy()
    {
        Application.logMessageReceivedThreaded -= CaptureLogThread;
        if(socket != null)
        {
            socket.Close();
        }
    }

    string info = string.Empty;
    float timer = 0f;
	void Update () {
        timer += Time.deltaTime;
        if(timer >= 1)
        {
            Debug.Log("pass 1s;");

            timer = 0f;
        }
	}

    private void OnGUI()
    {
        if (GUILayout.Button("打开链接"))
        {
            CreateSocket();
        }

        if (GUILayout.Button("断开链接"))
        {
            if(socket != null)
            {
                socket.Close(); 
            }
        }

        GUILayout.Label(info);
    }

    private Socket socket;
    private void CreateSocket()
    {
        if(socket != null)
        {
            return;
        }

        int port = 5000;
        string host = "127.0.0.1";
        IPAddress ip = IPAddress.Parse(host);

        IPEndPoint ipe = new IPEndPoint(ip, port);

        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Connect(ipe);
    }

    void CaptureLogThread(string condition, string stacktrace, UnityEngine.LogType type)
    {
        if (type == LogType.Error)
        {
            return;
        }

        //CreateSocket();

        if(socket != null)
        {
            try
            {
                info = condition + stacktrace;
                byte[] bs = Encoding.UTF8.GetBytes(info);
                socket.Send(bs);
            }
            catch (SocketException e)
            {
                Debug.LogError(e.Message);
            }
                
        }

        //socket.Close();
    }
}
