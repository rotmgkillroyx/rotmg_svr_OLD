﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Collections.Concurrent;
using System.Threading;
using System.Net;

namespace wServer
{
    //hackish code
    class NetworkHandler
    {
        enum ReceiveState
        {
            Awaiting,
            ReceivingHdr,
            ReceivingBody,
            Processing
        }
        class ReceiveToken
        {
            public int Length;
            public Packet Packet;
        }
        enum SendState
        {
            Awaiting,
            Ready,
            Sending
        }
        class SendToken
        {
            public Packet Packet;
        }

        public const int BUFFER_SIZE = 0x10000;
        SocketAsyncEventArgs send;
        SendState sendState = SendState.Awaiting;
        ReceiveState receiveState = ReceiveState.Awaiting;
        Socket skt;
        ClientProcessor parent;
        public NetworkHandler(ClientProcessor parent, Socket skt)
        {
            this.parent = parent;
            this.skt = skt;
        }

        public void BeginHandling()
        {
            send = new SocketAsyncEventArgs();
            send.Completed += IOCompleted;
            send.UserToken = new SendToken();
            //send.SetBuffer(new byte[BUFFER_SIZE], 0, BUFFER_SIZE);

            var receive = new SocketAsyncEventArgs();
            receive.Completed += IOCompleted;
            receive.UserToken = new ReceiveToken();
            receive.SetBuffer(new byte[BUFFER_SIZE], 0, BUFFER_SIZE);

            receiveState = ReceiveState.ReceivingHdr;
            receive.SetBuffer(0, 5);
            if (!skt.ReceiveAsync(receive))
                IOCompleted(this, receive);
        }

        void IOCompleted(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                bool repeat;
                do
                {
                    repeat = false;

                    if (e.SocketError != SocketError.Success)
                        throw new SocketException((int)e.SocketError);

                    if (e.LastOperation == SocketAsyncOperation.Receive)
                    {
                        switch (receiveState)
                        {
                            case ReceiveState.ReceivingHdr:
                                if (e.BytesTransferred < 5)
                                {
                                    parent.Disconnect();
                                    return;
                                }

                                var len = (e.UserToken as ReceiveToken).Length =
                                    IPAddress.NetworkToHostOrder(BitConverter.ToInt32(e.Buffer, 0)) - 5;
                                if (len < 0 || len > BUFFER_SIZE)
                                    throw new InternalBufferOverflowException();
                                (e.UserToken as ReceiveToken).Packet = Packet.Packets[(PacketID)e.Buffer[4]].CreateInstance();

                                receiveState = ReceiveState.ReceivingBody;
                                e.SetBuffer(0, len);
                                if (!skt.ReceiveAsync(e))
                                {
                                    repeat = true;
                                    continue;
                                }
                                break;
                            case ReceiveState.ReceivingBody:
                                if (e.BytesTransferred < (e.UserToken as ReceiveToken).Length)
                                {
                                    parent.Disconnect();
                                    return;
                                }

                                var pkt = (e.UserToken as ReceiveToken).Packet;
                                pkt.Read(parent, e.Buffer, (e.UserToken as ReceiveToken).Length);

                                receiveState = ReceiveState.Processing;
                                bool cont = OnPacketReceived(pkt);

                                if (cont)
                                {
                                    receiveState = ReceiveState.ReceivingHdr;
                                    e.SetBuffer(0, 5);
                                    if (!skt.ReceiveAsync(e))
                                    {
                                        repeat = true;
                                        continue;
                                    }
                                }
                                break;
                            default:
                                throw new InvalidOperationException(e.LastOperation.ToString());
                        }
                    }
                    else if (e.LastOperation == SocketAsyncOperation.Send)
                    {
                        switch (sendState)
                        {
                            case SendState.Ready:
                                var dat = (e.UserToken as SendToken).Packet.Write(parent);

                                sendState = SendState.Sending;
                                e.SetBuffer(dat, 0, dat.Length);
                                if (!skt.SendAsync(e))
                                {
                                    repeat = true;
                                    continue;
                                }
                                break;
                            case SendState.Sending:
                                (e.UserToken as SendToken).Packet = null;

                                if (CanSendPacket(e,true))
                                {
                                    repeat = true;
                                    continue;
                                }
                                break;
                            default:
                                throw new InvalidOperationException(e.LastOperation.ToString());
                        }
                    }
                    else
                        throw new InvalidOperationException(e.LastOperation.ToString());
                } while (repeat);
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }


        void OnError(Exception ex)
        {
            parent.Disconnect();
        }
        bool OnPacketReceived(Packet pkt)
        {
            return parent.ProcessPacket(pkt);
        }
        ConcurrentQueue<Packet> pendingPackets = new ConcurrentQueue<Packet>();
        bool CanSendPacket(SocketAsyncEventArgs e,bool ignoreSending)
        {
            lock (sendLock)
            {
                if (sendState == SendState.Ready ||
                    (!ignoreSending && sendState == SendState.Sending))
                    return false;
                Packet packet;
                if (pendingPackets.TryDequeue(out packet))
                {
                    (e.UserToken as SendToken).Packet = packet;
                    sendState = SendState.Ready;
                    return true;
                }
                else
                {
                    sendState = SendState.Awaiting;
                    return false;
                }
            }
        }

        object sendLock = new object();
        public void SendPacket(Packet pkt)
        {
            pendingPackets.Enqueue(pkt);
            if (CanSendPacket(send, false))
            {
                var dat = (send.UserToken as SendToken).Packet.Write(parent);

                sendState = SendState.Sending;
                send.SetBuffer(dat, 0, dat.Length);
                if (!skt.SendAsync(send))
                    IOCompleted(this, send);
            }
        }
        public void SendPackets(IEnumerable<Packet> pkts)
        {
            foreach (var i in pkts)
                pendingPackets.Enqueue(i);
            if (CanSendPacket(send, false))
            {
                var dat = (send.UserToken as SendToken).Packet.Write(parent);

                sendState = SendState.Sending;
                send.SetBuffer(dat, 0, dat.Length);
                if (!skt.SendAsync(send))
                    IOCompleted(this, send);
            }
        }
    }
}
