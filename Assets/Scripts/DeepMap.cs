using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeepMap : PostEffectsBase
{
    public Shader dmShader;
    private Material dmMaterial;
    public Material material{
        get{
            dmMaterial=CheckShaderAndCreateMaterial(dmShader,dmMaterial);
            return dmMaterial;
        }
    }
    [Range(0.0f,3.0f)]
    public float brightness = 1.0f;
    [Range(0.0f,3.0f)]
    public float saturation = 1.0f;
    [Range(0.0f,3.0f)]
    public float contrast = 1.0f;
    /***
    * 继承自monobehaviour
    * src :unity会把当前渲染得到的图像存储在第一个参数对应的纹理中
    * dest:将dest显示在屏幕上
    ***/
    void OnRenderImage(RenderTexture src,RenderTexture dest){
        if(material != null){
            material.SetFloat("_Brightness",brightness);
            material.SetFloat("_Saturation",saturation);
            material.SetFloat("_Contrast",contrast);

            Graphics.Blit(src,dest,material);
        }else{
            Graphics.Blit(src,dest);
        }
    }
}
