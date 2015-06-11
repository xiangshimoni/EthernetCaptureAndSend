/*
 * 数据包生成和发送 
 * 支持IP/ICMP/IGMP数据包
 * 支持：开源SharpPcap
 * 存储使用二进制格式，扩展名.pkt
 * 过滤器格式参考：http://www.winpcap.org/docs/docs_412/html/group__language.html
 * 中文：http://www.ferrisxu.com/WinPcap/html/group__language.html
 * 
*/

//TODO 设置网卡混杂模式选择
//TODO 数据包比较
//TODO 捕获数据包复制到发送列表
//TODO 单例运行，双击pkt加载

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PacketDotNet;
using System.ComponentModel;
using System.Collections.ObjectModel;
using SharpPcap;
using System.Threading;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using Microsoft.Win32;
using Be.Windows.Forms;

namespace NetHelper
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members

        /// <summary>
        /// Raised when a property on this object has a new value.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises this object's PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">The property that has a new value.</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                handler(this, e);
            }
        }

        #endregion // INotifyPropertyChanged Members

        private readonly int HEX_WIDTH = 16;

        private ObservableCollection<DisplayPacket> _sendEthernetPacketList = new ObservableCollection<DisplayPacket>();
        public ObservableCollection<DisplayPacket> SendEthernetPacketList 
        {
            get
            {
                return _sendEthernetPacketList;
            }
            set
            {
                _sendEthernetPacketList = value;
                OnPropertyChanged("SendEthernetPacketList");
            }
        }

        private ObservableCollection<DisplayPacket> _captureEthernetPacketList = new ObservableCollection<DisplayPacket>();
        public ObservableCollection<DisplayPacket> CaptureEthernetPacketList
        {
            get
            {
                return _captureEthernetPacketList;
            }
            set
            {
                _captureEthernetPacketList = value;
                OnPropertyChanged("CaptureEthernetPacketList");
            }
        }

        private ICaptureDevice _selectedSendICaptureDevice;
        public ICaptureDevice SelectedSendICaptureDevice
        {
            set
            {
                _selectedSendICaptureDevice = value;
                OnPropertyChanged("SelectedSendICaptureDevice");
            }
            get
            {
                return _selectedSendICaptureDevice;
            }
        }

        private ICaptureDevice _selectedCaptureICaptureDevice;
        public ICaptureDevice SelectedCaptureICaptureDevice
        {
            set
            {
                _selectedCaptureICaptureDevice = value;
                OnPropertyChanged("SelectedCaptureICaptureDevice");
            }
            get
            {
                return _selectedCaptureICaptureDevice;
            }
        }

        public ObservableCollection<String> Devices 
        {
            get
            {
                CaptureDeviceList devices = SharpPcap.CaptureDeviceList.Instance;
                ObservableCollection<String> device_strs = new ObservableCollection<string>();
                foreach (var item in devices)
	            {
		            device_strs.Add(item.Description);
	            }
                return device_strs;
            }
        }
        
        private DisplayPacket _selectedEthernetPacket;
        public DisplayPacket SelectedEthernetPacket
        {
            set 
            {
                _selectedEthernetPacket = value;
                #region 显示16进制
                if (value != null)
                {
                    hexDataGrid.Bytes2Grid(value.EthernetPacket.BytesHighPerformance.Bytes);
                    PacketInfo = decodePacket(value.EthernetPacket);
                    MemoryStream stream = new MemoryStream();
                    stream.Write(value.EthernetPacket.BytesHighPerformance.Bytes, 0, value.EthernetPacket.BytesHighPerformance.Bytes.Length);
                    loadHex(stream);
                }
                #endregion
                OnPropertyChanged("SelectedEthernetPacket");
            }
            get
            {
                return _selectedEthernetPacket;
            }
        }

        private IList _selectedEthernetPacketList;

        /// <summary>
        /// 发送时间间隔
        /// </summary>
        private int _timeInterval = 1000;
        public int TimeInterval 
        {
            get
            {
                return _timeInterval;
            }
            set
            {
                _timeInterval = value;
                OnPropertyChanged("TimeInterval");
            }
        }

        private String _packetInfo;
        public String PacketInfo 
        {
            get
            {
                return _packetInfo;
            }
            set
            {
                _packetInfo = value;
                OnPropertyChanged("PacketInfo");
            }
        }

        public string highlightFilterString = "";

        String decodePacket(EthernetPacket packet)
        {
            String ret = "";
            ret = String.Format("大小：{0}\r\n协议类型：{1}\r\n源mac地址：{2}\r\n目的mac地址：{3}\r\n",
                packet.BytesHighPerformance.Length,
                packet.Type,
                packet.SourceHwAddress,
                packet.DestinationHwAddress);
            if (packet.PayloadPacket is IPv4Packet)
            {
                IpPacket ipv4Packet = packet.PayloadPacket as IPv4Packet;
                ret += String.Format("大小：{0}\r\n协议类型：{1}\r\n源ip地址：{2}\r\n目的ip地址：{3}\r\n",
                ipv4Packet.BytesHighPerformance.Length,
                EthernetPacketType.IpV4,
                ipv4Packet.SourceAddress,
                ipv4Packet.DestinationAddress);
            }
            return ret;
        }

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;

            //if (SendEthernetPacketList != null)
            //{
            //    SendEthernetPacketList.Add(new DisplayPacket(EditPacketWindow.DefaultPackets()));
            //}
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            EditPacketWindow editor = new EditPacketWindow(SendEthernetPacketList);
            editor.ShowDialog();            
        }

        private void Capture()
        {
            /*
            // open the capture file
            var dev = new SharpPcap.OfflineCaptureDevice("SomeCapturedPackets.pcap");

            // read the next packet
            var rawCapture = dev.GetNextPacket();

            // parse the packet
            Packet p = Packet.ParsePacket(rawCapture.LinkLayerType, rawCapture.Data);

            // print out the packet contents
            Console.WriteLine(p);
             * */
        }

        bool isSending = false;
        BackgroundWorker worker;
        private void SendBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!isSending)
            {
                if (isSendForeverSet)
                {
                    try
                    {
                        worker = new BackgroundWorker();
                        //指定提供进度通知  
                        worker.WorkerReportsProgress = true;
                        //提供中断功能
                        worker.WorkerSupportsCancellation = true;
                        //线程的主要功能是处理事件  
                        //开启线程执行工作
                        worker.DoWork += new DoWorkEventHandler(worker_DoWork);
                        // Specify the function to use to handle progress  
                        //指定使用的功能来处理进度
                        worker.ProgressChanged += new ProgressChangedEventHandler(worker_ProgressChanged);
                        //进度条结束完成工作  
                        //1.工作完成  
                        //2.工作错误异常  
                        //3.取消工作 
                        worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
                        //如果进度条需要参数  
                        //调用System.ComponentModel.BackgroundWorker.RunWorkerAsync  
                        //传入你的参数至System.ComponentModel.BackgroundWorker.DoWork   
                        //提取参数  
                        //System.ComponentModel.DoWorkEventArgs.Argument   
                        int count = Int32.Parse(sendtimesTextBox.Text);
                        worker.RunWorkerAsync(new RunWorkerArgument(count, TimeInterval));
                        SendBtn.Content = "停止发送";
                        isSending = true;
                    }
                    catch (Exception e1)
                    {
                        MessageBox.Show(e1.Message);
                        SendBtn.Content = "发送全部";
                        isSending = false;
                    }
                }
                else 
                {
                    try
                    {
                        if (SendEthernetPacketList.Count == 0)
                        {
                            MessageBox.Show("请先创建数据包");
                            return;
                        }
                        Send();
                    }
                    catch (Exception e1)
                    {
                        MessageBox.Show(e1.Message);
                    }
                }
            }
            else//正在发送
            {
                if (worker.IsBusy)
                {
                    worker.CancelAsync();
                    SendBtn.Content = "发送";
                    isSending = false;
                }                
            }
        }

        bool isSelectedSending = false;
        BackgroundWorker sendSelectedWorker;
        /// <summary>
        /// 发送已选择的
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SendSelectedBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!isSelectedSending)
            {
                if (isSendForeverSet)
                {
                    try
                    {
                        sendSelectedWorker = new BackgroundWorker();
                        sendSelectedWorker.WorkerReportsProgress = true;
                        sendSelectedWorker.WorkerSupportsCancellation = true;
                        sendSelectedWorker.DoWork += new DoWorkEventHandler(worker_sendSelectedDoWork);
                        sendSelectedWorker.ProgressChanged += new ProgressChangedEventHandler(worker_ProgressChanged);
                        sendSelectedWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(sendSelectedWorker_RunWorkerCompleted);
                        int count = Int32.Parse(sendtimesTextBox.Text);
                        sendSelectedWorker.RunWorkerAsync(new RunWorkerArgument(count, TimeInterval));
                        SendSelectedBtn.Content = "停止发送选中包";
                        isSelectedSending = true;
                    }
                    catch (Exception e1)
                    {
                        MessageBox.Show(e1.Message);
                        SendSelectedBtn.Content = "发送选中包";
                        isSelectedSending = false;
                    }
                }
                else
                {
                    try
                    {
                        if (sendPacketsDataGrid.SelectedItems.Count == 0)
                        {
                            MessageBox.Show("请先选择数据包");
                            return;
                        }
                        SendSelected();
                    }
                    catch (Exception e1)
                    {
                        MessageBox.Show(e1.Message);
                    }
                }
            }
            else//正在发送
            {
                if (sendSelectedWorker.IsBusy)
                {
                    sendSelectedWorker.CancelAsync();
                    SendSelectedBtn.Content = "发送选中包";
                    isSelectedSending = false;
                }
            }
        }       

        void SendSelected()
        {
            if (_selectedEthernetPacketList == null)
            {
                throw new Exception("未选中数据包"); 
            }
            var packetList = _selectedEthernetPacketList;
            if(SelectedSendICaptureDevice == null)
            {
                throw new Exception("发送网卡未打开");
            }
            if (packetList.Count > 0)
            {
                foreach (var item in packetList)
                {
                    SelectedSendICaptureDevice.SendPacket((item as DisplayPacket).EthernetPacket.BytesHighPerformance.Bytes,
                        (item as DisplayPacket).EthernetPacket.BytesHighPerformance.Bytes.Length);
                }
            }
        }

        void Send()
        {
            if (SelectedSendICaptureDevice != null)
            {
                foreach (var item in SendEthernetPacketList)
                {         
                    SelectedSendICaptureDevice.SendPacket(item.EthernetPacket.BytesHighPerformance.Bytes, item.EthernetPacket.BytesHighPerformance.Bytes.Length);                    
                }
            }
        }

        #region 异步任务事件

        void sendSelectedWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                //label.Text = "Cancelled!取消";
                //TODO 执行取消操作
                
            }
            else if (e.Error != null)
            {
                //label.Text = "Error!异常";
            }
            else
            {
                //MessageBox.Show("处理结束");
            }
            SendSelectedBtn.Content = "发送选中包";
            isSelectedSending = false;
        }

        //单线程执行工作
        private void worker_sendSelectedDoWork(object sender, DoWorkEventArgs e)
        {
            var arg = (RunWorkerArgument)e.Argument;
            try
            {
                for (int i = 0; i < arg.RunTimes; i++)
                {
                    if (sendSelectedWorker != null)
                    {
                        if (sendSelectedWorker.CancellationPending)
                        {
                            e.Cancel = true;
                            return;
                        }
                    }
                    SendSelected();
                    sendSelectedWorker.ReportProgress((int)((double)(i+1) / arg.RunTimes * 100));
                    Thread.Sleep(arg.TimeInterval);
                };
            }
            catch (Exception e1)
            {
                MessageBox.Show(e1.Message);
                e.Cancel = true;
            }
        }

        private int _progressValue;
        public int ProgressValue 
        {
            get
            {
                return _progressValue;
            }
            set
            {
                _progressValue = value;
                OnPropertyChanged("ProgressValue");
            }
        }

        //单线程执行工作
        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {           
            var arg = (RunWorkerArgument)e.Argument;
            try
            {
                if (SendEthernetPacketList.Count == 0)
                {
                    MessageBox.Show("请先创建数据包");
                    return;
                }
                for (int i = 0; i < arg.RunTimes; i++)
                {
                    if (worker != null)
                    {
                        if (worker.CancellationPending)
                        {
                            e.Cancel = true;
                            return;
                        }
                    }
                    Send();
                    worker.ReportProgress((int)((double)(i + 1) / arg.RunTimes * 100));
                    Thread.Sleep(arg.TimeInterval);
                };
            }
            catch (Exception e1)
            {
                MessageBox.Show(e1.Message);
                e.Cancel = true;
            }
        }

        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //TODO 处理进度
            ProgressValue = e.ProgressPercentage;
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            
            if (e.Cancelled)
            {
                //label.Text = "Cancelled!取消";
                //TODO 执行取消操作

                //MessageBox.Show("处理取消");
            }
            else if (e.Error != null)
            {
                //label.Text = "Error!异常";
            }
            else
            {               
                //MessageBox.Show("处理结束");
            }
            SendBtn.Content = "发送全部";
            isSending = false;
        }        
        #endregion

        /// <summary>
        /// 选中发送列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sendPacketsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {            
            var packet = (sender as DataGrid).SelectedItem as DisplayPacket;
            if (packet == null)
            {
                SelectedEthernetPacket = null;
            }
            else
            {
                SelectedEthernetPacket = packet;
            }
            if ((sender as DataGrid).SelectedItems.Count > 0)
            {
                _selectedEthernetPacketList = (sender as DataGrid).SelectedItems;
            }
        }

        /// <summary>
        /// 选中捕获列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void capturePacketsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {                        
            var packet = (sender as DataGrid).SelectedItem as DisplayPacket;
            if (packet == null)
            {
                SelectedEthernetPacket = null;
            }
            else
            {
                SelectedEthernetPacket = packet;
            }            
        }   

        private void sendDevicesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OpenCloseSendDeviceBtn.IsChecked == true)
            {
                OpenCloseSendDeviceBtn_Unchecked(null, null);
                OpenCloseSendDeviceBtn.IsChecked = false;
            }
            if (sendDevicesComboBox.HasItems)
            {
                if (sendDevicesComboBox.SelectedIndex >= 0)
                {
                    SelectedSendICaptureDevice = SharpPcap.CaptureDeviceList.Instance[sendDevicesComboBox.SelectedIndex];
                }
            }
        }

        private void captureDevicesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OpenCloseCaptureDeviceBtn.IsChecked == true)
            {
                OpenCloseCaptureDeviceBtn_Unchecked(null, null);
                OpenCloseCaptureDeviceBtn.IsChecked = false;
            }
            if (captureDevicesComboBox.HasItems)
            {
                if (captureDevicesComboBox.SelectedIndex >= 0)
                {
                    SelectedCaptureICaptureDevice = SharpPcap.CaptureDeviceList.Instance[captureDevicesComboBox.SelectedIndex];
                }
            }
        }

        private void OpenCloseSendDeviceBtn_Checked(object sender, RoutedEventArgs e)
        {
            if (SelectedSendICaptureDevice != null)
            {
                //device.Open();
                SelectedSendICaptureDevice.Open(DeviceMode.Promiscuous);
                OpenCloseSendDeviceBtn.Content = "关闭发送网卡";
            }
        }

        private void OpenCloseSendDeviceBtn_Unchecked(object sender, RoutedEventArgs e)
        {
            if(SelectedSendICaptureDevice!=null)
            {
                SelectedSendICaptureDevice.Close();
                OpenCloseSendDeviceBtn.Content = "打开发送网卡";  
            }
        }

        private void OpenCloseCaptureDeviceBtn_Checked(object sender, RoutedEventArgs e)
        {
            if (SelectedCaptureICaptureDevice != null)
            {
                try
                {
                    //捕获过滤
                    highlightFilterString = highlightFilterTextBox.Text;

                    SelectedCaptureICaptureDevice.Open();
                    string filter = filterTextBox.Text;//过滤器字符串
                    SelectedCaptureICaptureDevice.Filter = filter;
                    SelectedCaptureICaptureDevice.OnPacketArrival += new PacketArrivalEventHandler(SelectedCaptureICaptureDevice_OnPacketArrival);
                    SelectedCaptureICaptureDevice.StartCapture();
                }
                catch (Exception e1)
                {
                    MessageBox.Show(e1.Message);
                    OpenCloseCaptureDeviceBtn.IsChecked = false;
                    return;
                }
                OpenCloseCaptureDeviceBtn.Content = "关闭捕获网卡";
            }
        }

        void SelectedCaptureICaptureDevice_OnPacketArrival(object sender, CaptureEventArgs e)
        {            
            Packet packet = Packet.ParsePacket(LinkLayers.Ethernet, e.Packet.Data);
            this.Dispatcher.BeginInvoke((Action)delegate()
            {
                //captureDataTextBox.Text += packet.ToString() + "\r\n";
                if (packet is EthernetPacket)
                {
                    CaptureEthernetPacketList.Add(new DisplayPacket(packet as EthernetPacket) { Timeval = e.Packet.Timeval });
                    #region 滚动到最后
                    var border = VisualTreeHelper.GetChild(capturePacketsDataGrid, 0) as Decorator;
                    if (border != null)
                    {
                        var scroll = border.Child as ScrollViewer;
                        if (scroll != null) scroll.ScrollToEnd();
                    }
                    #endregion
                }                
            });
        }

        private void OpenCloseCaptureDeviceBtn_Unchecked(object sender, RoutedEventArgs e)
        {
            if (SelectedCaptureICaptureDevice != null)
            {
                SelectedCaptureICaptureDevice.StopCapture();
                SelectedCaptureICaptureDevice.OnPacketArrival -= new PacketArrivalEventHandler(SelectedCaptureICaptureDevice_OnPacketArrival);
                SelectedCaptureICaptureDevice.Close();
                OpenCloseCaptureDeviceBtn.Content = "打开捕获网卡";
            }
        }

        bool isSendForeverSet = false;
        private void isSendForeverSetCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            isSendForeverSet = true;
        }

        private void isSendForeverSetCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            isSendForeverSet = false;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (SelectedCaptureICaptureDevice != null)
            {     
                SelectedCaptureICaptureDevice.Close();
            }
        }

        private void applyFilterBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedCaptureICaptureDevice != null)
            {
                if (SelectedCaptureICaptureDevice.Started)
                {
                    string filter = filterTextBox.Text;//过滤器字符串
                    SelectedCaptureICaptureDevice.Filter = filter;
                }
            }
        }

        private void hexDataGrid_SelectionChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            /*
            if ((sender as DataGrid).SelectedItem != null)
            {
                DataRowView items = (DataRowView)(sender as DataGrid).SelectedItem;
                var values = items.Row.ItemArray;
                
                string str;
                byte[] buff = new byte[values.Length];
                Int16 index = 0;
                for (int i = 0; i < values.Length; i += 2)
                {
                    if (values[i].ToString().Equals(""))
                    {
                        buff[index] = 0;
                    }
                    else
                    {
                        buff[index] = Convert.ToByte(values[i].ToString(), 16);
                    }
                    ++index;
                }
                str = Encoding.Default.GetString(buff);
                hexStrTextBox.Text = str;
            }
            */
            var control = (sender as DataGrid);
            int rowindex = 0;
            int colindex = 1;
            if (control.SelectedCells.Count > 0)
            {
                rowindex = control.Items.IndexOf(control.SelectedCells[0].Item);
                colindex = control.SelectedCells[0].Column.DisplayIndex - 1;
                hexStrTextBox.Focus();
                hexStrTextBox.Select(16 * rowindex + colindex, control.SelectedCells.Count);
            }
        }
        #region 菜单点击
        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            (new AboutWindow()).ShowDialog();
        }
        #endregion

        private void sendPacketsDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = e.Row.GetIndex() + 1;
        }

        /// <summary>
        /// 单元格编辑
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void hexDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {            
            //提交编辑            
            if (e.EditAction == DataGridEditAction.Commit)
            {
                try
                {
                    var control = (sender as DataGrid);
                    int rowindex = control.Items.IndexOf(control.SelectedCells[0].Item);
                    int colindex = e.Column.DisplayIndex - 1;
                    string value = (e.EditingElement as TextBox).Text;
                    if (value.Length >= 3)
                    {
                        e.Cancel = true;
                        return;
                    }
                    //提交到当前数据包
                    if (rowindex == SelectedEthernetPacket.EthernetPacket.BytesHighPerformance.Bytes.Length / HEX_WIDTH
                         && colindex == SelectedEthernetPacket.EthernetPacket.BytesHighPerformance.Bytes.Length % HEX_WIDTH)
                    {
                        //添加帧数据
                        byte[] bytes = new byte[SelectedEthernetPacket.EthernetPacket.BytesHighPerformance.Bytes.Length+1];
                        SelectedEthernetPacket.EthernetPacket.BytesHighPerformance.Bytes.CopyTo(bytes, 0);
                        bytes[rowindex * HEX_WIDTH + colindex] = (byte)Convert.ToInt32(value, 16);
                        var packet = Packet.ParsePacket(LinkLayers.Ethernet, bytes) as EthernetPacket;
                        SendEthernetPacketList.Remove(SelectedEthernetPacket);
                        _selectedEthernetPacket = new DisplayPacket(packet);
                        OnPropertyChanged("SelectedEthernetPacket");
                        SendEthernetPacketList.Add(_selectedEthernetPacket);
                    }
                    else if (rowindex * HEX_WIDTH + colindex < SelectedEthernetPacket.EthernetPacket.BytesHighPerformance.Bytes.Length
                        && rowindex <= SelectedEthernetPacket.EthernetPacket.BytesHighPerformance.Bytes.Length / HEX_WIDTH 
                        && colindex<HEX_WIDTH)
                    {
                        //编辑数据帧
                        byte[] bytes = SelectedEthernetPacket.EthernetPacket.BytesHighPerformance.Bytes;
                        bytes[rowindex * HEX_WIDTH + colindex] = Byte.Parse(value);
                        //SelectedEthernetPacket = Packet.ParsePacket(LinkLayers.Ethernet,bytes) as EthernetPacket;
                        var packet = Packet.ParsePacket(LinkLayers.Ethernet, bytes) as EthernetPacket;
                        SendEthernetPacketList.Remove(SelectedEthernetPacket);
                        _selectedEthernetPacket = new DisplayPacket(packet);
                        OnPropertyChanged("SelectedEthernetPacket");
                        SendEthernetPacketList.Add(_selectedEthernetPacket);
                    }                    
                    else 
                    {
                        //无处理，不是紧挨着添加
                        e.Cancel = true;
                    }
                }
                catch (Exception e1)
                {
                    MessageBox.Show(e1.Message);
                }
            }
        }

        /// <summary>
        /// 导出pkt
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void loadPktMenuItem_Click(object sender, RoutedEventArgs e)
        {
            loadPackets(SendEthernetPacketList);
        }

        private void exportSendPktMenuItem_Click(object sender, RoutedEventArgs e)
        {
            exportPacktes(SendEthernetPacketList);
        }

        private void exportCapturePktMenuItem_Click(object sender, RoutedEventArgs e)
        {
            exportPacktes(CaptureEthernetPacketList);
        }

        /// <summary>
        /// 打开文件
        /// </summary>
        /// <param name="fileName">文件名</param>
        private void loadPacketFile(string fileName, IList<DisplayPacket> packetList)
        {
            int pktlength;
            byte[] bytes;
            try
            {
                using (FileStream fs = new FileStream(fileName, FileMode.Open))
                {
                    BinaryReader br = new BinaryReader(fs);
                    while (br.BaseStream.Position < br.BaseStream.Length)
                    {
                        pktlength = br.ReadInt32();
                        bytes = br.ReadBytes(pktlength);
                        packetList.Add(new DisplayPacket((EthernetPacket)Packet.ParsePacket(LinkLayers.Ethernet, bytes)));
                    }
                    br.Close();
                }
            }
            catch (Exception e1)
            {
                MessageBox.Show(e1.Message);
            }
        }

        private IList<DisplayPacket> loadPacketFile(string fileName)
        {
            IList<DisplayPacket> packetList = new List<DisplayPacket>();
            int pktlength;
            byte[] bytes;
            try
            {
                using (FileStream fs = new FileStream(fileName, FileMode.Open))
                {
                    BinaryReader br = new BinaryReader(fs);
                    while (br.BaseStream.Position < br.BaseStream.Length)
                    {
                        pktlength = br.ReadInt32();
                        bytes = br.ReadBytes(pktlength);
                        packetList.Add(new DisplayPacket((EthernetPacket)Packet.ParsePacket(LinkLayers.Ethernet, bytes)));
                    }
                    br.Close();
                }
            }
            catch (Exception)
            {                
                packetList = null;
            }
            return packetList;
        }

        private void loadPackets(IList<DisplayPacket> packetList)
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.Title = "选择文件";
            openFileDialog.Filter = "pkt文件|*.pkt|所有文件|*.*";
            openFileDialog.FileName = string.Empty;
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.DefaultExt = "zip";
            System.Windows.Forms.DialogResult result = openFileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.Cancel)
            {
                return;
            }
            string fileName = openFileDialog.FileName;

            loadPacketFile(fileName, packetList);            
        }

        private void exportPacktes(IList<DisplayPacket> packetList)
        {
            System.Windows.Forms.SaveFileDialog sfd = new System.Windows.Forms.SaveFileDialog();
            sfd.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            sfd.Filter = "pkt文件|*.pkt|所有文件|*.*";
            sfd.DefaultExt = "pkt";
            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                EthernetPacket packet;
                using (FileStream fs = new FileStream(sfd.FileName, FileMode.OpenOrCreate))
                {
                    BinaryWriter bw = new BinaryWriter(fs);
                    foreach (var item in packetList)
                    {
                        packet = item.EthernetPacket;
                        bw.Write(packet.BytesHighPerformance.Bytes.Length);//四字节长度
                        //数据
                        bw.Write(packet.BytesHighPerformance.Bytes, 0, packet.BytesHighPerformance.Bytes.Length);
                    }
                    bw.Flush();
                    bw.Close();
                }
                MessageBox.Show("导出成功");
            }
            else
            {
                //取消
                return;
            }       
        }

        private void CloseMenuItem_Click(object sender, RoutedEventArgs e)
        {            
            Environment.Exit(0);
        }

        private void sendPacketsDataGrid_GotFocus(object sender, RoutedEventArgs e)
        {
            capturePacketsDataGrid.UnselectAll();
        }

        private void capturePacketsDataGrid_GotFocus(object sender, RoutedEventArgs e)
        {
            sendPacketsDataGrid.UnselectAll();
        }

        #region 清空列表
        private void sendListClearButton_Click(object sender, RoutedEventArgs e)
        {
            if (sendPacketsDataGrid.HasItems)
            {
                SendEthernetPacketList.Clear();
            }
        }

        private void captureListClearButton_Click(object sender, RoutedEventArgs e)
        {
            if (capturePacketsDataGrid.HasItems)
            {
                CaptureEthernetPacketList.Clear();
            }
        }
        #endregion

        #region 注册默认文件
        /// <summary>
        /// 注册文件关联
        /// </summary>
        /// <param name="fileTypeName">文件类型名称（英文）</param>
        /// <param name="fileExt">文件扩展名不带点</param>
        /// <param name="fileIcon">文件图标和应用程序在同一个目录下</param>
        public void RegFile(string fileTypeName, string fileExt, string fileIcon)
        {
            RegistryKey key = Registry.ClassesRoot.OpenSubKey("." + fileExt);
            if (key == null)
            {
                key = Registry.ClassesRoot.CreateSubKey("." + fileExt);
                key.SetValue("", fileTypeName + "." + fileExt);
                key.SetValue("Content Type", "application/" + fileExt);
                key = Registry.ClassesRoot.CreateSubKey(fileTypeName + "." + fileExt);
                key.SetValue("", fileTypeName);
                RegistryKey keySub = key.CreateSubKey("DefaultIcon");
                keySub.SetValue("", AppDomain.CurrentDomain.BaseDirectory + "\\" + fileIcon);
                keySub = key.CreateSubKey("shell\\open\\command");
                keySub.SetValue("", "\"" + System.Windows.Forms.Application.ExecutablePath + "\" \"%1\"");
            }
        }
        #endregion

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string command = Environment.CommandLine;//获取进程命令行参数
            string[] para = command.Split('\"');
            if (para.Length > 3)
            {
                //获取打开的文件的路径
                string pathC = para[3];
                //打开文件
                loadPacketFile(pathC, SendEthernetPacketList);
            }

            RegFile("Packet.file", "pkt", "packet.ico");
        }

        /// <summary>
        /// 加载Hex
        /// </summary>
        /// <param name="stream"></param>
        private void loadHex(MemoryStream stream)
        {
            try
            {
                DynamicFileByteProvider dynamicFileByteProvider;
                try
                {                   
                    dynamicFileByteProvider = new DynamicFileByteProvider(stream);
                }
                catch (IOException) // write mode failed
                {
                    return;
                }
                hexBox.ByteProvider = dynamicFileByteProvider;
            }
            catch (Exception)
            {
                return;
            }
            finally
            {
            }  
        }

        private void commitHexEdit_Click(object sender, RoutedEventArgs e)
        {
            DynamicFileByteProvider dynamicFileByteProvider = hexBox.ByteProvider as DynamicFileByteProvider;
            dynamicFileByteProvider.ApplyChanges();
            #region 重新生成数据包
            try
            {
                byte[] bytes = (dynamicFileByteProvider.StreamData as MemoryStream).ToArray();
                var packet = Packet.ParsePacket(LinkLayers.Ethernet, bytes) as EthernetPacket;
                SendEthernetPacketList.Remove(SelectedEthernetPacket);
                _selectedEthernetPacket = new DisplayPacket(packet);
                OnPropertyChanged("SelectedEthernetPacket");
                SendEthernetPacketList.Add(_selectedEthernetPacket);
            }
            catch (Exception e1)
            {
                MessageBox.Show(e1.Message);
            }
            #endregion
        }

        private void test()
        {
            
        }
    }
}
