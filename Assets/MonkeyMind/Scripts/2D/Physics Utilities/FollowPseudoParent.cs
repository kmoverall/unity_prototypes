using UnityEngine;
using System.Collections;

namespace MonkeyMind.TwoD
{ 
    public class FollowPseudoParent : MonoBehaviour {

        [ReadOnly]
        private VelocityTracker pseudoParent;
        public GameObject PseudoParent {
            get { return pseudoParent.gameObject; }
            set {
                if (value != null)
                    pseudoParent = value.GetComponent<VelocityTracker>();
                else
                    pseudoParent = null; 
            }
        }
	
	    // Update is called once per frame
	    void FixedUpdate () {
            if (pseudoParent != null) {
                gameObject.transform.Translate(pseudoParent.Velocity * Time.fixedDeltaTime);
                float angle = 0f;
                Vector3 axis = Vector3.zero;
                pseudoParent.AngularVelocity.ToAngleAxis(out angle, out axis);
                gameObject.transform.RotateAround(pseudoParent.transform.position, axis, angle * Time.fixedDeltaTime);
            }
	    }
    }
}
