﻿using System;

namespace CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP
{
    class IPPacket : EthernetPayload //IPv4 Only
    {
        const byte _verHi = 4 << 4; //Assume it is always 4
        int hlen; //convert this back to num of 32bit words
        byte typeofservice; //TODO, Implement this
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
        protected UInt16 ID;
        #region "Fragment"
        protected UInt16 FragmentFlags;
        public UInt16 FragmentOffset
        {
            get
            {
                return (UInt16)(FragmentFlags & ~(0x7 << 13));
            }
        }
        //1st bit is reserved
        public bool MoreFragments
        {
            get { return ((FragmentFlags & (1 << 15)) != 0); }
            set
            {
                if (value) { FragmentFlags |= unchecked((UInt16)(1 << 15)); }
                else { FragmentFlags &= unchecked((UInt16)(~(1 << 15))); }
            }
        }
        public bool DoNotFragment
        {
            get { return ((FragmentFlags & (1 << 14)) != 0); }
            set
            {
                if (value) { FragmentFlags |= (1 << 14); }
                else { FragmentFlags &= unchecked((byte)(~(1 << 14))); }
            }
        }
        #endregion
        byte ttl = 128;
        public byte Protocol;
        protected UInt16 Checksum;
        public byte[] SourceIP = new byte[4];
        public byte[] DestinationIP = new byte[4];

        IPPayload _pl;
        public IPPayload Payload
        {
            get
            {
                return _pl;
            }
        }
        public override byte[] GetBytes
        {
            get
            {
                CalculateCheckSum();
                _pl.CalculateCheckSum(SourceIP, DestinationIP);

                byte[] ret = new byte[Length];
                int counter = 0;
                NetLib.WriteByte08(ref ret, ref counter, (byte)(_verHi + (hlen >> 2)));
                NetLib.WriteByte08(ref ret, ref counter, typeofservice);//DSCP/ECN
                NetLib.WriteUInt16(ref ret, ref counter, _Length);

                NetLib.WriteUInt16(ref ret, ref counter, ID);
                NetLib.WriteUInt16(ref ret, ref counter, FragmentFlags);

                NetLib.WriteByte08(ref ret, ref counter, ttl);
                NetLib.WriteByte08(ref ret, ref counter, Protocol);
                NetLib.WriteUInt16(ref ret, ref counter, Checksum); //header csum

                NetLib.WriteByteArray(ref ret, ref counter, SourceIP);
                NetLib.WriteByteArray(ref ret, ref counter, DestinationIP); ;

                byte[] plBytes = _pl.GetBytes();
                NetLib.WriteByteArray(ref ret, ref counter, plBytes);
                return ret;
            }
        }
        //source ip
        //dest ip
        public IPPacket(IPPayload pl)
        {
            _pl = pl;
            hlen = 20;
            Length = (UInt16)(pl.Length + hlen);
            Protocol = _pl.Protocol;
        }

        public IPPacket(ICMP icmpkt)
        {
            ReadBuffer(icmpkt.Data, 0, icmpkt.Data.Length);
        }

        public IPPacket(EthernetFrame Ef)
        {
            ReadBuffer(Ef.RawPacket.buffer, Ef.HeaderLength, Ef.RawPacket.size);
        }

        private void ReadBuffer(byte[] buffer, int offset, int bufferSize)
        {
            int pktoffset = offset;

            //Bits 0-31
            byte v_hl;
            NetLib.ReadByte08(buffer, ref pktoffset, out v_hl);
            hlen = ((v_hl & 0xF) << 2);
            NetLib.ReadByte08(buffer, ref pktoffset, out typeofservice); //TODO, Implement this
            NetLib.ReadUInt16(buffer, ref pktoffset, out _Length);
            if (_Length > bufferSize - offset)
            {
                Console.Error.WriteLine("Unexpected Length");
                _Length = (UInt16)(bufferSize - offset);
            }
            //Console.Error.WriteLine("len=" + Length); //Includes hlen

            //Bits 32-63
            NetLib.ReadUInt16(buffer, ref pktoffset, out ID); //Send packets with unique IDs
            NetLib.ReadUInt16(buffer, ref pktoffset, out FragmentFlags);

            if (MoreFragments)
            {
                Console.Error.WriteLine("FragmentedPacket");
            }

            //Bits 64-95
            NetLib.ReadByte08(buffer, ref pktoffset, out ttl);
            NetLib.ReadByte08(buffer, ref pktoffset, out Protocol);
            NetLib.ReadUInt16(buffer, ref pktoffset, out Checksum);
            //bool ccsum = verifyCheckSum(Ef.RawPacket.buffer, pktoffset);
            //Console.Error.WriteLine("IP Checksum Good? " + ccsum);//Should ALWAYS be true

            //Bits 96-127
            NetLib.ReadByteArray(buffer, ref pktoffset, 4, out SourceIP);
            //Bits 128-159
            NetLib.ReadByteArray(buffer, ref pktoffset, 4, out DestinationIP);
            //Console.WriteLine("Target IP :" + DestinationIP[0] + "." + DestinationIP[1] + "." + DestinationIP[2] + "." + DestinationIP[3]);

            //Bits 160+
            if (hlen > 20) //IP options (if any)
            {
                Console.Error.WriteLine("hlen=" + hlen + " > 20");
                Console.Error.WriteLine("IP options are not supported");
                throw new NotImplementedException("IP options are not supported");
            }
            switch (Protocol) //(Prase Payload)
            {
                case (byte)IPType.ICMP:
                    _pl = new ICMP(buffer, pktoffset, Length - hlen);
                    //((ICMP)_pl).VerifyCheckSum(SourceIP, DestinationIP);
                    break;
                case (byte)IPType.TCP:
                    _pl = new TCP(buffer, pktoffset, Length - hlen);
                    //((TCP)_pl).VerifyCheckSum(SourceIP, DestinationIP);
                    break;
                case (byte)IPType.UDP:
                    _pl = new UDP(buffer, pktoffset, Length - hlen);
                    //((UDP)_pl).VerifyCheckSum(SourceIP, DestinationIP);
                    break;
                default:
                    throw new NotImplementedException("Unkown IPv4 Protocol " + Protocol.ToString("X2"));
                //break;
            }
        }
        private void CalculateCheckSum()
        {
            //if (!(i == 5)) //checksum feild is 10-11th byte (5th short), which is skipped
            byte[] headerSegment = new byte[hlen];
            int counter = 0;
            NetLib.WriteByte08(ref headerSegment, ref counter, (byte)(_verHi + (hlen >> 2)));
            NetLib.WriteByte08(ref headerSegment, ref counter, typeofservice);//DSCP/ECN
            NetLib.WriteUInt16(ref headerSegment, ref counter, _Length);

            NetLib.WriteUInt16(ref headerSegment, ref counter, ID);
            NetLib.WriteUInt16(ref headerSegment, ref counter, FragmentFlags);

            NetLib.WriteByte08(ref headerSegment, ref counter, ttl);
            NetLib.WriteByte08(ref headerSegment, ref counter, Protocol);
            NetLib.WriteUInt16(ref headerSegment, ref counter, 0); //header csum

            NetLib.WriteByteArray(ref headerSegment, ref counter, SourceIP);
            NetLib.WriteByteArray(ref headerSegment, ref counter, DestinationIP);

            Checksum = InternetChecksum(headerSegment);
        }
        public bool VerifyCheckSum()
        {
            byte[] headerSegment = new byte[hlen];
            int counter = 0;
            NetLib.WriteByte08(ref headerSegment, ref counter, (byte)(_verHi + (hlen >> 2)));
            NetLib.WriteByte08(ref headerSegment, ref counter, typeofservice);//DSCP/ECN
            NetLib.WriteUInt16(ref headerSegment, ref counter, _Length);

            NetLib.WriteUInt16(ref headerSegment, ref counter, ID);
            NetLib.WriteUInt16(ref headerSegment, ref counter, FragmentFlags);

            NetLib.WriteByte08(ref headerSegment, ref counter, ttl);
            NetLib.WriteByte08(ref headerSegment, ref counter, Protocol);
            NetLib.WriteUInt16(ref headerSegment, ref counter, Checksum); //header csum

            NetLib.WriteByteArray(ref headerSegment, ref counter, SourceIP);
            NetLib.WriteByteArray(ref headerSegment, ref counter, DestinationIP);

            UInt16 CsumCal = InternetChecksum(headerSegment);
            return (CsumCal == 0);
        }
        public static ushort InternetChecksum(byte[] buffer)
        {
            //source http://stackoverflow.com/a/2201090
            //byte[] buffer = value.ToArray();
            int length = buffer.Length;
            int i = 0;
            UInt32 sum = 0;
            UInt32 data = 0;
            while (length > 1)
            {
                data = 0;
                data = (UInt32)(
                ((UInt32)(buffer[i]) << 8) | ((UInt32)(buffer[i + 1]) & 0xFF)
                );

                sum += data;
                if ((sum & 0xFFFF0000) > 0)
                {
                    sum = sum & 0xFFFF;
                    sum += 1;
                }

                i += 2;
                length -= 2;
            }

            if (length > 0)
            {
                sum += (UInt32)(buffer[i] << 8);
                //sum += (UInt32)(buffer[i]);
                if ((sum & 0xFFFF0000) > 0)
                {
                    sum = sum & 0xFFFF;
                    sum += 1;
                }
            }
            sum = ~sum;
            sum = sum & 0xFFFF;
            return (UInt16)sum;
        }
    }
}