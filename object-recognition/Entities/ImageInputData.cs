using Microsoft.ML.Transforms.Image;
using System.Drawing;

namespace object_recognition.Entities
{
    public class ImageInputData
    {
        [ImageType(224, 224)]
        public Bitmap Image { get; set; }
        public string Label { get; set; }
        public int Index { get; set; }
    }
}
