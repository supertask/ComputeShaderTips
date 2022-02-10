using UnityEngine;
using Unity.Mathematics;

using ComputeShaderUtil;
using Sorting.BitonicSort;

public abstract class GridOptimizerBase {

    protected ComputeBuffer gridBuffer;
    protected ComputeBuffer gridPingPongBuffer;
    protected ComputeBuffer gridIndicesBuffer;
    protected ComputeBuffer sortedObjectsBufferOutput;

    protected int numObjects;

    //BitonicSort bitonicSort;
    FastestBitonicSort bitonicSort;


    protected ComputeShader GridSortCS;
    //protected static readonly int SIMULATION_BLOCK_SIZE_FOR_GRID = 256;
    

    protected int threadGroupSize;
    protected int numGrid;
    protected float gridH;
    protected Vector3 cellStartPos;
    private Kernel clearGridIndicesKernel, buildGridIndicesKernel, rearrangeParticlesKernel;

    public GridOptimizerBase(int numObjects) {
        this.numObjects = numObjects;
        //Debug.Log(numObjects);

        //this.threadGroupSize = Mathf.CeilToInt(numObjects / SIMULATION_BLOCK_SIZE_FOR_GRID);

        //this.bitonicSort = new BitonicSort();
        this.bitonicSort = new FastestBitonicSort();
    }

    #region Accessor
    /*
    public void SetNumObjects(int numObjects) {
        this.bitonicSort.SetNumElements(numObjects);
    }
    */
    public float GetGridH() {
        return gridH;
    }

    public ComputeBuffer GetGridBuffer() {
        return this.gridBuffer;
    }

    public ComputeBuffer GetGridIndicesBuffer() {
        return this.gridIndicesBuffer;
    }
    #endregion

    public void Release() {
        DestroyBuffer(gridBuffer);
        DestroyBuffer(gridIndicesBuffer);
        DestroyBuffer(gridPingPongBuffer);
        DestroyBuffer(sortedObjectsBufferOutput);
    }

    void DestroyBuffer(ComputeBuffer buffer) {
        if (buffer != null) {
            buffer.Release();
            buffer = null;
        }
    }

    //public void GridSort(ref ComputeBuffer particlesBuffer) {
    //output: gridAndMassIdsBuffer, sortedP2gMassBuffer
    public void GridSort(
        ComputeBuffer gridAndMassIdsBuffer) {

        GridSortCS.SetInt("_NumOfMasses", numObjects);
        SetCSVariables();

        //int kernel = 0;


        //int startIndex = 1024*4+300;
        //Util.DebugBuffer<uint2>(gridAndMassIdsBuffer, startIndex, startIndex+5);
        //Debug.Log("-----");

        //
        // Sort by grid index
        // output: gridAndMassIdsBuffer
        //
        //これがないと状態(コメントアウトする状態)だと，確実にバグる
        //ある状態でも，たまにバグる
        bitonicSort.Sort(ref gridAndMassIdsBuffer);

        //startIndex = 1024*20;
        //Util.DebugBuffer<uint2>(gridAndMassIdsBuffer, startIndex, startIndex+5);
        //Debug.Log("==========");

        this.clearGridIndicesKernel = new Kernel(this.GridSortCS, "ClearGridIndicesCS");
        GridSortCS.SetBuffer(this.clearGridIndicesKernel.Index, "_GridIndicesBuffer", gridIndicesBuffer);
        GridSortCS.Dispatch(this.clearGridIndicesKernel.Index,
            Mathf.CeilToInt(this.numGrid /  (float)clearGridIndicesKernel.ThreadX),
            (int)this.clearGridIndicesKernel.ThreadY,
            (int)this.clearGridIndicesKernel.ThreadZ);

        this.buildGridIndicesKernel = new Kernel(this.GridSortCS, "BuildGridIndicesCS");
        GridSortCS.SetBuffer(buildGridIndicesKernel.Index, "_GridAndMassIdsBuffer", gridAndMassIdsBuffer);
        GridSortCS.SetBuffer(buildGridIndicesKernel.Index, "_GridIndicesBuffer", gridIndicesBuffer);
        GridSortCS.Dispatch(this.buildGridIndicesKernel.Index,
            Mathf.CeilToInt(this.numObjects /  (float)buildGridIndicesKernel.ThreadX),
            (int)this.buildGridIndicesKernel.ThreadY,
            (int)this.buildGridIndicesKernel.ThreadZ);

        //Util.DebugBuffer<uint2>(gridAndMassIdsBuffer, startIndex, startIndex+3);
    }

    #region GPUSort
    
    #endregion GPUSort 

    protected abstract void InitializeBuffer();

    protected abstract void SetCSVariables();
}
