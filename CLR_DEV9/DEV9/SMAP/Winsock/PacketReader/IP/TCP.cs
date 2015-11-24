﻿using System;
using System.Collections.Generic;

namespace CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP
{
    class TCP : IPPayload
    {
        public UInt16 SourcePort;
        public UInt16 DestinationPort;
        public UInt32 SequenceNumber;
        public UInt32 AcknowledgementNumber;
        byte data_offset_and_NS_flag;
        protected int HeaderLength //Can have varying Header Len
        //Need to account for this at packet creation
        {
            get
            {
                return (data_offset_and_NS_flag >> 4) << 2;
            }
            set
            {
                byte NS = (byte)(data_offset_and_NS_flag & 1);
                data_offset_and_NS_flag = (byte)((value >> 2) << 4);
                data_offset_and_NS_flag |= NS;
            }
        }
        public bool NS
        {
            get { return ((data_offset_and_NS_flag & 1) != 0); }
            set
            {
                if (value) { data_offset_and_NS_flag |= (1); }
                else { data_offset_and_NS_flag &= unchecked((byte)(~(1))); }
            }
        }
        byte flags;
        public UInt16 WindowSize;
        protected UInt16 Checksum;
        protected UInt16 UrgentPointer;
        public List<TCPOption> Options = new List<TCPOption>();
        byte[] data;
        public override byte Protocol
        {
            get { return (byte)IPType.TCP; }
        }
        public override ushort Length
        {
            get
            {
                ReComputeHeaderLen();
                return (UInt16)(data.Length + HeaderLength);
            }
            protected set
            {
                throw new NotImplementedException();
            }
        }

        #region 'Flags'
        public bool CWR
        {
            get { return ((flags & (1 << 7)) != 0); }
            set
            {
                if (value) { flags |= (1 << 4); }
                else { flags &= unchecked((byte)(~(1 << 4))); }
            }
        }
        public bool ECE
        {
            get { return ((flags & (1 << 6)) != 0); }
            set
            {
                if (value) { flags |= (1 << 4); }
                else { flags &= unchecked((byte)(~(1 << 4))); }
            }
        }
        public bool URG
        {
            get { return ((flags & (1 << 5)) != 0); }
            set
            {
                if (value) { flags |= (1 << 4); }
                else { flags &= unchecked((byte)(~(1 << 4))); }
            }
        }
        public bool ACK
        {
            get { return ((flags & (1 << 4)) != 0); }
            set
            {
                if (value) { flags |= (1 << 4); }
                else { flags &= unchecked((byte)(~(1 << 4))); }
            }
        }
        public bool PSH
        {
            get { return ((flags & (1 << 3)) != 0); }
            set
            {
                if (value) { flags |= (1 << 3); }
                else { flags &= unchecked((byte)(~(1 << 3))); }
            }
        }
        public bool RST
        {
            get { return ((flags & (1 << 2)) != 0); }
            set
            {
                if (value) { flags |= (1 << 2); }
                else { flags &= unchecked((byte)(~(1 << 2))); }
            }
        }
        public bool SYN
        {
            get { return ((flags & (1 << 1)) != 0); }
            set
            {
                if (value) { flags |= (1 << 1); }
                else { flags &= unchecked((byte)(~(1 << 1))); }
            }
        }
        public bool FIN
        {
            get { return ((flags & (1)) != 0); }
            set
            {
                if (value) { flags |= (1); }
                else { flags &= unchecked((byte)(~(1))); }
            }
        }
        #endregion

        private void ReComputeHeaderLen()
        {
            int opOffset = 20;
            for (int i = 0; i < Options.Count; i++)
            {
                opOffset += Options[i].Length;
            }
            opOffset += opOffset % 4; //needs to be a whole number of 32bits
            HeaderLength = opOffset;
        }

        public override byte[] GetPayload()
        {
            return data;
        }
        public TCP(byte[] payload) //Length = IP payload len
        {
            data = payload;
        }
        public TCP(byte[] buffer, int offset, int parLength) //Length = IP payload len
        {
            int initialOffset = offset;
            //Bits 0-31
            NetLib.ReadUInt16(buffer, ref offset, out SourcePort);
            //Console.Error.WriteLine("src port=" + SourcePort); 
            NetLib.ReadUInt16(buffer, ref offset, out DestinationPort);
            //Console.Error.WriteLine("dts port=" + DestinationPort);

            //Bits 32-63
            NetLib.ReadUInt32(buffer, ref offset, out SequenceNumber);
            //Console.Error.WriteLine("seq num=" + SequenceNumber); //Where in the stream the start of the payload is

            //Bits 64-95
            NetLib.ReadUInt32(buffer, ref offset, out AcknowledgementNumber);
            //Console.Error.WriteLine("ack num=" + AcknowledgmentNumber); //the next expected byte(seq) number

            //Bits 96-127
            NetLib.ReadByte08(buffer, ref offset, out data_offset_and_NS_flag);
            //Console.Error.WriteLine("TCP hlen=" + HeaderLength);
            NetLib.ReadByte08(buffer, ref offset, out flags);
            NetLib.ReadUInt16(buffer, ref offset, out WindowSize);
            //Console.Error.WriteLine("win Size=" + WindowSize);

            //Bits 127-159
            NetLib.ReadUInt16(buffer, ref offset, out Checksum);
            NetLib.ReadUInt16(buffer, ref offset, out UrgentPointer);
            //Console.Error.WriteLine("urg ptr=" + UrgentPointer);

            //Bits 160+
            if (HeaderLength > 20) //TCP options
            {
                bool opReadFin = false;
                do
                {
                    byte opKind = buffer[offset];
                    byte opLen = buffer[offset + 1];
                    switch (opKind)
                    {
                        case 0:
                            //Console.Error.WriteLine("Got End of Options List @ " + (op_offset-offset-1));
                            opReadFin = true;
                            break;
                        case 1:
                            //Console.Error.WriteLine("Got NOP");
                            Options.Add(new TCPopNOP());
                            offset += 1;
                            continue;
                        case 2:
                            //Console.Error.WriteLine("Got MMS");
                            Options.Add(new TCPopMSS(buffer, offset));
                            break;
                        case 3:
                            Options.Add(new TCPopWS(buffer, offset));
                            break;
                        case 8:
                            //Console.Error.WriteLine("Got Timestamp");
                            Options.Add(new TCPopTS(buffer, offset));
                            break;
                        default:
                            Console.Error.WriteLine("Got TCP Unknown Option " + opKind + "with len" + opLen);
                            break;
                    }
                    offset += opLen;
                    if (offset == initialOffset + HeaderLength)
                    {
                        //Console.Error.WriteLine("Reached end of Options");
                        opReadFin = true;
                    }
                } while (opReadFin == false);
            }
            offset = initialOffset + HeaderLength;

            NetLib.ReadByteArray(buffer, ref offset, parLength - HeaderLength, out data);
            //AllDone
        }

        public override void CalculateCheckSum(byte[] srcIP, byte[] dstIP)
        {
            Int16 TCPLength = (Int16)(HeaderLength + data.Length);
            int pHeaderLen = (12 + TCPLength);
            if ((pHeaderLen & 1) != 0)
            {
                //Console.Error.WriteLine("OddSizedPacket");
                pHeaderLen += 1;
            }

            byte[] headerSegment = new byte[pHeaderLen];
            int counter = 0;

            NetLib.WriteByteArray(ref headerSegment, ref counter, srcIP);
            NetLib.WriteByteArray(ref headerSegment, ref counter, dstIP);
            counter += 1;//[8] = 0
            NetLib.WriteByte08(ref headerSegment, ref counter, Protocol);
            NetLib.WriteUInt16(ref headerSegment, ref counter, (UInt16)TCPLength);
            //Pseudo Header added
            //Rest of data is normal Header+data (with zerored checksum feild)
            //Null Checksum
            Checksum = 0;
            NetLib.WriteByteArray(ref headerSegment, ref counter, GetBytes());

            Checksum = IPPacket.InternetChecksum(headerSegment);
        }

        public override bool VerifyCheckSum(byte[] srcIP, byte[] dstIP)
        {
            UInt16 TCPLength = (UInt16)(Length);
            int pHeaderLen = (12 + TCPLength);
            if ((pHeaderLen & 1) != 0)
            {
                //Console.Error.WriteLine("OddSizedPacket");
                pHeaderLen += 1;
            }

            byte[] headerSegment = new byte[pHeaderLen];
            int counter = 0;

            NetLib.WriteByteArray(ref headerSegment, ref counter, srcIP);
            NetLib.WriteByteArray(ref headerSegment, ref counter, dstIP);
            counter += 1;//[8] = 0
            NetLib.WriteByte08(ref headerSegment, ref counter, Protocol);
            NetLib.WriteUInt16(ref headerSegment, ref counter, (UInt16)TCPLength);
            //Pseudo Header added
            //Rest of data is normal neader+data
            NetLib.WriteByteArray(ref headerSegment, ref counter, GetBytes());

            UInt16 CsumCal = IPPacket.InternetChecksum(headerSegment);
            //Console.Error.WriteLine("Checksum Good = " + (CsumCal == 0));
            return (CsumCal == 0);
        }

        public override byte[] GetBytes()
        {
            int len = Length;
            byte[] ret = new byte[len];
            int counter = 0;
            NetLib.WriteUInt16(ref ret, ref counter, SourcePort);
            NetLib.WriteUInt16(ref ret, ref counter, DestinationPort);
            NetLib.WriteUInt32(ref ret, ref counter, SequenceNumber);
            NetLib.WriteUInt32(ref ret, ref counter, AcknowledgementNumber);
            NetLib.WriteByte08(ref ret, ref counter, data_offset_and_NS_flag);
            NetLib.WriteByte08(ref ret, ref counter, flags);
            NetLib.WriteUInt16(ref ret, ref counter, WindowSize);
            NetLib.WriteUInt16(ref ret, ref counter, Checksum);
            NetLib.WriteUInt16(ref ret, ref counter, UrgentPointer);

            //options
            for (int i = 0; i < Options.Count; i++)
            {
                NetLib.WriteByteArray(ref ret, ref counter, Options[i].GetBytes());
            }
            counter = HeaderLength;
            NetLib.WriteByteArray(ref ret, ref counter, data);
            return ret;
        }
    }
}