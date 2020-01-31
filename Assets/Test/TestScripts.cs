using System.Text;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using System;

public class TestScripts : MonoBehaviour
{
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
}
