// TapeMeasureController.cs
using UnityEngine;
using TMPro;
using Oculus.Interaction;
using System.Collections.Generic;
using UnityEngine.Assertions;

[RequireComponent(typeof(RayInteractor))]
public class TapeMeasureController : MonoBehaviour
{
    [Header("Interaction Components")]
    [Tooltip("The RayInteractor used for pointing and finding MeasurePoints.")]
    [SerializeField]
    private RayInteractor _rayInteractor;

    [Tooltip("The ActiveState that represents the trigger button being pressed. This should be an OVRControllerButtonUsageActiveState.")]
    [SerializeField, Interface(typeof(IActiveState))]
    private MonoBehaviour _triggerActiveState;
    private IActiveState TriggerActiveState;

    [Header("Visuals & Raycasting")]
    public LineRenderer lineRenderer;
    public TextMeshProUGUI liveMeasurementText;
    public GameObject finalMeasurementUIPrefab;
    public GameObject reticle;
    public Material reticleHighlightMaterial;
    public Material reticleDefaultMaterial;

    [Tooltip("The layers the tape measure's line can collide with for visualization.")]
    public LayerMask collisionLayerMask = ~0;
    [Tooltip("The maximum distance for the measuring line.")]
    public float maxDistance = 100f;

    private enum MeasureState { Idle, Measuring }
    private MeasureState _currentState = MeasureState.Idle;
    
    private MeasurePoint _startMeasurePoint;
    private Vector3 _currentEndPoint;
    private bool _wasTriggerPressed = false;
    private Renderer _reticleRenderer;

    private readonly List<GameObject> _activeFinalUIs = new List<GameObject>();

    private void Awake()
    {
        TriggerActiveState = _triggerActiveState as IActiveState;
        // --- DEBUG ---
        if (TriggerActiveState == null)
        {
            Debug.LogError("[TapeMeasureController] CRITICAL: _triggerActiveState could not be cast to IActiveState.", this);
        }
    }

    private void Start()
    {
        Assert.IsNotNull(_rayInteractor, "Ray Interactor cannot be null.");
        Assert.IsNotNull(TriggerActiveState, "Trigger Active State cannot be null.");
        Assert.IsNotNull(lineRenderer, "Line Renderer cannot be null.");
        Assert.IsNotNull(liveMeasurementText, "Live Measurement Text cannot be null.");
        Assert.IsNotNull(reticle, "Reticle cannot be null.");

        _reticleRenderer = reticle.GetComponent<Renderer>();
        Assert.IsNotNull(_reticleRenderer, "Reticle needs a Renderer component.");
        
        // --- DEBUG ---
        Debug.Log("[TapeMeasureController] Start: All component assertions passed. Initializing.", this);

        reticle.SetActive(false);
        lineRenderer.enabled = false;
        liveMeasurementText.gameObject.SetActive(false);
    }

    private void Update()
    {
        HandleEndPointAndReticle();
        HandleInput();
        UpdateLineAndText();
    }

    private void HandleEndPointAndReticle()
    {
        Transform rayOrigin = _rayInteractor.transform;
        bool hitSomething = Physics.Raycast(rayOrigin.position, rayOrigin.forward, out RaycastHit hit, maxDistance, collisionLayerMask);

        if (hitSomething)
        {
            _currentEndPoint = hit.point;
            reticle.transform.position = _currentEndPoint;
            reticle.SetActive(true);
        }
        else
        {
            _currentEndPoint = rayOrigin.position + rayOrigin.forward * maxDistance;
            reticle.SetActive(false);
        }
        
        bool canHighlight = CanHighlightReticle();
        SetReticleHighlight(canHighlight);
    }

    private void HandleInput()
    {
        bool isTriggerPressed = TriggerActiveState.Active;

        // On Trigger Down
        if (isTriggerPressed && !_wasTriggerPressed)
        {
            // --- DEBUG ---
            Debug.Log($"[TapeMeasureController] Trigger Pressed. Current State: {_currentState}. Hovered Interactable: {_rayInteractor.Interactable?.name ?? "None"}", this);
            
            if (_currentState == MeasureState.Idle && _rayInteractor.Interactable != null)
            {
                MeasurePoint point = _rayInteractor.Interactable.GetComponent<MeasurePoint>();
                if (point != null)
                {
                    StartMeasurement(point);
                }
                else
                {
                    // --- DEBUG ---
                    Debug.LogWarning($"[TapeMeasureController] Trigger pressed on an interactable ('{_rayInteractor.Interactable.name}'), but it has no MeasurePoint component.", this);
                }
            }
        }
        // On Trigger Up
        else if (!isTriggerPressed && _wasTriggerPressed)
        {
            // --- DEBUG ---
            Debug.Log($"[TapeMeasureController] Trigger Released. Current State: {_currentState}. Hovered Interactable: {_rayInteractor.Interactable?.name ?? "None"}", this);

            if (_currentState == MeasureState.Measuring)
            {
                if (_rayInteractor.Interactable != null)
                {
                    MeasurePoint endPoint = _rayInteractor.Interactable.GetComponent<MeasurePoint>();
                    if (endPoint != null && endPoint == _startMeasurePoint.pair)
                    {
                        CompleteMeasurement(endPoint.transform.position);
                        return; // Exit to avoid calling CancelMeasurement
                    }
                }
                // --- DEBUG ---
                Debug.Log("[TapeMeasureController] Trigger released over invalid target or no target. Cancelling measurement.", this);
                CancelMeasurement();
            }
        }
        
        _wasTriggerPressed = isTriggerPressed;
    }
    
    private void StartMeasurement(MeasurePoint point)
    {
        // --- DEBUG ---
        Debug.Log($"[TapeMeasureController] Starting Measurement from '{point.name}'. Expected pair is '{point.pair?.name ?? "None"}'.", this);
        _currentState = MeasureState.Measuring;
        _startMeasurePoint = point;
        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, _startMeasurePoint.transform.position);
        liveMeasurementText.gameObject.SetActive(true);
    }
    
    private void UpdateLineAndText()
    {
        if (_currentState != MeasureState.Measuring) return;

        lineRenderer.SetPosition(1, _currentEndPoint);
        float distance = Vector3.Distance(_startMeasurePoint.transform.position, _currentEndPoint);
        liveMeasurementText.text = StructureScript.FormatMeasurement(distance);
    }

    private void CompleteMeasurement(Vector3 finalEndPoint)
    {
        float measuredDistance = Vector3.Distance(_startMeasurePoint.transform.position, finalEndPoint);
        // --- DEBUG ---
        Debug.Log($"[TapeMeasureController] Measurement Complete! Distance: {measuredDistance}. Recording to '{_startMeasurePoint.parentStructure.name}' for dimension '{_startMeasurePoint.dimensionType}'.", this);
        _startMeasurePoint.parentStructure.RecordMeasurement(_startMeasurePoint.dimensionType, measuredDistance);

        if (finalMeasurementUIPrefab != null)
        {
            GameObject finalUI = Instantiate(finalMeasurementUIPrefab, finalEndPoint, Quaternion.identity);
            var tmp = finalUI.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = StructureScript.FormatMeasurement(measuredDistance);
            }
            _activeFinalUIs.Add(finalUI);
        }
        
        ResetToIdle();
    }
    
    private void CancelMeasurement()
    {
        // --- DEBUG ---
        Debug.Log("[TapeMeasureController] Measurement Canceled.", this);
        ResetToIdle();
    }

    private void ResetToIdle()
    {
        // --- DEBUG ---
        Debug.Log("[TapeMeasureController] Resetting to Idle state.", this);
        _currentState = MeasureState.Idle;
        _startMeasurePoint = null;
        lineRenderer.enabled = false;
        liveMeasurementText.gameObject.SetActive(false);
    }
    
    private bool CanHighlightReticle()
    {
        MeasurePoint hoveredPoint = _rayInteractor.Interactable?.GetComponent<MeasurePoint>();
        if (hoveredPoint == null) return false;

        if (_currentState == MeasureState.Idle)
        {
            return true; // Can start a measurement from any valid point
        }
        
        if (_currentState == MeasureState.Measuring && _startMeasurePoint != null)
        {
            // Highlight only if the hovered point is the designated pair of the starting point
            return hoveredPoint == _startMeasurePoint.pair;
        }

        return false;
    }
    
    private void SetReticleHighlight(bool isHighlighted)
    {
        if (_reticleRenderer != null)
        {
            Material newMaterial = isHighlighted ? reticleHighlightMaterial : reticleDefaultMaterial;
            // --- DEBUG ---
            // Log only when the material actually changes to avoid spamming the console
            if (_reticleRenderer.material != newMaterial)
            {
                Debug.Log($"[TapeMeasureController] Setting reticle highlight: {isHighlighted}", this);
                _reticleRenderer.material = newMaterial;
            }
        }
    }
}