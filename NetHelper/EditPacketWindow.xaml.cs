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
using System.Windows.Shapes;
using PacketDotNet;
using System.Net.NetworkInformation;
using System.Net;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace NetHelper
{
    /// <summary>
    /// EditPacketWindow.xaml 的交互逻辑
    /// </summary>
    public partial class EditPacketWindow : Window
    {
        private ObservableCollection<DisplayPacket> _ethernetPacketList;

        public EditPacketWindow(ObservableCollection<DisplayPacket> ethernetPacketList)
        {
            InitializeComponent();

            _ethernetPacketList = ethernetPacketList;
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            var packet = GenePacket();
            if (packet == null)
            {
                return;
            }
            else
            {
                _ethernetPacketList.Add(new DisplayPacket(packet) { Id = _ethernetPacketList.Count+1});
            }
        }

        private void OkBtn_Click(object sender, RoutedEventArgs e)
        {            
            this.Close();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private EthernetPacket GenePacket()
        {
            try
            {
                ushort tcpSourcePort = 123;
                ushort tcpDestinationPort = 321;
                var tcpPacket = new TcpPacket(tcpSourcePort, tcpDestinationPort);

                var ipSourceAddress = IPAddress.Parse(srcIPTextBox.Text);
                var ipDestinationAddress = IPAddress.Parse(dstIPTextBox.Text);
                var ipPacket = new IPv4Packet(ipSourceAddress, ipDestinationAddress);

                //var sourceHwAddress = "90-90-90-90-90-90";
                var sourceHwAddress = srcMACTextBox.Text;
                var ethernetSourceHwAddress = PhysicalAddress.Parse(sourceHwAddress);
                //var destinationHwAddress = "80-80-80-80-80-80";
                var destinationHwAddress = dstMACTextBox.Text;
                var ethernetDestinationHwAddress = PhysicalAddress.Parse(destinationHwAddress);
                // NOTE: using EthernetPacketType.None to illustrate that the ethernet
                //       protocol type is updated based on the packet payload that is
                //       assigned to that particular ethernet packet
                EthernetPacket ethernetPacket = new EthernetPacket(ethernetSourceHwAddress,
                                                        ethernetDestinationHwAddress,
                                                        EthernetPacketType.None);

                // Now stitch all of the packets together
                ipPacket.PayloadPacket = tcpPacket;
                ethernetPacket.PayloadPacket = ipPacket;
                // and print out the packet to see that it looks just like we wanted it to
                //Console.WriteLine(ethernetPacket.ToString());
                return ethernetPacket;
            }
            catch(Exception e1)
            {
                MessageBox.Show(e1.Message);
                return null;
            }            
        }

        public static EthernetPacket DefaultPackets()
        {
            try
            {
                ushort tcpSourcePort = 123;
                ushort tcpDestinationPort = 321;
                var tcpPacket = new TcpPacket(tcpSourcePort, tcpDestinationPort);

                var ipSourceAddress = IPAddress.Parse("192.168.1.1");
                var ipDestinationAddress = IPAddress.Parse("192.168.1.2");
                var ipPacket = new IPv4Packet(ipSourceAddress, ipDestinationAddress);

                var sourceHwAddress = "12-34-12-34-12-34";
                var ethernetSourceHwAddress = PhysicalAddress.Parse(sourceHwAddress);
                var destinationHwAddress = "AB-CD-AB-CD-AB-CD";
                var ethernetDestinationHwAddress = PhysicalAddress.Parse(destinationHwAddress);
                // NOTE: using EthernetPacketType.None to illustrate that the ethernet
                //       protocol type is updated based on the packet payload that is
                //       assigned to that particular ethernet packet
                EthernetPacket ethernetPacket = new EthernetPacket(ethernetSourceHwAddress,
                                                        ethernetDestinationHwAddress,
                                                        EthernetPacketType.None);

                // Now stitch all of the packets together
                ipPacket.PayloadPacket = tcpPacket;
                ethernetPacket.PayloadPacket = ipPacket;
                // and print out the packet to see that it looks just like we wanted it to
                //Console.WriteLine(ethernetPacket.ToString());                
                return ethernetPacket;
            }
            catch (Exception e1)
            {
                MessageBox.Show(e1.Message);
                return null;
            }
        }
    }
}
