﻿//#define DEBUG_CC2D_RAYS
using UnityEngine;
using System;
using System.Collections.Generic;

namespace MonkeyMind.TwoD
{
    [RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D))]
    public class CharacterController2D : MonoBehaviour {
        #region internal types

        struct CharacterRaycastOrigins {
            public Vector3 topLeft;
            public Vector3 bottomRight;
            public Vector3 bottomLeft;

            public override string ToString() { return "Top Left: " + topLeft + " | Bottom Right: " + bottomRight + " | Bottom Left: " + bottomLeft; }
        }

        public class CharacterCollisionState2D {
            public bool right;
            public bool left;
            public bool above;
            public bool below;
            public bool becameGroundedThisFrame;
            public bool wasGroundedLastFrame;
            public bool movingDownSlope;
            public float slopeAngle;


            public bool hasCollision() {
                return below || right || left || above;
            }


            public void reset() {
                right = left = above = below = becameGroundedThisFrame = movingDownSlope = false;
                slopeAngle = 0f;
            }


            public override string ToString() {
                return string.Format("[CharacterCollisionState2D] r: {0}, l: {1}, a: {2}, b: {3}, movingDownSlope: {4}, angle: {5}, wasGroundedLastFrame: {6}, becameGroundedThisFrame: {7}",
                                        right, left, above, below, movingDownSlope, slopeAngle, wasGroundedLastFrame, becameGroundedThisFrame);
            }
        }

        #endregion


        #region events, properties and fields

        public event Action<RaycastHit2D> onControllerCollidedEvent;
        public event Action<Collider2D> onTriggerEnterEvent;
        public event Action<Collider2D> onTriggerStayEvent;
        public event Action<Collider2D> onTriggerExitEvent;


        /// <summary>
        /// when true, one way platforms will be ignored when moving vertically for a single frame
        /// </summary>
        public bool ignoreOneWayPlatformsThisFrame;

        [SerializeField]
        [Range(0.001f, 0.3f)]
        float _skinWidth = 0.02f;

        /// <summary>
        /// defines how far in from the edges of the collider rays are cast from. If cast with a 0 extent it will often result in ray hits that are
        /// not desired (for example a foot collider casting horizontally from directly on the surface can result in a hit)
        /// </summary>
        public float skinWidth
        {
            get { return _skinWidth; }
            set {
                _skinWidth = value;
                recalculateDistanceBetweenRays();
            }
        }


        /// <summary>
        /// mask with all layers that the player should interact with
        /// </summary>
        public LayerMask platformMask = 0;

        /// <summary>
        /// mask with all layers that trigger events should fire when intersected
        /// </summary>
        public LayerMask triggerMask = 0;

        /// <summary>
        /// mask with all layers that should act as one-way platforms. Note that one-way platforms should always be EdgeCollider2Ds. This is because it does not support being
        /// updated anytime outside of the inspector for now.
        /// </summary>
        [SerializeField]
        LayerMask oneWayPlatformMask = 0;

        /// <summary>
        /// the max slope angle that the CC2D can climb
        /// </summary>
        /// <value>The slope limit.</value>
        [Range(0f, 90f)]
        public float slopeLimit = 45f;

        /// <summary>
        /// the threshold in the change in vertical movement between frames that constitutes jumping
        /// </summary>
        /// <value>The jumping threshold.</value>
        public float jumpingThreshold = 0.07f;
        [HideInInspector]
        public bool isJumping = false;


        /// <summary>
        /// curve for multiplying speed based on slope (negative = down slope and positive = up slope)
        /// </summary>
        public AnimationCurve slopeSpeedMultiplier = new AnimationCurve(new Keyframe(-90f, 1.5f), new Keyframe(0f, 1f), new Keyframe(90f, 0f));

        [Range(2, 20)]
        public int totalHorizontalRays = 8;
        [Range(2, 20)]
        public int totalVerticalRays = 4;


        /// <summary>
        /// this is used to calculate the downward ray that is cast to check for slopes. We use the somewhat arbitrary value 75 degrees
        /// to calculate the length of the ray that checks for slopes.
        /// </summary>
        float _slopeLimitTangent = Mathf.Tan(75f * Mathf.Deg2Rad);


        [HideInInspector]
        [NonSerialized]
        public new Transform transform;
        [HideInInspector]
        [NonSerialized]
        public BoxCollider2D boxCollider;
        [HideInInspector]
        [NonSerialized]
        public Rigidbody2D rigidBody2D;

        [HideInInspector]
        [NonSerialized]
        public CharacterCollisionState2D collisionState = new CharacterCollisionState2D();
        [HideInInspector]
        [NonSerialized]
        public Vector3 velocity;
        public bool isGrounded { get { return collisionState.below; } }

        const float kSkinWidthFloatFudgeFactor = 0.001f;

        #endregion


        /// <summary>
        /// holder for our raycast origin corners (TR, TL, BR, BL)
        /// </summary>
        CharacterRaycastOrigins _raycastOrigins;

        /// <summary>
        /// stores our raycast hit during movement
        /// </summary>
        RaycastHit2D _raycastHit;

        /// <summary>
        /// stores any raycast hits that occur this frame. we have to store them in case we get a hit moving
        /// horizontally and vertically so that we can send the events after all collision state is set
        /// </summary>
        List<RaycastHit2D> _raycastHitsThisFrame = new List<RaycastHit2D>(2);
        HashSet<VelocityTracker> _belowVelocities = new HashSet<VelocityTracker>();
        Collider2D _attachedToCollider = null;
        bool _releasedColliderThisFrame = false;
        public bool ReleasedColliderThisFrame
        {
            get { return _releasedColliderThisFrame; }
        }

        // horizontal/vertical movement data
        float _verticalDistanceBetweenRays;
        float _horizontalDistanceBetweenRays;

        // we use this flag to mark the case where we are travelling up a slope and we modified our delta.y to allow the climb to occur.
        // the reason is so that if we reach the end of the slope we can make an adjustment to stay grounded
        bool _isGoingUpSlope = false;


        #region Monobehaviour

        void Awake() {
            // add our one-way platforms to our normal platform mask so that we can land on them from above
            platformMask |= oneWayPlatformMask;

            // cache some components
            transform = GetComponent<Transform>();
            boxCollider = GetComponent<BoxCollider2D>();
            rigidBody2D = GetComponent<Rigidbody2D>();

            // here, we trigger our properties that have setters with bodies
            skinWidth = _skinWidth;

            // we want to set our CC2D to ignore all collision layers except what is in our triggerMask
            for (var i = 0; i < 32; i++) {
                // see if our triggerMask contains this layer and if not ignore it
                if ((triggerMask.value & 1 << i) == 0)
                    Physics2D.IgnoreLayerCollision(gameObject.layer, i);
            }
        }


        public void OnTriggerEnter2D(Collider2D col) {
            if (onTriggerEnterEvent != null)
                onTriggerEnterEvent(col);
        }


        public void OnTriggerStay2D(Collider2D col) {
            if (onTriggerStayEvent != null)
                onTriggerStayEvent(col);
        }


        public void OnTriggerExit2D(Collider2D col) {
            if (onTriggerExitEvent != null)
                onTriggerExitEvent(col);
        }

        #endregion


        //[System.Diagnostics.Conditional("DEBUG_CC2D_RAYS")]
        void DrawRay(Vector3 start, Vector3 dir, Color color) {
            Debug.DrawRay(start, dir, color);
        }


        #region Public

        /// <summary>
        /// attempts to move the character to position + deltaMovement. Any colliders in the way will cause the movement to
        /// stop when run into.
        /// </summary>
        /// <param name="deltaMovement">Delta movement.</param>
        public void move(Vector3 deltaMovement) {
            // save off our current grounded state which we will use for wasGroundedLastFrame and becameGroundedThisFrame
            collisionState.wasGroundedLastFrame = collisionState.below;

            // clear our state
            collisionState.reset();
            _raycastHitsThisFrame.Clear();
            _isGoingUpSlope = false;
            _releasedColliderThisFrame = false;

            primeRaycastOrigins();
            _belowVelocities.Clear();

            if (_attachedToCollider != null && _attachedToCollider.GetComponent<VelocityTracker>() != null) {
                deltaMovement = _attachedToCollider.GetComponent<VelocityTracker>().Velocity * Time.deltaTime;
            }
            else if (_attachedToCollider != null) {
                deltaMovement = Vector3.zero;
            }

            //If something is pushing into the character, push the character
            correctSkinPierce(ref deltaMovement, Vector2.left);
            correctSkinPierce(ref deltaMovement, Vector2.right);
            correctSkinPierce(ref deltaMovement, Vector2.down);
            correctSkinPierce(ref deltaMovement, Vector2.up);

            // first, we check for a slope below us before moving
            // only check slopes if we are going down and grounded
            if (deltaMovement.y < 0f)
                handleVerticalSlope(ref deltaMovement);

            // now we check movement in the horizontal dir
            if (deltaMovement.x != 0f)
                moveHorizontally(ref deltaMovement);

            // next, check movement in the vertical dir
            if (deltaMovement.y != 0f)
                moveVertically(ref deltaMovement);

            //Add the average of the velocity of all objects the player is standing on that are tracking their own velocity
            foreach (VelocityTracker plform in _belowVelocities) {
                deltaMovement += plform.Velocity * Time.deltaTime / _belowVelocities.Count;
            }

            // move then update our state
            deltaMovement.z = 0;
            transform.Translate(deltaMovement, Space.World);

            // only calculate velocity if we have a non-zero deltaTime
            if (Time.deltaTime > 0f)
                velocity = deltaMovement / Time.deltaTime;

            // set our becameGrounded state based on the previous and current collision state
            if (!collisionState.wasGroundedLastFrame && collisionState.below)
                collisionState.becameGroundedThisFrame = true;

            // if we are going up a slope we artificially set a y velocity so we need to zero it out here
            if (_isGoingUpSlope)
                velocity.y = 0;

            // send off the collision events if we have a listener
            if (onControllerCollidedEvent != null) {
                for (var i = 0; i < _raycastHitsThisFrame.Count; i++)
                    onControllerCollidedEvent(_raycastHitsThisFrame[i]);
            }

            ignoreOneWayPlatformsThisFrame = false;
        }

        public void attachToCollider(Collider2D other) {
            _attachedToCollider = other;
        }

        public void detachFromCollider() {
            _attachedToCollider = null;
            _releasedColliderThisFrame = true;
        }


        /// <summary>
        /// moves directly down until grounded
        /// </summary>
        public void warpToGrounded() {
            do {
                move(new Vector3(0, -1f, 0));
            } while (!isGrounded);
        }


        /// <summary>
        /// this should be called anytime you have to modify the BoxCollider2D at runtime. It will recalculate the distance between the rays used for collision detection.
        /// It is also used in the skinWidth setter in case it is changed at runtime.
        /// </summary>
        public void recalculateDistanceBetweenRays() {
            // figure out the distance between our rays in both directions
            // horizontal
            var colliderUseableHeight = boxCollider.size.y * Mathf.Abs(transform.localScale.y) - (2f * _skinWidth);
            _verticalDistanceBetweenRays = colliderUseableHeight / (totalHorizontalRays - 1);

            // vertical
            var colliderUseableWidth = boxCollider.size.x * Mathf.Abs(transform.localScale.x) - (2f * _skinWidth);
            _horizontalDistanceBetweenRays = colliderUseableWidth / (totalVerticalRays - 1);
        }

        #endregion


        #region Movement Methods

        /// <summary>
        /// resets the raycastOrigins to the current extents of the box collider inset by the skinWidth. It is inset
        /// to avoid casting a ray from a position directly touching another collider which results in wonky normal data.
        /// </summary>
        /// <param name="futurePosition">Future position.</param>
        /// <param name="deltaMovement">Delta movement.</param>
        void primeRaycastOrigins() {
            // our raycasts need to be fired from the bounds inset by the skinWidth
            var modifiedBounds = boxCollider.bounds;
            modifiedBounds.Expand(-2f * _skinWidth);

            _raycastOrigins.topLeft = new Vector2(modifiedBounds.min.x, modifiedBounds.max.y);
            _raycastOrigins.bottomRight = new Vector2(modifiedBounds.max.x, modifiedBounds.min.y);
            _raycastOrigins.bottomLeft = modifiedBounds.min;
        }

        // Force player controller away from object if they are piercing the controller's skin
        void correctSkinPierce(ref Vector3 deltaMovement, Vector2 direction) {
            var rayDistance = 0.0f;
            if (direction == Vector2.right || direction == Vector2.left) {
                rayDistance = boxCollider.bounds.extents.x;
            }
            else if (direction == Vector2.up || direction == Vector2.down) {
                rayDistance = boxCollider.bounds.extents.y;
            }

            var pushDistance = 0.0f;
            var pushVelocity = Vector3.zero;
            Vector3 initialRayOrigin;
            if (direction == Vector2.right) {
                initialRayOrigin = _raycastOrigins.bottomRight - (rayDistance - _skinWidth) * Vector3.right;
            }
            else if (direction == Vector2.left) {
                initialRayOrigin = _raycastOrigins.bottomLeft - (rayDistance - _skinWidth) * Vector3.left;
            }
            else if (direction == Vector2.down) {
                initialRayOrigin = _raycastOrigins.bottomLeft - (rayDistance - _skinWidth) * Vector3.down;
            }
            else {
                initialRayOrigin = _raycastOrigins.topLeft - (rayDistance - _skinWidth) * Vector3.up;
            }

            //Handle horizontal piercing
            if (direction == Vector2.right || direction == Vector2.left) {
                for (var i = 0; i < totalHorizontalRays; i++) {
                    var ray = new Vector2(initialRayOrigin.x, initialRayOrigin.y + i * _verticalDistanceBetweenRays);

                    DrawRay(ray, direction * rayDistance, Color.red);

                    // if we are grounded we will include oneWayPlatforms only on the first ray (the bottom one). this will allow us to
                    // walk up sloped oneWayPlatforms
                    if (i == 0 && collisionState.wasGroundedLastFrame)
                        _raycastHit = Physics2D.Raycast(ray, direction, rayDistance, platformMask);
                    else
                        _raycastHit = Physics2D.Raycast(ray, direction, rayDistance, platformMask & ~oneWayPlatformMask);

                    if (_raycastHit) {
                        // set our new pushDistance and recalculate the rayDistance taking it into account
                        rayDistance = Mathf.Abs(_raycastHit.point.x - ray.x);
                        pushDistance = boxCollider.bounds.extents.x - rayDistance;

                        if (direction == Vector2.right) {
                            collisionState.right = true;
                        }
                        else {
                            collisionState.left = true;
                        }

                        _raycastHitsThisFrame.Add(_raycastHit);
                        //If the object has a velocity that is pushing into the character, track it and alter the character velocity
                        VelocityTracker velocityTrack = _raycastHit.collider.gameObject.GetComponent<VelocityTracker>();
                        if (velocityTrack != null &&
                            Mathf.Sign(velocityTrack.Velocity.x) == Mathf.Sign(direction.x) &&
                            Mathf.Abs(pushVelocity.x) < Mathf.Abs(velocityTrack.Velocity.x)) {
                            pushVelocity.x = velocityTrack.Velocity.x;
                        }
                        else if (_raycastHit.rigidbody != null &&
                            Mathf.Sign(_raycastHit.rigidbody.velocity.x) == Mathf.Sign(direction.x) &&
                            Mathf.Abs(pushVelocity.x) < Mathf.Abs(_raycastHit.rigidbody.velocity.x)) {
                            pushVelocity.x = _raycastHit.rigidbody.velocity.x;
                        }
                    }
                }
            }

            //Handle Vertical Piercing
            if (direction == Vector2.up || direction == Vector2.down) {
                // apply our horizontal deltaMovement here so that we do our raycast from the actual position we would be in if we had moved
                //initialRayOrigin.x += deltaMovement.x;

                // if we are checking up, we should ignore the layers in oneWayPlatformMask
                var mask = platformMask;
                if ((direction == Vector2.up && !collisionState.wasGroundedLastFrame) || ignoreOneWayPlatformsThisFrame)
                    mask &= ~oneWayPlatformMask;

                for (var i = 0; i < totalVerticalRays; i++) {
                    var ray = new Vector2(initialRayOrigin.x + i * _horizontalDistanceBetweenRays, initialRayOrigin.y);
                    DrawRay(ray, direction * rayDistance, Color.red);
                    _raycastHit = Physics2D.Raycast(ray, direction, rayDistance, mask);
                    if (_raycastHit) {
                        // set our new pushDistance and recalculate the rayDistance taking it into account
                        rayDistance = Mathf.Abs(_raycastHit.point.y - ray.y);
                        pushDistance = boxCollider.bounds.extents.y - rayDistance;

                        _raycastHitsThisFrame.Add(_raycastHit);
                        //If the object has a velocity that is pushing into the character, track it and alter the character velocity
                        VelocityTracker velocityTrack = _raycastHit.collider.gameObject.GetComponent<VelocityTracker>();
                        if (velocityTrack != null &&
                            Mathf.Sign(velocityTrack.Velocity.y) == Mathf.Sign(direction.y) &&
                            Mathf.Abs(pushVelocity.y) < Mathf.Abs(velocityTrack.Velocity.y)) {
                            pushVelocity.y = velocityTrack.Velocity.y;
                        }
                        else if (_raycastHit.rigidbody != null &&
                            Mathf.Sign(_raycastHit.rigidbody.velocity.y) == Mathf.Sign(direction.y) &&
                            Mathf.Abs(pushVelocity.y) < Mathf.Abs(_raycastHit.rigidbody.velocity.y)) {
                            pushVelocity.y = _raycastHit.rigidbody.velocity.y;
                        }

                        if (direction == Vector2.up) {
                            collisionState.above = true;
                        }
                        else {
                            collisionState.below = true;
                            if (_raycastHit.collider.gameObject.GetComponent<VelocityTracker>() != null)
                                _belowVelocities.Add(velocityTrack);
                        }
                    }
                }
            }

            deltaMovement += pushDistance * -1 * (Vector3)direction + pushVelocity * Time.deltaTime;
        }

        /// <summary>
        /// we have to use a bit of trickery in this one. The rays must be cast from a small distance inside of our
        /// collider (skinWidth) to avoid zero distance rays which will get the wrong normal. Because of this small offset
        /// we have to increase the ray distance skinWidth then remember to remove skinWidth from deltaMovement before
        /// actually moving the player
        /// </summary>
        void moveHorizontally(ref Vector3 deltaMovement) {
            var isGoingRight = deltaMovement.x > 0;
            var rayDistance = Mathf.Abs(deltaMovement.x) + _skinWidth;
            var rayDirection = isGoingRight ? Vector2.right : -Vector2.right;
            var initialRayOrigin = isGoingRight ? _raycastOrigins.bottomRight : _raycastOrigins.bottomLeft;

            for (var i = 0; i < totalHorizontalRays; i++) {
                var ray = new Vector2(initialRayOrigin.x, initialRayOrigin.y + i * _verticalDistanceBetweenRays);

                DrawRay(ray, rayDirection * rayDistance, Color.red);

                // if we are grounded we will include oneWayPlatforms only on the first ray (the bottom one). this will allow us to
                // walk up sloped oneWayPlatforms
                if (i == 0 && collisionState.wasGroundedLastFrame)
                    _raycastHit = Physics2D.Raycast(ray, rayDirection, rayDistance, platformMask);
                else
                    _raycastHit = Physics2D.Raycast(ray, rayDirection, rayDistance, platformMask & ~oneWayPlatformMask);

                if (_raycastHit) {
                    // the bottom ray can hit a slope but no other ray can so we have special handling for these cases
                    if (i == 0 && handleHorizontalSlope(ref deltaMovement, Vector2.Angle(_raycastHit.normal, Vector2.up))) {
                        _raycastHitsThisFrame.Add(_raycastHit);
                        break;
                    }

                    // set our new deltaMovement and recalculate the rayDistance taking it into account
                    deltaMovement.x = _raycastHit.point.x - ray.x;
                    rayDistance = Mathf.Abs(deltaMovement.x);

                    // remember to remove the skinWidth from our deltaMovement
                    if (isGoingRight) {
                        deltaMovement.x -= _skinWidth;
                        collisionState.right = true;
                    }
                    else {
                        deltaMovement.x += _skinWidth;
                        collisionState.left = true;
                    }

                    _raycastHitsThisFrame.Add(_raycastHit);

                    // we add a small fudge factor for the float operations here. if our rayDistance is smaller
                    // than the width + fudge bail out because we have a direct impact
                    if (rayDistance < _skinWidth + kSkinWidthFloatFudgeFactor)
                        break;
                }
            }
        }


        /// <summary>
        /// handles adjusting deltaMovement if we are going up a slope.
        /// </summary>
        /// <returns><c>true</c>, if horizontal slope was handled, <c>false</c> otherwise.</returns>
        /// <param name="deltaMovement">Delta movement.</param>
        /// <param name="angle">Angle.</param>
        bool handleHorizontalSlope(ref Vector3 deltaMovement, float angle) {
            // disregard 90 degree angles (walls)
            if (Mathf.RoundToInt(angle) == 90)
                return false;

            // if we can walk on slopes and our angle is small enough we need to move up
            if (angle < slopeLimit) {
                // we only need to adjust the deltaMovement if we are not jumping
                // TODO: this uses a magic number which isn't ideal! The alternative is to have the user pass in if there is a jump this frame
                if (!isJumping) {
                    // apply the slopeModifier to slow our movement up the slope
                    var slopeModifier = slopeSpeedMultiplier.Evaluate(angle);
                    deltaMovement.x *= slopeModifier;

                    // we dont set collisions on the sides for this since a slope is not technically a side collision.
                    // smooth y movement when we climb. we make the y movement equivalent to the actual y location that corresponds
                    // to our new x location using our good friend Pythagoras
                    deltaMovement.y = Mathf.Abs(Mathf.Tan(angle * Mathf.Deg2Rad) * deltaMovement.x);
                    var isGoingRight = deltaMovement.x > 0;

                    // safety check. we fire a ray in the direction of movement just in case the diagonal we calculated above ends up
                    // going through a wall. if the ray hits, we back off the horizontal movement to stay in bounds.
                    var ray = isGoingRight ? _raycastOrigins.bottomRight : _raycastOrigins.bottomLeft;
                    RaycastHit2D raycastHit;
                    if (collisionState.wasGroundedLastFrame)
                        raycastHit = Physics2D.Raycast(ray, deltaMovement.normalized, deltaMovement.magnitude, platformMask);
                    else
                        raycastHit = Physics2D.Raycast(ray, deltaMovement.normalized, deltaMovement.magnitude, platformMask & ~oneWayPlatformMask);

                    if (raycastHit) {
                        // we crossed an edge when using Pythagoras calculation, so we set the actual delta movement to the ray hit location
                        deltaMovement = (Vector3)raycastHit.point - ray;
                        if (isGoingRight)
                            deltaMovement.x -= _skinWidth;
                        else
                            deltaMovement.x += _skinWidth;
                    }

                    _isGoingUpSlope = true;
                    collisionState.below = true;
                    if (_raycastHit.collider.gameObject.GetComponent<VelocityTracker>() != null)
                        _belowVelocities.Add(_raycastHit.collider.gameObject.GetComponent<VelocityTracker>());
                }
            }
            else // too steep. get out of here
            {
                deltaMovement.x = 0;
            }

            return true;
        }


        void moveVertically(ref Vector3 deltaMovement) {
            var isGoingUp = deltaMovement.y > 0;
            var rayDistance = Mathf.Abs(deltaMovement.y) + _skinWidth;
            var rayDirection = isGoingUp ? Vector2.up : -Vector2.up;
            var initialRayOrigin = isGoingUp ? _raycastOrigins.topLeft : _raycastOrigins.bottomLeft;

            // apply our horizontal deltaMovement here so that we do our raycast from the actual position we would be in if we had moved
            initialRayOrigin.x += deltaMovement.x;

            // if we are moving up, we should ignore the layers in oneWayPlatformMask
            var mask = platformMask;
            if ((isGoingUp && !collisionState.wasGroundedLastFrame) || ignoreOneWayPlatformsThisFrame)
                mask &= ~oneWayPlatformMask;

            for (var i = 0; i < totalVerticalRays; i++) {
                var ray = new Vector2(initialRayOrigin.x + i * _horizontalDistanceBetweenRays, initialRayOrigin.y);
                DrawRay(ray, rayDirection * rayDistance, Color.red);
                _raycastHit = Physics2D.Raycast(ray, rayDirection, rayDistance, mask);
                if (_raycastHit) {
                    // set our new deltaMovement and recalculate the rayDistance taking it into account
                    deltaMovement.y = _raycastHit.point.y - ray.y;
                    rayDistance = Mathf.Abs(deltaMovement.y);

                    // remember to remove the skinWidth from our deltaMovement
                    if (isGoingUp) {
                        deltaMovement.y -= _skinWidth;
                        collisionState.above = true;
                    }
                    else {
                        deltaMovement.y += _skinWidth;
                        if (collisionState.slopeAngle < slopeLimit) {
                            collisionState.below = true;
                            if (_raycastHit.collider.gameObject.GetComponent<VelocityTracker>() != null)
                                _belowVelocities.Add(_raycastHit.collider.gameObject.GetComponent<VelocityTracker>());
                        }
                    }

                    _raycastHitsThisFrame.Add(_raycastHit);

                    // this is a hack to deal with the top of slopes. if we walk up a slope and reach the apex we can get in a situation
                    // where our ray gets a hit that is less then skinWidth causing us to be ungrounded the next frame due to residual velocity.
                    if (!isGoingUp && deltaMovement.y > 0.00001f)
                        _isGoingUpSlope = true;

                    // we add a small fudge factor for the float operations here. if our rayDistance is smaller
                    // than the width + fudge bail out because we have a direct impact
                    if (rayDistance < _skinWidth + kSkinWidthFloatFudgeFactor)
                        break;
                }
            }
        }


        /// <summary>
        /// checks the center point under the BoxCollider2D for a slope. If it finds one then the deltaMovement is adjusted so that
        /// the player stays grounded and the slopeSpeedModifier is taken into account to speed up movement.
        /// </summary>
        /// <param name="deltaMovement">Delta movement.</param>
        private void handleVerticalSlope(ref Vector3 deltaMovement) {
            // slope check from the center of our collider
            var centerOfCollider = (_raycastOrigins.bottomLeft.x + _raycastOrigins.bottomRight.x) * 0.5f;
            var rayDirection = -Vector2.up;

            // the ray distance is based on our slopeLimit
            var slopeCheckRayDistance = _slopeLimitTangent * (_raycastOrigins.bottomRight.x - centerOfCollider);

            Vector2 slopeRay = new Vector2();
            RaycastHit2D raycastHitLeft = Physics2D.Raycast(_raycastOrigins.bottomLeft, rayDirection, slopeCheckRayDistance, platformMask);
            RaycastHit2D raycastHitRight = Physics2D.Raycast(_raycastOrigins.bottomRight, rayDirection, slopeCheckRayDistance, platformMask);

            //Choose which side to raycast from, should select the side that is higher up on the slope
            if (raycastHitLeft && raycastHitRight) {
                if (raycastHitLeft.point.y >= raycastHitRight.point.y) {
                    DrawRay(_raycastOrigins.bottomLeft, rayDirection * slopeCheckRayDistance, Color.yellow);
                    slopeRay = _raycastOrigins.bottomLeft;
                    _raycastHit = raycastHitLeft;
                }
                else {
                    DrawRay(_raycastOrigins.bottomRight, rayDirection * slopeCheckRayDistance, Color.yellow);
                    slopeRay = _raycastOrigins.bottomRight;
                    _raycastHit = raycastHitRight;
                }
            }
            else if (raycastHitLeft) {
                DrawRay(_raycastOrigins.bottomLeft, rayDirection * slopeCheckRayDistance, Color.yellow);
                slopeRay = _raycastOrigins.bottomLeft;
                _raycastHit = raycastHitLeft;
            }
            else {
                DrawRay(_raycastOrigins.bottomRight, rayDirection * slopeCheckRayDistance, Color.yellow);
                slopeRay = _raycastOrigins.bottomRight;
                _raycastHit = raycastHitRight;
            }

            if (_raycastHit) {
                // bail out if we have no slope
                var angle = Vector2.Angle(_raycastHit.normal, Vector2.up);
                if (angle == 0)
                    return;
                if (angle < slopeLimit) {
                    // we are moving down the slope if our normal and movement direction are in the same x direction
                    var isMovingDownSlope = Mathf.Sign(_raycastHit.normal.x) == Mathf.Sign(deltaMovement.x);
                    if (isMovingDownSlope) {
                        // going down we want to speed up in most cases so the slopeSpeedMultiplier curve should be > 1 for negative angles
                        var slopeModifier = slopeSpeedMultiplier.Evaluate(-angle);
                        // we add the extra downward movement here to ensure we "stick" to the surface below
                        deltaMovement.y += _raycastHit.point.y - slopeRay.y - skinWidth;
                        deltaMovement.x *= slopeModifier;
                        collisionState.movingDownSlope = true;
                        collisionState.slopeAngle = angle;
                    }
                }
                else {
                    collisionState.slopeAngle = angle;
                    collisionState.below = false;
                    _belowVelocities.Clear();
                    //Redirect y motion based on angle
                    deltaMovement.x = Mathf.Abs(deltaMovement.y) * Mathf.Cos(Mathf.Deg2Rad * angle) * Mathf.Sign(_raycastHit.normal.x);
                    deltaMovement.y *= Mathf.Sin(Mathf.Deg2Rad * angle);
                    deltaMovement.y += _raycastHit.point.y - slopeRay.y - skinWidth;
                    collisionState.movingDownSlope = true;
                }
            }
        }

        #endregion

    }
}