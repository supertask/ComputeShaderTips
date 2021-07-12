using System.Linq;
using System.Runtime.InteropServices;

using UnityEngine;
using Unity.Mathematics;

using ComputeShaderUtil;

public class ParallelReductionGroupSum : MonoBehaviour
{

    public struct InterlockedMpmCell {
        public int mass;
        public int3 mass_x_velocity;
        public float3 velocity;
        public float3 force;
        public float2 padding;

        public override string ToString() => $"InterlockedMpmCell(mass={mass}, mass_x_velocity={mass_x_velocity})";

    };

    public ComputeShader cs;
    public int numOfArray = 128;

    private Kernel clearMassBuffersKernel;
    private Kernel clearGridBuffersKernel;
    private Kernel boundaryAndIntervalKernel;
    private Kernel gatherAndWriteKernel;
    private int numOfP2GMasses;
    private int numOfCells;

    private ComputeBuffer gridAndMassIdsBuffer;
    private ComputeBuffer p2gMassBuffer;
    private ComputeBuffer boundaryAndIntervalBuffer;
    private ComputeBuffer gridIndicesBuffer;
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
        public static int GridIndicesBuffer = Shader.PropertyToID("_GridIndicesBuffer");
        public static int GridAndMassIdsBuffer = Shader.PropertyToID("_GridAndMassIdsBuffer");
        public static int BoundaryAndIntervalBuffer = Shader.PropertyToID("_BoundaryAndIntervalBuffer");
        public static int GridBuffer = Shader.PropertyToID("_GridBuffer");
        //public static int GridMassBuffer = Shader.PropertyToID("_GridMassBuffer");
        //public static int GridMassVelBuffer = Shader.PropertyToID("_GridMassVelBuffer");

        public static int NumOfParticles = Shader.PropertyToID("_NumOfParticles");
        public static int NumOfP2GMasses = Shader.PropertyToID("_NumOfP2GMasses");
        public static int CellNeighbourLength = Shader.PropertyToID("_CellNeighbourLength");
    }
    #endregion

    void Start()
    {
        this.clearMassBuffersKernel = new Kernel(cs, "ClearMassBuffers");
        this.clearGridBuffersKernel = new Kernel(cs, "ClearGridBuffers");
        this.boundaryAndIntervalKernel = new Kernel(cs, "BoundaryAndInterval");
        this.gatherAndWriteKernel = new Kernel(cs, "GatherAndWrite");

        // mass length
        //                      grid index   0, 3,  3, 3, 3, 7,  7, 9
        uint[] masses =         new uint[] { 5, 7,  8, 1, 3, 2,  9, 6 }; //8

        //                      grid index   0, 3,  3, 3, 3,  7, 7, 9
        uint[] answerMassSum =  new uint[] { 5, 19, 0, 0, 0, 11, 0, 6 }; //8
        this.numOfP2GMasses = masses.Length;

        uint2[] gridAndMassIds = new uint2[] {
            // same with masses.length
            // x = grid index, y = mass index
            new uint2(0, 0),
            new uint2(3, 1),
            new uint2(3, 2),
            new uint2(3, 3),
            new uint2(3, 4),
            new uint2(7, 5),
            new uint2(7, 6),
            new uint2(9, 7)
        };
        uint2[] gridIndices = new uint2[] {
            // same with grid.Length
            // x = grid index start, y = grid index end
            new uint2(0, 1),
            new uint2(0, 0),
            new uint2(0, 0),
            new uint2(1, 5),
            new uint2(0, 0),
            new uint2(0, 0),
            new uint2(0, 0),
            new uint2(5, 7),
            new uint2(0, 0),
            new uint2(7, 8)
        };
        this.numOfCells = gridIndices.Length;

        uint2[] answerBoundaryAndInterval = new uint2[] {
            // same with masses.length
            // x = boundary, y = intervals
            new uint2(1, 0), //0
            new uint2(1, 3),
            new uint2(0, 2),
            new uint2(0, 1),
            new uint2(0, 0), //4
            new uint2(1, 1),
            new uint2(0, 0),
            new uint2(1, 0) //7
        };

        // Input Compute Buffers
        this.p2gMassBuffer = new ComputeBuffer(this.numOfP2GMasses, sizeof(uint));
        this.gridAndMassIdsBuffer = new ComputeBuffer(this.numOfP2GMasses, Marshal.SizeOf(typeof(uint2)) );
        this.gridIndicesBuffer = new ComputeBuffer(this.numOfCells, Marshal.SizeOf(typeof(uint2)) );
        this.p2gMassBuffer.SetData(masses);
        this.gridAndMassIdsBuffer.SetData(gridAndMassIds);
        this.gridIndicesBuffer.SetData(gridIndices);

        // Output Compute Buffers
        this.boundaryAndIntervalBuffer = new ComputeBuffer(this.numOfP2GMasses,
            Marshal.SizeOf(typeof(uint2)) );
        this.outputP2gMassBuffer = new ComputeBuffer(this.numOfP2GMasses, sizeof(uint));
        this.gridBuffer = new ComputeBuffer(this.numOfCells, Marshal.SizeOf(typeof(InterlockedMpmCell)) );
        //this.gridMassBuffer = new ComputeBuffer(this.numOfCells, Marshal.SizeOf(typeof(int)) );
        //this.gridMassVelBuffer = new ComputeBuffer(this.numOfCells, Marshal.SizeOf(typeof(int3)) );

        this.ClearBuffers();
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
        //this.cs.SetBuffer(this.clearGridBuffersKernel.Index,
        //    ShaderID.GridMassBuffer, this.gridMassBuffer);
        //this.cs.SetBuffer(this.clearGridBuffersKernel.Index,
        //    ShaderID.GridMassVelBuffer, this.gridMassVelBuffer);
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
            ShaderID.GridIndicesBuffer, this.gridIndicesBuffer);
        this.cs.SetBuffer(this.boundaryAndIntervalKernel.Index,
            ShaderID.BoundaryAndIntervalBuffer, this.boundaryAndIntervalBuffer);
        this.cs.Dispatch(this.boundaryAndIntervalKernel.Index,
            Mathf.CeilToInt(this.numOfP2GMasses / (float)this.boundaryAndIntervalKernel.ThreadX),
            (int)this.boundaryAndIntervalKernel.ThreadY,
            (int)this.boundaryAndIntervalKernel.ThreadZ);

        Debug.Log("boundary and interval");
        Debugger.LogBuffer<uint2>(this.boundaryAndIntervalBuffer, 0, this.numOfP2GMasses);
    }


    public void GatherAndWriteP2G()
    {
        this.cs.SetBuffer(this.gatherAndWriteKernel.Index,
            ShaderID.GridIndicesBuffer, this.gridIndicesBuffer);
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

        Debug.Log("p2gMass");
        Debugger.LogBuffer<uint>(this.outputP2gMassBuffer, 0, this.numOfP2GMasses);
        //Debug.Log("Mass on grid");
        //Debugger.LogBuffer<uint>(this.gridMassBuffer, 0, this.numOfCells);
        Debug.Log("Mass on grid");
        Debugger.LogBuffer<InterlockedMpmCell>(this.gridBuffer, 0, this.numOfCells);
    }

    public void ReleaseAll()
    {
        Util.ReleaseBuffer(this.gridAndMassIdsBuffer);
        Util.ReleaseBuffer(this.p2gMassBuffer);
        Util.ReleaseBuffer(this.boundaryAndIntervalBuffer);
        Util.ReleaseBuffer(this.gridIndicesBuffer);
        Util.ReleaseBuffer(this.outputP2gMassBuffer);
        Util.ReleaseBuffer(this.gridBuffer);
        //Util.ReleaseBuffer(this.gridMassBuffer);
        //Util.ReleaseBuffer(this.gridMassVelBuffer);
    }

}