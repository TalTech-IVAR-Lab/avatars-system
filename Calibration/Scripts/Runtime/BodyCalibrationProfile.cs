namespace Games.NoSoySauce.Avatars.Calibration
{
    using UnityEngine;

    /// <summary>
    ///     Stores human body dimensions required to calibrate an arbitrary player avatar.
    /// </summary>
    /// <remarks>
    ///     All measurements are in meters, 1:1 scale to the real world.
    ///     More details about calibration process can be found in <see cref="AvatarCalibrator" />.
    /// </remarks>
    [CreateAssetMenu(menuName = "NoSoySauce Games/Avatars/Body Calibration Profile")]
    public class BodyCalibrationProfile : ScriptableObject
    {
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
        ///    Horizontal distance between user's wrists when standing in T-pose.
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

        #region Methods

        /// <summary>
        ///     Encodes this <see cref="BodyCalibrationProfile"/> into a JSON string.
        /// </summary>
        public string Serialize() { return Serialize(this); }
        
        /// <summary>
        ///     Encodes given <see cref="BodyCalibrationProfile"/> into a JSON string.
        /// </summary>
        public static string Serialize(BodyCalibrationProfile profile)
        {
            string profileJson = JsonUtility.ToJson(profile);
            return profileJson;
        }

        /// <summary>
        ///     Decodes the given JSON string into <see cref="BodyCalibrationProfile"/> object.
        /// </summary>
        public static BodyCalibrationProfile Deserialize(string profileJson)
        {
            var decodedProfile = ScriptableObject.CreateInstance<BodyCalibrationProfile>();
            JsonUtility.FromJsonOverwrite(profileJson, decodedProfile);

            return decodedProfile;
        }

        #endregion
    }
}