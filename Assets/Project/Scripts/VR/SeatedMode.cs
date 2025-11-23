// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction.Locomotion;
using System.Collections;
using UnityEngine;

namespace Oculus.Interaction.ComprehensiveSample
{
    /// <summary>
    /// Adjusts the tracking space height when seated mode is enabled
    /// </summary>
    public class SeatedMode : MonoBehaviour
    {
        private const string PLAYERPREFS_KEY = "settings.seated_mode";

        [SerializeField]
        private OVRCameraRig _rig;
        [SerializeField]
        private PlayerLocomotor _locomotor;
        [SerializeField]
        private Transform _averageEyeLevel; //assumed to be a child
        [SerializeField]
        private float _seatedEyeHeight = 1.63f;

        private float _playerNaturalHeight;
        private float _heightOffset = 0f;

        private static bool? _isOn;
        public static bool IsOn
        {
            get
            {
                if (!_isOn.HasValue) _isOn = Store.GetInt(PLAYERPREFS_KEY) > 0;
                return _isOn.Value;
            }
        }

private IEnumerator Start()
        {
            // Wait for VR tracking to initialize properly on device
            yield return null;
            yield return null;
            yield return null;
            
            // CRITICAL: Reset tracking space to zero BEFORE capturing natural height
            // This ensures we're measuring from a clean baseline
            Vector3 trackingSpacePos = _rig.trackingSpace.localPosition;
            trackingSpacePos.y = 0;
            _rig.trackingSpace.localPosition = trackingSpacePos;
            
            yield return new WaitForEndOfFrame();
            
            // Now capture the player's natural standing eye height
            // Use localPosition.y which is relative to the rig, not world space
            _playerNaturalHeight = _rig.centerEyeAnchor.localPosition.y;
            
            Debug.Log($"[SeatedMode] Player natural height captured: {_playerNaturalHeight}m");
            Debug.Log($"[SeatedMode] Seated mode is: {(IsOn ? "ON" : "OFF")}");
            
            _locomotor.WhenLocomotionEventHandled += SyncPosition;
            
            // Apply the initial state
            UpdateCameraRigHeight();
        }

        private void Update()
        {
            var eyePose = PoseUtils.Delta(_rig.transform, _rig.centerEyeAnchor);
            eyePose.position = eyePose.position.SetY(_seatedEyeHeight);
            _averageEyeLevel.SetPose(eyePose, Space.Self);
        }

        private void OnDestroy()
        {
            _locomotor.WhenLocomotionEventHandled -= SyncPosition;
        }

        private void SyncPosition(LocomotionEvent locomotion, Pose _)
        {
            // When locomotion happens, just reapply the current offset
            // The rig.transform has already been moved by the locomotor
            // We just need to maintain the tracking space offset
            if (locomotion.IsTeleport())
            {
                // Reapply offset after teleport
                ApplyOffset();
            }
            else if (locomotion.IsSnapTurn())
            {
                // Rotation doesn't affect height
            }
        }

private void UpdateCameraRigHeight()
        {
            if (IsOn)
            {
                // Calculate how much to offset the tracking space
                // REMOVED Mathf.Max - we need negative offsets to lower the camera!
                _heightOffset = _seatedEyeHeight - _playerNaturalHeight;
                Debug.Log($"[SeatedMode] Seated mode ON - offset: {_heightOffset}m (target: {_seatedEyeHeight}m, natural: {_playerNaturalHeight}m)");
            }
            else
            {
                // No offset in standing mode
                _heightOffset = 0f;
                Debug.Log($"[SeatedMode] Seated mode OFF - offset reset to 0");
            }
            
            ApplyOffset();
        }

private void ApplyOffset()
        {
            // Apply the offset to the tracking space LOCAL position
            // This raises/lowers the camera without moving the rig.transform
            Vector3 localPos = _rig.trackingSpace.localPosition;
            float oldY = localPos.y;
            localPos.y = _heightOffset;
            _rig.trackingSpace.localPosition = localPos;
            
            Debug.Log($"[SeatedMode] Applied offset - trackingSpace.localPosition.y: {oldY}m -> {_heightOffset}m");
        }

        public static void SetSeatedMode(bool value)
        {
            _isOn = value;
            Store.SetInt(PLAYERPREFS_KEY, value ? 1 : 0);

            var instances = FindObjectsOfType<SeatedMode>();
            for (int i = 0; i < instances.Length; i++)
            {
                instances[i].UpdateCameraRigHeight();
            }
        }
    }
}