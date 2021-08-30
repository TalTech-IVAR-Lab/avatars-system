namespace Games.NoSoySauce.Avatars.Calibration.Hands
{
    using System.Collections.Generic;
    using System.IO;
    using Inputs.Utilities;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.XR;

    /// <summary>
    ///     Simplifies creation of new <see cref="WristPoseModifier" /> asset from the given wrist transform in the scene.
    /// </summary>
    public class WristPoseSampler : MonoBehaviour
    {
        /// <summary>
        ///     Creates a new <see cref="WristPoseModifier" /> asset from the given wrist transform in the scene.
        /// </summary>
        /// <param name="wristTransform">Transform in the scene which represents an already calibrated wrist pose.</param>
        /// <param name="hand">The hand the given transform corresponds to.</param>
        /// <param name="outputFolder">Relative path (to the Assets folder) of the folder to save the new <see cref="WristPoseModifier" /> asset to.</param>
        /// <param name="assetName">Name of the new <see cref="WristPoseModifier" /> asset.</param>
        public static void SampleWristPose(Transform wristTransform, Hand hand, WristPoseModifier.MirrorPlane mirrorPlane, string outputFolder, string assetName)
        {
            if (!wristTransform) return;
            if (outputFolder == string.Empty) return;

            string sdkRegex = GetActiveSdkString().ToLower();
            string controllerRegex = GetActiveControllerString(hand).ToLower();

            var newModifier = ScriptableObject.CreateInstance<WristPoseModifier>();
            newModifier.sdkRegex = sdkRegex;
            newModifier.controllerRegex = controllerRegex;
            newModifier.hand = hand;
            newModifier.mirrorPlane = mirrorPlane;
            newModifier.pose = new Pose
            {
                position = wristTransform.localPosition,
                rotation = wristTransform.localRotation
            };

        #if UNITY_EDITOR
            string outputFolderAbsolutePath = Path.Combine(Application.dataPath, outputFolder);
            Debug.Log($"checking {outputFolderAbsolutePath}");
            if (!Directory.Exists(outputFolderAbsolutePath))
            {
                Debug.Log($"DOES NOT! {outputFolderAbsolutePath}");
                Directory.CreateDirectory(outputFolderAbsolutePath);
                AssetDatabase.Refresh();
            }
            string fullOutputPath = Path.Combine(outputFolder, $"{assetName}.asset");
            AssetDatabase.CreateAsset(newModifier, "Assets/" + fullOutputPath);
            AssetDatabase.Refresh();
        #else
            Debug.LogError($"Creating {nameof(WristPoseModifier)} assets is not supported at runtime (yet).");
        #endif
        }

        public static string GetActiveSdkString() { return XRSettings.loadedDeviceName; }

        public static string GetActiveControllerString(Hand hand)
        {
            string controllerName = "None";

            var controllers = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller, controllers);

            foreach (var controller in controllers)
                if (hand == Hand.Left && controller.HasCharacteristic(InputDeviceCharacteristics.Left)
                    || hand == Hand.Right && controller.HasCharacteristic(InputDeviceCharacteristics.Right))
                {
                    controllerName = controller.name;
                    break;
                }

            return controllerName;
        }
    }
}