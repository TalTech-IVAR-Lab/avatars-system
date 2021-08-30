namespace Games.NoSoySauce.Avatars.Hands
{
	// using Photon.Pun;
    // using Games.NoSoySauce.Networking.Multiplayer;

    public class NetworkedAvatarHandController : AvatarHandController//, IPunObservable
    {
        /// <summary>
        /// Current target values of all five fingers encoded in a 5-byte array for lighter network transfer.
        /// </summary>
        /// <remarks>
        /// The values are encoded in the following order:
        ///  1st byte = Thumb
        ///  2nd byte = Index
        ///  3rd byte = Middle
        ///  4th byte = Ring
        ///  5th byte = Pinky
        /// </remarks>
        protected byte[] encodedFingerTargets = new byte[5];

        // public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        // {
        //     if (stream.IsWriting)
        //     {
        //         EncodeFingerTargets();
        //
        //         stream.SendNext(encodedFingerTargets[0]);
        //         stream.SendNext(encodedFingerTargets[1]);
        //         stream.SendNext(encodedFingerTargets[2]);
        //         stream.SendNext(encodedFingerTargets[3]);
        //         stream.SendNext(encodedFingerTargets[4]);
        //     }
        //     else
        //     {
        //         encodedFingerTargets[0] = (byte)stream.ReceiveNext();
        //         encodedFingerTargets[1] = (byte)stream.ReceiveNext();
        //         encodedFingerTargets[2] = (byte)stream.ReceiveNext();
        //         encodedFingerTargets[3] = (byte)stream.ReceiveNext();
        //         encodedFingerTargets[4] = (byte)stream.ReceiveNext();
        //
        //         DecodeFingerTargets();
        //     }
        // }

        /// <inheritdoc />
        protected override void SubscribeToFingerActions()
        {
            // if (MultiplayerManager.IsLocal(this) == false)
            // {
            //     /// Do not subscribe to inputs if not controlled locally.
            //     return;
            // }

            base.SubscribeToFingerActions();
        }

        /// <summary>
        /// Encodes current state of the fingers and saves it into <see cref="encodedFingerTargets"/> variable.
        /// </summary>
        protected virtual void EncodeFingerTargets()
        {
            encodedFingerTargets[0] = FloatToByte(fingerTargets[0]);
            encodedFingerTargets[1] = FloatToByte(fingerTargets[1]);
            encodedFingerTargets[2] = FloatToByte(fingerTargets[2]);
            encodedFingerTargets[3] = FloatToByte(fingerTargets[3]);
            encodedFingerTargets[4] = FloatToByte(fingerTargets[4]);
        }

        /// <summary>
        /// Decodes current state of the fingers from <see cref="encodedFingerTargets"/>.
        /// </summary>
        protected virtual void DecodeFingerTargets()
        {
            fingerTargets[0] = ByteToFloat(encodedFingerTargets[0]);
            fingerTargets[1] = ByteToFloat(encodedFingerTargets[1]);
            fingerTargets[2] = ByteToFloat(encodedFingerTargets[2]);
            fingerTargets[3] = ByteToFloat(encodedFingerTargets[3]);
            fingerTargets[4] = ByteToFloat(encodedFingerTargets[4]);
        }

        /// <summary>
        /// Converts float value (must be in range [0..1]) to a single byte.
        /// Used for encoding finger states.
        /// </summary>
        /// <param name="value">Float value to convert.</param>
        /// <returns>Converted byte value.</returns>
        protected byte FloatToByte(float value)
        {
            return (byte)(value * 255f);
        }

        /// <summary>
        /// Converts byte value to float in range [0..1].
        /// Used for decoding finger states.
        /// </summary>
        /// <param name="value">Byte value to convert.</param>
        /// <returns>Converted float value.</returns>
        protected float ByteToFloat(byte value)
        {
            return 1f / 256f * (float)value;
        }
    }
}