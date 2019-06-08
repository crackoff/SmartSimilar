using System.Collections.Generic;

namespace SmartSimilar.ML
{
    public static class DataExtractor
    {
        public static IEnumerable<Eyeglasses> ParseEyeglassesData(string srcData, EyeglassesType eyeglassesType)
        {
            var lines = srcData.Split('\n');
            bool isData = false;

            foreach (var line in lines)
            {
                if (isData)
                {
                    var values = line.Split('\t');
                    Eyeglasses eyeglasses;

                    switch (eyeglassesType)
                    {
                        case EyeglassesType.Sun:
                        {
                            if (values.Length != 7)
                            {
                                continue;
                            }

                            eyeglasses = new SunEyeglasses
                            {
                                SunSex = (SunSex) short.Parse(values[0]),
                                SunMaterial = (SunMaterial) short.Parse(values[1]),
                                SunShape = (SunShape) short.Parse(values[2]),
                                SunColor = (SunColor) short.Parse(values[3]),
                                Id = int.Parse(values[4]),
                                Name = values[5],
                                SunBrand = values[6],
                            };
                            break;
                        }

                        case EyeglassesType.Medical:
                        {
                            if (values.Length != 8)
                            {
                                continue;
                            }

                            eyeglasses = new MedicalEyeglasses
                            {
                                MedicalSex = (MedicalSex)short.Parse(values[0]),
                                MedicalMaterial = (MedicalMaterial)short.Parse(values[1]),
                                MedicalShape = (MedicalShape)short.Parse(values[2]),
                                MedicalColor = (MedicalColor)short.Parse(values[3]),
                                MedicalRimGlasses = (MedicalRimGlasses)short.Parse(values[4]),
                                Id = int.Parse(values[5]),
                                Name = values[6],
                                MedicalBrand = values[7],
                            };
                            break;
                        }

                        default:
                            continue;
                    }

                    yield return eyeglasses;
                }

                // Признак начала данных - строка "iblock_element_id"
                if (line.Contains("iblock_element_id"))
                {
                    isData = true;
                }
            }
        }

        public static EyeglassesType ParseEyeglassesType(string srcData)
        {
            if (srcData.Contains("O_TYPE"))
            {
                return EyeglassesType.Medical;
            }

            return EyeglassesType.Sun;
        }
    }
}
