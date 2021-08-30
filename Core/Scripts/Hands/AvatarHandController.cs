namespace Games.NoSoySauce.Avatars.Hands
{
    using System.Collections.Generic;
    using Malimbe.XmlDocumentationAttribute;
    using UnityEngine;
    using Zinnia.Action;

    /// <summary>
    /// Enum describing fingers of the hand.
    /// </summary>
    public enum Finger
    {
        Thumb,
        Index,
        Middle,
        Ring,
        Pinky
    }

    /// <summary>
    /// Describes override settings for hand pose.
    /// </summary>
    [System.Serializable]
    public class AvatarHandPoseOverride
    {
        [System.Serializable]
        public struct FingerValueOverride
        {
            /// <summary>
            /// Is this override active?
            /// </summary>
            public bool active;
            /// <summary>
            /// Value of the override.
            /// </summary>
            [Range(0f,1f)]
            public float value;
        }

        [Range(0f,1f)]
        public float weight = 1f;

        public FingerValueOverride ThumbValueOverride = new FingerValueOverride();
        public FingerValueOverride IndexValueOverride = new FingerValueOverride();
        public FingerValueOverride MiddleValueOverride = new FingerValueOverride();
        public FingerValueOverride RingValueOverride = new FingerValueOverride();
        public FingerValueOverride PinkyValueOverride = new FingerValueOverride();

        public FingerValueOverride GetFingerValueOverride(Finger finger)
        {
            switch (finger)
            {
                case Finger.Thumb:
                    {
                        return ThumbValueOverride;
                    }
                case Finger.Index:
                    {
                        return IndexValueOverride;
                    }
                case Finger.Middle:
                    {
                        return MiddleValueOverride;
                    }
                case Finger.Ring:
                    {
                        return RingValueOverride;
                    }
                case Finger.Pinky:
                    {
                        return PinkyValueOverride;
                    }
                default:
                    {
                        return ThumbValueOverride;
                    }
            }
        }
    }

    /// <summary>
    /// Script to control avatar hand based on XR finger input.
    /// </summary>
    /// <remarks>
    /// Finger animations are activated by changing their layer weights in the hand's AnimationController.
    /// Each finger must have its own animation layer.
    /// </remarks>
    public class AvatarHandController : MonoBehaviour
    {
        #region Variables
        [Header("Animation Settings")]
        /// <summary>
        /// <see cref="Animator"/> used for animating the hand.
        /// </summary>
        [field: DocumentedByXml]
        public Animator animator;
        /// <summary>
        /// The speed with which a finger will transition to it's target position.
        /// </summary>
        [field: DocumentedByXml]
        public float animationSpeed = 10f;

        [Header("Animation Overrides")]
        /// <summary>
        /// List of applied <see cref="AvatarHandPoseOverride"/>s.
        /// </summary>
        [SerializeField]
        [field: DocumentedByXml]
        protected List<AvatarHandPoseOverride> poseOverrides = new List<AvatarHandPoseOverride>();

        [Header("Finger Animation Layer Names")]
        /// <summary>
        /// Name of the animation layer used to control thumb.
        /// </summary>
        [field: DocumentedByXml]
        public string thumbLayerName = "Hand_Thumb";
        /// <summary>
        /// Name of the animation layer used to control index finger.
        /// </summary>
        [field: DocumentedByXml]
        public string indexLayerName = "Hand_Index";
        /// <summary>
        /// Name of the animation layer used to control middle finger.
        /// </summary>
        [field: DocumentedByXml]
        public string middleLayerName = "Hand_Middle";
        /// <summary>
        /// Name of the animation layer used to control ring finger.
        /// </summary>
        [field: DocumentedByXml]
        public string ringLayerName = "Hand_Ring";
        /// <summary>
        /// Name of the animation layer used to control pinky finger.
        /// </summary>
        [field: DocumentedByXml]
        public string pinkyLayerName = "Hand_Pinky";

        /// <summary>
        /// Index of the animation layer used to control thumb.
        /// </summary>
        protected int thumbLayerIndex = -1;
        /// <summary>
        /// Index of the animation layer used to control index finger.
        /// </summary>
        protected int indexLayerIndex = -1;
        /// <summary>
        /// Index of the animation layer used to control middle finger.
        /// </summary>
        protected int middleLayerIndex = -1;
        /// <summary>
        /// Index of the animation layer used to control ring finger.
        /// </summary>
        protected int ringLayerIndex = -1;
        /// <summary>
        /// Index of the animation layer used to control pinky finger.
        /// </summary>
        protected int pinkyLayerIndex = -1;

        [Header("Finger Input Sources")]
        /// <summary>
        /// Source <see cref="FloatAction"/> for getting thumb axis values.
        /// </summary>
        [field: DocumentedByXml]
        public FloatAction thumbSource;
        /// <summary>
        /// Source <see cref="FloatAction"/> for getting index axis values.
        /// </summary>
        [field: DocumentedByXml]
        public FloatAction indexSource;
        /// <summary>
        /// Source <see cref="FloatAction"/> for getting middle axis values.
        /// </summary>
        [field: DocumentedByXml]
        public FloatAction middleSource;
        /// <summary>
        /// Source <see cref="FloatAction"/> for getting ring axis values.
        /// </summary>
        [field: DocumentedByXml]
        public FloatAction ringSource;
        /// <summary>
        /// Source <see cref="FloatAction"/> for getting pinky axis values.
        /// </summary>
        [field: DocumentedByXml]
        public FloatAction pinkySource;

        /// <summary>
        /// Target values of the fingers (the ones they should be moving to).
        /// </summary>
        protected float[] fingerTargets = new float[5];
        /// <summary>
        /// Current raw values of each finger (with no overrides).
        /// </summary>
        /// <remarks>
        /// Overrides are not implemented yet - that's for later.
        /// </remarks>
        protected float[] fingerCurrentRawValues = new float[5];
        #endregion

        #region Finger Values Accessors
        /// <summary>
        /// Current <see cref="Finger.Thumb"/> value.
        /// </summary>
        public float ThumbValue => GetFingerValue(Finger.Thumb);
        /// <summary>
        /// Current <see cref="Finger.Index"/> value.
        /// </summary>
        public float IndexValue => GetFingerValue(Finger.Index);
        /// <summary>
        /// Current <see cref="Finger.Middle"/> value.
        /// </summary>
        public float MiddleValue => GetFingerValue(Finger.Middle);
        /// <summary>
        /// Current <see cref="Finger.Ring"/> value.
        /// </summary>
        public float RingValue => GetFingerValue(Finger.Ring);
        /// <summary>
        /// Current <see cref="Finger.Pinky"/> value.
        /// </summary>
        public float PinkyValue => GetFingerValue(Finger.Pinky);
        #endregion

        #region Unity callbacks
        private void Reset()
        {
            animator = GetComponent<Animator>();
        }

        protected virtual void OnEnable()
        {
            InitializeAnimatorParameters();

            SubscribeToFingerActions();

            /// Initialize explicitly into the initial state.
            InitializeFingers();
        }

        protected virtual void OnDisable()
        {
            UnsubscribeFromFingerActions();
        }

        protected virtual void LateUpdate()
        {
            LerpFingersToTargets();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Returns current value for the given <see cref="Finger"/>.
        /// </summary>
        /// <param name="finger"><see cref="Finger"/> to get value for.</param>
        /// <returns>Value.</returns>
        public float GetFingerValue(Finger finger)
        {
            switch (finger)
            {
                case Finger.Thumb:
                    {
                        return (thumbSource != null) ? thumbSource.Value : 0f;
                    }
                case Finger.Index:
                    {
                        return (indexSource != null) ? indexSource.Value : 0f;
                    }
                case Finger.Middle:
                    {
                        return (middleSource != null) ? middleSource.Value : 0f;
                    }
                case Finger.Ring:
                    {
                        return (ringSource != null) ? ringSource.Value : 0f;
                    }
                case Finger.Pinky:
                    {
                        return (pinkySource != null) ? pinkySource.Value : 0f;
                    }
                default:
                    {
                        return 0f;
                    }
            }
        }
        #endregion

        #region Initialization/subscription methods
        protected virtual void InitializeAnimatorParameters()
        {
            if (!animator) return;

            thumbLayerIndex = animator.GetLayerIndex(thumbLayerName);
            indexLayerIndex = animator.GetLayerIndex(indexLayerName);
            middleLayerIndex = animator.GetLayerIndex(middleLayerName);
            ringLayerIndex = animator.GetLayerIndex(ringLayerName);
            pinkyLayerIndex = animator.GetLayerIndex(pinkyLayerName);
        }

        protected virtual void InitializeFingers()
        {
            SetFingerTarget(Finger.Thumb, thumbSource.Value);
            SetFingerTarget(Finger.Index, indexSource.Value);
            SetFingerTarget(Finger.Middle, middleSource.Value);
            SetFingerTarget(Finger.Ring, ringSource.Value);
            SetFingerTarget(Finger.Pinky, pinkySource.Value);
        }

        protected virtual void SubscribeToFingerActions()
        {
            thumbSource?.ValueChanged.AddListener(ProcessThumbUpdate);
            indexSource?.ValueChanged.AddListener(ProcessIndexUpdate);
            middleSource?.ValueChanged.AddListener(ProcessMiddleUpdate);
            ringSource?.ValueChanged.AddListener(ProcessRingUpdate);
            pinkySource?.ValueChanged.AddListener(ProcessPinkyUpdate);
        }

        protected virtual void UnsubscribeFromFingerActions()
        {
            thumbSource?.ValueChanged.RemoveListener(ProcessThumbUpdate);
            indexSource?.ValueChanged.RemoveListener(ProcessIndexUpdate);
            middleSource?.ValueChanged.RemoveListener(ProcessMiddleUpdate);
            ringSource?.ValueChanged.RemoveListener(ProcessRingUpdate);
            pinkySource?.ValueChanged.RemoveListener(ProcessPinkyUpdate);
        }
        #endregion

        #region Finger processing callbacks
        protected virtual void ProcessThumbUpdate(float value)
        {
            SetFingerTarget(Finger.Thumb, value);
        }

        protected virtual void ProcessIndexUpdate(float value)
        {
            SetFingerTarget(Finger.Index, value);
        }

        protected virtual void ProcessMiddleUpdate(float value)
        {
            SetFingerTarget(Finger.Middle, value);
        }

        protected virtual void ProcessRingUpdate(float value)
        {
            SetFingerTarget(Finger.Ring, value);
        }

        protected virtual void ProcessPinkyUpdate(float value)
        {
            SetFingerTarget(Finger.Pinky, value);
        }
        #endregion

        #region Animation methods
        /// <summary>
        /// Sets target for the given finger.
        /// </summary>
        /// <param name="finger"><see cref="Finger"/> to set the target of.</param>
        /// <param name="value">New target value.</param>
        protected virtual void SetFingerTarget(Finger finger, float value)
        {
            int fingerIndex = (int)finger;

            fingerTargets[fingerIndex] = value;

            // Overrides are not yet implemented.
            {
                float combinedOverrideValue = 0f;

                foreach (var poseOverride in poseOverrides)
                {
                    var fingerOverride = poseOverride.GetFingerValueOverride(finger);
                    if (fingerOverride.active) combinedOverrideValue += fingerOverride.value;
                    // TODO: Finish this.
                }
                /*
                /// Set boolean finger state value.
                if (overrideAxisValues[fingerIndex] == OverrideState.NoOverride)
                {
                    fingerChangeStates[fingerIndex] = true;
                    fingerStates[fingerIndex] = (value == 0f) ? false : true;
                }

                /// Set float finger axis value.
                fingerRawAxis[fingerIndex] = value;
                if (overrideAxisValues[fingerIndex] == OverrideState.NoOverride)
                {
                    fingerAxis[fingerIndex] = value;
                }
                */
            }
        }

        /// <summary>
        /// Lerps all fingers to their current targets with the set <see cref="animationSpeed"/>.
        /// </summary>
        protected virtual void LerpFingersToTargets()
        {
            LerpFingerToTarget(Finger.Thumb);
            LerpFingerToTarget(Finger.Index);
            LerpFingerToTarget(Finger.Middle);
            LerpFingerToTarget(Finger.Ring);
            LerpFingerToTarget(Finger.Pinky);
        }

        /// <summary>
        /// Lerps given finger position to its current target with the set <see cref="animationSpeed"/>.
        /// </summary>
        /// <param name="finger"><see cref="Finger"/> to lerp.</param>
        protected virtual void LerpFingerToTarget(Finger finger)
        {
            int fingerIndex = (int)finger;

            float targetValue = fingerTargets[fingerIndex];
            float previousValue = fingerCurrentRawValues[fingerIndex];

            if (previousValue == targetValue) return;

            float newValue = previousValue;
            if (previousValue > targetValue)
            {
                newValue = previousValue - Time.deltaTime * animationSpeed;
                newValue = Mathf.Clamp(newValue, targetValue, previousValue);
            }
            if (previousValue < targetValue)
            {
                newValue = previousValue + Time.deltaTime * animationSpeed;
                newValue = Mathf.Clamp(newValue, previousValue, targetValue);
            }

            SetFingerPosition(finger, newValue);

            // Overrides are not yet implemented.
            {
                /*
                axisTypes[arrayIndex] = state;
                if (overrideAxisValues[arrayIndex] != OverrideState.NoOverride)
                {
                    if (fingerAxis[arrayIndex] != fingerForceAxis[arrayIndex])
                    {
                        LerpChangePosition(arrayIndex, fingerAxis[arrayIndex], fingerForceAxis[arrayIndex], animationSnapSpeed);
                    }
                    else if (overrideAxisValues[arrayIndex] == OverrideState.WasOverring)
                    {
                        SetOverrideValue(arrayIndex, ref overrideAxisValues, OverrideState.NoOverride);
                    }
                }
                else
                {
                    if (state == VRTK_ControllerEvents.AxisType.Digital)
                    {
                        if (fingerChangeStates[arrayIndex])
                        {
                            fingerChangeStates[arrayIndex] = false;
                            float startAxis = (fingerStates[arrayIndex] ? 0f : 1f);
                            float targetAxis = (fingerStates[arrayIndex] ? 1f : 0f);
                            LerpChangePosition(arrayIndex, startAxis, targetAxis, animationSnapSpeed);
                        }
                    }
                    else
                    {
                        SetFingerPosition(arrayIndex, fingerAxis[arrayIndex]);
                    }
                }

                //Final sanity check, if you're not touching anything but the override is still set, then clear the override.
                if (((interactTouch == null && interactNearTouch == null) || (interactNearTouch == null && interactTouch.GetTouchedObject() == null) || (interactNearTouch != null && interactNearTouch.GetNearTouchedObjects().Count == 0)) && overrideAxisValues[arrayIndex] != OverrideState.NoOverride)
                {
                    SetOverrideValue(arrayIndex, ref overrideAxisValues, OverrideState.NoOverride);
                }
                */
            }
        }

        /// <summary>
        /// Sets given finger value in the animator.
        /// </summary>
        /// <param name="finger"><see cref="Finger"/> to set position of.</param>
        /// <param name="value">The new value of the position.</param>
        protected virtual void SetFingerPosition(Finger finger, float value)
        {
            //Debug.Log("Setting " + finger.ToString() + " position to " + value);

            switch (finger)
            {
                case Finger.Thumb:
                    {
                        animator.SetLayerWeight(thumbLayerIndex, value);
                        break;
                    }
                case Finger.Index:
                    {
                        animator.SetLayerWeight(indexLayerIndex, value);
                        break;
                    }
                case Finger.Middle:
                    {
                        animator.SetLayerWeight(middleLayerIndex, value);
                        break;
                    }
                case Finger.Ring:
                    {
                        animator.SetLayerWeight(ringLayerIndex, value);
                        break;
                    }
                case Finger.Pinky:
                    {
                        animator.SetLayerWeight(pinkyLayerIndex, value);
                        break;
                    }
            }

            fingerCurrentRawValues[(int)finger] = value;

            // Overrides are not yet implemented.
            {
                /*
                if (overrideAxisValues[arrayIndex] == OverrideState.WasOverring)
                {
                    SetOverrideValue(arrayIndex, ref overrideAxisValues, OverrideState.NoOverride);
                }
                */
            }
        }
        #endregion

        #region Override methods
        public void ApplyOverride(AvatarHandPoseOverride poseOverride)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveOverride(AvatarHandPoseOverride poseOverride)
        {
            throw new System.NotImplementedException();
        }
        #endregion
    }
}