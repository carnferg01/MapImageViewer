using System;
using Windows.UI;


namespace MapImageViewer
{

    public static class ColorComparison
    {
        // Convert RGB to XYZ
        private static float[] RgbToXyz(Color color)
        {
            float r = color.R / 255.0f;
            float g = color.G / 255.0f;
            float b = color.B / 255.0f;

            // Apply gamma correction
            r = (r > 0.04045f) ? (float)Math.Pow((r + 0.055f) / 1.055f, 2.4f) : (r / 12.92f);
            g = (g > 0.04045f) ? (float)Math.Pow((g + 0.055f) / 1.055f, 2.4f) : (g / 12.92f);
            b = (b > 0.04045f) ? (float)Math.Pow((b + 0.055f) / 1.055f, 2.4f) : (b / 12.92f);

            // Convert to XYZ
            r *= 100.0f;
            g *= 100.0f;
            b *= 100.0f;

            float x = r * 0.4124564f + g * 0.3575761f + b * 0.1804375f;
            float y = r * 0.2126729f + g * 0.7151522f + b * 0.0721750f;
            float z = r * 0.0193339f + g * 0.1191920f + b * 0.9503041f;

            return new float[] { x, y, z };
        }

        // Convert XYZ to LAB
        private static float[] XyzToLab(float[] xyz)
        {
            float x = xyz[0] / 95.047f;
            float y = xyz[1] / 100.000f;
            float z = xyz[2] / 108.883f;

            x = (x > 0.008856f) ? (float)Math.Pow(x, 0.3333f) : (x * 7.787f + 16.0f / 116.0f);
            y = (y > 0.008856f) ? (float)Math.Pow(y, 0.3333f) : (y * 7.787f + 16.0f / 116.0f);
            z = (z > 0.008856f) ? (float)Math.Pow(z, 0.3333f) : (z * 7.787f + 16.0f / 116.0f);

            float l = Math.Max(0.0f, (116.0f * y) - 16.0f);
            float a = (x - y) * 500.0f;
            float b = (y - z) * 200.0f;

            return new float[] { l, a, b };
        }

        // Convert RGB to LAB
        private static float[] RgbToLab(Color color)
        {
            return XyzToLab(RgbToXyz(color));
        }

        // CIEDE2000 Calculation
        private static float Ciede2000(float[] lab1, float[] lab2)
        {
            const float kL = 1.0f;
            const float kC = 1.0f;
            const float kH = 1.0f;

            float l1 = lab1[0];
            float a1 = lab1[1];
            float b1 = lab1[2];

            float l2 = lab2[0];
            float a2 = lab2[1];
            float b2 = lab2[2];

            float deltaL = l2 - l1;
            float C1 = (float)Math.Sqrt(a1 * a1 + b1 * b1);
            float C2 = (float)Math.Sqrt(a2 * a2 + b2 * b2);
            float deltaC = C2 - C1;

            float deltaA = a2 - a1;
            float deltaB = b2 - b1;

            float deltaH2 = deltaA * deltaA + deltaB * deltaB - deltaC * deltaC;
            float deltaH = (float)Math.Sqrt(Math.Max(deltaH2, 0.0f));

            float h1 = (float)Math.Atan2(b1, a1);
            float h2 = (float)Math.Atan2(b2, a2);

            float deltaHPrime = h2 - h1;
            if (deltaHPrime < -Math.PI) deltaHPrime += 2.0f * (float)Math.PI;
            if (deltaHPrime > Math.PI) deltaHPrime -= 2.0f * (float)Math.PI;

            float LPrime = (l1 + l2) / 2.0f;
            float CPrime = (C1 + C2) / 2.0f;
            float HPrime = (h1 + h2) / 2.0f;

            float T = 1.0f - 0.17f * (float)Math.Cos(HPrime) + 0.24f * (float)Math.Cos(2.0f * HPrime) + 0.32f * (float)Math.Cos(3.0f * HPrime) - 0.20f * (float)Math.Cos(4.0f * HPrime);
            float SL = 1.0f + (0.015f * ((LPrime - 50.0f) * (LPrime - 50.0f))) / (float)Math.Sqrt(20.0f + ((LPrime - 50.0f) * (LPrime - 50.0f)));
            float SC = 1.0f + 0.045f * CPrime;
            float SH = 1.0f + 0.015f * CPrime * T;
            float RT = -2.0f * (float)Math.Sqrt(C1 * C2) * (float)Math.Sin(deltaHPrime / 2.0f);

            float dE = (float)Math.Sqrt(
                Math.Pow(deltaL / SL, 2.0f) +
                Math.Pow(deltaC / SC, 2.0f) +
                Math.Pow(deltaH / SH, 2.0f) +
                RT * (deltaC / SC) * (deltaH / SH)
            );

            return dE;
        }

        // Compare Colors
        public static float Compare(Color color1, Color color2)
        {
            float[] lab1 = RgbToLab(color1);
            float[] lab2 = RgbToLab(color2);

            return Ciede2000(lab1, lab2);
        }

        // Check if colors are close based on a threshold
        public static bool AreColorsClose(Color color1, Color color2, float threshold = 2.0f)
        {
            float deltaE = Compare(color1, color2);
            return deltaE <= threshold;
        }
    }



}
