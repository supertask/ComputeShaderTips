using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;

public class GPUInstancingVAT : MonoBehaviour
{
    [SerializeField]
    private ComputeShader m_computeShader;

    [Header("DrawMeshInstancedDirectのパラメータ")]
    [Space(20)]
    [SerializeField]
    private Mesh m_mesh;

    //[SerializeField]
    //private GameObject m_prefab;

    [SerializeField]
    private Shader m_shader; 


    [SerializeField]
    private Bounds m_bounds;

    [SerializeField]
    private ShadowCastingMode m_shadowCastingMode;

    [SerializeField]
    private bool m_receiveShadows;

    [Header("生成する数")]
    [Space(20)]
    [SerializeField]
    private int m_instanceCount;

    [SerializeField]
    private float m_offsetPositionY;

    [Header("モデルを歩かせる速さ")]
    [Space(20)]
    [SerializeField]
    private float m_speed;

    [SerializeField]
    private Texture m_colorTex;

    [SerializeField]
    private Texture m_positionTex;

    [SerializeField]
    private Texture m_normalTex;

    private Material m_material;
    //private Mesh m_mesh;


    private ComputeBuffer m_argsBuffer;
    private ComputeBuffer m_vatObjectDataBuffer;

    private int m_moveKernel;
    private Vector3Int m_groupSize;

    private int m_deltaTimeId = Shader.PropertyToID("_deltaTime");

    private float m_minBoundX;
    private float m_maxBoundX;
    private float m_minBoundZ;
    private float m_maxBoundZ;

    struct VATObjectData {
        public Vector3 position;
        public float animationOffset;
    }


    // Start is called before the first frame update
    void Start()
    {
        m_material = new Material(m_shader);
        CalculateBounds();
        InitializeArgsBuffer();
        InitializeComputeShader();
    }

    void Update() {

        m_computeShader.SetFloat(m_deltaTimeId, Time.deltaTime);
        m_computeShader.Dispatch(m_moveKernel, m_groupSize.x, m_groupSize.y, m_groupSize.z);
    }

    // Update is called once per frame
    void LateUpdate()
    {
        
        Graphics.DrawMeshInstancedIndirect(
            m_mesh,
            0,
            m_material,
            m_bounds,
            m_argsBuffer,
            0,
            null,
            m_shadowCastingMode,
            m_receiveShadows
        );

    }

    private void InitializeArgsBuffer() {

        uint[] args = new uint[5] { 0, 0, 0, 0, 0};

        args[0] = m_mesh.GetIndexCount(0);
        args[1] = (uint)m_instanceCount;

        m_argsBuffer = new ComputeBuffer(1, 4 * args.Length, ComputeBufferType.IndirectArguments);

        m_argsBuffer.SetData(args);
    }

    private void InitializeComputeShader() {

        m_instanceCount = Mathf.ClosestPowerOfTwo(m_instanceCount);

        InitializeVATObjectDataBuffer();

        m_moveKernel = m_computeShader.FindKernel("Move");
        
        m_computeShader.GetKernelThreadGroupSizes(m_moveKernel, out uint x, out uint y, out uint z);
        m_groupSize = new Vector3Int(m_instanceCount / (int)x, (int)y, (int)z);

        m_computeShader.SetFloat("_Speed", m_speed);
        m_computeShader.SetFloat("_MinBoundZ", m_minBoundZ);
        m_computeShader.SetFloat("_MaxBoundZ", m_maxBoundZ);

        m_computeShader.SetBuffer(m_moveKernel, "_VATObjectDataBuffer", m_vatObjectDataBuffer);

    }

    private void InitializeVATObjectDataBuffer() {

        VATObjectData[] vatObjectData = new VATObjectData[m_instanceCount];

        for(int i = 0; i < m_instanceCount; ++i) {

            vatObjectData[i].position = new Vector3(
                Random.Range(m_minBoundX, m_maxBoundX),
                m_offsetPositionY,
                Random.Range(m_minBoundZ, m_maxBoundZ)
            );
            vatObjectData[i].animationOffset = Random.Range(0, 10.0f);

        }

        m_vatObjectDataBuffer = new ComputeBuffer(m_instanceCount, Marshal.SizeOf(typeof(VATObjectData)));
        m_vatObjectDataBuffer.SetData(vatObjectData);
        m_material.SetBuffer("_VATObjectDataBuffer", m_vatObjectDataBuffer);
        m_material.SetTexture("_MainTex", m_colorTex);
        m_material.SetTexture("_PosTex", m_positionTex);
        m_material.SetTexture("_NmlTex", m_normalTex);

    }

    private void CalculateBounds() {

        m_minBoundX = m_bounds.center.x - m_bounds.size.x / 2.0f;
        m_maxBoundX = m_bounds.center.x + m_bounds.size.x / 2.0f;

        m_minBoundZ = m_bounds.center.z - m_bounds.size.z / 2.0f;
        m_maxBoundZ = m_bounds.center.z + m_bounds.size.z / 2.0f;

    }

    private void OnDestroy() {

        m_argsBuffer?.Release();
        m_vatObjectDataBuffer?.Release();

    }

}