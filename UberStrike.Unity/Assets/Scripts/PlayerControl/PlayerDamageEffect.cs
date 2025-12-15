using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerDamageEffect : MonoBehaviour
{
    [SerializeField]
    private float _height;

    [SerializeField]
    private float _width;

    [SerializeField]
    private float _duration;

    [SerializeField]
    private MeshRenderer _renderer;

    private Vector2[] FONT_METRICS = new Vector2[]
    {
        new Vector2(0, 42),     // 0
        new Vector2(42, 21),    // 1
        new Vector2(63, 29),    // 2
        new Vector2(92, 28),    // 3
        new Vector2(120, 34),   // 4
        new Vector2(154, 28),   // 5
        new Vector2(182, 33),   // 6
        new Vector2(215, 31),   // 7
        new Vector2(246, 34),   // 8
        new Vector2(280, 33)    // 9
    };

    private const int WIDTH = 313;
    private const int HEIGHT = 43;

    public float _speed;
    private float _offset;
    //private float _alpha;

    private bool _show;
    private float _time;

    private Vector3 _start;
    private Vector3 _direction;
    private Transform _transform;

    private void Awake()
    {
        _transform = transform;
        _start = _transform.position;
    }

    private void Update()
    {
        if (_show)
        {
            float x = _time * _speed - _offset;

            Vector3 pos = _direction * _time;
            pos.y = _height - x * x * _width;

            _time += Time.deltaTime;
            _transform.position = _start + pos;

            UpdateTransform();

            // fade out
            if (_time > _duration)
            {
                Color c = _renderer.material.GetColor("_Color");

                c.a = Mathf.Lerp(c.a, 0, Time.deltaTime * 3);
                _renderer.material.SetColor("_Color", c);

                if (c.a < 0.2f)
                    Destroy(gameObject);
            }
        }
    }

    public void Show(DamageInfo shot)
    {
        if (_width == 0)
        {
            _width = 1;
        }

        MeshFilter filter = gameObject.AddComponent<MeshFilter>();
        if (filter)
            filter.mesh = CreateCharacterMesh(shot.Damage, FONT_METRICS, WIDTH, HEIGHT);

        UpdateTransform();

        //_alpha = 1;
        _show = true;
        _offset = Mathf.Sqrt(_height / _width);
        _direction = Random.onUnitSphere;
        _renderer.material = new Material(_renderer.material);

        StartCoroutine(StartEnableRenderer());
    }

    private IEnumerator StartEnableRenderer()
    {
        yield return new WaitForSeconds(0.1f);

        _renderer.enabled = true;
    }

    private void UpdateTransform()
    {
        if (GameState.HasCurrentPlayer)
        {
            float distance = Vector3.Distance(_transform.position, GameState.LocalCharacter.Position);
            float scale = 0.003f + 0.0005f * distance * LevelCamera.Instance.FOV / 60;

            _transform.localScale = new Vector3(scale, scale, scale);
            _transform.rotation = GameState.LocalCharacter.Rotation;
        }
        else if (Camera.current)
        {
            Transform t = Camera.current.transform;
            float distance = Vector3.Distance(_transform.position, t.position);
            float scale = 0.003f + 0.0005f * distance * Camera.current.fieldOfView / 60;

            _transform.localScale = new Vector3(scale, scale, scale);
            _transform.rotation = Quaternion.LookRotation(new Vector3(t.forward.x, 0, t.forward.z));
        }
    }

    private Mesh CreateCharacterMesh(int number, Vector2[] metrics, int width, int height)
    {
        Mesh mesh = new Mesh();
        string str = Mathf.Abs(number).ToString();

        List<Vector3> vertexList = new List<Vector3>();
        List<Vector2> texcoordList = new List<Vector2>();
        List<int> triangleList = new List<int>();

        Vector3[] vertices = new Vector3[4];
        Vector2[] texcoords = new Vector2[4];
        int[] triangles = new int[] { 0, 1, 2, 0, 2, 3 };

        float total = 0, j = 0;

        for (int i = 0; i < str.Length; i++)
        {
            int idx = str[i] - '0';
            if (idx >= 0 && idx < 10)
            {
                for (int k = 0; k < 6; k++)
                {
                    triangleList.Add(triangles[k] + vertexList.Count);
                }

                vertices[0] = new Vector3(metrics[idx].x, 0, 0);
                vertices[1] = new Vector3(metrics[idx].x + metrics[idx].y, 0, 0);
                vertices[2] = new Vector3(metrics[idx].x + metrics[idx].y, height, 0);
                vertices[3] = new Vector3(metrics[idx].x, height, 0);

                texcoords[0] = new Vector2(vertices[0].x / width, vertices[0].y / height);
                texcoords[1] = new Vector2(vertices[1].x / width, vertices[1].y / height);
                texcoords[2] = new Vector2(vertices[2].x / width, vertices[2].y / height);
                texcoords[3] = new Vector2(vertices[3].x / width, vertices[3].y / height);

                vertexList.AddRange(vertices);
                texcoordList.AddRange(texcoords);

                total += metrics[idx].y;
            }
        }

        for (int i = 0; i < vertexList.Count / 4; i++)
        {
            vertexList[i * 4 + 1] -= new Vector3(vertexList[i * 4 + 0].x + total / 2 - j, height / 2);
            vertexList[i * 4 + 2] -= new Vector3(vertexList[i * 4 + 3].x + total / 2 - j, height / 2);
            vertexList[i * 4 + 3] -= new Vector3(vertexList[i * 4 + 3].x + total / 2 - j, height / 2);
            vertexList[i * 4 + 0] -= new Vector3(vertexList[i * 4 + 0].x + total / 2 - j, height / 2);

            j += (vertexList[i * 4 + 1].x - vertexList[i * 4].x);
        }

        mesh.vertices = vertexList.ToArray();
        mesh.uv = texcoordList.ToArray();
        mesh.triangles = triangleList.ToArray();

        return mesh;
    }
}