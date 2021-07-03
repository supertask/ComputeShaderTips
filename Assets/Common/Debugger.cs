using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComputeShaderUtil {

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
        public static void LogBuffer<T>(ComputeBuffer buffer, int startIndex, int endIndex) where T  : struct
        {
            int N = endIndex - startIndex;
            T[] array = new T[N];
            buffer.GetData(array, 0, startIndex, N);
            for (int i = 0; i < N; i++)
            {
                Debug.LogFormat("index={0}: {1}", startIndex + i, array[i]);
            }
        }
	}

}