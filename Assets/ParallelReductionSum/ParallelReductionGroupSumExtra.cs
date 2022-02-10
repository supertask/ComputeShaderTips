using System.Linq;
using System.Runtime.InteropServices;

using UnityEngine;
using Unity.Mathematics;

using ComputeShaderUtil;
using NearestNeighbour;

public class ParallelReductionGroupSumExtra : MonoBehaviour
{

    // Mpm Cell for interlocked add
    public struct LockMpmCell
    {
        public int mass;
        public int mass2;
        public int3 mass_x_velocity;
        public int3 mass_x_velocity2;
        public float3 velocity;
        public float3 force;
        public float2 padding;

        public override string ToString() {
            return $"LockMpmCell(mass={mass}, mass2={mass2}, mass_x_velocity={mass_x_velocity}, mass_x_velocity2={mass_x_velocity2}, velocity={velocity}, force={force}, padding={padding})";
        }
    };

    //For lock-free based GPU Optimization
    public struct P2GMass {
        public float mass;
        public float3 mass_x_velocity;
        
        public override string ToString() => $"P2GMass(mass={mass}, mass_x_velocity={mass_x_velocity})";
    }; 

    public ComputeShader cs;
    [SerializeField] public int gridWidth = 80;
    [SerializeField] public int gridHeight = 80;
    [SerializeField] public int gridDepth = 80;
    [SerializeField] private int numOfP2GMasses = 8;

    private GridOptimizer3D<P2GMass> gridOptimizer;
    private Kernel clearMassBuffersKernel;
    private Kernel clearGridBuffersKernel;
    private Kernel boundaryAndIntervalKernel;
    private Kernel gatherAndWriteKernel;
    private int numOfCells;

    private ComputeBuffer gridAndMassIdsBuffer;
    private ComputeBuffer p2gMassBuffer;
    private ComputeBuffer boundaryAndIntervalBuffer;
    private ComputeBuffer outputP2gMassBuffer;
    private ComputeBuffer gridBuffer;
    //private ComputeBuffer gridMassBuffer;
    //private ComputeBuffer gridMassVelBuffer;

    #region Shader property IDs
    public static class ShaderID
    {
        public static int CellNeighbourBuffer = Shader.PropertyToID("_CellNeighbourBuffer");
        public static int P2GMassBuffer = Shader.PropertyToID("_P2GMassBuffer");
        public static int OutputP2gMassBuffer = Shader.PropertyToID("_OutputP2gMassBuffer");
        public static int GridAndMassIdsBuffer = Shader.PropertyToID("_GridAndMassIdsBuffer");
        public static int BoundaryAndIntervalBuffer = Shader.PropertyToID("_BoundaryAndIntervalBuffer");
        public static int GridBuffer = Shader.PropertyToID("_GridBuffer");
        public static int GridIndicesBuffer = Shader.PropertyToID("_GridIndicesBuffer");
        //public static int GridMassBuffer = Shader.PropertyToID("_GridMassBuffer");
        //public static int GridMassVelBuffer = Shader.PropertyToID("_GridMassVelBuffer");

        public static int NumOfParticles = Shader.PropertyToID("_NumOfParticles");
        public static int NumOfP2GMasses = Shader.PropertyToID("_NumOfP2GMasses");
        public static int CellNeighbourLength = Shader.PropertyToID("_CellNeighbourLength");
    }
    #endregion

    void Start()
    {
        this.TestLongBuffer();
    }
    
    void TestLongBuffer()
    {
        this.numOfCells = this.gridWidth * this.gridHeight * this.gridDepth;
        
        //Init Nearest Neighbour
        this.gridOptimizer = new GridOptimizer3D<P2GMass>(
            this.numOfP2GMasses, this.GetGridBounds().size,
            this.GetGridDimension()
        );

        this.clearMassBuffersKernel = new Kernel(cs, "ClearMassBuffers");
        this.clearGridBuffersKernel = new Kernel(cs, "ClearGridBuffers");
        this.boundaryAndIntervalKernel = new Kernel(cs, "BoundaryAndInterval");
        this.gatherAndWriteKernel = new Kernel(cs, "GatherAndWrite");

        // Input Compute Buffers
        this.p2gMassBuffer = new ComputeBuffer(this.numOfP2GMasses, Marshal.SizeOf(typeof(P2GMass)) );
        this.gridAndMassIdsBuffer = new ComputeBuffer(this.numOfP2GMasses, Marshal.SizeOf(typeof(uint2)) );

        //質量リスト
        P2GMass[] p2gMasses = Enumerable.Range(0, this.numOfP2GMasses).Select(_ =>
            new P2GMass{
                mass = UnityEngine.Random.Range(0.0f, 10.0f),
                mass_x_velocity = new float3(
                    UnityEngine.Random.Range(-10.0f, 10.0f),
                    UnityEngine.Random.Range(-10.0f, 10.0f),
                    UnityEngine.Random.Range(-10.0f, 10.0f)
                )
            }
        ).ToArray();

        //グリッドIDと質量のIDのリスト
        uint2[] gridAndMassIds = Enumerable.Range(0, this.numOfP2GMasses).Select(_ =>
            new uint2(
                (uint) UnityEngine.Random.Range(0, this.numOfCells),    //Grid index
                (uint) UnityEngine.Random.Range(0, this.numOfP2GMasses) //Mass index
            )
        ).ToArray();
        this.p2gMassBuffer.SetData(p2gMasses);
        this.gridAndMassIdsBuffer.SetData(gridAndMassIds);

        Debugger.LogBuffer<P2GMass>(this.p2gMassBuffer, 0, this.numOfP2GMasses);
        Debugger.LogBuffer<uint2>(this.gridAndMassIdsBuffer, 0, this.numOfP2GMasses);


        // Output Compute Buffers
        this.boundaryAndIntervalBuffer = new ComputeBuffer(this.numOfP2GMasses,
            Marshal.SizeOf(typeof(uint2)) );
        this.outputP2gMassBuffer = new ComputeBuffer(this.numOfP2GMasses, sizeof(uint));
        this.gridBuffer = new ComputeBuffer(this.numOfCells, Marshal.SizeOf(typeof(LockMpmCell)) );

        this.ClearBuffers();
        
        this.gridOptimizer.GridSort(this.gridAndMassIdsBuffer); 

        this.ComputeBoundaryAndInterval();
        this.GatherAndWriteP2G();


        this.ReleaseAll();
    }

    public void ClearBuffers()
    {
        /*
        this.p2gScatteringOptCS.SetBuffer(this.clearMassBuffersKernel.Index,
            ShaderID.GridAndMassIdsBuffer, this.gridAndMassIdsBuffer);
        this.p2gScatteringOptCS.SetBuffer(this.clearMassBuffersKernel.Index,
            ShaderID.P2GMassBuffer, this.p2gMassBuffer);
        */

        //Clear p2g mass
        this.cs.SetBuffer(this.clearMassBuffersKernel.Index,
            ShaderID.BoundaryAndIntervalBuffer, this.boundaryAndIntervalBuffer);
        this.cs.SetBuffer(this.clearMassBuffersKernel.Index,
            ShaderID.OutputP2gMassBuffer, this.outputP2gMassBuffer);
        this.cs.Dispatch(this.clearMassBuffersKernel.Index,
            Mathf.CeilToInt(this.numOfP2GMasses / (float)this.clearMassBuffersKernel.ThreadX),
            (int)this.clearMassBuffersKernel.ThreadY,
            (int)this.clearMassBuffersKernel.ThreadZ);

        this.cs.SetBuffer(this.clearGridBuffersKernel.Index,
            ShaderID.GridBuffer, this.gridBuffer);
        this.cs.Dispatch(this.clearGridBuffersKernel.Index,
            Mathf.CeilToInt(this.numOfCells / (float)this.clearGridBuffersKernel.ThreadX),
            (int)this.clearGridBuffersKernel.ThreadY,
            (int)this.clearGridBuffersKernel.ThreadZ);
    }

    public void ComputeBoundaryAndInterval()
    {
        this.cs.SetBuffer(this.boundaryAndIntervalKernel.Index,
            ShaderID.GridAndMassIdsBuffer, this.gridAndMassIdsBuffer);
        this.cs.SetBuffer(this.boundaryAndIntervalKernel.Index,
            ShaderID.GridIndicesBuffer, this.gridOptimizer.GetGridIndicesBuffer());
        this.cs.SetBuffer(this.boundaryAndIntervalKernel.Index,
            ShaderID.BoundaryAndIntervalBuffer, this.boundaryAndIntervalBuffer);
        this.cs.Dispatch(this.boundaryAndIntervalKernel.Index,
            Mathf.CeilToInt(this.numOfP2GMasses / (float)this.boundaryAndIntervalKernel.ThreadX),
            (int)this.boundaryAndIntervalKernel.ThreadY,
            (int)this.boundaryAndIntervalKernel.ThreadZ);

        //Debug.Log("boundary and interval");
        //Debugger.LogBuffer<uint2>(this.boundaryAndIntervalBuffer, 0, this.numOfP2GMasses);
    }


    public void GatherAndWriteP2G()
    {
        this.cs.SetBuffer(this.gatherAndWriteKernel.Index,
            ShaderID.GridIndicesBuffer, this.gridOptimizer.GetGridIndicesBuffer());
        this.cs.SetBuffer(this.gatherAndWriteKernel.Index,
            ShaderID.GridAndMassIdsBuffer, this.gridAndMassIdsBuffer);
        this.cs.SetBuffer(this.gatherAndWriteKernel.Index,
            ShaderID.BoundaryAndIntervalBuffer, this.boundaryAndIntervalBuffer);
        this.cs.SetBuffer(this.gatherAndWriteKernel.Index,
            ShaderID.P2GMassBuffer, this.p2gMassBuffer);
        this.cs.SetBuffer(this.gatherAndWriteKernel.Index,
            ShaderID.OutputP2gMassBuffer, this.outputP2gMassBuffer);
        this.cs.SetBuffer(this.gatherAndWriteKernel.Index,
            ShaderID.GridBuffer, this.gridBuffer);
        //this.cs.SetBuffer(this.gatherAndWriteKernel.Index,
        //    ShaderID.GridMassBuffer, this.gridMassBuffer);
        //this.cs.SetBuffer(this.gatherAndWriteKernel.Index,
        //    ShaderID.GridMassVelBuffer, this.gridMassVelBuffer);
        this.cs.Dispatch(this.gatherAndWriteKernel.Index,
            Mathf.CeilToInt(this.numOfP2GMasses / (float)this.gatherAndWriteKernel.ThreadX),
            (int)this.gatherAndWriteKernel.ThreadY,
            (int)this.gatherAndWriteKernel.ThreadZ);

        //Debug.Log("p2gMass");
        //Debugger.LogBuffer<uint>(this.outputP2gMassBuffer, 0, this.numOfP2GMasses);

        //Debug.Log("Mass on grid");
        //Debugger.LogBuffer<uint>(this.gridMassBuffer, 0, this.numOfCells);

        //Debug.Log("Mass on grid");
        //Debugger.LogBuffer<LockMpmCell>(this.gridBuffer, 0, this.numOfCells);
    }
    
    
    public Bounds GetGridBounds()
    {
        return new Bounds( Vector3.zero, this.GetGridDimension() );
    }
    
    public Vector3 GetGridDimension()
    {
        return new Vector3(this.gridWidth, this.gridHeight, this.gridDepth);
    }

    public void ReleaseAll()
    {
        Util.ReleaseBuffer(this.gridAndMassIdsBuffer);
        Util.ReleaseBuffer(this.p2gMassBuffer);
        Util.ReleaseBuffer(this.boundaryAndIntervalBuffer);
        Util.ReleaseBuffer(this.outputP2gMassBuffer);
        Util.ReleaseBuffer(this.gridBuffer);
        //Util.ReleaseBuffer(this.gridMassBuffer);
        //Util.ReleaseBuffer(this.gridMassVelBuffer);
        this.gridOptimizer.Release();
    }

}