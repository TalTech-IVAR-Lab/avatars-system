namespace Games.NoSoySauce.Avatars.AvatarSystem.Humanoid
{
    using UnityEditor;

    /// <summary>
    /// Checks if FinalIK and KinematicCharacterController packages are present in the project.
    /// They are required by the avatars system, but cannot be included into packages dependencies normally, so we run a custom automated check. 
    /// </summary>
    [InitializeOnLoad]
    public class AvatarsSystemPackageDependencyChecker
    {
        static AvatarsSystemPackageDependencyChecker() { CheckDependenciesPresence(); }
        
        public static void CheckDependenciesPresence()
        {
            bool isFinalIKPresent = IsPresentInProject("RootMotion") || IsPresentInProject("FinalIK");
            bool isKCCPresent = IsPresentInProject("KinematicCharacterController");

            if (!isFinalIKPresent && !isKCCPresent)
            {
                EditorUtility.DisplayDialog(
                    "Avatars System: missing dependency warning", 
                    "FinalIK and Kinematic Character Controller packages are required by Avatars System, but it were not found in this project.\nPlease install them from the Asset Store.", 
                    "Ok"
                );
            }
            else if (!isFinalIKPresent)
            {
                EditorUtility.DisplayDialog(
                    "Avatars System: missing dependency warning", 
                    "FinalIK package is required by Avatars System, but it was not found in this project.\nPlease install it from the Asset Store.", 
                    "Ok"
                );
            }
            else if (!isKCCPresent)
            {
                EditorUtility.DisplayDialog(
                    "Avatars System: missing dependency warning", 
                    "Kinematic Character Controller package is required by Avatars System, but it was not found in this project.\nPlease install it from the Asset Store.", 
                    "Ok"
                );
            }
        }

        public static bool IsPresentInProject(string assetName)
        {
            string[] guids = AssetDatabase.FindAssets(assetName);
            return guids.Length > 0;
        }
    }
}