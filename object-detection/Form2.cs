using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace object_detector
{
    public partial class Form2 : Form
    {
        private readonly Form1 _form1;
        public Form2(Form1 from1)
        {
            _form1 = from1;
            InitializeComponent();
            trackBar1.Maximum = 255;
            trackBar2.Maximum = 10;
            trackBar3.Maximum = 100;
            trackBar4.Maximum = 100;
            trackBar5.Maximum = 10;
            trackBar5.Minimum = 1;
            trackBar7.Maximum = 20;
            trackBar8.Maximum = 100;
            trackBar9.Maximum = 100;
            trackBar12.Maximum = 20;
            UpdateTrackBars();
        }
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            _form1._sobelMinPorog = trackBar1.Value;
            UpdateLabels();
        }
        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            _form1._blurMask = trackBar2.Value;
            UpdateLabels();
        }
        private void trackBar3_Scroll(object sender, EventArgs e)
        {
            _form1._blurValue = trackBar3.Value;
            UpdateLabels();
        }

        private void trackBar4_Scroll(object sender, EventArgs e)
        {
            _form1._contrast = trackBar4.Value;
            UpdateLabels();
        }

        private void trackBar5_Scroll(object sender, EventArgs e)
        {
            _form1._imageSizeCoef = trackBar5.Value;
            _form1._imageHeight = _form1._imageSizeCoef != 0 ? _form1._imageInputHeight / _form1._imageSizeCoef : _form1._imageInputHeight;
            _form1._imageWidth = _form1._imageSizeCoef != 0 ? _form1._imageInputWidth / _form1._imageSizeCoef : _form1._imageInputWidth;
            UpdateLabels();
        }

        private void trackBar7_Scroll(object sender, EventArgs e)
        {
            _form1._strongPixelFilter = trackBar7.Value;
            UpdateLabels();
        }

        private void trackBar8_Scroll(object sender, EventArgs e)
        {
            _form1._strongPixelPercent = trackBar8.Value;
            UpdateLabels();
        }

        private void trackBar9_Scroll(object sender, EventArgs e)
        {
            _form1._predictionsCount = trackBar9.Value;
            UpdateLabels();
        }

        private void trackBar12_Scroll(object sender, EventArgs e)
        {
            _form1._detectMotionFrameCount = trackBar12.Value;
            UpdateLabels();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            StreamWriter sw;
            SaveFileDialog saveFileDialog = new SaveFileDialog();

            saveFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            saveFileDialog.DefaultExt = ".txt";
            saveFileDialog.FilterIndex = 2;
            saveFileDialog.RestoreDirectory = true;
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                sw = new StreamWriter(saveFileDialog.OpenFile());
                sw.WriteLine($"SobelMinPorog: {_form1._sobelMinPorog}");
                sw.WriteLine($"Width: {_form1._imageWidth}");
                sw.WriteLine($"Heiht: {_form1._imageHeight}");
                sw.WriteLine($"Contrast: {_form1._contrast}");
                sw.WriteLine($"BlurFilter: {_form1._blurMask}");
                sw.WriteLine($"BlurValue: {_form1._blurValue}");
                sw.WriteLine($"StrongFilter: {_form1._strongPixelFilter}");
                sw.WriteLine($"Strong %: {_form1._strongPixelPercent}");
                sw.WriteLine($"Predictions Count: {_form1._predictionsCount}");
                sw.Close();
            }
        }

        private void UpdateLabels()
        {
            if (label1.InvokeRequired)
            {
                label1.Invoke(new MethodInvoker(delegate
                {
                    label1.Text = $"Sobel: { trackBar1.Value}";
                }));
            }
            if (label2.InvokeRequired)
            {
                label2.Invoke(new MethodInvoker(delegate
                {
                    label2.Text = $"Blur Mask: { trackBar2.Value}";
                }));
            }
            if (label3.InvokeRequired)
            {
                label3.Invoke(new MethodInvoker(delegate
                {
                    label3.Text = $"Blur Value: { trackBar3.Value}";
                }));
            }
            if (label4.InvokeRequired)
            {
                label4.Invoke(new MethodInvoker(delegate
                {
                    label4.Text = $"Contrast: { trackBar4.Value}";
                }));
            }
            if (label5.InvokeRequired)
            {
                label5.Invoke(new MethodInvoker(delegate
                {
                    label5.Text = $"Compres coef: { trackBar5.Value}";
                }));
            }
            if (label7.InvokeRequired)
            {
                label7.Invoke(new MethodInvoker(delegate
                {
                    label7.Text = $"Strong pixel filter: { trackBar7.Value}";
                }));
            }
            if (label8.InvokeRequired)
            {
                label8.Invoke(new MethodInvoker(delegate
                {
                    label8.Text = $"Strong pixel %: { trackBar8.Value}";
                }));
            }
            if (label9.InvokeRequired)
            {
                label9.Invoke(new MethodInvoker(delegate
                {
                    label9.Text = $"Predictions Count: { trackBar9.Value}";
                }));
            }
            if (label12.InvokeRequired)
            {
                label12.Invoke(new MethodInvoker(delegate
                {
                    label12.Text = $"Detect motion frame count: { trackBar12.Value}";
                }));
            }
        }

        public void UpdateTrackBars()
        {
            if (trackBar1.InvokeRequired)
            {
                trackBar1.Invoke(new MethodInvoker(delegate
                {
                    trackBar1.Value = _form1._sobelMinPorog;
                }));
            }
            if (trackBar2.InvokeRequired)
            {
                trackBar2.Invoke(new MethodInvoker(delegate
                {
                    trackBar2.Value = _form1._blurMask;
                }));
            }
            if (trackBar3.InvokeRequired)
            {
                trackBar3.Invoke(new MethodInvoker(delegate
                {
                    trackBar3.Value = _form1._blurValue;
                }));
            }
            if (trackBar4.InvokeRequired)
            {
                trackBar4.Invoke(new MethodInvoker(delegate
                {
                    trackBar4.Value = _form1._contrast;
                }));
            }
            if (trackBar5.InvokeRequired)
            {
                trackBar5.Invoke(new MethodInvoker(delegate
                {
                    trackBar5.Value = _form1._imageSizeCoef;
                }));
            }
            if (trackBar7.InvokeRequired)
            {
                trackBar7.Invoke(new MethodInvoker(delegate
                {
                    trackBar7.Value = _form1._strongPixelFilter;
                }));
            }
            if (trackBar8.InvokeRequired)
            {
                trackBar8.Invoke(new MethodInvoker(delegate
                {
                    trackBar8.Value = _form1._strongPixelPercent;
                }));
            }
            if (trackBar9.InvokeRequired)
            {
                trackBar9.Invoke(new MethodInvoker(delegate
                {
                    trackBar9.Value = _form1._predictionsCount;
                }));
            }
            if (trackBar12.InvokeRequired)
            {
                trackBar12.Invoke(new MethodInvoker(delegate
                {
                    trackBar12.Value = _form1._detectMotionFrameCount;
                }));
            }

            UpdateLabels();
        }

    }
}
