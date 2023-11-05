using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Globalization;
using System.Text;

class TheWindow : Window
{

    string yourPath = @"C:\fraud_detection_2023\";

    System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

    // colors
    readonly SolidColorBrush font = new(Color.FromRgb(212, 212, 212));

    // layout
    Canvas canGlobal = new(), canVisual = new(), canCurrent = new(), canRuler = new(), canTest = new();

    int ys = 40; // y start point
    int xs = 15; // x start point menu

    double fs = 0; // feature size
    int height = 0;
    
    int features = 0, labelNum = 0;
    bool[] isOut, featureState;
    int[] trainLabels, labelLength;
    float[] trainData, minVal, maxVal;
    string[] featureNames, labelsNames;
    Brush[] br = new Brush[2];
    bool featureEngineering = false;
    int featureMode = -1;
    string timeInfo = "";
    Control cntrl;
    List<Rules> rules = new();

    [STAThread]
    static void Main() { new Application().Run(new TheWindow()); }
    private TheWindow() // constructor - set window
    {
        Content = canGlobal;
        Title = "Parallel Coordinates Distribution Count";
        Background = RGB(0, 0, 0);
        Width = 1600;
        Height = 800;

        MouseDown += Mouse_Down;
        MouseMove += Mouse_Move;
        MouseUp += Mouse_Up;
        SizeChanged += Window_SizeChanged;

        canGlobal.Children.Add(canVisual);
        canGlobal.Children.Add(canRuler);
        canGlobal.Children.Add(canCurrent);
        canGlobal.Children.Add(canTest);

        stopwatch = System.Diagnostics.Stopwatch.StartNew();
        DataInitCreditcard();
        ColorInit();
        stopwatch.Stop();
        timeInfo = ($"Data ready after {(stopwatch.Elapsed.TotalMilliseconds / 1000.0).ToString("F3")}s");

        return; // continue in Window_SizeChanged()...
    } // TheWindow end

    // core pcdc
    void DrawParallelCoordinates(string str = "")
    {
        DrawingContext dc = ContextHelpMod(false, ref canVisual);

        // draw console 
        Rect(ref dc, font, xs, ys + height + 30 + 25, 530, 35);
        Rect(ref dc, RGB(0, 0, 0), xs + 1, ys + height + 30 + 25 + 1, 530 - 2, 35 - 2);
        Text(ref dc, str, 11, font, xs + 5, ys + height + 55 + 4);
        
        BackgroundStuff(ref dc, features, trainData.Length);

        DrawGridInfo();

        DrawButtons();

        DrawDistributionCounts();

        dc.Close();

        return;

        void BackgroundStuff(ref DrawingContext dc, int features, int len)
        {
            // main info
            Text(ref dc, "Credit Card Fraud Detection Data: Size = " + (len / features).ToString() + ", Features = " + features.ToString()
               + ", Labels = " + labelNum.ToString() , 12, font, xs, ys - 32);

            // dataset info 
            byte cl = 0;
            Rect(ref dc, RGB(cl, cl, cl), 15, ys, (int)(features * fs), (int)height);

            Rect(ref dc, br[0], 430, (int)(ys - 34), (int)(400 * (labelLength[0] / (double)trainLabels.Length)), 15);
            Rect(ref dc, br[1], 430 + (int)(400 * (labelLength[0] / (double)trainLabels.Length)), (int)(ys - 34), (int)(400 * (labelLength[1] / (double)trainLabels.Length) + 1), 15);
            Text(ref dc, labelsNames[0] + " " + labelLength[0].ToString() + " (" + (labelLength[0] / (double)trainLabels.Length).ToString("F4") + "%)"
                + ", " + labelsNames[1] + " " + labelLength[1].ToString() + " (" + (labelLength[1] / (double)trainLabels.Length).ToString("F4") + "%)", 10, font, 435, (int)(ys - 34) + 2);

            // feature id's
            for (int i = 0; i < features; i++)
                Text(ref dc, featureNames[i].ToString(), 10, font, (int)(12 + (i + 1) * fs - TextWidth(featureNames[i].ToString(), 10)), ys - 14);
        }
        void DrawGridInfo()
        {
            // draw info lines and labels
            for (int j = 0; j < features; j++)
            {
                double x = 15 + (j + 1) * fs;
                dc.DrawRectangle(font, null, new Rect(x, ys, 1, height));
                var min = minVal[j];
                double range = maxVal[j] - min;

                for (int i = 0, cats = 10; i < cats + 1; i++) // accuracy lines 0, 20, 40...
                {
                    double yGrph = ys + height - i * (height / (double)cats);
                    double val = range / cats * i + min;
                    Line(ref dc, new Pen(font, 1.0), x - 2, yGrph, x, yGrph);
                    Text(ref dc, val.ToString("F2"), 7, font, (int)(x - TextWidth(val.ToString("F2"), 7) - 3), (int)yGrph - 3);
                }
            }
        }
        void DrawButtons()
        {
            byte b = 36;
            var buttonBG = RGB(b, b, b);
            DrawButton(ref dc, br[cntrl.ruleLabel], buttonBG, (cntrl.ruleLabel == 0 ? labelsNames[0] : labelsNames[1]) + " Rule " + (rules.Count + 1), 9, xs, ys + height + 25, 80, 20, 0);

            DrawButton(ref dc, font, buttonBG, "Test Rules", 9, xs, ys + height + 25, 80, 20, 1);
            DrawButton(ref dc, font, buttonBG, "Save Rules", 9, xs, ys + height + 25, 80, 20, 2);
            DrawButton(ref dc, font, buttonBG, "Load Rules", 9, xs, ys + height + 25, 80, 20, 3);
            DrawButton(ref dc, font, buttonBG, "Save Dataset", 9, xs, ys + height + 25, 80, 20, 4);
            DrawButton(ref dc, font, buttonBG, "Load Dataset", 9, xs, ys + height + 25, 80, 20, 5);
            DrawButton(ref dc, font, buttonBG, "Normalize Dataset", 9, xs, ys + height + 25, 80, 20, 6);
            DrawButton(ref dc, font, buttonBG, "Max Min Data", 9, xs, ys + height + 25, 80, 20, 7);

            void DrawButton(ref DrawingContext dc, Brush rgb, Brush buttonBG, string str, int strSz, int x, int y, int width, int height, int bttnNumber)
            {
                x += (10 + width) * bttnNumber;
                Rect(ref dc, rgb, x, y, width, height);
                Rect(ref dc, buttonBG, x + 1, y + 1, width - 2, height - 2); // Ruler
                Text(ref dc, str, strSz, font, x + 4, y + 5);
            }

            // background for each button 
            double gap = 0.5 * (fs - 4 * 12) + 1;
            for (int j = 0; j < 4; j++)
                for (int i = 0; i < featureState.Length; i++)
                    Rect(ref dc, buttonBG, (int)(i * fs + gap) + xs + j * 12 + 0, (int)(ys + height) + 7, 10, 10);

            // feature column shift and resolution change 
            for (int i = 0; i < featureState.Length; i++)
            {
                Text(ref dc, "▲", 9, font, (int)(i * fs + gap) + xs + 0 * 12 + 1, ys + height - 1 + 8);
                Text(ref dc, "+", 11, font, (int)(i * fs + gap) + xs + 1 * 12 + 1, ys + height - 2 + 8);
                Text(ref dc, "-", 13, font, (int)(i * fs + gap) + xs + 2 * 12 + 2, ys + height - 3 + 8);
                Text(ref dc, "▼", 9, font, (int)(i * fs + gap) + xs + 3 * 12 + 1, ys + height - 1 + 8);
            }

            if (featureEngineering) // feature engineering buttons
            {
                for (int i = 0; i < featureState.Length - 1; i++)
                {
                    Rect(ref dc, buttonBG, (int)(i * fs) + xs, (int)(ys - 13), 10, 10);
                    Rect(ref dc, RGB(168, 212, 255), (int)(i * fs) + xs + 1, (int)(ys - 12), 8, 8);
                }
                // background for each button
                Rect(ref dc, buttonBG, (int)(fs * (features - 1)) + xs + 3 + 0 * 12, (int)(ys - 13), 10, 10);
                Rect(ref dc, buttonBG, (int)(fs * (features - 1)) + xs + 3 + 1 * 12, (int)(ys - 13), 10, 10);
                Rect(ref dc, buttonBG, (int)(fs * (features - 1)) + xs + 3 + 2 * 12, (int)(ys - 13), 10, 10);
                Rect(ref dc, buttonBG, (int)(fs * (features - 1)) + xs + 3 + 3 * 12, (int)(ys - 13), 10, 10);

                Text(ref dc, "+", 11, font, (int)(fs * (features - 1)) + xs + 4 + 0 * 12, (int)(ys - 13) - 1);
                Text(ref dc, "-", 13, font, (int)(fs * (features - 1)) + xs + 5 + 1 * 12, (int)(ys - 13) - 2);
                Text(ref dc, "*", 9, font, (int)(fs * (features - 1)) + xs + 6 + 2 * 12, (int)(ys - 13) + 2);
                Text(ref dc, "=", 9, font, (int)(fs * (features - 1)) + xs + 4 + 3 * 12, (int)(ys - 13) - 0);
            }
            else // feature state on or off
            {
                Rect(ref dc, RGB(128, 128, 128), (int)(fs * features) + xs + 3, (int)(ys - 13), 10, 10);
                Rect(ref dc, font, (int)(fs * features) + 4 + xs, (int)(ys - 12), 8, 8);

                for (int i = 0; i < featureState.Length; i++)
                {
                    Rect(ref dc, RGB(128, 128, 128), (int)(i * fs) + xs, (int)(ys - 13), 10, 10);
                    Rect(ref dc, featureState[i] ? font : Brushes.Black, (int)(i * fs) + xs + 1, (int)(ys - 12), 8, 8);
                }
            }

        }
        void DrawDistributionCounts()
        {
            float[] maxMinusMin = new float[maxVal.Length];
            for (int i = 0; i < maxMinusMin.Length; i++) 
                maxMinusMin[i] = 1.0f / (maxVal[i] - minVal[i]);

            for (int labelId = 0; labelId < 2; labelId++) // for (int labelId = 1; labelId <= 0; labelId--)
            {
                // 1. init counts and maxCounts for each label
                int[] counts = new int[features * height],
                    maxCounts = new int[features];

                // 2. translate values into count height id of each data feature
                for (int i = 0; i < trainLabels.Length; i++)
                {
                    if (isOut[i] || trainLabels[i] != labelId) continue;

                    for (int j = 0, id = i * features; j < features; j++)
                        if (featureState[j])
                            if (trainData[id + j] <= maxVal[j] && trainData[id + j] >= minVal[j])
                                counts[(int)((trainData[id + j] - minVal[j]) * maxMinusMin[j] * (height - 1)) + j * height]++;
                }

                // 3. get max of each feature
                for (int j = 0; j < features; j++)
                    if (featureState[j])
                        for (int i = 0; i < height; i++)
                        {
                            int count = counts[i + j * height];
                            if (count > maxCounts[j]) maxCounts[j] = count;
                        }

                // 4. draw line with length and color intensity of the line based on counts
                for (int j = 0; j < features; j++)
                    if (featureState[j])
                        for (int i = 0; i < height; i++)
                        {
                            int count = counts[i + j * height];
                            if (count == 0) continue;
                            double probability = count / (double)maxCounts[j];
                            double pixelPos = ys + height - i;

                            // assign colors based on label ID
                            Color lineColor;
                            if (labelId == 0)
                            {
                                lineColor = Color.FromRgb((byte)(22 + 50 * probability), (byte)(31 + 40 * probability), (byte)(95 + 160 * probability));  // blue for label 0
                                Pen pen = new Pen(new SolidColorBrush(lineColor), 1.0);
                                Line(ref dc, pen,
                                    15 + (j + 0.5) * fs, pixelPos,
                                    15 + (j + 0.5) * fs - fs * 0.15 - probability * fs * 0.35, pixelPos);
                            }
                            else
                            {
                                lineColor = Color.FromRgb((byte)(65 + 190 * probability), (byte)(55 + 125 * probability), (byte)(0 * probability));  // gold for label 1
                                Pen pen = new Pen(new SolidColorBrush(lineColor), 1.0);
                                Line(ref dc, pen,
                                    15 + (j + 0.5) * fs, pixelPos,
                                    15 + (j + 0.5) * fs + fs * 0.15 + probability * fs * 0.35, pixelPos);
                            }
                        }
            }
        }

        double TextWidth(string text, int fontSize) =>
            new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new("TimesNewRoman"), fontSize, Brushes.Black).WidthIncludingTrailingWhitespace;
    }
    void DataInitCreditcard()
    {

        // directory path to save file
        string localFilePath = Path.Combine(yourPath, "creditcard_data.txt");

        // check if directory path exists and create if not
        if (!Directory.Exists(yourPath))
            Directory.CreateDirectory(yourPath);

        // check if data file exists else download
        if (!File.Exists(localFilePath))
        {
            string dataUrl = "https://datahub.io/machine-learning/creditcard/r/creditcard.csv";
            // Console.WriteLine("Data not found! Download from datahub.io"); // Console.WriteLine(dataUrl);

            byte[] data = new HttpClient().GetByteArrayAsync(dataUrl).Result;
            File.WriteAllBytes(localFilePath, data);
        }

        string attributes = "Time,V1,V2,V3,V4,V5,V6,V7,V8,V9,V10,V11,V12,V13,V14,V15,V16,V17,V18,V19,V20,V21,V22,V23,V24,V25,V26,V27,V28,Amount";
        string[] labelsNamesTemp = { "Transaction", "Fraud" };
        labelsNames = labelsNamesTemp;
        featureNames = attributes.Split(',').ToArray();
        features = featureNames.Length;
        labelNum = labelsNames.Length;

        string[] trainDataLines = File.ReadAllLinesAsync(localFilePath).Result.AsParallel().Skip(1).ToArray();
        // splitting data into 1d array 
        trainData = trainDataLines.AsParallel().SelectMany(line => line.Split(',').Take(features).Select(float.Parse)).ToArray();
        // parsing labels into 1d array
        trainLabels = trainDataLines.AsParallel().Select(line => (int)float.Parse(line.Split(',').Last().Trim('\''))).ToArray();

        SetMaxMin();
        SetWindow();
    }
    void SetMaxMin()
    {
        isOut = new bool[trainLabels.Length];

        labelLength = new int[labelsNames.Length];
        foreach (int label in trainLabels) labelLength[label]++;

        minVal = new float[features];
        maxVal = new float[features];
        for (int featureIndex = 0; featureIndex < features; featureIndex++)
        {
            float currentMin = float.MaxValue, currentMax = float.MinValue;
            for (int dataIndex = 0; dataIndex < trainLabels.Length; dataIndex++)
            {
                var value = trainData[featureIndex + features * dataIndex];
                if (value < currentMin) currentMin = value;
                if (value > currentMax) currentMax = value;
            }
            minVal[featureIndex] = currentMin;
            maxVal[featureIndex] = currentMax;
        }

        featureState = new bool[features];
        for (int i = 0; i < features; i++)
            featureState[i] = true;
    }

    // actions
    void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        canCurrent.Children.Clear();
        canTest.Children.Clear();
        canRuler.Children.Clear();
        SetWindow();

        DrawParallelCoordinates(timeInfo);
        timeInfo = "Changed windows size";
        cntrl.ruleFeature = -1;
    }
    void Mouse_Up(object sender, MouseEventArgs e)
    {
        if (cntrl.ruleFeature == -1) return;

        double y = e.GetPosition(this).Y;

        SetRule();

        void SetRule()
        {
            if (0 < cntrl.lastY - y) // min to max - down to up
                DrawRule((y < ys ? ys : y), cntrl.lastY);

            if (0 > cntrl.lastY - y)// max to min - up to down
                DrawRule(cntrl.lastY, (y - ys > height ? ys + height : y));

            void DrawRule(double start, double end)
            {
                double max_feature = maxVal[cntrl.ruleFeature], min_feature = minVal[cntrl.ruleFeature];
                double max_rule = max_feature + ((Math.Min(start, end) - ys) / height) * (min_feature - max_feature);
                double min_rule = max_feature + ((Math.Max(start, end) - ys) / height) * (min_feature - max_feature);

                rules.Add(new Rules { max = max_rule, min = min_rule, feature = cntrl.ruleFeature, label = cntrl.ruleLabel });

                DrawingContext dc = ContextHelpMod(true, ref canRuler);

                dc.DrawRectangle(br[cntrl.ruleLabel], null, new Rect(xs + (cntrl.ruleFeature + 1) * fs - 1, start, 5, end - start));
                dc.DrawRectangle(RGB(36, 36, 36), null, new Rect((cntrl.ruleFeature + 1) * fs + 0, start, 30, 32));

                Text(ref dc, "#" + rules.Count.ToString() + " (" + cntrl.ruleLabel.ToString()
                    + ")\n↑" + FormatNumber(max_rule) + "\n↓" + FormatNumber(min_rule)
                    , 8, font, (int)((cntrl.ruleFeature + 1) * fs) + 2, (int)start + 2);

                static string FormatNumber(double number)
                {
                    if (Math.Abs(number) >= 1e9)
                        return (number / 1e9).ToString("0.#") + "B";
                    else if (Math.Abs(number) >= 1e6)
                        return (number / 1e6).ToString("0.#") + "M";
                    else if (Math.Abs(number) >= 1e4)
                        return (number / 1e3).ToString("0") + "K";
                    else
                        return number.ToString("0.##");
                }
                cntrl.ruleFeature = -1;
                dc.Close();
                canCurrent.Children.Clear();
            }
            return;
        }
    }
    void Mouse_Move(object sender, MouseEventArgs e)
    {
        int y = (int)e.GetPosition(this).Y, x = (int)e.GetPosition(this).X;

        SelectRule();

        void SelectRule()
        {
            if (cntrl.ruleFeature != -1 && y != cntrl.lastPoint)
            {
                cntrl.lastPoint = y;
                DrawingContext dc = ContextHelpMod(false, ref canCurrent);
                for (int j = 0; j < features; j++)
                    if (x > (j + 1) * fs - 15 && x < (j + 1) * fs + 25 + 15)
                    {
                        if (0 < cntrl.lastY - y)
                        {
                            if (y < ys) y = ys;
                            dc.DrawRectangle(br[cntrl.ruleLabel], null, new Rect((j + 1) * fs + 8, y, 16, cntrl.lastY - y));
                        }
                        if (0 > cntrl.lastY - y)
                        {
                            if (y > ys + height) y = ys + height;
                            dc.DrawRectangle(br[cntrl.ruleLabel], null, new Rect((j + 1) * fs + 8, cntrl.lastY, 16, y - cntrl.lastY));
                        }
                        cntrl.ruleFeature = j;
                        break;
                    }
                dc.Close();
            }
        }
    }
    void Mouse_Down(object sender, MouseButtonEventArgs e)
    {
        int y = (int)e.GetPosition(this).Y, x = (int)e.GetPosition(this).X;

        // stack new feature
        if (featureEngineering)
        {
            for (int i = 0; i < featureState.Length - 1; i++)
                if (BoundsCheck2(x, y, (int)(i * fs) + xs, (int)(ys - 13), 10, 10))
                {
                    if (featureMode == -1)
                    {
                        featureNames[features - 1] = featureNames[i];
                        // feed new feature
                        maxVal[features - 1] = maxVal[i];
                        minVal[features - 1] = minVal[i];
                        for (int j = 0; j < trainLabels.Length; j++)
                            trainData[(j + 1) * features - 1] = trainData[j * features + i];

                        DrawParallelCoordinates("feature = " + i.ToString()); return;
                    }
                    if (featureMode == 0)
                    {
                        featureMode = -1;
                        for (int j = 0; j < trainLabels.Length; j++)
                            trainData[(j + 1) * features - 1] += trainData[j * features + i];
                        AddFeatureMode("+");
                        DrawParallelCoordinates("feature = " + i.ToString()); return;
                    }
                    if (featureMode == 1)
                    {
                        featureMode = -1;
                        for (int j = 0; j < trainLabels.Length; j++)
                            trainData[(j + 1) * features - 1] -= trainData[j * features + i];
                        AddFeatureMode("-");
                        DrawParallelCoordinates("feature = " + i.ToString()); return;
                    }
                    if (featureMode == 2)
                    {
                        featureMode = -1;
                        for (int j = 0; j < trainLabels.Length; j++)
                            trainData[(j + 1) * features - 1] *= trainData[j * features + i];
                        // feed new feature
                        AddFeatureMode("*");
                        DrawParallelCoordinates("feature = " + i.ToString()); return;
                    }
                    void AddFeatureMode(string str)
                    {
                        featureNames[features - 1] += str + featureNames[i];
                        // feed new feature
                        var tempMax = maxVal[features - 1];
                        var tempMin = minVal[features - 1];
                        for (int j = 0; j < trainLabels.Length; j++)
                        {
                            var val = trainData[(j + 1) * features - 1];
                            if (val > tempMax) tempMax = val;
                            if (val < tempMin) tempMin = val;
                        }
                        maxVal[features - 1] = tempMax;
                        minVal[features - 1] = tempMin;
                    }
                }
            // + 
            if (BoundsCheck2(x, y, (int)(fs * (features - 1)) + xs + 3 + 0 * 12, (int)(ys - 13), 10, 10))
            {
                featureMode = 0; DrawParallelCoordinates("featureMode = " + featureMode.ToString()); return;
            }
            // -
            if (BoundsCheck2(x, y, (int)(fs * (features - 1)) + xs + 3 + 1 * 12, (int)(ys - 13), 10, 10))
            {
                featureMode = 1; DrawParallelCoordinates("featureMode = " + featureMode.ToString()); return;
            }
            // *
            if (BoundsCheck2(x, y, (int)(fs * (features - 1)) + xs + 3 + 2 * 12, (int)(ys - 13), 10, 10))
            {
                featureMode = 2; DrawParallelCoordinates("featureMode = " + featureMode.ToString()); return;
            }
            // =
            if (BoundsCheck2(x, y, (int)(fs * (features - 1)) + xs + 3 + 3 * 12, (int)(ys - 13), 10, 10))
            {
                featureEngineering = false;
                featureMode = -1;
                DrawParallelCoordinates("Create new feature"); return;
            }
        }

        // add new feature
        if (BoundsCheck2(x, y, (int)(fs * features) + xs + 3, (int)(ys - 13), 10, 10))
        {
            // add feature
            if (!featureEngineering)
            {
                featureEngineering = true;
                // Extend maxVal, minVal, and featureNames arrays
                float[] updatedMaxVal = new float[features + 1];
                float[] updatedMinVal = new float[features + 1];
                string[] updatedFeatureNames = new string[features + 1];

                Array.Copy(maxVal, updatedMaxVal, features);
                Array.Copy(minVal, updatedMinVal, features);
                Array.Copy(featureNames, updatedFeatureNames, features);

                // Add the empty feature at the end
                updatedMaxVal[features] = 0.0f; // Set appropriate default value
                updatedMinVal[features] = 0.0f; // Set appropriate default value
                updatedFeatureNames[features] = ""; // Set appropriate name

                // Extend trainData array
                float[] updatedTrainData = new float[trainData.Length + trainLabels.Length * (features + 1)];
                Array.Copy(trainData, updatedTrainData, trainData.Length);

                // Iterate over trainLabels and add empty feature values
                for (int i = 0, c = 0; i < trainLabels.Length; i++)
                {
                    for (int j = 0; j < features; j++)
                        updatedTrainData[c++] = trainData[i * features + j];
                    // Assign a value for the empty feature
                    updatedTrainData[c++] = 0.0f; // Set appropriate default value
                }

                // Update variables with the new values
                maxVal = updatedMaxVal;
                minVal = updatedMinVal;
                maxVal[features] = 1;

                featureNames = updatedFeatureNames;
                trainData = updatedTrainData;
                features = features + 1;

                isOut = new bool[trainData.Length];
                featureState = new bool[features];
                for (int i = 0; i < featureState.Length; i++)
                    featureState[i] = true;

                SetWindow();
            }
            DrawParallelCoordinates("features = " + features.ToString()); return;
        }

        // check buttons and if clicked activate action
        if (SetMaxMinFeature()) return;

        if (CheckButtons()) return;

        ActivateRule();

        ClearRules();

        bool SetMaxMinFeature()
        {
            double gap = 0.5 * (fs - 4 * 12) + 1;
            for (int i = 0; i < featureState.Length; i++)
            {
                //  Rect(ref dc, buttonBG, (int)(i * fs + gap) + xs + j * 12 + 0, (int)(ys + height) + 7, 10, 10);
                int yHeight = ys + height + 7, xWidth = (int)(i * fs + gap) + xs;
                if (BoundsCheck(x, y, xWidth + 0 * 12, yHeight, 10)) // max ▲
                {
                    var maxMin = maxVal[i] - minVal[i];
                    maxVal[i] -= maxMin * 0.05f;
                    minVal[i] -= maxMin * 0.05f;
                    DrawParallelCoordinates(); return true;
                }
                if (BoundsCheck(x, y, xWidth + 1 * 12, yHeight, 10)) // max +
                {
                    maxVal[i] -= maxVal[i] * 0.05f;
                    minVal[i] -= minVal[i] * 0.05f;
                    DrawParallelCoordinates(); return true;
                }
                if (BoundsCheck(x, y, xWidth + 2 * 12, yHeight, 10)) // min -
                {
                    maxVal[i] += maxVal[i] * 0.05f;
                    minVal[i] += minVal[i] * 0.05f;
                    DrawParallelCoordinates(); return true;
                }
                if (BoundsCheck(x, y, xWidth + 3 * 12, yHeight, 10)) // min ▼
                {
                    var maxMin = maxVal[i] - minVal[i];
                    maxVal[i] += maxMin * 0.05f;
                    minVal[i] += maxMin * 0.05f;
                    DrawParallelCoordinates(); return true;
                }
            }
            return false;
        }
        bool CheckButtons()

        {
            double gap = 0;
            if (!featureEngineering)
                for (int i = 0; i < featureState.Length; i++)
                    if (BoundsCheck2(x, y, (int)(i * fs + gap) + xs, (int)(ys - 13), 10, 10))
                    {
                        featureState[i] = !featureState[i];
                        DrawParallelCoordinates("feature = " + i.ToString() + " is " + featureState[i].ToString()); return true;
                    }

            // button change label
            if (BoundsCheck2(x, y, xs + (10 + 80) * 0, ys + height + 25, 80, 20))
            {
                if (++cntrl.ruleLabel > labelNum - 1)
                    cntrl.ruleLabel = 0;

                DrawParallelCoordinates(
                    "Label changed to " + (cntrl.ruleLabel == 0 ? labelsNames[0] : labelsNames[1]));
                return true;
            }
            // button test rules
            if (BoundsCheck2(x, y, xs + (10 + 80) * 1, ys + height + 25, 80, 20))
            {
                // test rules
                int[] score = new int[2], all = new int[2];
                for (int i = 0; i < trainLabels.Length; i++)
                    foreach (var rule in rules) // check if rule is touched
                    {
                        var val = trainData[i * features + rule.feature];
                        if (val >= rule.min && val <= rule.max)
                        {
                            // add score 
                            if (rule.label == trainLabels[i]) score[rule.label]++;
                            all[trainLabels[i]]++;
                            isOut[i] = true;
                            break;
                        }
                    }
                DrawParallelCoordinates(
                    "Test accuracy = " + ((score[0] + score[1]) / (double)(trainLabels.Length)).ToString("F2") 
                    + " (" + (score[0] + score[1]) + "/" + trainLabels.Length + "), " +  "\n" + 
                    "Tansactions = " + ((score[0]) / (double)(all[0])).ToString("F2") + " (" + score[0] + "/" + all[0] + "), " +
                    "Frauds = " + ((score[1]) / (double)(all[1])).ToString("F2") + " (" + score[1] + "/" + all[1] + ")");
                return true;
            }
            // button save rules
            if (BoundsCheck2(x, y, xs + (10 + 80) * 2, ys + height + 25, 80, 20))
            {
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.InitialDirectory = yourPath;
                dlg.FileName = "myFirstRules.txt";
                if (dlg.ShowDialog() == true)
                    using (StreamWriter writer = new StreamWriter(dlg.FileName))
                        foreach (var rule in rules)
                            writer.WriteLine($"{rule.max},{rule.min},{rule.feature},{rule.label}");
                DrawParallelCoordinates("Save rules"); return true;
            }
            // button load rules
            if (BoundsCheck2(x, y, xs + (10 + 80) * 3, ys + height + 25, 80, 20))
            {
                Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
                ofd.InitialDirectory = yourPath;
                if (ofd.ShowDialog() == true)
                    using (StreamReader reader = new StreamReader(ofd.FileName))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            string[] parts = line.Split(',');
                            if (parts.Length == 4)
                            {
                                Rules rule = new Rules
                                {
                                    max = double.Parse(parts[0]),
                                    min = double.Parse(parts[1]),
                                    feature = int.Parse(parts[2]),
                                    label = int.Parse(parts[3])
                                };
                                rules.Add(rule);
                            }
                        }
                    }
                DrawParallelCoordinates("Load rules"); return true;
            }
            // button save dataset
            if (BoundsCheck2(x, y, xs + (10 + 80) * 4, ys + height + 25, 80, 20))
            {
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.InitialDirectory = yourPath;
                dlg.FileName = "myFirstDataset.txt";
                if (dlg.ShowDialog() == true)
                {
                    using (StreamWriter writer = new StreamWriter(dlg.FileName))
                    {
                        // Write the attribute names in the first line
                        List<string> selectedFeatureNames = new List<string>();
                        List<double> selectedMaxVal = new List<double>();
                        List<double> selectedMinVal = new List<double>();

                        for (int j = 0; j < features; j++)
                        {
                            if (featureState[j])
                            {
                                selectedFeatureNames.Add(featureNames[j]);
                                selectedMaxVal.Add(maxVal[j]);
                                selectedMinVal.Add(minVal[j]);
                            }
                        }

                        writer.WriteLine(string.Join(",", selectedFeatureNames));

                        // Write the maximum values in the second line
                        writer.WriteLine(string.Join(",", selectedMaxVal));

                        // Write the minimum values in the third line
                        writer.WriteLine(string.Join(",", selectedMinVal));

                        // Save data + label in the file
                        for (int i = 0; i < trainLabels.Length; i++)
                        {
                            if (isOut[i]) continue;

                            StringBuilder lineBuilder = new StringBuilder();

                            for (int j = 0, id = i * features; j < features; j++)
                            {
                                if (!featureState[j]) continue; // Skip if featureState[j] is false

                                double data = trainData[id + j];
                                if (data > maxVal[j])
                                    lineBuilder.Append(maxVal[j]);
                                else if (data < minVal[j])
                                    lineBuilder.Append(minVal[j]);
                                else
                                    lineBuilder.Append(data);

                                lineBuilder.Append(",");
                            }

                            // Append the label at the end
                            lineBuilder.Append(trainLabels[i]);

                            // Write the line to the file
                            writer.WriteLine(lineBuilder.ToString());
                        }
                        writer.Close();
                    }
                }
                DrawParallelCoordinates("Save dataset"); return true;
            }
            // button load dataset
            if (BoundsCheck2(x, y, xs + (10 + 80) * 5, ys + height + 25, 80, 20))
            {
                Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
                if (ofd.ShowDialog() == true)
                {
                    var lines = File.ReadAllLines(ofd.FileName);

                    // Read the attribute names from the first line
                    featureNames = lines[0].Split(',');

                    // Read the maximum values from the second line
                    maxVal = lines[1].Split(',').Select(float.Parse).ToArray();

                    // Read the minimum values from the third line
                    minVal = lines[2].Split(',').Select(float.Parse).ToArray();

                    int numAttributes = featureNames.Length;

                    // Initialize the data and label arrays based on the number of lines
                    trainData = new float[(lines.Length - 3) * numAttributes];
                    trainLabels = new int[lines.Length - 3];

                    // Read the data and labels
                    for (int i = 3; i < lines.Length; i++)
                    {
                        string[] parts = lines[i].Split(',');
                        if (parts.Length == numAttributes + 1)
                        {
                            for (int j = 0; j < numAttributes; j++)
                            {
                                trainData[(i - 3) * numAttributes + j] = float.Parse(parts[j]);
                            }
                            trainLabels[i - 3] = int.Parse(parts[numAttributes]);
                        }
                    }
                }

                isOut = new bool[trainLabels.Length];

                labelLength[0] = labelLength[1] = 0;
                foreach (int label in trainLabels) labelLength[label]++;
                features = maxVal.Length;
                featureState = new bool[features];
                for (int i = 0; i < features; i++)
                    featureState[i] = true;

                SetWindow();

                DrawParallelCoordinates("Load dataset"); return true;
            }
            // button NormalizeData
            if (BoundsCheck2(x, y, xs + (10 + 80) * 6, ys + height + 25, 80, 20))
            {

                // Normalize the trainData array
                NormalizeData(trainData, minVal, maxVal);
                DrawParallelCoordinates("NormalizeData");
                return true;
            }
            // button min-max   
            if (BoundsCheck2(x, y, xs + (10 + 80) * 7, ys + height + 25, 80, 20))
            {

                for (int featureIndex = 0; featureIndex < features; featureIndex++)
                {
                    minVal[featureIndex] = float.MaxValue;
                    maxVal[featureIndex] = float.MinValue;
                }
                // max min data
                for (int j = 0; j < features; j++)
                    for (int i = 0; i < trainLabels.Length; i++)
                    {
                        if (isOut[i]) continue;

                        //   if (trainData[i * features + j] <= maxVal[j] && trainData[i * features + j] >= minVal[j])
                        {
                            var value = trainData[i * features + j];
                            if (value > maxVal[j]) maxVal[j] = value;
                            if (value < minVal[j]) minVal[j] = value;
                        }
                    }


                DrawParallelCoordinates("max min data");
                return true;
            }

            static void NormalizeData(float[] trainData, float[] minValues, float[] maxValues)
            {
                for (int i = 0; i < trainData.Length / minValues.Length; i++)
                {
                    for (int j = 0; j < minValues.Length; j++)
                    {
                        float minValue = minValues[j];
                        float maxValue = maxValues[j];
                        float normalizedValue = (trainData[i * minValues.Length + j] - minValue) / (maxValue - minValue);
                        trainData[i * minValues.Length + j] = normalizedValue;
                    }
                }
                for (int i = 0; i < minValues.Length; i++) maxValues[i] = 1;
                for (int i = 0; i < minValues.Length; i++) minValues[i] = 0;

                return;

                for (int i = 0; i < trainData.Length / minValues.Length; i++)
                {
                    for (int j = 0; j < minValues.Length; j++)
                    {
                        float minValue = minValues[j];
                        float maxValue = maxValues[j];
                        float normalizedValue = (trainData[i * minValues.Length + j] - minValue) / (maxValue - minValue);
                        trainData[i * minValues.Length + j] = normalizedValue * 2 - 1;
                    }
                }
                for (int i = 0; i < minValues.Length; i++) maxValues[i] = 1;
                for (int i = 0; i < minValues.Length; i++) minValues[i] = -1;
            }

            return false;
        }
        void ActivateRule()
        {
            // left button was clicked
            if (e.ChangedButton == MouseButton.Left)
                // check inside pc bounds, then check if feature is clicked
                if (x > xs && x < xs + features * fs && y > ys && y < ys + height)
                {
                    cntrl.ruleFeature = -1;
                    for (int j = 0; j < features; j++)
                        if (x > (j + 1) * fs - 15 && x < (j + 1) * fs + 25 + 15)
                        {
                            cntrl.ruleFeature = j;
                            cntrl.lastY = y; return;
                        }
                }
        }
        void ClearRules()
        {
            if (e.ChangedButton == MouseButton.Right)
            {
                canRuler.Children.Clear();
                rules.Clear(); // removes all rules
                for (int i = 0; i < isOut.Length; i++)
                    isOut[i] = false;
            }
        }
        bool BoundsCheck(int x, int y, int width, int height, int padding) =>
            x >= width && x <= width + padding && y >= height && y <= height + padding;
        bool BoundsCheck2(int x, int y, int width, int height, int paddingW, int paddingH) =>
            x >= width && x <= width + paddingW && y >= height && y <= height + paddingH;
    }

    // helper
    void Rect(ref DrawingContext dc, Brush rgb, int x, int y, int width, int height) => dc.DrawRectangle(rgb, null, new Rect(x, y, width, height));
    void Line(ref DrawingContext dc, Pen pen, double xl, double yl, double xr, double yr) => dc.DrawLine(pen, new Point(xl, yl), new Point(xr, yr));
    void Text(ref DrawingContext dc, string str, int size, Brush rgb, int x, int y) =>
        dc.DrawText(new FormattedText(str, System.Globalization.CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new("TimesNewRoman"), size, rgb, VisualTreeHelper.GetDpi(this).PixelsPerDip), new Point(x, y));
    void SetWindow()
    {
        fs = (((Canvas)this.Content).RenderSize.Width - xs * 2 - 5) / features;
        height = (int)((Canvas)this.Content).RenderSize.Height - 100 - ys;
    }
    static DrawingContext ContextHelpMod(bool isInit, ref Canvas cTmp)
    {
        if (!isInit) cTmp.Children.Clear();
        DrawingVisualElement drawingVisual = new();
        cTmp.Children.Add(drawingVisual);
        return drawingVisual.drawingVisual.RenderOpen();
    }
    void ColorInit()
    {
        br[0] = RGB(60, 90, 215); // blue
        br[1] = RGB(213, 148, 0); // gold
        // br[2] = RGB(255, 0, 0); // red                
    }
    static Brush RGB(byte red, byte green, byte blue)
    {
        Brush brush = new SolidColorBrush(Color.FromRgb(red, green, blue));
        brush.Freeze();
        return brush;
    }

    struct Rules
    {
        public double max { get; set; }
        public double min { get; set; }
        public int feature { get; set; }
        public int label { get; set; }
    }
    struct Control
    {
        public int lastPoint { get; set; }
        public int lastY { get; set; }
        public int ruleFeature { get; set; }
        public int ruleLabel { get; set; }
    }
} // TheWindow end+

class DrawingVisualElement : FrameworkElement
{
    private readonly VisualCollection _children;
    public DrawingVisual drawingVisual;
    public DrawingVisualElement()
    {
        _children = new VisualCollection(this);
        drawingVisual = new DrawingVisual();
        _children.Add(drawingVisual);
    }
    public void ClearVisualElement() => _children.Clear();
    protected override int VisualChildrenCount => _children.Count;
    protected override Visual GetVisualChild(int index) => _children[index];
}
