﻿using System;
using System.Diagnostics;

namespace LibMeteoPL
{

    /*
    This interface must be implemented by client
    */
    public interface Utils
    {
        // calculate timestamp for given month, day and (fractional) hour; year must be provided by client as it doesn't appear on source img
        long getTimestamp(int m, int d, double hour);

        // handle failure in image processing
        void throwException(String description);
    }

    public class ModelUM
    {

        public const int WIDTH = 540;
        public const int HEIGHT = 660;

        Utils utils;

        // consts used across parser
        const int DIGIT_WIDTH = 5;
        const int DIGIT_HEIGHT = 8;
        const int DIGIT_SEPARATOR = 1;

        const int TIMESTAMP_DATE_ROW = 619;
        const int TIMESTAMP_TIME_ROW = 607;
        const int CHART_START_COL = 64;
        public const int CHART_WIDTH = 412; // standard source width
        const int PEAKS_NUMBER = 72; // to be checked

        const int TEMPERATURE_ROW_START = 58;
        const int TEMPERATURE_ROW_END = 133;
        const int TEMPERATURE_TEXT_COL = 39;
        public const int TEMPERATURE_PANEL_HEIGHT = TEMPERATURE_ROW_END- TEMPERATURE_ROW_START+1;

        const int PRESSURE_TEXT_COL = 33;
        const int PRESSURE_ROW_START = 230;
        const int PRESSURE_ROW_END = 305;
        const int PRESSURE_PANEL_HEIGHT = PRESSURE_ROW_END - PRESSURE_ROW_START + 1;

        const int WIND_TEXT_COL = 45;
        const int WIND_ROW_START = 316;
        const int WIND_ROW_END = 391;
        const int WIND_PANEL_HEIGHT = WIND_ROW_END - WIND_ROW_START + 1;

        const int DAYNIGHT_COL_START = 63;
        const int DAYNIGHT_ROW = 29;

        // color definitons
        public const int COLOR_BLACK = 0x000000;
        public const int COLOR_WHITE = 0xFFFFFF;
        public const int COLOR_TEMPERATURE_RED = 0xff0000;
        public const int COLOR_TEMPERATURE_MINMAX_RED1 = 0xf5d2d2;
        public const int COLOR_TEMPERATURE_MINMAX_RED2 = 0xfadcdc;
        public const int COLOR_DAY = 0xffffff;
        public const int COLOR_NIGHT = 0xe2e2e2;
        public const int COLOR_TEMPERATURE_NEGATIVE_BG_DAY = 0x87cefa;
        public const int COLOR_TEMPERATURE_NEGATIVE_BG_NIGHT = 0x82bee6;

        public const int COLOR_TEMPERATURE_PERC_BLUE = 0x0000ff;
        public const int COLOR_TEMPERATURE_PERC_MINMAX_BLUE = 0xb9dcff;

        public const int COLOR_PRESSURE_ALMOST_BLACK = 0x141414;

        public const int ERR = -1000000;

        public const int NOVALUE = 1048576; // 2^20 to fit int and double nicely

        // parsed data
        long timestamp = 0;



        // consider changing to ENUM, if all target languages support it
        // Section 1 - temperatures
        public const int TYPE_TEMPERATURE = 0;
        public const int TYPE_TEMPERATURE_MIN = 1;
        public const int TYPE_TEMPERATURE_MAX = 2;
        public const int TYPE_TEMPERATURE_PERCEPTIBLE = 3;
        public const int TYPE_TEMPERATURE_PERCEPTIBLE_MIN = 4;
        public const int TYPE_TEMPERATURE_PERCEPTIBLE_MAX = 5;
        public const int TYPE_SURFACE_TEMPERATURE_MIN = 6;
        public const int TYPE_SURFACE_TEMPERATURE_MAX = 7;
        public const int TYPE_DEWPOINT_TEMPERATURE = 8;
        // Section 2 - precipitation
        public const int TYPE_HUMIDITY = 9;
        public const int TYPE_RAIN_AVERAGE = 10;
        public const int TYPE_RAIN_MAX = 11;
        public const int TYPE_SNOW_AVERAGE = 12;
        public const int TYPE_SNOW_MAX = 13;
        public const int TYPE_PRECIPITIATION_ABOVE_SCALE = 14;
        public const int TYPE_CONVECTIVE_PRECIPITIATION = 15;
        // Section 3 - atmospheric pressure
        public const int TYPE_PRESSURE_HPA = 16;
        public const int TYPE_PRESSURE_MMHG = 17;
        // Section 4 - atmospheric pressure
        public const int TYPE_WIND_MS = 18;
        public const int TYPE_WIND_KMH = 19;
        public const int TYPE_WIND_GUST_MS = 20;
        public const int TYPE_WIND_GUST_KMH = 21;
        public const int TYPE_WIND_GUST_ABOVESCALE = 22;
        // Section X - extra data
        public const int TYPE_DAYNIGHT = 100;
        // Other sections - TBD


        // Section 1 - temperatures
        double[] temperature = new double[CHART_WIDTH];
        double[] temperatureMin = new double[CHART_WIDTH];
        double[] temperatureMax = new double[CHART_WIDTH];
        double[] temperaturePerc = new double[CHART_WIDTH];
        double[] temperaturePercMin = new double[CHART_WIDTH];
        double[] temperaturePercMax = new double[CHART_WIDTH];
        double[] temperatureSurfaceMin = new double[PEAKS_NUMBER];
        double[] temperatureSurfaceMax = new double[PEAKS_NUMBER];
        double[] temperatureDewPoint = new double[PEAKS_NUMBER];
        double temperature_precision;
        double temperature_row0;

        // Section 2 - precipitation
        double[] humidity = new double[CHART_WIDTH];
        double[] rainAvg = new double[PEAKS_NUMBER];
        double[] rainMax = new double[PEAKS_NUMBER];
        double[] snowAvg = new double[PEAKS_NUMBER];
        double[] snowMax = new double[PEAKS_NUMBER];
        bool[] percAboveScale = new bool[PEAKS_NUMBER];
        bool[] convectivePerc = new bool[PEAKS_NUMBER];

        // Section 3 - atmospheric pressure
        double[] pressurehPa = new double[CHART_WIDTH];
        double[] pressuremmHg = new double[CHART_WIDTH];
        double pressure_row0_hPa;
        double pressure_precision_hPa;

        // Section 4 - wind
        double[] windMs = new double[CHART_WIDTH];
        double[] windGutsMs = new double[CHART_WIDTH];
        double[] windKmh = new double[CHART_WIDTH];
        double[] windGutsKmh = new double[CHART_WIDTH];
        bool[] windGutsAboveScale = new bool[CHART_WIDTH];
        double wind_row0_ms;
        double wind_precision_ms;

        // Other sections - TBD
        double[] daynight = new double[CHART_WIDTH];


        // create object and parse img
        public ModelUM(int[] pixelsRGB, Utils utils)
        {
            this.utils = utils;

            if (pixelsRGB.Length != WIDTH * HEIGHT)
            {
                utils.throwException("Invalid size of input array");
            }

            // zero highest (alpha) byte
            for (int i = 0; i < pixelsRGB.Length; i++)
            {
                pixelsRGB[i] = pixelsRGB[i] & 0xffffff;
            }

            parsePixels(pixelsRGB);
        }

        public long getTimestamp(int px)
        {
            double part = px*1.0d / CHART_WIDTH;
            return timestamp + (int)(part * (CHART_WIDTH / 168.0) * 24 * 60 * 60 * 1000);
        }

        /*
        how many samples (data points) are present in given category
        */
        public int getSampleNumber(int type)
        {
            switch (type)
            {
                case TYPE_TEMPERATURE:
                case TYPE_TEMPERATURE_MAX:
                case TYPE_TEMPERATURE_MIN:
                case TYPE_TEMPERATURE_PERCEPTIBLE:
                case TYPE_TEMPERATURE_PERCEPTIBLE_MAX:
                case TYPE_TEMPERATURE_PERCEPTIBLE_MIN:
                case TYPE_PRESSURE_HPA:
                case TYPE_PRESSURE_MMHG:
                    return CHART_WIDTH;

            }

            utils.throwException("Type not yet implemented");
            return ERR;
        }

        /*
        get samples (data points) for given category
        */
        public double[] getSamples(int type)
        {
            switch (type)
            {
                case TYPE_TEMPERATURE:
                    return temperature;
                case TYPE_TEMPERATURE_MAX:
                    return temperatureMax;
                case TYPE_TEMPERATURE_MIN:
                    return temperatureMin;
                case TYPE_TEMPERATURE_PERCEPTIBLE:
                    return temperaturePerc;
                case TYPE_TEMPERATURE_PERCEPTIBLE_MAX:
                    return temperaturePercMax;
                case TYPE_TEMPERATURE_PERCEPTIBLE_MIN:
                    return temperaturePercMin;
                case TYPE_PRESSURE_HPA:
                    return pressurehPa;
                case TYPE_PRESSURE_MMHG:
                    return pressuremmHg;
                case TYPE_DAYNIGHT:
                    return daynight;
            }

            utils.throwException("Type not yet implemented");
            return null;
        }

        /*
        get value resolution for given category, will vary between different pics (due to different scale)
        */
        public double getPrecision(int type)
        {
            return getSamples(type).Length;
        }

        /*
        main parsing method
        */
        private void parsePixels(int[] pixelsRGB)
        {
            readTemperatureScale(pixelsRGB);
            readTemperatureValues(pixelsRGB);
            fixTemperatureData();

            readPressureScale(pixelsRGB);
            readPressureValues(pixelsRGB);
            fixPressureData();

            readWindScale(pixelsRGB);

            readOtherValues(pixelsRGB);

            readDate(pixelsRGB);
        }

        /*
        parse first image section
        */
        private void readTemperatureValues(int[] pixelsRGB)
        {

            for (int x = 0; x < CHART_WIDTH; x++)
            {

                temperature[x] = NOVALUE;
                temperatureMax[x] = -NOVALUE;
                temperatureMin[x] = NOVALUE;
                temperaturePerc[x] = NOVALUE;
                temperaturePercMax[x] = -NOVALUE;
                temperaturePercMin[x] = NOVALUE;

                int minT = NOVALUE;
                int maxT = -NOVALUE;
                int minP = NOVALUE;
                int maxP = -NOVALUE;
                for (int y = 0; y < TEMPERATURE_PANEL_HEIGHT; y++)
                {
                    int pixel = getPixel(pixelsRGB, WIDTH, CHART_START_COL + x, TEMPERATURE_ROW_START + y);
                    if (pixel == COLOR_TEMPERATURE_RED)
                    {
                        minT = Math.Min(minT, y);
                        maxT = Math.Max(maxT, y);
                    }

                    if (pixel == COLOR_TEMPERATURE_PERC_BLUE)
                    {
                        minP = Math.Min(minP, y);
                        maxP = Math.Max(maxP, y);
                    }
                    if (pixel == COLOR_TEMPERATURE_MINMAX_RED1 ||
                        pixel == COLOR_TEMPERATURE_MINMAX_RED2)
                    {
                        temperatureMin[x] = Math.Min(temperatureMin[x], temperature_row0 - temperature_precision * y);
                        temperatureMax[x] = Math.Max(temperatureMax[x], temperature_row0 - temperature_precision * y);
                    }
                    if (pixel == COLOR_TEMPERATURE_PERC_MINMAX_BLUE)
                    {
                        temperaturePercMin[x] = Math.Min(temperaturePercMin[x], temperature_row0 - temperature_precision * y);
                        temperaturePercMax[x] = Math.Max(temperaturePercMax[x], temperature_row0 - temperature_precision * y);
                    }

                }
                if (minT != NOVALUE)
                {
                    temperature[x] = temperature_row0 - temperature_precision * (maxT+ maxT + minT) / 3;
                }
                if (minP != NOVALUE)
                {
                    temperaturePerc[x] = temperature_row0 - temperature_precision * (maxP+ maxP + minP) / 3;
                }
            }
        }


        /*
        parse third image section
        */
        private void readPressureValues(int[] pixelsRGB)
        {

            for (int x = 0; x < CHART_WIDTH; x++)
            {
                bool nextx = false;

                pressurehPa[x] = NOVALUE;
                pressuremmHg[x] = NOVALUE;

                // intentionally skip columns with grid
                if (getPixel(pixelsRGB, WIDTH, CHART_START_COL + x, PRESSURE_ROW_START) != COLOR_BLACK)
                { 
                    for (int yTop = 0; yTop < PRESSURE_PANEL_HEIGHT && nextx == false; yTop++)
                    {
                        if (getPixel(pixelsRGB, WIDTH, CHART_START_COL + x, PRESSURE_ROW_START + yTop) == COLOR_BLACK ||
                            getPixel(pixelsRGB, WIDTH, CHART_START_COL + x, PRESSURE_ROW_START + yTop) == COLOR_PRESSURE_ALMOST_BLACK)
                        {

                            for (int yBottom = PRESSURE_PANEL_HEIGHT-1; yBottom >= 0 && nextx == false; yBottom--)
                            {
                                if (getPixel(pixelsRGB, WIDTH, CHART_START_COL + x, PRESSURE_ROW_START + yBottom) == COLOR_BLACK ||
                                    getPixel(pixelsRGB, WIDTH, CHART_START_COL + x, PRESSURE_ROW_START + yBottom) == COLOR_PRESSURE_ALMOST_BLACK)
                                {
                                    if (yBottom-yTop < 6)
                                    {
                                        pressurehPa[x] = pressure_row0_hPa - pressure_precision_hPa * yTop;
                                        pressuremmHg[x] = hPaTommHg(pressurehPa[x]);
                                    }
                                    nextx = true;
                                }
                            }

                        }
                    }
                }
            }

            /*
            for (int x = 0; x < CHART_WIDTH-2; x++)
            {
                // fix left edge of temperature array
                if (pressurehPa[x] != NOVALUE && pressurehPa[x+1] == NOVALUE && pressurehPa[x+2] != NOVALUE)
                {
                    pressurehPa[x+1] = (pressurehPa[x]+ pressurehPa[x+2])/2;
                    pressuremmHg[x+1] = hPaTommHg(pressurehPa[x+1]);
                }
            }
            */

        }

        private double hPaTommHg(double hPa)
        {
            return hPa * 0.75006156130;
        }


        /*
        parse first image section
        */
        private void readOtherValues(int[] pixelsRGB)
        {
            for (int x = 0; x < CHART_WIDTH; x++)
            {
                daynight[x] = getPixel(pixelsRGB, WIDTH, DAYNIGHT_COL_START + x, DAYNIGHT_ROW) == COLOR_WHITE ? 0f : 1f;
            }
        }


        /*
        fill data holes by averaging valid neighbour values
        */
        private void fixTemperatureData()
        {
            for (int x = 0; x < CHART_WIDTH; x++)
            {
                // fix left edge of temperature array
                if (temperature[x] != NOVALUE)
                {
                    if (x > 0)
                    {
                        for (int fixx = 0; fixx < x; fixx++)
                        {
                            temperature[fixx] = temperature[x];
                        }
                    }
                    break;
                }
            }

            for (int x = 0; x < CHART_WIDTH; x++)
            {
                // fix left edge of temperature perc array
                if (temperaturePerc[x] != NOVALUE)
                {
                    if (x > 0)
                    {
                        for (int fixx = 0; fixx < x; fixx++)
                        {
                            temperaturePerc[fixx] = temperaturePerc[x];
                        }
                    }
                    break;
                }
            }

            for (int x = 0; x < CHART_WIDTH; x++)
            {
                // fix left edge of temperature perceptible
                if (temperaturePerc[x] != NOVALUE)
                {
                    if (x > 0)
                    {
                        for (int fixx = 0; fixx < x; fixx++)
                        {
                            temperaturePerc[fixx] = temperaturePerc[x];
                        }
                    }
                    break;
                }

            }

            // fix right edge of temperature array
            for (int x = CHART_WIDTH - 1; x >= 0; x--)
            {
                if (temperature[x] != NOVALUE)
                {
                    if (x < CHART_WIDTH - 1)
                    {
                        for (int fixx = CHART_WIDTH - 1; fixx > x; fixx--)
                        {
                            temperature[fixx] = temperature[x];
                        }
                    }
                    break;
                }
            }

            // fix right edge of temperature perc array
            for (int x = CHART_WIDTH - 1; x >= 0; x--)
            {
                if (temperaturePerc[x] != NOVALUE)
                {
                    if (x < CHART_WIDTH - 1)
                    {
                        for (int fixx = CHART_WIDTH - 1; fixx > x; fixx--)
                        {
                            temperaturePerc[fixx] = temperaturePerc[x];
                        }
                    }
                    break;
                }
            }

            // fix right edge of temperature perceptible
            for (int x = CHART_WIDTH - 1; x >= 0; x--)
            {
                if (temperaturePerc[x] != NOVALUE)
                {
                    if (x < CHART_WIDTH - 1)
                    {
                        for (int fixx = CHART_WIDTH - 1; fixx > x; fixx--)
                        {
                            temperaturePerc[fixx] = temperaturePerc[x];
                        }
                    }
                    break;
                }
            }

            // fix holes of...
            for (int leftx = 0; leftx < CHART_WIDTH; leftx++)
            {
                //  ... temperature array
                if (temperature[leftx] == NOVALUE)
                {
                    int rightx = leftx + 1;
                    while (temperature[rightx] == NOVALUE)
                    {
                        rightx++;
                    }
                    double diff = (temperature[rightx] - temperature[leftx - 1]) / (rightx - leftx + 1);
                    for (int workx = leftx; workx < rightx; workx++)
                    {
                        temperature[workx] = temperature[leftx - 1] + diff * (workx - leftx + 1);
                    }
                }

                //  ... temperature oerc array
                if (temperaturePerc[leftx] == NOVALUE)
                {
                    int rightx = leftx + 1;
                    while (temperaturePerc[rightx] == NOVALUE)
                    {
                        rightx++;
                    }
                    double diff = (temperaturePerc[rightx] - temperaturePerc[leftx - 1]) / (rightx - leftx + 1);
                    for (int workx = leftx; workx < rightx; workx++)
                    {
                        temperaturePerc[workx] = temperaturePerc[leftx - 1] + diff * (workx - leftx + 1);
                    }
                }


            }

            // fix minmax temperatures by sticking to main
            for (int x = 0; x< CHART_WIDTH; x++)
            {
                if (temperaturePercMax[x] == -NOVALUE || temperaturePercMax[x] == NOVALUE)
                {
                    temperaturePercMax[x] = temperaturePerc[x];
                }
                if (temperaturePercMin[x] == -NOVALUE || temperaturePercMin[x] == NOVALUE)
                {
                    temperaturePercMin[x] = temperaturePerc[x];
                }
                if (temperatureMax[x] == -NOVALUE || temperatureMax[x] == NOVALUE)
                {
                    temperatureMax[x] = temperature[x];
                }
                if (temperatureMin[x] == -NOVALUE || temperatureMin[x] == NOVALUE)
                {
                    temperatureMin[x] = temperature[x];
                }
            }

            // TODO fix other data arrays here

        }

        /*
        fill data holes by averaging valid neighbour values
        */
        private void fixPressureData()
        {

            for (int x = 0; x < CHART_WIDTH; x++)
            {
                // fix left edge of pressure array
                if (pressurehPa[x] != NOVALUE)
                {
                    if (x > 0)
                    {
                        for (int fixx = 0; fixx < x; fixx++)
                        {
                            pressurehPa[fixx] = pressurehPa[x];
                            pressuremmHg[fixx] = hPaTommHg(pressurehPa[x]);
                        }
                    }
                    break;
                }
            }

            // fix right edge of pressure array
            for (int x = CHART_WIDTH - 1; x >= 0; x--)
            {
                if (pressurehPa[x] != NOVALUE)
                {
                    if (x < CHART_WIDTH - 1)
                    {
                        for (int fixx = CHART_WIDTH - 1; fixx > x; fixx--)
                        {
                            pressurehPa[fixx] = pressurehPa[x];
                            pressuremmHg[fixx] = hPaTommHg(pressurehPa[x]);
                        }
                    }
                    break;
                }
            }

            // fix holes of...
            for (int leftx = 0; leftx < CHART_WIDTH; leftx++)
            {
                //  ... pressure array
                if (pressurehPa[leftx] == NOVALUE)
                {
                    int rightx = leftx + 1;
                    while (pressurehPa[rightx] == NOVALUE)
                    {
                        rightx++;
                    }
                    double diff = (pressurehPa[rightx] - pressurehPa[leftx - 1]) / (rightx - leftx + 1);
                    for (int workx = leftx; workx < rightx; workx++)
                    {
                        pressurehPa[workx] = pressurehPa[leftx - 1] + diff * (workx - leftx + 1);
                        pressuremmHg[workx] = hPaTommHg(pressurehPa[workx]);
                    }
                }
            }
        }


        /*
        read scale of first image section
        */
        private void readTemperatureScale(int[] pixelsRGB)
        {
            int start = 0;
            int temp_start = 0;
            int end = 0;
            int temp_end = 0;

            for (int i = TEMPERATURE_ROW_START - DIGIT_HEIGHT; i < TEMPERATURE_ROW_END; i++)
            {
                int x1 = readUpTo3digit(TEMPERATURE_TEXT_COL, i, pixelsRGB, COLOR_BLACK);
                if (x1 != ERR)
                {
                    start = i + 4;
                    temp_start = x1;
                    break;
                }
            }

            for (int i = TEMPERATURE_ROW_END; i > TEMPERATURE_ROW_START - DIGIT_HEIGHT; i--)
            {
                int x2 = readUpTo3digit(TEMPERATURE_TEXT_COL, i, pixelsRGB, COLOR_BLACK);
                if (x2 != ERR)
                {
                    end = i + 4;
                    temp_end = x2;
                    break;
                }
            }

            double tempSpan = temp_start * temp_end < 0 ? Math.Abs(temp_start + temp_end) : Math.Abs(temp_start - temp_end);

            temperature_precision = tempSpan / Math.Abs(start - end);
            temperature_row0 = temp_start + temperature_precision * (start - TEMPERATURE_ROW_START);

            int x = 0;
        }


        /*
        read scale of first image section
        */
        private void readPressureScale(int[] pixelsRGB)
        {
            int start = 0;
            int pres_start = 0;
            int end = 0;
            int pres_end = 0;

            for (int i = PRESSURE_ROW_START - DIGIT_HEIGHT; i < PRESSURE_ROW_END; i++)
            {
                int x1 = read3or4digit(PRESSURE_TEXT_COL, i, pixelsRGB, COLOR_BLACK);
                if (x1 != ERR)
                {
                    start = i + 4;
                    pres_start = x1;
                    break;
                }
            }

            for (int i = PRESSURE_ROW_END; i > PRESSURE_ROW_START - DIGIT_HEIGHT; i--)
            {
                int x2 = read3or4digit(PRESSURE_TEXT_COL, i, pixelsRGB, COLOR_BLACK);
                if (x2 != ERR)
                {
                    end = i + 4;
                    pres_end = x2;
                    break;
                }
            }

            double presSpan = pres_start * pres_end < 0 ? Math.Abs(pres_start + pres_end) : Math.Abs(pres_start - pres_end);

            pressure_precision_hPa = presSpan / Math.Abs(start - end);
            pressure_row0_hPa = pres_start + temperature_precision * (start - PRESSURE_ROW_START);

            int x = 0;
        }



        /*
        read scale of first image section
        */
        private void readWindScale(int[] pixelsRGB)
        {
            int start = 0;
            int wind_start = 0;
            int end = 0;
            int wind_end = 0;

            for (int i = WIND_ROW_START - DIGIT_HEIGHT; i < WIND_ROW_END; i++)
            {
                int x1 = readUpTo2digit(WIND_TEXT_COL, i, pixelsRGB, COLOR_BLACK);
                if (x1 != ERR)
                {
                    start = i + 4;
                    wind_start = x1;
                    break;
                }
            }

            for (int i = WIND_ROW_END; i > WIND_ROW_START - DIGIT_HEIGHT; i--)
            {
                int x2 = readUpTo2digit(WIND_TEXT_COL, i, pixelsRGB, COLOR_BLACK);
                if (x2 != ERR)
                {
                    end = i + 4;
                    wind_end = x2;
                    break;
                }
            }

            double windSpan = wind_start * wind_end < 0 ? Math.Abs(wind_start + wind_end) : Math.Abs(wind_start - wind_end);

            wind_precision_ms = windSpan / Math.Abs(start - end);
            wind_row0_ms = wind_start + wind_precision_ms * (start - WIND_ROW_START);

        }

        private int readUpTo3digit(int x, int y, int[] pixelsRGB, int pixelColor)
        {
            int i = readXdigit(x, y, 3, pixelsRGB, pixelColor);
            if (i != ERR)
            {
                return i;
            }
            x += DIGIT_WIDTH + DIGIT_SEPARATOR;

            i = readXdigit(x, y, 2, pixelsRGB, pixelColor);
            if (i != ERR)
            {
                return i;
            }
            x += DIGIT_WIDTH + DIGIT_SEPARATOR;

            i = readXdigit(x, y, 1, pixelsRGB, pixelColor);
            if (i != ERR)
            {
                return i;
            }

            return ERR;
        }


        private int readUpTo2digit(int x, int y, int[] pixelsRGB, int pixelColor)
        {
            int i = readXdigit(x, y, 2, pixelsRGB, pixelColor);
            if (i != ERR)
            {
                return i;
            }
            x += DIGIT_WIDTH + DIGIT_SEPARATOR;

            i = readXdigit(x, y, 1, pixelsRGB, pixelColor);
            if (i != ERR)
            {
                return i;
            }

            return ERR;
        }

        private int read3or4digit(int x, int y, int[] pixelsRGB, int pixelColor)
        {
            int i = readXdigit(x, y, 4, pixelsRGB, pixelColor);
            if (i != ERR)
            {
                return i;
            }
            x += DIGIT_WIDTH + DIGIT_SEPARATOR;

            i = readXdigit(x, y, 3, pixelsRGB, pixelColor);
            if (i != ERR)
            {
                return i;
            }

            return ERR;
        }

        private int readXdigit(int x, int y, int digits, int[] pixelsRGB, int pixelColor)
        {
            bool negative = isMinus(pixelsRGB, x, y, pixelColor);
            int ret = 0;

            for (int i=0; i<digits; i++)
            {
                int d = readDigit(pixelsRGB, x+(i*(DIGIT_WIDTH+DIGIT_SEPARATOR)), y, pixelColor);

                if (ERR == d)
                {
                    return ERR;
                }

                if (d >= 0)
                {
                    ret = 10*ret + d;
                }

            }

            if (negative)
            {
                ret *= -1;
            }
            return ret;
        }

        private void readDate(int[] pixelsRGB)
        {

            int day = 0;
            int month = 0;
            double hour = 0;

            for (int x = CHART_START_COL; x < CHART_START_COL + CHART_WIDTH; x++)
            {
                if (
                    isDigit(pixelsRGB, x, TIMESTAMP_DATE_ROW, COLOR_BLACK) &&
                    isDigit(pixelsRGB, x + 7, TIMESTAMP_DATE_ROW, COLOR_BLACK) &&
                    isDigit(pixelsRGB, x + 17, TIMESTAMP_DATE_ROW, COLOR_BLACK) &&
                    isDigit(pixelsRGB, x + 24, TIMESTAMP_DATE_ROW, COLOR_BLACK)
                    )
                {
                    day = readDigit(pixelsRGB, x + 0, TIMESTAMP_DATE_ROW, COLOR_BLACK) * 10 +
                        readDigit(pixelsRGB, x + 7, TIMESTAMP_DATE_ROW, COLOR_BLACK);
                    month = readDigit(pixelsRGB, x + 17, TIMESTAMP_DATE_ROW, COLOR_BLACK) * 10 +
                        readDigit(pixelsRGB, x + 24, TIMESTAMP_DATE_ROW, COLOR_BLACK);

                    break;

                }
            }

            for (int x = CHART_START_COL; x < CHART_START_COL + CHART_WIDTH; x++)
            {
                if (
                    isDigit(pixelsRGB, x, TIMESTAMP_TIME_ROW, COLOR_BLACK) &&
                    isDigit(pixelsRGB, x + 7, TIMESTAMP_TIME_ROW, COLOR_BLACK) 
                    )
                {
                    hour = readDigit(pixelsRGB, x , TIMESTAMP_TIME_ROW, COLOR_BLACK) * 10 +
                        readDigit(pixelsRGB, x +7, TIMESTAMP_TIME_ROW, COLOR_BLACK);

                    int secPerPixel = 60 * 60 * 24 * 3 / CHART_WIDTH;

                    int pxs = (x - CHART_START_COL + 7);

                    hour = hour - ( secPerPixel * pxs / 3600 );

                    timestamp = utils.getTimestamp(month, day, hour);

                    break;

                }
            }


        }

        private bool isDigit(int[] pixels, int x, int y, int pixelColor)
        {
            return
                 (digitMatch(pixels, x, y, pixelColor, 0)) ||
                 (digitMatch(pixels, x, y, pixelColor, 1)) ||
                 (digitMatch(pixels, x, y, pixelColor, 2)) ||
                 (digitMatch(pixels, x, y, pixelColor, 8)) || // 8 must come before 3
                 (digitMatch(pixels, x, y, pixelColor, 4)) ||
                 (digitMatch(pixels, x, y, pixelColor, 5)) ||
                 (digitMatch(pixels, x, y, pixelColor, 6)) ||
                 (digitMatch(pixels, x, y, pixelColor, 7)) ||
                 (digitMatch(pixels, x, y, pixelColor, 3)) || // 8 must come before 3
                 (digitMatch(pixels, x, y, pixelColor, 9));
        }

        private bool isMinus(int[] pixels, int x, int y, int pixelColor)
        {
            return digitMatch(pixels, x, y, pixelColor, -1);
        }



        private int readDigit(int[] pixels, int x, int y, int pixelColor)
        {
            if (digitMatch(pixels, x, y, pixelColor, 0))
            {
                return 0;
            }
            if (digitMatch(pixels, x, y, pixelColor, 1))
            {
                return 1;
            }
            if (digitMatch(pixels, x, y, pixelColor, 2))
            {
                return 2;
            }
            if (digitMatch(pixels, x, y, pixelColor, 8)) // 8 must be before 3
            {
                return 8;
            }
            if (digitMatch(pixels, x, y, pixelColor, 4))
            {
                return 4;
            }
            if (digitMatch(pixels, x, y, pixelColor, 5))
            {
                return 5;
            }
            if (digitMatch(pixels, x, y, pixelColor, 6))
            {
                return 6;
            }
            if (digitMatch(pixels, x, y, pixelColor, 7))
            {
                return 7;
            }
            if (digitMatch(pixels, x, y, pixelColor, 3)) // 8 must be before 3
            {
                return 3;
            }
            if (digitMatch(pixels, x, y, pixelColor, 9))
            {
                return 9;
            }
            if (digitMatch(pixels, x, y, pixelColor, -1))
            {
                return -1;
            }
            return ERR;
        }

        private bool digitMatch(int[] pixels, int x, int y, int pixelColor, int digit)
        {
            switch(digit){
                case -1: return digitMatch(pixels, x, y, pixelColor, digitsMINUS_8, 8) ;
                case 0: return digitMatch(pixels, x, y, pixelColor, digits0_8, 8) || digitMatch(pixels, x, y, pixelColor, digits0_9, 9);
                case 1: return digitMatch(pixels, x, y, pixelColor, digits1_8, 8) || digitMatch(pixels, x, y, pixelColor, digits1_9, 9);
                case 2: return digitMatch(pixels, x, y, pixelColor, digits2_8, 8) || digitMatch(pixels, x, y, pixelColor, digits2_9, 9);
                case 3: return digitMatch(pixels, x, y, pixelColor, digits3_8, 8) || digitMatch(pixels, x, y, pixelColor, digits3_9, 9);
                case 4: return digitMatch(pixels, x, y, pixelColor, digits4_8, 8) || digitMatch(pixels, x, y, pixelColor, digits4_9, 9);
                case 5: return digitMatch(pixels, x, y, pixelColor, digits5_8, 8) || digitMatch(pixels, x, y, pixelColor, digits5_9, 9);
                case 6: return digitMatch(pixels, x, y, pixelColor, digits6_8, 8) || digitMatch(pixels, x, y, pixelColor, digits6_9, 9);
                case 7: return digitMatch(pixels, x, y, pixelColor, digits7_8, 8) || digitMatch(pixels, x, y, pixelColor, digits7_9, 9);
                case 8: return digitMatch(pixels, x, y, pixelColor, digits8_8, 8) || digitMatch(pixels, x, y, pixelColor, digits8_9, 9);
                case 9: return digitMatch(pixels, x, y, pixelColor, digits9_8, 8) || digitMatch(pixels, x, y, pixelColor, digits9_9, 9);
            }
            return false;
        }

        private bool digitMatch(int[] pixels, int x, int y, int pixelColor, int[] digitMatrix, int digitheight)
        {
            for (int xiter = 0; xiter < DIGIT_WIDTH; xiter++)
            {
                for (int yiter = 0; yiter < digitheight; yiter++)
                {
                    int px = getPixel(pixels, WIDTH, x + xiter, y + yiter);
                    if ( (1 == getPixel(digitMatrix, DIGIT_WIDTH, xiter, yiter) && px != pixelColor) ||
                         (0 == getPixel(digitMatrix, DIGIT_WIDTH, xiter, yiter) && px == pixelColor) 
                    )
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private int getPixel(int[] pixels, int width, int x, int y)
        {
            return pixels[x + y * width];
        }

        int[] digits0_8 = new int[]{
            0,1,1,1,0,
            1,0,0,0,1,
            1,0,0,0,1,
            1,0,0,0,1,
            1,0,0,0,1,
            1,0,0,0,1,
            1,0,0,0,1,
            0,1,1,1,0,
            };
        int[] digits0_9 = new int[]{
            0,1,1,1,0,
            1,0,0,0,1,
            1,0,0,0,1,
            1,0,0,0,1,
            1,0,0,0,1,
            1,0,0,0,1,
            1,0,0,0,1,
            1,0,0,0,1,
            0,1,1,1,0,
            };


        int[] digits1_8 = new int[]
            { // 1
            0,0,1,0,0,
            0,1,1,0,0,
            0,0,1,0,0,
            0,0,1,0,0,
            0,0,1,0,0,
            0,0,1,0,0,
            0,0,1,0,0,
            0,0,1,0,0,
            };
        int[] digits1_9 = new int[]
            { // 1
            0,0,1,0,0,
            1,1,1,0,0,
            0,0,1,0,0,
            0,0,1,0,0,
            0,0,1,0,0,
            0,0,1,0,0,
            0,0,1,0,0,
            0,0,1,0,0,
            0,0,1,0,0,
            };

        int[] digits2_8 = new int[]
            { // 2
            0,1,1,1,0,
            1,0,0,0,1,
            0,0,0,0,1,
            0,0,0,0,1,
            0,0,1,1,0,
            0,1,0,0,0,
            1,0,0,0,0,
            1,1,1,1,1,
            };
        int[] digits2_9 = new int[]
            { // 2
            0,1,1,1,0,
            1,0,0,0,1,
            0,0,0,0,1,
            0,0,0,1,0,
            0,0,1,0,0,
            0,1,0,0,0,
            1,0,0,0,0,
            1,0,0,0,0,
            1,1,1,1,1,
            };

        int[] digits8_8 = new int[]
            { // 8
            0,1,1,1,0,
            1,0,0,0,1,
            1,0,0,0,1,
            0,1,1,1,0,
            1,0,0,0,1,
            1,0,0,0,1,
            1,0,0,0,1,
            0,1,1,1,0,
            };
        int[] digits8_9 = new int[]
            { // 8
            0,1,1,1,0,
            1,0,0,0,1,
            1,0,0,0,1,
            1,0,0,0,1,
            0,1,1,1,0,
            1,0,0,0,1,
            1,0,0,0,1,
            1,0,0,0,1,
            0,1,1,1,0,
            };

        int[] digits4_8 = new int[]
            { // 4
            0,0,0,1,0,
            0,0,1,1,0,
            0,1,0,1,0,
            0,1,0,1,0,
            1,0,0,1,0,
            1,1,1,1,1,
            0,0,0,1,0,
            0,0,0,1,0,
            };
        int[] digits4_9 = new int[]
            { // 4
            0,0,0,1,0,
            0,0,1,1,0,
            0,1,0,1,0,
            0,1,0,1,0,
            1,0,0,1,0,
            0,0,0,1,0,
            1,1,1,1,1,
            0,0,0,1,0,
            0,0,0,1,0
            };

        int[] digits5_8 = new int[]
            { // 5
            0,1,1,1,1,
            0,1,0,0,0,
            0,1,0,0,0,
            0,1,1,1,0,
            0,0,0,0,1,
            0,0,0,0,1,
            1,0,0,0,1,
            0,1,1,1,0,
            };
        int[] digits5_9 = new int[]
            { // 5
            1,1,1,1,1,
            1,0,0,0,0,
            1,0,0,0,0,
            1,1,1,1,0,
            0,0,0,0,1,
            0,0,0,0,1,
            1,0,0,0,1,
            1,0,0,0,1,
            0,1,1,1,0,
            };

        int[] digits6_8 = new int[]
            { // 6
            0,1,1,1,0,
            1,0,0,0,1,
            1,0,0,0,0,
            1,1,1,1,0,
            1,0,0,0,1,
            1,0,0,0,1,
            1,0,0,0,1,
            0,1,1,1,0,
            };
        int[] digits6_9 = new int[]
            { // 6
            0,1,1,1,0,
            1,0,0,0,1,
            1,0,0,0,0,
            1,0,0,0,0,
            1,1,1,1,0,
            1,0,0,0,1,
            1,0,0,0,1,
            1,0,0,0,1,
            0,1,1,1,0,
            };

        int[] digits7_8 = new int[]
            { // 7
            1,1,1,1,1,
            0,0,0,0,1,
            0,0,0,1,0,
            0,0,1,0,0,
            0,0,1,0,0,
            0,1,0,0,0,
            0,1,0,0,0,
            0,1,0,0,0,
            };
        int[] digits7_9 = new int[]
            { // 7
            1,1,1,1,1,
            0,0,0,0,1,
            0,0,0,1,0,
            0,0,0,1,0,
            0,0,1,0,0,
            0,0,1,0,0,
            0,0,1,0,0,
            0,1,0,0,0,
            0,1,0,0,0,
            };

        int[] digits3_8 = new int[]
            { // 3
            0,1,1,1,0,
            1,0,0,0,1,
            0,0,0,0,1,
            0,0,1,1,0,
            0,0,0,0,1,
            0,0,0,0,1,
            1,0,0,0,1,
            0,1,1,1,0,
            };
        int[] digits3_9 = new int[]
            { // 3
            0,1,1,1,0,
            1,0,0,0,1,
            0,0,0,0,1,
            0,0,1,1,0,
            0,0,0,0,1,
            0,0,0,0,1,
            1,0,0,0,1,
            1,0,0,0,1,
            0,1,1,1,0,
            };

        int[] digits9_8 = new int[]
            { // 9
            0,1,1,1,0,
            1,0,0,0,1,
            1,0,0,0,1,
            1,0,0,0,1,
            0,1,1,1,1,
            0,0,0,0,1,
            1,0,0,0,1,
            0,1,1,1,0,
            };
        int[] digits9_9 = new int[]
            { // 9
            0,1,1,1,0, //  ***
            1,0,0,0,1, // *   *
            1,0,0,0,1, // *   *
            1,0,0,0,1, // *   *
            0,1,1,1,1, //  ****
            0,0,0,0,1, //     *
            0,0,0,0,1, //     *
            1,0,0,0,1, // *   *
            0,1,1,1,0, //  ***
            };

        int[] digitsMINUS_8 = new int[]
            { 
            0,0,0,0,0,
            0,0,0,0,0,
            0,0,0,0,0,
            0,0,0,0,0,
            0,0,1,1,1,
            0,0,0,0,0,
            0,0,0,0,0,
            0,0,0,0,0,
            };

    }
}
