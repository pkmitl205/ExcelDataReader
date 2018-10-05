using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ExcelDataReader;
using System.Diagnostics;
using ZedGraph;

namespace DisplayChartExcel
{
    public partial class Form1 : Form
    {
        private DataSet ds;

        public Form1()
        {
            InitializeComponent();
            // Create ZadGraph 
            CreateChart(zedGraphControl1);

        }

        // Create Chart 
        public void CreateChart(ZedGraphControl zgc)
        {
            GraphPane myPane = zgc.GraphPane;
            // Set the titles and axis labels
            myPane.Title.Text = "Demonstration of Dual Y Graph";
            myPane.XAxis.Title.Text = "Number";
            myPane.YAxis.Title.Text = "Value";
            // Make up some data points 
            PointPairList list = new PointPairList();
            // Generate a red curve with diamond symbols, and "Alpha" in the legend
            LineItem myCurve = myPane.AddCurve("Data",
               list, Color.Red, SymbolType.Diamond);
            // Calculate the Axis Scale Ranges
            zgc.AxisChange();
        }

        /*
        public static void getvalues(dataset dataset, string sheetname)
        {
            foreach (datarow row in dataset.tables[sheetname].rows)
            {
                foreach (var value in row.itemarray)
                {
                    console.writeline("{0}, {1}", value, value.gettype());
                }
            }
        }
        */

        private static IList<string> GetTablenames(DataTableCollection tables)
        {
            var tableList = new List<string>();
            foreach (var table in tables)
            {
                tableList.Add(table.ToString());
            }

            return tableList;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var result = openFileDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                textBox1.Text = openFileDialog1.FileName;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var extension = Path.GetExtension(textBox1.Text).ToLower();

            using (var stream = new FileStream(textBox1.Text, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var sw = new Stopwatch();
                sw.Start();
                IExcelDataReader reader = null;
                if (extension == ".xls")
                {
                    reader = ExcelReaderFactory.CreateBinaryReader(stream);
                }
                else if (extension == ".xlsx")
                {
                    reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                }
                else if (extension == ".csv")
                {
                    reader = ExcelReaderFactory.CreateCsvReader(stream);
                }

                if (reader == null)
                    return;

                var openTiming = sw.ElapsedMilliseconds;
                // reader.IsFirstRowAsColumnNames = firstRowNamesCheckBox.Checked;
                using (reader)
                {
                    ds = reader.AsDataSet(new ExcelDataSetConfiguration()
                    {
                        UseColumnDataType = false,
                        ConfigureDataTable = (tableReader) => new ExcelDataTableConfiguration()
                        {
                            UseHeaderRow = firstRowNamesCheckBox.Checked
                        }
                    });
                }

                toolStripStatusLabel1.Text = "Elapsed: " + sw.ElapsedMilliseconds.ToString() + " ms (" + openTiming.ToString() + " ms to open)";

                var tablenames = GetTablenames(ds.Tables);
                sheetCombo.DataSource = tablenames;

                if (tablenames.Count > 0)
                    sheetCombo.SelectedIndex = 0;

                UpdateZedGraph(zedGraphControl1);

                // dataGridView1.DataSource = ds;
                // dataGridView1.DataMember
            }
        }

        private void UpdateZedGraph(ZedGraphControl zedGraphControl1)
        {
            // Make sure that the curvelist has at least one curve
            if (zedGraphControl1.GraphPane.CurveList.Count <= 0)
                return;

            // Get the first CurveItem in the graph
            LineItem curve = zedGraphControl1.GraphPane.CurveList[0] as LineItem;
            if (curve == null)
                return;

            // Get the PointPairList
            IPointListEdit list = curve.Points as IPointListEdit;
            // If this is null, it means the reference at curve.Points does not
            // support IPointListEdit, so we won't be able to modify it
            if (list == null)
                return;

            list.Clear();
            int maxrows = ds.Tables[0].Rows.Count;
            Console.WriteLine("ds.Tables[0].Rows.Count = {0}", maxrows);

            for (int i = 0 ; i < maxrows; i++)
            {
                var a = ds.Tables[0].Rows[i];
                double x = (double)a.ItemArray.GetValue(0);
                double y = (double)a.ItemArray.GetValue(1);
                list.Add(x, y);
            }
            // Make sure the Y axis is rescaled to accommodate actual data
            zedGraphControl1.AxisChange();
            // Force a redraw
            zedGraphControl1.Invalidate();
        }

        private void SelectTable()
        {
            var tablename = sheetCombo.SelectedItem.ToString();

            dataGridView1.AutoGenerateColumns = true;
            dataGridView1.DataSource = ds; // dataset
            dataGridView1.DataMember = tablename;

            // GetValues(ds, tablename);
        }


        private void sheetCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            SelectTable();
        }
    }
}
