namespace Games.NoSoySauce.Avatars.Calibration.Body.Samples
{
    using System;
    using Malimbe.XmlDocumentationAttribute;
    using UnityEngine;
    using UnityEngine.Events;

    /// <summary>
    ///     Provides UnityEvents for <see cref="BodyCalibrationProfileCreator" />.
    /// </summary>
    public class BodyCalibrationProfileCreatorUnityEvents : MonoBehaviour
    {
        #region Variables

        /// <summary>
        ///     Reference to <see cref="BodyCalibrationProfileCreator" /> to get events from.
        /// </summary>
        [field: Header("References"), DocumentedByXml]
        public BodyCalibrationProfileCreator profileCreator;

        #endregion

        #region Unity Events

        /// <summary>
        ///     Event invoked when calibration starts.
        /// </summary>
        [field: Header("Calibration Lifecycle Events"), DocumentedByXml]
        public UnityEvent OnCalibrationStarted = new UnityEvent();

        /// <summary>
        ///     Event invoked when step one starts.
        /// </summary>
        [field: DocumentedByXml]
        public UnityEvent OnCalibrationStepOneStarted = new UnityEvent();

        /// <summary>
        ///     Event invoked when step one completes.
        /// </summary>
        [field: DocumentedByXml]
        public UnityEvent OnCalibrationStepOneCompleted = new UnityEvent();

        /// <summary>
        ///     Event invoked when step two starts.
        /// </summary>
        [field: DocumentedByXml]
        public UnityEvent OnCalibrationStepTwoStarted = new UnityEvent();

        /// <summary>
        ///     Event invoked when step two completes.
        /// </summary>
        [field: DocumentedByXml]
        public UnityEvent OnCalibrationStepTwoCompleted = new UnityEvent();

        /// <summary>
        ///     Event invoked when step three starts.
        /// </summary>
        [field: DocumentedByXml]
        public UnityEvent OnCalibrationStepThreeStarted = new UnityEvent();

        /// <summary>
        ///     Event invoked when step three completes.
        /// </summary>
        [field: DocumentedByXml]
        public UnityEvent OnCalibrationStepThreeCompleted = new UnityEvent();

        // TODO: implement events for steps 4 and 5

        /// <summary>
        ///     Event invoked when calibration completes.
        /// </summary>
        [field: DocumentedByXml]
        public UnityEvent OnCalibrationCompleted = new UnityEvent();

        /// <summary>
        ///     Invoked when a calibration state of current step changes to "Waiting for pose".
        /// </summary>
        [field: Header("Calibration State Events"), DocumentedByXml]
        public UnityEvent OnCalibrationStateWaitingForPose = new UnityEvent();

        /// <summary>
        ///     Invoked when a calibration state of current step changes to "Sampling".
        /// </summary>
        [field: DocumentedByXml]
        public UnityEvent OnCalibrationStateSampling = new UnityEvent();

        /// <summary>
        ///     Invoked when a calibration state of current step changes to "Idle".
        /// </summary>
        [field: DocumentedByXml]
        public UnityEvent OnCalibrationStateIdle = new UnityEvent();

        #endregion

        #region Unity Callbacks

        private void OnEnable()
        {
            SubscribeToCalibrationEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromCalibrationEvents();
        }

        #endregion

        #region Methods

        private void SubscribeToCalibrationEvents()
        {
            if (!profileCreator)
            {
                Debug.LogError($"Cannot subscribe to calibration events, as {nameof(profileCreator)} reference is not set. Please set it in the Inspector.", this);
                return;
            }

            profileCreator.OnCalibrationStarted += OnCalibrationStarted.Invoke;
            profileCreator.OnCalibrationCompleted += OnCalibrationCompleted.Invoke;
            profileCreator.OnCalibrationStepStarted += HandleCalibrationStepStartedEvent;
            profileCreator.OnCalibrationStepCompleted += HandleCalibrationStepCompletedEvent;
            profileCreator.OnCalibrationStateChanged += HandleCalibrationStateChangedEvent;
        }

        private void UnsubscribeFromCalibrationEvents()
        {
            if (!profileCreator) return;

            profileCreator.OnCalibrationStarted -= OnCalibrationStarted.Invoke;
            profileCreator.OnCalibrationCompleted -= OnCalibrationCompleted.Invoke;
            profileCreator.OnCalibrationStepStarted -= HandleCalibrationStepStartedEvent;
            profileCreator.OnCalibrationStepCompleted -= HandleCalibrationStepCompletedEvent;
            profileCreator.OnCalibrationStateChanged -= HandleCalibrationStateChangedEvent;
        }

        private void HandleCalibrationStepStartedEvent(BodyCalibrationProfileCreator.CalibrationStep step)
        {
            switch (step)
            {
                case BodyCalibrationProfileCreator.CalibrationStep.One:
                    OnCalibrationStepOneStarted.Invoke();
                    break;
                case BodyCalibrationProfileCreator.CalibrationStep.Two:
                    OnCalibrationStepTwoStarted.Invoke();
                    break;
                case BodyCalibrationProfileCreator.CalibrationStep.Three:
                    OnCalibrationStepThreeStarted.Invoke();
                    break;

                // TODO: implement events for steps 4 and 5
                default:
                    throw new ArgumentOutOfRangeException(nameof(step), step, null);
            }
        }

        private void HandleCalibrationStepCompletedEvent(BodyCalibrationProfileCreator.CalibrationStep step)
        {
            switch (step)
            {
                case BodyCalibrationProfileCreator.CalibrationStep.One:
                    OnCalibrationStepOneCompleted.Invoke();
                    break;
                case BodyCalibrationProfileCreator.CalibrationStep.Two:
                    OnCalibrationStepTwoCompleted.Invoke();
                    break;
                case BodyCalibrationProfileCreator.CalibrationStep.Three:
                    OnCalibrationStepThreeCompleted.Invoke();
                    break;

                // TODO: implement events for steps 4 and 5
                default:
                    throw new ArgumentOutOfRangeException(nameof(step), step, null);
            }
        }

        private void HandleCalibrationStateChangedEvent(BodyCalibrationProfileCreator.CalibrationState state)
        {
            switch (state)
            {
                case BodyCalibrationProfileCreator.CalibrationState.WaitingForPose:
                    OnCalibrationStateWaitingForPose.Invoke();
                    break;
                case BodyCalibrationProfileCreator.CalibrationState.Sampling:
                    OnCalibrationStateSampling.Invoke();
                    break;
                case BodyCalibrationProfileCreator.CalibrationState.Idle:
                    OnCalibrationStateIdle.Invoke();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        #endregion
    }
}