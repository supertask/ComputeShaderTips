using System.Linq;
using System.Runtime.InteropServices;

using Unity.Mathematics;
using UnityEngine;

using ComputeShaderUtil;

public class FloatToInt2 : MonoBehaviour
{
    public struct MyFloat
    {
        public float x;
        public override string ToString()
        {
            return $"MyFloat({x.ToString("F7")})";
        }
    }
    public struct MyFloat4
    {
        public float x;
        public float y;
        public float z;
        public float w;
        public override string ToString()
        {
            return $"MyFloat4({x.ToString("F7")}, {y.ToString("F7")}, {z.ToString("F7")}, {w.ToString("F7")})";
        }

    }
    public ComputeShader cs;
    public int numOfArray = 4;
    public bool debug = true;

    private Kernel floatToInt2Kernel;
    private ComputeBuffer inFloatBuffer, outFloatBuffer, int2Buffer, debugBuffer;


    #region Shader property IDs
    public static class ShaderID
    {
        public static int InFloatBuffer = Shader.PropertyToID("_InFloatBuffer");
        public static int OutFloatBuffer = Shader.PropertyToID("_OutFloatBuffer");
        public static int Int2Buffer = Shader.PropertyToID("_Int2Buffer");
        public static int DebugBuffer = Shader.PropertyToID("_DebugBuffer");
    }
    #endregion

    void Start()
    {
        this.CalcFloat();
        this.CalcFloat3();
    }


    private void CalcFloat()
    {
        this.floatToInt2Kernel = new Kernel(cs, "FloatToInt2");

        this.inFloatBuffer = new ComputeBuffer(this.numOfArray, Marshal.SizeOf(typeof(MyFloat)));
        this.outFloatBuffer = new ComputeBuffer(this.numOfArray, Marshal.SizeOf(typeof(MyFloat)));
        this.int2Buffer = new ComputeBuffer(this.numOfArray, Marshal.SizeOf(typeof(int2)));
        this.debugBuffer = new ComputeBuffer(this.numOfArray, Marshal.SizeOf(typeof(MyFloat4)));
        float[] floatArray = Enumerable.Range(0, this.numOfArray)
            .Select(_ => UnityEngine.Random.Range(-1000.0000001f, 1000.0000001f)).ToArray();
        this.inFloatBuffer.SetData(floatArray);
        //Debugger.LogBuffer<float>(this.inFloatBuffer, 0, this.numOfArray);

        this.cs.SetBuffer(this.floatToInt2Kernel.Index, ShaderID.InFloatBuffer, this.inFloatBuffer);
        this.cs.SetBuffer(this.floatToInt2Kernel.Index, ShaderID.OutFloatBuffer, this.outFloatBuffer);
        this.cs.SetBuffer(this.floatToInt2Kernel.Index, ShaderID.Int2Buffer, this.int2Buffer);
        this.cs.SetBuffer(this.floatToInt2Kernel.Index, ShaderID.DebugBuffer, this.debugBuffer);

        // グループ数は X * Y * Z で指定します。この例では 1 * 1 * 1 = 1 グループです。
        int numOfGroups = Mathf.CeilToInt(this.numOfArray / this.floatToInt2Kernel.ThreadX);
        this.cs.Dispatch(this.floatToInt2Kernel.Index, numOfGroups, (int)this.floatToInt2Kernel.ThreadY, (int)this.floatToInt2Kernel.ThreadZ);
        //Debug.Log("NumOfGroups: " + numOfGroups);

        int[] result = new int[this.numOfArray];

        int N = numOfArray;
        if (debug)
        {
            Debug.Log("inFloatBuffer");
            Debugger.LogBuffer<MyFloat>(this.inFloatBuffer, 0, N);
            Debug.Log("---");

            Debug.Log("outFloatBuffer");
            Debugger.LogBuffer<MyFloat>(this.outFloatBuffer, 0, N);
            Debug.Log("---");

            Debug.Log("debugBuffer");
            Debugger.LogBuffer<MyFloat4>(this.debugBuffer, 0, N);
            Debug.Log("---");

            Debug.Log("int2Buffer");
            Debugger.LogBuffer<int2>(this.int2Buffer, 0, N);
            Debug.Log("=========");
        }

        bool isCorrect = FloatToInt2.MatchFloatBuffers(this.inFloatBuffer, this.outFloatBuffer, 0, N);
        Debug.Log("isCorrect: " + isCorrect);

        // (5) 使い終わったバッファは必要なら解放します。

        Util.ReleaseBuffer(this.inFloatBuffer);
        Util.ReleaseBuffer(this.outFloatBuffer);
        Util.ReleaseBuffer(this.debugBuffer);
        Util.ReleaseBuffer(this.int2Buffer);

    }


    private void CalcFloat3()
    {
        /*
        this.floatToInt2Kernel = new Kernel(cs, "Float3ToInt2");

        this.inFloatBuffer = new ComputeBuffer(this.numOfArray, Marshal.SizeOf(typeof(MyFloat)));
        this.outFloatBuffer = new ComputeBuffer(this.numOfArray, Marshal.SizeOf(typeof(MyFloat)));
        this.int2Buffer = new ComputeBuffer(this.numOfArray, Marshal.SizeOf(typeof(int2)));
        this.debugBuffer = new ComputeBuffer(this.numOfArray, Marshal.SizeOf(typeof(MyFloat4)));
        float[] floatArray = Enumerable.Range(0, this.numOfArray)
            .Select(_ => UnityEngine.Random.Range(-1000.0000001f, 1000.0000001f)).ToArray();
        this.inFloatBuffer.SetData(floatArray);
        //Debugger.LogBuffer<float>(this.inFloatBuffer, 0, this.numOfArray);

        this.cs.SetBuffer(this.floatToInt2Kernel.Index, ShaderID.InFloatBuffer, this.inFloatBuffer);
        this.cs.SetBuffer(this.floatToInt2Kernel.Index, ShaderID.OutFloatBuffer, this.outFloatBuffer);
        this.cs.SetBuffer(this.floatToInt2Kernel.Index, ShaderID.Int2Buffer, this.int2Buffer);
        this.cs.SetBuffer(this.floatToInt2Kernel.Index, ShaderID.DebugBuffer, this.debugBuffer);

        // グループ数は X * Y * Z で指定します。この例では 1 * 1 * 1 = 1 グループです。
        int numOfGroups = Mathf.CeilToInt(this.numOfArray / this.floatToInt2Kernel.ThreadX);
        this.cs.Dispatch(this.floatToInt2Kernel.Index, numOfGroups, (int)this.floatToInt2Kernel.ThreadY, (int)this.floatToInt2Kernel.ThreadZ);
        //Debug.Log("NumOfGroups: " + numOfGroups);

        int[] result = new int[this.numOfArray];

        int N = numOfArray;
        if (debug)
        {
            Debug.Log("inFloatBuffer");
            Debugger.LogBuffer<MyFloat>(this.inFloatBuffer, 0, N);
            Debug.Log("---");

            Debug.Log("outFloatBuffer");
            Debugger.LogBuffer<MyFloat>(this.outFloatBuffer, 0, N);
            Debug.Log("---");

            Debug.Log("debugBuffer");
            Debugger.LogBuffer<MyFloat4>(this.debugBuffer, 0, N);
            Debug.Log("---");

            Debug.Log("int2Buffer");
            Debugger.LogBuffer<int2>(this.int2Buffer, 0, N);
            Debug.Log("=========");
        }

        bool isCorrect = FloatToInt2.MatchFloatBuffers(this.inFloatBuffer, this.outFloatBuffer, 0, N);
        Debug.Log("isCorrect: " + isCorrect);

        // (5) 使い終わったバッファは必要なら解放します。

        Util.ReleaseBuffer(this.inFloatBuffer);
        Util.ReleaseBuffer(this.outFloatBuffer);
        Util.ReleaseBuffer(this.debugBuffer);
        Util.ReleaseBuffer(this.int2Buffer);


        */
    }


    public static bool MatchFloatBuffers(
        ComputeBuffer buffer1, ComputeBuffer buffer2,
        int startIndex, int endIndex)
    {
        int N = endIndex - startIndex;
        MyFloat[] array1 = new MyFloat[N];
        buffer1.GetData(array1, 0, startIndex, N);
        MyFloat[] array2 = new MyFloat[N];
        buffer2.GetData(array2, 0, startIndex, N);
        for (int i = 0; i < N; i++)
        {
            if (array1[i].x == array2[i].x)
            {
                //Debug.LogFormat("Correct. index={0}: {1}, {2}",
                //    startIndex + i, array1[i], array2[i]);
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