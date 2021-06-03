using object_recognition.Entities;
using System.Collections.Generic;
using System.Drawing;

namespace object_recognition.Services.Interfaces
{
    public interface IMLNETService
    {
        void Execute();
        ImagePrediction ClassifySingleImage(Bitmap image);
        //IEnumerable<ImagePrediction> ClassifyListImages(List<Bitmap> images);
    }
}
