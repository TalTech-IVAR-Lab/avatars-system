namespace Games.NoSoySauce.Avatars.Calibration.Body
{
    using System;
    using System.Data;
    using System.IO;
    using UnityEngine;

    /// <summary>
    ///     Stores human body dimensions required to calibrate an arbitrary player avatar.
    /// </summary>
    /// <remarks>
    ///     All measurements are in meters, 1:1 scale to the real world.
    ///     More details about calibration process can be found in <see cref="HumanoidAvatarCalibrator" />.
    /// </remarks>
    [CreateAssetMenu(menuName = "NoSoySauce Games/Avatars/Body Calibration Profile")]
    public class BodyCalibrationProfile : ScriptableObject
    {
        #region Constants

        /// <summary>
        ///     Extension used for saved <see cref="BodyCalibrationProfile" /> files.
        /// </summary>
        public const string DefaultProfileFileExtension = ".bcprofile";

        /// <summary>
        ///     Relative path to where a newly created profile file must be saved (relative to application persistent data folder).
        /// </summary>
        private const string DefaultProfilesFolderRelativePath = "Body Calibration Profiles\\"; // <- TODO: implement relative path to save in persistent data folder

        #endregion

        #region Static Variables

        /// <summary>
        ///     Full path to where a newly created profile file must be saved. Available after Awake() call.
        /// </summary>
        [ClearOnReload]
        public static string DefaultProfilesFolderPath { get; private set; }

        #endregion

        #region Static Methods

        /// <summary>
        /// Initializes <see cref="DefaultProfilesFolderPath"/>.  
        /// </summary>
        [ExecuteOnReload]
        private static void SetDefaultProfilesFolderPath()
        {
            DefaultProfilesFolderPath = Path.Combine(Application.persistentDataPath, DefaultProfilesFolderRelativePath);
        }

        /// <summary>
        ///     Encodes given <see cref="BodyCalibrationProfile" /> into a JSON string.
        /// </summary>
        public static string Serialize(BodyCalibrationProfile profile)
        {
            // Nothing to do if nothing to serialize
            if (profile == null)
            {
                Debug.LogError("Cannot serialize calibration profile. Provided profile is null.");
                return null;
            }
            
            string profileJson = JsonUtility.ToJson(profile);
            return profileJson;
        }

        /// <summary>
        ///     Decodes the given JSON string into <see cref="BodyCalibrationProfile" /> object.
        /// </summary>
        public static BodyCalibrationProfile Deserialize(string profileJson)
        {
            var decodedProfile = CreateInstance<BodyCalibrationProfile>();
            JsonUtility.FromJsonOverwrite(profileJson, decodedProfile);

            return decodedProfile;
        }

        /// <summary>
        ///     Saves given <see cref="BodyCalibrationProfile" /> with the specified name in the specified folder.
        /// </summary>
        /// <param name="profile">Profile to be saved.</param>
        /// <param name="fileName">Name for the calibration profile file (without extension).</param>
        /// <param name="folderPath">
        ///     [Optional] Folder to save the profile in (profile will be saved to DefaultProfilesFolderPath
        ///     if not provided).
        /// </param>
        public static void Save(BodyCalibrationProfile profile, string fileName = null, string folderPath = null)
        {
            // Nothing to do if nothing to save
            if (profile == null)
            {
                Debug.LogError("Cannot save calibration profile. Provided profile is null.");
                return;
            }
            
            // Save in default folder if no path provided
            if (string.IsNullOrEmpty(folderPath)) folderPath = DefaultProfilesFolderPath;

            // Make sure output folder exists
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
            
            // Use generic name if no name is provided
            if (string.IsNullOrEmpty(fileName))
            {
                // Make sure new file has unique name (keep adding number to the end of file name until no same file name exists)
                fileName = "body_calibration_profile";
                int n = 1;
                string uniqueFileName = fileName;
                for (int i = 0; i < 5; i++)
                {
                    string fullPath = Path.Combine(folderPath, $"{uniqueFileName}{DefaultProfileFileExtension}");
                    if (!File.Exists(fullPath)) break;
                    
                    uniqueFileName = $"{fileName}_{n}";
                    n++;
                }

                fileName = uniqueFileName;
            }

            // TODO: add custom generic serializer implementation for all our assets?
            string profileJson = profile.Serialize();
            string filePath = Path.Combine(folderPath, $"{fileName}{DefaultProfileFileExtension}");
            using (StreamWriter streamWriter = File.CreateText(filePath)) { streamWriter.Write(profileJson); }
            
            Debug.Log($"Calibration profile '{fileName}' was successfully saved to '{filePath}'.");
        }

        /// <summary>
        ///     Loads <see cref="BodyCalibrationProfile" /> from the specified absolute path.
        /// </summary>
        /// <param name="filePath">Full path to the calibration profile file.</param>
        public static BodyCalibrationProfile Load(string filePath)
        {
            // TODO: add custom generic serializer implementation for all our assets?
            string allText = File.ReadAllText(filePath);
            var profile = JsonUtility.FromJson<BodyCalibrationProfile>(allText);
            
            if (profile == null)
            {
                Debug.LogError($"Cannot load body calibration profile. Provided file is not a valid calibration profile file ({filePath}).");
                return null;
            }

            return profile;
        }

        #endregion
        
        #region Public Variables

        /// <summary>
        ///     Vertical distance from calibrationRoot to eyes.
        /// </summary>
        /// <remarks>
        ///     Main calibration parameter, this affects uniform scaling of the avatar's model.
        /// </remarks>
        public float floorToEyes;

        /// <summary>
        ///     Vertical distance from calibrationRoot to shoulders.
        /// </summary>
        public float floorToShoulder;

        /// <summary>
        ///     Horizontal distance between shoulders.
        /// </summary>
        public float shouldersSpread;

        /// <summary>
        ///     Horizontal distance between user's wrists when standing in T-pose.
        /// </summary>
        public float wristsSpread;

        /// <summary>
        ///     Horizontal distance between user's wrist and shoulder when standing in T-pose.
        /// </summary>
        public float wristToShoulder;

        /// <summary>
        ///     Distance from elbow to shoulder.
        /// </summary>
        public float upperArmLength;

        /// <summary>
        ///     Distance from wrist to elbow.
        /// </summary>
        public float lowerArmLength;

        /// <summary>
        ///     [Optional] Vertical distance from calibrationRoot to hips when standing straight.
        /// </summary>
        public float floorToHips;

        /// <summary>
        ///     [Optional] Vertical distance from calibrationRoot to knees when standing straight.
        /// </summary>
        public float floorToKnee;

        #endregion
        
        #region Methods

        /// <summary>
        ///     Encodes this <see cref="BodyCalibrationProfile" /> into a JSON string.
        /// </summary>
        public string Serialize() => Serialize(this);

        /// <summary>
        ///     Saves given <see cref="BodyCalibrationProfile" /> with the specified name in the specified folder.
        /// </summary>
        /// <param name="fileName">Name for the calibration profile file (without extension).</param>
        /// <param name="folderPath">
        ///     [Optional] Folder to save the profile in (profile will be saved to DefaultProfilesFolderPath
        ///     if not provided).
        /// </param>
        public void Save(string fileName = null, string folderPath = null) => Save(this, fileName, folderPath);

        #endregion
    }
}