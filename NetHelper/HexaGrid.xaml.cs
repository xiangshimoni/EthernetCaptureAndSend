using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.ComponentModel;
using PacketDotNet;

namespace NetHelper
{
    /// <summary>
    /// HexaGrid.xaml 的交互逻辑
    /// </summary>
    public partial class HexaGrid : UserControl
    {        
        HexDataSet.Bytetable1DataTable oTable = new HexDataSet.Bytetable1DataTable();

        public string ASCIIString
        {
            get { return (string)GetValue(ASCIIStringProperty); }
            set { SetValue(ASCIIStringProperty, value); }
        }
        
        public static readonly DependencyProperty ASCIIStringProperty =
            DependencyProperty.Register("ASCIIString", typeof(string), typeof(HexaGrid), new UIPropertyMetadata(""));

        public byte[] Bytes { get; set; }

        public void Bytes2Grid(byte[] bytes)
        {
            oTable.Rows.Clear();

            int j = 0;
            for (int i = 0; i < bytes.Length; i += 16)
            {
                string[] myStringArr = new string[17];

                myStringArr[0] = j.ToString();
                for (int k = 0; k <= 15 & i + k < bytes.Length; k++)
                {
                    myStringArr[k + 1] = bytes[i + k].ToString("X2");
                }
                oTable.Rows.Add(myStringArr);
                j++;
            }
            dataGrid1.ItemsSource = null;
            dataGrid1.ItemsSource = oTable;
            #region 生成ASCII码
            geneAcsii(bytes);
            #endregion
            Bytes = bytes;
        }

        private void geneAcsii(byte[] bytes)
        {
            ASCIIString = Encoding.ASCII.GetString(bytes);
        }

        public HexaGrid()
        {
            InitializeComponent();
        }

        public delegate void SelectionChangedEventHandler(object sender, SelectedCellsChangedEventArgs e);
        public event SelectionChangedEventHandler SelectionChanged;

        private void dataGrid1_SelectionChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if (SelectionChanged != null)
            {
                SelectionChanged(sender, e);
            }
        }


        public delegate void CellEditEndingEventHandler(object sender, DataGridCellEditEndingEventArgs e);
        public event CellEditEndingEventHandler CellEditEnding;
        private void dataGrid1_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (CellEditEnding != null)
            {
                CellEditEnding(sender, e);
            }
        }          
    }
}
