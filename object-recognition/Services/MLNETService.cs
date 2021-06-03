using Microsoft.ML;
using Microsoft.ML.Data;
using object_core.Constants;
using object_recognition.Entities;
using object_recognition.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace object_recognition.Services
{
    public class MLNETService : IMLNETService
    {
        static readonly string _assetsPath = Path.Combine(AppConstant.AssetsPath, "assets");
        static readonly string _imagesFolder = Path.Combine(_assetsPath, "images");

        //static readonly string _predictSingleImage = Path.Combine(_imagesFolder, "kot.jpg");
        static readonly string _inceptionTensorFlowModel = Path.Combine(_assetsPath, "inception", "tensorflow_inception_graph.pb");
        private PredictionEngine<ImageInputData, ImagePrediction> _predictionEngine;
        private ITransformer _model;
        private MLContext _mlContext;
        private string[] _labels;
        public void Execute()
        {
            _mlContext = new MLContext();
            _model = GenerateModel();
        }

        private struct InceptionSettings
        {
            public const int ImageHeight = 224;
            public const int ImageWidth = 224;
            public const float Mean = 117;
            public const float Scale = 1;
            public const bool ChannelsLast = true;
        }

        private ITransformer GenerateModel()
        {
            ITransformer model;
            DataViewSchema modelSchema;
            if (File.Exists(AppConstant.TrainModelPath))
            {
                model = _mlContext.Model.Load(AppConstant.TrainModelPath, out modelSchema);
            }
            else
            {
                var pipeline = _mlContext.Transforms.ResizeImages(outputColumnName: "input",
                                                                      imageWidth: InceptionSettings.ImageWidth,
                                                                      imageHeight: InceptionSettings.ImageHeight,
                                                                      inputColumnName: nameof(ImageInputData.Image))
                           .Append(_mlContext.Transforms.ExtractPixels(outputColumnName: "input",
                                                                      interleavePixelColors: InceptionSettings.ChannelsLast,
                                                                      offsetImage: InceptionSettings.Mean,
                                                                      inputColumnName: "input"))
                           .Append(_mlContext.Model.LoadTensorFlowModel(_inceptionTensorFlowModel)
                                                               .ScoreTensorFlowModel(
                                                                      outputColumnNames: new[] { "softmax2_pre_activation" },
                                                                      inputColumnNames: new[] { "input" },
                                                                      addBatchDimensionInput: true))
                           .Append(_mlContext.Transforms.Conversion.MapValueToKey(outputColumnName: "LabelKey", inputColumnName: "Label"))
                           .Append(_mlContext.MulticlassClassification.Trainers.LbfgsMaximumEntropy(labelColumnName: "LabelKey", featureColumnName: "softmax2_pre_activation"))
                           .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabelValue", "PredictedLabel"))
                            .AppendCacheCheckpoint(_mlContext);

                var files = Directory.GetFiles(AppConstant.TrainDataPath, "*.*", SearchOption.AllDirectories);
                var listimages = new List<ImageInputData>();
                foreach (var file in files)
                {
                    var lastFolderpath = file.Substring(0, file.LastIndexOf("\\"));
                    var lable = lastFolderpath.Substring(lastFolderpath.LastIndexOf("\\") + 1);
                    var inputData = new ImageInputData()
                    {
                        Image = new Bitmap(file),
                        Label = lable
                    };
                    listimages.Add(inputData);
                };
                var trainingSet = _mlContext.Data.LoadFromEnumerable(listimages);
                model = pipeline.Fit(trainingSet);
                _mlContext.Model.Save(model, trainingSet.Schema, AppConstant.TrainModelPath);
            }
            _predictionEngine = _mlContext.Model.CreatePredictionEngine<ImageInputData, ImagePrediction>(model);
            var labelBuffer = new VBuffer<ReadOnlyMemory<char>>();
            _predictionEngine.OutputSchema["Score"].Annotations.GetValue("SlotNames", ref labelBuffer);
            _labels = labelBuffer.DenseValues().Select(l => l.ToString()).ToArray();
            return model;
        }

        public ImagePrediction ClassifySingleImage(Bitmap image)
        {
            ImageInputData imageInputData = new ImageInputData { Image = image };

            //Predict code for provided image
            ImagePrediction imagePredictions = _predictionEngine.Predict(imageInputData);
            var index = Array.IndexOf(_labels, imagePredictions.PredictedLabelValue);
            imagePredictions.CurrentScore = imagePredictions.Score[index];
            return imagePredictions;
        }
    }
}
