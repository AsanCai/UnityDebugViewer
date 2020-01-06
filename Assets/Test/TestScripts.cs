using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestScripts : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}

    float timer = 0f;
	void Update () {
        timer += Time.deltaTime;
        if(timer >= 1)
        {
            Debug.Log("pass 1s;");
            Debug.LogError("pass 1s;");
            Debug.LogWarning("pass 1s;");

            timer = 0f;
        }
	}
}
