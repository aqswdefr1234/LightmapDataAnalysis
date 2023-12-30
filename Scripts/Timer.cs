using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;
using System;

//This script is for testing purposes only.
public class Timer : MonoBehaviour
{
    private static double startTime;
    private static double endTime;
    
    public static void TimerStart()
    {
        DateTime now = DateTime.Now;
        startTime = (now - DateTime.MinValue).TotalSeconds;
        Debug.Log("Start Timer");
    }
    public static void TimerEnd()
    {
        DateTime now = DateTime.Now;
        endTime = (now - DateTime.MinValue).TotalSeconds;
        Debug.Log($"Timer : {endTime - startTime}");
        Debug.Log("End Timer");
    }
    
}
