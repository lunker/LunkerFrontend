using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LunkerLibrary.common.utils
{
    public static class WebNetworkManager
    {
        public static object ByteToStructure(byte[] data, Type type)
        {

            IntPtr buff = Marshal.AllocHGlobal(data.Length); // 배열의 크기만큼 비관리 메모리 영역에 메모리를 할당한다.

            Marshal.Copy(data, 0, buff, data.Length); // 배열에 저장된 데이터를 위에서 할당한 메모리 영역에 복사한다.
            object obj = Marshal.PtrToStructure(buff, type); // 복사된 데이터를 구조체 객체로 변환한다.
            Marshal.FreeHGlobal(buff); // 비관리 메모리 영역에 할당했던 메모리를 해제함

            if (Marshal.SizeOf(obj) != data.Length)// (((PACKET_DATA)obj).TotalBytes != data.Length) // 구조체와 원래의 데이터의 크기 비교
            {
                return null; // 크기가 다르면 null 리턴
            }

            return obj; // 구조체 리턴
        }// end method
        public static object[] ByteToStructureArray(byte[] data, Type type)
        {
            int objLength = data.Length / (Marshal.SizeOf(type));
            object[] objList = new object[objLength];

            for (int idx = 0; idx < objList.Length; idx++)
            {
                byte[] tmp = new byte[Marshal.SizeOf(type)];
                Array.Copy(data, Marshal.SizeOf(type) * idx, tmp, 0, tmp.Length);

                IntPtr buff = Marshal.AllocHGlobal(Marshal.SizeOf(type)); // 배열의 크기만큼 비관리 메모리 영역에 메모리를 할당한다.
                Marshal.Copy(tmp, 0, buff, tmp.Length); // 배열에 저장된 데이터를 위에서 할당한 메모리 영역에 복사한다.

                object obj = Marshal.PtrToStructure(buff, type); // 복사된 데이터를 구조체 객체로 변환한다.
                Marshal.FreeHGlobal(buff); // 비관리 메모리 영역에 할당했던 메모리를 해제함

                if (Marshal.SizeOf(obj) != data.Length)// (((PACKET_DATA)obj).TotalBytes != data.Length) // 구조체와 원래의 데이터의 크기 비교
                {
                    return null; // 크기가 다르면 null 리턴
                }
                objList[idx] = obj;
            }

            return objList; // 구조체 리턴
        }// end method

        // 구조체를 byte 배열로
        public static byte[] StructureToByte(object obj)
        {
            int datasize = Marshal.SizeOf(obj);//((PACKET_DATA)obj).TotalBytes; // 구조체에 할당된 메모리의 크기를 구한다.
            IntPtr buff = Marshal.AllocHGlobal(datasize); // 비관리 메모리 영역에 구조체 크기만큼의 메모리를 할당한다.
            Marshal.StructureToPtr(obj, buff, false); // 할당된 구조체 객체의 주소를 구한다.
            byte[] data = new byte[datasize]; // 구조체가 복사될 배열
            Marshal.Copy(buff, data, 0, datasize); // 구조체 객체를 배열에 복사
            Marshal.FreeHGlobal(buff); // 비관리 메모리 영역에 할당했던 메모리를 해제함

            return data; // 배열을 리턴
        }


        public static async Task<Object> ReadAsync(WebSocket peer, int msgLength, Type type)
        {
            return Task.Run( async ()=> {
                Object obj = null;
                //int rc = 0;
                byte[] buff = new byte[msgLength];

                WebSocketReceiveResult receiveResult = await peer.ReceiveAsync(new ArraySegment<byte>(buff), CancellationToken.None);

                if (receiveResult.MessageType == WebSocketMessageType.Close)
                {
                    await peer.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);

                }
                else if (receiveResult.MessageType == WebSocketMessageType.Text)
                {
                    await peer.CloseAsync(WebSocketCloseStatus.InvalidMessageType, "Cannot accept text frame", CancellationToken.None);

                }
                else
                {
                    obj = ByteToStructure(buff, type);
                }
                // read success
                return obj;
            });
        }
        /// <summary>
        /// ???? 맞는 구현임 이게?
        /// </summary>
        /// <param name="request"></param>
        /// <param name="msgLength"></param>
        /// <returns></returns>
        public static async Task<byte[]> ReadAsync(WebSocket peer, int msgLength)
        {
            return await Task.Run(async () => {
                int rc = 0;
                byte[] buff = new byte[msgLength];
                WebSocketReceiveResult receiveResult = await peer.ReceiveAsync(new ArraySegment<byte>(buff), CancellationToken.None);

                if (receiveResult.MessageType == WebSocketMessageType.Close)
                {
                    await peer.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);

                }
                else if (receiveResult.MessageType == WebSocketMessageType.Text)
                {
                    await peer.CloseAsync(WebSocketCloseStatus.InvalidMessageType, "Cannot accept text frame", CancellationToken.None);

                }

                return buff;
            });
        }
    
        /*
        public static void Send(WebSocket peer, Object obj)
        {
            int rc = 0;
            byte[] buff = null;

            if (obj is byte[])
            {
                buff = (byte[])obj;
            }
            else
            {
                buff = StructureToByte(obj);
            }
            
            await webSocket.SendAsync(new ArraySegment<byte>(buff, 0, buff.Length), WebSocketMessageType.Binary, true, CancellationToken.None);
        }

        public static  Task SendAsync(HttpListenerResponse response, Object obj)
        {
            int rc = 0;
            byte[] buff = null;
            if (obj is byte[])
            {
                buff = (byte[])obj;
            }
            else
            {
                buff = StructureToByte(obj);
            }
            return response.OutputStream.WriteAsync(buff, 0, buff.Length);
        }// end method
  
        */

        public static Task SendAsync(WebSocket peer, Object obj)
        {
            int rc = 0;
            byte[] buff = null;

            if (obj is byte[])
            {
                buff = (byte[])obj;
            }
            else
            {
                buff = StructureToByte(obj);
            }

            return peer.SendAsync(new ArraySegment<byte>(buff, 0, buff.Length), WebSocketMessageType.Binary, true, CancellationToken.None);
        }
    }// end class
}
