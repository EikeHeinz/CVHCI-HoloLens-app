using UnityEngine;
using System.IO;
using System.Text;
using Dummiesman;
using Microsoft.MixedReality.Toolkit.UI;


public class ObjectLoader : MonoBehaviour
{
    public Material bBoxHandleMat;
    public Material bBoxHandleMatGrab;
    public GameObject bBoxScaleHandlePrefab;
    public GameObject bBoxScaleHandleSlatePrefab;
    public GameObject bBoxRotateHandlePrefab;

    public Material defaultMat;

    private GameObject currentObj;


    /// <summary>
    /// creates a fully configured GameObject
    /// </summary>
    /// <param name="objectData">the OBJ data describing the object</param>
    public void DisplayObject(string objectData)
    {
        Debug.Log("Generating model");
        var textStream = new MemoryStream(Encoding.UTF8.GetBytes(objectData));
        Debug.Log("Created memorystream");
        GameObject go = new OBJLoader().Load(textStream);
        currentObj = go;
        go.SetActive(true);
        Debug.Log("Created gameobject");

        Debug.Log("Configuring gameobject");

        GameObject child = go.transform.GetChild(0).gameObject;

        AddComponentsToGameObject(go, false);
        AddComponentsToGameObject(child, true);
        
        // invoking rescaling after 2 seconds to prevent rescaling from not having an effect
        Invoke("RescaleObj", 2f);
        Debug.Log("Done");
        float endTime = Time.time;

        float startTime = gameObject.GetComponent<GestureHandler>().startTime;
        Debug.Log("Time elapsed: " + (endTime - startTime).ToString("F4"));
    }

    /// <summary>
    /// performs scaling, to limit the size of the object
    /// </summary>
    private void RescaleObj()
    {
        GameObject child = currentObj.transform.GetChild(0).gameObject;
        child.GetComponent<MeshFilter>().mesh.RecalculateBounds();
        child.GetComponent<MeshFilter>().sharedMesh.RecalculateBounds();
        currentObj = null;
    }

    /// <summary>
    /// Attaches and configures MRTK scripts to the gameobject to allow interaction.
    /// </summary>
    /// <param name="go">current gameobject</param>
    /// <param name="isChild">indicates whether this gameobject is a child of another gameobject</param>
    private void AddComponentsToGameObject(GameObject go, bool isChild)
    {
        if (go.GetComponent<MeshFilter>() == null)
        {
            go.AddComponent<MeshFilter>();
        }
        if (go.GetComponent<MeshRenderer>() == null)
        {
            go.AddComponent<MeshRenderer>();
        }

        if (isChild)
        {
            go.GetComponent<MeshFilter>().sharedMesh.RecalculateBounds();
        }
        else
        {
            go.transform.localScale = new Vector3(0.005f, 0.005f, 0.005f);


            Debug.Log("Adding MRTK components to gameobject");
            var boxCollider = go.AddComponent<BoxCollider>();
            boxCollider.enabled = true;
            var bbox = go.AddComponent<BoundingBox>();
            bbox.HandleMaterial = bBoxHandleMat;
            bbox.HandleGrabbedMaterial = bBoxHandleMatGrab;
            bbox.ScaleHandlePrefab = bBoxScaleHandlePrefab;
            bbox.ScaleHandleSlatePrefab = bBoxScaleHandleSlatePrefab;
            bbox.ScaleHandleSize = 0.016f;
            bbox.ScaleHandleColliderPadding = Vector3.one * 0.016f;
            bbox.RotationHandlePrefab = bBoxRotateHandlePrefab;
            bbox.RotationHandleSize = 0.016f;
            bbox.RotateHandleColliderPadding = Vector3.one * 0.016f;
            bbox.ProximityEffectActive = true;

            Debug.Log("Configured boundingbox");

            var manipulator = go.AddComponent<ObjectManipulator>();
            manipulator.AllowFarManipulation = true;


            MinMaxScaleConstraint scaleConstraint = bbox.gameObject.AddComponent<MinMaxScaleConstraint>();
            scaleConstraint.ScaleMaximum = 1.0f;
            scaleConstraint.ScaleMinimum = 0.2f;

            Debug.Log("Setting minmax scaling constraint");

            // to prevent accidental taps to spawn a new object
            go.AddComponent<TapToRemoveListener>();

            Debug.Log("Added hold tap to remove listener");

            Vector3 playerPos = Camera.main.transform.position;
            Vector3 playerDirection = Camera.main.transform.forward;
            Quaternion playerRotation = Camera.main.transform.rotation;
            go.transform.position = playerPos + playerDirection * 0.8f;
            go.transform.rotation = playerRotation;
            Debug.Log("local pos: " + go.transform.position);
            
            go.AddComponent<CloseSpeechListener>();
        }
    }
}
