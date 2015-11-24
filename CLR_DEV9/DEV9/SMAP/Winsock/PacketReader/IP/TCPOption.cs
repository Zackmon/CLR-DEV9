﻿using System;

namespace CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP
{
    abstract class TCPOption
    {
        public abstract byte Length
        {
            get;
        }
        public abstract byte Code
        {
            get;
        }
        public abstract byte[] GetBytes();
    }
    //class TCPopEOO : TCPOption
    //{
    //    public TCPopEOO()
    //    {

    //    }
    //    public override byte Length
    //    {
    //        get
    //        {
    //            return 1;
    //        }
    //    }
    //    public override byte[] GetBytes()
    //    {
    //        return new byte[] { 0 };
    //    }
    //}
    class TCPopNOP : TCPOption
    {
        public TCPopNOP()
        {

        }
        public override byte Length { get { return 1; } }
        public override byte Code { get { return 1; } }

        public override byte[] GetBytes()
        {
            return new byte[] { Code };
        }
    }
    class TCPopMSS : TCPOption
    {
        public UInt16 MaxSegmentSize;
        public TCPopMSS(UInt16 MSS) //Offset will include Kind and Len
        {
            //'(32 bits)'
            MaxSegmentSize = MSS;
        }
        public TCPopMSS(byte[] data, int offset) //Offset will include Kind and Len
        {
            //'(32 bits)'
            offset += 2;
            NetLib.ReadUInt16(data, ref offset, out MaxSegmentSize);
            Console.Error.WriteLine("Got Maximum segment size of " + MaxSegmentSize);
        }
        public override byte Length { get { return 4; } }
        public override byte Code { get { return 2; } }

        public override byte[] GetBytes()
        {
            byte[] ret = new byte[Length];
            int counter = 0;
            NetLib.WriteByte08(ref ret, ref counter, Code);
            NetLib.WriteByte08(ref ret, ref counter, Length);
            NetLib.WriteUInt16(ref ret, ref counter, MaxSegmentSize);
            return ret;
        }
    }
    class TCPopWS : TCPOption
    {
        byte WindowScale;
        public TCPopWS(byte WS) //Offset will include Kind and Len
        {
            //'(24 bits)'
            WindowScale = WS;
        }
        public TCPopWS(byte[] data, int offset) //Offset will include Kind and Len
        {
            //'(24 bits)'
            offset += 2;
            NetLib.ReadByte08(data, ref offset, out WindowScale);
            Console.Error.WriteLine("Got Window scale of " + WindowScale);
        }
        public override byte Length { get { return 3; } }
        public override byte Code { get { return 3; } }

        public override byte[] GetBytes()
        {
            byte[] ret = new byte[Length];
            int counter = 0;
            NetLib.WriteByte08(ref ret, ref counter, Code);
            NetLib.WriteByte08(ref ret, ref counter, Length);
            NetLib.WriteByte08(ref ret, ref counter, WindowScale);
            return ret;
        }
    }
    class TCPopTS : TCPOption
    {
        public UInt32 SenderTimeStamp;
        public UInt32 EchoTimeStamp;
        public TCPopTS(UInt32 SenderTS, UInt32 EchoTS)
        {
            SenderTimeStamp = SenderTS;
            EchoTimeStamp = EchoTS;
        }
        public TCPopTS(byte[] data, int offset) //Offset will include Kind and Len
        {
            //'(80 bits)'
            offset += 2;
            NetLib.ReadUInt32(data, ref offset, out SenderTimeStamp);
            NetLib.ReadUInt32(data, ref offset, out EchoTimeStamp);
        }
        public override byte Length { get { return 10; } }
        public override byte Code { get { return 8; } }

        public override byte[] GetBytes()
        {
            byte[] ret = new byte[Length];
            int counter = 0;
            NetLib.WriteByte08(ref ret, ref counter, Code);
            NetLib.WriteByte08(ref ret, ref counter, Length);
            NetLib.WriteUInt32(ref ret, ref counter, SenderTimeStamp);
            NetLib.WriteUInt32(ref ret, ref counter, EchoTimeStamp);
            return ret;
        }
    }
}