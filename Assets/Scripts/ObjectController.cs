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
using System.Xml;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Timeline;
using UnityMeshImporter;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace raisimUnity
{
    public enum MeshUpAxis: int
    {
        YUp = 0,    // this is the default for most cases
        ZUp,
        XUp,
    }
    
    public class ObjectController
    {
        // Cache
        private GameObject _objectCache;
        private Dictionary<string, Tuple<GameObject, MeshUpAxis>> _meshCache;

        private GameObject _arrowMesh;
        private Shader _standardShader;

        private string _colorString;

        public ObjectController(GameObject cache)
        {
            _objectCache = cache;
            _arrowMesh = Resources.Load("others/arrow") as GameObject;

            _meshCache = new Dictionary<string, Tuple<GameObject, MeshUpAxis>>();

            if (GraphicsSettings.renderPipelineAsset is HDRenderPipelineAsset)
            {
                _standardShader = Shader.Find("HDRP/Lit");
                _colorString = "_BaseColor";
            }
            else
            {
                _standardShader = Shader.Find("Standard");
                _colorString = "_Color";
            }

        }

        public void ClearCache()
        {
            _meshCache.Clear();
            foreach (Transform objT in _objectCache.transform)
            {
                GameObject.Destroy(objT.gameObject);
            }
        }

        public GameObject CreateRootObject(GameObject root, string name)
        {
            var rootObj = new GameObject(name);
            rootObj.transform.SetParent(root.transform, false);
            
            // Frame
            // var bodyFrame = new GameObject("frame");
            // bodyFrame.transform.SetParent(rootObj.transform, false);
            //
            // var xAxisMarker = GameObject.Instantiate(_arrowMesh);
            // xAxisMarker.GetComponentInChildren<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            // xAxisMarker.transform.SetParent(bodyFrame.transform, false);
            // xAxisMarker.tag = "frame";
            // xAxisMarker.name = "frameX";
            // xAxisMarker.transform.localRotation = Quaternion.LookRotation(new Vector3(-1, 0, 0));
            // xAxisMarker.transform.localScale = new Vector3(0.03f, 0.03f, 0.1f);
            // xAxisMarker.GetComponentInChildren<Renderer>().material.SetColor(_colorString, Color.red);
            //
            // var yAxisMarker = GameObject.Instantiate(_arrowMesh);
            // yAxisMarker.GetComponentInChildren<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            // yAxisMarker.transform.SetParent(bodyFrame.transform, false);
            // yAxisMarker.tag = "frame";
            // yAxisMarker.name = "frameY";
            // yAxisMarker.transform.localRotation = Quaternion.LookRotation(new Vector3(0, 1, 0));
            // yAxisMarker.transform.localScale = new Vector3(0.03f, 0.03f, 0.1f);
            // yAxisMarker.GetComponentInChildren<Renderer>().material.SetColor(_colorString, Color.blue);
            //
            // var zAxisMarker = GameObject.Instantiate(_arrowMesh);
            // zAxisMarker.GetComponentInChildren<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            // zAxisMarker.transform.SetParent(bodyFrame.transform, false);
            // zAxisMarker.tag = "frame";
            // zAxisMarker.name = "frameZ";
            // zAxisMarker.transform.localRotation = Quaternion.LookRotation(new Vector3(0, 0, -1));
            // zAxisMarker.transform.localScale = new Vector3(0.03f, 0.03f, 0.1f);
            // zAxisMarker.GetComponentInChildren<Renderer>().material.SetColor(_colorString, Color.green);
            
            return rootObj;
        }

        public GameObject CreateSphere(GameObject root, float radius)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.SetParent(root.transform, true);
            sphere.transform.localScale = new Vector3(radius*2.0f, radius*2.0f, radius*2.0f);
            sphere.GetComponentInChildren<Renderer>().material.shader = _standardShader;
            return sphere;
        }

        public GameObject CreateBox(GameObject root, float sx, float sy, float sz)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.transform.SetParent(root.transform, true);
            box.transform.localScale = new Vector3(sx, sz, sy);
            box.GetComponentInChildren<Renderer>().material.shader = _standardShader;
            return box;
        }

        public GameObject CreateCylinder(GameObject root, float radius, float height)
        {
            var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.transform.SetParent(root.transform, true);
            cylinder.transform.localScale = new Vector3(radius*2f, height*0.5f, radius*2f);
            cylinder.GetComponentInChildren<Renderer>().material.shader = _standardShader;
            return cylinder;
        }

        public GameObject CreateCapsule(GameObject root, float radius, float height)
        {
            // Note.
            // raisim geometry of capsule: http://ode.org/wiki/index.php?title=Manual#Capsule_Class
            // unity geometry of capsule: https://docs.unity3d.com/Manual/class-CapsuleCollider.html
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.transform.SetParent(root.transform, true);
            capsule.transform.localScale = new Vector3(radius*2f, height*0.5f+radius, radius*2f);
            capsule.GetComponentInChildren<Renderer>().material.shader = _standardShader;
            return capsule;
        }
        
        public GameObject CreateArrow(GameObject root, float radius, float height)
        {
            var arrow = GameObject.Instantiate(_arrowMesh);
            arrow.transform.SetParent(root.transform, true);
            arrow.transform.localScale = new Vector3(radius, radius, height);
            arrow.GetComponentInChildren<Renderer>().material.shader = _standardShader;
            return arrow;
        }

        public GameObject CreateHalfSpace(GameObject root, float height)
        {
            var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.transform.SetParent(root.transform, true);
            plane.name = "halfSpace";
            plane.transform.localPosition = new Vector3(0, height, 0);
            plane.transform.localScale = new Vector3(15, 1, 15);
            GameObject.DestroyImmediate(plane.GetComponent<Collider>());
            return plane;
        }
        
        public GameObject CreateTerrain(GameObject root, 
            ulong numSampleX, float sizeX, float centerX, ulong numSampleY, float sizeY, float centerY, 
            float[,] heights, bool recomputeNormal = true)
        {
            // Note that we create terrain with mesh since unity support only square size height map

            List<Vector3> vertices = new List<Vector3>();
            List<int> indices = new List<int>();
            List<Vector2> uvs = new List<Vector2>();
            List<Vector3> normals = new List<Vector3>();

            float gridSizeX = sizeX / (numSampleX - 1);
            float gridSizeY = sizeY / (numSampleY - 1);
            float gridStartX = centerX - sizeX * 0.5f;
            float gridStartY = centerY - sizeY * 0.5f;
            
            // vertices
            for (ulong i = 0; i < numSampleY-1; i++)
            {
                for (ulong j = 0; j < numSampleX-1; j++)
                {
                    // (x, y) = (j, i)
                    vertices.Add(ConvertRs2Unity(
                        gridStartX + j * gridSizeX,
                        gridStartY + i * gridSizeY,
                        heights[i, j]
                    ));
                    
                    // (x, y) = (j+1, i+1)
                    vertices.Add(ConvertRs2Unity(
                        gridStartX + (j + 1) * gridSizeX,
                        gridStartY + (i + 1) * gridSizeY,
                        heights[i + 1, j + 1]
                    ));
                    
                    // (x, y) = (j+1, i)
                    vertices.Add(ConvertRs2Unity(
                        gridStartX + (j + 1) * gridSizeX,
                        gridStartY + i * gridSizeY,
                        heights[i, j + 1]
                    ));

                    // (x, y) = (j, i)
                    vertices.Add(ConvertRs2Unity(
                        gridStartX + j * gridSizeX,
                        gridStartY + i * gridSizeY,
                        heights[i, j]
                    ));
                    
                    // (x, y) = (j, i+1)
                    vertices.Add(ConvertRs2Unity(
                        gridStartX + j * gridSizeX,
                        gridStartY + (i + 1) * gridSizeY,
                        heights[i + 1, j]
                    ));
                    
                    // (x, y) = (j+1, i+1)
                    vertices.Add(ConvertRs2Unity(
                        gridStartX + (j + 1) * gridSizeX,
                        gridStartY + (i + 1) * gridSizeY,
                        heights[i + 1, j + 1]
                    ));
                }
            }
            
            // triangles
            for (int i = 0; i < vertices.Count; i++) {
                indices.Add(i);
            }

            if (!recomputeNormal)
            {
                // normals
                for (int i = 0; i < vertices.Count; i += 3)
                {
                    Vector3 point1 = ConvertUnity2Rs(vertices[i].x, vertices[i].y, vertices[i].z);
                    Vector3 point2 = ConvertUnity2Rs(vertices[i + 1].x, vertices[i + 1].y, vertices[i + 1].z);
                    Vector3 point3 = ConvertUnity2Rs(vertices[i + 2].x, vertices[i + 2].y, vertices[i + 2].z);

                    Vector3 diff1 = point2 - point1;
                    Vector3 diff2 = point3 - point2;
                    Vector3 norm = Vector3.Cross(diff1, diff2);
                    norm = Vector3.Normalize(norm);
                    norm = ConvertRs2Unity(norm.x, norm.y, norm.z);

                    normals.Add(norm);
                    normals.Add(norm);
                    normals.Add(norm);
                }
            }

            // uvs
            for (ulong i = 0; i < numSampleY-1; i++)
            {
                for (ulong j = 0; j < numSampleX-1; j++)
                {
                    // (x, y) = (j, i)
                    uvs.Add(new Vector2(
                        (j / (float)numSampleX),
                        (i / (float)numSampleY)
                    ));

                    // (x, y) = (j+1, i+1)
                    uvs.Add(new Vector2(
                        ((j + 1) / (float)numSampleX),
                        ((i + 1) / (float)numSampleY)
                    ));
                    
                    // (x, y) = (j+1, i)
                    uvs.Add(new Vector2(
                        ((j + 1) / (float)numSampleX),
                        (i / (float)numSampleY)
                    ));

                    // (x, y) = (j, i)
                    uvs.Add(new Vector2(
                        (j / (float)numSampleX),
                        (i / (float)numSampleY)
                    ));
                                      
                    // (x, y) = (j, i+1)
                    uvs.Add(new Vector2(
                        (j / (float)numSampleX),
                        ((i + 1) / (float)numSampleY)
                    ));
                    
                    // (x, y) = (j+1, i+1)
                    uvs.Add(new Vector2(
                        ((j + 1) / (float)numSampleX),
                        ((i + 1) / (float)numSampleY)
                    ));
                }
            }

            Mesh mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.vertices = vertices.ToArray();
            mesh.triangles = indices.ToArray();
            mesh.uv = uvs.ToArray();
            if (!recomputeNormal) mesh.normals = normals.ToArray();
            else mesh.RecalculateNormals();

            var terrain = new GameObject("terrain");
            terrain.transform.SetParent(root.transform, true);
            terrain.AddComponent<MeshFilter>();
            terrain.AddComponent<MeshRenderer>();
            terrain.GetComponent<MeshFilter>().mesh = mesh;

            return terrain;
        }

        public GameObject CreateMesh(GameObject root, string meshFile, float sx, float sy, float sz, bool flipYz=false)
        {
            // meshFile is file name without file extension related to Resources directory
            // sx, sy, sz is scale 
            MeshUpAxis meshUpAxis = MeshUpAxis.YUp;
            GameObject mesh = null;

            if (_meshCache.ContainsKey(meshFile) && _meshCache[meshFile] != null) {}
            else
            {
                if (!File.Exists(meshFile))
                {
                    new RsuException(new Exception(),"Cannot find mesh file: " + meshFile);
                }

                string fileExtension = Path.GetExtension(meshFile);
                var loadedMesh = MeshImporter.Load(meshFile);
                
                // check up axis (for dae)
                if (fileExtension == ".dae")
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    if (xmlDoc != null)
                    {
                        xmlDoc.Load(meshFile);
                        var upAxisNode = xmlDoc.DocumentElement.SelectSingleNode("//*[contains(local-name(), 'up_axis')]").LastChild;

                        if (upAxisNode != null && !string.IsNullOrEmpty(upAxisNode.Value))
                        {
                            if (upAxisNode.Value == "Z_UP")
                                meshUpAxis = MeshUpAxis.ZUp;
                            else if (upAxisNode.Value == "Y_UP")
                                meshUpAxis = MeshUpAxis.YUp;
                            else
                                meshUpAxis = MeshUpAxis.XUp;
                        }
                    }
                }
                
                // save to cache
                loadedMesh.name = meshFile;
                loadedMesh.transform.SetParent(_objectCache.transform, false);
                loadedMesh.SetActive(false);
                loadedMesh.GetComponentInChildren<Renderer>().material.shader = _standardShader;
                _meshCache.Add(meshFile, new Tuple<GameObject, MeshUpAxis>(loadedMesh, meshUpAxis));
            }

            var cachedMesh = _meshCache[meshFile];
            mesh = GameObject.Instantiate(cachedMesh.Item1);
            if (mesh == null)
            {
                new RsuException(new Exception(),"Cannot load mesh file: " + meshFile);
            }
            
            mesh.SetActive(true);
            mesh.name = "mesh";
            mesh.transform.SetParent(root.transform, false);
            Vector3 originalScale = mesh.transform.localScale;
            mesh.transform.localScale = new Vector3((float)sx * originalScale[0], (float)sy * originalScale[1], (float)sz * originalScale[2]);

            if(cachedMesh.Item2 == MeshUpAxis.ZUp) {}
            else if(cachedMesh.Item2 == MeshUpAxis.YUp)
                mesh.transform.localRotation = new Quaternion(-0.7071f, 0, 0, 0.7071f) * mesh.transform.localRotation;
            else if(cachedMesh.Item2 == MeshUpAxis.XUp)
                mesh.transform.localRotation = new Quaternion(0, 0, 0.7071f, 0.7071f) * mesh.transform.localRotation;
            
            // add collider to children
            foreach (Transform children in mesh.transform)
            {
                children.gameObject.AddComponent<MeshCollider>();
            }
            return mesh;
        }

        public GameObject SetContactMarker(GameObject marker, Vector3 rsPos, Color color, float markerScale = 1)
        {
            markerScale = Math.Max(Math.Min(10.0f, markerScale), 0.1f);

            marker.transform.localScale = new Vector3(0.06f * markerScale, 0.06f * markerScale, 0.06f * markerScale);
            marker.GetComponent<Collider>().enabled = false;

            marker.tag = "contact";
            Quaternion q = new Quaternion(0, 0, 0, 1);
            SetTransform(marker, rsPos, q);
            marker.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            marker.GetComponent<Renderer>().material.SetColor(_colorString, color);
            return marker;
        }
        
        public GameObject SetContactForceMarker(GameObject marker, Vector3 rsPos, Vector3 force, Color color, float markerScale = 1)
        {
            markerScale = Math.Max(Math.Min(10.0f, markerScale), 0.1f);
            marker.GetComponentInChildren<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            marker.tag = "contact";
            
            Vector3 axis = new Vector3(-force.x, force.z, -force.y);
            axis.Normalize();
            
            marker.transform.localPosition = new Vector3(-rsPos.x, rsPos.z, -rsPos.y);
            Quaternion q = new Quaternion(); 
            q.SetLookRotation(axis, new Vector3(1,0,0));
            marker.transform.localRotation = q;
            marker.transform.localScale = new Vector3(
                0.3f * markerScale * force.magnitude, 
                0.3f * markerScale * force.magnitude,
                1.0f * markerScale * force.magnitude
            );
            marker.GetComponentInChildren<Renderer>().material.SetColor(_colorString, color);
            return marker;
        }
        
        public static void SetTransform(GameObject obj, Vector3 rsPos, Quaternion rsQuat)
        {
            // rsPos is position in RaiSim
            // rsQuat is quaternion in RaiSim
            
            var yaxis = rsQuat * new Vector3(0, 1.0f, 0);
            var zaxis = rsQuat * new Vector3(0, 0, 1.0f);

            Quaternion q = new Quaternion(0, 0, 0, 1);
            q.SetLookRotation(
                new Vector3(yaxis[0], -yaxis[2], yaxis[1]),
                new Vector3(-zaxis[0], zaxis[2], -zaxis[1])
            );

            obj.transform.localPosition = new Vector3(-rsPos.x, rsPos.z, -rsPos.y);
            obj.transform.localRotation = q;
        }
        
        private static Vector3 ConvertRs2Unity(float rx, float ry, float rz)
        {
            return new Vector3(-rx, rz, -ry);
        }

        private static Vector3 ConvertUnity2Rs(float ux, float uy, float uz)
        {
            return new Vector3(-ux, -uz, uy);
        }
    }
}