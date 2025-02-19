using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ephemera.NBagOfTricks;


namespace NTerm
{
    // /// <summary>Real serial port implementation.</summary>
    // public class RealSerialPort : ISerialPort
    // {
    //     readonly SerialPort _serialPort = new();

    //     #region ISerialPort implementation
    //     public int ReadBufferSize { get => _serialPort.ReadBufferSize; set => _serialPort.ReadBufferSize = value; }
    //     public int WriteBufferSize { get => _serialPort.WriteBufferSize; set => _serialPort.WriteBufferSize = value; }
    //     public int ReadTimeout { get => _serialPort.ReadTimeout; set => _serialPort.ReadTimeout = value; }
    //     public int WriteTimeout { get => _serialPort.WriteTimeout; set => _serialPort.WriteTimeout = value; }
    //     public string PortName { get => _serialPort.PortName; set => _serialPort.PortName = value; }
    //     public int BaudRate { get => _serialPort.BaudRate; set => _serialPort.BaudRate = value; }
    //     public Parity Parity { get => _serialPort.Parity; set => _serialPort.Parity = value; }
    //     public int DataBits { get => _serialPort.DataBits; set => _serialPort.DataBits = value; }
    //     public StopBits StopBits { get => _serialPort.StopBits; set => _serialPort.StopBits = value; }
    //     public bool IsOpen { get => _serialPort.IsOpen; }
    //     public Stream BaseStream { get { return _serialPort.BaseStream; } }
    //     public void Close() { _serialPort.Close(); }
    //     public void Dispose() { _serialPort.Dispose(); }
    //     public void Open() { _serialPort.Open(); }
    //     #endregion
    // }
}
