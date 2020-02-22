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
        // These are calculated from the import file and then used to create the output file
        private int numberOfRows;
        private int numberOfColumns;

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

            // Read the image from file
            int[,] originalImageArray = ImportCSV(out int unCompressedSizeInBits);

            // Print the original image
            Print2DArray(sb, originalImageArray);

            for (int predictor = 1; predictor <= 7; predictor++)
            {
                int[,] compressedImage = CompressImage(predictor, originalImageArray);

                sb.Append("<div style='border:1px solid black;margin-bottom:25px;padding:10px 10px 10px 30px;'>");

                sb.Append(String.Format("<h3>Predictor {0}: {1} </h3>", predictor, String.Format(@"<img src='..\..\Images\Predictor{0}.png' align='middle'>", predictor)));

                // Huffman encode the compressed image
                List<string> huffmanEncodedImage = HuffmanEncode(compressedImage, huffmanTable, out int compressedSizeInBits);

                PrintHuffmanEncodedImage(sb, huffmanEncodedImage);

                // Decode the Huffman Encoded Image
                int[,] huffmanDecodedImage = HuffmanDecode(huffmanEncodedImage, huffmanTable);

                // Decompress Image
                int[,] deCompressedImage = DecompressImage(predictor, huffmanDecodedImage);

                PrintStats(sb, unCompressedSizeInBits, compressedSizeInBits);

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

            // Get the total number of rows in the import file
            string[] rows = csvData.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            this.numberOfRows = rows.Count();

            // Get the total number of columns/cells in the import file
            this.numberOfColumns = (rows[0].Split(',')).Count();

            int[,] array = new int[numberOfRows, this.numberOfColumns];

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
            // Should be 16x16x8 for the project test image
            inputFileSizeInBits = this.numberOfRows * this.numberOfColumns * 8;

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

        private void PrintHuffmanEncodedImage(StringBuilder sb, List<string> huffmanEncodedImage)
        {
            sb.Append("<p>");
            sb.Append("<h4>Compressed Binary Sequence:</h4>");

            // Print each row of the Huffman encoded image
            foreach (string row in huffmanEncodedImage)
            {
                sb.Append(row);
                sb.Append("<br>");
            }

            sb.Append("</p>");
            sb.Append("<br>");
        }

        private void PrintStats(StringBuilder sb, int unCompressedSizeInBits, int compressedSizeInBits)
        {
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
        }

        /// <summary>
        /// Compress an image
        /// </summary>
        /// <param name="predictor"></param>
        /// <param name="originalImage"></param>
        /// <returns></returns>
        private int[,] CompressImage(int predictor, int[,] originalImage)
        {
            if (originalImage == null)
            {
                return null;
            }

            int dim1 = originalImage.GetLength(0);
            int dim2 = originalImage.GetLength(1);

            int[,] compressedImage = new int[dim1, dim2];

            // Loop through rows
            for (int row = 0; row < dim1; row++)
            {
                // Loop through columns
                for (int col = 0; col < dim2; col++)
                {
                    // Check if A exists, if so, get it's value
                    bool a_exists = TryGetA(originalImage, row, col, out int a);
                    // Check if B exists, if so, get it's value
                    bool b_exists = TryGetB(originalImage, row, col, out int b);
                    // Check if C exists, if so, get it's value
                    bool c_exists = TryGetC(originalImage, row, col, out int c);

                    int yhat = 0;

                    switch (predictor)
                    {
                        case 1:
                            yhat = Predictor1(a_exists, a, b_exists, b, c_exists, c);
                            break;
                        case 2:
                            yhat = Predictor2(a_exists, a, b_exists, b, c_exists, c);
                            break;
                        case 3:
                            yhat = Predictor3(a_exists, a, b_exists, b, c_exists, c);
                            break;
                        case 4:
                            yhat = Predictor4(a_exists, a, b_exists, b, c_exists, c);
                            break;
                        case 5:
                            yhat = Predictor5(a_exists, a, b_exists, b, c_exists, c);
                            break;
                        case 6:
                            yhat = Predictor6(a_exists, a, b_exists, b, c_exists, c);
                            break;
                        case 7:
                            yhat = Predictor7(a_exists, a, b_exists, b, c_exists, c);
                            break;
                    }

                    compressedImage[row, col] = (int)(originalImage[row, col] - yhat);
                }
            }

            return compressedImage;
        }

        private int[,] DecompressImage(int predictor, int[,] compressedImage)
        {
            if (compressedImage == null)
            {
                return null;
            }

            int dim1 = compressedImage.GetLength(0);
            int dim2 = compressedImage.GetLength(1);

            int[,] decompressedImage = new int[dim1, dim2];
            
            int previousDecompressedValue = decompressedImage[0, 0];

            // Loop through rows
            for (int row = 0; row < dim1; row++)
            {
                // Loop through columns
                for (int col = 0; col < dim2; col++)
                {
                    // Check if A exists, if so, get it's value
                    bool a_exists = TryGetA(decompressedImage, row, col, out int a);
                    // Check if B exists, if so, get it's value
                    bool b_exists = TryGetB(decompressedImage, row, col, out int b);
                    // Check if C exists, if so, get it's value
                    bool c_exists = TryGetC(decompressedImage, row, col, out int c);

                    int yhat = 0;

                    switch (predictor)
                    {
                        case 1:
                            yhat = Predictor1(a_exists, a, b_exists, b, c_exists, c);
                            break;
                        case 2:
                            yhat = Predictor2(a_exists, a, b_exists, b, c_exists, c);
                            break;
                        case 3:
                            yhat = Predictor3(a_exists, a, b_exists, b, c_exists, c);
                            break;
                        case 4:
                            yhat = Predictor4(a_exists, a, b_exists, b, c_exists, c);
                            break;
                        case 5:
                            yhat = Predictor5(a_exists, a, b_exists, b, c_exists, c);
                            break;
                        case 6:
                            yhat = Predictor6(a_exists, a, b_exists, b, c_exists, c);
                            break;
                        case 7:
                            yhat = Predictor7(a_exists, a, b_exists, b, c_exists, c);
                            break;
                    }

                    decompressedImage[row, col] = yhat + compressedImage[row, col];

                }
            }

            return decompressedImage;
        }

        #region Predictors

        private int Predictor1(bool a_exists, int a, bool b_exists, int b, bool c_exists, int c)
        {
            // If 'a' exists, then x-hat = x-a
            if (a_exists)
            {
                return a;
            }
            else
            {
                if (b_exists)
                {
                    return b;
                }
                else
                {
                    // Use the same value
                    return 0;
                }
            }
        }

        private int Predictor2(bool a_exists, int a, bool b_exists, int b, bool c_exists, int c)
        {
            // If 'b' exists, then x-hat = x-a
            if (b_exists)
            {
                return b;
            }
            else
            {
                if (a_exists)
                {
                    return a;
                }
                else
                {
                    // Use the same value
                    return 0;
                }
            }
        }

        private int Predictor3(bool a_exists, int a, bool b_exists, int b, bool c_exists, int c)
        {
            // If 'c' exists, then x-hat = x-c
            if (c_exists)
            {
                return c;
            }
            else
            {
                if (a_exists)
                {
                    return a;
                }
                else if (b_exists)
                {
                    return b;
                }
                else
                {
                    // Use the same value
                    return 0;
                }
            }
        }

        private int Predictor4(bool a_exists, int a, bool b_exists, int b, bool c_exists, int c)
        {
            if (a_exists && b_exists && c_exists)
            {
                return (a + b - c);
            }
            else
            {
                if (a_exists)
                {
                    return a;
                }
                else if (b_exists)
                {
                    return b;
                }
                else
                {
                    // Use the same value
                    return 0;
                }
            }
        }

        private int Predictor5(bool a_exists, int a, bool b_exists, int b, bool c_exists, int c)
        {
            if (a_exists && b_exists && c_exists)
            {
                return (a + ((b - c) / 2));
            }
            else
            {
                if (a_exists)
                {
                    return a;
                }
                else if (b_exists)
                {
                    return b;
                }
                else
                {
                    // Use the same value
                    return 0;
                }
            }
        }

        private int Predictor6(bool a_exists, int a, bool b_exists, int b, bool c_exists, int c)
        {
            if (a_exists && b_exists && c_exists)
            {
                return (b + ((a - c) / 2));
            }
            else
            {
                if (a_exists)
                {
                    return a;
                }
                else if (b_exists)
                {
                    return b;
                }
                else
                {
                    // Use the same value
                    return 0;
                }
            }
        }

        private int Predictor7(bool a_exists, int a, bool b_exists, int b, bool c_exists, int c)
        {
            if (a_exists && b_exists)
            {
                return ((a + b) / 2);
            }
            else
            {
                if (a_exists)
                {
                    return a;
                }
                else if (b_exists)
                {
                    return b;
                }
                else
                {
                    // Use the same value
                    return 0;
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
        /// Convert compressed image with Huffman encoding
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
            // Create the output array. Use values we calculated when we read in the file.
            int[,] output = new int[this.numberOfRows, this.numberOfColumns];

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
                    // Build the key from the bits in the row until it becomes a legit Huffman value
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

            return output;
        }
    }
}
