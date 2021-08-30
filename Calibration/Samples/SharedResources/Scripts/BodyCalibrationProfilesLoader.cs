namespace Games.NoSoySauce.Avatars.Calibration.Body
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Loads all available <see cref="BodyCalibrationProfile"/> files from the default folder and makes them available to other scripts.
    /// </summary>
    public class BodyCalibrationProfilesLoader : MonoBehaviour
    {
        public List<BodyCalibrationProfile> loadedProfiles = new List<BodyCalibrationProfile>();

        private void OnEnable()
        {
            Refresh();
        }

        private void OnDisable()
        {
            loadedProfiles.Clear();
        }

        /// <summary>
        /// Reloads all <see cref="BodyCalibrationProfile"/> files.
        /// </summary>
        /// <remarks>
        /// Use this to update <see cref="loadedProfiles"/> list if the new profile files get added/created.
        /// </remarks>
        public void Refresh()
        {
            string profilesFolderPath = BodyCalibrationProfile.DefaultProfilesFolderPath;
        }
    }
}