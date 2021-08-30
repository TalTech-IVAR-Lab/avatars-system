namespace Games.NoSoySauce.Avatars.AvatarSystem.Humanoid
{
    using System;
    using Malimbe.XmlDocumentationAttribute;
    // using Photon.Pun;
    using RootMotion.FinalIK;
    using TiliaExtensions.Locomotion.BodyRepresentation;
    using Tilia.Interactions.Interactables.Interactors;
    using UnityEngine;
    using Zinnia.Tracking.Collision;

    /// <summary>
    ///     Type of <see cref="RigAvatarLinker" /> used for linking humanoid rigs.
    /// </summary>
    [Obsolete("Avatars are now single prefabs. Rig and avatar are not split anymore, so there is no need in Linker.")]
    public class HumanoidRigAvatarLinker : RigAvatarLinker
    {
        #region Variables

        /// <summary>
        ///     <see cref="CollisionIgnorer" /> script of <see cref="BodyRepresentationFacade" /> used in the rig.
        /// </summary>
        [field: DocumentedByXml]
        public CollisionIgnorer rigBodyRepresentationCollisionIgnorer;

        /// <summary>
        ///     <see cref="CollisionIgnorer" /> associated with the left hand <see cref="InteractorFacade" /> of the rig.
        /// </summary>
        [field: DocumentedByXml]
        public CollisionIgnorer rigLeftInteractorCollisionIgnorer;

        /// <summary>
        ///     <see cref="CollisionIgnorer" /> associated with the right hand <see cref="InteractorFacade" /> of the rig.
        /// </summary>
        [field: DocumentedByXml]
        public CollisionIgnorer rigRightInteractorCollisionIgnorer;

        /// <summary>
        ///     <see cref="Transform" /> to serve as head target for the avatar's <see cref="VRIK" /> script.
        /// </summary>
        [field: DocumentedByXml]
        public Transform rigHeadIKTarget;

        /// <summary>
        ///     <see cref="Transform" /> to serve as left hand target for the avatar's <see cref="VRIK" /> script.
        /// </summary>
        [field: DocumentedByXml]
        public Transform rigLeftWristIKTarget;

        /// <summary>
        ///     <see cref="Transform" /> to serve as right hand target for the avatar's <see cref="VRIK" /> script.
        /// </summary>
        [field: DocumentedByXml]
        public Transform rigRightWristIKTarget;

        #endregion

        #region Public Methods

        /// <summary>
        ///     Links the given avatar to the associated player rig.
        /// </summary>
        /// <param name="avatar">Root <see cref="GameObject" /> of the avatar.</param>
        public override void Link(GameObject avatar)
        {
            // Call the base method.
            base.Link(avatar);

            // Add <see cref="avatar"/> to the list of targets of <see cref="rigBodyRepresentationCollisionIgnorer"/>.
            rigBodyRepresentationCollisionIgnorer?.Targets.AddUnique(avatar);

            // Add corresponding avatar elbows to the lists of targets of <see cref="rigLeftInteractorCollisionIgnorer"/> and <see cref="rigRightInteractorCollisionIgnorer"/>.
            var avatarAnimator = avatar.GetComponentInChildren<Animator>();
            if (avatarAnimator != null)
            {
                var avatarLeftElbowBone = avatarAnimator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
                if (avatarLeftElbowBone != null) rigLeftInteractorCollisionIgnorer?.Targets.AddUnique(avatarLeftElbowBone.gameObject);

                var avatarRightElbowBone = avatarAnimator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
                if (avatarRightElbowBone != null) rigLeftInteractorCollisionIgnorer?.Targets.AddUnique(avatarRightElbowBone.gameObject);
            }

            // Set anchors as <see cref="VRIK"/> targets.
            var avatarIK = avatar.GetComponentInChildren<VRIK>();
            if (avatarIK != null)
            {
                avatarIK.solver.spine.headTarget = rigHeadIKTarget;
                avatarIK.solver.leftArm.target = rigLeftWristIKTarget;
                avatarIK.solver.rightArm.target = rigRightWristIKTarget;
            }

            // TODO*: Connect avatar wrist finger controllers to interactors (physics & gestures)
            // * When implemented
        }

        #endregion

        // /// <summary>
        // ///     RPC for <see cref="Link(GameObject)" />.
        // /// </summary>
        // /// <param name="avatarViewId">ID of the avatar's <see cref="PhotonView" />.</param>
        // [PunRPC]
        // protected override void Link_RPC(int avatarViewId)
        // {
        //     Debug.Log("Avatar link request received!");
        //     var photonView = PhotonView.Find(avatarViewId);
        //     Link(photonView.gameObject);
        // }
    }
}