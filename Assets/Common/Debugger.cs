using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComputeShaderUtil
{

    public class Debugger
    {
        //
        // Debug Compute Buffer
        // When you define a struct/class,
        // please use override ToString(), public override string ToString() => $"({A}, {B})";
        //
        // debugging range is startIndex <= x < endIndex
        // example: 
        //    Util.DebugBuffer<uint2>(this.gridAndMassIdsBuffer, 1024, 1027); 
        //
        public static void LogBuffer<T>(ComputeBuffer buffer, int startIndex, int endIndex) where T : struct
        {
            int N = endIndex - startIndex;
            T[] array = new T[N];
            buffer.GetData(array, 0, startIndex, N);
            for (int i = 0; i < N; i++)
            {
                Debug.LogFormat("index={0}: {1}", startIndex + i, array[i]);
            }
        }

        public static bool MatchFloatBuffers(
            ComputeBuffer buffer1, ComputeBuffer buffer2,
            int startIndex, int endIndex)
        {
            int N = endIndex - startIndex;
            float[] array1 = new float[N];
            buffer1.GetData(array1, 0, startIndex, N);
            float[] array2 = new float[N];
            buffer2.GetData(array2, 0, startIndex, N);
            for (int i = 0; i < N; i++)
            {
                if (array1[i] == array2[i])
                {
                    Debug.LogFormat("Correct. index={0}: {1}, {2}",
                        startIndex + i, array1[i], array2[i]);
                }
                else
                {
                    Debug.LogErrorFormat("Incorrect!! index={0}: {1}, {2}",
                        startIndex + i, array1[i], array2[i]);
                    return false;
                }
            }
            return true;
        }
    }

}