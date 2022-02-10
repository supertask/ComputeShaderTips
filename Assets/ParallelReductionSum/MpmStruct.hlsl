//#ifndef MPM_STRUCT_INCLUDED
//#define MPM_STRUCT_INCLUDED

struct MpmParticle {
    int type;
	float3 position;
	float3 velocity;
    float mass;
    float volume;
    float3x3 C; //C = D * B at Affine particle in cell
    float3x3 Fe;
    float Jp;
};

struct MpmCell {
    float mass;
    float3 mass_x_velocity;
    float3 velocity;
    float3 force;
	float2 padding;
};

struct LockMpmCell {
    int mass;
    int mass2;
    int3 mass_x_velocity;
    int3 mass_x_velocity2;

    //int2 mass_pack;
    //int2 mass_x_velocity_x_pack;
    //int2 mass_x_velocity_y_pack;
    //int2 mass_x_velocity_z_pack;
    float3 velocity;
    float3 force;
	float2 padding;
};

// For GPU optimazation of MPM
// https://dl.acm.org/doi/10.1145/3272127.3275044
// http://pages.cs.wisc.edu/~sifakis/papers/GPU_MPM.pdf
struct P2GMass {
	float mass;
	float3 mass_x_velocity;
};

//#endif