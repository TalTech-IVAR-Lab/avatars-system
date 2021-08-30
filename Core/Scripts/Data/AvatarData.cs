using Games.NoSoySauce.Avatars.Calibration.Body;

namespace Games.NoSoySauce.Avatars
{
    using System;
    using AvatarSystem;
    using Calibration;
    using Malimbe.XmlDocumentationAttribute;
    using UnityEngine;

    /// <summary>
    ///     Stores data required by <see cref="AvatarWarden" /> to spawn an avatar.
    /// </summary>
    // NOTE: Don't forget to add [CreateAssetMenu] attribute to it when inheriting this class.
    //       If you don't do it, you won't be able to create AvatarData files in the Editor.
    [CreateAssetMenu(fileName = "New Avatar Data", menuName = "NoSoySauce Games/Avatars/Avatar Data", order = 0)]
    public class AvatarData : ScriptableObject
    {
        /// <summary>
        ///     Prefab of the rig to be used for storing and controlling the avatar.
        /// </summary>
        [NonSerialized, Obsolete("Avatars are now single prefabs, eliminating the need for separate rigs.")]
        [field: DocumentedByXml]
        public GameObject rigPrefab;

        /// <summary>
        ///     Prefab of the avatar to connect to the rig.
        /// </summary>
        [field: DocumentedByXml]
        public GameObject avatarPrefab;

        /// <summary>
        ///     <see cref="BodyCalibrationProfile" /> to apply to the avatar when it is spawned.
        /// </summary>
        public BodyCalibrationProfile calibrationProfile;

        /// <summary>
        ///     Empty methods added to make <see cref="Malimbe" /> notice the fields of this script.
        /// </summary>
        private void DummyMethod() { }
    }
}