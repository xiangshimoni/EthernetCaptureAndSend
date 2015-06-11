using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PacketDotNet;
using System.Net.NetworkInformation;
using System.Net;
using System.ComponentModel;
using SharpPcap;
using System.Windows.Media;

namespace NetHelper
{
    public class DisplayPacket:INotifyPropertyChanged
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

        private EthernetPacket _packet;
        public EthernetPacket EthernetPacket
        {
            get
            {
                return _packet;
            }
            set
            {
                _packet = value;
                OnPropertyChanged("EthernetPacket");
            }
        }

        public DisplayPacket()
        { 
            
        }

        public DisplayPacket(EthernetPacket packet)
        {
            _packet = packet;
        }

        private int _id;
        public int Id 
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;
                OnPropertyChanged("Id");
            }
        }

        private Brush _conclusionColor;
        /// <summary>
        /// 是否通过检测，不通过为红色
        /// </summary>
        public Brush ConclusionColor
        {
            set
            {
                _conclusionColor = value;
                OnPropertyChanged("ConclusionColor");
            }
            get
            {
                return _conclusionColor;
                //return Double.Parse(Percentage) < ThresholdValue ? Brushes.Red : Brushes.Transparent;
            }
        }

        private void OnIPChanged(IPAddress ipaddr)
        {
            string highlightFilterString = (App.Current.MainWindow as MainWindow).highlightFilterString;
            if (highlightFilterString.Equals(""))
            {
                return;
            }
            else
            {
                if (highlightFilterString.Substring(0, 9).Equals("src host "))
                {
                    ConclusionColor = ipaddr.Equals(IPAddress.Parse(highlightFilterString.Substring(9, highlightFilterString.Length-9))) ? 
                        Brushes.Red : Brushes.Transparent;
                }
            }
        }

        public PhysicalAddress SourceHwAddress 
        {
            get
            {
                return _packet.SourceHwAddress;
            }
            set
            {
                _packet.SourceHwAddress = value;                
                OnPropertyChanged("SourceHwAddress");
            }
        }

        public PhysicalAddress DestinationHwAddress
        {
            get
            {
                return _packet.DestinationHwAddress;
            }
            set
            {
                _packet.DestinationHwAddress = value;
                OnPropertyChanged("DestinationHwAddress");
            }
        }

        public IPAddress SourceIpAddress
        {
            get
            {
                if (_packet.PayloadPacket is IPv4Packet)
                {
                    OnIPChanged((_packet.PayloadPacket as IPv4Packet).SourceAddress);
                    return (_packet.PayloadPacket as IPv4Packet).SourceAddress;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (_packet.PayloadPacket is IPv4Packet)
                {
                    (_packet.PayloadPacket as IPv4Packet).SourceAddress = value;                    
                    OnPropertyChanged("SourceIpAddress");
                }                                
            }
        }

        public IPAddress DestinationIpAddress
        {
            get
            {
                if (_packet.PayloadPacket is IPv4Packet)
                {
                    return (_packet.PayloadPacket as IPv4Packet).DestinationAddress;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (_packet.PayloadPacket is IPv4Packet)
                {
                    (_packet.PayloadPacket as IPv4Packet).DestinationAddress = value;
                    OnPropertyChanged("DestinationIpAddress");
                }
            }
        }

        private string _intro = "无";
        public string Intro 
        {
            get
            {
                return _intro;
            }
            set
            {
                _intro = value;
                OnPropertyChanged("Intro");
            }
        }

        private PosixTimeval _timeval;
        public PosixTimeval Timeval 
        {
            get
            {
                return _timeval;
            }
            set
            {
                _timeval = value;
                OnPropertyChanged("Timeval");
            }
        }
        
        public int Length
        {
            get
            {
                return _packet.Bytes.Length;
            }            
        }

        public EthernetPacketType Type
        {
            get
            {
                return _packet.Type;
            }
        }
    }
}
