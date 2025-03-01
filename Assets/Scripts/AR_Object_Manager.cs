using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class AR_Object_Manager : MonoBehaviour
{
    [SerializeField] private List<GameObject> AR_Objects = new List<GameObject>();
    private Dictionary<string, GameObject> RegisteredGameObjects = new Dictionary<string, GameObject>();
    private ARTrackedImageManager _imageManager;
    private GameObject AR_ObjectPool;

    private void Awake()
    {
        _imageManager = GetComponent<ARTrackedImageManager>();
        if(_imageManager == null) return;
        _imageManager.trackablesChanged.AddListener(AR_Event);
    }

    private void Start()
    {
        AR_ObjectPool = new GameObject("AR_Objects");
        foreach (var arObject in AR_Objects)
        {
            var newobj = Instantiate(arObject, AR_ObjectPool.transform);
            newobj.name = arObject.name;
            newobj.transform.localPosition = Vector3.zero;
            newobj.SetActive(false);
            RegisteredGameObjects.Add(arObject.name,newobj);
            
        }
    }

    private void AR_Event(ARTrackablesChangedEventArgs<ARTrackedImage> AR_Args)
    {
        foreach (var AR_Object in AR_Args.added)
        {
            RegisteredGameObjects[AR_Object.name].SetActive(true);
        }

        foreach (var AR_Object in AR_Args.updated)
        {
            RegisteredGameObjects[AR_Object.name].transform.position = AR_Object.transform.position;
        }

        foreach (var AR_Object in AR_Args.removed)
        {
            RegisteredGameObjects[AR_Object.Value.name].SetActive(false);
        }
    }
}
