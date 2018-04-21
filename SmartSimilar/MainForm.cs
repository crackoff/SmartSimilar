using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Cloo;
using JetBrains.Annotations;
using NLog;

namespace SmartSimilar
{
    public partial class MainForm : Form
    {
        /// <summary>
        /// Логгер для выполнения операций
        /// </summary>
        [CanBeNull] private Logger _log;

        /// <summary>
        /// Контекст вычислений
        /// </summary>
        private ComputeContext _context;

        /// <summary>
        /// Доступные платформы
        /// </summary>
        private ReadOnlyCollection<ComputePlatform> _platforms;

        /// <summary>
        /// Доступные устройства
        /// </summary>
        private ConcurrentDictionary<int, ComputeDevice> _devices;

        public MainForm()
        {
            InitializeComponent();

            _platforms = ComputePlatform.Platforms;
            _log?.Info($"Доступны платформы: {string.Join(", ", _platforms.Select(x => x.Name))}");
            if (_platforms.Count == 0) _log?.Error("Нет ни одной доступной платформы");
            _context = new ComputeContext(ComputeDeviceTypes.Gpu, new ComputeContextPropertyList(_platforms[0]), null, IntPtr.Zero);

            _devices = new ConcurrentDictionary<int, ComputeDevice>(_platforms[0].Devices
                .Where(x => x.Type == ComputeDeviceTypes.Gpu).ToDictionary(x => (int)x.Handle.Value, x => x));
            _log?.Info($"Доступны устройства: {string.Join(", ", _devices.Values.Select(x => x.Name))}");

            if (_devices.Count == 0)
            {
                _log?.Error("Нет ни одного пригодного для вычислений устройства");
            }
        }

        private async void calculateButton_Click(object sender, EventArgs e)
        {
            var data = DataExtractor.GetGlassesData(textBox.Text, out bool isMedical, out var errors);

            if (errors.Count > 0)
                MessageBox.Show(string.Join(Environment.NewLine, errors));

            int[] result;
            try
            {
                textBox.Text = @"Расчет...";
                var calculator = new Calculator(_context);
                result = await calculator.Execute(_devices.First().Value, data, isMedical, 5);
            }
            catch (Exception exception)
            {
                textBox.Text = exception.ToString();
                return;
            }

            StringBuilder sb = new StringBuilder("DELETE FROM b_iblock_element_prop_m5 WHERE IBLOCK_PROPERTY_ID = 122;");
            sb.AppendLine();
            sb.AppendLine();

            int idIndex = isMedical ? 5 : 4;
            for (int i = 0; i < result.Length; i++)
            {
                sb.AppendFormat(
                    "INSERT b_iblock_element_prop_m5(IBLOCK_ELEMENT_ID,IBLOCK_PROPERTY_ID,VALUE,VALUE_NUM) VALUES ({0}, 122, {1}, {1});",
                    data[i / 5][idIndex], result[i]);
                sb.AppendLine();
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
