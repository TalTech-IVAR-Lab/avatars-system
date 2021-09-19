namespace Games.NoSoySauce.Avatars.Calibration
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using Malimbe.XmlDocumentationAttribute;
    using UnityEngine;
    using Zinnia.Extension;

    /// <summary>
    ///     Contains methods to create a new <see cref="BodyCalibrationProfile" />.
    /// </summary>
    public class BodyCalibrationProfileCreator : MonoBehaviour
    {
        #region Variables (settings)

        /// <summary>
        ///     Linear tolerance to be applied when collecting calibration data from the player, measured in m.
        /// </summary>
        [field: Header("Settings"), Range(0.01f, 0.10f), DocumentedByXml]
        public float linearCalibrationTolerance = 0.05f;

        /// <summary>
        ///     Angular tolerance to be applied when collecting calibration data from the player, measured in degrees.
        /// </summary>
        [field: Range(1f, 20f), DocumentedByXml]
        public float angularCalibrationTolerance = 10f;

        /// <summary>
        ///     Number of samples to collect for averaging values.
        /// </summary>
        [field: DocumentedByXml]
        public int sampleCount = 45;

        /// <summary>
        ///     Time in seconds between the moment when user enters the required pose and when the measurements are taken.
        ///     Increasing it can improve measurements accuracy, but calibration will take more time.
        /// </summary>
        [field: DocumentedByXml]
        public float measurementDelay = 1f;

        /// <summary>
        ///     Path where a newly created profile file must be saved.
        /// </summary>
        public string newProfilePath = Application.persistentDataPath;

        /// <summary>
        ///     Transform representing player's play area root.
        ///     All measurements are made in it's coordinate system.
        ///     Y axis must point up, Z - forward.
        /// </summary>
        /// <remarks>
        ///     The most important thing to note is that this transform's XZ plane must
        ///     be aligned with the real play area's floor.
        /// </remarks>
        [field: Header("References"), DocumentedByXml]
        public Transform calibrationRoot;

        /// <summary>
        ///     Transform representing the visor of the player.
        /// </summary>
        /// <remarks>
        ///     This is usually a headset camera transforms.
        /// </remarks>
        [field: Header("References"), DocumentedByXml]
        public Transform playerVisor;

        /// <summary>
        ///     Transform representing left wrist of the player.
        /// </summary>
        /// <remarks>
        ///     For precise calibration, this should be the actual wrist transform, not the raw controller one.
        /// </remarks>
        [field: DocumentedByXml]
        public Transform playerLeftWrist;

        /// <summary>
        ///     Transform representing right wrist of the player.
        /// </summary>
        /// <remarks>
        ///     For precise calibration, this should be the actual wrist transform, not the raw controller one.
        /// </remarks>
        [field: DocumentedByXml]
        public Transform playerRightWrist;

        #endregion

        #region Variables (utility)

        /// <summary>
        ///     Position of <see cref="playerVisor" /> in <see cref="calibrationRoot" /> coordinate system.
        /// </summary>
        public Vector3 visorPosition => calibrationRoot.InverseTransformPoint(playerVisor.position);

        /// <summary>
        ///     Position of <see cref="playerLeftWrist" /> in <see cref="calibrationRoot" /> coordinate system.
        /// </summary>
        public Vector3 leftWristPosition => calibrationRoot.InverseTransformPoint(playerLeftWrist.position);

        /// <summary>
        ///     Position of <see cref="playerRightWrist" /> in <see cref="calibrationRoot" /> coordinate system.
        /// </summary>
        public Vector3 rightWristPosition => calibrationRoot.InverseTransformPoint(playerRightWrist.position);

        #endregion

        #region Events

        /// <summary>
        ///     Invoked when calibration starts.
        /// </summary>
        public Action onCalibrationStarted;

        /// <summary>
        ///     Invoked when a calibration step completes.
        /// </summary>
        public Action<int> onCalibrationStepComplete;

        /// <summary>
        ///     Invoked when calibration completes.
        /// </summary>
        public Action onCalibrationComplete;

        #endregion

        #region Methods (calibration)

        public void StartNewCalibration() { StartCoroutine(NewCalibrationProfile_Coroutine()); }

        private IEnumerator NewCalibrationProfile_Coroutine()
        {
            Debug.Log("Creating new calibration profile.", null);

            var waitForFixedUpdate = new WaitForFixedUpdate();

            var newProfile = GenerateEmptyCalibrationProfile();

            // (!) Each calibration step is put in a separate scope ({...}) to avoid repeatedly creating same variables with different names.
            // Step 1 of calibration: standing straight
            {
                Debug.Log("Starting calibration Step 1. Please stand straight.", this);

                // Collecting data:
                //     floorToEyes
                //     shouldersSpread
                var floorToVisorSamples = new List<float>(sampleCount);
                var shouldersSpreadSamples = new List<float>(sampleCount);

                int i = -1;
                bool step1done = false;
                while (!step1done)
                {
                    float visorAltitude = visorPosition.y;
                    float leftWristAltitude = leftWristPosition.y;
                    float rightWristAltitude = rightWristPosition.y;

                    // Check if user is near the correct position to start measurements...
                    // Step 1 check: Left and right wrists are located in the lower 2/3 of the body when the user stands straight.
                    if (leftWristAltitude < 2f / 3f * visorAltitude
                        && rightWristAltitude < 2f / 3f * visorAltitude
                        && UserSymmetricAndLooksForward())
                    {
                        // If the user just entered the correct position, we wait for measurementDelay, then start taking measurements
                        // until the required number of samples is collected.
                        if (i < 0)
                        {
                            i = 0;
                            yield return new WaitForSeconds(measurementDelay);
                            yield return waitForFixedUpdate;
                            continue;
                        }

                        if (i < sampleCount)
                        {
                            floorToVisorSamples[i] = visorAltitude;
                            shouldersSpreadSamples[i] = Vector3.Distance(leftWristPosition, rightWristPosition);
                            i++;
                        }
                        else
                        {
                            step1done = true;
                        }
                    }
                    else
                    {
                        // Reset the counter and clear the arrays if the user went out of the calibration pose.
                        i = -1;
                        floorToVisorSamples.Clear();
                        shouldersSpreadSamples.Clear();
                    }

                    yield return waitForFixedUpdate;
                }

                newProfile.floorToEyes = CalculateAverage(floorToVisorSamples);
                newProfile.shouldersSpread = CalculateAverage(shouldersSpreadSamples);

                InvokeCalibrationEvent(1);
            }

            // Step 2 of calibration: standing in T-pose
            {
                Debug.Log("Starting calibration Step 2. Please stand in a T-pose.", this);

                // Collecting data:
                //     floorToEyes (extra check)
                //     groundToShoulder
                //     wristsSpread
                //     wristToShoulder
                var floorToVisorSamples = new List<float>(sampleCount);
                var groundToShoulderSamples = new List<float>(sampleCount);
                var wristsSpreadSamples = new List<float>(sampleCount);
                var wristToShoulderSamples = new List<float>(sampleCount);

                int i = -1;
                bool step2done = false;
                while (!step2done)
                {
                    float visorAltitude = visorPosition.y;
                    float leftWristAltitude = leftWristPosition.y;
                    float rightWristAltitude = rightWristPosition.y;

                    // Check if user is near the correct position to start measurements...
                    // Step 2 check: Left and right wrists are located in the upper 1/2 of the body, but below the head when the user stands in T-pose.
                    if (leftWristAltitude > 1f / 2f * visorAltitude
                        && rightWristAltitude > 1f / 2f * visorAltitude
                        && leftWristAltitude < visorAltitude
                        && rightWristAltitude < visorAltitude
                        && UserSymmetricAndLooksForward())
                    {
                        // If the user just entered the correct position, we wait for measurementDelay, then start taking measurements until the required number of samples is collected.
                        if (i < 0)
                        {
                            i = 0;
                            yield return new WaitForSeconds(measurementDelay);
                            yield return waitForFixedUpdate;
                            continue;
                        }

                        if (i < sampleCount)
                        {
                            floorToVisorSamples[i] = visorAltitude;
                            groundToShoulderSamples[i] = (leftWristAltitude + rightWristAltitude) / 2;
                            wristsSpreadSamples[i] = Vector3.Distance(leftWristPosition, rightWristPosition);
                            wristToShoulderSamples[i] = (wristsSpreadSamples[i] - newProfile.shouldersSpread) / 2;
                            i++;
                        }
                        else
                        {
                            step2done = true;
                        }
                    }
                    else
                    {
                        // Reset the counter and clear the arrays if the user went out of the calibration pose.
                        i = -1;
                        floorToVisorSamples.Clear();
                        groundToShoulderSamples.Clear();
                        wristsSpreadSamples.Clear();
                        wristToShoulderSamples.Clear();
                    }

                    yield return waitForFixedUpdate;
                }

                newProfile.floorToEyes = (newProfile.floorToEyes + CalculateAverage(floorToVisorSamples)) / 2;
                newProfile.floorToShoulder = CalculateAverage(groundToShoulderSamples);
                newProfile.wristsSpread = CalculateAverage(wristsSpreadSamples);
                newProfile.wristToShoulder = CalculateAverage(wristToShoulderSamples);

                InvokeCalibrationEvent(2);
            }

            // Step 3 of calibration: standing in Y-pose (T-pose with hands bent upwards)
            {
                Debug.Log("Starting calibration Step 3. Please stand in a Y-pose.", this);

                // Collecting data:
                //     floorToEyes (extra check)
                //     lowerArmLength
                //     upperArmLength
                var floorToVisorSamples = new List<float>(sampleCount);
                var lowerArmLengthSamples = new List<float>(sampleCount);
                var upperArmLengthSamples = new List<float>(sampleCount);

                int i = -1;
                bool step3done = false;
                while (!step3done)
                {
                    float visorAltitude = visorPosition.y;
                    float leftWristAltitude = leftWristPosition.y;
                    float rightWristAltitude = rightWristPosition.y;

                    // Check if user is near the correct position to start measurements...
                    // Step 3 check: Left and right wrists are located above the head when the user stands in Y-pose.
                    if (leftWristAltitude > visorAltitude
                        && rightWristAltitude > visorAltitude
                        && UserSymmetricAndLooksForward())
                    {
                        // If the user just entered the correct position, we wait for measurementDelay, then start taking measurements until the required number of samples is collected.
                        if (i < 0)
                        {
                            i = 0;
                            yield return new WaitForSeconds(measurementDelay);
                            yield return waitForFixedUpdate;
                            continue;
                        }

                        if (i < sampleCount)
                        {
                            floorToVisorSamples[i] = visorAltitude;
                            lowerArmLengthSamples[i] = (leftWristAltitude + rightWristAltitude) / 2f - newProfile.floorToShoulder;
                            upperArmLengthSamples[i] = (Vector3.Distance(leftWristPosition, rightWristPosition) - newProfile.shouldersSpread) / 2;
                            i++;
                        }
                        else
                        {
                            step3done = true;
                        }
                    }
                    else
                    {
                        // Reset the counter and clear the arrays if the user went out of the calibration pose.
                        i = -1;
                        floorToVisorSamples.Clear();
                        lowerArmLengthSamples.Clear();
                        upperArmLengthSamples.Clear();
                    }

                    yield return waitForFixedUpdate;
                }

                newProfile.floorToEyes = (newProfile.floorToEyes + CalculateAverage(floorToVisorSamples)) / 2;
                newProfile.upperArmLength = CalculateAverage(lowerArmLengthSamples);
                newProfile.lowerArmLength = CalculateAverage(upperArmLengthSamples);

                InvokeCalibrationEvent(3);
            }

            // Step 4 of calibration: wrists on thighs
            {
                // To be added later...
            }

            // Step 5 of calibration: wrists on knees
            {
                // To be added later...
            }

            // Save the created profile to disk.
            // TODO: move file writing functionality to a separate script, as it can be used in multiple places
            string profileJson = newProfile.Serialize();
            string newProfileName = "calibration_profile_test.bcprofile";
            string filePath = Path.Combine(newProfilePath, newProfileName);
            var streamWriter = File.CreateText(filePath);
            streamWriter.Write(profileJson);
            streamWriter.Close();
        }

        #endregion

        #region Methods (utilities)

        /// <summary>
        ///     Calculates an average value of the float array.
        /// </summary>
        private static float CalculateAverage(List<float> samples)
        {
            int count = samples.Count;
            float average = 0f;
            for (int i = 0; i < count; i++) average += samples[i];
            average /= count;
            return average;
        }

        /// <summary>
        ///     Creates new empty BodyCalibrationProfile with all fields set to 0.
        ///     If we create a Profile without setting all values to 0, it can lead to errors later (not all values are usually
        ///     added, i.e. hipToGround and kneeToGround will only be set when the user chooses full calibration mode).
        ///     This is used internally to create new Profiles.
        /// </summary>
        /// <returns></returns>
        private static BodyCalibrationProfile GenerateEmptyCalibrationProfile() { return ScriptableObject.CreateInstance<BodyCalibrationProfile>(); }

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
            return playerVisor.InverseTransformPoint(playerLeftWrist.position).x < 0f &&
                   playerVisor.InverseTransformPoint(playerRightWrist.position).x > 0f;
        }

        /// <summary>
        ///     Checks if the user's wrists are positioned symmetrically relative to the headset's YZ plane.
        /// </summary>
        private bool WristsSymmetricToTheHead()
        {
            float leftWristAbsX = Mathf.Abs(playerVisor.InverseTransformPoint(playerLeftWrist.position).x);
            float rightWristAbsX = Mathf.Abs(playerVisor.InverseTransformPoint(playerRightWrist.position).x);
            // TODO: convert linearCalibrationTolerance from calibrationRoot to headset coordinate system; current implementation will break if any transforms are scaled
            return leftWristAbsX.ApproxEquals(rightWristAbsX, linearCalibrationTolerance);
        }

        /// <summary>
        ///     Checks if the user's wrists are located almost on the same altitude (distance from the calibrationRoot).
        /// </summary>
        private bool WristsOnTheSameAltitude()
        {
            float leftWristAltitude = leftWristPosition.y;
            float rightWristAltitude = rightWristPosition.y;
            return leftWristAltitude.ApproxEquals(rightWristAltitude, linearCalibrationTolerance);
        }

        /// <summary>
        ///     Checks if the user's wrists are located almost on the same XY plane parallel to headset's XY plane.
        /// </summary>
        private bool WristsOnTheSameXYPlaneRelativeToTheHead()
        {
            float leftWristAbsZ = Mathf.Abs(playerVisor.InverseTransformPoint(playerLeftWrist.position).z);
            float rightWristAbsZ = Mathf.Abs(playerVisor.InverseTransformPoint(playerRightWrist.position).z);
            // TODO: convert linearCalibrationTolerance from calibrationRoot to headset coordinate system; current implementation will break if any transforms are scaled
            return leftWristAbsZ.ApproxEquals(rightWristAbsZ, linearCalibrationTolerance);
        }

        /// <summary>
        ///     Checks if the user's head is placed vertically (line of sight parallel to the ground).
        /// </summary>
        private bool UserHeadVertical()
        {
            var headUpwardDirection = playerVisor.TransformDirection(playerVisor.up).normalized;
            var calibrationUpwardDirection = calibrationRoot.TransformDirection(calibrationRoot.up).normalized;
            float angleDelta = Vector3.Angle(headUpwardDirection, calibrationUpwardDirection);
            return angleDelta.ApproxEquals(1f, angularCalibrationTolerance);
        }

        /// <summary>
        ///     Invokes <see cref="onCalibrationStepComplete" /> event for the given calibration step.
        /// </summary>
        private void InvokeCalibrationEvent(int step)
        {
            Debug.Log($"Calibration step {step} complete.", this);

            onCalibrationStepComplete?.Invoke(step);
        }

        #endregion
    }
}