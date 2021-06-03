using AForge.Video;
using AForge.Video.DirectShow;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using object_detector.Models;
using object_detector.Services;
using Accord.Math;
using Microsoft.Extensions.DependencyInjection;
using object_recognition.Services.Interfaces;
using object_recognition.Services;
using GleamTech.VideoUltimate;

namespace object_detector
{
    public partial class Form1 : Form
    {
        // B G R A
        private OpenFileDialog _openFileDialog { get; set; }
        private VideoFrameReader _videoFrameReader { get; set; }
        private List<Bitmap> _imagesFromVideo { get; set; }
        private List<byte[]> _prevImages { get; set; }
        private List<byte[]> _prevDetectImages { get; set; }
        private VideoCaptureDevice _captureDevice { get; set; }
        public float[,] _gaussianMask { get; set; }
        public Thread _thread { get; set; }
        public int _contrast { get; set; }
        public int _blurValue { get; set; }
        public int _whitePercent { get; set; }
        public int _blurMask { get; set; }
        public int _sobelMinPorog { get; set; }
        public int _imageWidth { get; set; }
        public int _imageHeight { get; set; }
        public int _imageInputHeight { get; set; }
        public int _imageInputWidth { get; set; }
        public int _imageSizeCoef { get; set; }
        public int _strongPixelFilter { get; set; }
        public int _strongPixelPercent { get; set; }
        public int _predictionsCount { get; set; }
        public int _detectMotionFrameCount { get; set; }
        public int _clusteringCoef { get; set; }
        public bool _isMonochrome { get; set; }
        public bool _isBlur { get; set; }
        public bool _isSobel1 { get; set; }
        public bool _isSobel2 { get; set; }
        public bool _isContrast { get; set; }
        public bool _isNonMaxSuppersession { get; set; }
        public bool _isStrongPixels1 { get; set; }
        public bool _isStrongPixels2 { get; set; }
        public bool _isDisplayPredictions { get; set; }
        public bool _isDetectMotion { get; set; }
        public bool _isClustering { get; set; }
        public bool _isVideo { get; set; }
        public bool _dispalyTrash { get; set; }
        public string _fileName { get; set; }
        CancellationTokenSource _videoFromFile { get; set; }
        public Form2 _settingsForm { get; set; }
        public IMLNETService _mlNetService;


        public Form1()
        {
            var serviceProvider = new ServiceCollection()
                .AddSingleton<IMLNETService, MLNETService>()
                .BuildServiceProvider();
            _mlNetService = serviceProvider.GetService<IMLNETService>();
            _mlNetService.Execute();
            InitializeComponent();
            _imageSizeCoef = 10;
            _contrast = 100;
            _blurValue = 3;
            _blurMask = 3;
            _sobelMinPorog = 80;
            _whitePercent = 5;
            _strongPixelFilter = 6;
            _strongPixelPercent = 23;
            _predictionsCount = 15;
            _detectMotionFrameCount = 3;
            _clusteringCoef = 15;
            checkBox1.Checked = _isMonochrome = false;
            checkBox2.Checked = _isBlur = true;
            checkBox3.Checked = _isSobel1 = true;
            checkBox4.Checked = _isContrast = false;
            checkBox5.Checked = _isNonMaxSuppersession = false;
            checkBox6.Checked = _isStrongPixels1 = true;
            checkBox7.Checked = _isDetectMotion = true;
            checkBox8.Checked = _isStrongPixels2 = false;
            checkBox9.Checked = _isDisplayPredictions = true;
            checkBox10.Checked = _isClustering = true;
            checkBox11.Checked = _dispalyTrash = false;
            checkBox12.Checked = _isSobel2 = true;
            _settingsForm = new Form2(this);
            var deviceCollection = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            _captureDevice = new VideoCaptureDevice(deviceCollection[0].MonikerString);
            _captureDevice.NewFrame += new NewFrameEventHandler(ShowVideoEvent);
            //_captureDevice.Start();
        }

        private void onOpenFileClick(object sender, EventArgs e)
        {
            _prevImages = new List<byte[]>();
            _prevDetectImages = new List<byte[]>();
            if (_videoFromFile != null)
            {
                ClearPictureBox();
            }
            _openFileDialog = new OpenFileDialog();
            if (_openFileDialog.ShowDialog() == DialogResult.OK)
            {
                _fileName = _openFileDialog.FileName;
            }
            else
            {
                return;
            }
            if (_fileName.Contains(".wmv") || _fileName.Contains(".mp4") || _fileName.Contains(".avi"))
            {
                checkBox7.Checked = true;
                checkBox7.Visible = true;
                _isVideo = true;
                _videoFromFile = new CancellationTokenSource();
                Task.Run(async () => await ShowVideoFromFile(), _videoFromFile.Token);
            }
            else
            {
                checkBox7.Checked = false;
                checkBox7.Visible = false;
                _isVideo = false;
                var file = new Bitmap(_fileName);
                pictureBox1.Image = DetectPredictions(file);
            }
        }

        private async Task ShowVideoFromFile()
        {
            try
            {
                using (_videoFrameReader = new VideoFrameReader(_fileName))
                {
                    int i = 0;
                    _isVideo = true;
                    while (_isVideo && _videoFrameReader.Read()) //Only if frame was read successfully
                    {
                        //Get a System.Drawing.Bitmap for the current frame
                        //You are responsible for disposing the bitmap when you are finished with it.
                        //So it's good practice to have a "using" statement for the retrieved bitmap.
                        using (var frame = _videoFrameReader.GetFrame())
                        {
                            var image = DetectPredictions(frame);
                            UpdateToolSripMenu(image);
                            pictureBox1.Image = image;
                            //_imagesFromVideo.Add(image);
                            i++;
                        }
                    }
                    _isVideo = false;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            //var settings = new VideoEncoderSettings(width: 1920, height: 1080, framerate: 30, codec: VideoCodec.H264);
            //settings.EncoderPreset = EncoderPreset.Fast;
            //settings.CRF = 17;
            //using (var file = MediaBuilder.CreateContainer($"{AppConstant.TimelapsePath}\\timelapse.avi").WithVideo(settings).Create())
            //{
            //    while (file.Video.FramesCount < 300)
            //    {
            //        file.Video.AddFrame(/*Your code*/);
            //    }
            //}
        }

        private void ShowVideoEvent(object sender, NewFrameEventArgs e)
        {
            UpdateToolSripMenu((Bitmap)e.Frame.Clone());
            pictureBox1.Image = DetectPredictions((Bitmap)e.Frame.Clone());
        }

        public Bitmap DetectPredictions(Bitmap image)
        {
            var inputImage = new Bitmap(image);
            int sizeCoef = _imageSizeCoef;
            BitmapData srcInputData = inputImage.LockBits(new Rectangle(0, 0, inputImage.Width, inputImage.Height),
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int inputImageBytes = srcInputData.Stride * srcInputData.Height;
            var inputImageBuffer = new byte[inputImageBytes];
            Marshal.Copy(srcInputData.Scan0, inputImageBuffer, 0, inputImageBytes);
            _imageInputWidth = inputImage.Width;
            _imageInputHeight = inputImage.Height;
            _imageWidth = _imageInputWidth / sizeCoef;
            _imageHeight = _imageInputHeight / sizeCoef;
            _settingsForm.UpdateTrackBars();

            image = ResizeImage(image, _imageWidth, _imageHeight);
            BitmapData srcData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height),
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int bytes = srcData.Stride * srcData.Height;
            var buffer = new byte[bytes];
            int width = srcData.Width;
            int height = srcData.Height;
            var degree = new byte[height, width];

            Marshal.Copy(srcData.Scan0, buffer, 0, bytes);
            buffer = _isContrast ? FilterService.Contrast(srcData, buffer, _contrast) : buffer;
            buffer = _isMonochrome ? FilterService.Monochrome(srcData, buffer) : buffer;
            buffer = _isBlur ? FilterService.Blur(srcData, buffer, _blurMask) : buffer;
            buffer = _isSobel1 ? FilterService.Sobel(srcData, buffer, ref degree, _sobelMinPorog, false) : buffer;
            buffer = _isClustering ? FilterService.Clustering(srcData, buffer, _clusteringCoef) : buffer;
            buffer = _isSobel2 ? FilterService.Sobel(srcData, buffer, ref degree, _sobelMinPorog, true) : buffer;
            buffer = _isNonMaxSuppersession ? FilterService.NonMaxSuppression(srcData, buffer, ref degree) : buffer;
            buffer = _isStrongPixels1 ? FilterService.StrongPixels(srcData, buffer, _strongPixelFilter, _strongPixelPercent) : buffer;
            buffer = _isDetectMotion ? DetectMotion(srcData, buffer, _detectMotionFrameCount) : buffer;
            buffer = _isStrongPixels2 ? FilterService.StrongPixels(srcData, buffer, _strongPixelFilter, _strongPixelPercent) : buffer;


            BitmapData resultData = null;
            if (_isDisplayPredictions)
            {
                var predictions = FindPredictions(srcData, buffer);
                buffer = DisplayPredictions(srcInputData, inputImageBuffer, predictions, sizeCoef);
                image = new Bitmap(srcInputData.Width, srcInputData.Height);
                resultData = image.LockBits(new Rectangle(0, 0, srcInputData.Width, srcInputData.Height),
                    ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                Marshal.Copy(buffer, 0, resultData.Scan0, buffer.Length);
            }
            else
            {
                image = new Bitmap(srcData.Width, srcData.Height);
                resultData = image.LockBits(new Rectangle(0, 0, srcData.Width, srcData.Height),
                    ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                Marshal.Copy(buffer, 0, resultData.Scan0, buffer.Length);
            }

            image.UnlockBits(resultData);

            return image;
        }


        public byte[] DisplayPredictions(BitmapData srcInputData, byte[] inputBuffer, List<Prediction> predictions, int sizeCoef)
        {
            var result = new List<Prediction>();
            var mlNetResult = new List<Prediction>();
            var resultBuffer = new byte[inputBuffer.Length];
            inputBuffer.CopyTo(resultBuffer);
            foreach (var prediction in predictions)
            {
                var predResult = new Prediction();
                predResult = GetPredictionBuffer(prediction, srcInputData, inputBuffer, sizeCoef);
                result.Add(predResult);
            }
            foreach (var prediction in result)
            {
                BitmapData resultData;
                int imageWidth = sizeCoef * prediction.Width;
                int imageHeigth = sizeCoef * prediction.Height;
                var image = new Bitmap(imageWidth, imageHeigth, PixelFormat.Format32bppArgb);
                resultData = image.LockBits(new Rectangle(0, 0, imageWidth, imageHeigth),
                    ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                Marshal.Copy(prediction.Buffer, 0, resultData.Scan0, prediction.Buffer.Length);
                image.UnlockBits(resultData);
                var mlPrediction = _mlNetService.ClassifySingleImage(image);
                if (mlPrediction.PredictedLabelValue != "trash" || _dispalyTrash)
                {
                    prediction.Image = image;
                    prediction.Label = mlPrediction.PredictedLabelValue;
                    prediction.Score = mlPrediction.CurrentScore;
                    mlNetResult.Add(prediction);
                }
            }

            foreach (var prediction in mlNetResult)
            {
                DisplayPrediction(prediction, srcInputData, ref resultBuffer, sizeCoef);
                if (flowLayoutPanel1.InvokeRequired)
                {
                    flowLayoutPanel1.Invoke(new MethodInvoker(delegate
                    {
                        var view = new PictureBox();
                        view.Image = prediction.Image;
                        view.SizeMode = PictureBoxSizeMode.Zoom;
                        view.Width = 150;
                        view.Parent = flowLayoutPanel1;
                        flowLayoutPanel1.SetFlowBreak(view, true);

                        var viewTextLabel = new Label();
                        viewTextLabel.Text = $"label: {prediction.Label}";
                        viewTextLabel.Width = 150;
                        viewTextLabel.Parent = flowLayoutPanel1;
                        flowLayoutPanel1.SetFlowBreak(viewTextLabel, true);

                        var viewTextScore = new Label();
                        viewTextScore.Text = $"score: {prediction.Score}";
                        viewTextScore.Width = 150;
                        viewTextScore.Parent = flowLayoutPanel1;
                        flowLayoutPanel1.SetFlowBreak(viewTextScore, true);
                    }));
                }

                //var files = Directory.GetFiles($"{AppConstant.TrainDataPath}\\{prediction.Label}");

                //var lastIndex = files.ToList().Max(x => {
                //    var fileName = x.Substring(x.LastIndexOf("\\") + 1);
                //    var startIndex = fileName.IndexOf(prediction.Label) + prediction.Label.Length;
                //    var endIndex = fileName.IndexOf(".");
                //    var index = fileName.Substring(startIndex, endIndex - startIndex);
                //    return int.Parse(index);
                //});

                //int newIndex = lastIndex + 1;
                //prediction.Image.Save($"{AppConstant.TrainDataPath}\\{prediction.Label}\\{prediction.Label}{newIndex}.jpg");

            }
            if (flowLayoutPanel1.Controls.Count > 30)
            {
                for (int i = 0; i <= flowLayoutPanel1.Controls.Count - 30; i++)
                {
                    if (flowLayoutPanel1.InvokeRequired)
                    {
                        flowLayoutPanel1.Invoke(new MethodInvoker(delegate
                        {
                            flowLayoutPanel1.Controls.Remove(flowLayoutPanel1.Controls[0]);
                        }));
                    }
                }
            }
            return resultBuffer;
        }

        public void DisplayPrediction(Prediction prediction, BitmapData srcInputData, ref byte[] resultBuffer, int sizeCoef)
        {
            int byteInputOffset = 0;
            int leftX = sizeCoef * (prediction.X - prediction.WidthL);
            int rightX = sizeCoef * (prediction.X + prediction.WidthR);
            int topY = sizeCoef * (prediction.Y - prediction.HeightT);
            int bottomY = sizeCoef * (prediction.Y + prediction.HeightB);
            for (int y = topY; y <= bottomY; y++)
            {
                for (int x = leftX; x <= rightX; x++)
                {
                    byteInputOffset = y * srcInputData.Stride + x * 4;

                    if (byteInputOffset % 4 != 0)
                    {
                        byteInputOffset = byteInputOffset - byteInputOffset % 4;
                    }


                    if (y == topY || y == bottomY || x == leftX || x == rightX)
                    {
                        resultBuffer[byteInputOffset] = 255;
                        resultBuffer[byteInputOffset + 1] = 255;
                        resultBuffer[byteInputOffset + 2] = 255;
                    }
                }
            }

        }

        public byte[] DetectMotion(BitmapData srcData, byte[] buffer, int detectMotionFrameCount)
        {
            int filterOffset = 1;
            int byteOffset = 0;
            byte[] result = buffer.Clone() as byte[];
            if (_prevImages.Count - detectMotionFrameCount < 0)
            {
                _prevImages.Add(result);
                return result;
            }
            _prevImages.Add(result);

            for (int y = filterOffset; y < srcData.Height - filterOffset; y++)
            {
                for (int x = filterOffset; x < srcData.Width - filterOffset; x++)
                {
                    bool isDetected = true;
                    byteOffset = y * srcData.Stride + x * 4;

                    for (int i = _prevImages.Count - 1; i > _prevImages.Count - detectMotionFrameCount - 1; i--)
                    {
                        isDetected = isDetected && (_prevImages[i][byteOffset] != 0 && buffer[byteOffset] != 0);
                    }
                    if (isDetected)
                    {
                        buffer[byteOffset] = 0;
                        buffer[byteOffset + 1] = 0;
                        buffer[byteOffset + 2] = 0;
                    }
                }
            }
            result = buffer.Clone() as byte[];
            if (_prevDetectImages.Count - detectMotionFrameCount < 0)
            {
                _prevDetectImages.Add(result);
                return result;
            }
            _prevDetectImages.Add(result);
            for (int y = filterOffset; y < srcData.Height - filterOffset; y++)
            {
                for (int x = filterOffset; x < srcData.Width - filterOffset; x++)
                {
                    byteOffset = y * srcData.Stride + x * 4;
                    for (int i = _prevDetectImages.Count - 1; i > _prevDetectImages.Count - detectMotionFrameCount - 1; i--)
                    {
                        if (_prevDetectImages[i][byteOffset] != 0 && buffer[byteOffset] == 0)
                        {
                            buffer[byteOffset] = 255;
                            buffer[byteOffset + 1] = 255;
                            buffer[byteOffset + 2] = 255;
                        }
                    }
                }
            }
            return buffer;
        }

        private List<Prediction> FindPredictions(BitmapData srcData, byte[] buffer)
        {

            var predictions = new List<Prediction>();
            int filterOffset = 3, byteOffset = 0;
            var usedPixelMap = new double[srcData.Height, srcData.Width];
            for (int y = filterOffset; y < srcData.Height - filterOffset; y++)
            {
                for (int x = filterOffset; x < srcData.Width - filterOffset; x++)
                {
                    Prediction currentPrediction = null;
                    byteOffset = y * srcData.Stride + x * 4;
                    double predictionPercent = 1;
                    bool isPrediction = buffer[byteOffset] != 0;


                    if (isPrediction)
                    {
                        predictionPercent = PredictionPixelPercent(srcData, buffer, filterOffset, y, x);

                        currentPrediction = new Prediction()
                        {
                            HeightT = 3,
                            HeightB = 3,
                            WidthL = 3,
                            WidthR = 3,
                            X = x,
                            Y = y,
                            MaxPercent = predictionPercent,
                        };
                        predictions.Add(currentPrediction);
                        SetUsedPixel(currentPrediction, ref usedPixelMap, ref predictions);
                    }
                }
            }

            predictions = new List<Prediction>();
            for (int i = 0; i < 20; i++)
            {
                var maxVal = usedPixelMap.ArgMax();

                var prediction = new Prediction()
                {
                    X = maxVal.Item2,
                    Y = maxVal.Item1,
                    HeightT = GetHeightT(srcData, usedPixelMap, maxVal.Item2, maxVal.Item1, 0),
                    HeightB = GetHeightB(srcData, usedPixelMap, maxVal.Item2, maxVal.Item1, 0),
                    WidthL = GetWidthL(srcData, usedPixelMap, maxVal.Item2, maxVal.Item1, 0),
                    WidthR = GetWidthR(srcData, usedPixelMap, maxVal.Item2, maxVal.Item1, 0),
                };
                predictions.Add(prediction);

                for (int fy = prediction.Y - prediction.HeightT; fy < prediction.Y + prediction.HeightB; fy++)
                {
                    for (int fx = prediction.X - prediction.WidthL; fx < prediction.X + prediction.WidthR; fx++)
                    {
                        usedPixelMap[fy, fx] = 0;
                    }
                }

            }
            return predictions;
        }

        public int GetHeightT(BitmapData srcData, double[,] usedPixelMap, int x, int y, int heightT)
        {
            int result = heightT;
            if (y - heightT <= 0 || x - 1 <= 0 || x + 1 >= srcData.Width || y + 1 >= srcData.Height)
            {
                return result;
            }
            if (usedPixelMap[y - heightT, x] != 0 || usedPixelMap[y - heightT, x - 1] != 0 || usedPixelMap[y - heightT, x + 1] != 0)
            {
                heightT++;
                result = GetHeightT(srcData, usedPixelMap, x, y, heightT);
            }
            return result;
        }

        public int GetHeightB(BitmapData srcData, double[,] usedPixelMap, int x, int y, int heightB)
        {
            int result = heightB;
            if (y - 1 <= 0 || x - 1 <= 0 || x + 1 >= srcData.Width || y + heightB >= srcData.Height)
            {
                return result;
            }
            if (usedPixelMap[y + heightB, x] != 0 || usedPixelMap[y + heightB, x - 1] != 0 || usedPixelMap[y + heightB, x + 1] != 0)
            {
                heightB++;
                result = GetHeightB(srcData, usedPixelMap, x, y, heightB);
            }
            return result;
        }

        public int GetWidthL(BitmapData srcData, double[,] usedPixelMap, int x, int y, int widthL)
        {
            int result = widthL;
            if (y - 1 <= 0 || x - widthL <= 0 || x + 1 >= srcData.Width || y + 1 >= srcData.Height)
            {
                return result;
            }
            if (usedPixelMap[y - 1, x - widthL] != 0 || usedPixelMap[y, x - widthL] != 0 || usedPixelMap[y + 1, x - widthL] != 0)
            {
                widthL++;
                result = GetWidthL(srcData, usedPixelMap, x, y, widthL);
            }
            return result;
        }

        public int GetWidthR(BitmapData srcData, double[,] usedPixelMap, int x, int y, int widthR)
        {
            int result = widthR;
            if (y - 1 <= 0 || x - 1 <= 0 || x + widthR >= srcData.Width || y + 1 >= srcData.Height)
            {
                return result;
            }
            if (usedPixelMap[y - 1, x + widthR] != 0 || usedPixelMap[y, x + widthR] != 0 || usedPixelMap[y + 1, x + widthR] != 0)
            {
                widthR++;
                result = GetWidthR(srcData, usedPixelMap, x, y, widthR);
            }
            return result;
        }

        public void SetUsedPixel(Prediction currentPrediction, ref double[,] usedPixelMap, ref List<Prediction> predictions)
        {
            for (int fy = currentPrediction.Y - currentPrediction.HeightT; fy < currentPrediction.Y + currentPrediction.HeightB; fy++)
            {
                for (int fx = currentPrediction.X - currentPrediction.WidthL; fx < currentPrediction.X + currentPrediction.WidthR; fx++)
                {
                    usedPixelMap[fy, fx] += currentPrediction.MaxPercent;
                }
            }
        }

        public double PredictionPixelPercent(BitmapData srcData, byte[] buffer, int filterOffset, int y, int x)
        {
            int predictionPixelsCount = 0, totalPixelCount = 0;
            for (int fy = y - filterOffset; fy < y + filterOffset; fy++)
            {
                for (int fx = x - filterOffset; fx < x + filterOffset; fx++)
                {
                    int pixelOffset = fy * srcData.Stride + fx * 4;
                    if (pixelOffset >= buffer.Length || pixelOffset < 0)
                    {
                        continue;
                    }
                    if (buffer[pixelOffset] != 0)
                    {
                        predictionPixelsCount++;
                    }
                    totalPixelCount++;
                }
            }
            return (double)predictionPixelsCount / totalPixelCount;
        }

        private Prediction GetPredictionBuffer(Prediction prediction, BitmapData srcInputData, byte[] inputBuffer, int sizeCoef)
        {
            int byteOffset = 0, byteInputOffset = 0;
            int resultLengrh = sizeCoef * sizeCoef * prediction.Width * prediction.Height * 4;
            var result = new byte[resultLengrh];
            int leftX = sizeCoef * (prediction.X - prediction.WidthL);
            int rightX = sizeCoef * (prediction.X + prediction.WidthR);
            int topY = sizeCoef * (prediction.Y - prediction.HeightT);
            int bottomY = sizeCoef * (prediction.Y + prediction.HeightB);
            for (int y = topY; y <= bottomY; y++)
            {
                for (int x = leftX; x <= rightX; x++)
                {
                    byteInputOffset = y * srcInputData.Stride + x * 4;
                    byteOffset = ((y - topY) * prediction.Width * 4 * sizeCoef) + (x - leftX) * 4;

                    if (byteInputOffset % 4 != 0)
                    {
                        byteInputOffset = byteInputOffset - byteInputOffset % 4;
                    }

                    result[byteOffset] = inputBuffer[byteInputOffset];
                    result[byteOffset + 1] = inputBuffer[byteInputOffset + 1];
                    result[byteOffset + 2] = inputBuffer[byteInputOffset + 2];
                    result[byteOffset + 3] = 255;
                }
            }

            prediction.Buffer = result;
            return prediction;
        }

        public Bitmap ResizeImage(Image image, int width, int height)
        {
            var rectangle = new Rectangle(0, 0, width, height);
            var newImage = new Bitmap(rectangle.Width, rectangle.Height);
            newImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(newImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, rectangle, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return newImage;
        }

        private void UpdateToolSripMenu(Bitmap image)
        {
            toolStripStatusLabel1.Text = $"Width: {image.Width}";
            toolStripStatusLabel2.Text = $"Height: {image.Height}";
        }

        private void onSettingsClick(object sender, EventArgs e)
        {
            _settingsForm.Show();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            _isMonochrome = checkBox1.Checked;
            if (_fileName != null) redrowImage();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            _isBlur = checkBox2.Checked;
            if (_fileName != null) redrowImage();
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            _isSobel1 = checkBox3.Checked;
            if (_fileName != null) redrowImage();
        }

        private void checkBox12_CheckedChanged(object sender, EventArgs e)
        {
            _isSobel2 = checkBox12.Checked;
            if (_fileName != null) redrowImage();
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            _isContrast = checkBox4.Checked;
            if (_fileName != null) redrowImage();
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            _isNonMaxSuppersession = checkBox5.Checked;
            if (_fileName != null) redrowImage();
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            _isStrongPixels1 = checkBox6.Checked;
            if (_fileName != null) redrowImage();
        }

        private void checkBox7_CheckedChanged(object sender, EventArgs e)
        {
            _isDetectMotion = checkBox7.Checked;
            if (_fileName != null) redrowImage();
        }

        private void checkBox8_CheckedChanged(object sender, EventArgs e)
        {
            _isStrongPixels2 = checkBox8.Checked;
            if (_fileName != null) redrowImage();
        }

        private void checkBox9_CheckedChanged(object sender, EventArgs e)
        {
            _isDisplayPredictions = checkBox9.Checked;
            if (_fileName != null) redrowImage();
        }

        private void checkBox10_CheckedChanged(object sender, EventArgs e)
        {
            _isClustering = checkBox10.Checked;
            if (_fileName != null) redrowImage();
        }

        private void checkBox11_CheckedChanged(object sender, EventArgs e)
        {
            _dispalyTrash = checkBox11.Checked;
            if (_fileName != null) redrowImage();
        }

        private void redrowImage()
        {
            if (_fileName != null && (_fileName.Contains(".jpg") || _fileName.Contains(".png")))
            {
                var file = new Bitmap(_fileName);
                pictureBox1.Image = DetectPredictions(file);
            }
        }

        private void clearViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClearPictureBox();
        }

        public void ClearPictureBox()
        {
            _videoFromFile.Cancel();
            _videoFrameReader.Dispose();
            _isVideo = false;
            if (pictureBox1.Image != null)
            {
                pictureBox1.Image.Dispose();
                pictureBox1.Image = null;
            }
        }
    }
}
