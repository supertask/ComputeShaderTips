using UnityEngine;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace NearestNeighbour {

    public class GridOptimizer3D<T> : GridOptimizerBase where T : struct {
        
        private Vector3 gridDim;

        public GridOptimizer3D(int numObjects, Vector3 range, Vector3 dimension) : base(numObjects) {
            this.gridDim = dimension;
            this.numGrid = (int)(dimension.x * dimension.y * dimension.z);
            this.gridH = range.x / gridDim.x;
            this.cellStartPos = Vector3.zero;

            this.GridSortCS = (ComputeShader)Resources.Load("GridSort3D");

            InitializeBuffer();

            Debug.Log("=== Instantiated Grid Sort === \nRange : " + range + "\nNumGrid : " + numGrid + "\nGridDim : " + gridDim + "\nGridH : " + gridH);
        }

        protected override void InitializeBuffer() {
            Debug.Log("numObjects: " + numObjects);

            //Uint2[] gridAndMassIds = Enumerable.Range(0, this.numObjects)
            //    .Select(_ => new Uint2(100, 200)).ToArray();
            gridBuffer = new ComputeBuffer(numObjects, Marshal.SizeOf(typeof(Uint2)));
            //gridBuffer.SetData(gridAndMassIds);

            gridPingPongBuffer = new ComputeBuffer(numObjects, Marshal.SizeOf(typeof(uint2)));
            //gridPingPongBuffer = new ComputeBuffer(numObjects, Marshal.SizeOf(typeof(Uint2)));

            gridIndicesBuffer = new ComputeBuffer(numGrid, Marshal.SizeOf(typeof(uint2)));
            //gridIndicesBuffer = new ComputeBuffer(numGrid, Marshal.SizeOf(typeof(Uint2)));
            sortedObjectsBufferOutput = new ComputeBuffer(numObjects, Marshal.SizeOf(typeof(T)));
        }

        public void SetCellStartPos(Vector3 cellStartPos) {
            this.cellStartPos = cellStartPos;
        }

        protected override void SetCSVariables() {
            GridSortCS.SetVector("_GridDim", gridDim);
            GridSortCS.SetFloat("_GridH", gridH);
            GridSortCS.SetVector("_CellStartPos", cellStartPos);
        }

    }
}