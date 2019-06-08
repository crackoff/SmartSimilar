using System.Diagnostics;
using JetBrains.Annotations;
using Microsoft.ML.Data;

namespace SmartSimilar.ML
{
    public enum EyeglassesType : byte
    {
        Sun,
        Medical,
    }

    public enum SunSex : short
    {
        [UsedImplicitly] Man = 1200,
        [UsedImplicitly] Woman = 1201,
        [UsedImplicitly] Unisex = 1202,
        [UsedImplicitly] Child = 1219,
    }

    public enum SunMaterial : short
    {
        [UsedImplicitly] Metal = 414,
        [UsedImplicitly] Plastic = 415,
        [UsedImplicitly] Combined = 1124,
    }

    public enum SunShape : short
    {
        [UsedImplicitly] Aviator = 1229,
        [UsedImplicitly] Round = 1233,
        [UsedImplicitly] Oval = 1231,
        [UsedImplicitly] Butterfly = 1230,
        [UsedImplicitly] Rectangle = 1232,
        [UsedImplicitly] Trapeze = 1234,
        [UsedImplicitly] Sport = 1326,
    }

    public enum SunColor : short
    {
        [UsedImplicitly] Black = 1250, 
        [UsedImplicitly] Blue = 1248, 
        [UsedImplicitly] Violet = 1249, 
        [UsedImplicitly] Bronze = 1237, 
        [UsedImplicitly] Brown = 1242, 
        [UsedImplicitly] White = 1236,
        [UsedImplicitly] Gold = 1241,
        [UsedImplicitly] Green = 1240,
        [UsedImplicitly] Gray = 1247, 
        [UsedImplicitly] Silver = 1246, 
        [UsedImplicitly] Yellow = 1239, 
        [UsedImplicitly] Orange = 1244, 
        [UsedImplicitly] Red = 1243, 
        [UsedImplicitly] Rose = 1245,
        [UsedImplicitly] Spotted = 1253, 
        [UsedImplicitly] Transparent = 1254,
    }

    public enum MedicalSex : short
    {
        [UsedImplicitly] Man = 1208,
        [UsedImplicitly] Woman = 1209,
        [UsedImplicitly] Unisex = 1210,
        [UsedImplicitly] Child = 1220,
    }

    public enum MedicalMaterial : short
    {
        [UsedImplicitly] Metal = 1203,
        [UsedImplicitly] Plastic = 1204,
        [UsedImplicitly] Combined = 1212,
    }

    public enum MedicalShape : short
    {
        [UsedImplicitly] Aviator = 1261,
        [UsedImplicitly] Round = 1265,
        [UsedImplicitly] Oval = 1263,
        [UsedImplicitly] Butterfly = 1262,
        [UsedImplicitly] Rectangle = 1264,
        [UsedImplicitly] Trapeze = 1266,
    }

    public enum MedicalColor : short
    {
        [UsedImplicitly] Black = 1267,
        [UsedImplicitly] Blue = 1268,
        [UsedImplicitly] Violet = 1269,
        [UsedImplicitly] Bronze = 1270,
        [UsedImplicitly] Brown = 1271,
        [UsedImplicitly] White = 1271,
        [UsedImplicitly] Gold = 1273,
        [UsedImplicitly] Green = 1274,
        [UsedImplicitly] Gray = 1275,
        [UsedImplicitly] Silver = 1276,
        [UsedImplicitly] Yellow = 1277,
        [UsedImplicitly] Orange = 1278,
        [UsedImplicitly] Red = 1279,
        [UsedImplicitly] Rose = 1280,
        [UsedImplicitly] Spotted = 1281,
        [UsedImplicitly] Transparent = 1282,
    }

    public enum MedicalRimGlasses : short
    {
        [UsedImplicitly] Rim = 1258,
        [UsedImplicitly] SemiRim = 1259,
        [UsedImplicitly] NoRim = 1260,
    }

    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public abstract class Eyeglasses
    {
        public int Id;
        public string Name;
        public abstract string Sex { get; }
        public abstract string Shape { get; }
        public abstract string Material { get; }
        public abstract string Color { get; }
        public abstract string RimGlasses { get; }
        public abstract string Brand { get; }

        [NoColumn] public uint Cluster;

        [NoColumn] public Eyeglasses[] SimilarEyeglasses;

        [NoColumn] public string DebuggerDisplay => $"{Name} ({Sex}, {Shape}, {Color}, {Material})";
    }

    public class SunEyeglasses : Eyeglasses // Cannot use structures here
    {
        [NoColumn] public SunSex SunSex;
        [NoColumn] public SunShape SunShape;
        [NoColumn] public SunMaterial SunMaterial;
        [NoColumn] public SunColor SunColor;
        [NoColumn] public string SunBrand;

        public override string Sex => SunSex.ToString();
        public override string Shape => SunShape.ToString();
        public override string Material => SunMaterial.ToString();
        public override string Color => SunColor.ToString();
        public override string RimGlasses => "N/A";
        public override string Brand => SunBrand;
    }

    public class MedicalEyeglasses : Eyeglasses
    {
        [NoColumn] public MedicalSex MedicalSex;
        [NoColumn] public MedicalShape MedicalShape;
        [NoColumn] public MedicalMaterial MedicalMaterial;
        [NoColumn] public MedicalColor MedicalColor;
        [NoColumn] public MedicalRimGlasses MedicalRimGlasses;
        [NoColumn] public string MedicalBrand;

        public override string Sex => MedicalSex.ToString();
        public override string Shape => MedicalShape.ToString();
        public override string Material => MedicalMaterial.ToString();
        public override string Color => MedicalColor.ToString();
        public override string RimGlasses => MedicalRimGlasses.ToString();
        public override string Brand => MedicalBrand;
    }

    public class Prediction
    {
        [ColumnName("PredictedLabel")] public uint SelectedClusterId;

        [ColumnName("Score")] public float[] Distance;
    }
}
