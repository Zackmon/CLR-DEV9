﻿using System;
using CLRDEV9.DEV9.SMAP.Winsock.PacketReader;

namespace CLRDEV9.DEV9.ATA
{
    partial class ATA_State
    {
        void CreateHDDinfo(int sizeMb)
        {
            UInt16 sectorSize = 512;
            Log_Verb("HddSize : " + DEV9Header.config.HddSize);
            long nbSectors = (((long)sizeMb / (long)sectorSize) * 1024L * 1024L);
            Log_Verb("nbSectors : " + nbSectors);

            identifyData = new byte[512];
            UInt16 heads = 255;
            UInt16 sectors = 63;
            UInt16 cylinders = (UInt16)(nbSectors / heads / sectors);
            int oldsize = cylinders * heads * sectors;

            //M-General configuration bit-significant information:
            #region
            //bit 0: 0 = ATA dev
            //bit 1: Hard Sectored (Retired)
            //bit 2: Soft Sectored (Retired) / Response incomplete
            //bit 3: Not MFM encoded (Retired)
            //bit 4: Head switch time > 15 usec (Retired)
            //bit 5: Spindle motor control option implemented (Retired)
            //bit 6: Non-Removable (Obsolete)
            //bit 7: Removable
            //bit 8-10: transfer speeds (<5, 5-10, >10MB/s) (Retired) 
            //bit 11: rotational speed tolerance is > 0.5%
            //bit 12: data strobe offset option available (Retired)
            //bit 13: track offset option available
            //bit 14: format speed tolerance gap required
            //bit 16: reserved
            #endregion
            int index = 0;
            DataLib.WriteUInt16(ref identifyData, ref index, 0x0040);    //word 0
            //Num of cylinders (Retired)
            DataLib.WriteUInt16(ref identifyData, ref index, cylinders); //word 1
            //Specific configuration
            index += 1 * 2;                                              //word 2
            //Num of heads (Retired)
            DataLib.WriteUInt16(ref identifyData, ref index, heads);     //word 3
            //Number of unformatted bytes per track (Retired)
            DataLib.WriteUInt16(ref identifyData, ref index, (UInt16)(sectorSize * sectors));//word 4
            //Number of unformatted bytes per sector (Retired)
            DataLib.WriteUInt16(ref identifyData, ref index, sectorSize);//word 5
            //Number of sectors per track (Retired)
            DataLib.WriteUInt16(ref identifyData, ref index, sectors);   //word 6
            //Reserved for assignment by the CompactFlash™ Association
            index += 2 * 2;                                              //word 7-8
            //Retired
            index += 1 * 2;                                              //word 9
            //M-Serial number (20 ASCII characters)
            DataLib.WriteCString(ref identifyData, ref index, "CLR-DEV9-AIR".PadRight(20));//word 10-19
            index = 20 * 2;
            //Buffer(cache) type (Retired)
            DataLib.WriteUInt16(ref identifyData, ref index, /*3*/0);    //word 20
            //Buffer(cache) size in sectors (Retired)
            DataLib.WriteUInt16(ref identifyData, ref index, /*512*/0);  //word 21
            //Number of ECC bytes available on read / write long commands (Obsolete)
            DataLib.WriteUInt16(ref identifyData, ref index, /*4*/0);    //word 22
            //M-Firmware revision (8 ASCII characters)
            DataLib.WriteCString(ref identifyData, ref index, "FIRM100".PadRight(8));//word 23-26
            index = 27 * 2;
            //M-Model number (40 ASCII characters)
            DataLib.WriteCString(ref identifyData, ref index, "CLR-DEV9 HDD AIR".PadRight(40));//word 27-46
            index = 47 * 2;
            //M-READ/WRITE MULI max sectors (TODO)
            index += 1 * 2;                                              //word 47
            //Dword IO supported
            DataLib.WriteUInt16(ref identifyData, ref index, 1);         //word 48
            //M-Capabilities
            #region
            //bits 7-0: Retired
            //bit 8: DMA supported
            //bit 9: LBA supported
            //bit 10:IORDY may be disabled
            //bit 11:IORDY supported
            //bit 12:Reserved
            //bit 13:Standby timer values as specified in this standard are supported
            #endregion
            DataLib.WriteUInt16(ref identifyData, ref index, ((1 << 11) | (1 << 9) | (1 << 8)));//word 49
            //M-Capabilities (0-Shall be set to one to indicate a device specific Standby timer value minimum)
            index += 1 * 2;                                              //word 50
            //PIO data transfer cycle timing mode (Obsolete)
            DataLib.WriteUInt16(ref identifyData, ref index, 0);         //word 51
            //DMA data transfer cycle timing mode (Obsolete)
            DataLib.WriteUInt16(ref identifyData, ref index, 0);         //word 52
            //M
            #region
            //bit 0: Fields in 54:58 are valid (Obsolete)
            //bit 1: Fields in 70:64 are valid
            //bit 2: Fields in 88 are valid
            #endregion
            DataLib.WriteUInt16(ref identifyData, ref index, (1 | (1 << 1) | (1 << 2)));//word 53
            //Number of current cylinders
            DataLib.WriteUInt16(ref identifyData, ref index, cylinders); //word 54
            //Number of current heads
            DataLib.WriteUInt16(ref identifyData, ref index, heads);     //word 55
            //Number of current sectors per track
            DataLib.WriteUInt16(ref identifyData, ref index, sectors);   //word 56
            //Current capacity in sectors
            DataLib.WriteUInt32(ref identifyData, ref index, (UInt32)oldsize);//word 57-58
            //M
            #region
            //bit 7-0: Current setting for number of logical sectors that shall be transferred per DRQ
            //         data block on READ/WRITE Multiple commands
            //bit 8: Multiple sector setting is valid
            #endregion
            index += 1 * 2;                                              //word 59
            //Total number of user addressable logical sectors
            DataLib.WriteUInt32(ref identifyData, ref index, (UInt32)nbSectors);//word 60-61
            //DMA modes
            #region
            //bits 0-7: Singleword modes supported (0,1,2)
            //bits 8-15: Transfer mode active
            #endregion
            if (sdmaMode > 0)
            {
                DataLib.WriteUInt16(ref identifyData, ref index, (UInt16)(0x07 | (1 << (sdmaMode + 8))));//word 62
            }
            else
            {
                DataLib.WriteUInt16(ref identifyData, ref index, 0x07);  //word 62
            }
            //DMA Modes
            #region
            //bits 0-7: Multiword modes supported (0,1,2)
            //bits 8-15: Transfer mode active
            #endregion
            if (mdmaMode > 0)
            {
                DataLib.WriteUInt16(ref identifyData, ref index, (UInt16)(0x07 | (1 << (mdmaMode + 8))));//word 63
            }
            else
            {
                DataLib.WriteUInt16(ref identifyData, ref index, 0x07);  //word 63
            }
            //M-Bit 0-7-PIO modes supported (0,1,2,3,4)
            DataLib.WriteUInt16(ref identifyData, ref index, 0x03);      //word 64 (pio3,4 supported) selection not reported here
            //M-Minimum Multiword DMA transfer cycle time per word
            DataLib.WriteUInt16(ref identifyData, ref index, 120);       //word 65
            //M-Manufacturer’s recommended Multiword DMA transfer cycle time
            DataLib.WriteUInt16(ref identifyData, ref index, 120);       //word 66
            //M-Minimum PIO transfer cycle time without flow control
            DataLib.WriteUInt16(ref identifyData, ref index, 120);       //word 67
            //M-Minimum PIO transfer cycle time with IORDY flow control
            DataLib.WriteUInt16(ref identifyData, ref index, 120);       //word 68
            //Reserved
            //69-70
            //Reserved
            //71-74
            //Queue depth (4bit, Maximum queue depth - 1)
            //75
            //Reserved
            //76-79
            index = 80 * 2;
            //M-Major revision number (1-3-Obsolete, 4-7-ATA4-7 supported)
            DataLib.WriteUInt16(ref identifyData, ref index, 0x70);      //word 80
            //M-Minor revision number
            DataLib.WriteUInt16(ref identifyData, ref index, 0);         //word 81
            //M-Supported Feature Sets (82)
            #region
            //bit 0: Smart
            //bit 1: Security Mode
            //bit 2: Removable media feature set
            //bit 3: Power management
            //bit 4: Packet (the CD features)
            //bit 5: Write cache
            //bit 6: Look-ahead
            //bit 7: Release interrupt
            //bit 8: SERVICE interrupt
            //bit 9: DEVICE RESET interrupt
            //bit 10: Host Protected Area
            //bit 11: (Obsolete)
            //bit 12: WRITE BUFFER command
            //bit 13: READ BUFFER command
            //bit 14: NOP
            //bit 15: (Obsolete)
            #endregion
            DataLib.WriteUInt16(ref identifyData, ref index, (UInt16)((1 << 14) | (1 << 5) | /*(1 << 1) | (1 << 10) |*/ 1));//word 82
            //M-Supported Feature Sets (83)
            #region
            //bit 0: DOWNLOAD MICROCODE
            //bit 1: READ/WRITE DMA QUEUED
            //bit 2: CFA (Card reader)
            //bit 3: Advanced Power Management
            //bit 4: Removable Media Status Notifications
            //bit 5: Power-Up Standby
            //bit 6: SET FEATURES required to spin up after power-up
            //bit 7: ??
            //bit 8: SET MAX security extension
            //bit 9: Automatic Acoustic Management
            //bit 10: 48bit LBA
            //bit 11: Device Configuration Overlay
            //bit 12: FLUSH CACHE
            //bit 13: FLUSH CACHE EXT
            //bit 14: 1
            #endregion
            DataLib.WriteUInt16(ref identifyData, ref index, (UInt16)((1 << 14) | (1 << 13) | (1 << 12) /*| (1 << 8)*/ |
                (Convert.ToUInt16(lba48Supported) << 10)));              //word 83
            //M-Supported Feature Sets (84)
            #region
            //bit 0: Smart error logging                            
            //bit 1: smart self-test
            //bit 2: Media serial number
            //bit 3: Media Card Pass Though
            //bit 4: Streaming feature set
            //bit 5: General Purpose Logging
            //bit 6: WRITE DMA FUA EXT & WRITE MULTIPLE FUA EXT
            //bit 7: WRITE DMA QUEUED FUA EXT
            //bit 8: 64bit World Wide Name
            //bit 9: URG bit supported for WRITE STREAM DMA EXT amd WRITE STREAM EXT
            //bit 10: URG bit supported for READ STREAM DMA EXT amd READ STREAM EXT
            //bit 13: IDLE IMMEDIATE with UNLOAD FEATURE
            //bit 14: 1
            #endregion
            DataLib.WriteUInt16(ref identifyData, ref index, (UInt16)((1 << 14) | (1 << 1) | 1));//word 84
            //M-Command set/feature enabled/supported (See word 82)
            DataLib.WriteUInt16(ref identifyData, ref index, (UInt16)(
                (Convert.ToUInt16(fetSmartEnabled)) |
                (Convert.ToUInt16(fetSecurityEnabled) << 1) |
                (Convert.ToUInt16(fetWriteCacheEnabled) << 5) |
                (Convert.ToUInt16(fetHostProtectedAreaEnabled) << 10) |
                (Convert.ToUInt16(true) << 14)));           //Fixed      //word 85
            //M-Command set/feature enabled/supported (See word 83)
            DataLib.WriteUInt16(ref identifyData, ref index, (UInt16)(
                /*(Convert.ToUInt16(true) << 8) | //SET MAX */
                (Convert.ToUInt16(lba48Supported) << 10) |  //Fixed
                (Convert.ToUInt16(true) << 12) |            //Fixed
                (Convert.ToUInt16(true) << 13)));           //Fixed      //word 86             
            //M-Command set/feature enabled/supported (See word 84)
            DataLib.WriteUInt16(ref identifyData, ref index, (UInt16)((1 << 14) | (1 << 1) | 1));
            DataLib.WriteUInt16(ref identifyData, ref index, (UInt16)(
                (Convert.ToUInt16(true)) |                  //Fixed
                (Convert.ToUInt16(true) << 1)));            //Fixed      //word 87
            //UDMA modes
            #region
            //bits 0-7: ultraword modes supported (0,1,2,4,5,6,7)
            //bits 8-15: Transfer mode active
            #endregion
            if (mdmaMode > 0)
            {
                DataLib.WriteUInt16(ref identifyData, ref index, (UInt16)(0x7f | (1 << (udmaMode + 8))));//word 88
            }
            else
            {
                DataLib.WriteUInt16(ref identifyData, ref index, 0x7f);  //word 88
            }
            //Time required for security erase unit completion
            //89
            //Time required for Enhanced security erase completion
            //90
            //Current advanced power management value
            //91
            //Master Password Identifier
            //92
            //Hardware reset result. The contents of bits (12:0) of this word shall change only during the execution of a hardware reset.
            #region
            //bit 0: 1
            //bit 1-2: How Dev0 determined Dev number (11 = unk)
            //bit 3: Dev 0 Passes Diag
            //bit 4: Dev 0 Detected assertion of PDIAG
            //bit 5: Dev 0 Detected assertion of DSAP
            //bit 6: Dev 0 Responds when Dev1 is selected
            //bit 7: Reserved
            //bit 8: 1
            //bit 9-10: How Dev1 determined Dev number
            //bit 11: Dev1 asserted 1
            //bit 12: Reserved
            //bit 13: Dev detected CBLID above Vih
            //bit 14: 1
            #endregion
            index = 93 * 2;
            DataLib.WriteUInt16(ref identifyData, ref index, (UInt16)(1 | (1 << 14) | 0x2000));//word 93
            //Vendor’s recommended acoustic management value.
            //94
            //Stream Minimum Request Size
            //95
            //Streaming Transfer Time - DMA
            //96
            //Streaming Access Latency - DMA and PIO
            //97
            //Streaming Performance Granularity
            //98-99
            //Total Number of User Addressable Sectors for the 48-bit Address feature set.
            index = 100 * 2;
            DataLib.WriteUInt64(ref identifyData, ref index, (UInt64)nbSectors);
            index -= 2;
            DataLib.WriteUInt16(ref identifyData, ref index, 0); //truncate to 48bits
            //Streaming Transfer Time - PIO
            //104
            //Reserved
            //105
            //Physical sector size / Logical Sector Size
            #region
            //bit 0-3: 2^X logical sectors per physical sector
            //bit 12: Logical sector longer than 512 bytes
            //bit 13: multiple logical sectors per physical sector
            //bit 14: 1
            #endregion
            index = 106 * 2;
            DataLib.WriteUInt16(ref identifyData, ref index, (UInt16)((1 << 14) | 0));
            //Inter-seek delay for ISO-7779acoustic testing in microseconds
            //107
            //WNN
            //108-111
            //Reserved
            //112-115
            //Reserved
            //116
            //Words per Logical Sector
            //117-118
            //Reserved
            //119-126
            //Removable Media Status Notification feature support
            //127
            //Security status
            #region
            //bit 0: Security supported
            //bit 1: Security enabled
            //bit 2: Security locked
            //bit 3: Security frozen
            //bit 4: Security count expired
            //bit 5: Enhanced erase supported
            //bit 6-7: reserved
            //bit 8: is Maximum Security
            #endregion
            //Vendor Specific
            //129-159
            //CFA power mode 1
            //160
            //Reserved for CFA
            //161-175
            //Current media serial number (60 ASCII characters)
            //176-205
            //Reserved
            //206-254
            //M-Integrity word
            //15:8 Checksum, 7:0 Signature
            CreateHDDinfoCsum();
        }
        void CreateHDDinfoCsum() //Is this correct?
        {
            byte counter = 0;
            unchecked
            {
                for (int i = 0; i < (512 - 1); i++)
                {
                    counter += identifyData[i];
                }
                counter += 0xA5;
            }
            identifyData[510] = 0xA5;
            identifyData[511] = (byte)(255 - counter + 1);
            counter = 0;
            unchecked
            {
                for (int i = 0; i < (512); i++)
                {
                    counter += identifyData[i];
                }
                Log_Error(counter.ToString());
            }
        }
    }
}
