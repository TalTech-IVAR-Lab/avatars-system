namespace Games.NoSoySauce.Avatars.Calibration.Hands
{
    using System.IO;
    using UnityEditor;
    using UnityEngine;

    public class WristPoseSamplerEditorWindow : EditorWindow
    {
        private Transform wristTransform;
        private Hand hand;
        private WristPoseModifier.MirrorPlane mirrorPlane;
        private string outputFolder;
        private string outputFileName;

        // Add menu named "My Window" to the Window menu
        [MenuItem("Tools/NoSoySauce Games/Avatars/Wrist Pose Sampler")]
        private static void Init()
        {
            // Get existing open window or if none, make a new one
            var window = GetWindow<WristPoseSamplerEditorWindow>();
            window.titleContent = new GUIContent("Wrist Pose Sampler");
            window.Show();

            // Init default values
            window.hand = Hand.Right;
            window.mirrorPlane = WristPoseModifier.MirrorPlane.YZ;
            window.outputFolder = "Resources/NoSoySauce Games/Avatars/Wrist Pose Modifiers";
            window.outputFileName = "New Wrist Pose Modifier";
        }

        private void OnGUI()
        {
            GUILayout.Label("Wrist Settings", EditorStyles.boldLabel);

            string explanationMessage = "This tool allows to create wrist pose profiles for VR controllers.\n" +
                                        "To create new profile, you have to provide a \"Wrist transform\" GameObject, which must be a child of SDK controller object. After the virtual wrist is correctly aligned relative to real user's wrist, click \"Sample\" and the pose asset will be created and saved. This asset can then be used to automatically align user's virtual wrists for the given VR controller model.";
            EditorGUILayout.HelpBox(explanationMessage, MessageType.None);

            string wristTransformTooltip = "Transform representing user wrist. Must be a child of the SDK controller object for the selected hand.";
            wristTransform = (Transform) EditorGUILayout.ObjectField(new GUIContent("Wrist transform", wristTransformTooltip), wristTransform, typeof(Transform), true);
            string handTooltip = "Which hand the sampled wrist belongs to?";
            hand = (Hand) EditorGUILayout.EnumPopup(new GUIContent("Hand", handTooltip), hand);
            string mirrorPlaneTooltip = "Along which axes should the sampled pose be mirrored when applied to other hand?";
            mirrorPlane = (WristPoseModifier.MirrorPlane) EditorGUILayout.EnumPopup(new GUIContent("Mirror plane", mirrorPlaneTooltip), mirrorPlane);

            GUI.enabled = false;
            EditorGUILayout.LabelField("Active SDK", WristPoseSampler.GetActiveSdkString());
            EditorGUILayout.LabelField("Active controller", WristPoseSampler.GetActiveControllerString(hand));
            GUI.enabled = true;

            GUILayout.Label("Output Settings", EditorStyles.boldLabel);
            outputFolder = EditorGUILayout.TextField("Output folder", outputFolder);
            outputFileName = EditorGUILayout.TextField("Output file name", outputFileName);

            GUI.enabled = wristTransform != null;
            if (GUILayout.Button("Sample")) WristPoseSampler.SampleWristPose(wristTransform, hand, mirrorPlane, outputFolder, outputFileName);

            GUI.enabled = true;
        }
    }
}