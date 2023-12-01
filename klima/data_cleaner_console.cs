// plumber gpt top level statement code!!!
// https://www.dwd.de/DE/leistungen/klimadatendeutschland/klarchivtagmonat.html?nn=16102
using System.Globalization;

// Specify the input file path
string inputFilePath = @"C:\klima\klima_duesseldorf.txt";

// Specify the output file path (modify the name as needed)
string outputFilePath = @"C:\klima\klima_duesseldorf_modified.txt";

// Read all lines from the input file
string[] lines = File.ReadAllLines(inputFilePath);

// Remove spaces from each line
for (int i = 0; i < lines.Length; i++)
    lines[i] = lines[i].Replace(" ", "");

if (lines.Length > 0)
{
    // Replace spaces in feature names from the first line
    lines[0] = lines[0].Replace(" ", "");

    // Process each data row, starting from index 1 to skip the titles
    for (int i = 0; i < lines.Length; i++)
    {
        // Split the line into individual features
        string[] features = lines[i].Split(';');

        // Replace all occurrences of -999 with 0
        for (int j = 0; j < features.Length; j++)
        {
            if (features[j].Trim() == "-999")
            {
                features[j] = "0";
            }
        }

        // Join the features back into a line without semicolons for the replaced values
        lines[i] = string.Join(";", features);
    }

    // Save the modified data back to the output file
    File.WriteAllLines(outputFilePath, lines);

    Console.WriteLine($"All occurrences of -999 values have been replaced with 0, spaces in feature names have been removed, and the modified file has been saved successfully at: {outputFilePath}");

    // Output the feature names from the first line
    Console.WriteLine("Feature Names:");
    Console.WriteLine(lines[0]);
}

// Check if there are lines in the file
if (lines.Length > 0)
{
    // Process each data row, starting from index 1 to skip the titles
    for (int i = 1; i < lines.Length; i++)
    {
        // Split the line into individual features
        string[] features = lines[i].Split(';');

        // Replace all occurrences of -999 with 0
        for (int j = 0; j < features.Length; j++)
        {
            if (features[j].Trim() == "-999")
            {
                features[j] = "0";
            }
        }

        // Join the features back into a line without semicolons for the replaced values
        lines[i] = string.Join(";", features);
    }

    // Save the modified data back to the output file
    //    File.WriteAllLines(outputFilePath, lines);

    Console.WriteLine($"All occurrences of -999 values have been replaced with 0, and the modified file has been saved successfully at: {outputFilePath}");
}

// Check if there are lines in the file
if (lines.Length > 0)
{
    // Specify feature indices to remove
    int[] featureIndicesToRemove = { 0, 2, 5, 9, 18 }; // Replace with the indices you want to remove
                                                       // Process the header line to remove specified features

    string[] headerFeatures = lines[0].Split(';');
    headerFeatures = headerFeatures.Where((value, index) => !featureIndicesToRemove.Contains(index)).ToArray();
    lines[0] = string.Join(";", headerFeatures);

    // Remove the specified features (including semicolons) for each data row
    for (int i = 1; i < lines.Length; i++)
    {
        // Split the line into individual features
        string[] features = lines[i].Split(';');

        // Remove the specified features (including semicolons)
        foreach (int indexToRemove in featureIndicesToRemove)
        {
            if (indexToRemove >= 0 && indexToRemove < features.Length)
            {
                features[indexToRemove] = string.Empty;
            }
        }

        // Join the features back into a line without semicolons for the removed values
        lines[i] = string.Join(";", features.Where(feature => !string.IsNullOrEmpty(feature)));
    }

    // Save the modified data back to the output file
    File.WriteAllLines(outputFilePath, lines);

    Console.WriteLine($"Specified features (including semicolons) have been removed, and the modified file has been saved successfully at: {outputFilePath}");
}
else
{
    Console.WriteLine("The input file is empty.");
}
