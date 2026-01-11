using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using InputSystemTouchPhase = UnityEngine.InputSystem.TouchPhase;
#endif

public class PlaceDigitalCarOnPlane : MonoBehaviour
{
    [Tooltip("Prefab to place on detected planes (DigitalCar)")]
    public GameObject placedPrefab;

    private ARRaycastManager raycastManager;
    private GameObject spawnedObject;

    private static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private void Awake()
    {
        raycastManager = GetComponent<ARRaycastManager>();

        // If the project uses only the new Input System, enable EnhancedTouch support
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        EnhancedTouchSupport.Enable();
#endif
    }

    private void Update()
    {
        // First, retrieve the tap/click position from touch or mouse
        if (!TryGetTapPosition(out Vector2 touchPosition))
            return;

        if (raycastManager != null &&
            raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;

            if (spawnedObject == null)
            {
                // First tap: instantiate the digital car
                spawnedObject = Instantiate(placedPrefab, hitPose.position, hitPose.rotation);
            }
            else
            {
                // Subsequent taps: move the car to the new plane position
                spawnedObject.transform.SetPositionAndRotation(hitPose.position, hitPose.rotation);
            }
        }
    }

    /// <summary>
    /// Get the tap position supporting BOTH old Input System and the new Input System
    /// </summary>
    private bool TryGetTapPosition(out Vector2 position)
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        // New Input System: using EnhancedTouch
        var touches = ETouch.activeTouches;
        if (touches.Count == 0)
        {
            position = default;
            return false;
        }

        var touch = touches[0];
        if (touch.phase != InputSystemTouchPhase.Began)
        {
            position = default;
            return false;
        }

        position = touch.screenPosition;
        return true;
#else
        // Old Input System: using Input.touchCount
        if (Input.touchCount == 0)
        {
            position = default;
            return false;
        }

        var touch = Input.GetTouch(0);
        if (touch.phase != TouchPhase.Began)
        {
            position = default;
            return false;
        }

        position = touch.position;
        return true;
#endif
    }
}
