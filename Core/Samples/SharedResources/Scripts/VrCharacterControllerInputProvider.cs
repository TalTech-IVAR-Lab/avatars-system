namespace Games.NoSoySauce.Avatars.Samples
{
    using ZinniaExtensions.Action;
    using UnityEngine;
    using Zinnia.Action;
    using Zinnia.Process;

    public class VrCharacterControllerInputProvider : MonoBehaviour, IProcessable
    {
        public VrCharacterController character;

        public PoseAction headsetPoseInput;
        public PoseAction playAreaPoseInput;
        public Vector2Action moveDirectionInput;
        public PoseAction moveOrientationInput;
        public BooleanAction snapTurnLeftInput;
        public BooleanAction snapTurnRightInput;
        public BooleanAction jumpInput;

        
        private VrCharacterController.VrCharacterInputs inputsStruct;

        private bool snapTurnLeftPressedLastFrame;
        private bool snapTurnRightPressedLastFrame;
        private bool jumpPressedLastFrame;

        public void Process() { SetCharacterInputs(); }

        public void SetCharacterInputs()
        {
            // Pose inputs
            inputsStruct.headsetPose = headsetPoseInput.Value;
            inputsStruct.playAreaPose = playAreaPoseInput.Value;
            
            // Direction inputs
            inputsStruct.moveDirection = moveDirectionInput.Value;
            inputsStruct.moveOrientation = moveOrientationInput.Value.rotation;

            // Snap turn inputs
            bool snapTurnLeftPressed = snapTurnLeftInput.Value;
            inputsStruct.snapTurnLeftDown = snapTurnLeftInput.Value && !snapTurnLeftPressedLastFrame;
            snapTurnLeftPressedLastFrame = snapTurnLeftPressed;
            bool snapTurnRightPressed = snapTurnRightInput.Value;
            inputsStruct.snapTurnRightDown = snapTurnRightInput.Value && !snapTurnRightPressedLastFrame;
            snapTurnRightPressedLastFrame = snapTurnRightPressed;
            
            // Jump input
            bool jumpPressed = jumpInput.Value;
            inputsStruct.jumpDown = jumpInput.Value && !jumpPressedLastFrame;
            jumpPressedLastFrame = jumpPressed;

            character.SetInputs(ref inputsStruct);
        }
    }
}