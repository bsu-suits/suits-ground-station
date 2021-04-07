using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI; 

public class InputManager2D : MonoBehaviour
{

    public static InputManager2D s;
    
    public Text[] buttons;

    public UnityEvent[] buttonActions;

    public GameObject arrowPrefab;
    public GameObject circlePrefab;
    public GameObject rectanglePrefab;
    public GameObject xPrefab;

    public GameObject tempObject = null;
    public GameObject cursorObject = null;

    private Vector3 placementLocation = Vector3.zero;
    private Quaternion placementRotation = Quaternion.identity;
    private bool isMouseDown = false;

    public bool isDrawing = false;
    public bool isPlacingIcons = false; 

    private int currentOptionSelected; 

    private void Start()
    {
        s = this;
        currentOptionSelected = 0;
        buttons[currentOptionSelected].color = Color.red;
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
    }

    public void setSelectedOption(int option)
    {
        currentOptionSelected = option;
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].color = Color.black;
            buttons[i].fontStyle = FontStyle.Normal;
        }
        buttons[option].color = Color.red;
        buttons[option].fontStyle = FontStyle.Bold;
    }

    public void arrowMode()
    {
        isDrawing = false;
        isPlacingIcons = true;
    }

    public void circleMode()
    {
        isDrawing = false;
        isPlacingIcons = true;
    }

    public void xMode()
    {
        isDrawing = false;
        isPlacingIcons = true;
    }

    public void rectangleMode()
    {
        isDrawing = false;
        isPlacingIcons = true;       
    }

    public void drawMode()
    {
        isDrawing = true;
        isPlacingIcons = false;
    }

    public void textBoxMode()
    {        
        isDrawing = false;
        isPlacingIcons = false;
        //TODO
    }

    
    private void FixedUpdate()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit))
        {
            cursorObject.transform.position = hit.point;
        }
        else
        {
            return;
        }

        if(EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (Input.GetMouseButton(0))
        {
        
        }
        if (isPlacingIcons)
        {
            //the Input.GetMouseButtonDown/Input.GetMouseButtonUp were being funky. so wrote my own togglyness.
            //set the location on mouse down
            if(!isMouseDown && Input.GetMouseButton(0))
            {
                isMouseDown = true;
                placementLocation = cursorObject.transform.position;
            }
            //set the rotation on mouse up
            if(isMouseDown &&!Input.GetMouseButton(0))
            {
                isMouseDown = false;
                //cursorObject.transform.LookAt(placementLocation,Vector3.up);//Kinda gotta do this the hard way it turns out
                placementRotation.eulerAngles = new Vector3(90.0f, 0, 0);
                //placementRotation = cursorObject.transform.rotation;
                Vector3 locationDif = placementLocation - cursorObject.transform.position;
                Vector2 topDownVec = new Vector2(locationDif.x, locationDif.z);
                float angle = Vector2.SignedAngle(new Vector2(0, 1.0f), topDownVec);
                placementRotation = placementRotation * Quaternion.Euler(new Vector3(0, 0, angle+90));//Spaghetti math ftw!
                
                
                
                

              switch(currentOptionSelected)
                {
                    case (3):
                        PhotonRPCLinks.getSingleton().sendIconSpawn(placementLocation, placementRotation, PhotonRPCLinks.iconType.ARROW);
                        break;
                    case (4):
                        PhotonRPCLinks.getSingleton().sendIconSpawn(placementLocation, placementRotation, PhotonRPCLinks.iconType.CIRCLE);
                        break;
                    case (5):
                        PhotonRPCLinks.getSingleton().sendIconSpawn(placementLocation, placementRotation, PhotonRPCLinks.iconType.RECTANGLE);
                        break;
                    case (6):
                        PhotonRPCLinks.getSingleton().sendIconSpawn(placementLocation, placementRotation, PhotonRPCLinks.iconType.X);
                        break;
                    default:
                        Debug.LogError("Not sure how this happenened, but good job.");
                        break;
                }

                //axe the temp object
                Destroy(tempObject);
                tempObject = null;
            }

            if(isMouseDown && Input.GetMouseButton(0) && tempObject==null)
            {
                switch (currentOptionSelected)
                {
                    case (3):
                        tempObject = GameObject.Instantiate(arrowPrefab, placementLocation, placementRotation, this.transform);
                        break;
                    case (4):
                        tempObject = GameObject.Instantiate(circlePrefab, placementLocation, placementRotation, this.transform);
                        break;
                    case (5):
                        tempObject = GameObject.Instantiate(rectanglePrefab, placementLocation, placementRotation, this.transform);
                        break;
                    case (6):
                        tempObject = GameObject.Instantiate(xPrefab, placementLocation, placementRotation, this.transform);
                        break;
                    default:
                        Debug.LogError("Not sure how this happenened, but good job.");
                        break;
                }
            }
            if (isMouseDown && Input.GetMouseButton(0) && tempObject)
            {
                placementRotation.eulerAngles = new Vector3(90.0f, 0, 0);
                Vector3 locationDif = placementLocation - cursorObject.transform.position;
                Vector2 topDownVec = new Vector2(locationDif.x, locationDif.z);
                float angle = Vector2.SignedAngle(new Vector2(0, 1.0f), topDownVec);
                placementRotation = placementRotation * Quaternion.Euler(new Vector3(0, 0, angle + 90));//Spaghetti math ftw!
                tempObject.transform.position = placementLocation;
                tempObject.transform.rotation = placementRotation;
            }

            /*
            GameObject child = cursorObject.gameObject;
            GameObject newIcon = Instantiate(child);
            newIcon.transform.position = child.transform.position;
            newIcon.transform.rotation = child.transform.rotation;
            newIcon.transform.localScale = new Vector3(1, 1, 1);
            newIcon.transform.parent = null;
            */
        }
            //old vr method
            /*
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
            */
        }

    public void attachIconToHand(GameObject icon)
    {
        //GameObject newIcon = Instantiate(icon);
        //newIcon.transform.SetParent(cursorObject.transform, true);
        //newIcon.transform.localPosition = new Vector3(0, 0, 0);

        /*
        rightHand.GetComponent<Renderer>().enabled = false; 
        GameObject newIcon = Instantiate(icon);
        foreach (Transform childTransform in rightHand.transform)
        {
            GameObject child = childTransform.gameObject;
            Destroy(child); 
        }
        newIcon.transform.SetParent(rightHand, true);
        newIcon.transform.localPosition = new Vector3(0, 0, 0);
        */
    }

    
}
