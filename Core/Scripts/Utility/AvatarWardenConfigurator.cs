namespace Games.NoSoySauce.Avatars.AvatarSystem.Utility
{
	using UnityEngine;
    using Malimbe.MemberChangeMethod;
    using Malimbe.XmlDocumentationAttribute;
    using Malimbe.PropertySerializationAttribute;
    using Games.NoSoySauce.Avatars;

    /// <summary>
    /// Component which allows to set <see cref="AvatarWarden.AvatarData"/> from the Inspector.
    /// </summary>
    public class AvatarWardenConfigurator : MonoBehaviour
    {
        /// <summary>
        /// <see cref="AvatarData"/> file containing all avatar information.
        /// </summary>
        [Serialized]
        [field: Header("References"), DocumentedByXml]
        public AvatarData AvatarData { get; set; }

        private void Awake()
        {
            OnAfterAvatarDataChange();
        }

        /// <summary>
        /// Called after <see cref="AvatarData"/> gets changed.
        /// </summary>
        [CalledAfterChangeOf(nameof(AvatarData))]
        private void OnAfterAvatarDataChange()
        {
            AvatarWarden.AvatarData = AvatarData;
        }
    }
}