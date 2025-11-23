// Extension methods for LocomotionEvent
// Required by SeatedMode from First Hand sample project

using UnityEngine;
using Oculus.Interaction.Locomotion;

namespace Oculus.Interaction.ComprehensiveSample
{
    public static class LocomotionEventExtensions
    {
        /// <summary>
        /// Returns true if the locomotion event is a teleport action
        /// </summary>
        public static bool IsTeleport(this LocomotionEvent locomotionEvent)
        {
            // Check if the event represents a teleport by examining the translation type
            return locomotionEvent.Translation == LocomotionEvent.TranslationType.Absolute;
        }

        /// <summary>
        /// Returns true if the locomotion event is a snap turn action
        /// </summary>
        public static bool IsSnapTurn(this LocomotionEvent locomotionEvent)
        {
            // Check if the event represents a snap turn by examining the rotation type
            return locomotionEvent.Rotation == LocomotionEvent.RotationType.Relative
                   && locomotionEvent.Translation == LocomotionEvent.TranslationType.None;
        }
    }
}