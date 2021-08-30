namespace Games.NoSoySauce.Avatars.AvatarSystem.Utility
{
    using System;
    using UnityEngine;
    using Malimbe.XmlDocumentationAttribute;
    using ZinniaExtensions.Action;

    /// <summary>
    /// Responsible for placing player rig to the avatar spawn pose in <see cref="AvatarWarden.OnBeforeAvatarSpawned"/> callback.
    /// </summary>
    /// <remarks>
    /// This is done to prevent player rig from overriding spawned avatar position.
    /// </remarks>
    public class AlignPlayerRigWithAvatarBeforeAvatarSpawn : MonoBehaviour
    {
        /// <summary>
        /// <see cref="PoseAction"/> which sets the pose of player rig.
        /// </summary>
        [field: DocumentedByXml]
        public PoseAction setPlayerRigPoseAction;
        
        private void OnEnable()
        {
            AvatarWarden.OnBeforeAvatarSpawned += AlignPlayerRigWithAvatar;
        }

        private void OnDisable()
        {
            AvatarWarden.OnBeforeAvatarSpawned -= AlignPlayerRigWithAvatar;
        }

        private void AlignPlayerRigWithAvatar(Pose avatarPose)
        {
            // TODO: Call SetPlayerRigPose static action
            throw new NotImplementedException();
            if (setPlayerRigPoseAction == null)
            {
                Debug.LogError($"Cannot set player rig pose as {nameof(setPlayerRigPoseAction)} reference is null. Please set it in the Inspector.", this);
                return;
            }
            
            setPlayerRigPoseAction.Receive(avatarPose);
        }

        /// <summary>
        /// Matches the pose of player rig with the avatar spawn pose.
        /// </summary>
        /// <param name="spawnPose">Pose where the avatar will be spawned.</param>
        /// <remarks>
        /// Here we need to match the pose of the headset projected on the play area XZ plane with the avatar spawn pose.
        /// </remarks>
        [Obsolete("Replaced with AlignPlayerRigWithAvatar()")]
        private void MatchPlayerRigWithSpawnPose(Pose spawnPose)
        {
            // var playAreaAlias = FindObjectOfType<PlayAreaAliasTag>()?.transform;
            // var headsetAlias = FindObjectOfType<HeadsetAliasTag>()?.transform;
            // var leftControllerAlias = FindObjectOfType<LeftControllerAliasTag>()?.transform;
            // var rightControllerAlias = FindObjectOfType<RightControllerAliasTag>()?.transform;
            //
            // if (playAreaAlias == null || headsetAlias == null) return;
            //
            // var projectedHeadsetPose = new Pose();
            // projectedHeadsetPose.position = Vector3.ProjectOnPlane(headsetAlias.position, playAreaAlias.up);
            // var projectedHeadsetForwardDirection = Vector3.ProjectOnPlane(headsetAlias.forward, playAreaAlias.up);
            // projectedHeadsetPose.rotation = Quaternion.LookRotation(projectedHeadsetForwardDirection, playAreaAlias.up);
            //
            // // Place play area to the new pose.
            // var playAreaOffset = GetPoseOffset(playAreaAlias, projectedHeadsetPose);
            // ApplyPoseOffset(playAreaAlias, spawnPose, playAreaOffset);
            //
            // // Process follower scripts on play area, and then other aliases.
            // playAreaAlias?.GetComponent<ObjectFollower>()?.Process();
            // headsetAlias?.GetComponent<ObjectFollower>()?.Process();
            // leftControllerAlias?.GetComponent<ObjectFollower>()?.Process();
            // rightControllerAlias?.GetComponent<ObjectFollower>()?.Process();
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