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
            // Get existing open window or if none, make a new one:
            var window = (WristPoseSamplerEditorWindow) GetWindow(typeof(WristPoseSamplerEditorWindow));
            window.titleContent = new GUIContent("Wrist Pose Sampler");
            window.Show();

            // Init default values.
            window.hand = Hand.Right;
            window.mirrorPlane = WristPoseModifier.MirrorPlane.YZ;
            window.outputFolder = "Resources/NoSoySauce/Avatars/Wrist Pose Modifiers";
            window.outputFileName = "New Wrist Pose Modifier";
        }

        private void OnGUI()
        {
            GUILayout.Label("Wrist Settings", EditorStyles.boldLabel);

            wristTransform = (Transform) EditorGUILayout.ObjectField("Wrist transform", wristTransform, typeof(Transform), true);
            hand = (Hand) EditorGUILayout.EnumPopup("Hand", hand);
            mirrorPlane = (WristPoseModifier.MirrorPlane) EditorGUILayout.EnumPopup("Mirror plane", mirrorPlane);

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