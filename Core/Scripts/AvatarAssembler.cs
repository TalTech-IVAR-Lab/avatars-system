namespace Games.NoSoySauce.Avatars
{
    using UnityEngine;
    // using Photon.Pun;
    // using Games.NoSoySauce.Networking.Multiplayer;

    /// <summary>
    /// Script responsible for assembling an avatar. This can include calibration, customization and other things.
    /// </summary>
    /// <remarks>
    /// Must be attached to any object inside avatar prefab. <see cref="AssembleAvatar_RPC"/> method is called by <see cref="AvatarWarden"/>.
    /// </remarks>
    // NOTE: Work in progress. This is updated during development process, as we add new avatar features and customization options.
    public class AvatarAssembler : MonoBehaviour
    {
        #region Variables
        /// Count of how many avatar instances spawned, including remote ones.
        /// Used to determine when the spawn over network is finished.
        private int readyInstancesCount = 0;
        #endregion

        // The main guy.
        #region AssembleAvatar() Method
        /// <summary>
        /// Single method to start the assembly process. To be called by <see cref="AvatarWarden"/>.
        /// </summary>
        /// <param name="avatarData"><see cref="AvatarData"/> used to assemble the avatar.</param>
        public virtual void AssembleAvatar(AvatarData avatarData)
        {
            // Networked call.
            // TODO: update when MultiFrame is ready
            // if (PhotonNetwork.InRoom && MultiplayerManager.IsLocal(gameObject))
            // {
            //     var assemblerView = MultiplayerManager.GetPhotonView(gameObject);
            //     // TODO: Make it possible to serialize ScriptableObject and send it over the network
            //     Debug.LogWarning("Here we have to send avatar data, don't forget! Need to serialize it.");
            //     //assemblerView?.RPC(nameof(AssembleAvatar_RPC), RpcTarget.OthersBuffered, null); // avatarData); - instead of null
            // }

            // More functionality can be added in subclasses...
        }

        /// <summary>
        /// RPC for <see cref="AssembleAvatar_RPC(AvatarData)"/>.
        /// </summary>
        /// <param name="avatarData"><see cref="AvatarData"/> used to assemble the avatar.</param>
        // [PunRPC]
        private void AssembleAvatar_RPC(AvatarData avatarData)
        {
            // TODO: Make it possible to serialize ScriptableObject and send it over the network
            AssembleAvatar(avatarData);
        }
        #endregion
    }
}
