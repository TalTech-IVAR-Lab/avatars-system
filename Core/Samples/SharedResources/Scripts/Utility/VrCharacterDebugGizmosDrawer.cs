namespace Games.NoSoySauce.Avatars.AvatarSystem.Samples
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using KinematicCharacterController;
    using UnityEngine;

    /// <summary>
    /// Draws debug gizmos for VR character controllers.
    /// </summary>
    public class VrCharacterDebugGizmosDrawer : MonoBehaviour
    {
        [Header("References")]
        public Transform playAreaTransform;

        public Transform headsetTransform;
        public KinematicCharacterMotor character;

        [Header("Gizmos Settings")]
        public Color headsetOffsetColor = Color.red;

        private void OnDrawGizmos() { DrawHeadsetOffsetGizmo(); }

        private void DrawHeadsetOffsetGizmo()
        {
            var charFloorPlane = new Plane(character.CharacterUp, character.TransientPosition);
            var headsetPosition = headsetTransform.position;

            var pointA = character.TransientPosition;
            var pointB = charFloorPlane.ClosestPointOnPlane(headsetPosition);

            Gizmos.color = headsetOffsetColor;
            Gizmos.DrawLine(pointA, pointB);
        }
    }
}