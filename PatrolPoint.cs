using System;
using UnityEngine;

[Serializable]
public class PatrolPoint
{
    public Transform point;
    public float waitTime = 2f;
    public Transform lookTarget;
    public string animationTrigger;
}
