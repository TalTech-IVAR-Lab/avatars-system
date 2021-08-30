namespace Games.NoSoySauce.Avatars.Calibration.Body
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Malimbe.PropertySerializationAttribute;
    // using Malimbe.XmlDocumentationAttribute;
    using ZinniaExtensions.Action;
    using UnityEngine;
    using Universal.Coroutines;
    using Zinnia.Extension;

    /// <summary>
    ///     Contains methods to create a new <see cref="BodyCalibrationProfile" />.
    /// </summary>
    /// <remarks>
    ///     Calibration profile contains data about the physical dimensions of the player's body,
    ///     and can be applied to different types of virtual avatars to align them with the player.
    /// </remarks>

    // TODO: add events for when head/wrists enter/exit correct sampling poses?
    // TODO: add sideways limitation to sampling volumes
    public class BodyCalibrationProfileCreator : MonoBehaviour
    {
        #region Data Types

        [Serializable]
        public enum CalibrationStep
        {
            None,
            One,
            Two,
            Three,
            Four,
            Five
        }

        [Serializable]
        public enum CalibrationState
        {
            /// <summary>
            ///     When the system is waiting for the user to get into required pose.
            /// </summary>
            WaitingForPose,

            /// <summary>
            ///     When the user has got into required pose and the system is sampling values.
            /// </summary>
            Sampling,

            /// <summary>
            ///     When calibration process is not looking for samples.
            /// </summary>
            Idle
        }

        #endregion

        #region Constants

        /* Calibration sampling volumes settings */

        // These values are deduced empirically based on average human body proportions.
        // They are not super-strict, but provide good enough limitations to expect the user's
        // wrists to be in a pose desired for given step.
        // Example human proportions: https://i.pinimg.com/564x/20/4c/fd/204cfd64fa366918b712c0538a7c5f37.jpg

        public const float STEP_ONE_WRISTS_VOLUME_LOWER_EDGE = 2f / 8f;
        public const float STEP_ONE_WRISTS_VOLUME_UPPER_EDGE = 6f / 8f;
        public const float STEP_TWO_WRISTS_VOLUME_LOWER_EDGE = 6f / 8f;
        public const float STEP_TWO_WRISTS_VOLUME_UPPER_EDGE = 8f / 8f;
        public const float STEP_THREE_WRISTS_VOLUME_LOWER_EDGE = 7f / 8f;
        public const float STEP_THREE_WRISTS_VOLUME_UPPER_EDGE = 10f / 8f;

        #endregion

        #region Static Methods

        /// <summary>
        ///     Calculates an average value of the float list.
        /// </summary>
        private static float CalculateAverage(IReadOnlyList<float> samples)
        {
            int count = samples.Count;
            float average = 0f;
            for (int i = 0; i < count; i++) { average += samples[i]; }

            average /= count;
            return average;
        }

        /// <summary>
        ///     Creates new empty BodyCalibrationProfile with all fields set to 0.
        ///     If we create a Profile without setting all values to 0, it can lead to errors later (not all values are usually
        ///     added, i.e. hipToGround and kneeToGround will only be set when the user chooses full calibration mode).
        ///     This is used internally to create new Profiles.
        /// </summary>
        private static BodyCalibrationProfile GenerateEmptyCalibrationProfile()
        {
            return ScriptableObject.CreateInstance<BodyCalibrationProfile>();
        }

        #endregion

        #region Public Variables

        /// <summary>
        ///     Linear tolerance to be applied when collecting calibration data from the player, measured in m.
        /// </summary>
        // [field: Header("Settings"), Range(0.01f, 0.10f), DocumentedByXml]
        public float linearCalibrationTolerance = 0.05f;

        /// <summary>
        ///     Angular tolerance to be applied when collecting calibration data from the player, measured in degrees.
        /// </summary>
        // [field: Range(1f, 30f), DocumentedByXml]
        public float angularCalibrationTolerance = 10f;

        /// <summary>
        ///     Number of samples to collect for averaging values.
        /// </summary>
        // [field: DocumentedByXml]
        public int sampleCount = 45;

        /// <summary>
        ///     Time in seconds between the moment when user enters the required pose and when the measurements are taken.
        ///     Increasing it can improve measurements accuracy, but calibration will take more time.
        /// </summary>
        // [field: DocumentedByXml]
        public float samplingDelay = 1f;
        
        /// <summary>
        /// Name of the profile to be created. Resets after each new profile creation.
        /// </summary>
        // [field: DocumentedByXml]
        public string profileName = null;

        /// <summary>
        ///     <see cref="BodyCalibrationProfile" /> which is currently being created, if any.
        /// </summary>
        [Serialized]
        // [field: DocumentedByXml]
        public BodyCalibrationProfile CurrentCalibrationProfile { get; private set; }

        public CalibrationStep CurrentCalibrationStep { get; private set; } = CalibrationStep.None;

        public CalibrationState CurrentCalibrationState { get; private set; } = CalibrationState.Idle;

        public float SamplingProgress { get; private set; }

        #endregion

        #region Internal Variables

        /// <summary>
        ///     <see cref="PoseAction" /> which provides a local pose of the user's headset camera (so-called "center eye") in play
        ///     area.
        /// </summary>
        // [field: Header("Pose Inputs"), DocumentedByXml]
        public PoseAction headsetLocalPoseAction;
        
        /// <summary>
        ///     <see cref="PoseAction" /> which provides a local pose of the user's left wrist in play area.
        /// </summary>
        /// <remarks>
        ///     For precise calibration, this should be the actual wrist transform, not the raw controller one.
        /// </remarks>
        // [field: DocumentedByXml]
        public PoseAction leftWristLocalPoseAction;

        /// <summary>
        ///     <see cref="PoseAction" /> which provides a local pose of the user's right wrist in play area.
        /// </summary>
        /// <remarks>
        ///     For precise calibration, this should be the actual wrist transform, not the raw controller one.
        /// </remarks>
        // [field: DocumentedByXml]
        public PoseAction rightWristLocalPoseAction;

        /// <summary>
        ///     Position of player headset in play area's coordinate system.
        /// </summary>
        private Pose CenterEyePose => headsetLocalPoseAction.Value;

        /// <summary>
        ///     Position of player's left wrist in play area's coordinate system.
        /// </summary>
        private Pose LeftWristPose => leftWristLocalPoseAction.Value;

        /// <summary>
        ///     Position of player's right wrist in play area's coordinate system.
        /// </summary>
        private Pose RightWristPose => rightWristLocalPoseAction.Value;

        /// <summary>
        ///     Position of player headset in play area's coordinate system.
        /// </summary>
        private Vector3 CenterEyePosition => CenterEyePose.position;

        /// <summary>
        ///     Position of player's left wrist in play area's coordinate system.
        /// </summary>
        private Vector3 LeftWristPosition => LeftWristPose.position;

        /// <summary>
        ///     Position of player's right wrist in play area's coordinate system.
        /// </summary>
        private Vector3 RightWristPosition => RightWristPose.position;

        /// <summary>
        ///     Current calibration coroutine.
        /// </summary>
        private BuffedCoroutine calibrationCoroutine = new BuffedCoroutine(null);

        #endregion

        #region Events

        /// <summary>
        ///     Invoked when calibration starts.
        /// </summary>
        public event Action OnCalibrationStarted;

        /// <summary>
        ///     Invoked when a calibration step starts.
        /// </summary>
        public event Action<CalibrationStep> OnCalibrationStepStarted;

        /// <summary>
        ///     Invoked when a calibration state of current step changes.
        /// </summary>
        public event Action<CalibrationState> OnCalibrationStateChanged;

        /// <summary>
        ///     Invoked when a calibration step completes.
        /// </summary>
        public event Action<CalibrationStep> OnCalibrationStepCompleted;

        /// <summary>
        ///     Invoked when calibration completes.
        /// </summary>
        public event Action OnCalibrationCompleted;

        #endregion

        #region Public Methods

        /// <summary>
        ///     Starts a new calibration coroutine.
        /// </summary>
        public void StartNewCalibration()
        {
            StopCalibration();
            calibrationCoroutine = BuffedCoroutine.StartCoroutine(NewCalibrationProfile_Coroutine());
            calibrationCoroutine.OnExecutionFinished += stoppedExplicitly =>
            {
                // Reset all variables when the calibration stops 
                CurrentCalibrationProfile = null;
                CurrentCalibrationStep = CalibrationStep.None;
                CurrentCalibrationState = CalibrationState.Idle;
            };
        }

        /// <summary>
        ///     Stops current calibration coroutine, if any is running.
        /// </summary>
        public void StopCalibration()
        {
            if (calibrationCoroutine.IsRunning) calibrationCoroutine.Stop();
        }

        #endregion

        #region Internal Methods (Calibration)

        private IEnumerator NewCalibrationProfile_Coroutine()
        {
            // Invoke calibration started event
            OnCalibrationStarted?.Invoke();

            // Create a new calibration profile
            Debug.Log("Creating new calibration profile.", this);
            CurrentCalibrationProfile = GenerateEmptyCalibrationProfile();

            // NOTE: Each calibration step is split into repeating functions which are then fed into a common wrapper method.
            //       See CalibrationStepWrapper_Coroutine() for more info on how this works.
            /* Step 1 of calibration: standing straight */
            bool CheckPose_StepOne()
            {
                // Step 1 check: Left and right wrists are located in the lower 2/3 of the body when the user stands straight.
                // return CheckWristsInVerticalRangeRelativeToHead(STEP_ONE_WRISTS_VOLUME_LOWER_EDGE, STEP_ONE_WRISTS_VOLUME_UPPER_EDGE)
                // && UserSymmetricAndLooksForward();
                return UserSymmetricAndLooksForward();
            }

            void SampleValues_StepOne(List<float[]> sampleCollections, int i)
            {
                // Extract collections of samples into variables
                float[] floorToVisorSamples = sampleCollections[0];
                float[] shouldersSpreadSamples = sampleCollections[1];

                // Add new samples
                float visorAltitude = CenterEyePosition.y;
                floorToVisorSamples[i] = visorAltitude;
                shouldersSpreadSamples[i] = Vector3.Distance(LeftWristPosition, RightWristPosition);
            }

            void StoreValues_StepOne(List<float[]> sampleCollections)
            {
                // Extract collections of samples into variables
                float[] floorToVisorSamples = sampleCollections[0];
                float[] shouldersSpreadSamples = sampleCollections[1];

                // Store averaged samples in calibration profile
                CurrentCalibrationProfile.floorToEyes = CalculateAverage(floorToVisorSamples);
                CurrentCalibrationProfile.shouldersSpread = CalculateAverage(shouldersSpreadSamples);
            }

            /* Step 2 of calibration: standing in T-pose */
            bool CheckPose_StepTwo()
            {
                // Step 2 check: Left and right wrists are located in the upper 1/2 of the body, but below the head when the user stands in T-pose.
                return CheckWristsInVerticalRangeRelativeToHead(STEP_TWO_WRISTS_VOLUME_LOWER_EDGE, STEP_TWO_WRISTS_VOLUME_UPPER_EDGE)
                       && UserSymmetricAndLooksForward();
            }

            void SampleValues_StepTwo(List<float[]> sampleCollections, int i)
            {
                // Extract collections of samples into variables
                float[] floorToVisorSamples = sampleCollections[0];
                float[] groundToShoulderSamples = sampleCollections[1];
                float[] wristsSpreadSamples = sampleCollections[2];
                float[] wristToShoulderSamples = sampleCollections[3];

                float visorAltitude = CenterEyePosition.y;
                float leftWristAltitude = LeftWristPosition.y;
                float rightWristAltitude = RightWristPosition.y;

                // Add new samples
                floorToVisorSamples[i] = visorAltitude;
                groundToShoulderSamples[i] = (leftWristAltitude + rightWristAltitude) / 2;
                wristsSpreadSamples[i] = Vector3.Distance(LeftWristPosition, RightWristPosition);
                wristToShoulderSamples[i] = (wristsSpreadSamples[i] - CurrentCalibrationProfile.shouldersSpread) / 2;
            }

            void StoreValues_StepTwo(List<float[]> sampleCollections)
            {
                // Extract collections of samples into variables
                float[] floorToVisorSamples = sampleCollections[0];
                float[] groundToShoulderSamples = sampleCollections[1];
                float[] wristsSpreadSamples = sampleCollections[2];
                float[] wristToShoulderSamples = sampleCollections[3];

                // Store averaged samples in calibration profile
                CurrentCalibrationProfile.floorToEyes = (CurrentCalibrationProfile.floorToEyes + CalculateAverage(floorToVisorSamples)) / 2;
                CurrentCalibrationProfile.floorToShoulder = CalculateAverage(groundToShoulderSamples);
                CurrentCalibrationProfile.wristsSpread = CalculateAverage(wristsSpreadSamples);
                CurrentCalibrationProfile.wristToShoulder = CalculateAverage(wristToShoulderSamples);
            }

            /* Step 3 of calibration: standing in Y-pose (T-pose with hands bent upwards) */
            bool CheckPose_StepThree()
            {
                // Step 3 check: Left and right wrists are located above the head when the user stands in Y-pose.
                return CheckWristsInVerticalRangeRelativeToHead(STEP_THREE_WRISTS_VOLUME_LOWER_EDGE, STEP_THREE_WRISTS_VOLUME_UPPER_EDGE)
                       && UserSymmetricAndLooksForward();
            }

            void SampleValues_StepThree(List<float[]> sampleCollections, int i)
            {
                // Extract collections of samples into variables
                float[] floorToVisorSamples = sampleCollections[0];
                float[] lowerArmLengthSamples = sampleCollections[1];
                float[] upperArmLengthSamples = sampleCollections[2];

                float visorAltitude = CenterEyePosition.y;
                float leftWristAltitude = LeftWristPosition.y;
                float rightWristAltitude = RightWristPosition.y;

                // Add new samples
                floorToVisorSamples[i] = visorAltitude;
                lowerArmLengthSamples[i] = (leftWristAltitude + rightWristAltitude) / 2f - CurrentCalibrationProfile.floorToShoulder;
                upperArmLengthSamples[i] = (Vector3.Distance(LeftWristPosition, RightWristPosition) - CurrentCalibrationProfile.shouldersSpread) / 2;
            }

            void StoreValues_StepThree(List<float[]> sampleCollections)
            {
                // Extract collections of samples into variables
                float[] floorToVisorSamples = sampleCollections[0];
                float[] lowerArmLengthSamples = sampleCollections[1];
                float[] upperArmLengthSamples = sampleCollections[2];

                // Store averaged samples in calibration profile
                CurrentCalibrationProfile.floorToEyes = (CurrentCalibrationProfile.floorToEyes + CalculateAverage(floorToVisorSamples)) / 2;
                CurrentCalibrationProfile.upperArmLength = CalculateAverage(lowerArmLengthSamples);
                CurrentCalibrationProfile.lowerArmLength = CalculateAverage(upperArmLengthSamples);
            }

            /* Step 4 of calibration: wrists on thighs */
            {
                // To be added later...
                // TODO: implement thighs calibration
            }

            /* Step 5 of calibration: wrists on knees */
            {
                // To be added later...
                // TODO: implement knees calibration
            }

            // Run all calibration steps
            yield return CalibrationStepWrapper_Coroutine(CalibrationStep.One, 2, CheckPose_StepOne, SampleValues_StepOne, StoreValues_StepOne);
            yield return CalibrationStepWrapper_Coroutine(CalibrationStep.Two, 4, CheckPose_StepTwo, SampleValues_StepTwo, StoreValues_StepTwo);
            yield return CalibrationStepWrapper_Coroutine(CalibrationStep.Three, 3, CheckPose_StepThree, SampleValues_StepThree, StoreValues_StepThree);

            // TODO: implement step 4
            // TODO: implement step 5

            // Save the created profile to disk
            CurrentCalibrationProfile.Save(profileName);

            // Invoke calibration completed event
            OnCalibrationCompleted?.Invoke();

            // Reset variables
            profileName = null;
            CurrentCalibrationProfile = null;
            CurrentCalibrationStep = CalibrationStep.None;
            ChangeCalibrationState(CalibrationState.Idle);
        }


        /// <summary>
        ///     Wraps common logic of each calibration step into a single method.
        /// </summary>
        /// <remarks>
        ///     Every calibration step can be broken down to this simple algorithm:
        ///     1. Wait for user to enter the required pose
        ///     2. Once user in a pose, keep sampling calibration values until we hit sampleCount
        ///     3. If the user leaves pose during sampling, reset the counter repeat the wait
        ///     4. Once sampling is completed, store sampled values in the calibration profile
        ///     This method takes 3 variable parts of this algorithm as input functions:
        ///     - Function to check if user is in correct pose
        ///     - Function to sample values
        ///     - Function to store values in the calibration profile
        ///     The rest (common stuff for all steps) is wrapped inside this method.
        /// </remarks>
        /// <param name="calibrationStep">Current <see cref="CalibrationStep" />.</param>
        /// <param name="sampleCollectionsCount">
        ///     Number of lists of samples to create. These lists will be fed into provided
        ///     sampling and storing functions.
        /// </param>
        /// <param name="PoseCheckMethod">
        ///     Function which checks if the user is in correct pose to start collecting samples for this
        ///     calibration step.
        /// </param>
        /// <param name="CollectSamplesMethod">
        ///     Function which collects samples. Called at sampleInterval when the user is in
        ///     correct pose.
        /// </param>
        /// <param name="StoreSamplesMethod">Function which stores collected samples in a calibration profile.</param>
        private IEnumerator CalibrationStepWrapper_Coroutine(CalibrationStep calibrationStep, int sampleCollectionsCount, Func<bool> PoseCheckMethod,
            Action<List<float[]>, int> CollectSamplesMethod, Action<List<float[]>> StoreSamplesMethod)
        {
            // Calibration step starts
            Debug.Log($"Starting calibration step {calibrationStep.ToString().ToLower()}.", this);
            CurrentCalibrationStep = calibrationStep;
            OnCalibrationStepStarted?.Invoke(calibrationStep);
            SamplingProgress = 0f;
            ChangeCalibrationState(CalibrationState.WaitingForPose);

            // Initialize required number collections for storing measurement samples
            var sampleCollections = new List<float[]>();
            for (int j = 0; j < sampleCollectionsCount; j++) { sampleCollections.Add(new float[sampleCount]); }

            // Initialize variables used in loop
            int i = -1;
            bool stepComplete = false;
            var waitForFixedUpdate = new WaitForFixedUpdate();
            float samplingDelayTimer = samplingDelay;
            while (!stepComplete)
            {
                yield return waitForFixedUpdate;

                // Update progress counter
                int samplesCollected = i + 1;
                SamplingProgress = samplesCollected / (float) sampleCount;

                // Check if user is near the correct position to start measurements
                if (PoseCheckMethod())
                {
                    // If state just changed, invoke event
                    ChangeCalibrationState(CalibrationState.Sampling);

                    // If the user just entered the correct position, we wait for samplingDelay, and
                    // then start taking measurements until the required number of samples is collected.
                    if (samplingDelayTimer > 0f)
                    {
                        samplingDelayTimer -= Time.deltaTime;
                        continue;
                    }

                    // Collect samples
                    i++;
                    if (i < sampleCount)
                    {
                        CollectSamplesMethod(sampleCollections, i);
                        continue;
                    }

                    // When enough samples are collected, calibration step is complete
                    stepComplete = true;
                }
                else
                {
                    // If state just changed, invoke event
                    ChangeCalibrationState(CalibrationState.WaitingForPose);

                    // Reset the counter, timer and clear all sample collections if the user went out of the calibration pose.
                    i = -1;
                    samplingDelayTimer = samplingDelay;
                    foreach (float[] collection in sampleCollections)
                    {
                        for (int j = 0; j < collection.Length; j++) { collection[j] = 0f; }
                    }
                }
            }

            // Store collected samples in current calibration profile
            StoreSamplesMethod(sampleCollections);

            // Calibration step completed
            SamplingProgress = 1f;
            ChangeCalibrationState(CalibrationState.Idle);
            OnCalibrationStepCompleted?.Invoke(calibrationStep);
            Debug.Log($"Calibration step {calibrationStep.ToString().ToLower()} complete.", this);
        }

        #endregion

        #region Internal Methods (Utilities)

        /// <summary>
        ///     Changes current <see cref="CurrentCalibrationState" /> and invokes an event about it.
        /// </summary>
        /// <param name="newState">New <see cref="CalibrationState" />.</param>
        private void ChangeCalibrationState(CalibrationState newState)
        {
            if (newState == CurrentCalibrationState) return;

            CurrentCalibrationState = newState;
            OnCalibrationStateChanged?.Invoke(newState);
        }


        /// <summary>
        ///     Checks if the user is in symmetric pose with his head straight forward.
        ///     This check is used in any of the calibration steps, as all the calibration poses require user to hold controllers
        ///     symmetrically from both sides of his head.
        /// </summary>
        private bool UserSymmetricAndLooksForward()
        {
            return WristsOnCorrectSidesFromTheHead()
                   && WristsSymmetricToTheHead()
                   && WristsOnTheSameAltitude()
                   && WristsOnTheSameXYPlaneRelativeToTheHead()
                   && UserHeadVertical();
        }

        /// <summary>
        ///     Checks if left wrist is on the left side from the headset, and right is on the right.
        /// </summary>
        private bool WristsOnCorrectSidesFromTheHead()
        {
            // Find YZ plane of the headset.
            // All calculations are made in local space of VR rig play area.
            Vector3 centerEyeRightProjected = Vector3.ProjectOnPlane(CenterEyePose.right, Vector3.up).normalized;
            if (Mathf.Approximately(centerEyeRightProjected.sqrMagnitude, 0f)) centerEyeRightProjected = Vector3.ProjectOnPlane(CenterEyePose.up, Vector3.up).normalized;
            var headYZPlane = new Plane(centerEyeRightProjected, CenterEyePosition);

            // Get signed distances from wrists to the plane 
            float leftWristToPlaneSigned = headYZPlane.GetDistanceToPoint(LeftWristPosition);
            float rightWristToPlaneSigned = headYZPlane.GetDistanceToPoint(RightWristPosition);

            return leftWristToPlaneSigned < 0f && rightWristToPlaneSigned > 0f;
        }

        /// <summary>
        ///     Checks if the user's wrists are positioned symmetrically relative to the headset's YZ plane.
        /// </summary>
        private bool WristsSymmetricToTheHead()
        {
            // Find YZ plane of the headset.
            // All calculations are made in local space of VR rig play area.
            Vector3 centerEyeRightProjected = Vector3.ProjectOnPlane(CenterEyePose.right, Vector3.up).normalized;
            if (Mathf.Approximately(centerEyeRightProjected.sqrMagnitude, 0f)) centerEyeRightProjected = Vector3.ProjectOnPlane(CenterEyePose.up, Vector3.up).normalized;
            var headYZPlane = new Plane(centerEyeRightProjected, CenterEyePosition);

            // Get absolute distances from wrists to the plane 
            float leftWristToPlaneAbs = Mathf.Abs(headYZPlane.GetDistanceToPoint(LeftWristPosition));
            float rightWristToPlaneAbs = Mathf.Abs(headYZPlane.GetDistanceToPoint(RightWristPosition));

            return leftWristToPlaneAbs.ApproxEquals(rightWristToPlaneAbs, linearCalibrationTolerance);
        }

        /// <summary>
        ///     Checks if the user's wrists are located almost on the same altitude (distance from the calibrationRoot).
        /// </summary>
        private bool WristsOnTheSameAltitude()
        {
            float leftWristAltitude = LeftWristPosition.y;
            float rightWristAltitude = RightWristPosition.y;

            return leftWristAltitude.ApproxEquals(rightWristAltitude, linearCalibrationTolerance);
        }

        /// <summary>
        ///     Checks if the user's wrists are located almost on the same XY plane parallel to headset's XY plane.
        /// </summary>
        private bool WristsOnTheSameXYPlaneRelativeToTheHead()
        {
            // Find XY plane of the headset.
            // All calculations are made in local space of VR rig play area.
            Vector3 centerEyeForwardProjected = Vector3.ProjectOnPlane(CenterEyePose.forward, Vector3.up).normalized;
            if (Mathf.Approximately(centerEyeForwardProjected.sqrMagnitude, 0f)) centerEyeForwardProjected = Vector3.ProjectOnPlane(CenterEyePose.up, Vector3.up).normalized;
            var headXYPlane = new Plane(centerEyeForwardProjected, CenterEyePosition);

            // Get signed distances from wrists to the plane 
            float leftWristToPlaneSigned = headXYPlane.GetDistanceToPoint(LeftWristPosition);
            float rightWristToPlaneSigned = headXYPlane.GetDistanceToPoint(RightWristPosition);

            return leftWristToPlaneSigned.ApproxEquals(rightWristToPlaneSigned, linearCalibrationTolerance);
        }

        /// <summary>
        ///     Checks whether both wrists are located in a vertical region of the body defined by lower and upper edge.
        ///     Edge parameters are given as fractions of the full headset altitude.
        /// </summary>
        /// <param name="lowerEdge">Lower edge of the vertical region in which the wrists must be.</param>
        /// <param name="upperEdge">Upper edge of the vertical region in which the wrists must be.</param>
        private bool CheckWristsInVerticalRangeRelativeToHead(float lowerEdge, float upperEdge)
        {
            float headAltitude = CenterEyePosition.y;
            float lowerEdgeAltitude = headAltitude * lowerEdge;
            float upperEdgeAltitude = headAltitude * upperEdge;

            float leftWristAltitude = LeftWristPosition.y;
            float rightWristAltitude = RightWristPosition.y;

            // Shortcut local function to check if the value is the given range (inclusive)
            bool InRange(float value, float from, float to)
            {
                return value >= from && value <= to;
            }

            return InRange(leftWristAltitude, lowerEdgeAltitude, upperEdgeAltitude) &&
                   InRange(rightWristAltitude, lowerEdgeAltitude, upperEdgeAltitude);
        }

        /// <summary>
        ///     Checks if the user's head is placed vertically (line of sight parallel to the ground).
        /// </summary>
        private bool UserHeadVertical()
        {
            // Angle between play area's up direction and user head's up direction
            Vector3 headUpwardDirection = CenterEyePose.rotation * Vector3.up;
            float verticalAngleDelta = Vector3.Angle(headUpwardDirection, Vector3.up);

            return verticalAngleDelta.ApproxEquals(0f, angularCalibrationTolerance);
        }

        #endregion
    }
}