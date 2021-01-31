using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingleBody
{
    // Start is called before the first frame update
    public string name;
    public int objId;
    public Vector3 position, linVel, angVel;
    public Vector4 quat;

    public SingleBody()
    {
        name = "Single Body";
        objId = -1;
        position = new Vector3(0, 0, 0);
        linVel = new Vector3(0, 0, 0);
        angVel = new Vector3(0, 0, 0);
        quat = new Vector4(1, 0, 0, 0);
    }
}
