using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class Rope : MonoBehaviour
{
    public Transform startTransform;
    public Transform endTransform;

    [Header("Whip Settings")]
    public int segmentCount = 25;
    public float ropeLength = 4f;
    public int constraintIterations = 30;
    public Vector3 gravity = new Vector3(0f, -9.81f, 0f);
    public Color whipBrownColor = new Color(0.45f, 0.25f, 0.1f);
    public float whipThickness = 0.08f;

    private struct WhipSegment
    {
        public Vector3 position;
        public Vector3 prevPosition;

        public WhipSegment(Vector3 pos)
        {
            position = pos;
            prevPosition = pos;
        }
    }

    private List<WhipSegment> segments = new List<WhipSegment>();
    private List<int> constraintIndices = new List<int>();
    private LineRenderer lineRenderer;
    private float segmentLength;
    private bool isInitialized = false;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        SetupLineRendererStyle();
        InitializeWhip();
    }

    public void InitializeWhip()
    {
        if (startTransform == null || endTransform == null) return;

        segmentLength = ropeLength / Mathf.Max(segmentCount - 1, 1);
        segments.Clear();
        constraintIndices.Clear();

        Vector3 startPos = startTransform.position;
        Vector3 endPos = endTransform.position;

        for (int i = 0; i < segmentCount; i++)
        {
            float t = (float)i / (segmentCount - 1);
            Vector3 pos = Vector3.Lerp(startPos, endPos, t);
            segments.Add(new WhipSegment(pos));
        }

        for (int i = 0; i < segmentCount - 1; i++)
        {
            constraintIndices.Add(i);
        }

        isInitialized = true;
    }

    void SetupLineRendererStyle()
    {
        lineRenderer.positionCount = segmentCount;
        lineRenderer.startWidth = whipThickness;
        lineRenderer.endWidth = whipThickness * 0.3f;

        // Smooth out corners and joints
        lineRenderer.numCornerVertices = 5;
        lineRenderer.numCapVertices = 5;
        lineRenderer.alignment = LineAlignment.View;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(whipBrownColor, 0.0f), new GradientColorKey(whipBrownColor * 0.7f, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
        );
        lineRenderer.colorGradient = gradient;
    }

    void FixedUpdate()
    {
        if (!isInitialized || startTransform == null || endTransform == null) return;

        if (float.IsNaN(startTransform.position.x) || float.IsNaN(endTransform.position.x)) return;

        Simulate();
    }

    void Update()
    {
        if (isInitialized && startTransform != null && endTransform != null)
        {
            DrawWhip();
        }
    }

    void Simulate()
    {
        WhipSegment startSeg = segments[0];
        startSeg.position = startTransform.position;
        startSeg.prevPosition = startTransform.position;
        segments[0] = startSeg;

        WhipSegment endSeg = segments[segmentCount - 1];
        endSeg.position = endTransform.position;
        endSeg.prevPosition = endTransform.position;
        segments[segmentCount - 1] = endSeg;

        for (int i = 1; i < segmentCount - 1; i++)
        {
            WhipSegment segment = segments[i];
            Vector3 velocity = segment.position - segment.prevPosition;

            if (float.IsNaN(velocity.x) || float.IsNaN(velocity.y) || float.IsNaN(velocity.z)) velocity = Vector3.zero;

            segment.prevPosition = segment.position;
            segment.position += velocity + gravity * Time.fixedDeltaTime * Time.fixedDeltaTime;
            segments[i] = segment;
        }

        for (int iteration = 0; iteration < constraintIterations; iteration++)
        {
            WhipSegment first = segments[0];
            first.position = startTransform.position;
            segments[0] = first;

            WhipSegment last = segments[segmentCount - 1];
            last.position = endTransform.position;
            segments[segmentCount - 1] = last;

            ShuffleList(constraintIndices);

            for (int c = 0; c < constraintIndices.Count; c++)
            {
                int i = constraintIndices[c];
                WhipSegment seg1 = segments[i];
                WhipSegment seg2 = segments[i + 1];

                Vector3 delta = seg2.position - seg1.position;
                float dist = delta.magnitude;

                if (dist < 0.0001f) continue;

                float error = dist - segmentLength;
                Vector3 changeDir = delta / dist;

                bool isStartFixed = (i == 0);
                bool isEndFixed = (i + 1 == segmentCount - 1);

                if (isStartFixed && !isEndFixed)
                {
                    seg2.position -= changeDir * error;
                }
                else if (!isStartFixed && isEndFixed)
                {
                    seg1.position += changeDir * error;
                }
                else if (!isStartFixed && !isEndFixed)
                {
                    seg1.position += changeDir * (error * 0.5f);
                    seg2.position -= changeDir * (error * 0.5f);
                }

                segments[i] = seg1;
                segments[i + 1] = seg2;
            }
        }
    }

    void ShuffleList(List<int> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            int value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    void DrawWhip()
    {
        Vector3[] positions = new Vector3[segmentCount];

        for (int i = 0; i < segmentCount; i++)
        {
            if (float.IsNaN(segments[i].position.x) || float.IsNaN(segments[i].position.y) || float.IsNaN(segments[i].position.z))
            {
                positions[i] = startTransform.position;
            }
            else
            {
                positions[i] = segments[i].position;
            }
        }

        lineRenderer.SetPositions(positions);
    }

    public void Reset()
    {
        InitializeWhip();
        DrawWhip();
    }
}