using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Sorajiro.Editor
{
    // Original tileable surface recipes. No reference-photo projection or downloaded textures.
    public static class TwilightSurfaceBuilder
    {
        private static Texture2D Save(string folder,string name,bool normal,System.Func<float,float,Color> pixel,bool linear=false)
        {
            const int size=512; var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)texture.SetPixel(x,y,pixel((float)x/size,(float)y/size));
            texture.Apply();string path=folder+"/"+name+".png";File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            if(normal)importer.textureType=TextureImporterType.NormalMap;
            importer.sRGBTexture=!(normal || linear);importer.wrapMode=TextureWrapMode.Repeat;importer.anisoLevel=8;importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        public static void Apply(Material[] materials,string folder)
        {
            var oak=Save(folder,"OakGrowth_original",false,(u,v)=>
            {
                float warp=.025f*Mathf.Sin(v*Mathf.PI*2)+.009f*Mathf.Sin(v*Mathf.PI*8);
                float rings=Mathf.PerlinNoise(12+(u+warp)*48,31+v*2);
                float pores=Mathf.Pow(Mathf.PerlinNoise(7+u*140,18+v*4),5);
                float grain=.90f+.10f*rings-.04f*pores;
                return new Color(grain,grain*.975f,grain*.94f);
            });
            // HDRP mask convention: R metallic, G AO, B detail mask, A smoothness.
            var oakMask=Save(folder,"Oak_Mask",false,(u,v)=>
                new Color(0,1,.5f,.40f+.12f*Mathf.PerlinNoise(12+u*48,31+v*2)),true);
            var plaster=Save(folder,"PlasterMicro_Normal",true,(u,v)=>
                new Color(.5f+.025f*Mathf.Sin(u*Mathf.PI*110)*Mathf.Sin(v*Mathf.PI*78),
                          .5f+.025f*Mathf.Cos(u*Mathf.PI*84)*Mathf.Sin(v*Mathf.PI*98),1));
            var woodNormal=Save(folder,"OakPores_Normal",true,(u,v)=>
            {
                float slope=.08f*Mathf.Sin(u*Mathf.PI*150+.15f*Mathf.Sin(v*Mathf.PI*4));
                return new Color(.5f+slope,.5f+.006f*Mathf.Sin(v*Mathf.PI*32),1,1);
            });
            var weave=Save(folder,"WovenCotton_Normal",true,(u,v)=>
                new Color(.5f+.035f*Mathf.Sin(u*Mathf.PI*128),.5f+.035f*Mathf.Sin(v*Mathf.PI*128),1,1));
            var clouds=Save(folder,"CloudQuilt_original",false,(u,v)=>
            {
                float d=100;
                foreach(var circle in new[]{new Vector3(.34f,.46f,.13f),new Vector3(.47f,.55f,.16f),new Vector3(.62f,.49f,.12f)})
                    d=Mathf.Min(d,Vector2.Distance(new Vector2(u,v),new Vector2(circle.x,circle.y))-circle.z);
                float white=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.002f,.004f,d));
                float thread=.98f+.02f*Mathf.Sin(u*Mathf.PI*128)*Mathf.Sin(v*Mathf.PI*128);
                return Color.Lerp(new Color(.36f,.46f,.62f),new Color(.85f,.84f,.78f),white)*thread;
            });
            for(int i=0;i<2;i++)
            {
                materials[i].SetTexture("_BaseColorMap",oak);materials[i].SetTexture("_NormalMap",woodNormal);
                materials[i].SetTexture("_MaskMap",oakMask);materials[i].SetFloat("_NormalScale",.22f);
                materials[i].SetFloat("_SmoothnessRemapMin",i==1?.30f:.18f);materials[i].SetFloat("_SmoothnessRemapMax",i==1?.70f:.50f);
                materials[i].SetFloat("_Smoothness",i==1?.52f:.32f);
            }
            materials[2].SetTexture("_NormalMap",plaster);materials[2].SetFloat("_NormalScale",.12f);
            for(int i=4;i<=7;i++)
            {if(i==6)continue;materials[i].SetTexture("_NormalMap",weave);materials[i].SetFloat("_NormalScale",.22f);materials[i].SetFloat("_Smoothness",.02f);}
            materials[4].SetTexture("_BaseColorMap",clouds);materials[4].SetColor("_BaseColor",Color.white);materials[4].SetTextureScale("_BaseColorMap",new Vector2(2,3));
            materials[13].SetFloat("_Smoothness",.12f);
            materials[12].SetFloat("_DoubleSidedEnable",1);materials[12].SetFloat("_DoubleSidedNormalMode",0);
            for(int i=17;i<=18;i++)
            {materials[i].SetTexture("_NormalMap",woodNormal);materials[i].SetFloat("_NormalScale",.10f);materials[i].SetFloat("_Smoothness",.12f);}
            for(int i=19;i<=20;i++)
            {materials[i].SetTexture("_NormalMap",weave);materials[i].SetFloat("_NormalScale",.18f);materials[i].SetFloat("_Smoothness",.02f);}
            foreach(var material in materials){HDMaterial.ValidateMaterial(material);EditorUtility.SetDirty(material);}
        }
    }
}
