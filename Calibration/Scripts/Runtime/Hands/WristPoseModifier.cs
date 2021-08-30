namespace Games.NoSoySauce.Avatars.Calibration.Hands
{
    using Malimbe.XmlDocumentationAttribute;
    using UnityEngine;

    /// <summary>
    ///     Stores data about the pose offset which has to be applied to virtual wrist transform to make it match the player's
    ///     real hand, based on currently connected controller for that specific hand.
    /// </summary>
    [CreateAssetMenu(menuName = "NoSoySauce Games/Avatars/Wrist Pose Modifier")]
    public class WristPoseModifier : ScriptableObject
    {
        public enum MirrorPlane
        {
            XY,
            XZ,
            YZ
        }

        /// <summary>
        ///     Regular expression for lowercase loaded XR SDK name, which has to be matched for this pose to become active.
        /// </summary>
        /// <remarks>
        ///     Same devices can have different pivot points depending on the active XR SDK. For example, Oculus Touch
        ///     controllers will have different pivot points in OpenVR and Oculus SDKs.
        /// </remarks>
        [field: Header("Modifier Settings"), DocumentedByXml]
        public string sdkRegex = string.Empty;

        /// <summary>
        ///     Regular expression for lowercase input device's name, which has to be matched for this pose to become active.
        /// </summary>
        [field: DocumentedByXml]
        public string controllerRegex = string.Empty;

        /// <summary>
        ///     Hand this that pose relates to.
        /// </summary>
        public Hand hand = Hand.Right;

        /// <summary>
        ///     Plane of symmetry for this pose modifier.
        ///     This is used in case the application needs to get the pose for the opposite hand using this modifier.
        /// </summary>
        public MirrorPlane mirrorPlane = MirrorPlane.YZ;

        /// <summary>
        ///     Pose to apply to virtual wrist.
        /// </summary>
        public Pose pose = Pose.identity;

        /// <summary>
        ///     For some informational notes.
        /// </summary>
        [field: Header("Other"), TextArea, DocumentedByXml]
        public string comment = string.Empty;

        /// <summary>
        ///     Returns this pose, mirrored around this modifier's <see cref="mirrorPlane" />.
        /// </summary>
        public Pose GetMirroredPose()
        {
            var mirroredPose = pose;

            // Mirror position and rotation of the original pose.
            switch (mirrorPlane)
            {
                case MirrorPlane.XY:
                {
                    mirroredPose.position.z *= -1f;
                    mirroredPose.rotation.x *= -1f;
                    mirroredPose.rotation.y *= -1f;
                    break;
                }
                case MirrorPlane.XZ:
                {
                    mirroredPose.position.y *= -1f;
                    mirroredPose.rotation.x *= -1f;
                    mirroredPose.rotation.z *= -1f;
                    break;
                }
                case MirrorPlane.YZ:
                {
                    mirroredPose.position.x *= -1f;
                    mirroredPose.rotation.y *= -1f;
                    mirroredPose.rotation.z *= -1f;
                    break;
                }
            }

            return mirroredPose;
        }
    }
}