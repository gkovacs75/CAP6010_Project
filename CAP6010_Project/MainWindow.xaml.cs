using System;
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
        List<Predictor> predictors = new List<Predictor>();

        public MainWindow()
        {
            InitializeComponent();
        }


        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("<!DOCTYPE html><html><body>");
            sb.Append("<h1>CAP 6010 Project Results</h1>");
            sb.Append("<h3>Gabor Kovacs</h3>");
            sb.Append("<hr>");


            Dictionary<string, string> huffmanTable = BuildHuffmanTable();

            sb.Append("<h3>Original Image:</h3>");

            int[,] rawValues = ImportCSV(out int unCompressedSizeInBits);

            Print2DArray(sb, rawValues);

            List<int[,]> listOfCompressedImages = CompressImage(rawValues);

            for (int predictor = 1; predictor <= 7; predictor++)
            {
                sb.Append("<p>");

                sb.Append(String.Format("<h3>Predictor {0}: {1} </h3>", predictor, String.Format(@"<img src='..\..\Images\Predictor{0}.png' align='middle'>", predictor)));

                List<string> binaryStrings = ConvertToHuffmanCode(listOfCompressedImages[predictor - 1], huffmanTable, out int compressedSizeInBits);

                sb.Append("<p>");
                sb.Append("<h4>Compressed Binary Sequence:</h4>");
                foreach (string binaryString in binaryStrings)
                {
                    sb.Append(binaryString);
                    sb.Append("<br>");
                }

                sb.Append("</p>");
                sb.Append("<br>");

                float compressionRatio = (float)unCompressedSizeInBits / (float)compressedSizeInBits;
                float bitsPerPixel = 8 / compressionRatio;

                sb.Append("<p>");
                sb.Append("Compression Ratio: ");
                sb.Append(((float)unCompressedSizeInBits).ToString() + " / " + ((float)compressedSizeInBits).ToString() + " = " + compressionRatio.ToString());
                sb.Append("<br>");
                sb.Append("Bits/Pixel: ");
                sb.Append("8 / " + compressionRatio.ToString() + " = " + bitsPerPixel.ToString());
                sb.Append("<br>");
                sb.Append("RMS: ");
                sb.Append("[rms value holder]");
                sb.Append("<br>");
                sb.Append("</p>");

                sb.Append("</p>");
            }

            sb.Append("</body></html>");

            File.WriteAllText(@"output.html", sb.ToString());

            Application.Current.Shutdown();
        }

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

        }

        private void UsePredictor6(bool a_exists, int a, bool b_exists, int b, bool c_exists, int c, int[,] inputArray, int[,] outputArray, int row, int col)
        {

        }

        private void UsePredictor7(bool a_exists, int a, bool b_exists, int b, bool c_exists, int c, int[,] inputArray, int[,] outputArray, int row, int col)
        {

        }


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

        /// <summary>
        /// Creates the Huffman table in a Dictionary object
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, string> BuildHuffmanTable()
        {
            Dictionary<string, string> huffmanTable = new Dictionary<string, string>();

            huffmanTable.Add("0", "1");
            huffmanTable.Add("1", "00");
            huffmanTable.Add("-1", "011");
            huffmanTable.Add("2", "0100");
            huffmanTable.Add("-2", "01011");
            huffmanTable.Add("3", "010100");
            huffmanTable.Add("-3", "0101011");
            huffmanTable.Add("4", "01010100");
            huffmanTable.Add("-4", "010101011");
            huffmanTable.Add("5", "0101010100");
            huffmanTable.Add("-5", "01010101011");
            huffmanTable.Add("6", "010101010100");
            huffmanTable.Add("-6", "0101010101011");

            return huffmanTable;
        }

        /// <summary>
        /// Take the compressed integers and convert them to Huffman values
        /// </summary>
        /// <param name="convertedIntegers"></param>
        /// <param name="huffmanTable"></param>
        /// <returns></returns>
        private List<string> ConvertToHuffmanCode(int[,] convertedIntegers, Dictionary<string, string> huffmanTable, out int compressedSizeInBits)
        {
            compressedSizeInBits = 0;

            List<string> huffmanCodeList = new List<string>();

            // Loop through rows
            for (int row = 0; row < convertedIntegers.GetLength(0); row++)
            {
                StringBuilder rowOfHuffmanCodes = new StringBuilder();
                rowOfHuffmanCodes.Clear();

                // Loop through columns
                for (int col = 0; col < convertedIntegers.GetLength(1); col++)
                {
                    int valueToEncode = convertedIntegers[row, col];
                    string encodedValue;

                    // Check the Huffman table to see if the code exists
                    if (huffmanTable.ContainsKey(valueToEncode.ToString()))
                    {
                        // If it exists, append it to the output list
                        encodedValue = huffmanTable[valueToEncode.ToString()];
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
    }
}
