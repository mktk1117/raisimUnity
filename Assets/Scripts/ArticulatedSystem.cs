using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using raisimUnity;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;
using Quaternion = System.Numerics.Quaternion;
using Toggle = UnityEngine.UI.Toggle;
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
    private GameObject _frameAsset;
    private GameObject[] _uiJoint;
    private GameObject[] _uiFrame;

    private MeshPool _coordinateFrame;
    public GameObject _framRoot;
    private int _guiType = 0;

    public ArticulatedSystem(Shader standardShader)
    {
        name = "Articulated System";
        objId = -1;
        var _nameAsset = Resources.Load<Text> ("menu_prefab/_as_name");
        _jointAsset = Resources.Load<GameObject> ("menu_prefab/_as_joint_p");
        _frameAsset = Resources.Load<GameObject> ("menu_prefab/_as_toggle");
        _uiName = GameObject.Find("as_name").GetComponent<Text>();
        _uiName.name = "articulated_system_name";
        _uiJoint = new GameObject[0];
        _uiFrame = new GameObject[0];

        _framRoot = new GameObject();
        var cofr = Resources.Load<GameObject> ("CoordinateFrame");
        _coordinateFrame = new MeshPool("articulated_system_frames", cofr, _framRoot, VisualTag.Both, standardShader);
    }

    public void Reset(int newObjId, int gcDim, int gvDim, int frame_size, int joint_size)
    {
        gc = new float[gcDim];
        gv = new float[gvDim];
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
        }

        if (objId != newObjId)
        {
            for (int i = 0; i < _uiJoint.Length; i++)
                GameObject.Destroy(_uiJoint[i]);
            
            for (int i = 0; i < _uiFrame.Length; i++)
                GameObject.Destroy(_uiFrame[i]);
            
            _uiJoint = new GameObject[gvDim];
            _uiFrame = new GameObject[frame_size];

            for (int i = 0; i < gvDim; i++)
            {
                _uiJoint[i] = Object.Instantiate (_jointAsset, GameObject.Find("As_Content").transform);
                _uiJoint[i].name = "joint " + i;
            }

            for (int i = 0; i < frame_size; i++)
            {
                _uiFrame[i] = Object.Instantiate (_frameAsset, GameObject.Find("As_Content").transform);
                _uiFrame[i].name = "frame " + i;
            }

        }
        
        frameSize = frame_size;
        jointSize = joint_size;
        
        for (int i = 0; i < _uiJoint.Length; i++)
        {
            _uiJoint[i].SetActive(false);
        }
                
        for (int i = 0; i < _uiFrame.Length; i++)
        {
            _uiFrame[i].SetActive(false);
        }
        
        objId = newObjId;
    }

    public void updateGui()
    {
        _uiName.text = name;
        int guiType = GameObject.Find("as_dropdown").GetComponent<Dropdown>().value;
        
        if (_guiType != guiType)
        {
            for (int i = 0; i < _uiJoint.Length; i++)
            {
                _uiJoint[i].SetActive(false);
            }
                
            for (int i = 0; i < _uiFrame.Length; i++)
            {
                _uiFrame[i].SetActive(false);
            }
        }
        
        if (guiType == 0)
        {
            int gcIdx = 0, gvIdx = 0;
            for (int i = 0; i < jointSize; i++)
            {
                _uiJoint[i].SetActive(true);
            }

            for (int i = 0; i < jointSize; i++)
            {
                if (jointTypes[i] == 1 || jointTypes[i] == 2)
                {
                    // revolute or prismatic
                    var name = _uiJoint[gvIdx].transform.Find("name").gameObject;
                    name.GetComponent<Text>().text = jointNames[i];
                    if (jointTypes[i] == 1)
                    {
                        name.GetComponent<Text>().text += "\n(rev) ";
                    }
                    
                    if (jointTypes[i] == 2)
                    {
                        name.GetComponent<Text>().text += "\n(pri) ";
                    }
                    
                    var angle = _uiJoint[gvIdx].transform.Find("angle").gameObject;
                    angle.GetComponent<Text>().text = "pos:" + gc[gcIdx].ToString("F4");
                    
                    var speed = _uiJoint[gvIdx].transform.Find("speed").gameObject;
                    speed.GetComponent<Text>().text = "vel:" + gv[gvIdx].ToString("F4");
                    gcIdx++;
                    gvIdx++;
                } 
                else if (jointTypes[i] == 3)
                {
                    // spherical
                    UnityEngine.Quaternion quat = new UnityEngine.Quaternion(gc[gcIdx+1], gc[gcIdx + 2], gc[gcIdx + 3], gc[gcIdx]);
                    UnityEngine.Vector3 axis;
                    float angle;
                    quat.ToAngleAxis(out angle, out axis);
                    axis *= angle;
                    var nameX = _uiJoint[gvIdx].transform.Find("name").gameObject;
                    nameX.GetComponent<Text>().text = jointNames[i]+"x";
                    var angleX = _uiJoint[gvIdx].transform.Find("angle").gameObject;
                    angleX.GetComponent<Text>().text = "pos:" + axis[0].ToString("F4");
                    var speedX = _uiJoint[gvIdx].transform.Find("speed").gameObject;
                    speedX.GetComponent<Text>().text = "vel:" + gv[gvIdx].ToString("F4");
                    
                    var nameY = _uiJoint[gvIdx+1].transform.Find("name").gameObject;
                    nameY.GetComponent<Text>().text = jointNames[i]+"y";
                    var angleY = _uiJoint[gvIdx+1].transform.Find("angle").gameObject;
                    angleY.GetComponent<Text>().text = "pos:" + axis[1].ToString("F4");
                    var speedY = _uiJoint[gvIdx+1].transform.Find("speed").gameObject;
                    speedY.GetComponent<Text>().text = "vel:" + gv[gvIdx+1].ToString("F4");
                    
                    var nameZ = _uiJoint[gvIdx+2].transform.Find("name").gameObject;
                    nameZ.GetComponent<Text>().text = jointNames[i]+"z";
                    var angleZ = _uiJoint[gvIdx+2].transform.Find("angle").gameObject;
                    angleZ.GetComponent<Text>().text = "pos:" + axis[2].ToString("F4");
                    var speedZ = _uiJoint[gvIdx+2].transform.Find("speed").gameObject;
                    speedZ.GetComponent<Text>().text = "vel:" + gv[gvIdx+2].ToString("F4");
                    
                    gcIdx += 4;
                    gvIdx += 3;
                    nameX.GetComponent<Text>().text += "\n(ball) ";
                    nameY.GetComponent<Text>().text += "\n(ball) ";
                    nameZ.GetComponent<Text>().text += "\n(ball) ";
                }
                else if (jointTypes[i] == 4)
                {
                    UnityEngine.Quaternion quat = new UnityEngine.Quaternion(gc[gcIdx+4], gc[gcIdx + 5], gc[gcIdx + 6], gc[gcIdx+3]);
                    UnityEngine.Vector3 axis;
                    UnityEngine.Vector3 position = new UnityEngine.Vector3(gc[gcIdx], gc[gcIdx + 1], gc[gcIdx + 2]);
                    float angle;
                    quat.ToAngleAxis(out angle, out axis);
                    axis *= angle;
                    
                    var nameP_X = _uiJoint[gvIdx].transform.Find("name").gameObject;
                    nameP_X.GetComponent<Text>().text = jointNames[i]+"p_x";
                    var angleP_X = _uiJoint[gvIdx].transform.Find("angle").gameObject;
                    angleP_X.GetComponent<Text>().text = "pos:" + position[0].ToString("F4");
                    var speedP_X = _uiJoint[gvIdx].transform.Find("speed").gameObject;
                    speedP_X.GetComponent<Text>().text = "vel:" + gv[gvIdx].ToString("F4");
                    
                    var nameP_Y = _uiJoint[gvIdx+1].transform.Find("name").gameObject;
                    nameP_Y.GetComponent<Text>().text = jointNames[i]+"p_y";
                    var angleP_Y = _uiJoint[gvIdx+1].transform.Find("angle").gameObject;
                    angleP_Y.GetComponent<Text>().text = "pos:" + position[1].ToString("F4");
                    var speedP_Y = _uiJoint[gvIdx+1].transform.Find("speed").gameObject;
                    speedP_Y.GetComponent<Text>().text = "vel:" + gv[gvIdx+1].ToString("F4");
                    
                    var nameP_Z = _uiJoint[gvIdx+2].transform.Find("name").gameObject;
                    nameP_Z.GetComponent<Text>().text = jointNames[i]+"p_z";
                    var angleP_Z = _uiJoint[gvIdx+2].transform.Find("angle").gameObject;
                    angleP_Z.GetComponent<Text>().text = "pos:" + position[2].ToString("F4");
                    var speedP_Z = _uiJoint[gvIdx+2].transform.Find("speed").gameObject;
                    speedP_Z.GetComponent<Text>().text = "vel:" + gv[gvIdx+2].ToString("F4");
                    
                    var nameX = _uiJoint[gvIdx+3].transform.Find("name").gameObject;
                    nameX.GetComponent<Text>().text = jointNames[i]+"a_x";
                    var angleX = _uiJoint[gvIdx+3].transform.Find("angle").gameObject;
                    angleX.GetComponent<Text>().text = "pos:" + axis[0].ToString("F4");
                    var speedX = _uiJoint[gvIdx+3].transform.Find("speed").gameObject;
                    speedX.GetComponent<Text>().text = "vel:" + gv[gvIdx+3].ToString("F4");
                    
                    var nameY = _uiJoint[gvIdx+4].transform.Find("name").gameObject;
                    nameY.GetComponent<Text>().text = jointNames[i]+"a_y";
                    var angleY = _uiJoint[gvIdx+4].transform.Find("angle").gameObject;
                    angleY.GetComponent<Text>().text = "pos:" + axis[1].ToString("F4");
                    var speedY = _uiJoint[gvIdx+4].transform.Find("speed").gameObject;
                    speedY.GetComponent<Text>().text = "vel:" + gv[gvIdx+4].ToString("F4");
                    
                    var nameZ = _uiJoint[gvIdx+5].transform.Find("name").gameObject;
                    nameZ.GetComponent<Text>().text = jointNames[i]+"a_z";
                    var angleZ = _uiJoint[gvIdx+5].transform.Find("angle").gameObject;
                    angleZ.GetComponent<Text>().text = "pos:" + axis[2].ToString("F4");
                    var speedZ = _uiJoint[gvIdx+5].transform.Find("speed").gameObject;
                    speedZ.GetComponent<Text>().text = "vel:" + gv[gvIdx+5].ToString("F4");
                    
                    gcIdx += 7;
                    gvIdx += 6;
                    
                    nameX.GetComponent<Text>().text += "\n(float) ";
                    nameY.GetComponent<Text>().text += "\n(float) ";
                    nameZ.GetComponent<Text>().text += "\n(float) ";
                    
                    nameP_X.GetComponent<Text>().text += "\n(float) ";
                    nameP_Y.GetComponent<Text>().text += "\n(float) ";
                    nameP_Z.GetComponent<Text>().text += "\n(float) ";
                }
            }
        } else if (guiType == 1)
        {
            for (int i = 0; i < frameSize; i++)
            {
                _uiFrame[i].SetActive(true);
                _uiFrame[i].transform.Find("Label").gameObject.GetComponent<Text>().text = frameNames[i];

                if (_uiFrame[i].GetComponent<Toggle>().isOn)
                {
                    var cof = _coordinateFrame.AddMesh();
                    ObjectController.SetTransform(cof, framesPos[i], framesQuat[i]);
                }
            }
        } else if (guiType == 2)
        {
            
        }
        
        _coordinateFrame.AllSet();
        _guiType = guiType;
    }

    public void Clear()
    {
        _coordinateFrame.Clear();
    }
}
