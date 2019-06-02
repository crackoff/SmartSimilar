using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SmartSimilar.ML;

namespace SmartSimilar
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void calculateButton_Click(object sender, EventArgs e)
        {
            IEnumerable<Eyeglasses> result;
            try
            {
                result = Calculator.Calculate(textBox.Text, 5);
            }
            catch (Exception exception)
            {
                textBox.Text = exception.Message;
                return;
            }

            StringBuilder sb = new StringBuilder("DELETE FROM b_iblock_element_prop_m5 WHERE IBLOCK_PROPERTY_ID = 122;");
            sb.AppendLine();
            sb.AppendLine();

            foreach (var eyeglasses in result)
            {
                for (int i = 0; i < eyeglasses.SimilarEyeglasses.Length; i++)
                {
                    sb.AppendFormat(
                        "INSERT b_iblock_element_prop_m5(IBLOCK_ELEMENT_ID,IBLOCK_PROPERTY_ID,VALUE,VALUE_NUM) VALUES ({0}, 122, {1}, {1});",
                        eyeglasses.Id, eyeglasses.SimilarEyeglasses[i].Id);
                    sb.AppendLine();
                }
            }

            sb.AppendLine();
            sb.AppendLine();
            sb.Append(@"
UPDATE b_iblock_element_prop_s5 s5
JOIN (
  SELECT 
    IBLOCK_ELEMENT_ID
  , CONCAT(
      'a:3:{s:5:""VALUE"";a:3:{i:'
      , GROUP_CONCAT(CONCAT(cast(r.row_number - 1 as CHAR), ';s:6:""', CAST(r.value as CHAR)) SEPARATOR '"";i:')
      , '"";}s:11:""DESCRIPTION"";a:3:{i:'
      , GROUP_CONCAT(CONCAT(cast(r.row_number - 1 as CHAR), ';s:0:""') SEPARATOR '"";i:')
      , '"";}s:2:""ID"";a:3:{i:'
      , GROUP_CONCAT(CONCAT(cast(r.row_number - 1 as CHAR), ';s:6:""', CAST(r.ID as CHAR)) SEPARATOR '"";i:')
      , '"";}}'
    ) prop
  FROM 
  (
    SELECT a.IBLOCK_ELEMENT_ID, a.VALUE, a.ID, count(*) as row_number 
    FROM b_iblock_element_prop_m5 a
    JOIN b_iblock_element_prop_m5 b ON a.IBLOCK_ELEMENT_ID = b.IBLOCK_ELEMENT_ID AND a.VALUE >= b.VALUE
    WHERE a.IBLOCK_PROPERTY_ID = 122 AND  b.IBLOCK_PROPERTY_ID = 122
    GROUP BY a.IBLOCK_ELEMENT_ID, a.VALUE, a.ID
  ) r
  GROUP BY IBLOCK_ELEMENT_ID
) f ON s5.iblock_element_id = f.IBLOCK_ELEMENT_ID
SET s5.property_122 = f.prop;
");

            textBox.Text = sb.ToString();
        }
    }
}
