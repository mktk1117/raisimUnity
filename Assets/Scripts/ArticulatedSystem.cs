using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class ArticulatedSystem
{
    public float[] gc, gv;
    public string name;
    public int objId;
    public List<Transform> frames;
    public List<string> frameNames;
    public List<int> frameType;

    public ArticulatedSystem()
    {
        gc = new float[1];
        gv = new float[1];
        name = "Articulated System";
        objId = -1;
        frames = new List<Transform>(100);
        frameNames = new List<string>(100);
        frameType = new List<int>(100);
    }

    public void ResetIfDifferent(int newObjId, int gcDim, int gvDim)
    {
        if (objId == newObjId)
        {
            return;
        }
        
        gc = new float[gcDim];
        gv = new float[gvDim];
        objId = newObjId;
        frames.Clear();
        frameNames.Clear();
        frameType.Clear();
    }
}
