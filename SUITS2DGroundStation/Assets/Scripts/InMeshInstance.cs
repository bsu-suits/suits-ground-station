using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InMeshInstance : MonoBehaviour
{
    public Mesh myMesh = null;
    public MeshFilter myMeshFilter = null;
    public MeshRenderer myMeshRenderer = null;
    public float startTime = 0;

    public void Start()
    {
        startTime = Time.realtimeSinceStartup;
    }

    public void updateRenderedMesh(Mesh newMesh)
    {
        myMeshFilter.mesh = myMesh = newMesh;
    }

    public void FixedUpdate()
    {

        //Color color = myMeshRenderer.material.color;
        //if(color.a>0)
        //    color.a -= 0.0001f;
        //myMeshRenderer.material.color = color;

        if (startTime + 120.0f < Time.realtimeSinceStartup)
        {
            Destroy(this.gameObject);
            Destroy(myMesh);
        }
    }
}
