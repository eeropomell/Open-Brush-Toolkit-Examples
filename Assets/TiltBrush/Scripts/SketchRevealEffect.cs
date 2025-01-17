﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;


[ExecuteAlways]
public class SketchRevealEffect : MonoBehaviour
{
    [HideInInspector]
    public int totalVertexCount = 0;

    [SerializeField] private List<Stroke> allStrokes;

    private float sketchStartTime = 0f;
    private float sketchEndTime = 0f;

    public float totalSketchTime = 0f;

    private bool isFirstFrame = true;

    public bool loop = false;
    private Coroutine CurrentEffectCoroutine;

    private float prevT;
    [Tooltip("t = 0 sketch is 0% visible, t = .5 sketch is 50% visible, t = 1 sketch is 100% visible, etc. Note: this is only enabled in edit mode")]
    [Range(0, 1)] public float t;

    private static readonly int PropertyIdClipEnd = Shader.PropertyToID("_ClipEnd");
    private static readonly int PropertyIdClipStart = Shader.PropertyToID("_ClipStart");

    // setting _ClipEnd to this value makes the brush fragment shader discard all vertices
    private const float CLIPEND_HIDE_ALL_VALUE = .0001f;

    [Serializable]
    public struct Stroke
    {
        public MeshFilter mf;
        public MeshRenderer mr;
    };

    void Awake()
    {

        totalSketchTime = 0f;
        InitStrokesArray();

        // totalSketchTime is in seconds
        totalSketchTime = (GetSketchLastTimestamp() - GetSketchFirstTimestamp()) / 1000;
        sketchStartTime = GetSketchFirstTimestamp();
        sketchEndTime = GetSketchLastTimestamp();

        prevT = -1f;
    }

    // called either from Awake or UpdateStrokesArray
    void InitStrokesArray()
    {
        if (allStrokes != null && allStrokes.Count > 0)
        {
            allStrokes.Clear();
        }
        else
        {
            allStrokes = new List<Stroke>();
        }

        totalVertexCount = 0;

        List<MeshFilter> allMeshFilters = GetComponentsInChildren<MeshFilter>().ToList();
        foreach (MeshFilter mf in allMeshFilters)
        {
            MeshRenderer meshRenderer = mf.GetComponent<MeshRenderer>();

            Material mat_ = meshRenderer.sharedMaterial;

            meshRenderer.material = new Material(mat_);
            // this will hide the material at the beginning
            meshRenderer.sharedMaterial.SetFloat(PropertyIdClipEnd, CLIPEND_HIDE_ALL_VALUE);
            meshRenderer.sharedMaterial.EnableKeyword("SHADER_SCRIPTING_ON");

            totalVertexCount += mf.sharedMesh.vertexCount;

            Stroke stroke = new Stroke();
            stroke.mf = mf;
            stroke.mr = meshRenderer;
            allStrokes.Add(stroke);
        }

        OrderStrokes();
    }

    public float CalculateTotalSketchTime()
    {
        return (GetSketchLastTimestamp() - GetSketchFirstTimestamp()) / 1000;
    }

    private float GetSketchFirstTimestamp()
    {
        if (allStrokes.Count < 1)
        {
            Debug.LogError("allStrokes is empty");
            return 0f;
        }

        Mesh mesh = allStrokes[0].mf.sharedMesh;
        return mesh.uv3[0].x;
    }

    private float GetSketchLastTimestamp()
    {
        if (allStrokes.Count < 1)
        {
            Debug.LogError("allStrokes is empty");
            return 0f;
        }

        Mesh mesh = allStrokes[allStrokes.Count - 1].mf.sharedMesh;
        return mesh.uv3[0].y;
    }

    private float GetStrokeID(Stroke stroke)
    {
        Mesh mesh = stroke.mf.sharedMesh;
        if (mesh == null)
        {
            return -1;
        }
        else if (mesh.uv3[0].x == 0.0f)
        {
            Debug.LogError("uv3[0].x == 0 for: " + mesh.name);
            return -1;
        }

        Vector2[] uv2 = mesh.uv3;
        Assert.AreNotEqual(0.0, uv2[0].x);
        return uv2[0].x;
    }

    public void OrderStrokes()
    {
        allStrokes.Sort((x, y) =>
        {
            float xStrokeID = GetStrokeID(x);
            float yStrokeID = GetStrokeID(y);
            if (AreFloatsEqual(xStrokeID, yStrokeID))
            {
                return 0;
            }

            if (xStrokeID < yStrokeID)
            {
                return -1;
            }
            else if (xStrokeID > yStrokeID)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        });
    }

    private static bool AreFloatsEqual(float a, float b, float epsilon = 0.00001f)
    {
        return Mathf.Abs(a - b) < epsilon;
    }

    public void UpdateStrokesArray()
    {
        InitStrokesArray();
    }

    public void ShowVerticesRange(int start, int end)
    {
        if (allStrokes == null || allStrokes.Count < 0)
        {
            return;
        }

        int startStrokeIndex = allStrokes.IndexOf(GetStrokeFromGlobalIndex(start));
        int endStrokeIndex = allStrokes.IndexOf(GetStrokeFromGlobalIndex(end));

        if (startStrokeIndex == -1 || endStrokeIndex == -1)
        {
            return;
        }

        int startStrokeLocalVertexIndex = GetLocalIndex(start);
        int endStrokeLocalVertexIndex = GetLocalIndex(end);

        allStrokes[startStrokeIndex].mr.sharedMaterial.SetFloat(PropertyIdClipStart,startStrokeLocalVertexIndex);
        allStrokes[endStrokeIndex].mr.sharedMaterial.SetFloat(PropertyIdClipEnd, endStrokeLocalVertexIndex);

        // two cases:
        // 1. vertices 'start' and 'end' belong to different stroke meshes
        // 2. vertices 'start' and 'end' belong to the same stroke mesh
        //
        // in 1st case, we have to make sure that the starting stroke shows all of its vertices
        // in 2nd case, we have to do SetFloat(ClipEnd,localEndIndex);

        if (startStrokeIndex != endStrokeIndex)
        {
            allStrokes[endStrokeIndex].mr.sharedMaterial.SetFloat(PropertyIdClipStart, 0);
            // this makes sure that the starting stroke is 100% visible
            allStrokes[startStrokeIndex].mr.sharedMaterial.SetFloat(PropertyIdClipEnd, end);
        }
        else
        {
            allStrokes[startStrokeIndex].mr.sharedMaterial.SetFloat(PropertyIdClipEnd,startStrokeLocalVertexIndex);
        }

        // this loop handles the strokes between the starting and ending strokes we want to show
        for (int i = startStrokeIndex+1; i < endStrokeIndex; i++)
        {
            MeshFilter mf = allStrokes[i].mf;
            Material material = allStrokes[i].mr.sharedMaterial;
            // show all vertices
            material.SetFloat(PropertyIdClipStart, 0);
            material.SetFloat(PropertyIdClipEnd, mf.sharedMesh.vertexCount);
        }
    }

    // logic is similar to ShowVerticesRange except it hides the strokes
    public void HideVerticesRange(int start, int end)
    {
        if (allStrokes == null || allStrokes.Count < 0)
        {
            return;
        }

        int startStrokeIndex = allStrokes.IndexOf(GetStrokeFromGlobalIndex(start));
        int endStrokeIndex = allStrokes.IndexOf(GetStrokeFromGlobalIndex(end));

        if (startStrokeIndex == -1 || endStrokeIndex == -1)
        {
            return;
        }

        int startStrokeLocalVertexIndex = GetLocalIndex(start);

        // todo: this is a temp fix to the first stroke being visible when calling HideVerticesRange(0,totalVertexCount-1)
        if (startStrokeLocalVertexIndex == 0)
        {
            allStrokes[startStrokeIndex].mr.sharedMaterial.SetFloat(PropertyIdClipStart,allStrokes[startStrokeIndex].mf.sharedMesh.vertexCount);
            allStrokes[startStrokeIndex].mr.sharedMaterial.SetFloat(PropertyIdClipEnd, allStrokes[startStrokeIndex].mf.sharedMesh.vertexCount);
        }
        else
        {
            allStrokes[startStrokeIndex].mr.sharedMaterial.SetFloat(PropertyIdClipStart,0);
            allStrokes[startStrokeIndex].mr.sharedMaterial.SetFloat(PropertyIdClipEnd, startStrokeLocalVertexIndex);
        }

        for (int i = startStrokeIndex + 1; i < endStrokeIndex; i++)
        {
            try
            {
                MeshFilter mf = allStrokes[i].mf;
                Material material = allStrokes[i].mr.sharedMaterial;
                // hide all vertices
                material.SetFloat(PropertyIdClipStart,mf.sharedMesh.vertexCount);
                material.SetFloat(PropertyIdClipEnd, mf.sharedMesh.vertexCount);
            }             catch (MissingReferenceException exception)
            {
                ; // this exception is caused if the mesh is deleted
                // it's expected to happen and doesn't need to be logged
            }
            catch (Exception exception)
            {
                Debug.LogError($"{exception}");
            }

        }

        int endStrokeLocalVertexIndex = GetLocalIndex(end);
        allStrokes[endStrokeIndex].mr.sharedMaterial.SetFloat(PropertyIdClipStart, endStrokeLocalVertexIndex);
        allStrokes[endStrokeIndex].mr.sharedMaterial.SetFloat(PropertyIdClipEnd, allStrokes[endStrokeIndex].mf.sharedMesh.vertexCount);
    }

    private int GetLocalIndex(int globalIndex)
    {
        int cumulativeVertexCount = 0;

        for (int i = 0; i < allStrokes.Count; i++)
        {
            try
            {
                Mesh mesh = allStrokes[i].mf.sharedMesh;
                if (globalIndex <= cumulativeVertexCount + mesh.vertexCount - 1)
                {
                    return globalIndex - cumulativeVertexCount;
                }
                cumulativeVertexCount += mesh.vertexCount;
            } catch (MissingReferenceException exception)
            {
                ; // this exception is caused if the mesh is deleted
                // it's expected to happen and doesn't need to be logged
            }
            catch (Exception exception)
            {
                Debug.LogError($"{exception}");
            }

        }

        return 0;
    }

    private Stroke GetStrokeFromGlobalIndex(int globalIndex)
    {
        int cumulativeVertexCount = 0;

        for (int i = 0; i < allStrokes.Count; i++)
        {
            Mesh mesh = allStrokes[i].mf.sharedMesh;
            if (globalIndex <= cumulativeVertexCount + mesh.vertexCount - 1)
            {
                return allStrokes[i];
            }

            cumulativeVertexCount += mesh.vertexCount;
        }

        return default;
    }


    private IEnumerator PlaySketchRevealEffectCoroutine(bool playInReverse = false)
    {
        float elapsedTime = 0f;
        while (true)
        {

            float normalizedTime = elapsedTime / totalSketchTime;

            if (playInReverse)
            {
                normalizedTime = 1 - normalizedTime;
            }

            int vertex = (int)(normalizedTime * (totalVertexCount-1));

            if (vertex == 0)
            {
                HideVerticesRange(0,totalVertexCount-1);
            }
            else if (vertex == totalVertexCount-1)
            {
                ShowVerticesRange(0,totalVertexCount-1);
            }
            else
            {
                ShowVerticesRange(0, vertex);
                HideVerticesRange(vertex + 1, (int)totalVertexCount-1);
            }

            elapsedTime += Time.deltaTime;

            if (elapsedTime > totalSketchTime)
            {
                if (loop)
                {
                    elapsedTime = 0f;
                }
                else
                {
                    // the effect ends
                    CurrentEffectCoroutine = null;
                    break;
                }
            }
            yield return null;
        }
    }


    public void PlaySketchRevealEffect(bool playInReverse = false)
    {
        if (CurrentEffectCoroutine != null)
        {
            StopCoroutine(CurrentEffectCoroutine);
        }

        CurrentEffectCoroutine = StartCoroutine(PlaySketchRevealEffectCoroutine(playInReverse));
    }

    public int tempVertIndex;

    // Update is called once per frame
    void Update()
    {
        if (isFirstFrame)
        {
            isFirstFrame = false;
        }

        // if we're in Edit mode, use t in [0,1] to drive the playback
        if (!Application.IsPlaying(gameObject))
        {

            if (AreFloatsEqual(prevT,t) || isFirstFrame)
            {
                prevT = t;
                return;
            }

            prevT = t;

            // if t == 0, hide all
            // if t == 1, show all
            int vertex = (int)(t * (totalVertexCount-1));

            if (vertex == 0)
            {
                HideVerticesRange(0,totalVertexCount-1);
            }
            else if (vertex == totalVertexCount-1)
            {
                ShowVerticesRange(0,totalVertexCount-1);
            }
            else
            {
                ShowVerticesRange(0, vertex);
                HideVerticesRange(vertex + 1, (int)totalVertexCount-1);
            }
        }
    }
}