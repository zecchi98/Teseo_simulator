using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Beacon : MonoBehaviour
{
    public enum BeaconPowers
    {
        Power10 = 1,
        Power9,
        Power8,
        Power7,
        Power6,
        Power5,
        Power4,
        Power3,
        Power2,
        Power1
    };
    [Header("Beacon simulated power profile (from 10 to 1)")]
    public BeaconPowers power;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
