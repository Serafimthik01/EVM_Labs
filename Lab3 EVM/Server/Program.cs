using System.IO.Pipes;
using System.Runtime.CompilerServices;

class Program
{
    static Task Main()
    {
        PipeServer pipeServer = new PipeServer();
        return pipeServer.Start();
    }
}

class PipeServer
{
    private PriorityQueue<DataRequest, int> dataQueue = new PriorityQueue<DataRequest, int>();
    StreamWriter file = new StreamWriter("output.txt");
    private Mutex mutQueue = new Mutex();
    private Mutex mutFile = new Mutex();

    public async Task Start()
    {
        CancellationTokenSource cts = new CancellationTokenSource();

        Task getArgsTasksWorker = GetArgsTasksWorker(cts.Token);
        Task hangleTaskWorker = HandleTaskWorker(cts.Token);

        // Обработка Ctrl + C
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        await Task.WhenAll(getArgsTasksWorker, hangleTaskWorker);
        file.Close();
    }

    private Task GetArgsTasksWorker(CancellationToken cancellationToken)
    {
        int ID = 1;

        return Task.Run(() =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Console.Write("\n\nEnter A, B and priority: ");

                if (TryParseInput(Console.ReadLine(), out double A, out double B, out int priority))
                {
                    DataRequest msg = new DataRequest()
                    {
                        Id = ID++,
                        A = A,
                        B = B
                    };

                    mutQueue.WaitOne();
                    dataQueue.Enqueue(msg, priority);
                    mutQueue.ReleaseMutex();

                    Console.WriteLine($"\nId = {msg.Id}, A = {msg.A}, B = {msg.B}, Приоритет = {priority}");
                }
                else
                {
                    Console.WriteLine("\nВведены неверные данные. Введите число А и Б, а также приоритет.");
                }
            }
        });
    }

    private Task HandleTaskWorker(CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                bool result;
                DataRequest msg;

                mutQueue.WaitOne();
                result = dataQueue.TryDequeue(out msg, out _);
                mutQueue.ReleaseMutex();

                if (!result)
                {
                    continue;
                }

                HandleTaskAsync(msg, cancellationToken);
            }
        });
    }

    private async void HandleTaskAsync(DataRequest msg, CancellationToken cancellationToken)
    {
        try
        {
            // Запуск процесса
            Process myProcess = new Process();
            myProcess.StartInfo.FileName = "C:\\Users\\Lenovo\\Desktop\\Laboratory1\\Lab3\\Client\\Client\\bin\\Debug\\net7.0\\Client.exe";
            myProcess.StartInfo.Arguments = $"channel{msg.Id}";
            myProcess.StartInfo.UseShellExecute = false;
            myProcess.StartInfo.CreateNoWindow = true;
            myProcess.Start();

            await using NamedPipeServerStream pipeServer = new NamedPipeServerStream($"channel{msg.Id}", PipeDirection.InOut);

            await pipeServer.WaitForConnectionAsync(cancellationToken);

            byte[] bytes = new byte[Unsafe.SizeOf<DataRequest>()];
            Unsafe.As<byte, DataRequest>(ref bytes[0]) = msg;
            await pipeServer.WriteAsync(bytes, cancellationToken);

            byte[] received_bytes = new byte[Unsafe.SizeOf<DataResponse>()];
            await pipeServer.ReadAsync(received_bytes, cancellationToken);

            DataResponse received_data = Unsafe.As<byte, DataResponse>(ref received_bytes[0]);

            mutFile.WaitOne();
            file.WriteLine($"Id: {received_data.Id}, A: {msg.A}, B: {msg.B}, Результат: {received_data.Result}");
            mutFile.ReleaseMutex();

            await myProcess.WaitForExitAsync(cancellationToken);
        }
        catch (Exception) { }
    }

    private bool TryParseInput(string? input, out double A, out double B, out int priority)
    {
        A = 0.0;
        B = 0.0;
        priority = 0;

        if (input == null)
        {
            return false;
        }

        string[] parts = input.Split(' ');
        if (parts.Length >= 2 && double.TryParse(parts[0], out A) && double.TryParse(parts[1], out B))
        {
            if (parts.Length >= 3 && int.TryParse(parts[2], out priority))
            {
                return true;
            }

            return true;
        }
        return false;
    }
}

//public struct Structure
//{
//    public int num1;
//    public int num2;
//    public int pr;
//}

//class PipeServer
//{
//    private static PriorityQueue<Structure, int> dataQueue = new PriorityQueue<Structure, int>();
//    private static Mutex mutex = new Mutex();

//    private static Task Main()
//    {
//        CancellationTokenSource source = new CancellationTokenSource();
//        CancellationToken token = source.Token;
//        using NamedPipeServerStream pipeServer = new("channel", PipeDirection.InOut);
//        Console.WriteLine("Ожидание подключения клиента...");
//        pipeServer.WaitForConnection();
//        string str = string.Empty;
//        Console.WriteLine("Клиент подключен");

//        Console.CancelKeyPress += (sender, eventArgs) =>
//        {
//            source.Cancel();
//        };

//        return Task.WhenAll(SenderTask(pipeServer, token), ReceiverTask(pipeServer, token));

//        Task SenderTask(NamedPipeServerStream pipeServer, CancellationToken token)
//        {
//            return Task.Run(() =>
//            {
//                while (!token.IsCancellationRequested)
//                {
//                    int _num1, _num2, _priority;
//                    Console.Write("Введите num1: ");
//                    int.TryParse(Console.ReadLine(), out _num1);
//                    Console.Write("Введите num2: ");
//                    int.TryParse(Console.ReadLine(), out _num2);
//                    Console.Write("Введите приоритетность: ");
//                    if (!int.TryParse(Console.ReadLine(), out _priority))
//                    {
//                        _priority = 0;
//                    }                  
//                    Structure data = new Structure
//                    {
//                        num1 = _num1,
//                        num2 = _num2,
//                        pr = _priority
//                    };
//                    mutex.WaitOne();
//                    dataQueue.Enqueue(data, _priority);
//                    Console.WriteLine(dataQueue.Count);
//                    mutex.ReleaseMutex();
//                }
//            });
//        }

//        Task ReceiverTask(NamedPipeServerStream pipeServer, CancellationToken token)
//        {
//            while (!token.IsCancellationRequested)
//            {
//                Structure st;
//                int priority;
//                mutex.WaitOne();
//                bool flag = dataQueue.TryDequeue(out st, out priority);
//                mutex.ReleaseMutex();
//                if (flag)
//                {
//                    byte[] dataBytes = new byte[Unsafe.SizeOf<Structure>()];
//                    Unsafe.As<byte, Structure>(ref dataBytes[0]) = st;
//                    pipeServer.Write(dataBytes, 0, dataBytes.Length);
//                    byte[] receivedBytes = new byte[Unsafe.SizeOf<Structure>()];
//                    if (pipeServer.Read(receivedBytes, 0, receivedBytes.Length) == receivedBytes.Length)
//                    {
//                        st = Unsafe.As<byte, Structure>(ref receivedBytes[0]);
//                    }
//                    str += $"num1 = {st.num1}; num2 = {st.num2}; приоритет= {st.pr}";
//                }
//            }
//            return Task.CompletedTask;
//        }
//    }
//}


//public struct Structure
//{
//    public int num1;
//    public int num2;
//    public int pr;
//}

//class PipeServer
//{
//    private static readonly PriorityQueue<Structure, int> dataQueue = new PriorityQueue<Structure, int>();
//    private static readonly Mutex mutex = new Mutex();

//    public static async Task Main()
//    {
//        using var cts = new CancellationTokenSource();
//        CancellationToken token = cts.Token;

//        Console.CancelKeyPress += (sender, eventArgs) => {
//            cts.Cancel();
//        };

//        using var pipeServer = new NamedPipeServerStream("channel", PipeDirection.InOut);

//        Console.WriteLine("Ждем клиента....");

//        await pipeServer.WaitForConnectionAsync();

//        Console.WriteLine("Клиент подключен");

//        var senderTask = SenderTask(pipeServer, token);
//        var receiverTask = ReceiverTask(pipeServer, token);

//        await Task.WhenAll(senderTask, receiverTask);
//    }

//    private static Task SenderTask(NamedPipeServerStream pipeServer, CancellationToken token)
//    {
//        while (!token.IsCancellationRequested)
//        {
//            int num1, num2, priority;

//            Console.Write("Введите num1: ");
//            if (!int.TryParse(Console.ReadLine(), out num1))
//            {
//                continue; // Пропустить ввод, если он не является числом
//            }

//            Console.Write("Введите num2: ");
//            if (!int.TryParse(Console.ReadLine(), out num2))
//            {
//                continue; // Пропустить ввод, если он не является числом
//            }

//            Console.Write("Введите приоритетность: ");
//            if (!int.TryParse(Console.ReadLine(), out priority))
//            {
//                priority = 0;
//            }

//            Structure data = new Structure
//            {
//                num1 = num1,
//                num2 = num2,
//                pr = priority
//            };

//            mutex.WaitOne();
//            dataQueue.Enqueue(data, priority);
//            Console.WriteLine(dataQueue.Count);
//            mutex.ReleaseMutex();
//        }

//        return Task.CompletedTask;
//    }

//    private static async Task ReceiverTask(NamedPipeServerStream pipeServer, CancellationToken token)
//    {
//        while (!token.IsCancellationRequested)
//        {
//            Structure st;
//            int priority;

//            mutex.WaitOne();
//            bool flag = dataQueue.TryDequeue(out st, out priority);
//            mutex.ReleaseMutex();

//            if (flag)
//            {
//                byte[] dataBytes = new byte[Unsafe.SizeOf<Structure>()];
//                Unsafe.As<byte, Structure>(ref dataBytes[0]) = st;
//                await pipeServer.WriteAsync(dataBytes, 0, dataBytes.Length);

//                byte[] receivedBytes = new byte[Unsafe.SizeOf<Structure>()];
//                if (await pipeServer.ReadAsync(receivedBytes, 0, receivedBytes.Length) == receivedBytes.Length)
//                {
//                    st = Unsafe.As<byte, Structure>(ref receivedBytes[0]);
//                }

//                string info = $"num1 = {st.num1}; num2 = {st.num2}; приоритет= {st.pr}";
//                Console.WriteLine(info);
//            }
//        }
//    }
//}
