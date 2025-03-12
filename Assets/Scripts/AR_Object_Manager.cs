using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class AR_Object_Manager : MonoBehaviour
{
    [SerializeField] List<AR_Object> _ARPrefabs;
    private ARTrackedImageManager _trackedImageManager;
    private Dictionary<string, AR_Object> _RegisteredARObjects;
    private Transform AR_ObjectPool;

    private void Awake()
    {
        _trackedImageManager = GetComponent<ARTrackedImageManager>();
        _RegisteredARObjects = new Dictionary<string, AR_Object>();
        AR_ObjectPool = new GameObject("AR Object Pool").transform;
    }

    private void Start()
    {
        _trackedImageManager.trackablesChanged.AddListener(OnTrackedImageChanged);

        foreach (var arPrefab in _ARPrefabs)
        {
            var obj = Instantiate(arPrefab, AR_ObjectPool);
            obj.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            obj.gameObject.SetActive(false);
            _RegisteredARObjects.Add(arPrefab.name, obj);
            
        }
    }

    private void OnDestroy()
    {
        _trackedImageManager.trackablesChanged.RemoveListener(OnTrackedImageChanged);
    }

    private void OnTrackedImageChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        foreach (var AddedImage in eventArgs.added)
        {
            UpdateTrackedImage(AddedImage);
        }
        
        foreach (var UpdatedImage in eventArgs.updated)
        {
            UpdateTrackedImage(UpdatedImage);
        }
        
        foreach (var RemovedImage in eventArgs.removed)
        {
            _RegisteredARObjects[RemovedImage.Value.referenceImage.name].gameObject.SetActive(false);
            UpdateTrackedImage(RemovedImage.Value);
        }
        
    }


    public void UpdateTrackedImage(ARTrackedImage trackedImage)
    {
        var OBJ = _RegisteredARObjects[trackedImage.referenceImage.name];
        if (trackedImage.trackingState is TrackingState.None)
        {
            OBJ.gameObject.SetActive(false);
            return;
        }
        else
        {
            OBJ.gameObject.SetActive(true);
            OBJ.gameObject.transform.SetPositionAndRotation(trackedImage.transform.position, trackedImage.transform.rotation);
        }
        
    }
}