using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using PacketDotNet;
using System.Net;
using System.Net.NetworkInformation;
using System.Windows;

namespace NetHelper
{
    class MACAddrConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string mac_address = value.ToString();
            return String.Format("{0}-{1}-{2}-{3}-{4}-{5}",
                mac_address.Substring(0,2),
                mac_address.Substring(2,2),
                mac_address.Substring(4,2),
                mac_address.Substring(6,2),
                mac_address.Substring(8,2),
                mac_address.Substring(8,2));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                return PhysicalAddress.Parse(value.ToString());
            }
            catch (Exception e1)
            {
                MessageBox.Show(e1.Message);
                return PhysicalAddress.Parse("90-90-90-90-90-90");
            }            
        }        
    }

    class IPAddrConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
           return value.ToString();            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                return IPAddress.Parse(value.ToString());
            }
            catch (Exception e1)
            {
                MessageBox.Show(e1.Message);
                return IPAddress.Parse("192.168.1.1");
            }
        }
    }    
}
