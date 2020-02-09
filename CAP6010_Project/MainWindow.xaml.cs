﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CAP6010_Project
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Run();
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            Run();
        }

        private void Run()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("<!DOCTYPE html><html><body>");
            sb.Append("<h1>CAP 6010 Project Results</h1>");
            sb.Append("<h3>Gabor Kovacs</h3>");
            sb.Append("<hr>");


            Dictionary<string, string> huffmanTable = BuildHuffmanTable();

            sb.Append("<h3>Original Image:</h3>");

            int[,] originalImageArray = ImportCSV(out int unCompressedSizeInBits);

            Print2DArray(sb, originalImageArray);

            List<int[,]> listOfCompressedImages = CompressImage(originalImageArray);

            for (int predictor = 1; predictor <= 7; predictor++)
            {
                sb.Append("<div style='border:1px solid black;margin-bottom:25px;padding:10px 10px 10px 30px;'>");

                sb.Append(String.Format("<h3>Predictor {0}: {1} </h3>", predictor, String.Format(@"<img src='..\..\Images\Predictor{0}.png' align='middle'>", predictor)));

                List<string> huffmanEncodedImage = HuffmanEncode(listOfCompressedImages[predictor - 1], huffmanTable, out int compressedSizeInBits);

                sb.Append("<p>");
                sb.Append("<h4>Compressed Binary Sequence:</h4>");

                foreach (string row in huffmanEncodedImage)
                {
                    sb.Append(row);
                    sb.Append("<br>");
                }

                sb.Append("</p>");
                sb.Append("<br>");

                // Decode the Huffman Encoded Image
                int[,] compressedImage = HuffmanDecode(huffmanEncodedImage, huffmanTable);

                // Decompress Image


                float compressionRatio = (float)unCompressedSizeInBits / (float)compressedSizeInBits;
                float bitsPerPixel = 8 / compressionRatio;

                sb.Append("<p>");
                sb.Append("Compression Ratio: ");
                sb.Append(((float)unCompressedSizeInBits).ToString() + " / " + ((float)compressedSizeInBits).ToString() + " = " + compressionRatio.ToString());
                sb.Append("<br>");
                sb.Append("Bits/Pixel: ");
                sb.Append("8 / " + compressionRatio.ToString() + " = " + bitsPerPixel.ToString());
                sb.Append("<br>");
                sb.Append("RMS Error: ");
                sb.Append("[rms value holder]");
                sb.Append("<br>");
                sb.Append("</p>");

                sb.Append("</div>");
            }

            sb.Append("</body></html>");

            File.WriteAllText(@"output.html", sb.ToString());

            Application.Current.Shutdown();
        }

        /// <summary>
        /// Imports a file of comma separated values
        /// </summary>
        /// <param name="inputFileSizeInBits">Size (in bits) of the import data</param>
        /// <returns>a 2D array of the imported values</returns>
        private int[,] ImportCSV(out int inputFileSizeInBits)
        {
            inputFileSizeInBits = 0;

            string filePath = @"../../Files/inputfile1.csv";

            string csvData;

            try
            {
                csvData = File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            int rowIndex = 0;

            string[] rows = csvData.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            int numCells = (rows[0].Split(',')).Count();

            int[,] array = new int[rows.Count(), numCells];

            foreach (string row in rows)
            {
                string[] cells = row.Split(',');

                int columnIndex = 0;

                foreach (string cell in cells)
                {
                    array[rowIndex, columnIndex] = int.Parse(cell);

                    columnIndex++;
                }

                rowIndex++;
            }

            // Calculate the size (in bits) of the import data
            inputFileSizeInBits = rows.Count() * numCells * 8;

            return array;
        }

        private void Print2DArray(StringBuilder sb, int[,] array)
        {
            sb.Append("<table border=1>");

            int dim1 = array.GetLength(0);
            int dim2 = array.GetLength(1);

            for (int row = 0; row < dim1; row++)
            {
                sb.Append("<tr>");

                // Loop through columns
                for (int col = 0; col < dim2; col++)
                {
                    sb.Append("<td width='50px'>");
                    sb.Append(array[row, col].ToString());
                    sb.Append("</td>");
                }
                sb.Append("</tr>");
            }

            sb.Append("</table>");
            sb.Append("<br>");
        }

        /// <summary>
        /// Compress values with specified predictor
        /// </summary>
        /// <param name="inputArray">2D Input Array</param>
        /// <param name="predictor">Predictor value (1-7) to use for conversion</param>
        /// <returns></returns>
        private List<int[,]> CompressImage(int[,] inputArray)
        {
            if (inputArray == null)
            {
                return null;
            }

            // Create a list that will hold the 7 outputs
            List<int[,]> outputs = new List<int[,]>();

            // Create the 7 output arrays and make it the same size as the input array
            int dim1 = inputArray.GetLength(0);
            int dim2 = inputArray.GetLength(1);

            // Output for all 7 2D arrays
            int[,] outputArrayForPredictor1 = new int[dim1, dim2];
            int[,] outputArrayForPredictor2 = new int[dim1, dim2];
            int[,] outputArrayForPredictor3 = new int[dim1, dim2];
            int[,] outputArrayForPredictor4 = new int[dim1, dim2];
            int[,] outputArrayForPredictor5 = new int[dim1, dim2];
            int[,] outputArrayForPredictor6 = new int[dim1, dim2];
            int[,] outputArrayForPredictor7 = new int[dim1, dim2];

            // Loop through rows
            for (int row = 0; row < dim1; row++)
            {
                // Loop through columns
                for (int col = 0; col < dim2; col++)
                {
                    // Check if A exists, if so, get it's value
                    bool a_exists = TryGetA(inputArray, row, col, out int a);
                    // Check if B exists, if so, get it's value
                    bool b_exists = TryGetB(inputArray, row, col, out int b);
                    // Check if C exists, if so, get it's value
                    bool c_exists = TryGetC(inputArray, row, col, out int c);

                    // Run each of the 7 predictors
                    UsePredictor1(a_exists, a, b_exists, b, c_exists, c, inputArray, outputArrayForPredictor1, row, col);
                    UsePredictor2(a_exists, a, b_exists, b, c_exists, c, inputArray, outputArrayForPredictor2, row, col);
                    UsePredictor3(a_exists, a, b_exists, b, c_exists, c, inputArray, outputArrayForPredictor3, row, col);
                    UsePredictor4(a_exists, a, b_exists, b, c_exists, c, inputArray, outputArrayForPredictor4, row, col);
                    UsePredictor5(a_exists, a, b_exists, b, c_exists, c, inputArray, outputArrayForPredictor5, row, col);
                    UsePredictor6(a_exists, a, b_exists, b, c_exists, c, inputArray, outputArrayForPredictor6, row, col);
                    UsePredictor7(a_exists, a, b_exists, b, c_exists, c, inputArray, outputArrayForPredictor7, row, col);
                }
            }

            outputs.Add(outputArrayForPredictor1);
            outputs.Add(outputArrayForPredictor2);
            outputs.Add(outputArrayForPredictor3);
            outputs.Add(outputArrayForPredictor4);
            outputs.Add(outputArrayForPredictor5);
            outputs.Add(outputArrayForPredictor6);
            outputs.Add(outputArrayForPredictor7);

            return outputs;
        }

        #region Predictors

        private void UsePredictor1(bool a_exists, int a, bool b_exists, int b, bool c_exists, int c, int[,] inputArray, int[,] outputArray, int row, int col)
        {
            // If 'a' exists, then x-hat = x-a
            if (a_exists)
            {
                outputArray[row, col] = (int)(inputArray[row, col] - a);
            }
            else
            {
                if (b_exists)
                {
                    outputArray[row, col] = (int)(inputArray[row, col] - b);
                }
                else
                {
                    // Use the same value
                    outputArray[row, col] = inputArray[row, col];
                }
            }
        }

        private void UsePredictor2(bool a_exists, int a, bool b_exists, int b, bool c_exists, int c, int[,] inputArray, int[,] outputArray, int row, int col)
        {
            // If 'b' exists, then x-hat = x-a
            if (b_exists)
            {
                outputArray[row, col] = (int)(inputArray[row, col] - b);
            }
            else
            {
                if (a_exists)
                {
                    outputArray[row, col] = (int)(inputArray[row, col] - a);
                }
                else
                {
                    // Use the same value
                    outputArray[row, col] = inputArray[row, col];
                }
            }
        }

        private void UsePredictor3(bool a_exists, int a, bool b_exists, int b, bool c_exists, int c, int[,] inputArray, int[,] outputArray, int row, int col)
        {
            // If 'c' exists, then x-hat = x-c
            if (c_exists)
            {
                outputArray[row, col] = (int)(inputArray[row, col] - c);
            }
            else
            {
                if (a_exists)
                {
                    outputArray[row, col] = (int)(inputArray[row, col] - a);
                }
                else if (b_exists)
                {
                    outputArray[row, col] = (int)(inputArray[row, col] - b);
                }
                else
                {
                    // Use the same value
                    outputArray[row, col] = inputArray[row, col];
                }
            }
        }

        private void UsePredictor4(bool a_exists, int a, bool b_exists, int b, bool c_exists, int c, int[,] inputArray, int[,] outputArray, int row, int col)
        {
            if (a_exists && b_exists && c_exists)
            {
                outputArray[row, col] = (int)(inputArray[row, col] - (a + b - c));
            }
            else
            {
                if (a_exists)
                {
                    outputArray[row, col] = (int)(inputArray[row, col] - a);
                }
                else if (b_exists)
                {
                    outputArray[row, col] = (int)(inputArray[row, col] - b);
                }
                else
                {
                    // Use the same value
                    outputArray[row, col] = inputArray[row, col];
                }
            }
        }

        private void UsePredictor5(bool a_exists, int a, bool b_exists, int b, bool c_exists, int c, int[,] inputArray, int[,] outputArray, int row, int col)
        {
            if (a_exists && b_exists && c_exists)
            {
                outputArray[row, col] = (int)(inputArray[row, col] - (a + ((b - c) / 2)));
            }
            else
            {
                if (a_exists)
                {
                    outputArray[row, col] = (int)(inputArray[row, col] - a);
                }
                else if (b_exists)
                {
                    outputArray[row, col] = (int)(inputArray[row, col] - b);
                }
                else
                {
                    // Use the same value
                    outputArray[row, col] = inputArray[row, col];
                }
            }
        }

        private void UsePredictor6(bool a_exists, int a, bool b_exists, int b, bool c_exists, int c, int[,] inputArray, int[,] outputArray, int row, int col)
        {
            if (a_exists && b_exists && c_exists)
            {
                outputArray[row, col] = (int)(inputArray[row, col] - (b + ((a - c) / 2)));
            }
            else
            {
                if (a_exists)
                {
                    outputArray[row, col] = (int)(inputArray[row, col] - a);
                }
                else if (b_exists)
                {
                    outputArray[row, col] = (int)(inputArray[row, col] - b);
                }
                else
                {
                    // Use the same value
                    outputArray[row, col] = inputArray[row, col];
                }
            }
        }

        private void UsePredictor7(bool a_exists, int a, bool b_exists, int b, bool c_exists, int c, int[,] inputArray, int[,] outputArray, int row, int col)
        {
            if (a_exists && b_exists)
            {
                outputArray[row, col] = (int)(inputArray[row, col] - ((a + b) / 2));
            }
            else
            {
                if (a_exists)
                {
                    outputArray[row, col] = (int)(inputArray[row, col] - a);
                }
                else if (b_exists)
                {
                    outputArray[row, col] = (int)(inputArray[row, col] - b);
                }
                else
                {
                    // Use the same value
                    outputArray[row, col] = inputArray[row, col];
                }
            }
        }

        #endregion

        #region Predictor Helpers

        /// <summary>
        /// Check if there is a value to the left of the current cell
        /// </summary>
        /// <param name="inputArray"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <param name="cellValue"></param>
        /// <returns></returns>
        private bool TryGetA(int[,] inputArray, int row, int col, out int cellValue)
        {
            try
            {
                cellValue = inputArray[row, col - 1];
                return true;
            }
            catch (Exception ex)
            {
                if (ex.Message == "Index was outside the bounds of the array.")
                {
                    cellValue = 0;
                    return false;
                }
                else
                {
                    throw ex;
                }
            }
        }

        /// <summary>
        /// Check if there is a value above current cell
        /// </summary>
        /// <param name="inputArray"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <param name="cellValue"></param>
        /// <returns></returns>
        private bool TryGetB(int[,] inputArray, int row, int col, out int cellValue)
        {
            try
            {
                cellValue = inputArray[row - 1, col];
                return true;
            }
            catch (Exception ex)
            {
                if (ex.Message == "Index was outside the bounds of the array.")
                {
                    cellValue = 0;
                    return false;
                }
                else
                {
                    throw ex;
                }
            }
        }

        /// <summary>
        /// Check if there is a value to the left and above the current cell
        /// </summary>
        /// <param name="inputArray"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <param name="cellValue"></param>
        /// <returns></returns>
        private bool TryGetC(int[,] inputArray, int row, int col, out int cellValue)
        {
            try
            {
                cellValue = inputArray[row - 1, col - 1];
                return true;
            }
            catch (Exception ex)
            {
                if (ex.Message == "Index was outside the bounds of the array.")
                {
                    cellValue = 0;
                    return false;
                }
                else
                {
                    throw ex;
                }
            }
        }

        #endregion

        /// <summary>
        /// Creates the Huffman table in a Dictionary object
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, string> BuildHuffmanTable()
        {
            Dictionary<string, string> huffmanTable = new Dictionary<string, string>();

            huffmanTable.Add("1", "0");
            huffmanTable.Add("00", "1");
            huffmanTable.Add("011", "-1");
            huffmanTable.Add("0100", "2");
            huffmanTable.Add("01011", "-2");
            huffmanTable.Add("010100", "3");
            huffmanTable.Add("0101011", "-3");
            huffmanTable.Add("01010100", "4");
            huffmanTable.Add("010101011", "-4");
            huffmanTable.Add("0101010100", "5");
            huffmanTable.Add("01010101011", "-5");
            huffmanTable.Add("010101010100", "6");
            huffmanTable.Add("0101010101011", "-6");

            return huffmanTable;
        }

        /// <summary>
        /// Take the compressed image and convert them to Huffman values
        /// </summary>
        /// <param name="compressedImage"></param>
        /// <param name="huffmanTable"></param>
        /// <returns></returns>
        private List<string> HuffmanEncode(int[,] compressedImage, Dictionary<string, string> huffmanTable, out int compressedSizeInBits)
        {
            compressedSizeInBits = 0;

            List<string> huffmanCodeList = new List<string>();

            // Loop through rows
            for (int row = 0; row < compressedImage.GetLength(0); row++)
            {
                StringBuilder rowOfHuffmanCodes = new StringBuilder();
                rowOfHuffmanCodes.Clear();

                // Loop through columns
                for (int col = 0; col < compressedImage.GetLength(1); col++)
                {
                    int valueToEncode = compressedImage[row, col];
                    string encodedValue;

                    // Check the Huffman table to see if the value exists
                    if (huffmanTable.ContainsValue(valueToEncode.ToString()))
                    {
                        // If it exists, append it to the output list
                        encodedValue = huffmanTable.FirstOrDefault(x => x.Value == valueToEncode.ToString()).Key;
                        //encodedValue = huffmanTable[valueToEncode.ToString()];
                    }
                    else
                    {
                        // If it doesn't exist, then convert the value to a binary
                        encodedValue = Convert.ToString(valueToEncode, 2).PadLeft(8, '0');
                    }

                    rowOfHuffmanCodes.Append(encodedValue);
                }

                compressedSizeInBits += rowOfHuffmanCodes.ToString().Length;

                huffmanCodeList.Add(rowOfHuffmanCodes.ToString());
            }

            return huffmanCodeList;
        }


        private int[,] HuffmanDecode(List<string> huffmanEncodedImage, Dictionary<string, string> huffmanTable)
        {
            // Create the output array. The no. of rows will be the same as in huffmanEncodedImage. The no. of columns will need to be calculated.
            int[,] output = new int[huffmanEncodedImage.Count, 4];

            // Get the first row from the list of binary strings
            string firstRow = huffmanEncodedImage[0];
            // Get the first byte, this value is not a Huffman value but rather an actual value
            string firstByte = firstRow.Substring(0, 8);
            // Covert that byte into an int
            int firstValue = Convert.ToInt32(firstByte, 2);
            // Write the first value to the output array
            output[0, 0] = firstValue;

            // Remove this first byte
            huffmanEncodedImage[0] = firstRow.Substring(8, firstRow.Length - 8);

            int rowNumber = 0;
            int colNumber = 1; // Start at column 1 since we already filled in the first value

            foreach (string row in huffmanEncodedImage)
            {
                string key = String.Empty;

                foreach (char bit in row)
                {
                    key += bit.ToString();

                    if (huffmanTable.ContainsKey(key))
                    {
                        output[rowNumber, colNumber] = int.Parse(huffmanTable[key]);

                        // Value found, increment column index
                        colNumber++;
                        key = String.Empty;
                    }
                    else
                    {

                    }
                }

                rowNumber++;
                colNumber = 0; // Reset column for output
            }

            return null;
        }
    }
}
