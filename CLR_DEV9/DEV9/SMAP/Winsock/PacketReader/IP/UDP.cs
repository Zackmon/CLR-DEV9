﻿using System;
using System.Net;

namespace CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP
{
    class UDP : IPPayload
    {
        public UInt16 SourcePort;
        public UInt16 DestinationPort;
        UInt16 _Length;
        public override UInt16 Length
        {
            get
            {
                return _Length;
            }
            protected set
            {
                _Length = value;
            }
        }
        protected UInt16 Checksum;
        protected int HeaderLength
        {
            get
            {
                return 8;
            }
        }
        byte[] data;
        public override byte Protocol
        {
            get { return (byte)IPType.UDP; }
        }
        public override byte[] GetPayload()
        {
            return data;
        }
        public UDP(byte[] parData)
        {
            data = parData;
            Length = (UInt16)(data.Length + HeaderLength);
        }
        public UDP(byte[] buffer, int offset, int parLength)
        {
            //Bits 0-31
            NetLib.ReadUInt16(buffer, ref offset, out SourcePort);
            //Console.Error.WriteLine("src port=" + SourcePort); 
            NetLib.ReadUInt16(buffer, ref offset, out DestinationPort);
            //Console.Error.WriteLine("dts port=" + DestinationPort);
            //Bits 32-63

            NetLib.ReadUInt16(buffer, ref offset, out _Length); //includes header length
            NetLib.ReadUInt16(buffer, ref offset, out Checksum);

            if (_Length > parLength)
            {
                //Console.Error.WriteLine("Unexpected Length");
                _Length = (UInt16)(parLength);
            }

            //Bits 64+
            //data = new byte[Length - HeaderLength];
            NetLib.ReadByteArray(buffer, ref offset, Length - HeaderLength, out data);
            //AllDone
        }
        public override void CalculateCheckSum(byte[] srcIP, byte[] dstIP)
        {
            int pHeaderLen = (12) + HeaderLength + data.Length;
            if ((pHeaderLen & 1) != 0)
            {
                pHeaderLen += 1;
            }

            byte[] headerSegment = new byte[pHeaderLen];
            int counter = 0;

            NetLib.WriteByteArray(ref headerSegment, ref counter, srcIP);
            NetLib.WriteByteArray(ref headerSegment, ref counter, dstIP);
            counter += 1;//[8] = 0
            NetLib.WriteByte08(ref headerSegment, ref counter, Protocol);
            NetLib.WriteUInt16(ref headerSegment, ref counter, Length);

            //Pseudo Header added
            //Rest of data is normal Header+data (with zerored checksum feild)
            //Null Checksum
            Checksum = 0;
            NetLib.WriteByteArray(ref headerSegment, ref counter, GetBytes());

            Checksum = IPPacket.InternetChecksum(headerSegment); //For performance, we can set this to = zero
        }

        public override bool VerifyCheckSum(byte[] srcIP, byte[] dstIP)
        {
            int pHeaderLen = (12) + HeaderLength + data.Length;
            if ((pHeaderLen & 1) != 0)
            {
                pHeaderLen += 1;
            }

            byte[] headerSegment = new byte[pHeaderLen];
            int counter = 0;

            NetLib.WriteByteArray(ref headerSegment, ref counter, srcIP);
            NetLib.WriteByteArray(ref headerSegment, ref counter, dstIP);
            counter += 1;//[8] = 0
            NetLib.WriteByte08(ref headerSegment, ref counter, Protocol);
            NetLib.WriteUInt16(ref headerSegment, ref counter, Length);

            //Pseudo Header added
            //Rest of data is normal Header+data (with zerored checksum feild)
            NetLib.WriteByteArray(ref headerSegment, ref counter, GetBytes());

            UInt16 CsumCal = IPPacket.InternetChecksum(headerSegment);
            //Console.Error.WriteLine("UDP Checksum Good = " + (CsumCal == 0));
            return (CsumCal == 0);
        }
        public override byte[] GetBytes()
        {
            byte[] ret = new byte[Length];
            int counter = 0;
            NetLib.WriteUInt16(ref ret, ref counter, SourcePort);
            NetLib.WriteUInt16(ref ret, ref counter, DestinationPort);
            NetLib.WriteUInt16(ref ret, ref counter, Length);
            NetLib.WriteUInt16(ref ret, ref counter, Checksum);

            NetLib.WriteByteArray(ref ret, ref counter, data);
            return ret;
        }
    }
}
