using UnityEngine;
using System.Collections;

namespace MonkeyMind.TwoD
{
    [ExecuteInEditMode]
    public class SnapToGround : MonoBehaviour
    {

        //IMPORTANT: Only one of these may be true at a time
        public bool useRenderer = true;
        public bool useCollider = false;
        public bool useTransform = false;

        public bool autoSnap = false;
        public bool autoSnapEditor = true;

        public LayerMask snapLayer;

        [HideInInspector]
        public GameObject snappedToObject;

        void Update()
        {
            if (autoSnap)
            {
                Snap();
            }
        }

        public void Snap()
        {

            Vector3 start;

            Collider2D collide = GetComponent<Collider2D>();
            Renderer render = GetComponent<Renderer>();
            if (collide != null && useCollider)
            {
                start = collide.bounds.center;
                start += 0.05f * transform.up;
                RaycastHit2D rayHit = Physics2D.BoxCast(start, collide.bounds.extents * 2, 0, -1 * transform.up, 50f, snapLayer);
                if (rayHit)
                {
                    transform.position = new Vector3(rayHit.centroid.x - collide.offset.x, rayHit.centroid.y - collide.offset.y, transform.position.z);
                    snappedToObject = rayHit.collider.gameObject;
                }
                else {
                    snappedToObject = null;
                }
            }
            else if (render != null && useRenderer)
            {
                start = render.bounds.center;
                start += 0.05f * transform.up;
                RaycastHit2D rayHit = Physics2D.Raycast(start, -1 * transform.up, 50f, snapLayer);
                if (rayHit)
                {
                    transform.position = new Vector3(rayHit.point.x, rayHit.point.y + render.bounds.extents.y, transform.position.z);
                    snappedToObject = rayHit.collider.gameObject;
                }
                else {
                    snappedToObject = null;
                }
            }
            else if (useTransform)
            {
                start = transform.position;
                start += 0.05f * transform.up;
                RaycastHit2D rayHit = Physics2D.Raycast(start, -1 * transform.up, 50f, snapLayer);
                if (rayHit)
                {
                    transform.position = new Vector3(rayHit.point.x, rayHit.point.y, transform.position.z);
                    snappedToObject = rayHit.collider.gameObject;
                }
                else {
                    snappedToObject = null;
                }
            }

            if (GetComponent<FollowPseudoParent>() != null)
            {
                GetComponent<FollowPseudoParent>().PseudoParent = snappedToObject;
            }
        }
    }
}
