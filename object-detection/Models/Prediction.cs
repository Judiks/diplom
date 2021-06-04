using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace object_detector.Models
{
    public class Prediction
    {
        public int WidthL { get; set; }
        public int WidthR { get; set; }
        public int HeightT { get; set; }
        public int HeightB { get; set; }
        public int Height { get { return HeightB + HeightT + 1; } }
        public int Width { get { return WidthL + WidthR + 1; } }
        public int X { get; set; }
        public int Y { get; set; }
        public byte[] Buffer { get; set; }
        public Bitmap Image { get; set; }
        public double Score { get; set; }
        public string Label { get; set; }
        public double MaxPercent { get; set; }
        public bool IsSave { get; set; }
    }
}
