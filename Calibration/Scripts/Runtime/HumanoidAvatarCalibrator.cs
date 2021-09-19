namespace Games.NoSoySauce.Avatars.Calibration
{
    using UnityEngine;

    /// <summary>
    ///     Contains methods to apply <see cref="BodyCalibrationProfile" /> to a humanoid avatar.
    /// </summary>
    public class HumanoidAvatarCalibrator : MonoBehaviour
    {
        #region Methods (calibration)

        /// <summary>
        ///     Calibrates given avatar according to the given <see cref="BodyCalibrationProfile" />.
        ///     Avatar must be already in a Scene and in a symmetric T-pose.
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="avatarRoot"></param>
        // TODO: currently this method assumes that the avatar is in the scene and its root has identity scale (1;1;1). All calibration is done in world space. This means we can only calibrate avatars in real scale (no gnomes or giants). Make this work for scaled avatars in the future.
        public static void ApplyCalibrationProfileToAvatar(BodyCalibrationProfile profile, Transform avatarRoot)
        {
            // We use humanoid bone mapping of the animator attached to avatar to easily access avatar's bones.
            var avatarAnimator = avatarRoot.GetComponent<Animator>();
            if (!avatarAnimator)
            {
                Debug.LogError($"Cannot calibrate avatar '{avatarRoot.name}': missing {nameof(Animator)} component. {nameof(Animator)} with a humanoid avatar set up is required to access avatar bones for calibration.");
                return;
            }

            // Get the bone transforms.
            var avatarLeftWrist = avatarAnimator.GetBoneTransform(HumanBodyBones.LeftHand);
            var avatarLeftLowerArm = avatarAnimator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            var avatarLeftUpperArm = avatarAnimator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            var avatarRightWrist = avatarAnimator.GetBoneTransform(HumanBodyBones.RightHand);
            var avatarRightLowerArm = avatarAnimator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            var avatarRightUpperArm = avatarAnimator.GetBoneTransform(HumanBodyBones.RightUpperArm);

            // Get visor transform.
            var avatarVisorTag = avatarAnimator.GetComponentInChildren<VisorTag>(); //avatarBones.head.Find("visor");
            if (avatarVisorTag == null)
            {
                Debug.LogError($"Cannot calibrate avatar '{avatarRoot.name}': avatar needs to have a transform with a '{nameof(VisorTag)}' component attached. It represents avatar's sight origin. Visor's Z axis should point in the direction of sight, Y axis - upwards. Please add it. Calibration will not work without it.");
                return;
            }

            var avatarVisor = avatarVisorTag.transform;

            // 1. Scale avatar's root so that avatar visor height matches user visor height.
            {
                float avatarVisorHeight = avatarVisor.transform.position.y - avatarRoot.transform.position.y;
                float scaleFactor = profile.floorToEyes / avatarVisorHeight;
                avatarRoot.localScale *= scaleFactor;
            }

            // 2. Raise avatar's shoulder bones so that avatar's shoulder height matches user shoulder height.
            {
                float avatarShoulderHeight = (avatarLeftUpperArm.position.y + avatarRightUpperArm.position.y) / 2f - avatarRoot.position.y;
                float delta = profile.floorToShoulder - avatarShoulderHeight; // Height delta.
                avatarAnimator.GetBoneTransform(HumanBodyBones.Head).Translate(0f, delta, 0f, Space.World); // Raise head by delta, so that visor heights will match.
            }

            // 3. Extend avatar's shoulders, so that avatar's shoulder spread matches user shoulder spread.
            {
                var avatarShoulderVectorRtL = avatarLeftUpperArm.position - avatarRightUpperArm.position; // Vector from avatar's right to left shoulder.
                float delta = (profile.shouldersSpread - avatarShoulderVectorRtL.magnitude) / 2f; // One shoulder spread delta.
                var deltaVectorRtL = avatarShoulderVectorRtL.normalized * delta;
                var deltaVectorLtR = avatarShoulderVectorRtL.normalized * -delta; // Same, but in opposite direction. Shoulders should be symmetric.
                avatarLeftUpperArm.Translate(deltaVectorRtL, Space.World);
                avatarRightUpperArm.Translate(deltaVectorLtR, Space.World);
            }

            // 4. Extend avatar's upper arms, so that avatar's forearm length matches user forearm length.
            {
                // (!) Here, calculations are done separately for each hand. It can be simplified, if the avatar is symmetric, but the Knight avatar used in our demo has slightly assymetric arms (though in a T-pose).
                // Left forearm.
                var avatarUpperArmVectorRtL = avatarLeftLowerArm.position - avatarLeftUpperArm.position;
                float deltaLeft = profile.lowerArmLength - avatarUpperArmVectorRtL.magnitude;
                var deltaVectorRtL = avatarUpperArmVectorRtL.normalized * deltaLeft;
                avatarLeftLowerArm.Translate(deltaVectorRtL, Space.World);
                // Right forearm.
                var avatarUpperArmVectorLtR = avatarRightLowerArm.position - avatarRightUpperArm.position;
                float deltaRight = profile.lowerArmLength - avatarUpperArmVectorLtR.magnitude;
                var deltaVectorLtR = avatarUpperArmVectorLtR.normalized * deltaRight;
                avatarRightLowerArm.Translate(deltaVectorLtR, Space.World);
            }

            // 5. Extend avatar's lower arms, so that avatar's arm length matches user arm length.
            {
                // (!) Here, calculations are done separately for each hand. It can be simplified, if the avatar is symmetric, but the Knight avatar used in our demo has slightly assymetric arms (though in a T-pose).
                // Left arm.
                var avatarArmVectorRtL = avatarLeftWrist.position - avatarLeftLowerArm.position;
                float deltaLeft = profile.upperArmLength - avatarArmVectorRtL.magnitude;
                var deltaVectorRtL = avatarArmVectorRtL.normalized * deltaLeft;
                avatarLeftLowerArm.Translate(deltaVectorRtL, Space.World);
                // Right arm.
                var avatarArmVectorLtR = avatarRightWrist.position - avatarRightLowerArm.position;
                float deltaRight = profile.upperArmLength - avatarArmVectorLtR.magnitude;
                var deltaVectorLtR = avatarArmVectorLtR.normalized * deltaRight;
                avatarRightLowerArm.Translate(deltaVectorLtR, Space.World);
            }

            // 6. [Optional] Adjust avatar thigh bone length to match user's hipToGround and kneeToGround values.
            {
                // Code will be added later.
            }

            Debug.Log("Applied BodyCalibrationProfile to avatar (" + avatarRoot.name + ").", null);
        }

        #endregion

        #region Methods (utilities)

        private static void ExtendBone(Transform start, Transform end, float delta) { }

        #endregion
    }
}