Shader "FullScreen/FullScreenCustomPass"
{
    Properties
    {
        //调整外轮廓绘制的变量
        _SamplePrecision ("Sampling Precision", Range(1, 3)) = 1
        _OutlineWidth ("Outline Width", Float) = 2
        _OuterColor ("Outer Color", Color) = (1, 1, 0, 1)
        
        //遮挡填充图
        _InnerColor ("Inner Color", Color) = (1, 1, 0, 1)
        _Texture ("Texture", 2D) = "black" { }
        _TextureSize ("Texture Pixels Size", float) = 32
    }
    HLSLINCLUDE

    #pragma vertex Vert

    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"

    // The PositionInputs struct allow you to retrieve a lot of useful information for your fullScreenShader:
    // struct PositionInputs
    // {
    //     float3 positionWS;  // World space position (could be camera-relative)
    //     float2 positionNDC; // Normalized screen coordinates within the viewport    : [0, 1) (with the half-pixel offset)
    //     uint2  positionSS;  // Screen space pixel coordinates                       : [0, NumPixels)
    //     uint2  tileCoord;   // Screen tile coordinates                              : [0, NumTiles)
    //     float  deviceDepth; // Depth from the depth buffer                          : [0, 1] (typically reversed)
    //     float  linearDepth; // View space Z coordinate                              : [Near, Far]
    // };

    // To sample custom buffers, you have access to these functions:
    // But be careful, on most platforms you can't sample to the bound color buffer. It means that you
    // can't use the SampleCustomColor when the pass color buffer is set to custom (and same for camera the buffer).
    // float4 SampleCustomColor(float2 uv);
    // float4 LoadCustomColor(uint2 pixelCoords);
    // float LoadCustomDepth(uint2 pixelCoords);
    // float SampleCustomDepth(float2 uv);

    // There are also a lot of utility function you can use inside Common.hlsl and Color.hlsl,
    // you can check them out in the source code of the core SRP package.

    //定义不同角度的三角函数
    #define c45 0.707107
    #define c225 0.9238795
    #define s225 0.3826834

    #define MAXSAMPLES 16
    static float2 offsets[MAXSAMPLES] = {
        float2(1, 0),
        float2(-1, 0),
        float2(0, 1),
        float2(0, -1),

        float2(c45, c45),
        float2(c45, -c45),
        float2(-c45, c45),
        float2(-c45, -c45),

        float2(c225, s225),
        float2(c225, -s225),
        float2(-c225, s225),
        float2(-c225, -s225),
        float2(s225, c225),
        float2(s225, -c225),
        float2(-s225, c225),
        float2(-s225, -c225)
    };
    //再次声明同名变量
    int _SamplePrecision = 1;
    float _OutlineWidth = 2;
    float4 _OuterColor;
    float4 _InnerColor;
    // texture2D _Texture; 在原来的CG语法当中,当属性中定义了一个纹理类型的变量
    // Properties{
    //     _MainText("Main Texture",2D) = "white"{}
    // }
    // 我们想要在CGPROGRAM中引用这个纹理,就需要声明一个采样器。sampler2D _MainTex;
    // 而在HLSL中,sampler2D这个对象被拆分为两部分,即纹理对象和采样器。你需要同时声明两个变量来保存它们。如下所示。
    TEXTURE2D(_Texture);
    SAMPLER(sampler_Texture);
    float _TextureSize = 1024;
    float _BehindFactor = 0.7;  //在这里的初始化没用

    float4 FullScreenPass(Varyings varyings) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);
        float depth = LoadCameraDepth(varyings.positionCS.xy);
        PositionInputs posInput = GetPositionInput(varyings.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
        float3 viewDirection = GetWorldSpaceNormalizeViewDir(posInput.positionWS);
        // float4 color = float4(0.0, 0.0, 0.0, 0.0);

        // // Load the camera color buffer at the mip 0 if we're not at the before rendering injection point
        // if (_CustomPassInjectionPoint != CUSTOMPASSINJECTIONPOINT_BEFORE_RENDERING)
        //     color = float4(CustomPassLoadCameraColor(varyings.positionCS.xy, 0), 1);

        // Add your custom pass code here !!!!!!!
        //CustomColorBuffer中本次处理的像素的颜色值   ColorBuffer中存储的DrawRendererCunstomPass生成的图片,除了渲染的物体是白色,其他地方都是透明的
        float4 c = LoadCustomColor(posInput.positionSS); //LoadCustomColor() 和 SampleCustomColor()两种方法获取像素颜色，参数不同 SS表示 screen space

        //调整采样次数以4, 8, 16幂次增长
        int sampleCount = min(2 * pow(2, _SamplePrecision), MAXSAMPLES) ;

        //计算每像素之间的uv差
        float2 uvOffsetPerPixel = 1.0 / _ScreenSize .xy;

        float4 outline = 0;
        for (uint i = 0; i < sampleCount; ++ i)
        {
            //取sampleCount次采样中的最大值   对于屏幕中其他物体,sampleCustom中的值为(0,0,0,0),outline的值为0
            outline = max(SampleCustomColor(posInput.positionNDC + uvOffsetPerPixel * _OutlineWidth * offsets[i]), outline);
        }

        //去掉原本纯色块的部分 这个shader只是在渲染好的画面上再加一层,当outline=0,透明,不影响原先物体的显示
        //outline *= _OuterColor * (1 - c.a);

        //非描边的内容用斜线填充了，则不再需要这步操作了
        //outline *= _OuterColor * (1 - c.a);

        //读取CustomDepthBuffer
        float d = LoadCustomDepth(posInput.positionSS);

        //进行深度的判断,如果判定为被遮挡则用我们设置的透明度,反之则为0。
        //0.0000001为bias,避免浮点数的精度问题导致的误差。
        _BehindFactor = 0.7;
        float alphaFactor = (depth > d + 0.0000001) ? _BehindFactor : 0;
        //对InnerColorTexture进行采样
        float4 innerColor = SAMPLE_TEXTURE2D(_Texture, s_trilinear_repeat_sampler, posInput.positionSS / _TextureSize) * _InnerColor;
        //float4 innerColor = SAMPLE_TEXTURE2D(_Texture, sampler_Texture, posInput.positionSS / _TextureSize) * _InnerColor;

        innerColor.a *= alphaFactor;

        float4 output = 0;
        //将描边赋值给output
        output = lerp(output, _OuterColor * float4(outline.rgb, 1), outline.a);
        //将纯色色块覆盖的区域以InnerColor替代  c 是CustomColorBuffer中的颜色值
        output = lerp(output, innerColor * float4(c.rgb, 1), c.a);
        //output = innerColor * float4(c.rgb, 1);
        //output = float4(alphaFactor,alphaFactor,alphaFactor,1);
        return output;
    }

    ENDHLSL

    SubShader
    {
        Pass
        {
            Name "Custom Pass 0"

            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha 
            //其中src 是 source,源,unity这里指当前片元着色器输出的颜色,SrcAlpha自然就是片元着色器返回的四维变量的z值
            //Dst是指缓存区中的颜色,表示上一次渲染的颜色
            //这句话说,使用此Shader的物体,开启混合模式,(此时片元着色器返回的四维变量的z值才有意义),使用这个z*srcColor+(1-z)*dstColor
            Cull Off

            HLSLPROGRAM
                #pragma fragment FullScreenPass
            ENDHLSL
        }
    }
    Fallback Off
}
