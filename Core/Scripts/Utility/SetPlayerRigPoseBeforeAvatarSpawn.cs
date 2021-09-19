namespace Games.NoSoySauce.Avatars.AvatarSystem.Utility
{
	using UnityEngine;
    using Malimbe.XmlDocumentationAttribute;
    using Malimbe.PropertySerializationAttribute;
    using Zinnia.Tracking.Follow;
    using Games.NoSoySauce.Avatars;
    using Games.NoSoySauce.SoyVRTK.Presence.Tags;

    /// <summary>
    /// Responsible for placing player rig to the avatar spawn pose in <see cref="AvatarWarden.OnBeforeAvatarSpawned"/> callback.
    /// </summary>
    /// <remarks>
    /// This is done to prevent player rig from overriding spawned avatar position.
    /// </remarks>
    public class SetPlayerRigPoseBeforeAvatarSpawn : MonoBehaviour
    {
        private void OnEnable()
        {
            AvatarWarden.OnBeforeAvatarSpawned += MatchPlayerRigWithSpawnPose;
        }

        private void OnDisable()
        {
            AvatarWarden.OnBeforeAvatarSpawned -= MatchPlayerRigWithSpawnPose;
        }

        /// <summary>
        /// Matches the pose of player rig with the avatar spawn pose.
        /// </summary>
        /// <param name="spawnPose">Pose where the avatar will be spawned.</param>
        /// <remarks>
        /// Here we need to match the pose of the headset projected on the play area XZ plane with the avatar spawn pose.
        /// </remarks>
        private void MatchPlayerRigWithSpawnPose(Pose spawnPose)
        {
            var playAreaAlias = FindObjectOfType<PlayAreaAliasTag>()?.transform;
            var headsetAlias = FindObjectOfType<HeadsetAliasTag>()?.transform;
            var leftControllerAlias = FindObjectOfType<LeftControllerAliasTag>()?.transform;
            var rightControllerAlias = FindObjectOfType<RightControllerAliasTag>()?.transform;

            if (playAreaAlias == null || headsetAlias == null) return;

            var projectedHeadsetPose = new Pose();
            projectedHeadsetPose.position = Vector3.ProjectOnPlane(headsetAlias.position, playAreaAlias.up);
            var projectedHeadsetForwardDirection = Vector3.ProjectOnPlane(headsetAlias.forward, playAreaAlias.up);
            projectedHeadsetPose.rotation = Quaternion.LookRotation(projectedHeadsetForwardDirection, playAreaAlias.up);

            // Place play area to the new pose.
            var playAreaOffset = GetPoseOffset(playAreaAlias, projectedHeadsetPose);
            ApplyPoseOffset(playAreaAlias, spawnPose, playAreaOffset);

            // Process follower scripts on play area, and then other aliases.
            playAreaAlias?.GetComponent<ObjectFollower>()?.Process();
            headsetAlias?.GetComponent<ObjectFollower>()?.Process();
            leftControllerAlias?.GetComponent<ObjectFollower>()?.Process();
            rightControllerAlias?.GetComponent<ObjectFollower>()?.Process();
        }

        private Pose GetPoseOffset(Transform target, Pose relativeTo)
        {
            var targetPose = new Pose();
            targetPose.position = target.position;
            targetPose.rotation = target.rotation;
            return GetPoseOffset(targetPose, relativeTo);
        }

        private Pose GetPoseOffset(Pose target, Pose relativeTo)
        {
            var offset = new Pose();
            offset.position = target.position - relativeTo.position;
            offset.rotation = target.rotation * Quaternion.Inverse(relativeTo.rotation);
            return offset;
        }

        private void ApplyPoseOffset(Transform target, Pose relativeTo, Pose offset)
        {
            target.position = relativeTo.position + offset.position;
            target.rotation = offset.rotation * relativeTo.rotation;
        }
    }
}