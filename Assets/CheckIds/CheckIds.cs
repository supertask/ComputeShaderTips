using UnityEngine;
using ComputeShaderUtil;

public class CheckIds : MonoBehaviour
{
    public ComputeShader csCheckIds;
    public int numOfArray = 128;

    private Kernel kCheckIds;
    private ComputeBuffer intBuffer;

    void Start()
    {
        // (1) カーネルのインデックスを保存します。

        this.kCheckIds = new Kernel(csCheckIds, "Check1DIds");

        // (2) csCheckIds で計算した結果を保存するためのバッファ (ComputeBuffer) を設定します。
        // csCheckIds 内に、同じ型で同じ名前のバッファが定義されている必要があります。

        // ComputeBuffer は どの程度の領域を確保するかを指定して初期化する必要があります。
        // この例だと int 4 つ分です。

        this.intBuffer = new ComputeBuffer(this.numOfArray, sizeof(int));
        this.csCheckIds.SetBuffer(this.kCheckIds.Index, "_IntBuffer", this.intBuffer);

        // グループ数は X * Y * Z で指定します。この例では 1 * 1 * 1 = 1 グループです。
        int numOfGroups = (int) (this.numOfArray / this.kCheckIds.ThreadX);
        this.csCheckIds.Dispatch(this.kCheckIds.Index,
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