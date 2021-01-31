using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

public struct ArticulatedSystem
{
    public float[] gc, gv;
    public string name;
    public List<Transform> frames;
    public List<string> frameNames;
}
