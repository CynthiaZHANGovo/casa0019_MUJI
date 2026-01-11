using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Detect QR codes for bus routes 339 / 108 and toggle only the bus panels.
/// Other UI modules (temperature/weather/etc.) remain visible.
/// </summary>
[RequireComponent(typeof(ARTrackedImageManager))]
public class BusQRImageController : MonoBehaviour
{
    [Header("UI Root Object")]
    public GameObject transitHubCanvas;   // Entire World Space Canvas (full UI)

    [Header("Bus Panels")]
    public GameObject busPanel339;        // BusPanel339 under MainContent
    public GameObject busPanel108;        // BusPanel108 under MainContent

    [Header("Optional Canvas Offset relative to QR")]
    public Vector3 canvasOffset = new Vector3(0f, 0.1f, 0f);

    private ARTrackedImageManager imageManager;

    void Awake()
    {
        imageManager = GetComponent<ARTrackedImageManager>();
    }

    void OnEnable()
    {
        imageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    void OnDisable()
    {
        imageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs args)
    {
        // Newly detected / updated tracked images
        foreach (var img in args.added)
        {
            UpdateForImage(img);
        }

        foreach (var img in args.updated)
        {
            UpdateForImage(img);
        }

        // No need to handle removed images here;
        // we simply keep showing the last active UI
    }

    void UpdateForImage(ARTrackedImage img)
    {
        if (img.trackingState != TrackingState.Tracking)
            return;
        if (transitHubCanvas == null)
            return;

        // 1. First time, attach the Canvas under this QR transform
        if (transitHubCanvas.transform.parent != img.transform)
        {
            // worldPositionStays = false means adopt local coordinates directly
            transitHubCanvas.transform.SetParent(img.transform, false);
        }

        // 2. Apply offset in the QR's local coordinate space
        //    canvasOffset is interpreted relative to the QR's origin
        transitHubCanvas.transform.localPosition = canvasOffset;

        // 3. Keep the Canvas aligned to the QR (modify rotation if desired)
        transitHubCanvas.transform.localRotation = Quaternion.identity;
        // Example for slight tilt:
        // transitHubCanvas.transform.localRotation = Quaternion.Euler(60f, 0f, 0f);

        // 4. Ensure the UI is enabled
        if (!transitHubCanvas.activeSelf)
            transitHubCanvas.SetActive(true);

        // Below toggles the correct bus panel without touching location or rotation
        string name = img.referenceImage.name;
        if (name == "QR_339")
        {
            if (busPanel339 != null) busPanel339.SetActive(true);
            if (busPanel108 != null) busPanel108.SetActive(false);
        }
        else if (name == "QR_108")
        {
            if (busPanel339 != null) busPanel339.SetActive(false);
            if (busPanel108 != null) busPanel108.SetActive(true);
        }
    }
}
