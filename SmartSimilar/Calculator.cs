using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Cloo;
using JetBrains.Annotations;
using NLog;

namespace SmartSimilar
{
    /// <summary>
    /// Основной алгоритм Лада/Рено/Ниссан
    /// </summary>
    internal class Calculator : ComputeProgram
    {
        /// <summary>
        /// Логгер для выполнения операций
        /// </summary>
        [CanBeNull] private Logger _log;

        /// <summary>
        /// Создает новый экземпляр <see cref="Calculator"/>
        /// </summary>
        /// <param name="context"></param>
        public Calculator(ComputeContext context) : base (context, SourceCode)
        {
            _log = LogManager.GetCurrentClassLogger();
        }
        
        /// <summary>
        /// Подготовить программу к запуску
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private (ComputeKernel, ComputeCommandQueue) Prepare(string name, ComputeDevice device)
        {
            try
            {
                Build(new[] {device}, null, null, IntPtr.Zero);
                var kernel = CreateKernel(name);

                var queue = new ComputeCommandQueue(Context, device, ComputeCommandQueueFlags.None);

                return (kernel, queue);
            }
            catch (ComputeException)
            {
                var log = GetBuildLog(device);
                _log?.Error($"Compile error: '{log}'");
                throw;
            }
        }

        /// <summary>
        /// Выполнить алгоритм поиска похожих на устройстве OpenCL
        /// </summary>
        /// <param name="device">Устройство, на котором будет выполняться программа</param>
        /// <param name="input">Входные данные</param>
        /// <param name="isMedical">Признак медицинских оправ</param>
        /// <param name="similarsCount">Количество похожих оправ для поиска</param>
        /// <remarks>Данная функция обязательно должна быть потокобезопасной!!!</remarks>
        public async Task<int[]> Execute(ComputeDevice device, List<List<int>> input, bool isMedical, int similarsCount)
        {
            long[] localCycles = {1};

            // Компиляция программы
            var (kernel, queue) = Prepare(isMedical ? "calc_the_sames_med" : "calc_the_sames", device);

            // Копирование постоянных параметров
            int vecsize = isMedical ? 6 : 5;
            int[] inputBytes = new int[input.Count * vecsize];
            int[] result = new int[input.Count * similarsCount];

            for (int i = 0; i < input.Count; i++)
            for (int j = 0; j < (isMedical ? 6 : 5); j++)
                inputBytes[i * vecsize + j] = input[i][j];

            var param0 = new ComputeBuffer<int>(Context, ComputeMemoryFlags.CopyHostPointer | ComputeMemoryFlags.ReadOnly, inputBytes);

            // Память под входные и выходные параметры
            var paramOut = new ComputeBuffer<int>(Context, ComputeMemoryFlags.WriteOnly, result.Length);

            // Задаем аргументы
            kernel.SetMemoryArgument(0, param0);
            kernel.SetMemoryArgument(1, paramOut);
            kernel.SetValueArgument(2, input.Count);
            kernel.SetValueArgument(3, similarsCount);

            // Разрешаем доступ к записи в managed-область из unmanaged кода для входного и выходного параметров
            // И получаем небезопасные указатели
            var param0Handle = GCHandle.Alloc(inputBytes, GCHandleType.Pinned);
            var param0Ptr = Marshal.UnsafeAddrOfPinnedArrayElement((Array) inputBytes, 0);
            var paramOutHandle = GCHandle.Alloc(result, GCHandleType.Pinned);
            var paramOutPtr = Marshal.UnsafeAddrOfPinnedArrayElement((Array) result, 0);

            queue.Write(param0, true, 0, 1, param0Ptr, null);
            queue.Write(paramOut, true, 0, result.Length, paramOutPtr, null);

            var events = new ComputeEventList();

            try
            {
                int stepCount = (int) Math.Ceiling(input.Count / 100d);
                for (int i = 0; i < stepCount; i++)
                {
                    kernel.SetValueArgument(4, i * 100);

                    var cntByCycle = new long[] {Math.Min(input.Count - i * 100, 100)};

                    queue.Execute(kernel, null, cntByCycle, localCycles, events);

                    await Task.Run(() => events.Wait());
                }

                queue.Read(paramOut, true, 0, result.Length, paramOutPtr, null);

                return result;
            }
            finally
            {
                paramOut.Dispose();
                param0.Dispose();
                kernel.Dispose();
                queue.Dispose();
                param0Handle.Free();
                paramOutHandle.Free();
            }
        }

        #region SourceCode
        /// <summary>
        /// Исходный код программы
        /// </summary>
        private const string SourceCode = @"
// Таблица подобия по полу
int constant sexes[4] = {1200, 1201, 1202, 1219};
int constant sexes_med[4] = {1208, 1209, 1210, 1220};
int constant sex_s[4][4] = {{10, 0, 9, 0},
                            { 0,10, 9, 0},
                            { 9, 9,10, 0},
                            { 0, 0, 0,10}};

// Таблица подобия по форме
int constant shapes[6] = {1229, 1233, 1231, 1230, 1232, 1234};
int constant shapes_med[6] = {1261, 1265, 1263, 1262, 1264, 1266};
int constant shape_s[6][6] = {{10, 5, 1, 0, 3, 3},
                              { 5,10, 9, 3, 0, 0},
                              { 1, 9,10, 4, 6, 1},
                              { 0, 3, 4,10, 1, 7},
                              { 3, 0, 6, 1,10, 8},
                              { 3, 0, 1, 7, 8,10}};

// Таблица подобия по цвету
int constant colors[16] = {1250, 1248, 1249, 1237, 1242, 1236, 1241, 1240, 1247, 1246, 1239, 1244, 1243, 1245, 1253, 1254};
int constant colors_med[16] = {1267, 1268, 1269, 1270, 1271, 1272, 1273, 1274, 1275, 1276, 1277, 1278, 1279, 1280, 1281, 1282};
int constant color_s[16][16] = {{10, 9, 7, 0, 2, 0, 0, 0, 6, 0, 0, 0, 0, 0, 2, 0},
                                { 9,10, 8, 0, 0, 0, 0, 5, 1, 0, 0, 0, 0, 0, 1, 0},
                                { 7, 8,10, 0, 0, 0, 0, 0, 0, 0, 0, 0, 5, 2, 0, 0},
                                { 0, 0, 0,10, 9, 0, 4, 0, 0, 3, 1, 0, 1, 0, 3, 0},
                                { 2, 0, 0, 9,10, 0, 4, 0, 0, 0, 1, 3, 3, 0, 7, 0},
                                { 0, 0, 0, 0, 0,10, 0, 0, 3, 2, 0, 0, 0, 2, 0, 6},
                                { 0, 0, 0, 4, 4, 0,10, 0, 0, 1, 6, 2, 0, 0, 0, 0},
                                { 0, 5, 0, 0, 0, 0, 0,10, 0, 0, 0, 0, 0, 0, 0, 0},
                                { 6, 1, 0, 0, 0, 3, 0, 0,10, 9, 0, 0, 0, 0, 0, 2},
                                { 0, 0, 0, 3, 0, 2, 1, 0, 9,10, 0, 0, 0, 0, 0, 0},
                                { 0, 0, 0, 1, 1, 0, 6, 0, 0, 0,10, 8, 3, 2, 1, 0},
                                { 0, 0, 0, 0, 3, 0, 2, 0, 0, 0, 8,10, 7, 1, 2, 0},
                                { 0, 0, 5, 1, 3, 0, 0, 0, 0, 0, 3, 7,10, 6, 1, 0},
                                { 0, 0, 2, 0, 0, 2, 0, 0, 0, 0, 2, 1, 6,10, 0, 0},
                                { 2, 1, 0, 3, 7, 0, 0, 0, 0, 0, 1, 2, 1, 0,10, 0},
                                { 0, 0, 0, 0, 0, 6, 0, 0, 2, 0, 0, 0, 0, 0, 0,10}};

// Таблица подобия по материалу
int constant materials[3] = {414, 415, 1124};
int constant materials_med[3] = {1203, 1204, 1212};
int constant material_s[3][3] = {{10, 3, 7},
                                 { 3,10, 7},
                                 { 7, 7,10}};

// Таблица подобия по типу оправы
int constant rim_glasses_med[3] = {1258, 1259, 1260};
int constant rim_glass_s[3][3] = {{10, 5, 0},
                                  { 5,10, 5},
                                  { 0, 5,10}};

// Опредедение мощностей по каждому из параметров сравнения
int constant sex_power = 1000;
int constant mat_power = 50;
int constant shape_power = 500;
int constant color_power = 100;
int constant rim_glass_power = 200;
int constant shape_power_med = 700;
int constant color_power_med = 400;

/*
 * Функция определения индекса элемента в массиве
 */
int indexOf(__constant int* arr, int len, int search) {
    for (int i = 0; i < len; i++)
        if (arr[i] == search)
            return i;

    return -1;
}

/*
 * Функция поиска похожих очков (солнцезащитные) для магазина https://smartvision-optica.ru/
 */
__kernel void calc_the_sames (
        __global int *in,
        __global int *out,
        int t_cnt,
        int s_cnt,
        int start_n) {

    int p_cnt = 5; // Количетво столбцов
    size_t tid = get_global_id(0);
    int pid = (start_n + tid) * p_cnt;
    int oid = (start_n + tid) * s_cnt;

    // Смещения: 0 - пол, 1 - материал, 2 - форма, 3 - цвет
    int sex = indexOf(sexes, 4, in[pid + 0]);
    int mat = indexOf(materials, 3, in[ pid + 1]);
    int shp = indexOf(shapes, 6, in[pid + 2]);
    int clr = indexOf(colors, 16, in[pid + 3]);

    // Обнуляем кэш и выходной буфер
    __local long si[4096];
    for (int i = 0; i < 4096; i++) si[i] = 0;
    for (int i = 0; i < s_cnt; i++) out[oid + i] = 0;

    for (int i = 0; i < t_cnt; i++) {
        if (i == start_n + tid) continue; // Не будем добавлять в похожие себя

        long cur = sex_s[sex][indexOf(sexes, 4, in[i * p_cnt + 0])] * sex_power;
        cur += material_s[mat][indexOf(materials, 3, in[i * p_cnt + 1])] * mat_power;
        cur += shape_s[shp][indexOf(shapes, 6, in[i * p_cnt + 2])] * shape_power;
        cur += color_s[clr][indexOf(colors, 16, in[i * p_cnt + 3])] * color_power;

        for (int j = 0; j < s_cnt; j++) {
            if (cur > si[j]) {
                si[j] = cur;
                out[oid + j] = in[i * p_cnt + 4];
                break;
            }
        }
    }
}

/*
 * Функция поиска похожих очков (медицинские оправы) для магазина https://smartvision-optica.ru/
 */
__kernel void calc_the_sames_med (
        __global int *in,
        __global int *out,
        int t_cnt,
        int s_cnt,
        int start_n) {

    int p_cnt = 6; // Количетво столбцов
    size_t tid = get_global_id(0);
    int pid = (start_n + tid) * p_cnt;
    int oid = (start_n + tid) * s_cnt;

    // Смещения: 0 - пол, 1 - материал, 2 - форма, 3 - цвет
    int sex = indexOf(sexes_med, 4, in[pid + 0]);
    int mat = indexOf(materials_med, 3, in[ pid + 1]);
    int shp = indexOf(shapes_med, 6, in[pid + 2]);
    int clr = indexOf(colors_med, 16, in[pid + 3]);
    int rim = indexOf(rim_glasses_med, 3, in[pid + 4]);

    // Обнуляем кэш и выходной буфер
    __local long si[4096];
    for (int i = 0; i < 4096; i++) si[i] = 0;
    for (int i = 0; i < s_cnt; i++) out[oid + i] = 0;

    for (int i = 0; i < t_cnt; i++) {
        if (i == start_n + tid) continue; // Не будем добавлять в похожие себя

        long cur = sex_s[sex][indexOf(sexes_med, 4, in[i * p_cnt + 0])] * sex_power;
        cur += material_s[mat][indexOf(materials_med, 3, in[i * p_cnt + 1])] * mat_power;
        cur += shape_s[shp][indexOf(shapes_med, 6, in[i * p_cnt + 2])] * shape_power_med;
        cur += color_s[clr][indexOf(colors_med, 16, in[i * p_cnt + 3])] * color_power_med;
        cur += rim_glass_s[rim][indexOf(rim_glasses_med, 3, in[i * p_cnt + 4])] * rim_glass_power;

        for (int j = 0; j < s_cnt; j++) {
            if (cur > si[j]) {
                si[j] = cur;
                out[oid + j] = in[i * p_cnt + 5];
                break;
            }
        }
    }
}
        ";
        #endregion

        /// <summary>
        /// Releases the associated OpenCL object.
        /// </summary>
        /// <param name="manual"> Specifies the operation mode of this method. </param>
        /// <remarks> <paramref name="manual"/> must be <c>true</c> if this method is invoked directly by the application. </remarks>
        protected override void Dispose(bool manual)
        {
        }
    }
}