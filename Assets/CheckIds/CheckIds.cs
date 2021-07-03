using UnityEngine;
using ComputeShaderUtil;

public class CheckIds : MonoBehaviour
{
    public ComputeShader cs;
    public int numOfArray = 128;

    private Kernel kCheckIds;
    private ComputeBuffer intBuffer;

    void Start()
    {
        this.kCheckIds = new Kernel(cs, "Check1DIds");

        this.intBuffer = new ComputeBuffer(this.numOfArray, sizeof(int));
        this.cs.SetBuffer(this.kCheckIds.Index, "_IntBuffer", this.intBuffer);

        // グループ数は X * Y * Z で指定します。この例では 1 * 1 * 1 = 1 グループです。
        int numOfGroups = (int) (this.numOfArray / this.kCheckIds.ThreadX);
        this.cs.Dispatch(this.kCheckIds.Index,
            numOfGroups, 1, 1);
        Debug.Log("NumOfGroups: " + numOfGroups);

        int[] result = new int[this.numOfArray];

        this.intBuffer.GetData(result);

        for (int i = 0; i < this.numOfArray; i++)
        {
            Debug.LogFormat("i: {0}, intBuffer: {1}", i, result[i]);
        }

        // (5) 使い終わったバッファは必要なら解放します。

        this.intBuffer.Release();
    }
}