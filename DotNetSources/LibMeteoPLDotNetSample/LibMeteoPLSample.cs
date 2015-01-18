using System;
using System.Drawing;
using LibMeteoPL;
using System.IO;

namespace LibMeteoPL
{
    class LibMeteoPLSample : Utils
    {
        static void Main(string[] args)
        {
            new LibMeteoPLSample().init();

        }

        public void init()
        {

            int[] pixels = new int[ModelUM.WIDTH * ModelUM.HEIGHT];

            Bitmap img = new Bitmap("../../../../samples/mgram2.png");

            for (int x = 0; x < img.Width; x++)
            {
                for (int y = 0; y < img.Height; y++)
                {
                    Color pixel = img.GetPixel(x, y);
                    pixels[x + y * ModelUM.WIDTH] = (pixel.ToArgb() & 0x00ffffff);
                }
            }

            ModelUM model = new ModelUM(pixels, this);

            double[] temp = model.getSamples(ModelUM.TYPE_TEMPERATURE);
            double[] temp_perc = model.getSamples(ModelUM.TYPE_TEMPERATURE_PERCEPTIBLE);
            double[] pressure = model.getSamples(ModelUM.TYPE_PRESSURE_HPA);

            StreamWriter file2 = new StreamWriter(@"../../../../samples/mgram1dotnetoutput.txt");
            for (int i = 0; i < temp.Length; i++)
            {
                file2.Write(temp[i].ToString("F"));
                file2.Write(";");
            }
            file2.WriteLine();
            for (int i = 0; i < temp.Length; i++)
            {
                file2.Write(temp_perc[i].ToString("F"));
                file2.Write(";");
            }
            file2.WriteLine();
            for (int i = 0; i < temp.Length; i++)
            {
                file2.Write(pressure[i].ToString("F"));
                file2.Write(";");
            }
            file2.WriteLine();
            file2.Close();

        }

        public long getTimestamp(int m, int d, double hourD)
        {
            int year = DateTime.Now.Year; // WARNING! Not always valid, used for simplification
            int hour = (int)Math.Floor(hourD);
            int minute = (int)((hourD - hour) * 60);
            var timeSpan = new DateTime(year, m, d, hour, minute, 0) - new DateTime(1970, 1, 1, 0, 0, 0);
            return (long)timeSpan.TotalSeconds;

        }

        public void throwException(String str)
        {
            throw new Exception(str);
        }
    }
}
