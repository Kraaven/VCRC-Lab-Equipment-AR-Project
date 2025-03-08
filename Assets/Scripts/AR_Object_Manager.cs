using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class AR_Object_Manager : MonoBehaviour
{
    [SerializeField] private List<AR_Object> AR_Objects = new List<AR_Object>();
    private Dictionary<string, AR_Object> RegisteredGameObjects = new Dictionary<string, AR_Object>();
    private Dictionary<string, float> LastSeenTime = new Dictionary<string, float>();
    private ARTrackedImageManager imageManager;
    private GameObject arObjectPool;
    
    [Header("Tracking Settings")]
    [Tooltip("Time in seconds before fully deactivating a lost marker")]
    [SerializeField] private float deactivationDelay = 3.0f;
    
    [Tooltip("Allow reactivation of previously seen markers")]
    [SerializeField] private bool allowReactivation = true;

    private void Awake()
    {
        imageManager = GetComponent<ARTrackedImageManager>();
        if (imageManager == null)
        {
            Debug.LogError("ARTrackedImageManager component is missing!");
            return;
        }

        imageManager.trackablesChanged.AddListener(OnARTrackedImagesChanged);
    }
    
    private void OnDestroy()
    {
        if (imageManager != null)
        {
            imageManager.trackablesChanged.RemoveListener(OnARTrackedImagesChanged);
        }
    }

    private void Start()
    {
        arObjectPool = new GameObject("AR_Objects");
        
        foreach (var arObject in AR_Objects)
        {
            if (arObject == null)
            {
                Debug.LogWarning("Null AR object in the list - skipping");
                continue;
            }

            var newObj = Instantiate(arObject, arObjectPool.transform);
            newObj.name = arObject.name;
            newObj.transform.localPosition = Vector3.zero;
            newObj.gameObject.SetActive(false);
            
            // Check for duplicate names
            if (RegisteredGameObjects.ContainsKey(arObject.name))
            {
                Debug.LogWarning($"Duplicate AR object name: {arObject.name}. Only the last one will be used.");
            }
            
            RegisteredGameObjects[arObject.name] = newObj;
            LastSeenTime[arObject.name] = 0f;
        }
    }

    private void Update()
    {
        // Check for objects that need delayed deactivation
        float currentTime = Time.time;
        foreach (var entry in LastSeenTime)
        {
            string imageName = entry.Key;
            float lastSeen = entry.Value;
            
            // Skip objects that are already inactive or have never been seen
            if (!RegisteredGameObjects[imageName].gameObject.activeSelf || lastSeen == 0f)
                continue;
                
            // If we haven't seen this marker in a while, deactivate it
            if (currentTime - lastSeen > deactivationDelay)
            {
                UnactivateObject(imageName);
            }
        }
    }

    private void OnARTrackedImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        // Handle added images
        foreach (var trackedImage in eventArgs.added)
        {
            string imageName = trackedImage.referenceImage.name;
            
            if (RegisteredGameObjects.TryGetValue(imageName, out AR_Object arObject))
            {
                arObject.gameObject.SetActive(true);
                UpdateARObject(trackedImage);
                LastSeenTime[imageName] = Time.time;
            }
        }

        // Handle updated images
        foreach (var trackedImage in eventArgs.updated)
        {
            string imageName = trackedImage.referenceImage.name;
            
            // Update the last seen time for this marker
            LastSeenTime[imageName] = Time.time;
            
            // If the object is inactive but we allow reactivation, reactivate it
            if (allowReactivation && !RegisteredGameObjects[imageName].gameObject.activeSelf)
            {
                if (trackedImage.trackingState == TrackingState.Tracking)
                {
                    RegisteredGameObjects[imageName].gameObject.SetActive(true);
                }
            }
            
            UpdateARObject(trackedImage);
        }

        // Handle removed images - we now use our delayed deactivation, 
        // so we don't immediately respond to removal events
        foreach (var trackedImage in eventArgs.removed)
        {
            // We'll handle actual deactivation in the Update method after the delay
        }
    }

    private void UpdateARObject(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;
        var arObject = RegisteredGameObjects[imageName];
        
        // Update the AR object with the new tracking information
        switch (trackedImage.trackingState)
        {
            case TrackingState.Tracking:
                // Pass tracking information to the AR object
                arObject.UpdateTransform(
                    trackedImage.transform.position,
                    trackedImage.transform.rotation,
                    "Tracking");
                break;
                
            case TrackingState.Limited:
                if (arObject.AddError(Time.deltaTime))
                {
                    // Too many errors, deactivate
                    UnactivateObject(imageName);
                }
                else
                {
                    // Limited tracking but still usable
                    arObject.UpdateTransform(
                        trackedImage.transform.position,
                        trackedImage.transform.rotation,
                        "Limited");
                }
                break;
                
            case TrackingState.None:
                if (arObject.AddKillStrike(Time.deltaTime))
                {
                    // Lost tracking completely
                    UnactivateObject(imageName);
                }
                else
                {
                    // Notify the object of tracking loss
                    arObject.UpdateTransform(
                        trackedImage.transform.position,
                        trackedImage.transform.rotation,
                        "None");
                }
                break;
        }
    }
    
    public void UnactivateObject(string objectName)
    {
        if (RegisteredGameObjects.TryGetValue(objectName, out AR_Object arObject))
        {
            arObject.RidError();
            arObject.gameObject.SetActive(false);
            
            // Optionally store the last good transform for when it reactivates
            // This happens inside the AR_Object now
        }
    }
    
    public void ReactivateObject(string objectName)
    {
        if (RegisteredGameObjects.TryGetValue(objectName, out AR_Object arObject))
        {
            arObject.RidError();
            arObject.gameObject.SetActive(true);
        }
    }
}