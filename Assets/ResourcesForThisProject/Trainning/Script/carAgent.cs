using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.Windows;
using Input = UnityEngine.Input;
using System.Numerics;
using Vector3 = UnityEngine.Vector3;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;

public enum InputType
{ 
    Continuous = 0,
    Discrete = 1
};

public class carAgent : Agent
{
    [SerializeField] private Transform StartPoint;
    
    [SerializeField] private DataLoader carData;

    //private Transform CurrentPos, NextPos;
    
    private VehicleControl carControl;
    
    private Rigidbody carRg;
   
    public CheckPointTracker tracker;

    public int CollisionCount;

    public float CollisionStayTime;

    public float StayTime;

    public int StayCounts;

    public float EndEpisodeTime = 10f;

    public float resetTime = 5f;

    public InputType carInputType;

    public void Start()
    {
        StayTime = StayCounts = 0;
        tracker.OnPlayerPassCorrectPoint += carAgent_OnPlayerThroughCorrectPoint;
        tracker.OnPlayerPassWrongPoint += carAgent_OnPlayerThroughWrongPoint;
        carRg = GetComponent<Rigidbody>();
        carControl = GetComponent<VehicleControl>();
        if (carControl != null)
            carControl.activeControl = true;
    }

    public override void OnEpisodeBegin()
    {
        StayCounts = 0;
        CollisionCount = 0;
        CollisionStayTime = 0f;

        transform.position = StartPoint.position;
        transform.rotation = StartPoint.rotation; 
        
        tracker.ResetNextPoint(this);
        tracker.ResetBonus(this);
        tracker.ResetEnding(this);

        carControl.controlMode = ControlMode.train;
        carRg.velocity = Vector3.zero;
        carData.accel = 0f;
        carData.steer = 0f;
        //CurrentPos = StartPoint;
    }
    //set inputs of netural network,which must be normailzed!!!
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.position.normalized);
        sensor.AddObservation(carRg.velocity.normalized);
        Transform NextCheckPointPos = tracker.GetNextCheckPoint(this).transform;
        Vector3 NextCheckPointFwd = NextCheckPointPos.right.normalized;
        sensor.AddObservation(Vector3.Dot(transform.forward.normalized,NextCheckPointFwd));
    }
    //code for get input when debugging with developer operation
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        if(carInputType == InputType.Continuous)
        {
            ActionSegment<float> ContinuteActions = actionsOut.ContinuousActions;
            ContinuteActions[0] = Input.GetAxis("Vertical");
            ContinuteActions[1] = Input.GetAxis("Horizontal");
        }
        else if (carInputType == InputType.Discrete)
        {
            int forwardAction = 0;
            if (Input.GetAxis("Vertical") > 0) forwardAction = 1;
            if (Input.GetAxis("Vertical") < 0) forwardAction = 2;

            int turnAction = 0;
            if (Input.GetAxis("Horizontal") > 0) turnAction = 1;
            if (Input.GetAxis("Horizontal") < 0) turnAction = 2;
            ActionSegment<int> discreteActions = actionsOut.DiscreteActions;
            discreteActions[0] = forwardAction;
            discreteActions[1] = turnAction;
        }
    }

    public void carAgent_OnPlayerThroughCorrectPoint(object sender, EventArgs e)
    {
        StayTime = 0f;
        AddReward(+5.0f);
        float Bonus = tracker.GetTurningBonus(this);
        if(Bonus > 1f)
        {
            AddReward(+5.0f * Bonus);
        }
    }

    public void carAgent_OnPlayerThroughWrongPoint(object sender, EventArgs e)
    {
        AddReward(-5.0f);
    }
    public void CheckStayTime()
    {
        if (StayTime > resetTime)
        {
            StayCounts++;
            AddReward(-0.5f * StayTime);
            //reset position and rotation to last checkpoint
            CheckPoint point = tracker.GetCurrentCheckPoint(this);
            if (point)
            {
                float rd = Random.Range(0,3);
                Quaternion CheckpointRot = point.transform.rotation;
                if (rd < 1.0f)
                {
                    transform.position = point.transform.position;                    
                    transform.rotation = CheckpointRot * Quaternion.Euler(0, 90, 0);
                }
                else if(point.transform.childCount > 0)
                {
                    if(rd > 2.0f)
                    {
                        transform.position = point.transform.GetChild(0).transform.position;
                        transform.rotation = CheckpointRot * Quaternion.Euler(0, 90, 0);
                    }
                    else if(rd > 1.0f && rd < 2.0f)
                    {
                        transform.position = point.transform.GetChild(1).transform.position;
                        transform.rotation = CheckpointRot * Quaternion.Euler(0, 90, 0);
                    }
                }
            }
            else
            {
                transform.position = StartPoint.position;
                transform.rotation = StartPoint.rotation;
            }
            StayTime = 0f;
        }
    }
    //aquiring outputs of the netrual network and set to the car moving input
    //excute every steps
    public override void OnActionReceived(ActionBuffers actions)
    {
        float forwardAmount = 0f;
        float turnAmount = 0f;

        if(carInputType == InputType.Continuous)
        {
            forwardAmount = actions.ContinuousActions[0];
            turnAmount = actions.ContinuousActions[1];
        }
        else if (carInputType == InputType.Discrete)
        {
            switch (actions.DiscreteActions[0])
            {
                case 0: forwardAmount = 0; break;
                case 1: forwardAmount += 1f; break;
                case 2: forwardAmount -= 1f; break;

            }
            switch (actions.DiscreteActions[1])
            {
                case 0: turnAmount = 0; break;
                case 1: turnAmount += 1f; break;
                case 2: turnAmount -= 1f; break;
            }
        }
        
        carData.accel = forwardAmount;
        carData.steer = turnAmount;
        //use dot product to get positve reward while move forward
        float FwdSpeed = Vector3.Dot(transform.forward, carRg.velocity);
        AddReward(forwardAmount * FwdSpeed * .05f);

        //if (StayCounts > EndEpisodeTime)
        //{
        //    AddReward(-1.0f);
        //    EndEpisode();
        //}
        if (tracker.isEnd[this])
        {
            AddReward(+50.0f);
            EndEpisode();
        }
        CheckStayTime();
    }
   
    public void OnCollisionEnter(Collision other)
    {       
        AddReward(-1.0f);     
        CollisionCount++;      
    }

    public void OnCollisionStay(Collision collision)
    {
        CollisionStayTime += Time.deltaTime;       
        AddReward(CollisionStayTime * .1f);       
    }

    public void OnCollisionExit(Collision collision)
    {
        CollisionStayTime = 0f;
    }

    public void Update()
    {      
        StayTime += Time.deltaTime;              
    }

}
