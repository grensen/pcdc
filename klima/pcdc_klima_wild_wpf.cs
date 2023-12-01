using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Net.Http;
using System.Globalization;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Reflection.Emit;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Xml.Linq;
using System.Security.Cryptography;

class TheWindow : Window
{
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
    string[] featureNames;
    Brush[] br = new Brush[3];
    bool featureEngineering = false;

    Control cntrl;
    List<Rules> rules = new();
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
    [STAThread]
    static void Main() { new Application().Run(new TheWindow()); }
    TheWindow() // constructor - set window
    {
        Content = canGlobal;
        Title = "Parallel Coordinates Distribution Count";
        Background = RGB(0, 0, 0);
        Width = 1600;
        Height = 700 + 100;

        MouseDown += Mouse_Down;
        MouseMove += Mouse_Move;
        MouseUp += Mouse_Up;
        SizeChanged += Window_SizeChanged;

        canGlobal.Children.Add(canVisual);
        canGlobal.Children.Add(canRuler);
        canGlobal.Children.Add(canCurrent);
        canGlobal.Children.Add(canTest);

        DataInitForex1();
        ColorInit();
        return; // continue in Window_SizeChanged()...
    } // TheWindow end
    void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        canCurrent.Children.Clear();
        canTest.Children.Clear();
        canRuler.Children.Clear();
        SetWindow();
        DrawParallelCoordinates();
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

                dc.DrawRectangle(br[cntrl.ruleLabel], null, new Rect(xs + (cntrl.ruleFeature + 1) * fs - 1, start, 3, end - start));
                dc.DrawRectangle(RGB(36, 36, 36), null, new Rect((cntrl.ruleFeature + 1) * fs + 0, start, 30, 32));

                Text(ref dc, "#" + rules.Count.ToString() + " (" + cntrl.ruleLabel.ToString() + ")\n↑" + FormatNumber(max_rule) + "\n↓" + FormatNumber(min_rule), 8, font, (int)((cntrl.ruleFeature + 1) * fs) + 2, (int)start + 2);
                //Text(ref dc, "↑" + max_rule.ToString("F2"), 7, font, (int)((cntrl.ruleFeature + 1) * fs) + 2, (int)start + 12);
                //Text(ref dc, "↓" + min_rule.ToString("F2"), 7, font, (int)((cntrl.ruleFeature + 1) * fs) + 2, (int)start + 22);
                static string FormatNumber(double number)
                {
                    if (Math.Abs(number) >= 1e9)
                    {
                        return (number / 1e9).ToString("0.#") + "B";
                    }
                    else if (Math.Abs(number) >= 1e6)
                    {
                        return (number / 1e6).ToString("0.#") + "M";
                    }
                    else if (Math.Abs(number) >= 1e4)
                    {
                        return (number / 1e3).ToString("0") + "K";
                    }
                    else
                    {
                        return number.ToString("0.##");
                    }
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
        //   if (featureEngineering) return;



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
                        cntrl.ruleFeature = j; break;
                    }
                dc.Close();
            }
        }
    }

    string[] styleNames = { "Max Class", "Stack All", "Max All" };
    int style = 0;
    float countThickness = 1.0f;
    int featureMode = -1;
    void Mouse_Down(object sender, MouseButtonEventArgs e)
    {
        int y = (int)e.GetPosition(this).Y, x = (int)e.GetPosition(this).X;


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
                        for (int j = 0, c = 0; j < trainLabels.Length; j++)
                            trainData[(j + 1) * features - 1] = trainData[j * features + i];

                        DrawParallelCoordinates("feature = " + i.ToString()); return;
                    }
                    if (featureMode == 0)
                    {
                        featureMode = -1;
                        for (int j = 0, c = 0; j < trainLabels.Length; j++)
                            trainData[(j + 1) * features - 1] += trainData[j * features + i];
                        AddFeatureMode("+");
                        DrawParallelCoordinates("feature = " + i.ToString()); return;
                    }
                    if (featureMode == 1)
                    {
                        featureMode = -1;
                        for (int j = 0, c = 0; j < trainLabels.Length; j++)
                            trainData[(j + 1) * features - 1] -= trainData[j * features + i];
                        AddFeatureMode("-");
                        DrawParallelCoordinates("feature = " + i.ToString()); return;
                    }
                    if (featureMode == 2)
                    {
                        featureMode = -1;
                        for (int j = 0, c = 0; j < trainLabels.Length; j++)
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
                        for (int j = 0, c = 0; j < trainLabels.Length; j++)
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
            /*
            Rect(ref dc, buttonBG, (int)(fs * (features - 1)) + xs + 3 + 0 * 12, (int)(ys - 13), 10, 10);
            Rect(ref dc, buttonBG, (int)(fs * (features - 1)) + xs + 3 + 1 * 12, (int)(ys - 13), 10, 10);
            Rect(ref dc, buttonBG, (int)(fs * (features - 1)) + xs + 3 + 2 * 12, (int)(ys - 13), 10, 10);
            Rect(ref dc, buttonBG, (int)(fs * (features - 1)) + xs + 3 + 3 * 12, (int)(ys - 13), 10, 10);

             */
            //  return;
        }

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
                for (int i = 0; i < featureState.Length; i++) featureState[i] = true;

                SetWindow();
            }


            DrawParallelCoordinates("features = " + features.ToString()); return;
        }

        // check buttons and if clicked activate action
        if (SetMaxMinFeature()) return;

        if (CheckButtons()) return;

        ActivateRule();

        ClearRules();

        // DrawButton(ref dc, font, buttonBG, labelStyle[labelStyleID], 9, xs + 0, 3, 80, 20, 3);
        if (BoundsCheck2(x, y, xs + (10 + 80) * 3, 3, 80, 20)) // + count line thickness
        {

            if (++labelStyleID > 3) labelStyleID = 0;
            int closeID = labelStyleID + 0;

            for (int i = 0; i < trainLabels.Length - 1; i++)
            {
                if (trainData[i * features + closeID] < trainData[(i + 1) * features + closeID]) trainLabels[i] = 0;
                else if (trainData[i * features + closeID] > trainData[(i + 1) * features + closeID]) trainLabels[i] = 1;
                else trainLabels[i] = 2;
            }
            DrawParallelCoordinates(); return;
        }

        /*
        Rect(ref dc, buttonBG, (10 + 80) * 9 + xs - 5, ys + height + 25, 10, 10);
        Text(ref dc, "+", 11, font, (int)((10 + 80) * 9) + xs - 4, ys + height + 25 - 2);
        Rect(ref dc, buttonBG, (10 + 80) * 9 + xs - 5, ys + height + 36, 10, 10);
        Text(ref dc, "-", 13, font, (int)((10 + 80) * 9) + xs + -3, ys + height + 36 - 3);*/
        if (BoundsCheck(x, y, (10 + 80) * 9 + xs - 5, ys + height + 25, 10)) // + count line thickness
        {
            countThickness += 0.05f;
            DrawParallelCoordinates($"countThickness = {countThickness:F3}"); return;
        }
        if (BoundsCheck(x, y, (10 + 80) * 9 + xs - 5, ys + height + 36, 10)) // - count line thickness
        {
            if (countThickness <= 0.06f) return;

            countThickness -= 0.05f;
            DrawParallelCoordinates($"countThickness = {countThickness:F3}"); return;
        }
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
            ///  Rect(ref dc, buttonBG, (int)(i * fs + gap) + xs, (int)(ys - 13), 10, 10);
            // style count button
            if (BoundsCheck2(x, y, xs + (10 + 80) * 8, ys + height + 25, 80, 20))
            {
                style++;
                if (style >= 3) style = 0;

                DrawParallelCoordinates("Style");
                return true;
            }
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

            // button NormalizeData
            if (BoundsCheck2(x, y, xs + (10 + 80) * 6, ys + height + 25, 80, 20))
            {

                // Normalize the trainData array
                NormalizeData(trainData, minVal, maxVal);
                DrawParallelCoordinates("NormalizeData");
                return true;
            }
            static void NormalizeData//01
                (float[] trainData, float[] minValues, float[] maxValues)
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
            }
            static void NormalizeDataOneMinusOne
                (float[] trainData, float[] minValues, float[] maxValues)
            {
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

            // button change label
            if (BoundsCheck2(x, y, xs + (10 + 80) * 0, ys + height + 25, 80, 20))
            {
                if (++cntrl.ruleLabel > labelsNames.Length - 1)
                    cntrl.ruleLabel = 0;

                DrawParallelCoordinates("Label changed to " + (cntrl.ruleLabel));
                return true;
            }
            // button test rules
            if (BoundsCheck2(x, y, xs + (10 + 80) * 1, ys + height + 25, 80, 20))
            {
                // test rules
                int[] score = new int[labelsNames.Length], all = new int[labelsNames.Length];
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
                DrawParallelCoordinates("Test accuracy = " + ((score.Sum()) / (double)(all.Sum())).ToString("F2") +
                    "  Predicted: " + ((double)all.Sum() / trainLabels.Length).ToString("F2") +
                    "\n" + "Higher: " + ((score[0]) / (double)(all[0])).ToString("F2") + " (" + score[0] + "/" + all[0] + "), " +

                            "Lower: " + ((score[1]) / (double)(all[1])).ToString("F2") + " (" + score[1] + "/" + all[1] + "), " +
  "Same: " + ((score[2]) / (double)(all[2])).ToString("F2") + " (" + score[2] + "/" + all[2] + ") "
                            );


                /*
                // console
                DrawingContext dc = ContextHelpMod(false, ref canTest);
                Text(ref dc, "Test accuracy = " + ((score[0] + score[1]) / (double)(trainLabels.Length)).ToString("F2") +
                    "\nTansaction accuracy = " + ((score[0]) / (double)(all[0])).ToString("F2") + " (" + score[0] + "/" + all[0] + "), " +
                            " Fraud accuracy = " + ((score[1]) / (double)(all[1])).ToString("F2") + " (" + score[1] + "/" + all[1] + ")"
                            , 11, font, xs + 5, ys + height + 55 + 4);
                dc.Close();*/
                return true;
            }
            // button save rules
            if (BoundsCheck2(x, y, xs + (10 + 80) * 2, ys + height + 25, 80, 20))
            {
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.FileName = "myfirstRules3.txt";
                if (dlg.ShowDialog() == true)
                    using (StreamWriter writer = new StreamWriter(dlg.FileName))
                        foreach (var rule in rules)
                            writer.WriteLine($"{rule.max},{rule.min},{rule.feature},{rule.label}");
                /*
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                var filePath = @"C:\creditcard\myfirstRules3.txt";
               // filePath = @"C:\creditcard\" + @dlg.FileName;
                if (dlg.ShowDialog() == true)
                    using (StreamWriter writer = new StreamWriter(filePath))
                        foreach (var rule in rules)
                            writer.WriteLine($"{rule.max},{rule.min},{rule.feature},{rule.label}");
*/
                DrawParallelCoordinates("Save rules"); return true;
            }
            // button load rules
            if (BoundsCheck2(x, y, xs + (10 + 80) * 3, ys + height + 25, 80, 20))
            {
                Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
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
                var filePath = @"C:\creditcard\myfirstDataset1.txt";
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.FileName = filePath;
                if (dlg.ShowDialog() == true)
                {
                    int ftCnt = 0;
                    filePath = dlg.FileName; // Update the file path with the chosen file name

                    using (StreamWriter writer = new StreamWriter(filePath))
                    {
                        // Write the attribute names in the first line
                        List<string> selectedFeatureNames = new List<string>();
                        List<double> selectedMaxVal = new List<double>();
                        List<double> selectedMinVal = new List<double>();

                        for (int j = 0; j < features; j++)
                        {
                            if (featureState[j])
                            {
                                ftCnt++;
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
                    /*using (StreamWriter writer = new StreamWriter(filePath))
                    {
                        // Write the attribute names in the first line
                        writer.WriteLine(string.Join(",", featureNames));

                        // Write the maximum values in the second line
                        writer.WriteLine(string.Join(",", maxVal));

                        // Write the minimum values in the third line
                        writer.WriteLine(string.Join(",", minVal));

                        // Save data + label in the file
                        for (int i = 0; i < trainLabels.Length; i++)
                        {
                            if (isOut[i]) continue;
                            
                            StringBuilder lineBuilder = new StringBuilder();

                            for (int j = 0, id = i * maxVal.Length; j < maxVal.Length; j++)
                            {
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
                    }*/
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

                for (int i = 0; i < labelLength.Length; i++)
                    labelLength[i] = 0;

                foreach (int label in trainLabels) labelLength[label]++;
                features = maxVal.Length;
                featureState = new bool[features];
                for (int i = 0; i < features; i++)
                    featureState[i] = true;

                SetWindow();

                DrawParallelCoordinates("Load dataset"); return true;
            }

            return false;


        }
        bool BoundsCheck2(int x, int y, int width, int height, int paddingW, int paddingH) =>
                    x >= width && x <= width + paddingW && y >= height && y <= height + paddingH;
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
                for (int i = 0; i < isOut.Length; i++) isOut[i] = false;
                return;
            }
        }
    }
    bool BoundsCheck(int x, int y, int width, int height, int padding) =>
        x >= width && x <= width + padding && y >= height && y <= height + padding;

    int labelStyleID = 3;
    string[] labelStyle = { "Open-NextOpen", "High-NextHigh", "Low-NextLow", "Temp-NextTemp" };

    void DrawParallelCoordinates//AgeRelated
        (string str = "")
    {

        DrawingContext dc = ContextHelpMod(false, ref canVisual);

        // draw console 
        Rect(ref dc, font, xs, ys + height + 30 + 25, 530, 35);
        Rect(ref dc, RGB(0, 0, 0), xs + 1, ys + height + 30 + 25 + 1, 530 - 2, 35 - 2);
        Text(ref dc, str, 11, font, xs + 5, ys + height + 55 + 4);

        BackgroundStuff(ref dc, features, trainData.Length);

        DrawDistributionCounts();

        DrawGridInfo();

        DrawButtons();

        void DrawButtons()
        {
            byte b = 36;
            var buttonBG = RGB(b, b, b);


            DrawButton(ref dc, font, buttonBG, labelStyle[labelStyleID], 9, xs + 95, 3, 80, 20, 3);

            DrawButton(ref dc, br[cntrl.ruleLabel], buttonBG, (cntrl.ruleLabel == 0 ? labelsNames[0] : labelsNames[1]) + " Rule " + (rules.Count + 1), 9, xs, ys + height + 25, 80, 20, 0);

            DrawButton(ref dc, font, buttonBG, "Test Rules", 9, xs, ys + height + 25, 80, 20, 1);
            DrawButton(ref dc, font, buttonBG, "Save Rules", 9, xs, ys + height + 25, 80, 20, 2);
            DrawButton(ref dc, font, buttonBG, "Load Rules", 9, xs, ys + height + 25, 80, 20, 3);
            DrawButton(ref dc, font, buttonBG, "Save Dataset", 9, xs, ys + height + 25, 80, 20, 4);
            DrawButton(ref dc, font, buttonBG, "Load Dataset", 9, xs, ys + height + 25, 80, 20, 5);
            DrawButton(ref dc, font, buttonBG, "Normalize Dataset", 9, xs, ys + height + 25, 80, 20, 6);
            DrawButton(ref dc, font, buttonBG, "Max Min Data", 9, xs, ys + height + 25, 80, 20, 7);
            DrawButton(ref dc, font, buttonBG, styleNames[style], 9, xs, ys + height + 25, 80, 20, 8);

            void DrawButton(ref DrawingContext dc, Brush rgb, Brush buttonBG, string str, int strSz, int x, int y, int width, int height, int bttnNumber)
            {
                x += (10 + width) * bttnNumber;
                Rect(ref dc, rgb, x, y, width, height);
                Rect(ref dc, buttonBG, x + 1, y + 1, width - 2, height - 2); // Ruler
                Text(ref dc, str, strSz, font, x + 4, y + 5);
            }

            double gap = 0.5 * (fs - 4 * 12) + 1;
            for (int j = 0; j < 4; j++)
                for (int i = 0; i < featureState.Length; i++)
                    Rect(ref dc, buttonBG, (int)(i * fs + gap) + xs + j * 12 + 0, (int)(ys + height) + 7, 10, 10);
           
            
            
            for (int i = 0; i < featureState.Length; i++)
            {
                Text(ref dc, "▲", 9, font, (int)(i * fs + gap) + xs + 0 * 12 + 1, ys + height - 1 + 8);
                Text(ref dc, "+", 11, font, (int)(i * fs + gap) + xs + 1 * 12 + 1, ys + height - 2 + 8);
                Text(ref dc, "-", 13, font, (int)(i * fs + gap) + xs + 2 * 12 + 2, ys + height - 3 + 8);
                Text(ref dc, "▼", 9, font, (int)(i * fs + gap) + xs + 3 * 12 + 1, ys + height - 1 + 8);
            }

            // count thickness
            Rect(ref dc, buttonBG, (10 + 80) * 9 + xs - 5, ys + height + 25, 10, 10);
            Text(ref dc, "+", 11, font, (int)((10 + 80) * 9) + xs - 4, ys + height + 25 - 2);
            Rect(ref dc, buttonBG, (10 + 80) * 9 + xs - 5, ys + height + 36, 10, 10);
            Text(ref dc, "-", 13, font, (int)((10 + 80) * 9) + xs + -3, ys + height + 36 - 3);
            if (featureEngineering)
            {
                for (int i = 0; i < featureState.Length - 1; i++)
                {
                    Rect(ref dc, buttonBG, (int)(i * fs) + xs, (int)(ys - 13), 10, 10);
                    Rect(ref dc, RGB(168, 212, 255), (int)(i * fs) + xs + 1, (int)(ys - 12), 8, 8);
                }

                Rect(ref dc, buttonBG, (int)(fs * (features - 1)) + xs + 3 + 0 * 12, (int)(ys - 13), 10, 10);
                Rect(ref dc, buttonBG, (int)(fs * (features - 1)) + xs + 3 + 1 * 12, (int)(ys - 13), 10, 10);
                Rect(ref dc, buttonBG, (int)(fs * (features - 1)) + xs + 3 + 2 * 12, (int)(ys - 13), 10, 10);
                Rect(ref dc, buttonBG, (int)(fs * (features - 1)) + xs + 3 + 3 * 12, (int)(ys - 13), 10, 10);

                Text(ref dc, "+", 11, font, (int)(fs * (features - 1)) + xs + 4 + 0 * 12, (int)(ys - 13) - 1);
                Text(ref dc, "-", 13, font, (int)(fs * (features - 1)) + xs + 5 + 1 * 12, (int)(ys - 13) - 2);
                Text(ref dc, "*", 9, font, (int)(fs * (features - 1)) + xs + 6 + 2 * 12, (int)(ys - 13) + 2);
                Text(ref dc, "=", 9, font, (int)(fs * (features - 1)) + xs + 4 + 3 * 12, (int)(ys - 13) - 0);
            }
            else
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

        dc.Close();

        return;

        void DrawDistributionCounts()
        {
            //  int h = (int)(height * countThickness);
            int h = (int)Math.Round(height * countThickness);
            if (style == 0)
            {
                float[] maxMinusMin = new float[maxVal.Length];
                for (int i = 0; i < maxMinusMin.Length; i++) maxMinusMin[i] = 1.0f / (maxVal[i] - minVal[i]);

                for (int labelId = 0; labelId < labelNum; labelId++) // for (int labelId = 1; labelId <= 0; labelId--)
                {

                    // 1. init counts and maxCounts for each label
                    int[] counts = new int[features * h],
                        maxCounts = new int[features];


                    // 2. translate values into count height id of each data feature
                    for (int i = 0; i < trainLabels.Length; i++)
                    {
                        if (isOut[i] || trainLabels[i] != labelId) continue;

                        for (int j = 0, id = i * features; j < features; j++)
                            if (featureState[j])
                                if (trainData[id + j] <= maxVal[j] && trainData[id + j] >= minVal[j])
                                    counts[(int)((trainData[id + j] - minVal[j]) * maxMinusMin[j] * (h - 1) + j * h)]++;
                    }

                    // 3. get max of each feature
                    for (int j = 0; j < features; j++)
                        if (featureState[j])
                            for (int i = 0; i < h; i++)
                            {
                                int count = counts[i + j * h];
                                if (count > maxCounts[j]) maxCounts[j] = count;
                            }
                    float pixel = height / MathF.Round(height * countThickness);
                    // 4. draw line with length and color intensity of the line based on counts
                    float labelShift = labelId / 3.0f;
                    // 4. draw line with length and color intensity of the line based on counts
                    for (int j = 0; j < features; j++)
                        if (featureState[j])
                            for (int i = 0; i < h; i++)
                            {
                                int count = counts[i + j * h];
                                if (count == 0) continue;
                                double probability = count / (double)maxCounts[j];
                                double pixelPos = (ys + height - Math.Floor(i * pixel)); //int pixelPos = (int)(ys + height - (i * pixel));
                                //double pixelPos = (ys + height - (i * pixel));
                                // assign colors based on label ID
                                Color lineColor;
                                if (labelId == 0)
                                {
                                    lineColor = Color.FromRgb((byte)(22 + 50 * probability), (byte)(31 + 40 * probability), (byte)(75 + 170 * probability));  // blue for label 0
                                    Pen pen = new Pen(new SolidColorBrush(lineColor), pixel);
                                    Line(ref dc, pen,
                                        17 + (j + labelShift) * fs, pixelPos,
                                        17 + (j + labelShift) * fs + fs * 0.03 + probability * fs * 0.3, pixelPos);
                                }
                                else if (labelId == 1)
                                {
                                    lineColor = Color.FromRgb((byte)(65 + 190 * probability), (byte)(55 + 125 * probability), (byte)(0 * probability));  // gold for label 1
                                    Pen pen = new Pen(new SolidColorBrush(lineColor), pixel);
                                    Line(ref dc, pen,
                                        17 + (j + 0.66) * fs, pixelPos,
                                        17 + (j + 0.66) * fs + fs * 0.03 + probability * fs * 0.3, pixelPos);
                                }
                                else if (labelId == 2)
                                {
                                    lineColor = Color.FromRgb((byte)(30 + 89 * probability), (byte)(0 * probability), (byte)(47 + 160 * probability));  // purple for label 2
                                    Pen pen = new Pen(new SolidColorBrush(lineColor), pixel);
                                    Line(ref dc, pen,
                                       17 + (j + 0.33) * fs, pixelPos,
                                       17 + (j + 0.33) * fs + fs * 0.03 + probability * fs * 0.3, pixelPos);
                                }
                            }
                }
                return;
            }
            else
            {
                // if (style == 1) h = (int)Math.Round((height ) * countThickness) / 3;
                float[] maxMinusMin = new float[maxVal.Length];
                for (int i = 0; i < maxMinusMin.Length; i++) maxMinusMin[i] = 1.0f / (maxVal[i] - minVal[i]);

                int[] maxCounts = new int[features];
                for (int labelId = 0; labelId < labelNum; labelId++) // for (int labelId = 1; labelId <= 0; labelId--)
                {
                    // 1. init counts and maxCounts for each label
                    int[] counts = new int[features * h];

                    // 2. translate values into count height id of each data feature
                    for (int i = 0; i < trainLabels.Length; i++)
                    {
                        if (isOut[i] || trainLabels[i] != labelId) continue;

                        for (int j = 0, id = i * features; j < features; j++)
                            if (featureState[j])
                                if (trainData[id + j] <= maxVal[j] && trainData[id + j] >= minVal[j])
                                    counts[(int)((trainData[id + j] - minVal[j]) * maxMinusMin[j] * (h - 1) + j * h)]++;
                    }

                    // 3. get max of each feature
                    for (int j = 0; j < features; j++)
                        if (featureState[j])
                            for (int i = 0; i < h; i++)
                            {
                                int count = counts[i + j * h];
                                if (count > maxCounts[j]) maxCounts[j] = count;
                            }
                }

                //   for (int labelId = 0; labelId < labelNum; labelId++) //
                for (int labelId = labelNum - 1; labelId >= 0; labelId--)
                {
                    // 1. init counts and maxCounts for each label
                    int[] counts = new int[features * h];

                    // 2. translate values into count height id of each data feature
                    for (int i = 0; i < trainLabels.Length; i++)
                    {
                        if (isOut[i] || trainLabels[i] != labelId) continue;

                        for (int j = 0, id = i * features; j < features; j++)
                            if (featureState[j])
                                if (trainData[id + j] <= maxVal[j] && trainData[id + j] >= minVal[j])
                                    counts[(int)((trainData[id + j] - minVal[j]) * maxMinusMin[j] * (h - 1) + j * h)]++;
                    }

                    float pixel = height / MathF.Round(height * countThickness);

                    // 4. draw line with length and color intensity of the line based on counts
                    float labelShift = labelId / 3.0f;
                    // labelShift = 0.0f;
                    if (style == 2)
                        for (int j = 0; j < features; j++)
                            if (featureState[j])
                                for (int i = 0; i < h; i++)
                                {
                                    int count = counts[i + j * h];
                                    if (count == 0) continue;
                                    double probability = count / (double)maxCounts[j];
                                    double pixelPos = (ys + height - Math.Floor(i * pixel)); //int pixelPos = (int)(ys + height - (i * pixel));
                                    // assign colors based on label ID
                                    Color lineColor;
                                    if (labelId == 0)
                                    {
                                        lineColor = Color.FromRgb((byte)(22 + 50 * probability), (byte)(31 + 40 * probability), (byte)(75 + 170 * probability));  // blue for label 0
                                        Pen pen = new Pen(new SolidColorBrush(lineColor), pixel);
                                        Line(ref dc, pen,
                                            17 + (j + labelShift) * fs, pixelPos,
                                            17 + (j + labelShift) * fs + fs * 0.03 + probability * fs * 0.3, pixelPos);
                                    }
                                    else if (labelId == 1)
                                    {
                                        lineColor = Color.FromRgb((byte)(65 + 190 * probability), (byte)(55 + 125 * probability), (byte)(0 * probability));  // gold for label 1
                                        Pen pen = new Pen(new SolidColorBrush(lineColor), pixel);
                                        Line(ref dc, pen,
                                            17 + (j + 0.66) * fs, pixelPos,
                                            17 + (j + 0.66) * fs + fs * 0.03 + probability * fs * 0.3, pixelPos);
                                    }
                                    else if (labelId == 2)
                                    {
                                        lineColor = Color.FromRgb((byte)(30 + 89 * probability), (byte)(0 * probability), (byte)(47 + 160 * probability));  // purple for label 2
                                        Pen pen = new Pen(new SolidColorBrush(lineColor), pixel);
                                        Line(ref dc, pen,
                                           17 + (j + 0.33) * fs, pixelPos,
                                           17 + (j + 0.33) * fs + fs * 0.03 + probability * fs * 0.3, pixelPos);
                                    }
                                }

                    labelShift = 0.0f;
                    if (style == 1)
                        for (int j = 0; j < features; j++)
                            if (featureState[j])
                                for (int i = 0; i < h; i++)
                                {

                                    int count = counts[i + j * h];
                                    if (count == 0) continue;

                                    double probability = count / (double)maxCounts[j];
                                    double pixelPos = (ys + height - Math.Floor(i * pixel)) + (pixel / 3) * labelId;
                                    // assign colors based on label ID
                                    Color lineColor;
                                    if (labelId == 0)
                                    {
                                        lineColor = Color.FromRgb((byte)(22 + 50 * probability), (byte)(31 + 40 * probability), (byte)(75 + 170 * probability));  // blue for label 0
                                        Pen pen = new Pen(new SolidColorBrush(lineColor), pixel / 3);
                                        Line(ref dc, pen,
                                            17 + (j + labelShift) * fs, pixelPos,
                                            17 + (j + labelShift) * fs + probability * fs, pixelPos);
                                    }
                                    else if (labelId == 1)
                                    {
                                        lineColor = Color.FromRgb((byte)(65 + 190 * probability), (byte)(55 + 125 * probability), (byte)(0 * probability));  // gold for label 1
                                        Pen pen = new Pen(new SolidColorBrush(lineColor), pixel / 3);
                                        Line(ref dc, pen,
                                            17 + (j + labelShift) * fs, pixelPos,
                                            17 + (j + labelShift) * fs + probability * fs, pixelPos);
                                    }
                                    else if (labelId == 2)
                                    {
                                        lineColor = Color.FromRgb((byte)(30 + 89 * probability), (byte)(0 * probability), (byte)(47 + 160 * probability));  // purple for label 2
                                        Pen pen = new Pen(new SolidColorBrush(lineColor), pixel / 3);
                                        Line(ref dc, pen,
                                           17 + (j + labelShift) * fs, pixelPos,
                                           17 + (j + labelShift) * fs + probability * fs, pixelPos);
                                    }
                                }
                    /* for (int j = 0; j < features; j++)
                         if (featureState[j])
                             for (int i = 0; i < h; i++)
                             {
                                 int count = counts[i + j * h];
                                 if (count == 0) continue;
                                 double probability = count / (double)maxCounts[j];
                                 int pixelPos = (int)(ys + height - (i * pixel));
                                 // assign colors based on label ID
                                 Color lineColor;
                                 if (labelId == 0)
                                 {
                                     lineColor = Color.FromRgb((byte)(22 + 50 * probability), (byte)(31 + 40 * probability), (byte)(75 + 170 * probability));  // blue for label 0
                                     Pen pen = new Pen(new SolidColorBrush(lineColor), pixel);
                                     Line(ref dc, pen,
                                         17 + (j + labelShift) * fs, pixelPos,
                                         17 + (j + labelShift) * fs + probability * fs, pixelPos);
                                 }
                                 else if (labelId == 1)
                                 {
                                     lineColor = Color.FromRgb((byte)(65 + 190 * probability), (byte)(55 + 125 * probability), (byte)(0 * probability));  // gold for label 1
                                     Pen pen = new Pen(new SolidColorBrush(lineColor), pixel);
                                     Line(ref dc, pen,
                                         17 + (j + labelShift) * fs, pixelPos,
                                         17 + (j + labelShift) * fs + probability * fs, pixelPos);
                                 }
                                 else if (labelId == 2)
                                 {
                                     lineColor = Color.FromRgb((byte)(30 + 89 * probability), (byte)(0 * probability), (byte)(47 + 160 * probability));  // purple for label 2
                                     Pen pen = new Pen(new SolidColorBrush(lineColor), pixel);
                                     Line(ref dc, pen,
                                        17 + (j + labelShift) * fs, pixelPos,
                                        17 + (j + labelShift) * fs + probability * fs, pixelPos);
                                 }
                             }
                     */
                    /*
                    for (int j = 0; j < features; j++)
                        if (featureState[j])
                            for (int i = 0; i < h; i++)
                            {
                                int count = counts[i + j * h];
                                if (count == 0) continue;
                                double probability = count / (double)maxCounts[j];
                                int pixelPos = (int)(ys + height - (i * pixel));
                                // assign colors based on label ID
                                Color lineColor;
                                if (labelId == 0)
                                {
                                    lineColor = Color.FromRgb((byte)(22 + 50 * probability), (byte)(31 + 40 * probability), (byte)(75 + 170 * probability));  // blue for label 0
                                    Pen pen = new Pen(new SolidColorBrush(lineColor), pixel);
                                    Line(ref dc, pen,
                                        17 + (j + labelShift) * fs, pixelPos,
                                        17 + (j + labelShift) * fs + fs * 0.03 + probability * fs * 0.3, pixelPos);
                                }
                                else if (labelId == 1)
                                {
                                    lineColor = Color.FromRgb((byte)(65 + 190 * probability), (byte)(55 + 125 * probability), (byte)(0 * probability));  // gold for label 1
                                    Pen pen = new Pen(new SolidColorBrush(lineColor), pixel);
                                    Line(ref dc, pen,
                                        17 + (j + labelShift) * fs, pixelPos,
                                        17 + (j + labelShift) * fs + fs * 0.03 + probability * fs * 0.3, pixelPos);
                                }
                                else if (labelId == 2)
                                {
                                    lineColor = Color.FromRgb((byte)(30 + 89 * probability), (byte)(0 * probability), (byte)(47 + 160 * probability));  // purple for label 2
                                    Pen pen = new Pen(new SolidColorBrush(lineColor), pixel);
                                    Line(ref dc, pen,
                                       17 + (j + labelShift) * fs, pixelPos,
                                       17 + (j + labelShift) * fs + fs * 0.03 + probability * fs * 0.3, pixelPos);
                                }
                            }
                    */
                }
            }



        }
        void DrawDistributionCountsSource()
        {
            float[] maxMinusMin = new float[maxVal.Length];
            for (int i = 0; i < maxMinusMin.Length; i++) maxMinusMin[i] = 1.0f / (maxVal[i] - minVal[i]);

            for (int labelId = 0; labelId < labelNum; labelId++) // for (int labelId = 1; labelId <= 0; labelId--)
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
                                counts[(int)((trainData[id + j] - minVal[j]) * maxMinusMin[j] * (height - 1)) + j * (int)height]++;
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
                            int pixelPos = (int)(ys + height - i);
                            // assign colors based on label ID
                            Color lineColor;
                            if (labelId == 0)
                            {
                                lineColor = Color.FromRgb((byte)(22 + 50 * probability), (byte)(31 + 40 * probability), (byte)(75 + 170 * probability));  // blue for label 0
                                Pen pen = new Pen(new SolidColorBrush(lineColor), 1);
                                Line(ref dc, pen,
                                    17 + (j + 0.0) * fs, pixelPos,
                                    17 + (j + 0.0) * fs + fs * 0.03 + probability * fs * 0.3, pixelPos);
                            }
                            else if (labelId == 1)
                            {
                                lineColor = Color.FromRgb((byte)(65 + 190 * probability), (byte)(55 + 125 * probability), (byte)(0 * probability));  // gold for label 1
                                Pen pen = new Pen(new SolidColorBrush(lineColor), 1);
                                Line(ref dc, pen,
                                    17 + (j + 0.66) * fs, pixelPos,
                                    17 + (j + 0.66) * fs + fs * 0.03 + probability * fs * 0.3, pixelPos);
                            }
                            else if (labelId == 2)
                            {
                                lineColor = Color.FromRgb((byte)(30 + 89 * probability), (byte)(0 * probability), (byte)(47 + 160 * probability));  // purple for label 2
                                Pen pen = new Pen(new SolidColorBrush(lineColor), 1);
                                Line(ref dc, pen,
                                   17 + (j + 0.33) * fs, pixelPos,
                                   17 + (j + 0.33) * fs + fs * 0.03 + probability * fs * 0.3, pixelPos);
                            }
                        }
            }

        }
        void DrawGridInfo()
        {
            // draw info lines and labels
            for (int j = 0; j < features; j++)
            {
                double x = 15 + (j + 1) * fs;
                //  dc.DrawLine(new Pen(font, 1.0), new Point(x, ys), new Point(x, ys + height));
                // Rect(ref dc, font, (int)x, (int)ys, 1, (int)height);
                dc.DrawRectangle(font, null, new Rect(x, ys, 1, height));
                var min = minVal[j];
                double range = maxVal[j] - min;

                for (int i = 0, cats = 10; i < cats + 1; i++) // accuracy lines 0, 20, 40...
                {
                    double yGrph = ys + height - i * (height / (double)cats);
                    double val = range / cats * i + min;
                    Line(ref dc, new Pen(font, 1.0), x - 2, yGrph, x, yGrph);
                    Text(ref dc, val.ToString("F6"), 7, font, (int)(x - TextWidth(val.ToString("F6"), 7) - 3), (int)yGrph - 3);
                }
            }
        }
        void BackgroundStuff(ref DrawingContext dc, int features, int len)
        {

            // main info
            Text(ref dc, "Climate-Duesseldorf 1969-2022: " + (len / features).ToString() + " Length, " + features.ToString()
                + " Features, " + labelNum.ToString() + " Labels", 12, font, xs, ys - 32);

            // dataset info 
            byte cl = 0;
            Rect(ref dc, RGB(cl, cl, cl), 15, ys, (int)(features * fs), (int)height);

            int first = 0;
            for (int i = 0; i < -3; i++)
            {
                int second = (int)(400 * (labelLength[i] / (double)trainLabels.Length));
                Rect(ref dc, br[i], 430 + first, (int)(ys - 34), second, 15);
                first = second;
            }

            int sl1 = 0, sr1 = (int)(300 * (labelLength[0] / (double)trainLabels.Length));
            Rect(ref dc, br[0], 430 + sl1, (int)(ys - 34), sr1, 15);
            int sl2 = sr1, sr2 = (int)(300 * (labelLength[2] / (double)trainLabels.Length));
            Rect(ref dc, br[2], 430 + sl2, (int)(ys - 34), sr2, 15);
            int sl3 = sr2 + sr1, sr3 = (int)(300 * (labelLength[1] / (double)trainLabels.Length));
            Rect(ref dc, br[1], 430 + sl3, (int)(ys - 34), sr3, 15);

            //  Rect(ref dc, font, 429, (int)(ys - 34 - 1), 400 + 3, 17);
            // Rect(ref dc, br[0], 430, (int)(ys - 34), (int)(400 * (labelLength[0] / (double)trainLabels.Length)), 15);
            // Rect(ref dc, br[1], 430 + (int)(400 * (labelLength[0] / (double)trainLabels.Length)), (int)(ys - 34), (int)(400 * (labelLength[1] / (double)trainLabels.Length) + 1), 15);
            Text(ref dc, labelsNames[0] + " " + labelLength[0].ToString() + " (" + (labelLength[0] / (double)trainLabels.Length * 100).ToString("F3") + "%)"
                + ", " + labelsNames[2] + " " + labelLength[2].ToString() + " (" + (labelLength[2] / (double)trainLabels.Length * 100).ToString("F3") + "%)"
                + ", " + labelsNames[1] + " " + labelLength[1].ToString() + " (" + (labelLength[1] / (double)trainLabels.Length * 100).ToString("F3") + "%)", 11, Brushes.Black, 435 + 20, (int)(ys - 34) + 1);

            // feature id's
            for (int i = 0; i < features; i++)
                Text(ref dc, featureNames[i].ToString(), 10, font, (int)(12 + (i + 1) * fs - TextWidth(featureNames[i].ToString(), 10)), ys - 14);
        }

        double TextWidth(string text, int fontSize) =>
            new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new("TimesNewRoman"), fontSize, Brushes.Black).WidthIncludingTrailingWhitespace;
    }

    string[] labelsNames = {  "-", "+",  "="
    };
    // string[] labelsNames = { "not been diagnosed", "diagnosed" };

    void DataInitForex1()
    {
        string filePath = @"C:\klima\data\klima_duesseldorf_modified.txt";

        string[] lines = File.ReadLines(filePath).ToArray();

        string[] featureNames1 = lines[0].Split(';');

        featureNames = featureNames1;
        features = featureNames.Length;
        labelNum = labelsNames.Length;

        int firstCuts = 1;
        int startIndex = 0;
        float[] trainDataSource = new float[(lines.Length - firstCuts) * features];

        for (int i = firstCuts; i < lines.Length; i++)
        {
            string[] currentValues = lines[i].Split(';');

            for (int j = 0; j < currentValues.Length; j++)
            {
                trainDataSource[startIndex++] = float.Parse(currentValues[j], CultureInfo.InvariantCulture);
            }
        }

        int highTempID = 11;
        trainLabels = new int[lines.Length - firstCuts];

        for (int i = 0; i < trainLabels.Length - 1; i++)
        {
            if (trainDataSource[i * features + highTempID] < trainDataSource[(i + 1) * features + highTempID]) trainLabels[i] = 0;
            else if (trainDataSource[i * features + highTempID] > trainDataSource[(i + 1) * features + highTempID]) trainLabels[i] = 1;
            else trainLabels[i] = 2;
        }

        trainData = trainDataSource;
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
        br[2] = RGB(119, 0, 200); // red                
    }
    static Brush RGB(byte red, byte green, byte blue)
    {
        Brush brush = new SolidColorBrush(Color.FromRgb(red, green, blue));
        brush.Freeze();
        return brush;
    }
} // TheWindow end

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
