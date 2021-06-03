using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace object_detector.Services
{
    public static class FilterService
    {
        public static byte[] Contrast(BitmapData srcData, byte[] buffer, float value)
        {
            int byteOffset = 0;
            value = (100.0f + value) / 100.0f;

            for (int y = 0; y < srcData.Height; y++)
            {
                for (int x = 0; x < srcData.Width; x++)
                {
                    byteOffset = y * srcData.Stride + x * 4;
                    byte B = buffer[byteOffset];
                    byte G = buffer[byteOffset + 1];
                    byte R = buffer[byteOffset + 2];

                    float Red = R / 255.0f;
                    float Green = G / 255.0f;
                    float Blue = B / 255.0f;
                    Red = (((Red - 0.5f) * value) + 0.5f) * 255.0f;
                    Green = (((Green - 0.5f) * value) + 0.5f) * 255.0f;
                    Blue = (((Blue - 0.5f) * value) + 0.5f) * 255.0f;

                    int iR = (int)Red;
                    iR = iR > 255 ? 255 : iR;
                    iR = iR < 0 ? 0 : iR;
                    int iG = (int)Green;
                    iG = iG > 255 ? 255 : iG;
                    iG = iG < 0 ? 0 : iG;
                    int iB = (int)Blue;
                    iB = iB > 255 ? 255 : iB;
                    iB = iB < 0 ? 0 : iB;

                    buffer[byteOffset] = (byte)iB;
                    buffer[byteOffset + 1] = (byte)iG;
                    buffer[byteOffset + 2] = (byte)iR;
                }
            }
            return buffer;
        }

        public static byte[] Monochrome(BitmapData srcData, byte[] buffer)
        {
            int byteOffset = 0;
            for (int y = 0; y < srcData.Height; y++)
            {
                for (int x = 0; x < srcData.Width; x++)
                {
                    byteOffset = y * srcData.Stride + x * 4;
                    byte middle = (byte)((buffer[byteOffset] + buffer[byteOffset + 1] + buffer[byteOffset + 2]) / 3);
                    buffer[byteOffset] = middle;
                    buffer[byteOffset + 1] = middle;
                    buffer[byteOffset + 2] = middle;
                }
            }
            return buffer;
        }

        public static byte[] Clustering(BitmapData srcData, byte[] buffer, int coef)
        {
            byte[] result = new byte[buffer.Length];
            int filterOffset = 1;
            int byteOffset = 0, pixelOffset = 0;
            for (int y = filterOffset; y < srcData.Height - filterOffset; y++)
            {
                for (int x = filterOffset; x < srcData.Width - filterOffset; x++)
                {
                    byteOffset = y * srcData.Stride + x * 4;
                    for (int fy = 0; fy <= filterOffset; fy++)
                    {
                        for (int fx = 0; fx <= filterOffset; fx++)
                        {
                            pixelOffset = byteOffset + fy * srcData.Stride + fx * 4;
                            if (buffer[byteOffset] - buffer[pixelOffset] > -coef &&
                                buffer[byteOffset] - buffer[pixelOffset] < coef &&
                                buffer[byteOffset + 1] - buffer[pixelOffset + 1] > -coef &&
                                buffer[byteOffset + 1] - buffer[pixelOffset + 1] < coef &&
                                buffer[byteOffset + 2] - buffer[pixelOffset + 2] > -coef &&
                                buffer[byteOffset + 2] - buffer[pixelOffset + 2] < coef)
                            {
                                buffer[pixelOffset] = buffer[byteOffset];
                                buffer[pixelOffset + 1] = buffer[byteOffset + 1];
                                buffer[pixelOffset + 2] = buffer[byteOffset + 2];
                                buffer[pixelOffset + 3] = buffer[byteOffset + 3];
                            }
                        }

                    }

                }
            }

            return buffer;
        }

        public static bool IsStrongArray(BitmapData srcData, int byteOffset, byte[] buffer, int strongPixelFilter, int strongPixelPercent)
        {
            int filterOffset = strongPixelFilter;
            int pixelOffset = 0;
            double total = 0;
            for (int OffsetY = -filterOffset; OffsetY < filterOffset; OffsetY++)
            {
                for (int OffsetX = -filterOffset; OffsetX < filterOffset; OffsetX++)
                {
                    pixelOffset = byteOffset + OffsetY * srcData.Stride + OffsetX * 4;
                    if (pixelOffset < 0 || pixelOffset >= buffer.Length)
                    {
                        continue;
                    }
                    if (buffer[pixelOffset] > 0)
                        total++;
                }
            }
            double avarageValue = total / Math.Pow(strongPixelFilter + strongPixelFilter + 1, 2);

            if (avarageValue > strongPixelPercent / 100f)
            {
                return true;
            }
            return false;
        }

        public static byte[] StrongPixels(BitmapData srcData, byte[] buffer, int strongPixelFilter, int strongPixelPercent)
        {
            int byteOffset = 0;
            int filterOffset = 1;
            var result = buffer.Clone() as byte[];
            for (int OffsetY = filterOffset; OffsetY < srcData.Height - filterOffset; OffsetY++)
            {
                for (int OffsetX = filterOffset; OffsetX < srcData.Width - filterOffset; OffsetX++)
                {
                    byteOffset = OffsetY * srcData.Stride + OffsetX * 4;
                    bool isStrong = IsStrongArray(srcData, byteOffset, buffer, strongPixelFilter, strongPixelPercent);
                    if (!isStrong)
                    {
                        result[byteOffset] = 0;
                        result[byteOffset + 1] = 0;
                        result[byteOffset + 2] = 0;
                        result[byteOffset + 3] = 255;
                    }
                }
            }
            return result;
        }

        public static byte[] NonMaxSuppression(BitmapData srcData, byte[] buffer, ref byte[,] degree)
        {
            int byteOffset = 0;
            int filterOffset = 1;
            var pixels = new byte[8];
            byte forward = 0, back = 0;
            for (int OffsetY = filterOffset; OffsetY < srcData.Height - filterOffset; OffsetY++)
            {
                for (int OffsetX = filterOffset; OffsetX < srcData.Width - filterOffset; OffsetX++)
                {
                    forward = back = 0;
                    byteOffset = OffsetY * srcData.Stride + OffsetX * 4;
                    pixels[0] = buffer[byteOffset - srcData.Stride - 4]; // 0
                    pixels[1] = buffer[byteOffset - 4];                  // 1
                    pixels[2] = buffer[byteOffset + srcData.Stride - 4]; // 2
                    pixels[3] = buffer[byteOffset + srcData.Stride];     // 3   // 0 3 5
                    pixels[4] = buffer[byteOffset + srcData.Stride + 4]; // 4   // 1 x 6
                    pixels[5] = buffer[byteOffset + 4];                  // 5   // 2 4 7
                    pixels[6] = buffer[byteOffset - srcData.Stride + 4]; // 6
                    pixels[7] = buffer[byteOffset - srcData.Stride];     // 7
                    switch (degree[OffsetY, OffsetX])
                    {
                        case 0:
                            {
                                forward = pixels[3];
                                back = pixels[4];
                                break;
                            }
                        case 45:
                            {
                                forward = pixels[5];
                                back = pixels[2];
                                break;
                            }
                        case 90:
                            {
                                forward = pixels[6];
                                back = pixels[1];
                                break;
                            }
                        case 135:
                            {
                                forward = pixels[7];
                                back = pixels[0];
                                break;
                            }
                        default:
                            break;

                    }

                    if ((buffer[byteOffset] < forward) || (buffer[byteOffset] < back))
                    {
                        buffer[byteOffset] = 0;
                        buffer[byteOffset + 1] = 0;
                        buffer[byteOffset + 2] = 0;
                    }
                    else
                    {
                        buffer[byteOffset] = buffer[byteOffset];
                        buffer[byteOffset + 1] = buffer[byteOffset];
                        buffer[byteOffset + 2] = buffer[byteOffset];
                    }
                }
            }
            return buffer;
        }

        public static byte[] Sobel(BitmapData srcData, byte[] buffer, ref byte[,] degree, int sobelMinPorog, bool isResult)
        {
            double gx = 0;
            double gy = 0;
            double gradient = 0;
            var result = new byte[buffer.Length];
            var pixels = new byte[8];
            int temp;
            double div;
            //This is how much your center pixel is offset from the border of your kernel
            //Sobel is 3x3, so center is 1 pixel from the kernel border
            int filterOffset = 1;
            int byteOffset = 0;

            //Start with the pixel that is offset 1 from top and 1 from the left side
            //this is so entire kernel is on your image
            for (int y = filterOffset; y < srcData.Height - filterOffset; y++)
            {
                for (int x = filterOffset; x < srcData.Width - filterOffset; x++)
                {
                    //position of the kernel center pixel
                    byteOffset = y * srcData.Stride + x * 4;
                    pixels[0] = buffer[byteOffset - srcData.Stride - 4];
                    pixels[1] = buffer[byteOffset - 4];
                    pixels[2] = buffer[byteOffset + srcData.Stride - 4];
                    pixels[3] = buffer[byteOffset + srcData.Stride];
                    pixels[4] = buffer[byteOffset + srcData.Stride + 4];
                    pixels[5] = buffer[byteOffset + 4];
                    pixels[6] = buffer[byteOffset - srcData.Stride + 4];
                    pixels[7] = buffer[byteOffset - srcData.Stride];

                    gx = (pixels[2] + 2 * pixels[3] + pixels[4]) - (pixels[0] + 2 * pixels[7] + pixels[6]);
                    gy = (pixels[0] + 2 * pixels[1] + pixels[2]) - (pixels[6] + 2 * pixels[5] + pixels[4]);
                    gradient = Math.Sqrt(gx * gx + gy * gy);

                    //set new data in the other byte array for your image data
                    if (gradient > 255)
                    {
                        result[byteOffset] = 255;
                        result[byteOffset + 1] = 255;
                        result[byteOffset + 2] = 255;
                        result[byteOffset + 3] = 255;
                    }
                    else if (gradient < sobelMinPorog)
                    {
                        result[byteOffset] = 0;
                        result[byteOffset + 1] = 0;
                        result[byteOffset + 2] = 0;
                        result[byteOffset + 3] = 255;
                    }
                    else
                    {
                        result[byteOffset] = (byte)gradient;
                        result[byteOffset + 1] = (byte)gradient;
                        result[byteOffset + 2] = (byte)gradient;
                        result[byteOffset + 3] = 255;
                    }

                    if (gx == 0)
                    {
                        temp = (gy == 0) ? 0 : 90;
                    }
                    else
                    {
                        div = (double)gy / (double)gx;

                        if (div < 0)
                        {
                            temp = (int)(180 - Math.Atan(-div) * 180 / Math.PI);
                        }
                        else
                        {
                            temp = (int)(Math.Atan(div) * 180 / Math.PI);
                        }

                        if (temp < 22.5)
                        {
                            temp = 0;
                        }
                        else if (temp < 67.5)
                        {
                            temp = 45;
                        }
                        else if (temp < 112.5)
                        {
                            temp = 90;
                        }
                        else if (temp < 157.5)
                        {
                            temp = 135;
                        }
                        else
                        {
                            temp = 0;
                        }
                    }
                    degree[y, x] = (byte)temp;
                }
            }
            for (int y = filterOffset; y < srcData.Height - filterOffset; y++)
            {
                for (int x = filterOffset; x < srcData.Width - filterOffset; x++)
                {
                    byteOffset = y * srcData.Stride + x * 4;
                    if (result[byteOffset] != 0)
                    {
                        buffer[byteOffset] = result[byteOffset];
                        buffer[byteOffset + 1] = result[byteOffset + 1];
                        buffer[byteOffset + 2] = result[byteOffset + 2];
                    }
                }
            }
            return isResult ? result : buffer;
        }

        private static float[,] GaussianBlur(int lenght, double weight)
        {
            var kernel = new float[lenght, lenght];
            float kernelSum = 0;
            int foff = (lenght - 1) / 2;
            double distance = 0;
            float constant = 1f / (float)(2 * Math.PI * weight * weight);
            for (int y = -foff; y <= foff; y++)
            {
                for (int x = -foff; x <= foff; x++)
                {
                    distance = ((y * y) + (x * x)) / (2 * weight * weight);
                    kernel[y + foff, x + foff] = constant * (float)Math.Exp(-distance);
                    kernelSum += kernel[y + foff, x + foff];
                }
            }
            for (int y = 0; y < lenght; y++)
            {
                for (int x = 0; x < lenght; x++)
                {
                    kernel[y, x] = kernel[y, x] * 1f / kernelSum;
                }
            }
            return kernel;
        }

        public static byte[] Blur(BitmapData srcData, byte[] buffer, int kernelSize)
        {
            var colorChannels = 3;
            var result = new byte[buffer.Length];
            var rgb = new double[colorChannels];
            var kernel = GaussianBlur(kernelSize, srcData.Width);
            var foff = (kernel.GetLength(0) - 1) / 2;
            var kcenter = 0;
            var kpixel = 0;
            for (int y = foff; y < srcData.Height - foff; y++)
            {
                for (int x = foff; x < srcData.Width - foff; x++)
                {
                    for (int c = 0; c < colorChannels; c++)
                    {
                        rgb[c] = 0.0;
                    }
                    kcenter = y * srcData.Stride + x * 4;
                    for (int fy = -foff; fy <= foff; fy++)
                    {
                        for (int fx = -foff; fx <= foff; fx++)
                        {
                            kpixel = kcenter + fy * srcData.Stride + fx * 4;
                            for (int c = 0; c < colorChannels; c++)
                            {
                                rgb[c] += (buffer[kpixel + c]) * kernel[fy + foff, fx + foff];
                            }
                        }
                    }
                    for (int c = 0; c < colorChannels; c++)
                    {
                        if (rgb[c] > 255)
                        {
                            rgb[c] = 255;
                        }
                        else if (rgb[c] < 0)
                        {
                            rgb[c] = 0;
                        }
                    }
                    for (int c = 0; c < colorChannels; c++)
                    {
                        result[kcenter + c] = (byte)rgb[c];
                    }
                    result[kcenter + 3] = 255;
                }
            }
            return result;
        }
    }
}
