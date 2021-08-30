namespace Games.NoSoySauce.Avatars.AvatarSystem
{
    using System;
    using Malimbe.XmlDocumentationAttribute;
    // using Networking.Multiplayer;
    // using Photon.Pun;
    using UnityEngine;

    /// <summary>
    ///     Script responsible for linking an avatar to the rig and setting up all required references.
    /// </summary>
    [Obsolete("Avatars are now single prefabs. Rig and avatar are not split anymore, so there is no need in Linker.")]
    public class RigAvatarLinker : MonoBehaviour
    {
        #region Variables

        /// <summary>
        ///     <see cref="GameObject" /> which the avatar should become parented to when linked.
        /// </summary>
        [field: Header("References"), DocumentedByXml]
        public GameObject avatarContainer;

        #endregion

        #region Methods

        /// <summary>
        ///     Links the given avatar to the associated player rig.
        /// </summary>
        /// <param name="avatar">Root <see cref="GameObject" /> of the avatar.</param>
        public virtual void Link(GameObject avatar)
        {
            if (avatar == null) return;
            if (avatarContainer == null)
            {
                Debug.LogError($"Cannot link the avatar because {nameof(avatarContainer)} reference is not set.", this);
                return;
            }

            // Networked call.
            // TODO: update when MultiFrame is ready
            // if (PhotonNetwork.InRoom && MultiplayerManager.IsLocal(gameObject))
            // {
            //     var avatarView = MultiplayerManager.GetPhotonView(avatar);
            //     if (avatarView == null)
            //     {
            //         Debug.LogError(string.Format("Avatar '{0}' does not have a {1}.", nameof(avatar), nameof(PhotonView), this));
            //         return;
            //     }
            //
            //     var linkerView = MultiplayerManager.GetPhotonView(gameObject);
            //     linkerView?.RPC(nameof(Link_RPC), RpcTarget.OthersBuffered, avatarView.ViewID);
            // }

            // Parent to <see cref="avatarContainer"/>.
            var avatarRoot = avatar.transform;
            avatarRoot.SetParent(avatarContainer.transform);
            avatarRoot.localPosition = Vector3.zero;
            avatarRoot.localRotation = Quaternion.identity;

            // More functionality can be added in subclasses...
        }

        // /// <summary>
        // ///     RPC for <see cref="Link(GameObject)" />.
        // /// </summary>
        // /// <param name="avatarViewId">ID of the avatar's <see cref="PhotonView" />.</param>
        // [PunRPC]
        // protected virtual void Link_RPC(int avatarViewId)
        // {
        //     var photonView = PhotonView.Find(avatarViewId);
        //     Link(photonView.gameObject);
        // }

        #endregion
    }
}