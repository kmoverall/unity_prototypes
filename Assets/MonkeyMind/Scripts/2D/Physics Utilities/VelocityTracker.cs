using UnityEngine;
using System.Collections;

namespace MonkeyMind.TwoD
{
    public class VelocityTracker : MonoBehaviour {

        Vector3 prevPosition;
        Quaternion prevRotation;
        Vector3 velocity;
        Quaternion angularVelocity;

        bool wasUpdatedThisFrame = false;

        public Vector3 Velocity {
            get { UpdateVelocity(); return velocity; }
        }
        public Quaternion AngularVelocity {
            get { UpdateVelocity(); return angularVelocity; }
        }

        // Use this for initialization
        void Start() {
            prevPosition = transform.position;
            prevRotation = transform.rotation;
        }

        // Update is called once per frame
        void FixedUpdate() {
            UpdateVelocity();
        }

        void UpdateVelocity() {
            if (!wasUpdatedThisFrame)
            {
                velocity = (transform.position - prevPosition) / Time.fixedDeltaTime;
                angularVelocity = Quaternion.SlerpUnclamped(Quaternion.identity, Quaternion.Inverse(prevRotation) * transform.rotation, 1 / Time.fixedDeltaTime);
                prevPosition = transform.position;
                prevRotation = transform.rotation;
                wasUpdatedThisFrame = true;
                StartCoroutine("ClearVelocityUpdate");
            }
        }

        IEnumerator ClearVelocityUpdate() {
            yield return new WaitForFixedUpdate();
            wasUpdatedThisFrame = false;
            yield break;
        }
    }
}
