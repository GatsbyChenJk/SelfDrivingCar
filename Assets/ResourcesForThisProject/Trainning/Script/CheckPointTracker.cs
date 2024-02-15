using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public class CheckPointTracker : MonoBehaviour
{
    public int[] nextPoint;
    //data structure for trainning mutliple agents
    public Dictionary<carAgent,bool> isEnd;
    public Dictionary<carAgent, int> PointForAgent;
    public Dictionary<carAgent, float> BonusForAgent;

    public List<CheckPoint> CheckPoints;
    public carAgent[] Agents;

    //called when agent through any checkpoint
    public event EventHandler OnPlayerPassCorrectPoint;
    public event EventHandler OnPlayerPassWrongPoint;

    public void OnEnable()
    {
        Agents = FindObjectsOfType<carAgent>();
        nextPoint = new int[Agents.Length];
        //remember to initialize
        isEnd = new Dictionary<carAgent, bool>();
        PointForAgent = new Dictionary<carAgent, int>();
        BonusForAgent = new Dictionary<carAgent, float>();

        for (int i = 0; i < Agents.Length; i++)
        {     
            nextPoint[i] = 0;
            isEnd.Add(Agents[i], false);
            PointForAgent.Add(Agents[i], 0);
            BonusForAgent.Add(Agents[i], 0);
        }
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).gameObject.activeSelf)
                CheckPoints.Add(transform.GetChild(i).GetComponent<CheckPoint>());
            else
                break;
        }     
        foreach(CheckPoint point in CheckPoints)
        {
            point.CheckPointIndex = CheckPoints.IndexOf(point);           
        }
    }
    
    public float CalculateBonus(CheckPoint passedPoint, CheckPoint nextPoint)       
    {
        float deltaRot = 0f;
        if(passedPoint != null && nextPoint != null)
        deltaRot = passedPoint.transform.rotation.eulerAngles.y - nextPoint.transform.rotation.eulerAngles.y;
        return Mathf.Abs(deltaRot);
    }
    //callback function,callback to renew next checkpoint data for each agent while it through the check point correctly
    public void OnPlayerThroughPoint(CheckPoint ThroughPoint,carAgent Agent)
    {
        if (PointForAgent[Agent] == CheckPoints.IndexOf(ThroughPoint))
        {
            isEnd[Agent] = (PointForAgent[Agent] + 1) == CheckPoints.Count;
            PointForAgent[Agent] = (PointForAgent[Agent] + 1) % CheckPoints.Count;
            BonusForAgent[Agent] = CalculateBonus(CheckPoints[PointForAgent[Agent]], ThroughPoint);
            OnPlayerPassCorrectPoint?.Invoke(this,EventArgs.Empty);
        }
       else
        {
            OnPlayerPassWrongPoint?.Invoke(this,EventArgs.Empty);
        }
    }

    public void Update()
    {
        for (int i = 0; i < Agents.Length; i++)
            nextPoint[i] = PointForAgent[Agents[i]];
    }

    public CheckPoint GetNextCheckPoint(carAgent Agent)
    {
        return CheckPoints[PointForAgent[Agent]];
    }

    public CheckPoint GetCurrentCheckPoint(carAgent Agent)
    {
        if (PointForAgent[Agent] == 0)
            return null;
        return CheckPoints[PointForAgent[Agent] - 1];
    }

    public float GetTurningBonus(carAgent Agent)
    {
        return BonusForAgent[Agent];
    }

    public void ResetNextPoint(carAgent Agent)
    {
        PointForAgent[Agent] = 0;
    }

    public void ResetBonus(carAgent Agent)
    {
        BonusForAgent[Agent] = 0;
    }

    public void ResetEnding(carAgent Agent)
    {
        isEnd[Agent] = false;
    }

}
