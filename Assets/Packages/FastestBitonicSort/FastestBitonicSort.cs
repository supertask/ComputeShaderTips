using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;
using Unity.Mathematics;

namespace Sorting.BitonicSort
{
    public class FastestBitonicSort
    {
        private int kernel_ParallelBitonic_B16;
        private int kernel_ParallelBitonic_B8;
        private int kernel_ParallelBitonic_B4;
        private int kernel_ParallelBitonic_B2;
        private int kernel_ParallelBitonic_C4;
        private int kernel_ParallelBitonic_C2;
        private ComputeShader bitonicSortCS;
        const int THREADNUM_X = 4;//これを変えたらComputeSahder側も変えないといけない
        const int GROUP_SHARED_SIZE = 8;//これを変えたらComputeSahder側も変えないといけない

        //配列の要素数
        //int N = 1 << 21;//下限 1<<8,上限 1<<27
        public FastestBitonicSort()
        {
            //host_data = new uint2[N];//ソートしたいデータ
            //inBuffer = new ComputeBuffer(host_data.Length, Marshal.SizeOf(typeof(uint2)) );
            this.bitonicSortCS = (ComputeShader)Resources.Load("FastestBitonicSort");
            InitKernels();
        }

        void InitKernels()
        {
            kernel_ParallelBitonic_B16 = bitonicSortCS.FindKernel("ParallelBitonic_B16");
            kernel_ParallelBitonic_B8 = bitonicSortCS.FindKernel("ParallelBitonic_B8");
            kernel_ParallelBitonic_B4 = bitonicSortCS.FindKernel("ParallelBitonic_B4");
            kernel_ParallelBitonic_B2 = bitonicSortCS.FindKernel("ParallelBitonic_B2");
            kernel_ParallelBitonic_C4 = bitonicSortCS.FindKernel("ParallelBitonic_C4");
            kernel_ParallelBitonic_C2 = bitonicSortCS.FindKernel("ParallelBitonic_C2");
        }


        public void Sort(ref ComputeBuffer inBuffer)
        {
            int numOfElements = inBuffer.count;
            //引数をセット
            bitonicSortCS.SetBuffer(kernel_ParallelBitonic_B16, "data", inBuffer);
            bitonicSortCS.SetBuffer(kernel_ParallelBitonic_B8, "data", inBuffer);
            bitonicSortCS.SetBuffer(kernel_ParallelBitonic_B4, "data", inBuffer);
            bitonicSortCS.SetBuffer(kernel_ParallelBitonic_B2, "data", inBuffer);
            bitonicSortCS.SetBuffer(kernel_ParallelBitonic_C4, "data", inBuffer);
            bitonicSortCS.SetBuffer(kernel_ParallelBitonic_C2, "data", inBuffer);

            int nlog = (int)(Mathf.Log(numOfElements, 2));
            int B_indx, inc;
            int kernel_id;

            for (int i = 0; i < nlog; i++)
            {
                inc = 1 << i;
                for (int j = 0; j < i + 1; j++)
                {
                    //if (inc <= 128) break;//あとはshared memory内におさまるので
                    if (inc <= GROUP_SHARED_SIZE/2) break;//あとはshared memory内におさまるので

                    if (inc >= 2048)
                    {
                        B_indx = 16;
                        kernel_id = kernel_ParallelBitonic_B16;
                    }
                    else if (inc >= 1024)
                    {
                        B_indx = 8;
                        kernel_id = kernel_ParallelBitonic_B8;
                    }
                    else if (inc >= 512)
                    {
                        B_indx = 4;
                        kernel_id = kernel_ParallelBitonic_B4;
                    }
                    else 
                    {
                        B_indx = 2;
                        kernel_id = kernel_ParallelBitonic_B2;
                    }

                    bitonicSortCS.SetInt("inc", inc * 2 / B_indx);
                    bitonicSortCS.SetInt("dir", 2 << i);
                    bitonicSortCS.Dispatch(kernel_id, numOfElements / B_indx / THREADNUM_X, 1, 1);
                    inc /= B_indx;
                }

                //これ以降はshared memoryに収まりそうなサイズなので
                bitonicSortCS.SetInt("inc0", inc);
                bitonicSortCS.SetInt("dir", 2 << i);
                if ((inc == 8) | (inc == 32) | (inc == 128) | (inc == 256))
                {
                    //bitonicSortCS.Dispatch(kernel_ParallelBitonic_C4, numOfElements / 4 / 64, 1, 1);
                    bitonicSortCS.Dispatch(kernel_ParallelBitonic_C4, numOfElements / GROUP_SHARED_SIZE, 1, 1);
                }
                else 
                {
                    //bitonicSortCS.Dispatch(kernel_ParallelBitonic_C2, numOfElements / 2 / 128, 1, 1);
                    bitonicSortCS.Dispatch(kernel_ParallelBitonic_C2, numOfElements / GROUP_SHARED_SIZE, 1, 1);
                }
            }

            //Debug.Log("要素数=" + inBuffer.count);
            //resultDebug();
        }

        public void SortWithoutSharedMemory(ComputeBuffer inBuffer)
        {
            int numOfElements = inBuffer.count;
            //引数をセット
            bitonicSortCS.SetBuffer(kernel_ParallelBitonic_B16, "data", inBuffer);
            bitonicSortCS.SetBuffer(kernel_ParallelBitonic_B8, "data", inBuffer);
            bitonicSortCS.SetBuffer(kernel_ParallelBitonic_B4, "data", inBuffer);
            bitonicSortCS.SetBuffer(kernel_ParallelBitonic_B2, "data", inBuffer);

            int nlog = (int)(Mathf.Log(numOfElements, 2));
            int B_indx, inc;
            int kernel_id;

            for (int i = 0; i < nlog; i++)
            {
                inc = 1 << i;
                for (int j = 0; j < i + 1; j++)
                {
                    if (inc == 0) break;

                    if ((inc >= 8) & (nlog >= 10))
                    {
                        B_indx = 16;
                        kernel_id = kernel_ParallelBitonic_B16;
                    }
                    else if ((inc >= 4) & (nlog >= 9))
                    {
                        B_indx = 8;
                        kernel_id = kernel_ParallelBitonic_B8;
                    }
                    else if ((inc >= 2) & (nlog >= 8))
                    {
                        B_indx = 4;
                        kernel_id = kernel_ParallelBitonic_B4;
                    }
                    else
                    {
                        B_indx = 2;
                        kernel_id = kernel_ParallelBitonic_B2;
                    }

                    bitonicSortCS.SetInt("inc", inc * 2 / B_indx);
                    bitonicSortCS.SetInt("dir", 2 << i);
                    bitonicSortCS.Dispatch(kernel_id, numOfElements / B_indx / THREADNUM_X, 1, 1);
                    inc /= B_indx;
                }
            }
        }





        void BitonicSort_normal(ComputeBuffer inBuffer)
        {
            int numOfElements = inBuffer.count;
            //引数をセット
            bitonicSortCS.SetBuffer(kernel_ParallelBitonic_B2, "data", inBuffer);

            int nlog = (int)(Mathf.Log(numOfElements, 2));
            int B_indx, inc;

            for (int i = 0; i < nlog; i++)
            {
                inc = 1 << i;
                for (int j = 0; j < i + 1; j++)
                {
                    B_indx = 2;
                    bitonicSortCS.SetInt("inc", inc * 2 / B_indx);
                    bitonicSortCS.SetInt("dir", 2 << i);
                    bitonicSortCS.Dispatch(kernel_ParallelBitonic_B2, numOfElements / B_indx / THREADNUM_X, 1, 1);
                    inc /= B_indx;
                }
            }
        }







        void resultDebug(ComputeBuffer inBuffer)
        {
            uint2[] host_data = new uint2[inBuffer.count];
            // device to host
            inBuffer.GetData(host_data);
            
            Debug.Log("GPU上でソートした結果");
            for (int i = 0; i < Mathf.Min(1024, inBuffer.count); i++)
            {
                Debug.Log("index="+host_data[i].y+" key=" +host_data[i].x);
            }

            /*
            int flag = 0;
            for (int i = 1; i < inBuffer.count; i++)
            {
                if (host_data[i].key > host_data[i - 1].key){
                    flag = 1;
                    break;
                }
            }

            if (flag == 1)
            {
                Debug.Log("ソートできてない！");
            }
            */

        }


        private void OnDestroy()
        {
        }
    }
}