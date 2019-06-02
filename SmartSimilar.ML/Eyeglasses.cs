using System.Diagnostics;
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
        Man = 1200,
        Woman = 1201,
        Unisex = 1202,
        Child = 1219,
    }

    public enum SunMaterial : short
    {
        Metal = 414,
        Plastic = 415,
        Combined = 1124,
    }

    public enum SunShape : short
    {
        Aviator = 1229, 
        Round = 1233, 
        Oval = 1231, 
        Butterfly = 1230, 
        Rectangle = 1232, 
        Trapeze = 1234, 
        Sport = 1326,
    }

    public enum SunColor : short
    {
        Black = 1250, 
        Blue = 1248, 
        Violet = 1249, 
        Bronze = 1237, 
        Brown = 1242, 
        White = 1236,
        Gold = 1241,
        Green = 1240,
        Gray = 1247, 
        Silver = 1246, 
        Yellow = 1239, 
        Orange = 1244, 
        Red = 1243, 
        Rose = 1245, 
        Spotted = 1253, 
        Transparent = 1254,
    }

    public enum MedicalSex : short
    {
        Man = 1208,
        Woman = 1209,
        Unisex = 1210,
        Child = 1220,
    }

    public enum MedicalMaterial : short
    {
        Metal = 1203,
        Plastic = 1204,
        Combined = 1212,
    }

    public enum MedicalShape : short
    {
        Aviator = 1261,
        Round = 1265,
        Oval = 1263,
        Butterfly = 1262,
        Rectangle = 1264,
        Trapeze = 1266,
    }

    public enum MedicalColor : short
    {
        Black = 1267,
        Blue = 1268,
        Violet = 1269,
        Bronze = 1270,
        Brown = 1271,
        White = 1271,
        Gold = 1273,
        Green = 1274,
        Gray = 1275,
        Silver = 1276,
        Yellow = 1277,
        Orange = 1278,
        Red = 1279,
        Rose = 1280,
        Spotted = 1281,
        Transparent = 1282,
    }

    public enum MedicalRimGlasses : short
    {
        Rim = 1258, 
        SemiRim = 1259, 
        NoRim = 1260,
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

        [NoColumn]
        public uint Cluster;

        [NoColumn]
        public Eyeglasses[] SimilarEyeglasses;

        [NoColumn]
        public string DebuggerDisplay => $"{Name} ({Sex}, {Shape}, {Color}, {Material})";
    }

    public class SunEyeglasses : Eyeglasses // Cannot use structures here
    {
        [NoColumn]
        public SunSex SunSex;
        [NoColumn]
        public SunShape SunShape;
        [NoColumn]
        public SunMaterial SunMaterial;
        [NoColumn]
        public SunColor SunColor;

        public override string Sex => SunSex.ToString();
        public override string Shape => SunShape.ToString();
        public override string Material => SunMaterial.ToString();
        public override string Color => SunColor.ToString();
        public override string RimGlasses => "N/A";
    }

    public class MedicalEyeglasses : Eyeglasses
    {
        [NoColumn]
        public MedicalSex MedicalSex;
        [NoColumn]
        public MedicalShape MedicalShape;
        [NoColumn]
        public MedicalMaterial MedicalMaterial;
        [NoColumn]
        public MedicalColor MedicalColor;
        [NoColumn]
        public MedicalRimGlasses MedicalRimGlasses;

        public override string Sex => MedicalSex.ToString();
        public override string Shape => MedicalShape.ToString();
        public override string Material => MedicalMaterial.ToString();
        public override string Color => MedicalColor.ToString();
        public override string RimGlasses => MedicalRimGlasses.ToString();
    }

    public class Prediction
    {
        [ColumnName("PredictedLabel")]
        public uint SelectedClusterId;

        [ColumnName("Score")]
        public float[] Distance;
    }
}
