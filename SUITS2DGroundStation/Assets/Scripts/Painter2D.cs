using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.EventSystems;

public class Painter2D : MonoBehaviour {

    public GameObject m_LineRendererPrefab = null;

    public Transform m_RightHand;
    public Transform m_DrawObject;

    public float m_DrawTick = 0.10f; //time between each draw call
    public Material m_HandColorPickerMat;
    public UnityEngine.Color[] m_ColorSelection = { UnityEngine.Color.red, UnityEngine.Color.green, UnityEngine.Color.blue, UnityEngine.Color.white };
    public float[] m_SizeSelection = { .1f, .05f, .01f };

    private float m_LineWidth;

    private int m_ColorIndex;
    private int m_SizeIndex;

    private bool m_SizeRecentlyChanged;

    private LineRenderer m_LineRenderer = null;
    private LineRenderer m_PreviousLineRenderer; //needed for undoing 
    private Stack<LineRenderer> m_DrawingStack;
    private float m_PaintingTime;

    // Use this for initialization
    void Start ()
    {
        m_LineRenderer = null;
        m_DrawingStack = new Stack<LineRenderer>();

        m_LineWidth = m_SizeSelection[0];
        m_HandColorPickerMat.color = m_ColorSelection[0];
        selectNextColor();
        selectNextSize();
    }
	
	// Update is called once per frame
	void Update ()
    {
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }
        if (!InputManager2D.s.isDrawing) return; 
        HandleColoring();
        HandleSizing();
        HandlePainting();
	}

    private void DrawPoint(Vector3 point)
    {
        m_LineRenderer.positionCount++; //increase number of points by one
        m_LineRenderer.SetPosition(m_LineRenderer.positionCount - 1, point);
        m_LineRenderer.startWidth = m_LineWidth;
        m_LineRenderer.endWidth = m_LineWidth;
        m_LineRenderer.material.color = m_HandColorPickerMat.color;
    }

    private void HandlePainting()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit))
        {
            m_DrawObject.transform.position = hit.point;
        }
        else
        {
            return;
        }
        if (Input.GetMouseButton(0))
        {
           

                if (m_LineRenderer == null)
                {
                    GameObject g = Instantiate(m_LineRendererPrefab,this.transform);
                    m_LineRenderer = g.GetComponent<LineRenderer>();
                    //Debug.Log("Line renderer instantiated");
                    return;
                }
                if (Time.time - m_PaintingTime >= m_DrawTick)
                {
                    m_PaintingTime = Time.time;
                    DrawPoint(hit.point);
                    //Debug.Log("Point drawn");
                }
            
        }
        else if (Input.GetMouseButtonUp(0))
        {
            m_DrawingStack.Push(m_LineRenderer);
            if(m_LineRenderer)
                PhotonRPCLinks.getSingleton().SendLineRenderer(m_LineRenderer);
            //Debug.Log("Line renderer Sent");
            m_LineRenderer = null;
        }
        //old vr method
        /*
        if (OVRInput.Get(OVRInput.Button.SecondaryHandTrigger))
        {
            if (m_LineRenderer == null)
            {
                GameObject g = Instantiate(m_LineRendererPrefab, transform);
                m_LineRenderer = g.GetComponent<LineRenderer>();
                return;
            }

            if (Time.time - m_PaintingTime >= m_DrawTick)
            {
                m_PaintingTime = Time.time;
                DrawPoint();
            }
        }

        if (OVRInput.GetUp(OVRInput.Button.SecondaryHandTrigger))
        {
            m_DrawingStack.Push(m_LineRenderer);
            //NetworkController.networkControllerSingleton.SendLineRenderer(m_LineRenderer);
            PhotonRPCLinks.getSingleton().SendLineRenderer(m_LineRenderer);
            m_LineRenderer = null;
        }

        if (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger))
        {
            UndoDrawing();
            //NetworkController.networkControllerSingleton.SendLineUndoRenderer();
            PhotonRPCLinks.getSingleton().SendLineRendererUndo();
        }
        */
    }
    public void selectNextSize()
    {
        m_SizeIndex = Mathf.FloorToInt(Mathf.Repeat((float)m_SizeIndex - 1.0f, m_SizeSelection.Length)); //repeat is exclusive with length
        m_LineWidth = m_SizeSelection[m_SizeIndex];
        m_DrawObject.localScale = new Vector3(m_LineWidth, m_LineWidth, m_LineWidth);
    }
    private void HandleSizing()
    {
        //old vr method
        /*
        Vector2 thumbstickInput = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
        if (thumbstickInput.y <= -0.8f && m_SizeRecentlyChanged == false)
        {
            //thumbstick pressed to the right
            m_SizeIndex = Mathf.FloorToInt(Mathf.Repeat((float)m_SizeIndex + 1.0f, m_SizeSelection.Length));
            m_LineWidth = m_SizeSelection[m_SizeIndex];

            m_DrawObject.localScale = new Vector3(m_LineWidth, m_LineWidth, m_LineWidth);

            m_SizeRecentlyChanged = true;
        }
        else if (thumbstickInput.y >= 0.8f && m_SizeRecentlyChanged == false)
        {
            //thumbstick pressed to the left
            m_SizeIndex = Mathf.FloorToInt(Mathf.Repeat((float)m_SizeIndex - 1.0f, m_SizeSelection.Length)); //repeat is exclusive with length
            m_LineWidth = m_SizeSelection[m_SizeIndex];

            m_DrawObject.localScale = new Vector3(m_LineWidth, m_LineWidth, m_LineWidth);

            m_SizeRecentlyChanged = true;
        }
        else if (thumbstickInput.y >= -0.5f && thumbstickInput.y <= 0.5f)
        {
            //thumbstick not being pressed/reseting thumbstick
            m_SizeRecentlyChanged = false;
        }
        */
    }

    public void selectNextColor()
    {
        m_ColorIndex = Mathf.FloorToInt(Mathf.Repeat((float)m_ColorIndex + 1.0f, m_ColorSelection.Length));
        m_HandColorPickerMat.color = m_ColorSelection[m_ColorIndex];
    }
    private void HandleColoring()
    {
        //old vr method
        /*
        if (OVRInput.GetDown(OVRInput.Button.SecondaryThumbstick))
        {
            m_ColorIndex = Mathf.FloorToInt(Mathf.Repeat((float)m_ColorIndex + 1.0f, m_ColorSelection.Length));
            m_HandColorPickerMat.color = m_ColorSelection[m_ColorIndex];
        }
        */
    }

    private void UndoDrawing()
    {
        if (m_DrawingStack.Count > 0)
        {
            Destroy(m_DrawingStack.Pop());
        }
    }
}
