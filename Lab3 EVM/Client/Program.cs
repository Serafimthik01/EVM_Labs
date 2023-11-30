using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System;
using System.Threading;

public delegate double Func(double x);

public struct Response
{
    public int s;
    public double Res;
}

public struct Request
{
    public int s;
    public double num1;
    public double num2;
}


public class Rule
{
    public static double Integrate(Func f, double a, double b, double epsilon)
    {
        int n = 1;
        double previousResult, currentResult;

        currentResult = (b - a) * (f(a) + f(b)) / 2;

        do
        {
            n *= 2;
            double h = (b - a) / n;
            previousResult = currentResult;
            currentResult = 0.5 * (f(a) + f(b));
            for (int i = 1; i < n; i++)
            {
                currentResult += f(a + i * h);
            }

            currentResult *= h;

        } while (Math.Abs(currentResult - previousResult) > epsilon);

        return currentResult;
    }
}


class PipeClient
{
    static void Main(string[] args)
    {
        if (args.Length < 1) 
        { 
            return; 
        }

        Func f = x => 2 * x * x;
        using NamedPipeClientStream pipeClient = new(".", args[0], PipeDirection.InOut);
        pipeClient.Connect();
        byte[] bytes = new byte[Unsafe.SizeOf<Request>()];
        try
        {
            pipeClient.Read(bytes);
        }
        finally { }

        Request received_data = Unsafe.As<byte, Request>(ref bytes[0]);
        Response response_data = new()
        {
            s = received_data.s,
            Res = Rule.Integrate(f, received_data.num1, received_data.num2, 0.0000001)
        };

        byte[] response_bytes = new byte[Unsafe.SizeOf<Response>()];
        Unsafe.As<byte, Response>(ref response_bytes[0]) = response_data;
        try
        {
            pipeClient.Write(response_bytes);
        }
        finally { }
    }
}


//public struct Structure
//{
//    public int num1;
//    public int num2;
//    public int priority;
//}

//class Client
//{
//    static void Main()
//    {
//        using (NamedPipeClientStream Client = new(".", "channel", PipeDirection.InOut))
//        {
//            Console.WriteLine("Ожидание сервера...");
//            Client.Connect();
//            try
//            {
//                Console.WriteLine("Вы подключены к серверу");
//                while (true)
//                {
//                    byte[] bytes = new byte[Unsafe.SizeOf<Structure>()];
//                    Client.Read(bytes, 0, bytes.Length);
//                    Structure receivedData = Unsafe.As<byte, Structure>(ref bytes[0]);
//                    Console.WriteLine($"Полученные данные: num1 = {receivedData.num1}, num2 = {receivedData.num2}, приоритет = {receivedData.priority}");
//                    receivedData.num1 += receivedData.num2;
//                    byte[] modified_bytes = new byte[Unsafe.SizeOf<Structure>()];
//                    Unsafe.As<byte, Structure>(ref modified_bytes[0]) = receivedData;
//                    Client.Write(modified_bytes, 0, modified_bytes.Length);
//                }
//            }
//            catch (Exception) { }
//        }
//    }
//}

//public struct Structure
//{
//    public int num1;
//    public int num2;
//    public int pr;
//}

//public class Client
//{
//    public static void Main()
//    {
//        using (NamedPipeClientStream client = new NamedPipeClientStream(".", "channel", PipeDirection.InOut))
//        {
//            try
//            {
//                client.Connect();
//                Console.WriteLine("Успешное подключение к серверу.");

//                var receivedData = ReadStructureFromPipe(client);
//                Console.WriteLine($"Полученные данные: num1 = {receivedData.num1}, num2 = {receivedData.num2}, приоритет = {receivedData.pr}");
//                receivedData.num1 += receivedData.num2;
//                WriteStructureToPipe(client, receivedData);
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Ошибка: {ex.Message}");
//            }
//        }
//    }

//    private static Structure ReadStructureFromPipe(NamedPipeClientStream client)
//    {
//        byte[] bytes = new byte[Unsafe.SizeOf<Structure>()];
//        int bytesRead = client.Read(bytes, 0, bytes.Length);

//        if (bytesRead != 0)
//        {
//            return Unsafe.As<byte, Structure>(ref bytes[0]);
//        }
//        else
//        {
//            Console.WriteLine("Ошибка чтения данных из канала.");
//            return default;
//        }
//    }

//    private static void WriteStructureToPipe(NamedPipeClientStream client, Structure data)
//    {
//        byte[] bytes = new byte[Unsafe.SizeOf<Structure>()];
//        Unsafe.As<byte, Structure>(ref bytes[0]) = data;
//        client.Write(bytes, 0, bytes.Length);
//    }
//}