using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.HighDefinition;
using Quaternion = System.Numerics.Quaternion;
using Vector3 = System.Numerics.Vector3;

public class ArticulatedSystem
{
    public float[] gc, gv;
    public string[] jointNames;
    public int[] jointTypes;
    public string name;
    public int objId;
    public int frameSize = 0;
    public int jointSize = 0;
    public UnityEngine.Vector3[] framesPos;
    public UnityEngine.Quaternion[] framesQuat;
    public string[] frameNames;

    private Text _uiName;
    private GameObject _jointAsset;
    private GameObject[] _uiJoint;

    public ArticulatedSystem()
    {
        name = "Articulated System";
        objId = -1;
        var _nameAsset = Resources.Load<Text> ("menu_prefab/_as_name");
        _jointAsset = Resources.Load<GameObject> ("menu_prefab/_as_joint_p");
        _uiName = Object.Instantiate (_nameAsset, GameObject.Find("_AsDescription").transform);
        _uiName.name = "articulated_system_name";
    }

    public void Reset(int newObjId, int gcDim, int gvDim, int frame_size, int joint_size)
    {
        gc = new float[gcDim];
        gv = new float[gvDim];
        objId = newObjId;
        if (frameSize < frame_size)
        {
            framesPos = new UnityEngine.Vector3[frame_size];
            framesQuat = new UnityEngine.Quaternion[frame_size];
            frameNames = new string[frame_size];
        }
        
        if (jointSize < joint_size)
        {
            jointNames = new string[joint_size];
            jointTypes = new int[joint_size];
            for (int i = 0; i < jointSize; i++)
                GameObject.Destroy(_uiJoint[i]);
            _uiJoint = new GameObject[joint_size];

            UnityEngine.Vector3 uiLocation = _uiName.transform.position;
            uiLocation.y += 20;

            for (int i = 0; i < joint_size; i++)
            {
                uiLocation.y += -45;
                _uiJoint[i] = Object.Instantiate (_jointAsset, GameObject.Find("_AsDescription").transform);
                _uiJoint[i].transform.position = uiLocation;
            }
        }
        frameSize = frame_size;
        jointSize = joint_size;
    }

    public void updateGui()
    {
        _uiName.text = name;
        for (int i = 0; i < jointSize; i++)
        {
            var text = _uiJoint[i].transform.Find("Text").gameObject;
            text.GetComponent<Text>().text = jointNames[i];
        }
    }
}
