using System;
using System.Collections;
using UnityEditor;
using UnityEngine;

public class ProceduralAnimation : MonoBehaviour
{
    [SerializeField] LayerMask groundLayer;

    [SerializeField] Transform pelvisTransform;
    [SerializeField] Transform[] footIKTransforms;

    [Space(10)]

    [SerializeField] float stepSize = 1.5f;
    [SerializeField] float stepInterval = 1.5f;
    [SerializeField] float stepSpeed = 4f;
    [SerializeField] AnimationCurve footLiftingCurve;

    [Space(10)]

    [SerializeField] float pelvisHeight = 1.6f;
    [SerializeField] float pelvisMovementSpeed = 8f;

    [Range(0f, 1f)]
    [SerializeField] float towardsTarget = 0.5f;

    [Space(10)]

    [SerializeField] float tiltAmount = 20f;
    [SerializeField] float tiltSpeed = 5f;

    [Space(10)]

    [SerializeField] float breathSpeed = 0.5f;
    [SerializeField] float breathMovement = 0.005f;

    [Space(10)]

    [SerializeField] bool debug = false;

    private Vector3[] footLockedLocations;
    private Vector3[] footTargetLocations;
    private Vector3[] footLocalTransforms;

    private Vector3 averageFootLocation;

    private Vector3 previousWorldLocation;
    private Vector3 smoothedVelocity = Vector3.zero;
    private float[] footTimers;

    private bool[] wantToMove;
    private bool[] isMoving;

    private int numLegs;

    // Start is called before the first frame update
    void Start()
    {
        numLegs = footIKTransforms.Length;

        footLockedLocations = new Vector3[numLegs];
        footTargetLocations = new Vector3[numLegs];
        footLocalTransforms = new Vector3[numLegs];
        footTimers = new float[numLegs];
        wantToMove = new bool[numLegs];
        isMoving = new bool[numLegs];

        for (int i = 0; i < numLegs; i++)
        {
            footLockedLocations[i] = footIKTransforms[i].position;
            footTargetLocations[i] = footIKTransforms[i].position;
            footLocalTransforms[i] = footIKTransforms[i].position - transform.position;

            footTimers[i] = (float)i / numLegs;
            wantToMove[i] = false;
            isMoving[i] = false;
        }

        previousWorldLocation = transform.position;
    }

    IEnumerator PerformStep(int i)
    {
        float lerpAlpha = 0f;
        while (lerpAlpha < 1f)
        {
            footIKTransforms[i].position = Vector3.Lerp(footLockedLocations[i], footTargetLocations[i], lerpAlpha);

            // make the leg move higher if the target is farther away
            float distance = Vector3.Distance(footIKTransforms[i].position, footTargetLocations[i]);
            footIKTransforms[i].position +=
                    footLiftingCurve.Evaluate(lerpAlpha) *          // up and down curve
                    Remap(distance, 0, stepSize * 3, 0, 5) * Vector3.up;

            lerpAlpha += Time.deltaTime * stepSpeed;
            yield return null;
        }

        footIKTransforms[i].position = footTargetLocations[i];
        footLockedLocations[i] = footTargetLocations[i];

        wantToMove[i] = false;
        isMoving[i] = false;
        footTimers[i] = (float)i / numLegs;
    }

    void Update()
    {
        AdjustPelvisTransform();

        CalculateVelocity();
        CalculateNewFootTarget();

        for (int i = 0; i < numLegs; i++)
        {
            // calculate the distance between foot's current location and the target location
            float distance = Vector3.Distance(footLockedLocations[i], footTargetLocations[i]);

            // if the distance is greater than the threshold, increase the timer to trigger the movement
            if (distance > stepSize && wantToMove[i] == false)
            {
                // add a bit randomness so all legs won't be moving at the same time
                footTimers[i] = stepInterval - Remap(i, 0, numLegs, 0f, 0.25f);

                // set the flag to true so the next frame won't enter this branch
                wantToMove[i] = true;
            }

            // time to move the leg
            if (footTimers[i] >= stepInterval && !isMoving[i])
            {
                isMoving[i] = true;
                StartCoroutine(PerformStep(i));
            }
            else
            {
                // otherwise stick to the current position
                footIKTransforms[i].position = footLockedLocations[i];
            }

            footTimers[i] += Time.deltaTime;
        }

        CalculateAverageFootLocation();
    }

    private void AdjustPelvisTransform()
    {
        // lift the pelvis to avoid collision with the environment
        if (Physics.SphereCast(averageFootLocation + Vector3.up * 5f, 2f, Vector3.down, out RaycastHit hitInfo, groundLayer))
        {
            pelvisTransform.position = Vector3.Lerp(pelvisTransform.position, hitInfo.point + Vector3.up * pelvisHeight, Time.deltaTime * pelvisMovementSpeed);
        }

        // tilt the pelvis towards the movement
        float z = Vector3.Dot(transform.forward, smoothedVelocity.normalized) * smoothedVelocity.magnitude * tiltAmount;
        float x = Vector3.Dot(transform.right, smoothedVelocity.normalized) * smoothedVelocity.magnitude * tiltAmount;
        float y = transform.eulerAngles.y + 90;

        Quaternion targetRotation = Quaternion.Euler(x, y, z);
        pelvisTransform.rotation = Quaternion.Lerp(pelvisTransform.rotation, targetRotation, Time.deltaTime * tiltSpeed);

        // simulate breathing by adding some vertical movements
        pelvisTransform.position += breathMovement * Mathf.Sin(Time.time * breathSpeed) * Vector3.up;
    }

    private float Remap(float value, float from1, float to1, float from2, float to2)
    {
        // clamp the value
        value = Math.Max(from1, value);
        value = Math.Min(value, to1);

        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }

    private Vector3 Remap(Vector3 value, float from1, float to1, float from2, float to2)
    {
        // clamp the value
        value = Vector3.Max(Vector3.one * from1, value);
        value = Vector3.Min(value, Vector3.one * to1);

        return (value - Vector3.one * from1) / (to1 - from1) * (to2 - from2) + Vector3.one * from2;
    }


    private void CalculateAverageFootLocation()
    {
        Vector3 targetLocation = Vector3.zero;
        Vector3 footLocation = Vector3.zero;
        for (int i = 0; i < numLegs; i++)
        {
            targetLocation += footTargetLocations[i];
            footLocation += footIKTransforms[i].position;
        }
        averageFootLocation = Vector3.Lerp(footLocation, targetLocation, towardsTarget) / numLegs;
    }

    private void CalculateNewFootTarget()
    {
        for (int i = 0; i < numLegs; i++)
        {
            // predicted foot position based on the current velocity
            // smoothedVelocity would be clamped by the Remap function to prevent extreme values
            Vector3 footDefaultPos = transform.TransformPoint(footLocalTransforms[i]);
            Vector3 footPos = footDefaultPos + Remap(smoothedVelocity, -stepSize * 3, stepSize * 3, -stepSize, stepSize);
            Vector3 centerPos = transform.position;

            // offset towards centerPos to prevent leg collides with the environment
            Vector3 start = Vector3.Lerp(footPos, centerPos, 0.5f) + Vector3.up * 5;
            Vector3 end = footPos + Vector3.down * 5;
            Vector3 direction = end - start;

            if (Physics.SphereCast(start, 0.5f, direction, out RaycastHit hitInfo, 10, groundLayer))
            {
                footTargetLocations[i] = hitInfo.point;
            }
            else
            {
                footTargetLocations[i] = footPos;
            }
        }
    }

    private void CalculateVelocity()
    {
        Vector3 velocity = (transform.position - previousWorldLocation) / Time.deltaTime;
        smoothedVelocity = Vector3.Lerp(smoothedVelocity, velocity, 5 * Time.deltaTime);

        previousWorldLocation = transform.position;
    }

    public void OnDrawGizmos()
    {
        if (debug)
        {
            // draw foot locked and target locations
            for (int i = 0; footTargetLocations != null && i < numLegs; i++)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(footTargetLocations[i], 0.3f);

                Gizmos.color = Color.red;
                Gizmos.DrawSphere(footLockedLocations[i], 0.1f);
            }

            // draw velocity
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + smoothedVelocity;
            float thickness = 10;

            Handles.DrawBezier(startPos, endPos, startPos, endPos, Color.magenta, null, thickness);
        }
    }
}
