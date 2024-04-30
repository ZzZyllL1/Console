using MySql.Data.MySqlClient;
using S7.Net;
using System;
using System.Collections;
using System.Timers;
using System.Data;
using STTech.BytesIO.Tcp;
using System.Net.Sockets;
using TcpClient = System.Net.Sockets.TcpClient;
using System.IO;
using System.Net;
using System.Text;

namespace ConsoleApp1
{
    public static class Program
    {
       public static int x = 0;
        private static Plc plc;
        private static Timer timer = new Timer();
        public  static MySqlConnection mySqlConnection;


        static void Main(string[] args)
        {
            // 声明一个连接数据库的对象
            MySqlConnectionStringBuilder mysqlCSB = new MySqlConnectionStringBuilder();
            mysqlCSB.Database = "data";   // 设置连接的数据库名
            mysqlCSB.Server = "8.130.135.127";  // 设置连接数据库的IP地址
            mysqlCSB.Port = 3306;           // MySql端口号
            mysqlCSB.UserID = "root";       // 设置登录数据库的账号
            mysqlCSB.Password = "root";     // 设置登录数据库的密码


            // 创建连接
            mySqlConnection = new MySqlConnection(mysqlCSB.ToString());

            // 打开连接(如果处于关闭状态才进行打开)
            if (mySqlConnection.State == ConnectionState.Closed)
            {
                mySqlConnection.Open();
            }

            Socket client_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress ipAdress = IPAddress.Parse("127.0.0.1");
            //IPAddress[] ips;
            //ips = Dns.GetHostAddresses("7u0v681221.vicp.fun");
            //网络端点：为待请求连接的IP地址和端口号
            IPEndPoint ipEndpoint = new IPEndPoint(ipAdress, 4001);
            //connect()向服务端发出连接请求。客户端不需要bind()绑定ip和端口号，
            //因为系统会自动生成一个随机的地址（具体应该为本机IP+随机端口号）
            client_socket.Connect(ipEndpoint);
            while (true)
            {
                string rl = Console.ReadLine();
                //发送消息到服务端
                client_socket.Send(Encoding.UTF8.GetBytes(rl.ToUpper()));

                byte[] buffer = new byte[1024 * 1024];
                //接收服务端消息
                int num = client_socket.Receive(buffer);
                string str = Encoding.UTF8.GetString(buffer, 0, num);
                Console.WriteLine("收到服务端数据 : " + str);
                timer.Interval = 1000;
                timer.Elapsed += Timer_Elapsed;
                timer.Start();


                //    //创建PLC通信对象
                //    plc = new Plc(CpuType.S71200, "192.168.66.10", 0, 0);
                //    //连接PLC
                //    plc.Open();
                //    if (plc.IsConnected)
                //    {
                //        Console.WriteLine("PLC连接成功");
                //        timer.Interval = 2000;
                //        timer.Elapsed += Timer_Elapsed;
                //        timer.Start();
                //    }
                //    else
                //    {
                //        Console.WriteLine("PLC连接失败");
                //    }

                //    Console.ReadLine();
            }
        }

       

         public static void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            
            byte[] res = plc.ReadBytes(DataType.Memory, 0, 0, 1);
            BitArray bit = new BitArray(res);
            //string msg = BitConverter.ToString(res);
            //LogHelper.Info(msg);
            for (int i = 0; i < 8; i++)
            {
                Console.WriteLine("M0.{0}"+bit[i],i);
                string add = "M0."+i;
                bool sta = bit[i];//转为BOOL数组
                String mysqlcheck="select * from data";
                MySqlCommand mySqlCommand1 = new MySqlCommand(mysqlcheck, mySqlConnection);
                mySqlCommand1.ExecuteNonQuery();

                // 创建要插入的MySQL语句
                string mysqlInsert;
                switch (x)
                {
                    case 0:
                         mysqlInsert = "insert into data (addr,stat) values('" + add + "', '" + sta + "')";
                        if (mysqlcheck != "") { mysqlInsert = "replace into data (addr,stat) values('" + add + "', '" + sta + "')"; break; }
                        break;
                    default:
                        mysqlInsert = "replace into data (addr,stat) values('" + add + "', '" + sta + "')";
                        break;
                }
                // 创建用于实现MySQL语句的对象
                MySqlCommand mySqlCommand = new MySqlCommand(mysqlInsert, mySqlConnection);
                // 执行MySQL语句进行插入
                mySqlCommand.ExecuteNonQuery();

            }
            x = x + 1;
        }       
    }
}
