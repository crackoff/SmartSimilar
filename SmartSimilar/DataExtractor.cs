using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmartSimilar
{
    static class DataExtractor
    {
        public static List<List<int>> GetGlassesData(string srcData, out bool isMedical, out List<string> errors)
        {
            List<List<int>> ret = new List<List<int>>();

            var lines = srcData.Split('\n');
            errors = new List<string>();
            bool isData = false;
            isMedical = false;

            foreach (var line in lines)
            {
                if (isData)
                {
                    var values = line.Split('\t');
                    int pCnt = isMedical ? 6 : 5;
                    if (values.Length != pCnt)
                    {
                        continue;
                    }

                    List<int> row = new List<int>();
                    for (int i = 0; i < pCnt; i++)
                    {
                        row.Add(int.Parse(values[i]));
                    }

                    bool valid = isMedical ? ValidateMedicalRow(row) : ValidateSunglassRow(row);
                    if (!valid)
                    {
                        var err = new StringBuilder("Unprocessed row: ");
                        for (int i = 0; i < row.Count; i++)
                        {
                            err.Append(row[i]);
                            err.Append(", ");
                        }

                        errors.Add(err.ToString());
                        continue;
                    }

                    ret.Add(row);
                }

                // Признак медицинских оправ
                if (line.Contains("O_TYPE"))
                    isMedical = true;

                // Признак начала данных - строка "iblock_element_id"
                if (line.Contains("iblock_element_id"))
                    isData = true;
            }

            return ret;
        }

        static bool ValidateSunglassRow(List<int> row)
        {
            int[] sexes = {1200, 1201, 1202, 1219};
            int[] materials = {414, 415, 1124};
            int[] shapes = {1229, 1233, 1231, 1230, 1232, 1234};
            int[] colors =
            {
                1250, 1248, 1249, 1237, 1242, 1236, 1241, 1240,
                1247, 1246, 1239, 1244, 1243, 1245, 1253, 1254
            };

            if (!sexes.Contains(row[0])) return false;
            if (!materials.Contains(row[1])) return false;
            if (!shapes.Contains(row[2])) return false;
            if (!colors.Contains(row[3])) return false;

            return true;
        }

        static bool ValidateMedicalRow(List<int> row)
        {
            int[] sexes = {1208, 1209, 1210, 1220};
            int[] materials = {1203, 1204, 1212};
            int[] shapes = {1261, 1265, 1263, 1262, 1264, 1266};
            int[] colors =
            {
                1267, 1268, 1269, 1270, 1271, 1272, 1273, 1274,
                1275, 1276, 1277, 1278, 1279, 1280, 1281, 1282
            };
            int[] rimGlasses = {1258, 1259, 1260};

            if (!sexes.Contains(row[0])) return false;
            if (!materials.Contains(row[1])) return false;
            if (!shapes.Contains(row[2])) return false;
            if (!colors.Contains(row[3])) return false;
            if (!rimGlasses.Contains(row[4])) return false;

            return true;
        }
    }
}
