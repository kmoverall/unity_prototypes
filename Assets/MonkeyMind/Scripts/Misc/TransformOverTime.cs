using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TransformOverTime : MonoBehaviour {

    public float forTime;
    float timeSoFar = 0;

    public Vector3 translation;
    public Vector3 rotation;

	void Update () {
        timeSoFar += Time.deltaTime;
        if (forTime == 0 || timeSoFar <= forTime)
        {
            transform.Translate(translation * Time.deltaTime);
            transform.Rotate(rotation * Time.deltaTime);
        }
	}
}
