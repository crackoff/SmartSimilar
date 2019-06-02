using System;
using System.Collections.Generic;
using System.Linq;
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
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(nameof(Eyeglasses.Shape), nameof(Eyeglasses.Shape)))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(nameof(Eyeglasses.Material), nameof(Eyeglasses.Material)))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(nameof(Eyeglasses.Color), nameof(Eyeglasses.Color)))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(nameof(Eyeglasses.RimGlasses), nameof(Eyeglasses.RimGlasses)))
                .Append(mlContext.Transforms.Concatenate("Features", nameof(Eyeglasses.Sex), nameof(Eyeglasses.Shape), nameof(Eyeglasses.Material), nameof(Eyeglasses.Color), nameof(Eyeglasses.RimGlasses)));

            DataOperationsCatalog.TrainTestData trainTestData = mlContext.Data.TrainTestSplit(data, testFraction: 0.1);

            // Тренировка
            int numberOfClusters = parsedData.Length / (numberOfSimilar * 5);
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
                eyeglasses.SimilarEyeglasses = clusters[eyeglasses.Cluster].PickRandom(numberOfSimilar).ToArray();
                yield return eyeglasses;
            }
        }

        /// <summary>
        /// Выбрать случайные элементы массива
        /// </summary>
        public static IEnumerable<T> PickRandom<T>(this List<T> values, int numberValues) // TODO: exclude themselves 
        {
            var random = new Random();

            if (numberValues >= values.Count)
                numberValues = values.Count - 1;

            var indexes = Enumerable.Range(0, values.Count).ToArray();

            for (var i = 0; i < numberValues; i++)
            {
                var j = random.Next(i, values.Count);

                var temp = indexes[i];
                indexes[i] = indexes[j];
                indexes[j] = temp;

                yield return values[indexes[i]];
            }
        }
    }
}
