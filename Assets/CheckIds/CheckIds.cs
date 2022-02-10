using UnityEngine;
using ComputeShaderUtil;

public class CheckIds : MonoBehaviour
{
    public ComputeShader cs;
    public int numOfArray = 128;

    private Kernel kCheckIds;
    private ComputeBuffer groupIdBuffer, groupThreadIdBuffer, groupIndexBuffer;


    #region Shader property IDs
    public static class ShaderID
    {
        public static int GroupIdBuffer = Shader.PropertyToID("_GroupIdBuffer");
        public static int GroupThreadIdBuffer = Shader.PropertyToID("_GroupThreadIdBuffer");
        public static int GroupIndexBuffer = Shader.PropertyToID("_GroupIndexBuffer");
    }
    #endregion

    void Start()
    {
        this.kCheckIds = new Kernel(cs, "Check1DIds");

        this.groupIdBuffer = new ComputeBuffer(this.numOfArray, sizeof(int));
        this.groupThreadIdBuffer = new ComputeBuffer(this.numOfArray, sizeof(int));
        this.groupIndexBuffer = new ComputeBuffer(this.numOfArray, sizeof(int));
        //this.cs.DisableKeyword("T2");
        //this.cs.EnableKeyword("T2");
        this.cs.SetBuffer(this.kCheckIds.Index, ShaderID.GroupIdBuffer, this.groupIdBuffer);
        this.cs.SetBuffer(this.kCheckIds.Index, ShaderID.GroupThreadIdBuffer, this.groupThreadIdBuffer);
        this.cs.SetBuffer(this.kCheckIds.Index, ShaderID.GroupIndexBuffer, this.groupIndexBuffer);

        // グループ数は X * Y * Z で指定します。この例では 1 * 1 * 1 = 1 グループです。
        int numOfGroups = Mathf.CeilToInt(this.numOfArray / this.kCheckIds.ThreadX);
        this.cs.Dispatch(this.kCheckIds.Index, numOfGroups, (int)this.kCheckIds.ThreadY, (int)this.kCheckIds.ThreadZ);
        Debug.Log("NumOfGroups: " + numOfGroups);

        int[] result = new int[this.numOfArray];

        int N = 15;
        Debug.Log("groupId");
        Debugger.LogBuffer<uint>(this.groupIdBuffer, 0, N);
        Debug.Log("---");

        Debug.Log("groupThreadId");
        Debugger.LogBuffer<uint>(this.groupThreadIdBuffer, 0, N);
        Debug.Log("---");

        Debug.Log("groupIndex");
        Debugger.LogBuffer<uint>(this.groupIndexBuffer, 0, N);
        Debug.Log("=========");

        // (5) 使い終わったバッファは必要なら解放します。

        Util.ReleaseBuffer(this.groupIdBuffer);
        Util.ReleaseBuffer(this.groupThreadIdBuffer);
        Util.ReleaseBuffer(this.groupIndexBuffer);
    }
}