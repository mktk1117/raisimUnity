/*
 * MIT License
 * 
 * Copyright (c) 2019, Dongho Kang, Robotics Systems Lab, ETH Zurich
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using UnityEngine;

namespace raisimUnity
{
    public class TcpHelper
    {
        private const int MaxBufferSize = 33554432;
        private const int MaxPacketSize = 1024;
        private const int FooterSize = sizeof(Byte);

        // Tcp Address
        private string _tcpAddress = "127.0.0.1";
        private int _tcpPort = 8080;

        // Tcp client and stream
        private TcpClient _client = null;
        private NetworkStream _stream = null;
        
        // Buffer
        private byte[] _buffer;
        private byte[] _writeBuffer;
        private int _bufferOffset = 0;
        private int _writeBufferOffset = 0;
        
        // Read data timer
        private float readDataTime = 0;

        // for server request
        private bool _messageReceived = false;
            
        public enum  ServerRequestType : int {
            NoRequest = 0,
            StartRecordVideo = 1,
            StopRecordVideo = 2,
            FocusOnSpecificObject = 3,
            SetCameraTo = 4
        };
        
        public TcpHelper()
        {
            _tcpAddress = "127.0.0.1";
            _tcpPort = 8080;
            _buffer = new byte[MaxBufferSize];
            _writeBuffer = new byte[MaxBufferSize];
        }

        public void Flush()
        {
            if (_stream != null)
            {
                _stream.Flush();
            }
        }

        public void EstablishConnection(int waitTime = 1000)
        {
            try
            {
             // create tcp client and stream
                if (_client == null || !_client.Connected)
                {
                    _client = new TcpClient(_tcpAddress, _tcpPort);
                    _client.Client.NoDelay = true;
                    _stream = _client.GetStream();
                }
            }
            catch (Exception e)
            {
            }
        }

        public bool TryConnection()
        {
            GameObject.Find("_CanvasSidebar").GetComponent<UIController>().setState("TcpHelper/TryConnection");

            // create tcp client and stream
            if (_client == null || !_client.Connected)
            {
                GameObject.Find("_CanvasSidebar").GetComponent<UIController>().setState("TcpHelper/TryConnection: creating a new connection");
                _client = new TcpClient();
                CancellationToken ct = new CancellationToken(); // Required for "*.Task()" method
                _client.ConnectAsync(_tcpAddress, _tcpPort).Wait(1000, ct);
                if (!_client.Connected)
                {
                    return false;
                }

                _client.Client.NoDelay = true;
                _stream = _client.GetStream();
                return true;
            }

            return false;
        }

        public void CloseConnection()
        {
            GameObject.Find("_CanvasSidebar").GetComponent<UIController>().setState("TcpHelper/CloseConnection");

            try
            {
                // clear tcp stream and client
                if (_stream != null)
                {
                    _stream.Close();
                    _stream = null;
                }

                if (_client != null)
                {
                    _client.Close();
                    _client = null;
                }
            }
            catch (Exception e)
            {
                new RsuException(e);
            }
        }
        
        public bool CheckConnection()
        {
            GameObject.Find("_CanvasSidebar").GetComponent<UIController>().setState("TcpHelper/CheckConnection");

            try
            {
                if( _client!=null && _client.Client!=null && _client.Client.Connected )
                {
                    if( _client.Client.Poll(10000, SelectMode.SelectRead) )
                    {
                        if( _client.Client.Receive(_buffer, SocketFlags.Peek)==0 )
                            return false;
                        else
                            return true;
                    }
                    else
                        return true;
                }
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }
        
        public int ReadData()
        {
            GameObject.Find("_CanvasSidebar").GetComponent<UIController>().setState("TcpHelper/ReadData");

            // Clear buffer first
            Array.Clear(_buffer, 0, MaxBufferSize);

            // Wait until stream data is available 
            readDataTime = Time.realtimeSinceStartup;
            while (!_stream.DataAvailable)
            {
                if (Time.realtimeSinceStartup - readDataTime > 1.5f)
                    // If data is not available until timeout, return error
                    new RsuException("ReadData timeout!");
            }
            
            int numBytes = 0;
            Byte footer = Convert.ToByte('c');
            int readCounter = 0;
            int valread; 

            while (footer == Convert.ToByte('c'))
            {
                _buffer[numBytes + MaxPacketSize - FooterSize] = Convert.ToByte('c');
                
                valread = 0;
                while (valread < MaxPacketSize)
                {
                    int recieved = _stream.Read(_buffer, numBytes, MaxPacketSize - valread);
                    valread += recieved;
                    numBytes += recieved;
                    readCounter++;
                }
                
                footer = _buffer[numBytes - FooterSize];
                numBytes -= FooterSize;
            }
            
            if (footer != Convert.ToByte('e'))
                new RsuException("TcpHelper: Read data exception. The footer is not end.");

            _bufferOffset = 0;
            _messageReceived = true;
            return numBytes;
        }

        public void WriteData()
        {
            _stream.Write(_writeBuffer, 0, _writeBufferOffset);
            _writeBufferOffset = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ServerStatus GetDataServerStatus()
        {
            return (ServerStatus)(GetDataInt());
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ServerRequestType GetServerRequest()
        {
            return (ServerRequestType)(GetDataInt());
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RsVisualType GetDataRsVisualType()
        {
            return (RsVisualType)(GetDataInt());
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ServerMessageType GetDataServerMessageType()
        {
            return (ServerMessageType)(GetDataInt());
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RsObejctType GetDataRsObejctType()
        {
            return (RsObejctType)(GetDataInt());
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RsShapeType GetDataRsShapeType()
        {
            return (RsShapeType)(GetDataInt());
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double GetDataDouble()
        {
            var data = BitConverter.ToDouble(_buffer, _bufferOffset).As<double>();
            _bufferOffset = _bufferOffset + sizeof(double);
            return data;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetDataFloat()
        {
            var data = BitConverter.ToSingle(_buffer, _bufferOffset).As<float>();
            _bufferOffset = _bufferOffset + sizeof(float);
            return data;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong GetDataUlong()
        {
            var data = BitConverter.ToUInt64(_buffer, _bufferOffset).As<ulong>();
            _bufferOffset = _bufferOffset + sizeof(ulong);
            return data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long GetDataLong()
        {
            var data = BitConverter.ToInt64(_buffer, _bufferOffset).As<long>();
            _bufferOffset = _bufferOffset + sizeof(long);
            return data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint GetDataUint()
        {
            var data = BitConverter.ToUInt32(_buffer, _bufferOffset).As<uint>();
            _bufferOffset = _bufferOffset + sizeof(uint);
            return data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetDataInt()
        {
            var data = BitConverter.ToInt32(_buffer, _bufferOffset).As<int>();
            _bufferOffset = _bufferOffset + sizeof(int);
            return data;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetDataInt(int data)
        {
            var data_in_bytes = BitConverter.GetBytes(data);
            Buffer.BlockCopy(data_in_bytes, 0, _writeBuffer, _writeBufferOffset, data_in_bytes.Length);
            _writeBufferOffset = _writeBufferOffset + sizeof(int);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort GetDataUshort()
        {
            var data = BitConverter.ToUInt16(_buffer, _bufferOffset).As<ushort>();
            _bufferOffset = _bufferOffset + sizeof(ushort);
            return data;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short GetDataShort()
        {
            var data = BitConverter.ToInt16(_buffer, _bufferOffset).As<short>();
            _bufferOffset = _bufferOffset + sizeof(short);
            return data;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte GetDataByte()
        {
            var data = _buffer[_bufferOffset].As<byte>();
            _bufferOffset = _bufferOffset + sizeof(byte);
            return data;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte GetDataSbyte()
        {
            var data = ((sbyte)_buffer[_bufferOffset]).As<sbyte>();
            _bufferOffset = _bufferOffset + sizeof(sbyte);
            return data;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetDataString()
        {
            ulong size = GetDataUlong();
            var data = Encoding.UTF8.GetString(_buffer, _bufferOffset, (int)size).As<string>();
            _bufferOffset = _bufferOffset + (int)size;
            return data;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetDataBool()
        {
            var data = BitConverter.ToBoolean(_buffer, _bufferOffset).As<bool>();
            _bufferOffset = _bufferOffset + sizeof(bool);
            return data;
        }

        //**************************************************************************************************************
        //  Getter and Setters 
        //**************************************************************************************************************
        
        public bool DataAvailable
        {
            get => _client != null && _client.Connected && _stream != null;
        }

        public bool MessageReceived
        {
            get { return _messageReceived; }
            set { _messageReceived = value; }
        }

        public bool Connected
        {
            get => _client != null && _client.Connected;
        }

        public string TcpAddress
        {
            get => _tcpAddress;
            set => _tcpAddress = value;
        }

        public int TcpPort
        {
            get => _tcpPort;
            set => _tcpPort = value;
        }
    }
}