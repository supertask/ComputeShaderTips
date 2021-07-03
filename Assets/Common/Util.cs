using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComputeShaderUtil {

	public class Util
	{
        public static void ReleaseBuffer(ComputeBuffer buffer)
        {
            if (buffer != null)
            {
                buffer.Release();
                buffer = null;
            }
        }

	}

}