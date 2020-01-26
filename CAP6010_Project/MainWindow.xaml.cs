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

            predictors.Add(new Predictor { Id = 0, Name = "Choose a Predictor" });
            predictors.Add(new Predictor { Id = 1, Name = "Predictor 1: ", Photo = "Images/Predictor1.png" });
            predictors.Add(new Predictor { Id = 2, Name = "Predictor 2: ", Photo = "Images/Predictor2.png" });
            predictors.Add(new Predictor { Id = 3, Name = "Predictor 3: ", Photo = "Images/Predictor3.png" });
            predictors.Add(new Predictor { Id = 4, Name = "Predictor 4: ", Photo = "Images/Predictor4.png" });
            predictors.Add(new Predictor { Id = 5, Name = "Predictor 5: ", Photo = "Images/Predictor5.png" });
            predictors.Add(new Predictor { Id = 6, Name = "Predictor 6: ", Photo = "Images/Predictor6.png" });
            predictors.Add(new Predictor { Id = 7, Name = "Predictor 7: ", Photo = "Images/Predictor7.png" });

            predictorsCombobox.ItemsSource = predictors;
            predictorsCombobox.SelectedIndex = 0;
        }


        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (predictorsCombobox.SelectedIndex == 0)
            {
                MessageBox.Show("Choose a Predictor", "Predictor", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int[,] values = ImportCSV();

            Dictionary<string, string> huffmanTable = BuildHuffmanTable();

            int[,] encodedWithDataTable = Encode(values, predictorsCombobox.SelectedIndex);
        }

        /// <summary>
        /// Encode with Predictor 1: x-hat = A
        /// </summary>
        /// <param name="inputArray">2D Input Array</param>
        /// <returns></returns>
        private int[,] Encode(int[,] inputArray, int predictor)
        {
            if (inputArray == null)
            {
                return null;
            }

            // Create the output array and make it the same size as the input array
            int[,] outputArray = new int[inputArray.GetLength(0), inputArray.GetLength(1)];

            // Loop through rows
            for (int row = 0; row < inputArray.GetLength(0); row++)
            {
                // Loop through columns
                for (int col = 0; col < inputArray.GetLength(1); col++)
                {
                    // Check if A exists, if so, get it's value
                    bool a_exists = TryGetA(inputArray, row, col, out int a);
                    bool b_exists = TryGetB(inputArray, row, col, out int b);
                    bool c_exists = TryGetC(inputArray, row, col, out int c);

                    // If 'a' exists, then x-hat = x-a
                    if (a_exists)
                    {
                        outputArray[row, col] = (int)(inputArray[row, col] - a);
                    }
                    else
                    {
                        if (b_exists)
                        {
                            // Row > 1, Col = 1.
                            outputArray[row, col] = (int)(inputArray[row, col] - b);
                        }
                        else
                        {
                            // Use the same value. Row = 1, Col = 1.
                            outputArray[row, col] = inputArray[row, col];
                        }
                    }
                }
            }

            return outputArray;
        }

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

        private int[,] ImportCSV()
        {
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

            return array;
        }

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
    }
}
