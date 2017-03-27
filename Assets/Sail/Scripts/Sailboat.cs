using UnityEngine;
using Rewired;
using System.Collections;
using System.Collections.Generic;

public class Sailboat : MonoBehaviour {

    [SerializeField]
    Vector2 wind = Vector3.left;
    [SerializeField]
    float maxLiftForce = 1;
    [SerializeField]
    float maxDragForce = 1;

    [SerializeField]
    int playerID = 0;
    Player player;

    [SerializeField]
    Transform sailHinge;

    Vector2 bearing = Vector2.zero;
    float sailOpen;

	void Awake () {
        player = ReInput.players.GetPlayer(playerID);
	}
	
	void Update () {
        bearing = player.GetAxis2D("Steer Horizontal", "Steer Vertical");
        bearing.Normalize();
        sailOpen = player.GetAxis("Sail Axis");
        
        GetComponent<Rigidbody2D>().MoveRotation(Mathf.Atan2(bearing.y, bearing.x) * Mathf.Rad2Deg);
        sailHinge.localRotation = Quaternion.Euler(0, 0, sailOpen * 90);
    }

    void FixedUpdate()
    {
        Vector2 v = GetComponent<Rigidbody2D>().velocity;
        v = wind - v;

        float windAngle = Mathf.Atan2(wind.y, wind.x) * Mathf.Rad2Deg;
        float attackAngle = Mathf.Abs(sailHinge.rotation.eulerAngles.z - windAngle);

        Vector2 liftDir = new Vector2(-v.y, v.x);
        liftDir.Normalize();

        if (attackAngle > 90)
            attackAngle = 180 - attackAngle;

        float liftMag = Mathf.Lerp(maxLiftForce, 0, attackAngle / 90) * v.magnitude * v.magnitude;
        float dragMag = Mathf.Lerp(0, maxDragForce, attackAngle / 90) * v.magnitude * v.magnitude;

        //Vector2 force = dragMag * v.normalized;

        Debug.DrawRay(transform.position, dragMag * v.normalized, Color.red);
        Debug.DrawRay(transform.position, liftMag * liftDir, Color.green);
    }
}
