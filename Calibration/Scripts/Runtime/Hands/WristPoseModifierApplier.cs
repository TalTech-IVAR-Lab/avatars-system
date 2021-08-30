namespace Games.NoSoySauce.Avatars.Calibration.Hands
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using Inputs.Utilities;
    using Malimbe.PropertySerializationAttribute;
    using Malimbe.XmlDocumentationAttribute;
    using ZinniaExtensions.Action;
    using UnityEngine;
    using UnityEngine.XR;
    using Zinnia.Data.Attribute;

    /// <summary>
    ///     Adjusts the local pose of the transform based on the connected VR controller.
    ///     This allows to adjust virtual wrists' positions to match the real player's hands.
    /// </summary>
    public class WristPoseModifierApplier : MonoBehaviour
    {
        #region Variables

        /// <summary>
        ///     The hand this modifier is responsible for.
        ///     <see cref="WristPoseModifierApplier" /> will automatically mirror the wrist pose values if the modifier has
        ///     different hand recorded in it.
        /// </summary>
        [field: Header("Settings"), DocumentedByXml]
        public Hand hand = Hand.Right;

        /// <summary>
        ///     A collection of <see cref="WristPoseModifier" />s to extract the wrist pose data for each specified SDK.
        /// </summary>
        [field: DocumentedByXml]
        public List<WristPoseModifier> modifiers = new List<WristPoseModifier>();

        #endregion

        #region Debug Variables

        /// <summary>
        ///     <see cref="WristPoseModifier" /> which is currently applied.
        /// </summary>
        /// <remarks>
        ///     This is for debugging.
        /// </remarks>
        [Serialized]
        [field: Restricted(RestrictedAttribute.Restrictions.ReadOnlyAlways), DocumentedByXml]
        private WristPoseModifier ActiveModifier { get; set; }

        /// <summary>
        ///     Name of the controller which modifier is currently applied. No modifier is applied if this field is empty.
        /// </summary>
        /// <remarks>
        ///     This is for debugging.
        /// </remarks>
        [Serialized]
        [field: Restricted(RestrictedAttribute.Restrictions.ReadOnlyAlways), DocumentedByXml]
        private string ActiveDevice { get; set; }

        #endregion

        #region Events

        /// <summary>
        ///     Event called when the pose gets updated.
        /// </summary>
        [field: Header("Events"), DocumentedByXml]
        public PoseAction.UnityEvent onPoseModified = new PoseAction.UnityEvent();

        #endregion

        #region Unity Callbacks

        protected void OnEnable()
        {
            SubscribeToInputDevicesEvents();

            // Initialize explicitly.
            var devices = new List<InputDevice>();
            InputDevices.GetDevices(devices);
            foreach (var device in devices) ProcessDevice(device);
        }

        protected void OnDisable()
        {
            UnsubscribeFromInputDevicesEvents();

            EmitPoseModifiedEvent(Pose.identity);
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Subscribes to <see cref="InputDevices" /> connection events.
        /// </summary>
        private void SubscribeToInputDevicesEvents()
        {
            InputDevices.deviceConnected += ProcessDevice;
            InputDevices.deviceDisconnected += ProcessDevice;
            InputDevices.deviceConfigChanged += ProcessDevice;
        }

        /// <summary>
        ///     Unsubscribes from <see cref="InputDevices" /> connection events.
        /// </summary>
        private void UnsubscribeFromInputDevicesEvents()
        {
            InputDevices.deviceConnected -= ProcessDevice;
            InputDevices.deviceDisconnected -= ProcessDevice;
            InputDevices.deviceConfigChanged -= ProcessDevice;
        }

        /// <summary>
        ///     Checks if any of transform modifiers should be applied for the given <see cref="InputDevice" />, and applies it.
        /// </summary>
        /// <param name="device"><see cref="InputDevice" /> to check.</param>
        private void ProcessDevice(InputDevice device)
        {
            // If device is not valid or is not a controller, do nothing
            if (!device.isValid) return;
            if (!device.HasCharacteristic(InputDeviceCharacteristics.Controller)) return;
            // If device does not match the selected hand, do nothing.
            if (hand == Hand.Left && !device.HasCharacteristic(InputDeviceCharacteristics.Left)) return;
            if (hand == Hand.Right && !device.HasCharacteristic(InputDeviceCharacteristics.Right)) return;
            
            // Set device as active if it passed previous checks
            ActiveDevice = device.name;

            // Check if the loaded XR SDK and controller type match any of the pose overrides, and apply that override
            foreach (var modifier in modifiers)
            {
                bool sdkMatch = Regex.IsMatch(XRSettings.loadedDeviceName.ToLower(), modifier.sdkRegex);
                bool controllerMatch = Regex.IsMatch(device.name.ToLower(), modifier.controllerRegex);

                if (sdkMatch && controllerMatch)
                {
                    // If selected hand does not match the hand of the modifier, just get a mirrored pose
                    bool handMatch = hand == modifier.hand;
                    var pose = handMatch ? modifier.pose : modifier.GetMirroredPose();

                    ActiveModifier = modifier;

                    EmitPoseModifiedEvent(pose);

                    return;
                }
            }
        }

        /// <summary>
        ///     Emits <see cref="onPoseModified" /> event.
        /// </summary>
        /// <param name="pose">Modified <see cref="Pose" /> value to emit.</param>
        private void EmitPoseModifiedEvent(Pose pose) { onPoseModified?.Invoke(pose); }

        #endregion
    }
}