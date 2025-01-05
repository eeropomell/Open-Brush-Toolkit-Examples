using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// how much of the sketch is revealed depends directly on the distance between the player and the sketch
// e.g. if distance is >4m, then sketch 0% visible,
// else if distance is <2m, then sketch 100% visible,
// and e.g. for distance = 3m, sketch is 50% visible
[ExecuteAlways]
public class SketchRevealEffectByDistanceExample : MonoBehaviour
{

    [SerializeField] private SketchRevealEffect _sketchRevealEffect;

    [SerializeField] private Transform player;

    [SerializeField] private float DistanceToFullyHide = 3f;
    [SerializeField] private float DistanceToFullyShow = 2f;

    [SerializeField] private bool enabled = true;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        // in edit mode, we handle drawing the DistanceToFullyHide/DistanceToFullyShow in OnDrawGizmos (it's below)
        if (!Application.IsPlaying(gameObject))
        {
            return;
        }

        float dist = Vector3.Distance(player.position, _sketchRevealEffect.transform.position);

        SetVisibilityByDistance(dist);
    }


    private void SetVisibilityByDistance(float dist)
    {
        if (dist >= DistanceToFullyHide)
        {
            _sketchRevealEffect.HideVerticesRange(0,_sketchRevealEffect.totalVertexCount-1);
        } else if (dist <= DistanceToFullyShow)
        {
            _sketchRevealEffect.ShowVerticesRange(0,_sketchRevealEffect.totalVertexCount-1);
        }
        else
        {

            float normalizedDist = MapInterval(dist, DistanceToFullyShow, DistanceToFullyHide, 0,1);
            // makes it so that 0 means 0% visible and 1 means 100% visible
            normalizedDist = 1 - normalizedDist;

            int vertex = (int)(normalizedDist * (_sketchRevealEffect.totalVertexCount-1));

            _sketchRevealEffect.ShowVerticesRange(0, vertex);
            _sketchRevealEffect.HideVerticesRange(vertex + 1, _sketchRevealEffect.totalVertexCount-1);
        }
    }

    private static float MapInterval(float value, float fromMin, float fromMax, float toMin, float toMax)
    {
        float normalizedValue = (value - fromMin) / (fromMax - fromMin);
        return toMin + normalizedValue * (toMax - toMin);
    }

    private void OnDrawGizmos()
    {
        if (_sketchRevealEffect == null)
        {
            return;
        }
        Gizmos.color = Color.black;
        DrawCircle(DistanceToFullyHide, _sketchRevealEffect.transform.position);

        Gizmos.color = Color.white;
        DrawCircle(DistanceToFullyShow, _sketchRevealEffect.transform.position);
    }

    private void DrawCircle(float radius, Vector3 center)
    {

        int segments = 32;

        Vector3 previousPoint = Vector3.zero;
        for (int i = 0; i <= segments; i++)
        {

            float angle = (float)i / segments * Mathf.PI * 2;


            Vector3 currentPoint = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;

            if (i > 0)
            {
                Gizmos.DrawLine(center + previousPoint, center + currentPoint);
            }

            previousPoint = currentPoint;
        }
    }
}
