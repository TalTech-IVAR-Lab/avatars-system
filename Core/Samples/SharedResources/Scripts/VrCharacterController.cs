namespace Games.NoSoySauce.Avatars.Samples
{
    using System;
    using System.Collections.Generic;
    using KinematicCharacterController;
    using Malimbe.XmlDocumentationAttribute;
    using ZinniaExtensions.Action;
    using UnityEngine;

    public class VrCharacterController : MonoBehaviour, ICharacterController
    {
        #region Data Types

        /// <summary>
        ///     Enum describing possible states of the character.
        /// </summary>
        public enum CharacterState
        {
            Default
            //Climbing // TODO: implement climbing
            // Tip for when implementing climbing: there is no velocity to care about - character is fully driven by the headset.
            // This means that climbing reconciliation is basically graceful reconciliation without velocity!
        }

        /// <summary>
        ///     Struct containing all inputs for <see cref="VrCharacterController" />.
        /// </summary>
        public struct VrCharacterInputs
        {
            public Vector2 moveDirection;
            public Quaternion moveOrientation;
            public Pose headsetPose;
            public Pose playAreaPose;
            public bool jumpDown;
            public bool snapTurnLeftDown;
            public bool snapTurnRightDown;
        }

        /// <summary>
        ///     Reconciliation mode determines how user's head gets synced with the virtual character.
        /// </summary>
        public enum ReconciliationMode
        {
            /// <summary>
            ///     Strict mode reconciles all headset movements back to character
            ///     if character and headset positions do not match after simulation.
            /// </summary>
            /// <remarks>
            ///     This forces the headset back into the physically correct character
            ///     position if the player tries to stick his head into the wall, for
            ///     example.
            /// </remarks>
            Strict,

            /// <summary>
            ///     Graceful mode only reconciles headset movements along character's vertical axis.
            /// </summary>
            /// <remarks>
            ///     This allows to ascend/descend slopes with headset movement
            ///     while still allowing to freely dive into vertical walls.
            ///     In this case, putting the player back into the character
            ///     must be handled by the application (for example, blink headset
            ///     back into correct position if time or distance limit for being
            ///     outside of character is exceeded).
            /// </remarks>
            Graceful,

            /// <summary>
            ///     No reconciliation. Player moves independently from the character.
            /// </summary>
            Disabled
        }

        /// <summary>
        ///     Struct containing data for processing rotation for a give angle.
        /// </summary>
        [Serializable]
        public struct SnapRotationData
        {
            public SnapRotationData(float angle, float duration)
            {
                timer = this.duration = duration;
                currentAngle = previousAngle = startAngle = 0f;
                endAngle = angle;
                isDone = false;
            }

            /// <summary>
            ///     Initial angle of this rotation.
            /// </summary>
            public float startAngle;

            /// <summary>
            ///     Angle of this rotation during the last update.
            /// </summary>
            public float previousAngle;

            /// <summary>
            ///     Current angle of this rotation.
            /// </summary>
            public float currentAngle;

            /// <summary>
            ///     Final angle of this rotation.
            /// </summary>
            public float endAngle;

            /// <summary>
            ///     Total duration of this rotation.
            /// </summary>
            public float duration;

            /// <summary>
            ///     Remaining time of this rotation.
            /// </summary>
            public float timer;

            /// <summary>
            ///     Progress of the rotation from 0 to 1.
            /// </summary>
            public float Progress => timer > 0f && duration > 0f ? 1f - timer / duration : 1f;

            /// <summary>
            ///     Is rotation finished?
            /// </summary>
            public bool IsFinished => Progress >= 1f;

            /// <summary>
            ///     Change in the rotation angle in the last <see cref="Update" /> call.
            /// </summary>
            public float LastAngleDelta => currentAngle - previousAngle;

            /// <summary>
            ///     Updates the rotation to move to <see cref="endAngle" />.
            /// </summary>
            public void Update()
            {
                timer -= Time.deltaTime;
                if (timer < 0f) timer = 0f;
                previousAngle = currentAngle;
                currentAngle = Mathf.Lerp(startAngle, endAngle, Progress);

                isDone = Progress >= 1f;
            }

            public bool isDone;

            /// <summary>
            ///     Stops the rotation.
            /// </summary>
            public void Stop() { timer = 0f; }
        }

        #endregion

        #region Constants

        /// <summary>
        ///     Motion vectors' magnitudes will be rounded to this precision before movement calculations.
        /// </summary>
        /// <remarks>
        ///     This allows to avoid movement bugs due to floating point errors.
        /// </remarks>
        private const float FLOAT_ROUNDING_PRECISION = 0.00001f;

        /// <summary>
        ///     Multiplier for rounding function. Used in <see cref="RoundFloat" />.
        /// </summary>
        private const float FLOAT_ROUNDING_MULTIPLIER = 1f / FLOAT_ROUNDING_PRECISION;

        /// <summary>
        ///     Vectors with square magnitude less than or equal to this value will be considered zero-length.
        /// </summary>
        /// <remarks>
        ///     This allows to avoid movement bugs due to floating point errors.
        /// </remarks>
        private const float ZERO_SQARE = 0.00000001f;

        #endregion

        #region Public Variables

        public KinematicCharacterMotor motor;

        /// <summary>
        ///     Reconciliation mode determines how user's head gets synced with the virtual character.
        /// </summary>
        [field: Header("VR Movement Reconciliation"), DocumentedByXml]
        public ReconciliationMode reconciliationMode = ReconciliationMode.Strict;

        /// <summary>
        ///     Maximum velocity with which character can move to the headset.
        ///     This value directly affects how hard the player can push virtual objects when moving through them using headset
        ///     motion.
        /// </summary>
        [field: DocumentedByXml]
        public float maxHeadsetReconciliationVelocity = Mathf.Infinity;

        /// <summary>
        ///     If enabled, player will be able to move the character using headset motion while airborne.
        /// </summary>
        [field: DocumentedByXml]
        public bool allowHeadsetInputWhenAirborne = true;
        
        /// <summary>
        ///     <see cref="PoseAction"/> which sets the pose of the player's VR rig.
        /// </summary>
        /// <remarks>
        ///    Pose of the player's VR rig is a projection of the headset on play area's floor.
        /// </remarks>
        public PoseAction playerPoseSettingAction;

        [field: Header("Stable Movement")]
        public float maxStableMoveSpeed = 10f;

        // TODO: check whether this can and should be used
        public float maxStableDistanceFromLedge = 5f;

        // TODO: check whether this can and should be used
        [field: Range(0f, 180f)]
        public float maxStableDenivelationAngle = 180f;

        [field: Header("Air Movement")]
        public float maxAirMoveSpeed = 10f;

        public float airAccelerationSpeed = 5f;
        public float drag = 0.1f;

        /// <summary>
        ///     Speed of snap turn.
        /// </summary>
        [field: Header("Snap Turn Settings"), DocumentedByXml]
        public float snapDuration = 0.2f;

        /// <summary>
        ///     Amount of degrees to rotate for on snap.
        /// </summary>
        [field: DocumentedByXml, Range(0f, 90f)]
        public float snapAngle = 45f;

        /// <summary>
        ///     Multiplier to apply to <see cref="snapAngle" />.
        /// </summary>
        [field: DocumentedByXml]
        public float snapAngleMultiplier = 1f;

        /// <summary>
        ///     Should the rotation commands be buffered or discarded in case the last rotation is still happening?
        /// </summary>
        [field: DocumentedByXml]
        public bool bufferSnapRotationCommands = true;

        /// <summary>
        ///     Should the rotation direction change immediately when the input in opposing direction is received, or should it
        ///     finish processing the buffer first?
        /// </summary>
        [field: DocumentedByXml]
        public bool changeSnapDirectionImmediately = true;

        /*
        // NOTE: left it here in case we will be implementing jumping
        [Header("Jumping")]
        public bool allowJumpingWhenSliding;

        public bool allowDoubleJump;
        public bool allowWallJump;
        public float jumpSpeed = 10f;
        public float jumpPreGroundingGraceTime;
        public float jumpPostGroundingGraceTime;
        */

        /// <summary>
        /// A list of <see cref="Collider"/>s to be ignored by this character's capsule.
        /// </summary>
        [field: Header("Collisions"), DocumentedByXml]
        public List<Collider> ignoredColliders = new List<Collider>();
        /// <summary>
        /// A list of <see cref="GameObject"/>s to be ignored by this character's capsule.
        /// Collisions with all children of the objects on this list will be ignored.
        /// </summary>
        [field: DocumentedByXml]
        public List<GameObject> ignoredGameObjects = new List<GameObject>();

        [field: Header("Misc")]
        public bool orientTowardsGravity;
        public Vector3 gravity = new Vector3(0, -30f, 0);
        public Transform meshRoot;

        public CharacterState CurrentCharacterState { get; private set; }

        #endregion

        #region Internal Variables

        /* Collisions */
        private readonly Collider[] probedColliders = new Collider[8];

        /* Direction */
        private Vector3 lookInputVector;

        /* Linear motion */

        /// <summary>
        ///     Character motion due to player directional input in ultimate circumstances.
        /// </summary>
        private Vector3 ultimateVelocityInputMotionThisFrame;

        /// <summary>
        ///     Character motion due to headset displacement in ultimate circumstances.
        /// </summary>
        private Vector3 ultimateHeadsetMotionThisFrame;
        
        private Vector3 moveInputVector;
        private Pose headsetPoseInput;
        private Pose playAreaPoseInput;
        private float headsetAltitude;
        private Pose initialPlayerRootPose;
        
        /* Rotational motion */
        /// <summary>
        ///     Is any rotation command currently being processed?
        /// </summary>
        protected bool IsSnapRotating => !currentSnapRotation.IsFinished;
        
        /// <summary>
        ///     Current rotation.
        /// </summary>
        protected SnapRotationData currentSnapRotation = new SnapRotationData(0f, 0f);

        /// <summary>
        ///     A <see cref="Queue" /> to hold incoming rotation commands.
        /// </summary>
        protected Queue<SnapRotationData> bufferedSnapRotations = new Queue<SnapRotationData>();
        
        /* Jump */
        private bool jumpRequested;
        private bool jumpConsumed;
        private bool doubleJumpConsumed;
        private bool jumpedThisFrame;
        private bool canWallJump;
        private Vector3 wallJumpNormal;
        private float timeSinceJumpRequested = Mathf.Infinity;
        private float timeSinceLastAbleToJump;

        /* External forces */
        private Vector3 internalVelocityAdd = Vector3.zero;

        #endregion

        #region Debugging Variables

        // These are variables used for debuggiing. Make them public or serializable is case you need to be seen in Inspector again.

        //[Header("Debugs collinear")]
        private Vector3 _charMotionCollinear;
        private float _charMotionMag;
        private bool _charMoved;
        private float _headsetInputCollinearDot;
        private float _headsetInputCollinearMag;
        private float _headsetInputProjMagRnd;
        private bool _headsetMoved;
        private float _velocityInputCollinearDot;
        private float _velocityInputCollinearMag;
        private float _velocityInputProjMagRnd;
        private bool _velocityInputMoved;
        private string _collinearSolutionCase;

        // [Header("Debugs orthogonal")]
        private Vector3 _charMotionOrtho;
        private float _headsetInputOrthoDot;
        private float _headsetInputOrthoMag;
        private float _headsetInputOrthoMagRnd;
        private float _velocityInputOrthoDot;
        private float _velocityInputOrthoMag;
        private float _velocityInputOrthoMagRnd;
        private float _velocityInputOrthoOnHeadsetDot;
        private bool _velocityInputOrthoOnHeadsetCodirectional;

        #endregion

        #region Unity Events

        /* not needed because these events do not account the state the character is in 
                /// <summary>
                ///     Event invoked before the character simlation tick.
                /// </summary>
                [field: DocumentedByXml]
                public UnityEvent onBeforeCharacterProcessed = new UnityEvent();
        
                /// <summary>
                ///     Event invoked after the character simlation tick.
                /// </summary>
                [field: DocumentedByXml]
                public UnityEvent onAfterCharacterProcessed = new UnityEvent();
        */

        #endregion

        #region Unity Callbacks

        private void Start()
        {
            // Assign to motor
            motor.CharacterController = this;

            // Disable interpolation as its effects have not yet been tested in VR
            // TODO: test if interpolation enhances motion experience in VR 
            // KinematicCharacterSystem.Settings.Interpolate = false;

            // Handle initial state
            TransitionToState(CharacterState.Default);
        }

        private void OnDrawGizmos()
        {
            var pointOne = transform.position;
            var pointTwo = pointOne + _charMotionCollinear * 1.5f;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(pointOne, pointTwo);

            pointTwo = pointOne + _charMotionOrtho * 1.5f;
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(pointOne, pointTwo);
        }

        #endregion

        #region ICharacterController Callbacks

        /// <summary>
        ///     (Called by KinematicCharacterMotor during its update cycle)
        ///     This is called before the character begins its movement update
        /// </summary>
        public void BeforeCharacterUpdate(float deltaTime)
        {
            // internal state switching should be done here

            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                {
                    // Handle headset altitude decrease
                    {
                        // We apply height change before the simulation only if the headset height becomes smaller
                        // Height increase is done after the simulation to avoid clipping into the ceiling
                        var playAreaFloorPlane = new Plane(playAreaPoseInput.up, playAreaPoseInput.position);
                        headsetAltitude = playAreaFloorPlane.GetDistanceToPoint(headsetPoseInput.position);
                        if (headsetAltitude < motor.Capsule.height) motor.SetCapsuleDimensions(motor.Capsule.radius, headsetAltitude, headsetAltitude / 2f);
                    }

                    break;
                }
            }
        }

        /// <summary>
        ///     (Called by KinematicCharacterMotor during its update cycle)
        ///     This is where you tell your character what its rotation should be right now.
        ///     This is the ONLY place where you should set the character's rotation
        /// </summary>
        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                {
                    /* Handle basic orientation */
                    {
                        if (lookInputVector != Vector3.zero)
                            // Set current rotation (no smoothing for VR)
                            currentRotation = Quaternion.LookRotation(lookInputVector, motor.CharacterUp);
                        if (orientTowardsGravity)
                            // Rotate from current up to invert gravity
                            currentRotation = Quaternion.FromToRotation(currentRotation * Vector3.up, -gravity) * currentRotation;
                    }
                    
                    /* Handle snap rotation */
                    {
                        // If not rotating, try to get new rotation command from buffer
                        if (!IsSnapRotating && bufferedSnapRotations.Count > 0) currentSnapRotation = bufferedSnapRotations.Dequeue();

                        // If rotating, update the rotation data struct and apply rotation to the character
                        if (IsSnapRotating)
                        {
                            currentSnapRotation.Update();
                            currentRotation = Quaternion.AngleAxis(currentSnapRotation.LastAngleDelta, motor.CharacterUp) * currentRotation;
                        }
                    }

                    break;
                }
            }
        }

        /// <summary>
        ///     (Called by KinematicCharacterMotor during its update cycle)
        ///     This is where you tell your character what its velocity should be right now.
        ///     This is the ONLY place where you can set the character's velocity
        /// </summary>
        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                {
                    // Initialize
                    Vector3 moveInputVelocity;
                    Vector3 headsetInputVelocity;

                    // Calculate player displacement
                    var playerDisplacementThisFrame = initialPlayerRootPose.position - motor.TransientPosition;

                    /* Grounded movement */
                    if (motor.GroundingStatus.IsStableOnGround)
                    {
                        //Debug.LogError($"stableground {_moveInputVector.magnitude}");
                        currentVelocity = Vector3.zero;

                        // Move input
                        if (moveInputVector.sqrMagnitude > ZERO_SQARE)
                        {
                            // Calculate movement input velocity
                            var inputRight = Vector3.Cross(moveInputVector, motor.CharacterUp);
                            var reorientedInput = Vector3.Cross(motor.GroundingStatus.GroundNormal, inputRight).normalized * moveInputVector.magnitude;
                            moveInputVelocity = reorientedInput * maxStableMoveSpeed;
                            // Calculate ultimate horizontal displacement due to movement input
                            var horizontalInputVelocity = Vector3.ProjectOnPlane(moveInputVelocity, motor.CharacterUp);
                            ultimateVelocityInputMotionThisFrame = horizontalInputVelocity * deltaTime;

                            // Set movement velocity (no smoothing for VR)
                            currentVelocity += moveInputVelocity;
                        }
                        else { ultimateVelocityInputMotionThisFrame = Vector3.zero; }

                        // Headset input
                        if (playerDisplacementThisFrame.sqrMagnitude > ZERO_SQARE)
                        {
                            // Calculate headset input velocity
                            headsetInputVelocity = playerDisplacementThisFrame / deltaTime;
                            headsetInputVelocity -= Vector3.Dot(headsetInputVelocity, motor.CharacterUp) * motor.CharacterUp; // remove any vertical effect of displacement due to floating point precision
                            headsetInputVelocity = Vector3.ClampMagnitude(headsetInputVelocity, maxHeadsetReconciliationVelocity);
                            // Calculate ultimate horizontal displacement due to headset motion
                            ultimateHeadsetMotionThisFrame = headsetInputVelocity * deltaTime;

                            // Add headset correction to total velocity
                            currentVelocity += headsetInputVelocity;
                        }
                        else { ultimateHeadsetMotionThisFrame = Vector3.zero; }
                    }
                    /* Airborne movement */
                    else
                    {
                        //Debug.LogError($"airborne {_moveInputVector.magnitude}");
                        // Move input
                        if (moveInputVector.sqrMagnitude > ZERO_SQARE)
                        {
                            moveInputVelocity = moveInputVector * maxAirMoveSpeed;

                            // Prevent climbing on un-stable slopes with air movement
                            if (motor.GroundingStatus.FoundAnyGround)
                            {
                                var perpendicularObstructionNormal = Vector3.Cross(Vector3.Cross(motor.CharacterUp, motor.GroundingStatus.GroundNormal), motor.CharacterUp).normalized;
                                moveInputVelocity = Vector3.ProjectOnPlane(moveInputVelocity, perpendicularObstructionNormal);
                            }

                            // Calculate ultimate horizontal displacement due to movement input
                            var horizontalInputVelocity = Vector3.ProjectOnPlane(moveInputVelocity, motor.CharacterUp);
                            ultimateVelocityInputMotionThisFrame = horizontalInputVelocity * deltaTime;

                            var velocityDiff = Vector3.ProjectOnPlane(moveInputVelocity - currentVelocity, gravity);
                            currentVelocity += velocityDiff * (airAccelerationSpeed * deltaTime);
                        }
                        else { ultimateHeadsetMotionThisFrame = Vector3.zero; }

                        // Headset input
                        if (playerDisplacementThisFrame.sqrMagnitude > ZERO_SQARE)
                        {
                            if (allowHeadsetInputWhenAirborne)
                            {
                                // Add headset correction
                                headsetInputVelocity = playerDisplacementThisFrame / deltaTime;
                                headsetInputVelocity -= Vector3.Dot(headsetInputVelocity, motor.CharacterUp) * motor.CharacterUp; // remove any vertical effect of displacement due to floating point precision
                                headsetInputVelocity = Vector3.ClampMagnitude(headsetInputVelocity, maxHeadsetReconciliationVelocity);
                                // Calculate ultimate horizontal displacement due to headset motion
                                ultimateHeadsetMotionThisFrame = headsetInputVelocity * deltaTime;

                                // Add headset correction to total velocity
                                var velocityDiff = Vector3.ProjectOnPlane(headsetInputVelocity - currentVelocity, gravity);
                                currentVelocity += velocityDiff;
                            }
                            else
                            {
                                // Headset does not affect character motion
                                headsetInputVelocity = Vector3.zero;
                                ultimateHeadsetMotionThisFrame = Vector3.zero;
                            }
                        }
                        else { ultimateHeadsetMotionThisFrame = Vector3.zero; }

                        // Gravity
                        currentVelocity += gravity * deltaTime;

                        // Drag
                        currentVelocity *= 1f / (1f + drag * deltaTime);
                    }

                    break;
                }
            }
        }

        /// <summary>
        ///     (Called by KinematicCharacterMotor during its update cycle)
        ///     This is called after the character has finished its movement update
        /// </summary>
        public void AfterCharacterUpdate(float deltaTime)
        {
            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                {
                    // Handle headset altitude increase
                    {
                        // We apply height change after the simulation only if the headset height becomes bigger
                        if (headsetAltitude > motor.Capsule.height)
                        {
                            float heightBeforeCheck = motor.Capsule.height;
                            motor.SetCapsuleDimensions(motor.Capsule.radius, headsetAltitude, headsetAltitude / 2f);
                            if (motor.CharacterOverlap(
                                    motor.TransientPosition,
                                    motor.TransientRotation,
                                    probedColliders,
                                    motor.CollidableLayers,
                                    QueryTriggerInteraction.Ignore) > 0)
                                // If obstructions, just stick to current dimensions
                                motor.SetCapsuleDimensions(motor.Capsule.radius, heightBeforeCheck, heightBeforeCheck / 2f);

                            // TODO: what happens to the player? think about it:
                            //       if any obstructions are found, we are definitely not moving player back into the character;
                            //       otherwise, it can result in some weird scenarios, i.e. if you are crouching in a low pathway
                            //       and you decide to rise in real life, ceiling stays above your head as a result, but then you
                            //       can't get back up after you leave the pathway.
                            else
                                // If no obstructions, match the headset height
                                motor.SetCapsuleDimensions(motor.Capsule.radius, headsetAltitude, headsetAltitude / 2f);
                        }
                    }

                    // Handle reconciliation
                    {
                        Pose reconciliationPose; // <- pose to which headset root should be moved at te end of the frame to sync with virtual character

                        // Select reconciliation pose
                        switch (reconciliationMode)
                        {
                            case ReconciliationMode.Graceful:
                            {
                                // Find character's lateral motion vector and its direction
                                var totalCharMotion = motor.TransientPosition - motor.InitialSimulationPosition;
                                var charLateralMotion = Vector3.ProjectOnPlane(totalCharMotion, motor.CharacterUp);
                                var charLateralMotionDirection = GetCharacterLateralMotionDirectionForReconciliation(charLateralMotion);

                                // Calculate reconciliation vector component collinear to character motion
                                var lateralReconciliationVectorCollinear = CalculateReconciliationVectorCollinear(charLateralMotionDirection, charLateralMotion);

                                // Calculate reconciliation vector component orthogonal to character motion
                                var lateralReconciliationVectorOrthogonal = CalculateReconciliationVectorOrthogonal(charLateralMotionDirection);

                                // Combine lateral reconciliation vectors
                                var lateralReconciliationVector = lateralReconciliationVectorCollinear + lateralReconciliationVectorOrthogonal;

                                // Find vertical reconciliation vector
                                var playerToCharacterPositionDelta = motor.TransientPosition - initialPlayerRootPose.position;
                                var verticalReconciliationVector = Vector3.Dot(playerToCharacterPositionDelta, motor.CharacterUp) * motor.CharacterUp;

                                reconciliationPose = new Pose
                                {
                                    position = initialPlayerRootPose.position + lateralReconciliationVector + verticalReconciliationVector,
                                    rotation = motor.TransientRotation // bug: not handling rotation for now
                                };
                                break;
                            }
                            case ReconciliationMode.Strict:
                            {
                                var charPositionBeforeSimulation = motor.InitialSimulationPosition;
                                var charPositionAfterSimulation = motor.TransientPosition;
                                var totalCharDisplacement = charPositionAfterSimulation - charPositionBeforeSimulation;

                                var charDisplacementWithoutHeadset = totalCharDisplacement - ultimateHeadsetMotionThisFrame;

                                // Match player position with character regardless of the circumstances.
                                reconciliationPose = new Pose
                                {
                                    position = motor.TransientPosition,
                                    rotation = initialPlayerRootPose.rotation // bug not handling rotation for now
                                };
                                break;
                            }
                            case ReconciliationMode.Disabled:
                            default:
                            {
                                // If reconciliation is disabled, don't change player pose
                                reconciliationPose = initialPlayerRootPose;
                                break;
                            }
                        }

                        // Apply reconciliation
                        SetPlayerRigPose(reconciliationPose);
                    }

                    break;
                }
            }
        }

        public bool IsColliderValidForCollisions(Collider coll)
        {
            // Ignored colliders list check
            if (ignoredColliders.Contains(coll)) return false;
            
            // Ignored GameObjects list check
            foreach (var ignoredGameObject in ignoredGameObjects)
            {
                if (coll.transform.IsChildOf(ignoredGameObject.transform)) return false;
            }
            
            return true;
        }

        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }

        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }

        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) { }

        public void PostGroundingUpdate(float deltaTime) { }

        public void OnDiscreteCollisionDetected(Collider hitCollider) { }

        #endregion

        #region Methods

        /// <summary>
        ///     This is called every frame by MyPlayer in order to tell the character what its inputs are
        /// </summary>
        public void SetInputs(ref VrCharacterInputs inputs)
        {
            // Clamp input
            var moveInputVector = Vector3.ClampMagnitude(new Vector3(inputs.moveDirection.x, 0f, inputs.moveDirection.y), 1f);

            // Calculate headset direction and rotation on the character plane
            var planarDirection = Vector3.ProjectOnPlane(inputs.moveOrientation * Vector3.forward, motor.CharacterUp).normalized;
            if (Math.Abs(planarDirection.sqrMagnitude) < float.Epsilon) planarDirection = Vector3.ProjectOnPlane(inputs.moveOrientation * Vector3.up, motor.CharacterUp).normalized;
            var planarRotation = Quaternion.LookRotation(planarDirection, motor.CharacterUp);

            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                {
                    // Direction inputs
                    this.moveInputVector = planarRotation * moveInputVector;
                    lookInputVector = planarDirection;
                    
                    // Snap turn inputs
                    if (inputs.snapTurnLeftDown) RequestSnapRotationLeft();
                    if (inputs.snapTurnRightDown) RequestSnapRotationRight();
                    
                    // Pose inputs
                    headsetPoseInput = inputs.headsetPose;
                    playAreaPoseInput = inputs.playAreaPose;

                    // Find player root pose (headset's vertical projection on Play Area's floor)
                    var playAreaUpDirection = playAreaPoseInput.up;
                    var playAreaFloorPlane = new Plane(playAreaUpDirection, playAreaPoseInput.position);
                    var projectedHeadsetForwardDirection = Vector3.ProjectOnPlane(headsetPoseInput.forward, playAreaUpDirection).normalized;
                    if (Math.Abs(projectedHeadsetForwardDirection.sqrMagnitude) < ZERO_SQARE) projectedHeadsetForwardDirection = Vector3.ProjectOnPlane(headsetPoseInput.up, playAreaUpDirection);
                    initialPlayerRootPose = new Pose
                    {
                        position = playAreaFloorPlane.ClosestPointOnPlane(headsetPoseInput.position),
                        rotation = Quaternion.LookRotation(projectedHeadsetForwardDirection, playAreaUpDirection)
                    };

                    // Jump input
                    if (inputs.jumpDown)
                    {
                        timeSinceJumpRequested = 0f;
                        jumpRequested = true;
                    }

                    break;
                }
            }
        }

        /// <summary>
        ///     Adds arbitrary velocity to the character.
        /// </summary>
        /// <param name="velocity">Velocity to add.</param>
        public void AddVelocity(Vector3 velocity)
        {
            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                {
                    internalVelocityAdd += velocity;
                    break;
                }
            }
        }

        /// <summary>
        ///     Teleports the player to character's current position.
        /// </summary>
        public void ForceReconciliate()
        {
            var reconciledPlayerPose = new Pose
            {
                position = motor.TransientPosition,
                rotation = motor.TransientRotation
            };
            SetPlayerRigPose(reconciledPlayerPose);
        }

        /// <summary>
        ///     Handles movement state transitions and enter/exit callbacks
        /// </summary>
        public void TransitionToState(CharacterState newState)
        {
            var tmpInitialState = CurrentCharacterState;
            OnStateExit(tmpInitialState, newState);
            CurrentCharacterState = newState;
            OnStateEnter(newState, tmpInitialState);
        }

        /// <summary>
        ///     Event when entering a state
        /// </summary>
        private void OnStateEnter(CharacterState state, CharacterState fromState)
        {
            switch (state)
            {
                case CharacterState.Default: { break; }
            }
        }

        /// <summary>
        ///     Event when exiting a state
        /// </summary>
        private void OnStateExit(CharacterState state, CharacterState toState)
        {
            switch (state)
            {
                case CharacterState.Default: { break; }
            }
        }

        /// <summary>
        ///     Sets the pose of the player's headset projection on play area floor in world space.
        /// </summary>
        /// <param name="pose">Pose to set.</param>
        private void SetPlayerRigPose(Pose pose)
        {
            if (!playerPoseSettingAction)
            {
                Debug.LogError($"Character pose cannot be applied to the player: '{nameof(playerPoseSettingAction)}' reference is not set.");
                return;
            }
            
            playerPoseSettingAction.Receive(pose);
        }

        /// <summary>
        ///     Rounds floating-point number to given precision.
        /// </summary>
        private float RoundFloat(float x) { return Mathf.Round(x * FLOAT_ROUNDING_MULTIPLIER) * FLOAT_ROUNDING_PRECISION; }

        /// <summary>
        ///     Calculates the direction of character motion to be used for reconciliation this frame.
        /// </summary>
        private Vector3 GetCharacterLateralMotionDirectionForReconciliation(Vector3 charLateralMotion)
        {
            // Booleans to check which types of motion happened this frame
            bool charLateralMoved = charLateralMotion.sqrMagnitude > ZERO_SQARE;
            bool headsetMoved = ultimateHeadsetMotionThisFrame.sqrMagnitude > ZERO_SQARE;
            bool velocityMoved = ultimateVelocityInputMotionThisFrame.sqrMagnitude > ZERO_SQARE;

            // Find direction of character's lateral motion
            var charLateralMotionDirection = charLateralMotion.normalized;
            // Special case 1: when there is no character movement, we set character move direction along the headset motion vector.
            // This way we can still project headset motion and velocity.
            if (!charLateralMoved)
            {
                charLateralMotionDirection = ultimateHeadsetMotionThisFrame.normalized;
                // Special case 2: when there is no character nor headset movement, we set character move direction along velocity vector. 
                if (!headsetMoved) charLateralMotionDirection = ultimateVelocityInputMotionThisFrame.normalized;
            }

            // Debugs
            _charMoved = charLateralMoved;
            _headsetMoved = headsetMoved;
            _velocityInputMoved = velocityMoved;

            return charLateralMotionDirection;
        }

        /// <summary>
        ///     Calculates reconciliation vector component collinear to character motion.
        /// </summary>
        private Vector3 CalculateReconciliationVectorCollinear(Vector3 charLateralMotionDirection, Vector3 charLateralMotion)
        {
            var lateralReconciliationVectorCollinear = Vector3.zero;

            // Find character motion magnitude
            float charLateralMotionMagnitude = charLateralMotion.magnitude;

            // Find headset and character motion dot product to find headset motion projection onto character motion
            float ultimateHeadsetMotionDot = Vector3.Dot(ultimateHeadsetMotionThisFrame, charLateralMotionDirection);
            float ultimateHeadsetMotionProjectedMagnitude = Mathf.Abs(ultimateHeadsetMotionDot);
            var ultimateHeadsetMotionProjected = ultimateHeadsetMotionDot * charLateralMotionDirection;

            // Find velocity input and character motion dot product to find velocity input motion projection onto character motion
            float ultimateVelocityInputMotionDot = Vector3.Dot(ultimateVelocityInputMotionThisFrame, charLateralMotionDirection);
            float ultimateVelocityInputMotionProjectedMagnitude = Mathf.Abs(ultimateVelocityInputMotionDot);
            var ultimateVelocityInputMotionProjected = ultimateVelocityInputMotionDot * charLateralMotionDirection;

            // Check directionality of inputs
            bool headsetInputCodirectional = ultimateHeadsetMotionDot > -FLOAT_ROUNDING_PRECISION; // <- whether headset and character motion vectors face in the same direction (angle between them is < 90°)
            bool moveInputCodirectional = ultimateVelocityInputMotionDot > -FLOAT_ROUNDING_PRECISION; // <- whether velocity input and character motion vectors face in the same direction (angle between them is < 90°)
            bool bothInputsCodirectional = headsetInputCodirectional == moveInputCodirectional; // <- whether headset and velocity input motions face the same direction (angle between them is < 90°)

            // Round magnitudes before using them in comparisons
            charLateralMotionMagnitude = RoundFloat(charLateralMotionMagnitude);
            ultimateHeadsetMotionProjectedMagnitude = RoundFloat(ultimateHeadsetMotionProjectedMagnitude);
            ultimateVelocityInputMotionProjectedMagnitude = RoundFloat(ultimateVelocityInputMotionProjectedMagnitude);

            // Next go the solutions to calculate required reconciliation motion.
            // There are 12 solution cases in total. All of them are explained in PDF attached to this Trello card: https://trello.com/c/8jga5MXL.
            {
                /* Case tree 1.X */
                if (bothInputsCodirectional)
                {
                    if (headsetInputCodirectional)
                    {
                        if (charLateralMotionMagnitude > ultimateHeadsetMotionProjectedMagnitude)
                        {
                            // v Cases 1.1 - 1.2
                            _collinearSolutionCase = "1.1 - 1.2";
                            lateralReconciliationVectorCollinear = charLateralMotion - ultimateHeadsetMotionProjected;
                        }
                        else
                        {
                            // v Case 1.3
                            _collinearSolutionCase = "1.3";
                            lateralReconciliationVectorCollinear = Vector3.zero;
                        }
                    }
                    else
                    {
                        // v Case 1.4
                        _collinearSolutionCase = "1.4";
                        lateralReconciliationVectorCollinear = charLateralMotion;
                    }
                }
                /* Case tree 2.X */
                else if (ultimateHeadsetMotionProjectedMagnitude >= ultimateVelocityInputMotionProjectedMagnitude)
                {
                    if (headsetInputCodirectional)
                    {
                        if (charLateralMotionMagnitude >= ultimateHeadsetMotionProjectedMagnitude)
                        {
                            // v Case 2.1
                            _collinearSolutionCase = "2.1";
                            lateralReconciliationVectorCollinear = charLateralMotion - ultimateHeadsetMotionProjected;
                        }
                        else
                        {
                            if (charLateralMotionMagnitude > ultimateHeadsetMotionProjectedMagnitude - ultimateVelocityInputMotionProjectedMagnitude)
                            {
                                // v Case 2.2
                                _collinearSolutionCase = "2.2";
                                lateralReconciliationVectorCollinear = charLateralMotion - ultimateHeadsetMotionProjected;
                            }
                            else
                            {
                                // v Case 2.3
                                _collinearSolutionCase = "2.3";
                                lateralReconciliationVectorCollinear = ultimateVelocityInputMotionProjected;
                            }
                        }
                    }
                    else
                    {
                        // v Case 2.4
                        _collinearSolutionCase = "2.4";
                        lateralReconciliationVectorCollinear = charLateralMotion + ultimateVelocityInputMotionProjected;
                    }
                }
                /* Case tree 3.X */
                else
                {
                    // v Cases 3.1 - 3.4
                    _collinearSolutionCase = "3.1 - 3.4";
                    lateralReconciliationVectorCollinear = charLateralMotion - ultimateHeadsetMotionProjected;
                }
            }

            // Debugs
            _charMotionMag = charLateralMotion.magnitude;
            _charMotionCollinear = charLateralMotionDirection;
            _headsetInputCollinearDot = ultimateHeadsetMotionDot;
            _headsetInputCollinearMag = ultimateHeadsetMotionProjected.magnitude;
            _velocityInputCollinearDot = ultimateVelocityInputMotionDot;
            _velocityInputCollinearMag = ultimateVelocityInputMotionProjected.magnitude;
            _headsetInputProjMagRnd = ultimateHeadsetMotionProjectedMagnitude;
            _velocityInputProjMagRnd = ultimateVelocityInputMotionProjectedMagnitude;

            return lateralReconciliationVectorCollinear;
        }

        /// <summary>
        ///     Calculates reconciliation vector component orthogonal to character motion.
        /// </summary>
        private Vector3 CalculateReconciliationVectorOrthogonal(Vector3 charLateralMotionDirection)
        {
            var lateralReconciliationVectorOrthogonal = Vector3.zero;

            // Find direction vector orthogonal to character's motion direction
            var charLateralMotionOrthoDirection = Vector3.Cross(charLateralMotionDirection, motor.CharacterUp);


            // Find component of headset motion which is orthogonal to character motion
            float ultimateHeadsetMotionOrthogonalDot = Vector3.Dot(ultimateHeadsetMotionThisFrame, charLateralMotionOrthoDirection);
            float ultimateHeadsetMotionOrthogonalMagnitude = Mathf.Abs(ultimateHeadsetMotionOrthogonalDot);
            var ultimateHeadsetMotionOrthogonal = ultimateHeadsetMotionOrthogonalDot * charLateralMotionOrthoDirection;


            // Find component of velocity input motion which is orthogonal to character motion
            float ultimateVelocityInputMotionOrthogonalDot = Vector3.Dot(ultimateVelocityInputMotionThisFrame, charLateralMotionOrthoDirection);
            float ultimateVelocityInputMotionOrthogonalMagnitude = Mathf.Abs(ultimateVelocityInputMotionOrthogonalDot);
            var ultimateVelocityInputMotionOrthogonal = ultimateVelocityInputMotionOrthogonalDot * charLateralMotionOrthoDirection;


            // Booleans to check which types of motion happened this frame
            bool headsetMovedOrthogonal = ultimateHeadsetMotionOrthogonal.sqrMagnitude > ZERO_SQARE;

            // Round magnitudes before using them in comparisons
            ultimateHeadsetMotionOrthogonalMagnitude = RoundFloat(ultimateHeadsetMotionOrthogonalMagnitude);
            ultimateVelocityInputMotionOrthogonalMagnitude = RoundFloat(ultimateVelocityInputMotionOrthogonalMagnitude);

            // Debugs
            _charMotionOrtho = charLateralMotionOrthoDirection;
            _headsetInputOrthoDot = ultimateHeadsetMotionOrthogonalDot;
            _headsetInputOrthoMag = ultimateHeadsetMotionOrthogonal.magnitude;
            _velocityInputOrthoDot = ultimateVelocityInputMotionOrthogonalDot;
            _velocityInputOrthoMag = ultimateVelocityInputMotionOrthogonal.magnitude;
            _headsetInputOrthoMagRnd = ultimateHeadsetMotionOrthogonalMagnitude;
            _velocityInputOrthoMagRnd = ultimateVelocityInputMotionOrthogonalMagnitude;

            // Clamp orthogonal velocity input component to orthogonal headset motion if the latter is present
            if (headsetMovedOrthogonal)
            {
                var ultimateHeadsetMotionOrthogonalDirection = ultimateHeadsetMotionOrthogonal.normalized;
                float ultimateVelocityInputMotionOnHeadsetDot = headsetMovedOrthogonal ? Vector3.Dot(ultimateVelocityInputMotionOrthogonal, ultimateHeadsetMotionOrthogonalDirection) : 0f;
                bool velocityOrthoAndHeadsetOrthoCodirectional = ultimateVelocityInputMotionOnHeadsetDot > -FLOAT_ROUNDING_PRECISION;
                if (velocityOrthoAndHeadsetOrthoCodirectional) ultimateVelocityInputMotionOrthogonal = Vector3.zero;
                else ultimateVelocityInputMotionOrthogonal = ultimateVelocityInputMotionOrthogonal.normalized * Mathf.Clamp(ultimateVelocityInputMotionOrthogonalMagnitude, 0f, ultimateHeadsetMotionOrthogonalMagnitude);

                _velocityInputOrthoOnHeadsetDot = ultimateVelocityInputMotionOnHeadsetDot;
                _velocityInputOrthoOnHeadsetCodirectional = velocityOrthoAndHeadsetOrthoCodirectional;
            }
            else
            {
                _velocityInputOrthoOnHeadsetDot = 0f;
                _velocityInputOrthoOnHeadsetCodirectional = true;
            }

            // Add velocity input component to reconciliation vector
            lateralReconciliationVectorOrthogonal += ultimateVelocityInputMotionOrthogonal;

            return lateralReconciliationVectorOrthogonal;
        }

        /// <summary>
        /// Shortcut for buffering left snap rotation command.
        /// </summary>
        private void RequestSnapRotationLeft()
        {
            RequestSnapRotation(-snapAngle * snapAngleMultiplier, snapDuration);
        }

        /// <summary>
        /// Shortcut for buffering right snap rotation command.
        /// </summary>
        private void RequestSnapRotationRight()
        {
            RequestSnapRotation(snapAngle * snapAngleMultiplier, snapDuration);
        }
        
        /// <summary>
        ///     Add rotation to the specified angle to <see cref="bufferedSnapRotations"/>.
        /// </summary>
        /// <param name="angle">Angle to rotate to.</param>
        /// <param name="duration">Rotation duration.</param>
        private void RequestSnapRotation(float angle, float duration)
        {
            if (IsSnapRotating && !bufferSnapRotationCommands) return;
            if (IsSnapRotating && changeSnapDirectionImmediately)
            {
                // If can change direction immediately, check if the direction is changing.
                bool newDirection = angle > 0f;
                bool oldDirection = currentSnapRotation.endAngle > 0f;

                // If the direction is changed...
                if (newDirection != oldDirection)
                {
                    // Adjust target angle as if the current rotation had finished and we applied new rotation on top of it.
                    float currentRemainingAngle = currentSnapRotation.endAngle - currentSnapRotation.currentAngle;
                    angle = currentRemainingAngle + angle;
                    
                    // Shrink duration proportionally to how much of current rotation is passed.
                    duration = currentSnapRotation.currentAngle / currentSnapRotation.endAngle * duration;

                    // Clear the buffer and stop current rotation.
                    bufferedSnapRotations.Clear();
                    currentSnapRotation.Stop();
                }
            }

            // Create new rotation object and add it to buffer
            var rotation = new SnapRotationData(angle, duration);
            bufferedSnapRotations.Enqueue(rotation);
        }

        #endregion
    }
}