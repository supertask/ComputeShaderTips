# ComputeShaderTips

## Group Parallel Reduction Sum for MLS-MPM Fluid

Group Parallel Reduction Sum is writtern on [GPU Optimization of Material Point Methods](https://pages.cs.wisc.edu/~sifakis/papers/GPU_MPM.pdf).

<img src="https://user-images.githubusercontent.com/5733604/153383972-1ef1f4c5-86f4-42aa-ae85-7f280d8feebe.png" width="320px" title = "Optimized particles-to-grid transfer" alt=". Optimized particles-to-grid transfer">


## Converter between Float and Int2

If you want to use InterlockedAdd with float value, you need to use this converter. And also this converter is tested by generatiting a bunch of random float values.
Please check out the code bellow.

https://github.com/supertask/ComputeShaderTips/blob/main/Assets/FloatToInt2/FloatToInt2.compute#L24-L58


## CheckIds

Plotter for groud id, groupThreadID, gropIndex
