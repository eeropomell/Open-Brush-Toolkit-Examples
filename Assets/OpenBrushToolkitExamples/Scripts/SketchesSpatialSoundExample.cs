using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// all the sketches that are children of this GameObject,
// there is a spatial sound source of the pencil sound
// and it is always located at the nearest sketch
public class SketchesSpatialSoundExample : MonoBehaviour
{

    [SerializeField] private Transform player;

    [SerializeField]
    private bool playEffect = false;

    private SketchRevealEffect[] _sketchRevealEffects;

    public AudioSource audioSource;

    private SketchRevealEffect currentlyNearestSketch;


    [SerializeField] [Tooltip("The example can be activated when inside the activation zone")]
    private BoxCollider ActivationZone;

    // Start is called before the first frame update
    void Start()
    {

        _sketchRevealEffects = GameObject.FindObjectsOfType<SketchRevealEffect>();
    }

    // Update is called once per frame
    void Update()
    {

        if (!playEffect)
        {
            audioSource.volume = 0f;
            if (IsInsideCubeXZ(player.transform.position, ActivationZone))
            {
                if (Input.GetKeyDown(KeyCode.E))
                {
                    playEffect = true;
                    StartTheExample();
                }
            }

            return;
        }

        audioSource.volume = 1f;

        SketchRevealEffect sketchRevealEffect = GetNearestSketch(player);

        // we need to change the position of the audiosource
        if (currentlyNearestSketch != sketchRevealEffect)
        {
            audioSource.transform.position = sketchRevealEffect.transform.position;
        }
    }

    private void StartTheExample()
    {
        foreach (SketchRevealEffect sketchRevealEffect in _sketchRevealEffects)
        {
            sketchRevealEffect.loop = true;
            sketchRevealEffect.PlaySketchRevealEffect();
        }
    }

    private SketchRevealEffect GetNearestSketch(Transform target)
    {
        float shortestDistSoFar = Vector3.Distance(_sketchRevealEffects[0].transform.position, target.position);
        int indexOfNearestTemp = 0;
        for (int i = 1; i < _sketchRevealEffects.Length; i++)
        {
            SketchRevealEffect sketchRevealEffect = _sketchRevealEffects[i];
            if (Vector3.Distance(sketchRevealEffect.transform.position, target.position) < shortestDistSoFar)
            {
                shortestDistSoFar = Vector3.Distance(sketchRevealEffect.transform.position, target.position);
                indexOfNearestTemp = i;
            }
        }

        return _sketchRevealEffects[indexOfNearestTemp];
    }



    private bool IsInsideCubeXZ(Vector3 position, BoxCollider cubeCollider)
    {
        Vector3 cubeCenter = cubeCollider.bounds.center;
        Vector3 cubeSize = cubeCollider.bounds.size;

        float xMin = cubeCenter.x - cubeSize.x / 2f;
        float xMax = cubeCenter.x + cubeSize.x / 2f;

        float zMin = cubeCenter.z - cubeSize.z / 2f;
        float zMax = cubeCenter.z + cubeSize.z / 2f;

        return position.x >= xMin && position.x <= xMax &&
               position.z >= zMin && position.z <= zMax;
    }
}
