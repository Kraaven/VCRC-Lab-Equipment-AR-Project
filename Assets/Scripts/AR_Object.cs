using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class AR_Object : MonoBehaviour
{
    [SerializeField] private Canvas MainUI;
    //[SerializeField] private Button InfoButton;
    [SerializeField] private TMP_Text TitleName;
    [SerializeField] private GameObject[] informationObjects;
    private bool DataVisibilityToggle;
    

    public void Start()
    {
        foreach (var informationObject in informationObjects)
        {
            informationObject.SetActive(false);
        }
        
        //MainUI.rootCanvas.worldCamera = Camera.main;
        MainUI.worldCamera = Camera.main;
    }

    public void ToggleButtons()
    {
        DataVisibilityToggle = !DataVisibilityToggle;

        foreach (var informationObject in informationObjects)
        {
            informationObject.SetActive(DataVisibilityToggle);
        }
    }

    public void Update()
    {
        MainUI.transform.LookAt(Camera.main.transform.position, Vector3.up);
        MainUI.transform.rotation = Quaternion.Euler(0,MainUI.transform.rotation.eulerAngles.y+180,0);
    }
}