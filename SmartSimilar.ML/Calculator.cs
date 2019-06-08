using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.ML;

namespace SmartSimilar.ML
{
    public static class Calculator
    {
        public static IEnumerable<Eyeglasses> Calculate(string sourceData, int numberOfSimilar)
        {
            if (numberOfSimilar > 8 || numberOfSimilar < 1)
                throw new ArgumentOutOfRangeException(nameof(numberOfSimilar), "Количество похожих: от 1 до 8");

            // Загрузка данных
            var eyeglassesType = DataExtractor.ParseEyeglassesType(sourceData);
            var parsedData = DataExtractor.ParseEyeglassesData(sourceData, eyeglassesType).ToArray();
            var mlContext = new MLContext(1);
            var data = mlContext.Data.LoadFromEnumerable(parsedData);

            // Трансформация
            var dataProcessPipeline = mlContext.Transforms.Categorical
                .OneHotEncoding(nameof(Eyeglasses.Sex), nameof(Eyeglasses.Sex))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(nameof(Eyeglasses.Shape)))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(nameof(Eyeglasses.Material)))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(nameof(Eyeglasses.Color)))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(nameof(Eyeglasses.RimGlasses)))
                .Append(mlContext.Transforms.Concatenate("Features", nameof(Eyeglasses.Sex), nameof(Eyeglasses.Shape), nameof(Eyeglasses.Material), nameof(Eyeglasses.Color), nameof(Eyeglasses.RimGlasses)));

            DataOperationsCatalog.TrainTestData trainTestData = mlContext.Data.TrainTestSplit(data);

            // Тренировка
            int numberOfClusters = parsedData.Length / (numberOfSimilar * 6);
            var trainer = mlContext.Clustering.Trainers.KMeans(featureColumnName: "Features", numberOfClusters: numberOfClusters);
            var trainingPipeline = dataProcessPipeline.Append(trainer);
            var trainedModel = trainingPipeline.Fit(trainTestData.TrainSet);

            // Вычисления
            var predictionEngine = mlContext.Model.CreatePredictionEngine<Eyeglasses, Prediction>(trainedModel);

            var clusters = new Dictionary<uint, List<Eyeglasses>>();
            foreach (var eyeglasses in parsedData)
            {
                var result = predictionEngine.Predict(eyeglasses);

                if (!clusters.ContainsKey(result.SelectedClusterId))
                    clusters[result.SelectedClusterId] = new List<Eyeglasses>();

                eyeglasses.Cluster = result.SelectedClusterId;
                clusters[result.SelectedClusterId].Add(eyeglasses);
            }

            // Построение результирующего набора
            foreach (var eyeglasses in parsedData)
            {
                eyeglasses.SimilarEyeglasses = clusters[eyeglasses.Cluster].PickRandomSimilar(eyeglasses, numberOfSimilar).ToArray();
                yield return eyeglasses;
            }
        }

        private static readonly Random Random = new Random();

        /// <summary>
        /// Выбрать случайные элементы массива с учетом бренда 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<Eyeglasses> PickRandomSimilar(this List<Eyeglasses> values, Eyeglasses eyeglasses, int numberValues)
        {
            if (numberValues >= values.Count - 1)
                numberValues = values.Count - 2;

            var returnedValues = 0;
            var indexes = Enumerable.Range(0, values.Count).ToArray();
            var currentBrand = new List<Eyeglasses>();

            for (var i = 0; i < values.Count - 1 && returnedValues < numberValues; i++)
            {
                var j = Random.Next(i, values.Count);

                var temp = indexes[i];
                indexes[i] = indexes[j];
                indexes[j] = temp;

                if (values[indexes[i]].Id == eyeglasses.Id)
                {
                    // The same item - do not return
                    continue;
                }

                if (values[indexes[i]].Brand == eyeglasses.Brand)
                {
                    // The same brand - return with low priority
                    currentBrand.Add(values[indexes[i]]);
                    continue;
                }

                returnedValues++;
                yield return values[indexes[i]];
            }

            for (var i = 0; returnedValues++ < numberValues; i++)
            {
                yield return currentBrand[i];
            }
        }
    }
}
