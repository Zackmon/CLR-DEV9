﻿using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;

namespace CLRDEV9.DEV9.SMAP.Winsock.Sessions
{
    class ICMPSession : Session
    {
        Object sentry = new Object();

        List<ICMP> recvbuff = new List<ICMP>();

        List<Ping> pings = new List<Ping>();

        Dictionary<ConnectionKey, Session> Connections;

        public ICMPSession(Dictionary<ConnectionKey, Session> parConnections)
        {
            Connections = parConnections;
        }

        struct PingData
        {
            public byte[] HeaderData;
            public byte[] Data;
        }

        public void PingCompleate(object sender, System.Net.NetworkInformation.PingCompletedEventArgs e)
        {
            Console.Error.WriteLine("Ping Complete");
            PingData Seq = (PingData)e.UserState;
            PingReply rep = e.Reply;
            Ping ping = (Ping)sender;

            lock (sentry)
            {
                pings.Remove(ping);
                ping.Dispose();

                switch (rep.Status)
                {
                    case IPStatus.Success:
                        ICMP retICMP = new ICMP(Seq.Data);
                        retICMP.HeaderData = Seq.HeaderData;
                        retICMP.Type = 0; //echo reply
                        recvbuff.Add(retICMP);
                        break;
                    default:
                        open -= 1;
                        break;
                }
            }
        }

        public override IPPayload recv()
        {
            //Console.Error.WriteLine("UDP Recive");
            lock (sentry)
            {
                if (recvbuff.Count != 0)
                {
                    ICMP ret;
                    ret = recvbuff[0];
                    recvbuff.RemoveAt(0);
                    open -= 1;
                    return ret;
                }
            }

            return null;
        }
        public override bool send(IPPayload payload)
        {
            ICMP icmp = (ICMP)payload;

            switch (icmp.Type)
            {
                case 8: //Echo
                    //Code == zero
                    Console.Error.WriteLine("Send Ping");
                    lock (sentry)
                    {
                        open += 1;
                    }
                    PingData PD;
                    PD.Data = icmp.Data;
                    PD.HeaderData = icmp.HeaderData;
                    Ping nPing = new Ping();
                    nPing.PingCompleted += PingCompleate;
                    lock (sentry)
                    {
                        pings.Add(nPing);
                    }
                    nPing.SendAsync(new IPAddress(DestIP), PD);
                    System.Threading.Thread.Sleep(1); //Hack Fix
                    break;
                case 3: //
                    switch (icmp.Code)
                    {
                        case 3:
                            Console.Error.WriteLine("Recived Packet Rejected, Port Closed");
                            IPPacket retpkt = new IPPacket(icmp);
                            byte[] srvIP = retpkt.SourceIP;
                            byte prot = retpkt.Protocol;
                            UInt16 srvPort = 0;
                            UInt16 ps2Port = 0;
                            switch (prot)
                            {
                                case (byte)IPType.TCP:
                                    TCP tcp = (TCP)retpkt.Payload;
                                    srvPort = tcp.SourcePort;
                                    ps2Port = tcp.DestinationPort;
                                    break;
                                case (byte)IPType.UDP:
                                    UDP udp = (UDP)retpkt.Payload;
                                    srvPort = udp.SourcePort;
                                    ps2Port = udp.DestinationPort;
                                    break;
                            }
                            ConnectionKey Key = new ConnectionKey();
                            Key.IP0 = srvIP[0]; Key.IP1 = srvIP[1]; Key.IP2 = srvIP[2]; Key.IP3 = srvIP[3];
                            Key.Protocol = prot;
                            Key.PS2Port = ps2Port;
                            Key.SRVPort = srvPort;
                            if (Connections.Remove(Key))
                            {
                                Console.Error.WriteLine("Closed Rejected Connection");
                            }
                            else
                            {
                                Console.Error.WriteLine("Failed To Close Rejected Connection");
                            }
                            break;
                        default:
                            throw new NotImplementedException("Unsupported ICMP Code For Destination Unreachable" + icmp.Code);
                    }
                    break;
                default:
                    throw new NotImplementedException("Unsupported ICMP Type" + icmp.Type);
            }

            return true;
        }

        int open = 0;
        public override bool isOpen()
        {
            lock (sentry)
            {
                return (open != 0);
            }
        }
        public override void Dispose()
        {
            lock (sentry)
            {
                open = 0;
                foreach (Ping ping in pings)
                {
                    ping.SendAsyncCancel();
                    ping.Dispose();
                }
            }
        }
    }
}