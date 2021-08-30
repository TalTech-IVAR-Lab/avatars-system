namespace Games.NoSoySauce.Avatars.Calibration.Body.Samples
{
    using System;
    using UnityEngine;
    using Zinnia.Process;

    /// <summary>
    ///     Visualizes limits in which user's head and wrists must be during current calibration step.
    /// </summary>
    /// <remarks>
    ///     Intended to be used with Calibration VR Rig Visualizer.
    /// </remarks>
    public class BodyCalibrationProfileCreatorVolumeVisualizer : MonoBehaviour, IProcessable
    {
        #region Public Variables

        [Header("References")]
        public BodyCalibrationProfileCreator calibrationProfileCreator;

        public Transform centerEye;
        public Transform leftWrist;
        public Transform rightWrist;

        [Header("Materials")]
        public Material waitingVolumeMaterial;

        public Material samplingVolumeMaterial;
        public Material idleVolumeMaterial;

        #endregion

        #region Internal Variables

        private Transform wristVolumeTransform;
        private MeshRenderer wristVolumeRenderer;

        private float LinearCalibrationTolerance => calibrationProfileCreator.linearCalibrationTolerance;
        private float AngularCalibrationTolerance => calibrationProfileCreator.angularCalibrationTolerance;

        /// <summary>
        ///     Position of player headset in play area's coordinate system.
        /// </summary>
        private Pose CenterEyePose => new Pose(centerEye.localPosition, centerEye.localRotation);

        /// <summary>
        ///     Position of player's left wrist in play area's coordinate system.
        /// </summary>
        private Pose LeftWristPose => new Pose(leftWrist.localPosition, leftWrist.localRotation);

        /// <summary>
        ///     Position of player's right wrist in play area's coordinate system.
        /// </summary>
        private Pose RightWristPose => new Pose(rightWrist.localPosition, rightWrist.localRotation);

        /// <summary>
        ///     Position of player headset in play area's coordinate system.
        /// </summary>
        private Vector3 CenterEyePosition => CenterEyePose.position;

        /// <summary>
        ///     Rotation of player headset in play area's coordinate system.
        /// </summary>
        private Quaternion CenterEyeRotation => CenterEyePose.rotation;

        /// <summary>
        ///     Position of player's left wrist in play area's coordinate system.
        /// </summary>
        private Vector3 LeftWristPosition => LeftWristPose.position;

        /// <summary>
        ///     Position of player's right wrist in play area's coordinate system.
        /// </summary>
        private Vector3 RightWristPosition => RightWristPose.position;

        #endregion

        #region Unity Callbacks

        private void OnEnable()
        {
            CreateVolumes();
        }

        private void OnDisable()
        {
            DestroyVolumes();
        }

        public void Process()
        {
            UpdateVolumes();
        }

        #endregion

        #region Internal Methods

        private void CreateVolumes()
        {
            wristVolumeTransform = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;

            // Give volumes names
            wristVolumeTransform.gameObject.name = "[Wrist Calibration Volume Visualizer]";

            // Set non-editable flags
            // wristVolumeTransform.gameObject.hideFlags = HideFlags.NotEditable;

            // Make all volumes siblings of center eye object, so that they have the same parent in hierarchy and same local coordinate system
            wristVolumeTransform.SetParent(centerEye.parent, false);

            // Grab mesh renderers for changing materials depending on calibration state
            wristVolumeRenderer = wristVolumeTransform.GetComponent<MeshRenderer>();

            UpdateVolumes();
        }

        private void DestroyVolumes()
        {
            if (wristVolumeTransform) Destroy(wristVolumeTransform);
        }

        private void UpdateVolumes()
        {
            UpdateWristVolumes();
            UpdateHeadVolumes();
        }

        private void UpdateWristVolumes()
        {
            /* Update state */
            {
                bool calibrationRunning = calibrationProfileCreator.CurrentCalibrationState != BodyCalibrationProfileCreator.CalibrationState.Idle;

                wristVolumeTransform.gameObject.SetActive(calibrationRunning);

                // Nothing else to update if meshes are disabled
                if (!calibrationRunning) return;
            }

            /* Update material */
            {
                switch (calibrationProfileCreator.CurrentCalibrationState)
                {
                    case BodyCalibrationProfileCreator.CalibrationState.WaitingForPose:
                        wristVolumeRenderer.material = waitingVolumeMaterial;
                        break;
                    case BodyCalibrationProfileCreator.CalibrationState.Sampling:
                        wristVolumeRenderer.material = samplingVolumeMaterial;
                        break;
                    case BodyCalibrationProfileCreator.CalibrationState.Idle:
                        wristVolumeRenderer.material = idleVolumeMaterial;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            /* Update dimensions and material */

            float lowerVolumeEdge = 0f;
            float upperVolumeEdge = 0f;

            BodyCalibrationProfileCreator.CalibrationStep currentStep = calibrationProfileCreator.CurrentCalibrationStep;
            switch (currentStep)
            {
                case BodyCalibrationProfileCreator.CalibrationStep.None:
                {
                    wristVolumeTransform.localScale = Vector3.zero;
                    return;
                }
                case BodyCalibrationProfileCreator.CalibrationStep.One:
                {
                    lowerVolumeEdge = BodyCalibrationProfileCreator.STEP_ONE_WRISTS_VOLUME_LOWER_EDGE;
                    upperVolumeEdge = BodyCalibrationProfileCreator.STEP_ONE_WRISTS_VOLUME_UPPER_EDGE;
                    break;
                }
                case BodyCalibrationProfileCreator.CalibrationStep.Two:
                {
                    lowerVolumeEdge = BodyCalibrationProfileCreator.STEP_TWO_WRISTS_VOLUME_LOWER_EDGE;
                    upperVolumeEdge = BodyCalibrationProfileCreator.STEP_TWO_WRISTS_VOLUME_UPPER_EDGE;
                    break;
                }
                case BodyCalibrationProfileCreator.CalibrationStep.Three:
                {
                    lowerVolumeEdge = BodyCalibrationProfileCreator.STEP_THREE_WRISTS_VOLUME_LOWER_EDGE;
                    upperVolumeEdge = BodyCalibrationProfileCreator.STEP_THREE_WRISTS_VOLUME_UPPER_EDGE;
                    break;
                }
                case BodyCalibrationProfileCreator.CalibrationStep.Four:
                {
                    // TODO: implement calibration step 4
                    break;
                }
                case BodyCalibrationProfileCreator.CalibrationStep.Five:
                {
                    // TODO: implement calibration step 5
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            /* Position */

            // Find XY plane of the headset.
            // All calculations are made in local space of VR rig play area.
            Vector3 centerEyeForwardProjected = Vector3.ProjectOnPlane(CenterEyePose.forward, Vector3.up).normalized;
            if (Mathf.Approximately(centerEyeForwardProjected.sqrMagnitude, 0f)) centerEyeForwardProjected = Vector3.ProjectOnPlane(CenterEyePose.up, Vector3.up).normalized;
            var headXYPlane = new Plane(centerEyeForwardProjected, CenterEyePosition);

            // Get unsigned distances from wrists to the plane 
            float leftWristToXYPlaneSigned = headXYPlane.GetDistanceToPoint(LeftWristPosition);
            float rightWristToXYPlaneSigned = headXYPlane.GetDistanceToPoint(RightWristPosition);
            Vector3 volumeForwardOffsetVector = CenterEyePosition + centerEyeForwardProjected * (leftWristToXYPlaneSigned + rightWristToXYPlaneSigned) / 2f;
            float volumeForwardOffset = volumeForwardOffsetVector.z;

            wristVolumeTransform.localPosition = new Vector3
            {
                x = CenterEyePosition.x,
                y = CenterEyePosition.y * 0.5f * (upperVolumeEdge + lowerVolumeEdge),
                z = CenterEyePosition.z + volumeForwardOffset
            };

            /* Rotation */

            wristVolumeTransform.localRotation = Quaternion.LookRotation(centerEyeForwardProjected, Vector3.up);

            /* Scale */

            // Find YZ plane of the headset.
            // All calculations are made in local space of VR rig play area.
            Vector3 centerEyeRightProjected = Vector3.ProjectOnPlane(CenterEyePose.right, Vector3.up).normalized;
            if (Mathf.Approximately(centerEyeRightProjected.sqrMagnitude, 0f)) centerEyeRightProjected = Vector3.ProjectOnPlane(CenterEyePose.up, Vector3.up).normalized;
            var headYZPlane = new Plane(centerEyeRightProjected, CenterEyePosition);

            // Get absolute distances from wrists to the plane 
            float leftWristToYZPlaneAbs = Mathf.Abs(headYZPlane.GetDistanceToPoint(LeftWristPosition));
            float rightWristToYZPlaneAbs = Mathf.Abs(headYZPlane.GetDistanceToPoint(RightWristPosition));

            // Find wrist minimal distance from headset YZ plane
            float minWristDistanceToSide = Mathf.Min(leftWristToYZPlaneAbs, rightWristToYZPlaneAbs);

            wristVolumeTransform.localScale = new Vector3
            {
                x = minWristDistanceToSide * 2f + LinearCalibrationTolerance * 2f,
                y = CenterEyePosition.y * (upperVolumeEdge - lowerVolumeEdge),
                z = LinearCalibrationTolerance
            };
        }

        private void UpdateHeadVolumes()
        {
            // No actual volume needed as only head rotation is accounted during calibration.
            // All focus is on wrists.
        }

        #endregion
    }
}