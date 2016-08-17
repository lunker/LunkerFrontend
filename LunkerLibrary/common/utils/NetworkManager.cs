using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LunkerLibrary.common.Utils
{
    public static class NetworkManager
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

            for (int idx=0; idx<objList.Length; idx++)
            {
                byte[] tmp = new byte[Marshal.SizeOf(type)];
                Array.Copy(data, Marshal.SizeOf(type) * idx, tmp,0, tmp.Length );

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

        public static Object ReadAsync(Socket peer, int msgLength, Type type)
        {
            //logger.Debug("[ChatServer][ReadAsync()] start");
            int headerLength = msgLength;

            byte[] buff = new byte[headerLength];

            Task readTask = Task.Factory.FromAsync(peer.BeginReceive(buff, 0, buff.Length, SocketFlags.None, null, peer), peer.EndReceive); // 생성과 동시에 실행됨
            readTask.ContinueWith((parent) =>
            {
                string getMsg = Encoding.UTF8.GetString(buff);
                Console.WriteLine(getMsg);

                //logger.Debug("[ChatServer][ReadAsync()] end");
                return NetworkManager.ByteToStructure(buff, type);
            });
            return null;
        }// end 

        public static byte[] ReadAsync(Socket peer, int msgLength)
        {
            //logger.Debug("[ChatServer][ReadAsync()] start");
            int headerLength = msgLength;

            byte[] buff = new byte[headerLength];

            Task readTask = Task.Factory.FromAsync(peer.BeginReceive(buff, 0, buff.Length, SocketFlags.None, null, peer), peer.EndReceive); // 생성과 동시에 실행됨
            readTask.Wait();
            return buff;
        }

        public static Task ReadAsyncTask(Socket peer, int msgLength, Type type)
        {
            int headerLength = msgLength;

            byte[] buff = new byte[headerLength];

            Task readTask = Task.Factory.FromAsync(peer.BeginReceive(buff, 0, buff.Length, SocketFlags.None, null, peer), peer.EndReceive); // 생성과 동시에 실행됨
           
            

            return readTask;
        }

        public static void SendAsync(Socket peer, Object message)
        {
            if(!(message is byte[]))
            {
                message = StructureToByte(message);
            }

            Task sendTask = Task.Factory.FromAsync(
                peer.BeginSend((byte[])message, 0, ((byte[])message).Length, SocketFlags.None, null, peer), 
                peer.EndSend);
            sendTask.Wait();

        }// end method

        public static Task SendAsyncTask(Socket peer, Object message)
        {
            if (!(message is byte[]))
            {
                message = StructureToByte(message);
            }

            Task sendTask = Task.Factory.FromAsync(
                peer.BeginSend((byte[])message, 0, ((byte[])message).Length, SocketFlags.None, null, peer),
                peer.EndSend);

            return sendTask;
        }

    }
}
