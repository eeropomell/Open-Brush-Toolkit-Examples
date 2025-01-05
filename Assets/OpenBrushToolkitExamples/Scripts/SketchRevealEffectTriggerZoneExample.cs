using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SketchRevealEffectTriggerZoneExample : MonoBehaviour
{

    [SerializeField] private SketchRevealEffect _sketchRevealEffect;
    public Transform Player;

    [SerializeField] [Tooltip("If the player is within triggerZoneDist distance from sketch, the opening animation is played")]
    private float triggerZoneDist = 3f;


    [SerializeField] [Tooltip("This overrides TotalSketchTime")] private float sketchRevealDuration = 5f;
    [SerializeField] private float sketchHideDuration = 5f;

    [Tooltip("If the player hasn't been in the trigger zone for this amount of seconds, the closing animation is played")]
    public float timeToWaitUntilHide = 3f;

    private bool InTriggerZone = false;

    // if -1 it means that player has left the trigger zone and we have already played the closing animation fully
    // and since then the player hasn't entered
    private float leftTriggerZoneTime;

    void Start()
    {
        Debug.Log($"sketchRevealEffect: {_sketchRevealEffect}");
        if (_sketchRevealEffect != null)
        {
            _sketchRevealEffect.HideVerticesRange(0,_sketchRevealEffect.totalVertexCount-1);
        }

        leftTriggerZoneTime = -1f;
    }

    private void OnEnterTriggerZone()
    {
        InTriggerZone = true;
        _sketchRevealEffect.totalSketchTime = sketchRevealDuration;
        _sketchRevealEffect.PlaySketchRevealEffect(false);
    }

    private bool ShouldPlayClosingAnimation()
    {
        return Time.time - leftTriggerZoneTime >= timeToWaitUntilHide;
    }

    private void HandleNotInTriggerZone()
    {
        float dist = Vector3.Distance(Player.position, _sketchRevealEffect.transform.position);

        // entering the trigger zone
        if (dist <= triggerZoneDist)
        {
            OnEnterTriggerZone();
        }
        else
        {
            // we're not in trigger zone and didn't enter it, so check if the time has been > timeUntilClosingAnimation seconds
            // if yes, play the closing animation

            // first check if the animation has already been played
            if (leftTriggerZoneTime < 0)
            {
                return;
            }

            if (ShouldPlayClosingAnimation())
            {
                _sketchRevealEffect.totalSketchTime = sketchHideDuration;
                _sketchRevealEffect.PlaySketchRevealEffect(true);
                // we set -1 to make sure that the closing animation plays only once for every time the player leaves the trigger zone
                leftTriggerZoneTime = -1;
            }
        }
    }

    private void HandleInTriggerZone()
    {
        float dist = Vector3.Distance(Player.position, _sketchRevealEffect.transform.position);

        if (dist <= triggerZoneDist)
        {
            InTriggerZone = true;
        }
        else
        {
            // leaving the trigger zone
            InTriggerZone = false;
            leftTriggerZoneTime = Time.time;
        }
    }

    // Update is called once per frame
    void Update()
    {

        if (!InTriggerZone)
        {
            HandleNotInTriggerZone();
        }
        else
        {
            HandleInTriggerZone();
        }

    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        DrawCircle(triggerZoneDist, _sketchRevealEffect.transform.position);
    }

    private void DrawCircle(float radius, Vector3 center)
    {

        int segments = 32;

        Vector3 previousPoint = Vector3.zero;
        for (int i = 0; i <= segments; i++)
        {

            float angle = (float)i / segments * Mathf.PI * 2;


            Vector3 currentPoint = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;

            // If not the first point, draw a line from the previous point
            if (i > 0)
            {
                Gizmos.DrawLine(center + previousPoint, center + currentPoint);
            }

            previousPoint = currentPoint;
        }
    }
}
