using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;


public class GestureHandler : MonoBehaviour, IMixedRealityGestureHandler<Vector3>
{

    public Material bBoxHandleMat;
    public Material bBoxHandleMatGrab;
    public GameObject bBoxScaleHandlePrefab;
    public GameObject bBoxScaleHandleSlatePrefab;
    public GameObject bBoxRotateHandlePrefab;

    public Material defaultMat;

    private PhotoProvider photoProvider;

    public float startTime;

    public void Start()
    {

    }

    private void OnEnable()
    {
        CoreServices.InputSystem?.PushFallbackInputHandler(GameObject.Find("GlobalGestureEventListener"));
    }

    private void OnDisable()
    {
        CoreServices.InputSystem?.PopFallbackInputHandler();
    }

    public void OnGestureCanceled(InputEventData eventData)
    {
        Debug.Log("Global cancel");
    }

    public void OnGestureCompleted(InputEventData<Vector3> eventData)
    {
        Debug.Log("Global complete vec3");
    }

    /// <summary>
    /// Starts the pipeline
    /// </summary>
    /// <param name="eventData"></param>
    public void OnGestureCompleted(InputEventData eventData)
    {
        startTime = Time.time;
        Debug.Log("Global complete");
        eventData.Use();
        Debug.Log("Used eventdata, evaluating");

        var action = eventData.MixedRealityInputAction.Description;

        // finding pointer that triggered the action and retrieve current end point
        if (action == "Select")
        {
            Vector3 endPoint = Vector3.zero;
            foreach (var source in CoreServices.InputSystem.DetectedInputSources)
            {
                if (source.SourceType == Microsoft.MixedReality.Toolkit.Input.InputSourceType.Hand)
                {
                    foreach (var p in source.Pointers)
                    {
                        if (p is IMixedRealityNearPointer)
                        {
                            // ignore
                        }
                        if (p.Result != null)
                        {
                            var startPoint = p.Position;
                            endPoint = p.Result.Details.Point;
                        }
                    }
                }
            }
            photoProvider = gameObject.GetComponent<PhotoProvider>();
            photoProvider.TakePhoto(endPoint);

        }
    }

    public void OnGestureStarted(InputEventData eventData)
    {
        Debug.Log("Global started");
    }

    public void OnGestureUpdated(InputEventData<Vector3> eventData)
    {
        Debug.Log("Global updated vec3");
    }

    public void OnGestureUpdated(InputEventData eventData)
    {
        Debug.Log("Global updated");
    }
}

