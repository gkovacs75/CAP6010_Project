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
            predictors.Add(new Predictor { Id = 1, Name = "Predictor 2: ", Photo = "Images/Predictor2.png" });
            predictors.Add(new Predictor { Id = 1, Name = "Predictor 3: ", Photo = "Images/Predictor3.png" });
            predictors.Add(new Predictor { Id = 1, Name = "Predictor 4: ", Photo = "Images/Predictor4.png" });
            predictors.Add(new Predictor { Id = 1, Name = "Predictor 5: ", Photo = "Images/Predictor5.png" });
            predictors.Add(new Predictor { Id = 1, Name = "Predictor 6: ", Photo = "Images/Predictor6.png" });
            predictors.Add(new Predictor { Id = 1, Name = "Predictor 7: ", Photo = "Images/Predictor7.png" });

            predictorsCombobox.ItemsSource = predictors;
            predictorsCombobox.SelectedIndex = 0;
        }


        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            //if (predictorsCombobox.SelectedIndex == 0)
            //{
            //    MessageBox.Show("Choose a Predictor");
            //    return;
            //}

            //DataTable dataTable = ImportCSV();
            byte[,] values = ImportCSV2();

            Dictionary<string, string> huffmanTable = BuildHuffmanTable();

            //DataTable encodedWithDataTable = EncodeWithPredictor1(dataTable);
            byte[,] encodedWithDataTable = EncodeWithPredictor1(values);
        }

        /// <summary>
        /// Encode with Predictor 1: x-hat = A
        /// </summary>
        /// <param name="inputArray">2D Input Array</param>
        /// <returns></returns>
        private byte[,] EncodeWithPredictor1(byte[,] inputArray)
        {
            // Create the output array and make it the same size as the input array
            byte[,] outputArray = new byte[inputArray.GetLength(0), inputArray.GetLength(1)];

            // Loop through rows
            for (int row = 0; row < inputArray.GetLength(0); row++)
            {
                // Loop through columns
                for (int col = 0; col < inputArray.GetLength(1); col++)
                {
                    // Check if A exists, if so, get it's value
                    bool a_exists = TryGetA(inputArray, row, col, out byte a);

                    // If a exists, then x-hat = x-a
                    if (a_exists)
                    {
                        outputArray[row, col] = (byte)(inputArray[row, col] - a);
                    }
                    else
                    {
                        // Use the same value
                        outputArray[row, col] = inputArray[row, col];
                    }
                }
            }

            return outputArray;
        }

        private bool TryGetA(byte[,] inputArray, int row, int col, out byte cellValue)
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

        private bool TryGetB(byte[,] inputArray, int row, int col, out byte cellValue)
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

        private bool TryGetC(byte[,] inputArray, int row, int col, out byte cellValue)
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

        private DataTable EncodeWithPredictor1(DataTable dataTable)
        {
            foreach (DataRow row in dataTable.Rows)
            {
                foreach (DataColumn col in dataTable.Columns)
                {
                    object callValue = row[col];
                }
            }

            return null;
        }

        private DataTable ImportCSV()
        {
            string filePath = @"../../Files/inputfile1.csv";

            DataTable dataTable = new DataTable();
            dataTable.Clear();

            string csvData = File.ReadAllText(filePath);

            int rowIndex = 0;

            foreach (string row in csvData.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
            {
                if (!string.IsNullOrEmpty(row))
                {
                    dataTable.Rows.Add();

                    int columnIndex = 0;

                    foreach (string cell in row.Split(','))
                    {
                        if (rowIndex == 0)
                        {
                            dataTable.Columns.Add("", typeof(byte));
                        }

                        dataTable.Rows[dataTable.Rows.Count - 1][columnIndex] = byte.Parse(cell);

                        columnIndex++;
                    }

                    rowIndex++;
                }
            }

            //myDataGrid.ItemsSource = dt.DefaultView;

            return dataTable;
        }

        private byte[,] ImportCSV2()
        {
            string filePath = @"../../Files/inputfile1.csv";

            string csvData = File.ReadAllText(filePath);

            int rowIndex = 0;

            string[] rows = csvData.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            int numCells = (rows[0].Split(',')).Count();

            byte[,] array = new byte[rows.Count(), numCells];

            foreach (string row in rows)
            {
                string[] cells = row.Split(',');

                int columnIndex = 0;

                foreach (string cell in cells)
                {
                    array[rowIndex, columnIndex] = byte.Parse(cell);

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
