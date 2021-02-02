using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshPool
{
    private string _name;
    private GameObject _mesh;
    private int _size = 0;
    private int _index = 0;
    private string _tag;
    private GameObject _root;
    private Shader _standardShader;

    public MeshPool(string name, GameObject mesh, GameObject root, string tag, Shader standardShader)
    {
        _mesh = mesh;
        mesh.SetActive(false);
        _name = name;
        _tag = tag;
        _root = root;
        _standardShader = standardShader;
    }

    public GameObject AddMesh()
    {
        if (_index == _size)
        {
            GameObject go = GameObject.Instantiate(_mesh);
            go.name = _name + _index;
            go.tag = _tag;
            go.transform.SetParent(_root.transform, true);
            go.GetComponentInChildren<MeshRenderer>().material.shader = _standardShader;
            _size = _index + 1;
            go.SetActive(true);
            _index++;
            return go;
        }
        else
        {
            GameObject go = _root.transform.Find(_name + _index.ToString()).gameObject;
            _index++;
            go.SetActive(true);
            return go;
        }
    }

    public void AllSet()
    {
        for (int i = _index; i < _size; i++)
        {
            var go = _root.transform.Find(_name + i.ToString()).gameObject;
            go.SetActive(false);
        }
        _index = 0;
    }

    public void Clear()
    {
        for (int i = 0; i < _size; i++)
        {
            var go = _root.transform.Find(_name + i.ToString()).gameObject;
            Object.Destroy(go);
        }
        _size = 0;
        _index = 0;
    }
}
