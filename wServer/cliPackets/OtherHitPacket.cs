﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace wServer.cliPackets
{
    public class OtherHitPacket : ClientPacket
    {
        public int Time { get; set; }
        public byte BulletId { get; set; }
        public int ObjectId { get; set; }
        public int TargetId { get; set; }

        public override PacketID ID { get { return PacketID.OtherHit; } }
        public override Packet CreateInstance() { return new OtherHitPacket(); }

        protected override void Read(ClientProcessor psr, NReader rdr)
        {
            Time = rdr.ReadInt32();
            BulletId = rdr.ReadByte();
            ObjectId = rdr.ReadInt32();
            TargetId = rdr.ReadInt32();
        }
        protected override void Write(ClientProcessor psr, NWriter wtr)
        {
            wtr.Write(Time);
            wtr.Write(BulletId);
            wtr.Write(ObjectId);
            wtr.Write(TargetId);
        }
    }
}
