using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GLDrawGraph : MonoSingleton<GLDrawGraph>
{
    private const int SAMPLE_COUNT = 200;

    private static Material glMaterial;
    private Rect graph1Rect = new Rect(10, 10, 200, 80);
    private int xOffset = 0;
    private int yOffset = 0;
    private int xLimit = 0;
    private int yLimit = 0;
    private bool doDrag = false;

    private Dictionary<int, List<float>> _graphArrays;
    private Dictionary<int, float> _graphArraysMax;
    private static Dictionary<int, string> _captions = new Dictionary<int, string>(10);
    private Dictionary<int, float> _lastSamples;

    private float[] _nullNodes;
    private float[] _accumulatedNodes;
    private int _currentSample = 0;

    public enum VIEW
    {
        SPLIT,
        ACCUMULATED,
    }

    public VIEW ViewStyle;

    public Color[] Colors;

    public float SampleFrequency = 0.01f;

    public bool DrawGraph = true;

    private void Awake()
    {
        _graphArrays = new Dictionary<int, List<float>>(10);
        _graphArraysMax = new Dictionary<int, float>(10);

        _lastSamples = new Dictionary<int, float>(10);

        _accumulatedNodes = new float[SAMPLE_COUNT];
        _nullNodes = new float[SAMPLE_COUNT];

    }

    private void Start()
    {
        StartCoroutine(startSampleLoop());
        createGLMaterial();
    }

    private void Update()
    {
        if (!DrawGraph) return;

        if (Input.GetMouseButtonDown(0) && graph1Rect.Contains(Input.mousePosition))
        {
            xOffset = (int)(Input.mousePosition.x - graph1Rect.x);
            yOffset = (int)(Input.mousePosition.y - graph1Rect.y);
            doDrag = true;
        }
        if (graph1Rect.Contains(Input.mousePosition))
        {
            graph1Rect.height += (int)(Input.GetAxis("Mouse ScrollWheel") * 40);
        }
        if (doDrag)
        {
            graph1Rect = new Rect(Input.mousePosition.x - xOffset, Input.mousePosition.y - yOffset, graph1Rect.width, graph1Rect.height);
        }
        if (Input.GetMouseButtonUp(0))
        {
            doDrag = false;
        }
        //Clean up the graph to make sure it's in screen
        yLimit = (int)(Screen.height - graph1Rect.height);
        xLimit = (int)(Screen.width - graph1Rect.width);
        graph1Rect = new Rect(Mathf.Clamp(graph1Rect.x, 2, xLimit), Mathf.Clamp(graph1Rect.y, 2, yLimit), graph1Rect.width, Mathf.Clamp(graph1Rect.height, 40, Screen.height));
    }

    //IEnumerator startTestLoop()
    //{
    //    while (true)
    //    {
    //        AddSampleValue(Random.Range(0, 2), Random.value);
    //        yield return new WaitForSeconds(0.1f);
    //    }
    //}

    IEnumerator startSampleLoop()
    {
        while (true)
        {
            if (DrawGraph && SampleFrequency > 0)
            {
                yield return new WaitForSeconds(SampleFrequency);

                _currentSample++;

                _currentSample = _currentSample % SAMPLE_COUNT;

                foreach (List<float> nodes in _graphArrays.Values)
                {
                    nodes[_currentSample] = 0;
                }
            }
            else
            {
                yield return new WaitForSeconds(1);
            }
        }
    }

    public static void AddSampleValue(int graph, float v)
    {
        if (Exists)
        {
            if (Instance.DrawGraph)
            {
                if (!Instance._graphArrays.ContainsKey(graph))
                {
                    Instance._graphArrays[graph] = new List<float>(Instance._nullNodes);
                    Instance._graphArraysMax[graph] = 0;
                }

                Instance._graphArrays[graph][Instance._currentSample] = v;
                Instance._graphArraysMax[graph] = Mathf.Max(Instance._graphArraysMax[graph], Mathf.Abs(v));
                Instance._lastSamples[graph] = v;
            }
        }
    }

    public static int GraphId;

    public static void AddCaption(int graph, string caption)
    {
        _captions[graph] = caption;
    }

    private static void createGLMaterial()
    {
        if (!glMaterial)
        {
            glMaterial = new Material("Shader \"Lines/Colored Blended\" {" +
            "SubShader { Pass { " +
            " Blend SrcAlpha OneMinusSrcAlpha " +
            " ZWrite Off Cull Off Fog { Mode Off } " +
            " BindChannels {" +
            " Bind \"vertex\", vertex Bind \"color\", color }" +
            "} } }");
            glMaterial.hideFlags = HideFlags.HideAndDontSave;
            glMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
        }
    }

    private void OnGUI()
    {
        if (!DrawGraph) return;

        switch (ViewStyle)
        {
            case VIEW.SPLIT:
                {
                    GUI.color = Color.black;
                    float sectionHeight = graph1Rect.height / _graphArrays.Count;

                    int graphCount = _graphArrays.Count - 1;
                    foreach (KeyValuePair<int, List<float>> graph in _graphArrays)
                    {
                        string caption = "<no caption> ";
                        if (_captions.TryGetValue(graph.Key, out caption))
                            GUI.Label(new Rect(graph1Rect.x + 5, (Screen.height - graph1Rect.yMax) + (graphCount * sectionHeight), 200, 20), caption);

                        float sample;
                        if (_lastSamples.TryGetValue(graph.Key, out sample))
                            GUI.Label(new Rect(graph1Rect.xMax - 50, (Screen.height - graph1Rect.yMax) + (graphCount * sectionHeight), 200, 20), sample.ToString("f2"));

                        graphCount--;
                    }
                    GUI.color = Color.white;
                    break;
                }
        }
    }

    private void OnPostRender()
    {
        if (!DrawGraph) return;

        if (_graphArrays.Count == 0 || Colors.Length == 0)
        {
            return;
        }

        glMaterial.SetPass(0);
        GL.PushMatrix();
        GL.LoadPixelMatrix();

        float leftOffset;
        float sectionHeight = graph1Rect.height / _graphArrays.Count;

        switch (ViewStyle)
        {
            case VIEW.SPLIT:
                {
                    draw2DRectOutline(new Rect(graph1Rect.x - 1, graph1Rect.y - 1, graph1Rect.width + 1, graph1Rect.height + 1), new Color(1, 1, 1, 0.5f), new Color(1, 1, 1, 1), 0);

                    int graphCount = 0;
                    foreach (KeyValuePair<int, List<float>> nodes in _graphArrays)
                    {
                        Color c = Colors[graphCount % Colors.Length];
                        for (int i = 0; i < nodes.Value.Count; i++)
                        {
                            int j = (i + _currentSample) % SAMPLE_COUNT;

                            leftOffset = graph1Rect.x + i;
                            float val = nodes.Value[j];// / _graphArraysMax[nodes.Key];
                            draw2DLine(new Rect(leftOffset, graph1Rect.y + (graphCount * sectionHeight), 1, val * sectionHeight), c, 0);
                        }
                        graphCount++;
                    }
                    break;
                }
            case VIEW.ACCUMULATED:
                {
                    draw2DRectOutline(new Rect(309, 9, graph1Rect.width + 1, graph1Rect.height + 1), new Color(1, 1, 1, 0.5f), new Color(1, 1, 1, 1), 0);

                    for (int i = 0; i < _accumulatedNodes.Length; i++)
                    {
                        _accumulatedNodes[i] = 0;
                    }

                    int graphCount = 0;
                    foreach (List<float> nodes in _graphArrays.Values)
                    {
                        Color c = Colors[graphCount % Colors.Length];
                        for (int i = 0; i < nodes.Count; i++)
                        {
                            int j = (i + _currentSample) % SAMPLE_COUNT;

                            leftOffset = 310 + i;
                            draw2DLine(new Rect(leftOffset, 10 + (_accumulatedNodes[j] * sectionHeight), 1, nodes[j] * sectionHeight), c, 0);
                            _accumulatedNodes[j] += nodes[j];
                        }
                        graphCount++;
                    }
                    break;
                }
        }

        GL.PopMatrix();
    }

    private void draw2DRect(Rect rect, Color color, int depth)
    {
        GL.Begin(GL.QUADS);
        GL.Color(color);
        GL.Vertex3(rect.x, rect.y + rect.height, depth); // bottom left
        GL.Vertex3(rect.x + rect.width, rect.y + rect.height, depth); // bottom right
        GL.Vertex3(rect.x + rect.width, rect.y, depth); // top right
        GL.Vertex3(rect.x, rect.y, depth); // top left
        GL.End();
    }

    private void draw2DRectOutline(Rect rect, Color color, Color outlineColor, int depth)
    {
        draw2DRect(rect, color, depth);
        draw2DOutline(rect, outlineColor, depth);
    }

    private void draw2DOutline(Rect rect, Color color, int depth)
    {
        GL.Begin(GL.LINES);
        GL.Color(color);
        GL.Vertex3(rect.x, rect.y, depth); // Bottom
        GL.Vertex3(rect.x + rect.width, rect.y, depth);
        GL.Vertex3(rect.x, rect.y + rect.height, depth); // Top
        GL.Vertex3(rect.x + rect.width, rect.y + rect.height, depth);
        GL.Vertex3(rect.x, rect.y, depth);// Left
        GL.Vertex3(rect.x, rect.y + rect.height, depth);
        GL.Vertex3(rect.x + rect.width, rect.y, depth);// Right
        GL.Vertex3(rect.x + rect.width, rect.y + rect.height, depth);
        GL.End();
    }

    private void draw2DLine(Rect rect, Color color, int depth)
    {
        GL.Begin(GL.LINES);
        GL.Color(color);
        GL.Vertex3(rect.x, rect.y, depth);
        GL.Vertex3(rect.x, rect.y + rect.height, depth);
        GL.End();
    }

    private void draw2DPoint(Rect rect, Color color, int depth)
    {
        //TODO: using line to draw point is ugly
        GL.Begin(GL.LINES);
        GL.Color(color);
        GL.Vertex3(rect.x, rect.y + rect.height, depth);
        GL.Vertex3(rect.x, rect.y + rect.height + 1, depth);
        GL.End();
    }
}
