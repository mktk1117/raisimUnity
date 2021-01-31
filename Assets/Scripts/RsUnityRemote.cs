/*
 * MIT License
 * 
 * Copyright (c) 2019, Dongho Kang, Robotics Systems Lab, ETH Zurich
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;
using System.Collections.Specialized;
using CColor = System.Drawing.Color;

namespace raisimUnity
{
    enum ClientStatus : int
    {
        Idle = 0,    // waiting for connection or server is hibernating
        InitializingObjects,
        UpdateObjectPosition,
    }

    public enum RsObejctType : int
    {
        RsSphereObject = 0, 
        RsBoxObject,
        RsCylinderObject,
        RsConeObject, 
        RsCapsuleObject,
        RsMeshObject,
        RsHalfSpaceObject, 
        RsCompoundObject,
        RsHeightMapObject,
        RsArticulatedSystemObject,
    }

    public enum RsShapeType : int
    {
        RsBoxShape = 0, 
        RsCylinderShape,
        RsSphereShape,
        RsMeshShape,
        RsCapsuleShape, 
        RsConeShape,
    }

    public enum RsVisualType : int
    {
        RsVisualSphere = 0,
        RsVisualBox,
        RsVisualCylinder,
        RsVisualCapsule,
        RsVisualMesh,
        RsVisualArrow
    }

    static class VisualTag
    {
        public const string Visual = "visual";
        public const string Collision = "collision";
        public const string ArticulatedSystemCollision = "articulated_system_collision";
        public const string Frame = "frame";
        public const string Both = "both";
    }

    public class RsUnityRemote : MonoBehaviour
    {
        // Prevent repeated instances
        private static RsUnityRemote instance;
        
        private XmlReader _xmlReader;
        private ResourceLoader _loader;
        private TcpHelper _tcpHelper;
        public Dictionary<string, string> _objName;
        
        private RsUnityRemote()
        {
            _tcpHelper = new TcpHelper();
            _xmlReader = new XmlReader();
            _loader = new ResourceLoader();
            _objName = new Dictionary<string, string>();
        }
        
        // Status
        private ClientStatus _clientStatus;

        // Visualization
        private bool _showVisualBody = true;
        private bool _showCollisionBody = false;
        private bool _showContactPoints = false;
        private bool _showContactForces = false;
        private bool _showBodyFrames = false;
        private float _contactPointMarkerScale = 1;
        private float _contactForceMarkerScale = 1;
        private float _bodyFrameMarkerScale = 1;
        private GameObject _arrowMesh;
        public List<string> _skyNames;
        public List<Cubemap> _skyCubemaps;

        // Root objects
        private GameObject _objectsRoot;
        private GameObject _visualsRoot;
        private GameObject _contactPointsRoot;
        private GameObject _polylineRoot;
        private GameObject _contactForcesRoot;
        private GameObject _objectCache;
        
        // Object controller 
        private ObjectController _objectController;
        private ulong _numInitializedObjects;
        private ulong _numWorldObjects; 
        private ulong _numInitializedVisuals;
        private ulong _numWorldVisuals;
        private ulong _numWorldVisualsSingleBodies;
        private ulong _numWorldVisualsArticulatedSystems;

        private ulong _wireN=0;
        
        // Shaders
        private Shader _transparentShader;
        private Shader _standardShader;
        
        // Default materials
        private Material _planeMaterial;
        private Material _whiteMaterial;
        private Material _wireMaterial;
        private Material _defaultMaterialR;
        private Material _defaultMaterialG;
        private Material _defaultMaterialB;
        private Material _transparentMaterial;

        // Modal view
        // private ErrorViewController _errorModalView;
        private LoadingViewController _loadingModalView;
        
        // Configuration number (should be always matched with server)
        private ulong _objectConfiguration = 0; 
        private CameraController _camera = null;
        private string _defaultShader;
        private string _colorString;
        
        // objects reinitialize
        private bool _initialization = true;

        private String _errorLogFile = "";

        // meshPoos
        private MeshPool _contactForceMeshPool;
        private MeshPool _contactPointMeshPool;
        private MeshPool _externalForceMeshPool;
        private MeshPool _externalTorqueMeshPool;
        private MeshPool _polylineMeshPool;
        
        // controlled objects
        public int objSelectedId = -1;
        public ArticulatedSystem _articulatedSystem;
        public SingleBody _singleBody;

        void Start()
        {
            // object roots
            if (!File.Exists(Application.dataPath+"/../Logs"))
                Directory.CreateDirectory(Application.dataPath+"/../Logs");
            _errorLogFile = Path.Combine(Application.dataPath+"/../Logs/", "raisim_error_log.txt");

            File.WriteAllText(_errorLogFile, "error logs \n");
            _objectsRoot = new GameObject("_RsObjects");
            _objectsRoot.transform.SetParent(transform);
            _objectCache = new GameObject("_ObjectCache");
            _objectCache.transform.SetParent(transform);
            _visualsRoot = new GameObject("_VisualObjects");
            _visualsRoot.transform.SetParent(transform);
            _contactPointsRoot = new GameObject("_ContactPoints");
            _contactPointsRoot.transform.SetParent(transform);
            _polylineRoot = new GameObject("_polylineRoot");
            _polylineRoot.transform.SetParent(transform);
            _contactForcesRoot = new GameObject("_ContactForces");
            _contactForcesRoot.transform.SetParent(transform);
            _camera = GameObject.Find("Main Camera").GetComponent<CameraController>();
            _arrowMesh = Resources.Load("others/arrow") as GameObject;

            if (GraphicsSettings.renderPipelineAsset is HDRenderPipelineAsset)
            {
                _defaultShader = "HDRP/Lit";
                _colorString = "_BaseColor";
                _skyNames.Add("sky1"); _skyCubemaps.Add(Resources.Load<Cubemap>("AllSkyFree/sky1/sky1"));
                _skyNames.Add("sky2"); _skyCubemaps.Add(Resources.Load<Cubemap>("AllSkyFree/sky2/sky2"));
                _skyNames.Add("sky3"); _skyCubemaps.Add(Resources.Load<Cubemap>("AllSkyFree/sky3/sky3"));
                _skyNames.Add("sky4"); _skyCubemaps.Add(Resources.Load<Cubemap>("AllSkyFree/sky4/sky4"));
                _skyNames.Add("sky5"); _skyCubemaps.Add(Resources.Load<Cubemap>("AllSkyFree/sky5/sky5"));
                _skyNames.Add("sky6"); _skyCubemaps.Add(Resources.Load<Cubemap>("AllSkyFree/sky6/sky6"));
                _skyNames.Add("sky7"); _skyCubemaps.Add(Resources.Load<Cubemap>("AllSkyFree/sky7/sky7"));
                _skyNames.Add("sky8"); _skyCubemaps.Add(Resources.Load<Cubemap>("AllSkyFree/sky8/sky8"));
            }
            else
            {
                _defaultShader = "Standard";
                _colorString = "_Color";
            }
            
            // object controller 
            _objectController = new ObjectController(_objectCache);

            // shaders 
            _standardShader = Shader.Find(_defaultShader);
            _transparentShader = Shader.Find("RaiSim/Transparent");

            // materials
            _planeMaterial = Resources.Load<Material>("Tiles1");
            _whiteMaterial = Resources.Load<Material>("white");
            _wireMaterial = Resources.Load<Material>("wire");
            _defaultMaterialR = Resources.Load<Material>("Plastic1");
            _defaultMaterialG = Resources.Load<Material>("Plastic2");
            _defaultMaterialB = Resources.Load<Material>("Plastic3");
            _transparentMaterial = Resources.Load<Material>("transparent");
            
            // ui controller 
            // _errorModalView = GameObject.Find("_CanvasModalViewError").GetComponent<ErrorViewController>();
            _loadingModalView = GameObject.Find("_CanvasModalViewLoading").GetComponent<LoadingViewController>();
            _clientStatus = ClientStatus.Idle;

            // mesh pools
            _contactForceMeshPool = new MeshPool("contactForce", _arrowMesh, _contactForcesRoot, VisualTag.Both, _standardShader);
            _contactPointMeshPool = new MeshPool("contactPoint", GameObject.CreatePrimitive(PrimitiveType.Sphere), _contactPointsRoot, VisualTag.Both, _standardShader);
            _externalForceMeshPool = new MeshPool("externalForce", _arrowMesh, _contactForcesRoot, VisualTag.Both, _standardShader);
            _externalTorqueMeshPool = new MeshPool("externalTorque", _arrowMesh, _contactForcesRoot, VisualTag.Both, _standardShader);
            _polylineMeshPool = new MeshPool("polyline", GameObject.CreatePrimitive(PrimitiveType.Cube), _polylineRoot, VisualTag.Both, _standardShader);

            _articulatedSystem = new ArticulatedSystem();
            _singleBody = new SingleBody();
        }

        public void EstablishConnection(int waitTime=1000)
        {
            _tcpHelper.EstablishConnection(waitTime);
            _clientStatus = ClientStatus.InitializingObjects;
        }

        public void CloseConnection()
        {
            ClearScene();
            
            _tcpHelper.CloseConnection();
            _clientStatus = ClientStatus.Idle;
        }

        void Update()
        {
            // Broken connection: clear
            if(!_tcpHelper.CheckConnection())
                CloseConnection();
     
            // Data available: handle communication
            if (_tcpHelper.DataAvailable)
            {
                try
                {
                    switch (_clientStatus)
                    {
                        //**********************************************************************************************
                        // Step 0
                        //**********************************************************************************************
                        case ClientStatus.Idle:
                        {
                            try
                            {
                                // Server hibernating
                                CloseConnection();
                                _clientStatus = ClientStatus.InitializingObjects;
                                ReadAndCheckServer(ClientMessageType.RequestServerStatus);
                            }
                            catch (Exception e)
                            {
                                new RsuException(e, "RsUnityRemote: ClientStatus.Idle");
                            }
                            
                            break;
                        }
                        //**********************************************************************************************
                        // Step 1
                        //**********************************************************************************************
                        case ClientStatus.InitializingObjects:
                        {
                            try
                            {
                                if (_initialization)
                                {
                                    ClearScene();
                                    // Read XML string
                                    // ReadXmlString();
                                    if (ReadAndCheckServer(ClientMessageType.RequestInitializeObjects) != ServerMessageType.Initialization)
                                        return;
                                    _objectConfiguration = _tcpHelper.GetDataUlong();
                                    _numWorldObjects = _tcpHelper.GetDataUlong();
                                    _numInitializedObjects = 0;
                                    _numInitializedVisuals = 0;
                                    _initialization = false;
                                }
                                
                                if (_numInitializedObjects < _numWorldObjects)
                                {
                                    // Initialize objects from data
                                    // If the function call time is > 0.1 sec, rest of objects are initialized in next Update iteration
                                    PartiallyInitializeObjects();
                                    _loadingModalView.Show(true);
                                    _loadingModalView.SetTitle("Initializing RaiSim Objects");
                                    _loadingModalView.SetMessage("Loading meshes...");
                                    _loadingModalView.SetProgress((float) _numInitializedObjects / _numWorldObjects);   

                                    if (_numInitializedObjects == _numWorldObjects)
                                    {
                                        _wireN = _tcpHelper.GetDataUlong();
                                        for (ulong i = 0; i < _wireN; i++)
                                        {
                                            var objFrame = _objectController.CreateRootObject(_objectsRoot, "wire" + i);
                                            var cylinder = _objectController.CreateCylinder(objFrame, 1, 1);
                                            cylinder.GetComponentInChildren<MeshRenderer>().material.shader =
                                                _standardShader;
                                            cylinder.GetComponentInChildren<MeshRenderer>().material = _wireMaterial;
                                            cylinder.tag = VisualTag.Both;
                                        }
                                    }
                                }
                                
                                if (_numInitializedObjects == _numWorldObjects)
                                {
                                    if (_numInitializedVisuals == 0)
                                    {
                                        _numWorldVisuals = _tcpHelper.GetDataUlong();
                                        _numWorldVisualsSingleBodies = _tcpHelper.GetDataUlong();
                                        _numWorldVisualsArticulatedSystems = _tcpHelper.GetDataUlong();    
                                    }
                                    PartiallyInitializeVisuals();
                                    _loadingModalView.Show(true);
                                    _loadingModalView.SetTitle("Initializing Visual Objects");
                                    _loadingModalView.SetMessage("Loading meshes...");
                                    _loadingModalView.SetProgress((float) _numInitializedVisuals / _numWorldVisuals);   
                                    
                                    if (_numInitializedVisuals == _numWorldVisuals)
                                    {
                                        // Disable other cameras than main camera
                                        foreach (var cam in Camera.allCameras)
                                            if (cam != Camera.main) 
                                                cam.enabled = false;
                                        
                                        _tcpHelper.GetDataServerMessageType(); /// not used here
                                        
                                        UpdateObjectsPosition();

                                        // Initialization done 
                                        _clientStatus = ClientStatus.UpdateObjectPosition;
                                        _initialization = true;
                                        ShowOrHideObjects();
                                        _loadingModalView.Show(false);
                                        GameObject.Find("_CanvasSidebar").GetComponent<UIController>().ConstructLookAt();
                                    }
                                }
                            } catch (Exception e)
                            {
                                new RsuException(e, "RsUnityRemote: InitializeObjects");
                            }
                            
                            break;
                        }
                        //**********************************************************************************************
                        // Step 2
                        //**********************************************************************************************
                        case ClientStatus.UpdateObjectPosition:
                        {
                            try
                            {
                                if(ReadAndCheckServer(ClientMessageType.RequestObjectPosition) != ServerMessageType.ObjectPositionUpdate)
                                    return;
                                
                                if (!UpdateObjectsPosition())
                                    return;
                                
                                if(_showContactPoints || _showContactForces)
                                    UpdateContacts();

                                _contactForceMeshPool.AllSet();            
                                _contactPointMeshPool.AllSet();

                            // If configuration number for visuals doesn't match, _clientStatus is updated to ReinitializeObjectsStart  
                            // Else clientStatus is updated to UpdateVisualPosition
                            } 
                            catch (Exception e)
                            {
                                new RsuException(e, "RsUnityRemote: UpdateObjectPosition");
                            }
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(_errorLogFile, true))
                    {
                        file.WriteLine(e.ToString()+"\n \n");
                    }
                    GameObject.Find("_CanvasSidebar").GetComponent<UIController>().setError(e.ToString());

                    _clientStatus = ClientStatus.Idle;
                    // Close connection
                    ClearScene();
                }
            }
        }

        private void processServerRequest()
        {
            ulong requestN = _tcpHelper.GetDataUlong();
            for (ulong i = 0; i < requestN; i++)
            {
                var requestType = _tcpHelper.GetServerRequest();
                switch (requestType)
                {
                    case TcpHelper.ServerRequestType.NoRequest:
                        break;
                
                    case TcpHelper.ServerRequestType.SetCameraTo:
                        float px = (float)_tcpHelper.GetDataDouble();
                        float py = (float)_tcpHelper.GetDataDouble();
                        float pz = (float)_tcpHelper.GetDataDouble();
                        float lx = (float)_tcpHelper.GetDataDouble();
                        float ly = (float)_tcpHelper.GetDataDouble();
                        float lz = (float)_tcpHelper.GetDataDouble();
                        _camera.transform.LookAt(new Vector3(px, pz, py), new Vector3(lx, lz, ly));
                        break;
                
                    case TcpHelper.ServerRequestType.FocusOnSpecificObject:
                        var obj = _tcpHelper.GetDataString();
                        if (obj != "")
                        { 
                            _camera.Follow(obj);
                        }
                        break;
                
                    case TcpHelper.ServerRequestType.StartRecordVideo:
                        var videoName = _tcpHelper.GetDataString();
                        _camera.StartRecording(videoName);
                        break;
                
                    case TcpHelper.ServerRequestType.StopRecordVideo:
                        _camera.FinishRecording();
                        break;
                }
            }
        }
        
        private void ClearScene()
        {
            // Objects
            foreach (Transform objT in _objectsRoot.transform)
            {
                Destroy(objT.gameObject);
            }

            // mesh pools
            _contactPointMeshPool.Clear();
            _contactForceMeshPool.Clear();
            _externalForceMeshPool.Clear();
            _externalTorqueMeshPool.Clear();
            _polylineMeshPool.Clear();

            // visuals
            foreach (Transform child in _visualsRoot.transform)
            {
                Destroy(child.gameObject);
            }

            
            // clear appearances
            if(_xmlReader != null)
                _xmlReader.ClearAppearanceMap();
            
            // clear modal view
            _loadingModalView.Show(false);
            
            // clear object cache
            _objName.Clear();
            
            // Resources.UnloadUnusedAssets();
        }

        private void addArticulatedSystem(string objectIndex)
        {
            string urdfDirPathInServer = _tcpHelper.GetDataString(); 

            // visItem = 0 (visuals)
            // visItem = 1 (collisions)
            for (int visItem = 0; visItem < 2; visItem++)
            {
                ulong numberOfVisObjects = _tcpHelper.GetDataUlong();

                for (ulong j = 0; j < numberOfVisObjects; j++)
                {
                    RsShapeType shapeType = _tcpHelper.GetDataRsShapeType();
                    String material = _tcpHelper.GetDataString();
                    Color color = getColor();
                    ulong group = _tcpHelper.GetDataUlong();

                    string subName = objectIndex + "/" + visItem + "/" + j;
                    var objFrame = _objectController.CreateRootObject(_objectsRoot, subName);

                    string tag = "";
                    if (visItem == 0)
                        tag = VisualTag.Visual;
                    else if (visItem == 1)
                        tag = VisualTag.ArticulatedSystemCollision;

                    GameObject obj = null;

                    if (shapeType == RsShapeType.RsMeshShape)
                    {
                        string meshFile = _tcpHelper.GetDataString();
                        string meshFileExtension = Path.GetExtension(meshFile);

                        double sx = _tcpHelper.GetDataDouble();
                        double sy = _tcpHelper.GetDataDouble();
                        double sz = _tcpHelper.GetDataDouble();

                        string meshFilePathInResourceDir = _loader.RetrieveMeshPath(urdfDirPathInServer, meshFile);
                        if (meshFilePathInResourceDir == null)
                        {
                            new RsuException("Cannot find mesh from resource directories = " + meshFile);
                        }

                        try
                        {
                            obj = _objectController.CreateMesh(objFrame, meshFilePathInResourceDir, (float)sx, (float)sy, (float)sz);
                            obj.tag = tag;
                        }
                        catch (Exception e)
                        {
                            new RsuException("Cannot create mesh: " + e.Message);
                        }
                    }
                    else
                    {
                        ulong size = _tcpHelper.GetDataUlong();
                            
                        var visParam = new List<double>();
                        for (ulong k = 0; k < size; k++)
                        {
                            double visSize = _tcpHelper.GetDataDouble();
                            visParam.Add(visSize);
                        }
                      
                        switch (shapeType)
                        {
                            case RsShapeType.RsBoxShape:
                                if (visParam.Count != 3) new RsuException("Box Mesh error");
                                obj = _objectController.CreateBox(objFrame, (float) visParam[0], (float) visParam[1], (float) visParam[2]);
                                break;
                            case RsShapeType.RsCapsuleShape:
                                if (visParam.Count != 2) new RsuException("Capsule Mesh error");
                                obj = _objectController.CreateCapsule(objFrame, (float)visParam[0], (float)visParam[1]);
                                break;
                            case RsShapeType.RsConeShape:
                                // TODO URDF does not support cone shape
                                break;
                            case RsShapeType.RsCylinderShape:
                                if (visParam.Count != 2) new RsuException("Cylinder Mesh error");
                                obj = _objectController.CreateCylinder(objFrame, (float)visParam[0], (float)visParam[1]);
                                break;
                            case RsShapeType.RsSphereShape:
                                if (visParam.Count != 1) new RsuException("Sphere Mesh error");
                                obj = _objectController.CreateSphere(objFrame, (float)visParam[0]);
                                break;
                        }
                        
                        obj.tag = tag;
                        if(color.a != 0)
                            setColor(obj, color);
                    }
                }
            }
        }
        
        private void PartiallyInitializeObjects()
        {
            while (_numInitializedObjects < _numWorldObjects)
            {
                ulong objectIndex = _tcpHelper.GetDataUlong();
                RsObejctType objectType = _tcpHelper.GetDataRsObejctType();
                
                // get name and find corresponding appearance from XML
                string name = _tcpHelper.GetDataString();
                if (name != "" && !_objName.ContainsKey(name))
                {
                    _objName.Add(objectIndex.ToString(),name);    
                }
                
                if (objectType == RsObejctType.RsArticulatedSystemObject)
                {
                    addArticulatedSystem(objectIndex.ToString());
                }
                else if (objectType == RsObejctType.RsHalfSpaceObject)
                {
                    // get material
                    String appearance = _tcpHelper.GetDataString();
                    Material material;
                    material = _planeMaterial;
                    
                    float height = _tcpHelper.GetDataFloat();
                    var objFrame = _objectController.CreateRootObject(_objectsRoot, objectIndex.ToString());
                    var plane = _objectController.CreateHalfSpace(objFrame, height);
                    plane.GetComponentInChildren<Renderer>().material = _whiteMaterial;
                    plane.tag = VisualTag.Collision;
                    var planeVis = _objectController.CreateHalfSpace(objFrame, height);
                    planeVis.GetComponentInChildren<Renderer>().material = material;
                    planeVis.GetComponentInChildren<Renderer>().material.mainTextureScale = new Vector2(15, 15);
                    planeVis.tag = VisualTag.Visual;
                    planeVis.name = "halfspace_viz";
                }
                else if (objectType == RsObejctType.RsHeightMapObject)
                {
                    String appearance = _tcpHelper.GetDataString();

                    // center
                    float centerX = _tcpHelper.GetDataFloat();
                    float centerY = _tcpHelper.GetDataFloat();
                    // size
                    float sizeX = _tcpHelper.GetDataFloat();
                    float sizeY = _tcpHelper.GetDataFloat();
                    // num samples
                    ulong numSampleX = _tcpHelper.GetDataUlong();
                    ulong numSampleY = _tcpHelper.GetDataUlong();
                    ulong numSample = _tcpHelper.GetDataUlong();
                        
                    // height values 
                    float[,] heights = new float[numSampleY, numSampleX];
                    for (ulong j = 0; j < numSampleY; j++)
                    {
                        for (ulong k = 0; k < numSampleX; k++)
                        {
                            float height = _tcpHelper.GetDataFloat();
                            heights[j, k] = height;
                        }
                    }

                    var objFrame = _objectController.CreateRootObject(_objectsRoot, objectIndex.ToString());
                    var terrain = _objectController.CreateTerrain(objFrame, numSampleX, sizeX, centerX, numSampleY, sizeY, centerY, heights, true);
                    terrain.tag = VisualTag.Both;
                    if (appearance == "")
                    {
                        terrain.GetComponentInChildren<MeshRenderer>().material = _whiteMaterial;
                    }
                    else
                    {
                        setColorFromString(gameObject, appearance);
                    }
                }
                else if (objectType == RsObejctType.RsCompoundObject)
                {
                    String appearance = _tcpHelper.GetDataString();
                    ulong numberOfVisObjects = _tcpHelper.GetDataUlong();

                    for (ulong j = 0; j < numberOfVisObjects; j++)
                    {
                        RsObejctType obType = _tcpHelper.GetDataRsObejctType();
                        string subName = objectIndex.ToString() + "/" + j.ToString();
                        var objFrame = _objectController.CreateRootObject(_objectsRoot, subName);
                        string tag = VisualTag.Both;
                        GameObject gameObject = null;
                        
                        switch (obType)
                        {
                            case RsObejctType.RsBoxObject:
                            {
                                double x = _tcpHelper.GetDataDouble();
                                double y = _tcpHelper.GetDataDouble();
                                double z = _tcpHelper.GetDataDouble();
                                gameObject = _objectController.CreateBox(objFrame, (float) x, (float) y, (float) z);
                            }
                                break;
                            case RsObejctType.RsCapsuleObject:
                            {
                                double radius = _tcpHelper.GetDataDouble();
                                double height = _tcpHelper.GetDataDouble();

                                gameObject = _objectController.CreateCapsule(objFrame, (float)radius, (float)height);
                            }
                                break;
                            case RsObejctType.RsConeObject:
                            {
                                // TODO URDF does not support cone shape
                            }
                                break;
                            case RsObejctType.RsCylinderObject:
                            {
                                double radius = _tcpHelper.GetDataDouble();
                                double height = _tcpHelper.GetDataDouble();
                                gameObject = _objectController.CreateCylinder(objFrame, (float)radius, (float)height);
                            }
                                break;
                            case RsObejctType.RsSphereObject:
                                gameObject = _objectController.CreateSphere(objFrame, (float)_tcpHelper.GetDataDouble());
                                break;                            
                        }
                        
                        gameObject.GetComponentInChildren<MeshRenderer>().material.shader = _standardShader;
                        if (appearance == "")
                        {
                            switch (_numInitializedObjects % 3)
                            {
                                case 0:
                                    gameObject.GetComponentInChildren<MeshRenderer>().material = _defaultMaterialR;
                                    break;
                                case 1:
                                    gameObject.GetComponentInChildren<MeshRenderer>().material = _defaultMaterialG;
                                    break;
                                case 2:
                                    gameObject.GetComponentInChildren<MeshRenderer>().material = _defaultMaterialB;
                                    break;
                                default:
                                    gameObject.GetComponentInChildren<MeshRenderer>().material = _defaultMaterialR;
                                    break;
                            }    
                        }
                        else
                        {
                            setColorFromString(gameObject, appearance);
                        }
                        gameObject.tag = tag;
                    }
                }
                else
                {
                    // single body object
                    String appearance = _tcpHelper.GetDataString();

                    // create base frame of object
                    var objFrame = _objectController.CreateRootObject(_objectsRoot, objectIndex.ToString());
                    
                    // collision body 
                    GameObject collisionObject = null;
                    
                    switch (objectType) 
                    {
                        case RsObejctType.RsSphereObject :
                        {
                            float radius = _tcpHelper.GetDataFloat();
                            collisionObject =  _objectController.CreateSphere(objFrame, radius);
                        }
                            break;

                        case RsObejctType.RsBoxObject :
                        {
                            float sx = _tcpHelper.GetDataFloat();
                            float sy = _tcpHelper.GetDataFloat();
                            float sz = _tcpHelper.GetDataFloat();
                            collisionObject = _objectController.CreateBox(objFrame, sx, sy, sz);
                        }
                            break;
                        case RsObejctType.RsCylinderObject:
                        {
                            float radius = _tcpHelper.GetDataFloat();
                            float height = _tcpHelper.GetDataFloat();
                            collisionObject = _objectController.CreateCylinder(objFrame, radius, height);
                        }
                            break;
                        case RsObejctType.RsCapsuleObject:
                        {
                            float radius = _tcpHelper.GetDataFloat();
                            float height = _tcpHelper.GetDataFloat();
                            collisionObject = _objectController.CreateCapsule(objFrame, radius, height);
                        }
                            break;
                        case RsObejctType.RsMeshObject:
                        {
                            string meshFile = _tcpHelper.GetDataString();
                            float scale = _tcpHelper.GetDataFloat();
                            
                            string meshFileName = Path.GetFileName(meshFile);       
                            string meshFileExtension = Path.GetExtension(meshFile);
                            
                            string meshFilePathInResourceDir = _loader.RetrieveMeshPath(Path.GetDirectoryName(meshFile), meshFileName);
                            
                            collisionObject = _objectController.CreateMesh(objFrame, meshFilePathInResourceDir, 
                                scale, scale, scale);
                        }
                            break;
                    }
                    collisionObject.tag = VisualTag.Both;
           
                    // default material
                    if (appearance == "")
                    {
                        switch (_numInitializedObjects % 3)
                        {
                            case 0:
                                collisionObject.GetComponentInChildren<MeshRenderer>().material = _defaultMaterialR;
                                break;
                            case 1:
                                collisionObject.GetComponentInChildren<MeshRenderer>().material = _defaultMaterialG;
                                break;
                            case 2:
                                collisionObject.GetComponentInChildren<MeshRenderer>().material = _defaultMaterialB;
                                break;
                            default:
                                collisionObject.GetComponentInChildren<MeshRenderer>().material = _defaultMaterialR;
                                break;
                        }
                    }
                    else
                    {
                        setColorFromString(collisionObject, appearance);
                    }
                }

                _numInitializedObjects++;
                if (Time.deltaTime > 0.3)
                    // If initialization takes too much time, do the rest in next iteration (to prevent freezing GUI(
                    break;
            }
        }

        private void PartiallyInitializeVisuals()
        {
            while (_numInitializedVisuals < _numWorldVisuals)
            {
                if (_numInitializedVisuals < _numWorldVisualsSingleBodies)
                {
                    RsVisualType objectType = _tcpHelper.GetDataRsVisualType();
                
                    // get name and find corresponding appearance from XML
                    string objectName = _tcpHelper.GetDataString();
                    Color color = getColor();
                    string materialName = _tcpHelper.GetDataString();
                    bool glow = _tcpHelper.GetDataBool();
                    bool shadow = _tcpHelper.GetDataBool();

                    var visFrame = _objectController.CreateRootObject(_visualsRoot, objectName);
                    
                    GameObject visual = null;
                        
                    switch (objectType)
                    {
                        case RsVisualType.RsVisualSphere :
                        {
                            float radius = _tcpHelper.GetDataFloat();
                            visual =  _objectController.CreateSphere(visFrame, radius);
                            visual.tag = VisualTag.Visual;
                        }
                            break;
                        case RsVisualType.RsVisualBox:
                        {
                            float sx = _tcpHelper.GetDataFloat();
                            float sy = _tcpHelper.GetDataFloat();
                            float sz = _tcpHelper.GetDataFloat();
                            visual = _objectController.CreateBox(visFrame, sx, sy, sz);
                            visual.tag = VisualTag.Visual;
                        }
                            break;
                        case RsVisualType.RsVisualCylinder:
                        {
                            float radius = _tcpHelper.GetDataFloat();
                            float height = _tcpHelper.GetDataFloat();
                            visual = _objectController.CreateCylinder(visFrame, radius, height);
                            visual.tag = VisualTag.Visual;
                        }
                            break;
                        case RsVisualType.RsVisualCapsule:
                        {
                            float radius = _tcpHelper.GetDataFloat();
                            float height = _tcpHelper.GetDataFloat();
                            visual = _objectController.CreateCapsule(visFrame, radius, height);
                            visual.tag = VisualTag.Visual;
                        }
                            break;
                        case RsVisualType.RsVisualArrow:
                        {
                            float radius = _tcpHelper.GetDataFloat();
                            float height = _tcpHelper.GetDataFloat();
                            visual = _objectController.CreateArrow(visFrame, radius, height);
                            visual.tag = VisualTag.Visual;
                        }
                            break;
                    }
                    
                    // set material or color
                    if (string.IsNullOrEmpty(materialName) && visual != null)
                    {
                        // set material by rgb 
                        visual.GetComponentInChildren<Renderer>().material.SetColor(_colorString, color);
                        if(glow)
                        {
                            visual.GetComponentInChildren<Renderer>().material.EnableKeyword("_EMISSION");
                            visual.GetComponentInChildren<Renderer>().material.SetColor("_EmissionColor", color);
                        }
                    }
                    else
                    {
                        // set material from
                        Material material = Resources.Load<Material>(materialName);
                        visual.GetComponentInChildren<Renderer>().material = material;
                    }
                    
                    // set shadow 
                    if (shadow)
                        visual.GetComponentInChildren<Renderer>().shadowCastingMode = ShadowCastingMode.On;
                    else
                        visual.GetComponentInChildren<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                }
                else
                {
                    for (ulong i = 0; i < _numWorldVisualsArticulatedSystems; i++)
                    {
                        string name = _tcpHelper.GetDataString();
                        addArticulatedSystem(name+i);
                    }
                }
                
                _numInitializedVisuals++;
                if (Time.deltaTime > 0.3f)
                    // If initialization takes too much time, do the rest in next iteration (to prevent freezing GUI(
                    break;
            }
        }

        private void setObjectPosition(string objectName)
        {
            double posX = _tcpHelper.GetDataDouble();
            double posY = _tcpHelper.GetDataDouble();
            double posZ = _tcpHelper.GetDataDouble();
                    
            double quatW = _tcpHelper.GetDataDouble();
            double quatX = _tcpHelper.GetDataDouble();
            double quatY = _tcpHelper.GetDataDouble();
            double quatZ = _tcpHelper.GetDataDouble();

            GameObject localObject = GameObject.Find(objectName);

            if (localObject != null)
            {
                ObjectController.SetTransform(
                    localObject, 
                    new Vector3((float)posX, (float)posY, (float)posZ), 
                    new Quaternion((float)quatX, (float)quatY, (float)quatZ, (float)quatW)
                );
            }
            else
            {
                new RsuException("Cannot find unity game object: " + objectName);
            }
        }

        private Color getColor()
        {
            double colorR = _tcpHelper.GetDataDouble();
            double colorG = _tcpHelper.GetDataDouble();
            double colorB = _tcpHelper.GetDataDouble();
            double colorA = _tcpHelper.GetDataDouble();
            return new Color((float) colorR, (float) colorG, (float) colorB, (float) colorA);
        }
        
        private bool UpdateObjectsPosition() 
        {
            if (_tcpHelper.GetDataUlong() != _objectConfiguration)
            {
                _numInitializedObjects = 0;
                _clientStatus = ClientStatus.InitializingObjects;
                return false;
            }
            
            ulong numObjects = _tcpHelper.GetDataUlong();

            for (ulong i = 0; i < numObjects; i++)
            {
                ulong localIndexSize = _tcpHelper.GetDataUlong();
                for (ulong j = 0; j < localIndexSize; j++)
                {
                    string objectName = _tcpHelper.GetDataString();
                    setObjectPosition(objectName);
                }
            }

            // visual objects
            ulong numVisObjects = _tcpHelper.GetDataUlong();
            ulong numVisObjectsSb = _tcpHelper.GetDataUlong();
            ulong numVisObjectsAs = _tcpHelper.GetDataUlong();

            for (ulong i = 0; i < numVisObjectsSb; i++)
            {
                string visualName = _tcpHelper.GetDataString();
                setObjectPosition(visualName);
                
                RsVisualType objectType = _tcpHelper.GetDataRsVisualType();
                Color color = getColor();
                double sizeA = _tcpHelper.GetDataDouble();
                double sizeB = _tcpHelper.GetDataDouble();
                double sizeC = _tcpHelper.GetDataDouble();

                GameObject localObject = GameObject.Find(visualName);

                if (localObject != null)
                {
                    // set material by rgb
                    setColor(localObject, color);

                    switch (objectType)
                    {
                        case RsVisualType.RsVisualSphere :
                            localObject.transform.localScale = new Vector3((float)sizeA, (float)sizeA, (float)sizeA);
                            break;
                        case RsVisualType.RsVisualBox:
                            localObject.transform.localScale = new Vector3((float)sizeA, (float)sizeB, (float)sizeC);
                            break;
                        case RsVisualType.RsVisualCylinder:
                            localObject.transform.localScale = new Vector3((float)sizeA, (float)sizeB, (float)sizeA);
                            break;
                        case RsVisualType.RsVisualCapsule:
                            localObject.transform.localScale = new Vector3((float)sizeA, (float)sizeB*0.5f+(float)sizeA*0.5f, (float)sizeA);
                            break;
                        case RsVisualType.RsVisualArrow:
                            localObject.transform.localScale = new Vector3((float)sizeA, (float)sizeA, (float)sizeB);
                            break;
                    }
                }
                else
                {
                    new RsuException("Cannot find unity game object: " + visualName);
                }
            }

            for (ulong i = 0; i < numVisObjectsAs; i++)
            {
                Color color = getColor();
                ulong localIndexSize = _tcpHelper.GetDataUlong();
                
                for (ulong j = 0; j < localIndexSize; j++)
                {
                    string objectName = _tcpHelper.GetDataString();
                    setObjectPosition(objectName);
                    if (color.a != 0)
                        setColor(GameObject.Find(objectName), color);
                }
            }

            // polylines objects
            numObjects = _tcpHelper.GetDataUlong();
            List<List<Vector3>> lineList = new List<List<Vector3>>();
            List<Color> colorList = new List<Color>();
            List<double> widthList = new List<double>();

            ulong polyLineSegN = 0;

            for (ulong i = 0; i < numObjects; i++)
            {
                string visualName = _tcpHelper.GetDataString();
                Color color = getColor();
                double width = _tcpHelper.GetDataDouble();
                widthList.Add(width);
                colorList.Add(color);
                
                var npoints = _tcpHelper.GetDataUlong();
                if (npoints != 0)
                    polyLineSegN += npoints - 1;
                lineList.Add(new List<Vector3>());
                for (ulong j = 0; j < npoints; j++)
                    lineList.Last().Add(new Vector3((float)_tcpHelper.GetDataDouble(), (float)_tcpHelper.GetDataDouble(), (float)_tcpHelper.GetDataDouble()));
            }

            for (int i = 0; i < lineList.Count; i++)
            {
                var line = lineList[i];
                for (int j = 0; j < line.Count-1; j++)
                {
                    var box = _polylineMeshPool.AddMesh();
                    var pos1 = line[j];
                    var pos2 = line[j + 1];
                    
                    Quaternion q = new Quaternion(); 
                    q.SetLookRotation(new Vector3((float)(pos1[0]-pos2[0]), (float)(pos1[1]-pos2[1]), (float)(pos1[2]-pos2[2])), new Vector3(1,0,0));
                
                    ObjectController.SetTransform(box, new Vector3((float)(pos1[0]+pos2[0])/2.0f, (float)(pos1[1]+pos2[1])/2.0f, (float)(pos1[2]+pos2[2])/2.0f), q);

                    double length = Math.Sqrt((pos1[0] - pos2[0]) * (pos1[0] - pos2[0]) + (pos1[1] - pos2[1]) * (pos1[1] - pos2[1]) +
                                              (pos1[2] - pos2[2]) * (pos1[2] - pos2[2]));
                    box.GetComponent<Renderer>().material.SetColor(_colorString, colorList[(int)i]);
                    box.transform.localScale = new Vector3((float)widthList[i], (float)length, (float)widthList[i]);
                }
            }

            _polylineMeshPool.AllSet();

            // constraints
            _wireN = _tcpHelper.GetDataUlong();
            for (ulong i = 0; i < _wireN; i++)
            {
                double posX1 = _tcpHelper.GetDataDouble();
                double posY1 = _tcpHelper.GetDataDouble();
                double posZ1 = _tcpHelper.GetDataDouble();
                
                double posX2 = _tcpHelper.GetDataDouble();
                double posY2 = _tcpHelper.GetDataDouble();
                double posZ2 = _tcpHelper.GetDataDouble();
                
                GameObject localObject = GameObject.Find("wire"+i);
                
                Quaternion q = new Quaternion(); 
                q.SetLookRotation(new Vector3((float)(posX1-posX2), (float)(posY1-posY2), (float)(posZ1-posZ2)), new Vector3(1,0,0));
                
                ObjectController.SetTransform(localObject,
                    new Vector3((float)(posX1+posX2)/2.0f, (float)(posY1+posY2)/2.0f, (float)(posZ1+posZ2)/2.0f),
                    q);

                double length = Math.Sqrt((posX1 - posX2) * (posX1 - posX2) + (posY1 - posY2) * (posY1 - posY2) +
                                          (posZ1 - posZ2) * (posZ1 - posZ2));
                localObject.transform.localScale = new Vector3((float)0.005, (float)length, (float)0.005);
            }
            
            // external force
            ulong ExternalForceN = _tcpHelper.GetDataUlong();

            // create contact marker
            List<Tuple<Vector3, Vector3>> externalForceList = new List<Tuple<Vector3, Vector3>>();
            float forceMaxNorm = 0;

            for (ulong i = 0; i < ExternalForceN; i++)
            {
                double posX = _tcpHelper.GetDataDouble();
                double posY = _tcpHelper.GetDataDouble();
                double posZ = _tcpHelper.GetDataDouble();

                double forceX = _tcpHelper.GetDataDouble();
                double forceY = _tcpHelper.GetDataDouble();
                double forceZ = _tcpHelper.GetDataDouble();
                var force = new Vector3((float) forceX, (float) forceY, (float) forceZ);
                
                externalForceList.Add(new Tuple<Vector3, Vector3>(
                    new Vector3((float) posX, (float) posY, (float) posZ), force
                ));
                
                forceMaxNorm = Math.Max(forceMaxNorm, force.magnitude);
            }

            for (ulong i = 0; i < ExternalForceN; i++)
            {
                var forceMarker = _externalForceMeshPool.AddMesh();
                var contact = externalForceList[(int) i];
                _objectController.SetContactForceMarker(
                    forceMarker, contact.Item1, contact.Item2 / forceMaxNorm, Color.green,
                    _contactForceMarkerScale);
                forceMarker.SetActive(true);
            }

            _externalForceMeshPool.AllSet();

            forceMaxNorm = 0;
            // external torque
            var ExternalTorqueN = _tcpHelper.GetDataUlong();
            List<Tuple<Vector3, Vector3>> externalTorqueList = new List<Tuple<Vector3, Vector3>>();
            
            for (ulong i = 0; i < ExternalTorqueN; i++)
            {
                double posX = _tcpHelper.GetDataDouble();
                double posY = _tcpHelper.GetDataDouble();
                double posZ = _tcpHelper.GetDataDouble();

                double forceX = _tcpHelper.GetDataDouble();
                double forceY = _tcpHelper.GetDataDouble();
                double forceZ = _tcpHelper.GetDataDouble();
                var force = new Vector3((float) forceX, (float) forceY, (float) forceZ);
                
                externalTorqueList.Add(new Tuple<Vector3, Vector3>(
                    new Vector3((float) posX, (float) posY, (float) posZ), force
                ));
                
                forceMaxNorm = Math.Max(forceMaxNorm, force.magnitude);
            }
            
            for (ulong i = 0; i < ExternalTorqueN; i++)
            {
                var forceMarker = _externalTorqueMeshPool.AddMesh();
                var contact = externalTorqueList[(int) i];
                _objectController.SetContactForceMarker(
                    forceMarker, contact.Item1, contact.Item2 / forceMaxNorm, Color.yellow,
                    _contactForceMarkerScale);
                forceMarker.SetActive(true);
            }
            _externalTorqueMeshPool.AllSet();
            
            // get object details
            // objType == 0: SingleBody
            // objType == 1: ArticulatedSystem
            // objType == -1: None
            int objType = _tcpHelper.GetDataInt();

            if (objType == 0)
            {
                float posX = _tcpHelper.GetDataFloat();
                float posY = _tcpHelper.GetDataFloat();
                float posZ = _tcpHelper.GetDataFloat();
                float quatW = _tcpHelper.GetDataFloat();
                float quatX = _tcpHelper.GetDataFloat();
                float quatY = _tcpHelper.GetDataFloat();
                float quatZ = _tcpHelper.GetDataFloat();
                
                _singleBody.position = new Vector3(posX, posY, posZ);
                _singleBody.quat = new Vector4(quatW, quatX, quatY, quatZ);
                
                float linVelX = _tcpHelper.GetDataFloat();
                float linVelY = _tcpHelper.GetDataFloat();
                float linVelZ = _tcpHelper.GetDataFloat();
                float anglVelX = _tcpHelper.GetDataFloat();
                float anglVelY = _tcpHelper.GetDataFloat();
                float anglVelZ = _tcpHelper.GetDataFloat();
                
                _singleBody.linVel = new Vector3(linVelX, linVelY, linVelZ);
                _singleBody.angVel = new Vector3(anglVelX, anglVelY, anglVelZ);
            }
            else if (objType == 1)
            {
                int gcDim = _tcpHelper.GetDataInt();
                int gvDim = _tcpHelper.GetDataInt();
                int frameSize = _tcpHelper.GetDataInt();

                _articulatedSystem.ResetIfDifferent(objSelectedId, gcDim, gvDim);
                for (int i = 0; i < gcDim; i++)
                {
                    _articulatedSystem.gc[i] = _tcpHelper.GetDataFloat();
                }
                
                for (int i = 0; i < gvDim; i++)
                {
                    _articulatedSystem.gv[i] = _tcpHelper.GetDataFloat();
                }
                
                for (int i = 0; i < frameSize; i++)
                {
                    String frameName = _tcpHelper.GetDataString();
                    String frameType
                    _articulatedSystem.frameNames[i] = _tcpHelper.GetDataString();
                    _articulatedSystem.frameType[i] = _tcpHelper.GetDataInt();
                    
                    float posX = _tcpHelper.GetDataFloat();
                    float posY = _tcpHelper.GetDataFloat();
                    float posZ = _tcpHelper.GetDataFloat();
                    float quatW = _tcpHelper.GetDataFloat();
                    float quatX = _tcpHelper.GetDataFloat();
                    float quatY = _tcpHelper.GetDataFloat();
                    float quatZ = _tcpHelper.GetDataFloat();

                    _articulatedSystem.frames[i].transform.position = new Vector3(posX, posY, posZ);
                    _articulatedSystem.frames[i].transform.rotation = new Quaternion(quatX, quatY, quatZ, quatW);
                }
            }

            // Update object position done.
            // Go to visual object position update
            _clientStatus = ClientStatus.UpdateObjectPosition;

            return true;
        }

        private ServerMessageType ReadAndCheckServer(ClientMessageType type)
        {
            int counter = 0;
            int receivedData = 0;
            while (counter++ < 1 && receivedData == 0)
            {
                _tcpHelper.SetDataInt((int)type);
                
                if (_camera._selected)
                {
                    var nameSplited = _camera._selected.name.Split('/').ToList();
                    int objId = -1;
                    if (nameSplited.Count > 0)
                    {
                        objId = Int32.Parse(nameSplited[0]);
                        _tcpHelper.SetDataInt(objId);
                    }
                }
                else
                {
                    _tcpHelper.SetDataInt(-1);    
                }
                _tcpHelper.WriteData();
                receivedData = _tcpHelper.ReadData();
            }

            if (receivedData == 0)
                new RsuException("cannot connect");

            ServerStatus state = _tcpHelper.GetDataServerStatus();
            processServerRequest();

            if (state == ServerStatus.StatusTerminating)
            {
                new RsuException("Server is terminating");
                return ServerMessageType.Reset;
            }
            else if (state == ServerStatus.StatusHibernating)
            {
                return ServerMessageType.Reset;
            }
            
            return _tcpHelper.GetDataServerMessageType();
        }

        private bool UpdateContacts()
        {
            if (ReadAndCheckServer(ClientMessageType.RequestContactInfos) != ServerMessageType.ContactInfoUpdate)
                return false;
            
            ulong numContacts = _tcpHelper.GetDataUlong();

            // create contact marker
            List<Tuple<Vector3, Vector3>> contactList = new List<Tuple<Vector3, Vector3>>();
            float forceMaxNorm = 0;

            for (ulong i = 0; i < numContacts; i++)
            {
                double posX = _tcpHelper.GetDataDouble();
                double posY = _tcpHelper.GetDataDouble();
                double posZ = _tcpHelper.GetDataDouble();

                double forceX = _tcpHelper.GetDataDouble();
                double forceY = _tcpHelper.GetDataDouble();
                double forceZ = _tcpHelper.GetDataDouble();
                var force = new Vector3((float) forceX, (float) forceY, (float) forceZ);
                
                contactList.Add(new Tuple<Vector3, Vector3>(
                    new Vector3((float) posX, (float) posY, (float) posZ), force
                ));
                
                forceMaxNorm = Math.Max(forceMaxNorm, force.magnitude);
            }
            
            for (ulong i = 0; i < numContacts; i++)
            {
                var forceMaker = _contactForceMeshPool.AddMesh();
                var posMarker = _contactPointMeshPool.AddMesh();
                
                var contact = contactList[(int) i];

                if (contact.Item2.magnitude > 0)
                {
                    if (_showContactPoints)
                    {
                        _objectController.SetContactMarker(
                            posMarker, contact.Item1, Color.red, _contactPointMarkerScale);
                        posMarker.SetActive(true);
                    }

                    if (_showContactForces)
                    {
                        _objectController.SetContactForceMarker(
                            forceMaker, contact.Item1, contact.Item2 / forceMaxNorm, Color.blue,
                            _contactForceMarkerScale);
                        forceMaker.SetActive(true);
                    }
                }
                else
                {
                    forceMaker.SetActive(false);
                    posMarker.SetActive(false);
                }
            }
            
            return true;
        }
        
        void OnApplicationQuit()
        {
            // close tcp client
            _tcpHelper.CloseConnection();
            
            // save preference
            _loader.SaveToPref();
        }

        public void ShowOrHideObjects()
        {
            // Visual body
            foreach (var obj in GameObject.FindGameObjectsWithTag(VisualTag.Visual))
            {
                foreach (var collider in obj.GetComponentsInChildren<Collider>())
                    collider.enabled = _showVisualBody;
                
                foreach (var renderer in obj.GetComponentsInChildren<Renderer>())
                    renderer.enabled = _showVisualBody;
            }

            // Collision body
            foreach (var obj in GameObject.FindGameObjectsWithTag(VisualTag.Collision))
            {
                foreach (var col in obj.GetComponentsInChildren<Collider>())
                    col.enabled = _showCollisionBody;
                
                foreach (var ren in obj.GetComponentsInChildren<Renderer>())
                    ren.enabled = _showCollisionBody;
            }
            
            // Articulated System Collision body
            foreach (var obj in GameObject.FindGameObjectsWithTag(VisualTag.ArticulatedSystemCollision))
            {
                foreach (var col in obj.GetComponentsInChildren<Collider>())
                    col.enabled = _showCollisionBody || _showVisualBody;
                
                foreach (var ren in obj.GetComponentsInChildren<Renderer>())
                    ren.enabled = _showCollisionBody;
            }
            
            // Body frames
            foreach (var obj in GameObject.FindGameObjectsWithTag(VisualTag.Frame))
                foreach (var renderer in obj.GetComponentsInChildren<Renderer>())
                    renderer.enabled = _showBodyFrames;
        }

        //**************************************************************************************************************
        //  Getter and Setters 
        //**************************************************************************************************************
        
        public bool ShowVisualBody
        {
            get => _showVisualBody;
            set => _showVisualBody = value;
        }

        public bool ShowCollisionBody
        {
            get => _showCollisionBody;
            set => _showCollisionBody = value;
        }

        public bool ShowContactPoints
        {
            get => _showContactPoints;
            set => _showContactPoints = value;
        }

        public bool ShowContactForces
        {
            get => _showContactForces;
            set => _showContactForces = value;
        }

        public bool ShowBodyFrames
        {
            get => _showBodyFrames;
            set => _showBodyFrames = value;
        }

        public float ContactPointMarkerScale
        {
            get => _contactPointMarkerScale;
            set => _contactPointMarkerScale = value;
        }

        public float ContactForceMarkerScale
        {
            get => _contactForceMarkerScale;
            set => _contactForceMarkerScale = value;
        }

        public float BodyFrameMarkerScale
        {
            get => _bodyFrameMarkerScale;
            set
            {
                _bodyFrameMarkerScale = value;
                foreach (var obj in GameObject.FindGameObjectsWithTag(VisualTag.Frame))
                {
                    obj.transform.localScale = new Vector3(0.03f * value, 0.03f * value, 0.1f * value);
                }
            }
        }

        public string TcpAddress
        {
            get => _tcpHelper.TcpAddress;
            set => _tcpHelper.TcpAddress = value;
        }

        public int TcpPort
        {
            get => _tcpHelper.TcpPort;
            set => _tcpHelper.TcpPort = value;
        }


        public bool TcpConnected
        {
            get => _tcpHelper.Connected;
        }

        public bool IsServerHibernating
        {
            get
            {
                return _clientStatus == ClientStatus.Idle && _tcpHelper.DataAvailable;
            }
        }

        public ResourceLoader ResourceLoader
        {
            get { return _loader; }
        }

        private void setColorFromString(GameObject gameObject, string mat)
        {
            Color color = StringToColor(mat.ToLower());
            setColor(gameObject, color);
        }

        private void setColor(GameObject gameObject, Color color)
        {
            MeshRenderer[] children = gameObject.GetComponentsInChildren<MeshRenderer>();
            for (int i = 0; i < children.Length; ++i)
            {
                if (color.a > 0.99f)
                {
                    children[i].material = _whiteMaterial;
                    children[i].material.SetColor(_colorString, color);
                }
                else
                {
                    children[i].material = _transparentMaterial;
                    children[i].material.SetColor(_colorString, color);    
                }
            }
        }
        
        private Color StringToColor(String mat)
        {
            List<String> list = mat.Split(',').ToList();
            Color color;
            if (list.Count == 1)
                if (ColorUtility.TryParseHtmlString(mat, out color))
                    return color;
            
            if (list.Count == 3)
                return new Color(float.Parse(list[0]), float.Parse(list[1]), float.Parse(list[2]));
            
            if (list.Count == 4)
                return new Color(float.Parse(list[0]), float.Parse(list[1]), float.Parse(list[2]), float.Parse(list[3]));

            return Color.gray;
        }
    }
}