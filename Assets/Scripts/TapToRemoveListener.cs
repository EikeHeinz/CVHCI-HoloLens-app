using UnityEngine;
using Microsoft.MixedReality.Toolkit.Input;

public class TapToRemoveListener : MonoBehaviour, IMixedRealityGestureHandler<Vector3>
{

    private System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();

    void IMixedRealityGestureHandler.OnGestureCanceled(InputEventData eventData)
    {
        Debug.Log("Gesture cancelled");
        eventData.Use();
    }

    void IMixedRealityGestureHandler.OnGestureCompleted(InputEventData eventData)
    {
        Debug.Log("Gesture completed");
        timer.Stop();
        long durationSec = timer.ElapsedMilliseconds / 1000;
        Debug.Log("Duration in seconds: " + durationSec);
        if (durationSec > 2)
        {
            Debug.Log("Deleting object");
            Object.Destroy(this.gameObject);
        }
        Debug.Log("UsingEventData");
        eventData.Use();
    }

    void IMixedRealityGestureHandler<Vector3>.OnGestureCompleted(InputEventData<Vector3> eventData)
    {
        Debug.Log("completed gesture with vector3");
        timer.Stop();
        long durationSec = timer.ElapsedMilliseconds / 1000;
        Debug.Log("Duration in seconds: " + durationSec);
        if (durationSec > 2)
        {
            Debug.Log("Deleting object");
            Object.Destroy(this.gameObject);
        }
        Debug.Log("UsingEventData");
        eventData.Use();
    }

    void IMixedRealityGestureHandler.OnGestureStarted(InputEventData eventData)
    {
        Debug.Log("Started gesture");
        Debug.Log("Starting timer");
        timer.Start();
        Debug.Log("Using eventData");
        eventData.Use();
    }

    void IMixedRealityGestureHandler.OnGestureUpdated(InputEventData eventData)
    {
        Debug.Log("Updated gesture");
        eventData.Use();
        timer.Stop();
    }

    void IMixedRealityGestureHandler<Vector3>.OnGestureUpdated(InputEventData<Vector3> eventData)
    {
        Debug.Log("Updated with vector3");
        eventData.Use();
        timer.Stop();
    }
}
