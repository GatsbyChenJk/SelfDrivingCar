using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckPoint : MonoBehaviour
{
    public CheckPointTracker tracker;

    public int CheckPointIndex = -1;
 

    public void OnEnable()
    {               
        tracker = FindObjectOfType<CheckPointTracker>();        
    }

    public void OnTriggerEnter(Collider other)
    {   
       
        if(other.TryGetComponent<MeshCollider>(out MeshCollider col))
        {
            carAgent Agt = col.GetComponentInParent<carAgent>();
            tracker.OnPlayerThroughPoint(this, Agt);            
        }
    }

    

}
