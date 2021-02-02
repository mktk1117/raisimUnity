using System;
using System.Collections;
using System.Collections.Generic;
using raisimUnity;
using UnityEngine;
using UnityEngine.UI;

public class SingleBody
{
    // Start is called before the first frame update
    public string name;
    public int objId;
    public UnityEngine.Vector3 position, linVel, angVel;
    public UnityEngine.Quaternion quat;
    private MeshPool _coordinateFrame;
    private GameObject _framRoot;
    private GameObject nameui, pos_x, pos_y, pos_z, rot_x, rot_y, rot_z, vel_x, vel_y, vel_z, ang_x, ang_y, ang_z;
    
    public SingleBody(Shader standardShader)
    {
        _framRoot = new GameObject();
        var cofr = Resources.Load<GameObject> ("CoordinateFrame");
        _coordinateFrame = new MeshPool("articulated_system_frames", cofr, _framRoot, VisualTag.Both, standardShader);
        name = "Single Body";
        objId = -1;
        position = new Vector3(0, 0, 0);
        linVel = new Vector3(0, 0, 0);
        angVel = new Vector3(0, 0, 0);
        quat = new Quaternion(0, 0, 0, 1);

        pos_x = GameObject.Find("sb_pos_x");
        pos_y = GameObject.Find("sb_pos_y");
        pos_z = GameObject.Find("sb_pos_z");
        
        rot_x = GameObject.Find("sb_rot_x");
        rot_y = GameObject.Find("sb_rot_y");
        rot_z = GameObject.Find("sb_rot_z");
        
        vel_x = GameObject.Find("sb_vel_x");
        vel_y = GameObject.Find("sb_vel_y");
        vel_z = GameObject.Find("sb_vel_z");
        
        ang_x = GameObject.Find("sb_ang_x");
        ang_y = GameObject.Find("sb_ang_y");
        ang_z = GameObject.Find("sb_ang_z");

        nameui = GameObject.Find("sb_name");
    }

    public void UpdateGui()
    {
        UnityEngine.Vector3 axis;
        float angle;
        quat.ToAngleAxis(out angle, out axis);
        axis *= angle;
        nameui.GetComponent<Text>().text = name;
        
        pos_x.GetComponent<Text>().text = "Pos_x:    " + position[0].ToString("F4");
        pos_y.GetComponent<Text>().text = "Pos_y:    " + position[1].ToString("F4");
        pos_z.GetComponent<Text>().text = "Pos_z:    " + position[2].ToString("F4");
        
        rot_x.GetComponent<Text>().text = "Rot_x:    " + axis[0].ToString("F4");
        rot_y.GetComponent<Text>().text = "Rot_y:    " + axis[1].ToString("F4");
        rot_z.GetComponent<Text>().text = "Rot_z:    " + axis[2].ToString("F4");
        
        vel_x.GetComponent<Text>().text = "LinVel_x: " + linVel[0].ToString("F4");
        vel_y.GetComponent<Text>().text = "LinVel_y: " + linVel[1].ToString("F4");
        vel_z.GetComponent<Text>().text = "LinVel_z: " + linVel[2].ToString("F4");
        
        ang_x.GetComponent<Text>().text = "AngVel_x: " + angVel[0].ToString("F4");
        ang_y.GetComponent<Text>().text = "AngVel_y: " + angVel[1].ToString("F4");
        ang_z.GetComponent<Text>().text = "AngVel_z: " + angVel[2].ToString("F4");
    }
    
    public void Clear()
    {
        _coordinateFrame.Clear();
    }
}
