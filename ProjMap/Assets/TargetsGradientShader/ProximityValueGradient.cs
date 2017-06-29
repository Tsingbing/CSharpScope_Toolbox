using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ProximityValueGradient : MonoBehaviour
{
    public Vector4[] _targetsPositionsArray; //must fit values 
    public float[] _targetsValuesArray; //must be finite int for shader 
    Renderer _render;
    void Update()
    {
        _render = gameObject.GetComponent<Renderer>();
        for (int i = 0; i < _targetsValuesArray.Length; i++)
        {
            _targetsPositionsArray[i] = transform.GetChild(i).transform.position;
        }

        //pass two arrays to shader, with position of targets and vaule of each
        _render.sharedMaterial.SetFloatArray("_TargetValues", _targetsValuesArray);
        _render.sharedMaterial.SetVectorArray("_TargetPositions", _targetsPositionsArray);
    }
}