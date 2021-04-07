using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI; 

public class InputManager : MonoBehaviour
{
    public static InputManager s;

    public Text[] buttons;

    public UnityEvent[] buttonActions;

    public GameObject arrowPrefab;
    public GameObject circlePrefab;
    public GameObject rectanglePrefab;
    public GameObject xPrefab;

    public Transform rightHand;

    public bool isDrawing = false;
    public bool isPlacingIcons = false; 

    private int currentOptionSelected; 

    private void Start()
    {
        s = this;
        currentOptionSelected = 0;
        buttons[currentOptionSelected].color = Color.red;
        buttonActions[currentOptionSelected].Invoke();
    }

    private void toggleOption()
    {
        buttons[currentOptionSelected].color = Color.black;
        buttons[currentOptionSelected].fontStyle = FontStyle.Normal; 
        if (currentOptionSelected >= buttons.Length - 1)
        {
            currentOptionSelected = 0; 
        } else
        {
            currentOptionSelected++; 
        }
        buttons[currentOptionSelected].color = Color.red;
        buttons[currentOptionSelected].fontStyle = FontStyle.Bold;
        buttonActions[currentOptionSelected].Invoke(); 
    }

    private void Update()
    {
        if (OVRInput.GetUp(OVRInput.Button.Two) || Input.GetKeyUp(KeyCode.DownArrow)) 
        {
            toggleOption();
        }

        if (isPlacingIcons)
        {
            if (OVRInput.GetUp(OVRInput.Button.SecondaryHandTrigger) || Input.GetKeyUp(KeyCode.Space))
            {
                foreach (Transform childTransform in rightHand.transform)
                {
                    GameObject child = childTransform.gameObject;
                    GameObject newIcon = Instantiate(child);
                    newIcon.transform.position = child.transform.position;
                    newIcon.transform.rotation = child.transform.rotation;
                    newIcon.transform.localScale = new Vector3(1,1,1); 
                    newIcon.transform.parent = null; 
                }
            }
        }
    }

    public void attachIconToHand(GameObject icon)
    {
        rightHand.GetComponent<Renderer>().enabled = false; 
        GameObject newIcon = Instantiate(icon);
        foreach (Transform childTransform in rightHand.transform)
        {
            GameObject child = childTransform.gameObject;
            Destroy(child); 
        }
        newIcon.transform.SetParent(rightHand, true);
        newIcon.transform.localPosition = new Vector3(0, 0, 0);
    }

    public void arrowMode()
    {
        isDrawing = false;
        isPlacingIcons = true; 
        attachIconToHand(arrowPrefab); 
    }

    public void circleMode()
    {
        isDrawing = false;
        isPlacingIcons = true; 
        attachIconToHand(circlePrefab); 
    }

    public void xMode()
    {
        isDrawing = false;
        isPlacingIcons = true; 
        attachIconToHand(xPrefab); 
    }

    public void rectangleMode()
    {
        isDrawing = false;
        isPlacingIcons = true; 
        attachIconToHand(rectanglePrefab); 
    }

    public void drawMode()
    {
        rightHand.GetComponent<Renderer>().enabled = true;
        isDrawing = true;
        isPlacingIcons = false; 
    }

    public void textBoxMode()
    {
        rightHand.GetComponent<Renderer>().enabled = true;
        isDrawing = false;
        isPlacingIcons = false; 
        //TODO
    }
}
