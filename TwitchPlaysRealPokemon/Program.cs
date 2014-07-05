
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TechLifeForum;

namespace TwitchPlaysRealPokemon
{
    class Program
    {
        public static CommandForm commandForm = null;

        [STAThread]
        static void Main()
        {
            TextWriterTraceListener writer = new TextWriterTraceListener(System.Console.Out);
            Debug.Listeners.Clear();
            Debug.Listeners.Add(writer);
            Trace.Listeners.Clear();
            Trace.Listeners.Add(writer);

            Application.EnableVisualStyles();
            TwitchPlaysRealPokemon tprp = new TwitchPlaysRealPokemon();
            (new Thread(new ThreadStart(tprp.Loop))).Start();

#if !DRYRUN
            commandForm = new CommandForm();
            Application.Run(commandForm);

            Application.ThreadException += Application_ThreadException;
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
#endif
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(((Exception)e.ExceptionObject).Message);
        }

        static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            Console.WriteLine(e.Exception.Message);
        }
    }

    class TwitchPlaysRealPokemon
    {
        SerialPort port = null;
        IrcClient client = null;

        FileStream commandStream = null;
        Stream commands = null;
        FileStream chatStream = null;
        StreamWriter chat = null;

        Thread[] messageProcessors = new Thread[8];
        Queue<Command>[] messageQueues = new Queue<Command>[8];

        DateTime lastStartPress = DateTime.MinValue;
        Object lastStartPressLock = new Object();
        DateTime lastSelectPress = DateTime.MinValue;
        Object lastSelectPressLock = new Object();

        Dictionary<String, DateTime> lastSlowModeCommandTime = new Dictionary<string, DateTime>();

        bool allowCommand = true;

        public void Loop()
        {
            for (int i = 0; i <= 7; i++)
            {
                messageQueues[i] = new Queue<Command>();
                messageProcessors[i] = new Thread(new ParameterizedThreadStart(messageProcessor));
                messageProcessors[i].Start(i);
            }

            string consoleCommand = null;

            while (true)
            {
#if !DRYRUN
                if (port == null || !port.IsOpen)
                {
                    if (port != null)
                        port.Dispose();
                    try
                    {
                        port = new SerialPort(Properties.Settings.Default.ComPort, 9600, Parity.None, 8, StopBits.One);
                        port.Open();
                        Console.WriteLine("Connected to Serial Port");
                    }
                    catch (IOException ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
#endif

                if (client == null || !client.Connected)
                {
                    client = new IrcClient(Properties.Settings.Default.IrcServer);
                    client.Nick = Properties.Settings.Default.IrcUser;
                    client.ServerPass = Properties.Settings.Default.IrcPassword;
                    client.OnConnect += client_OnConnect;
                    client.ExceptionThrown += client_ExceptionThrown;
                    client.Connect();
                    client.ChannelMessage += client_ChannelMessage;
                }
                
#if !DRYRUN
                if (commandStream == null || !commandStream.CanWrite)
                {
                    if (commands != null)
                        commands.Dispose();
                    commands = null;

                    if (commandStream != null)
                        commandStream.Dispose();

                    commandStream = new FileStream(Properties.Settings.Default.CommandFile, FileMode.Append);
                }

                if (commands == null)
                {
                    commands = Stream.Synchronized(commandStream);
                }
                commandStream.Flush(true);

                if (chatStream == null || !chatStream.CanWrite)
                {
                    if (chat != null)
                        chat.Dispose();
                    chat = null;

                    if (chatStream != null)
                        chatStream.Dispose();

                    chatStream = new FileStream(Properties.Settings.Default.ChatFile, FileMode.Append);
                }

                if (chat == null)
                {
                    chat = new StreamWriter(chatStream, UTF8Encoding.UTF8);
                }
                chatStream.Flush(true);
#endif

                if ((consoleCommand = Console.ReadLine()) != null)
                {
                    if (consoleCommand == "DISABLE INPUT")
                    {
                        allowCommand = false;
                    }
                    else if (consoleCommand == "ENABLE INPUT")
                    {
                        allowCommand = true;
                    }
                    else if (consoleCommand.StartsWith("ADDSM"))
                    {
                        Properties.Settings.Default.SlowModeUsers.Add(consoleCommand.Substring(6));
                    }
                    else if (consoleCommand.StartsWith("DELSM"))
                    {
                        if (Properties.Settings.Default.SlowModeUsers.Contains(consoleCommand.Substring(6)))
                        {
                            Properties.Settings.Default.SlowModeUsers.Remove(consoleCommand.Substring(6));
                        }
                    }
                    else if (consoleCommand.Equals("SOFT RESET"))
                    {
                        lock(port)
                        {
                            port.Write("qwui"); // a b start select
                            Thread.Sleep(500);
                            port.Write("asjk"); // a b start select
                        }
                    }
                    else if (consoleCommand == "TEST INPUT")
                    {
                        (new Thread(new ThreadStart(() =>
                        {
                            Array commandArray = Enum.GetValues(typeof(Command));
                            Random random = new Random();
                            while (true)
                            {
                                for (int i = 0; i <= 7; i++)
                                {
                                    Command randomCommand = (Command)commandArray.GetValue(random.Next(commandArray.Length));

                                    if (randomCommand == Command.Start)
                                        continue;

                                    if (randomCommand == Command.A || randomCommand == Command.B || randomCommand == Command.Select)
                                        if (random.Next(7) != 1)
                                            continue;

                                    messageQueues[i].Enqueue(randomCommand);

                                    Program.commandForm.Invoke(new Action(() =>
                                    {
                                        Program.commandForm.lines.Add(new KeyValuePair<string, string>("TEST INPUT", randomCommand.ToString().ToLower()));
                                        Program.commandForm.Invalidate();
                                    }));

                                    Program.commandForm.Invoke(new Action(() =>
                                    {
                                        Program.commandForm.lines.Add(new KeyValuePair<string, string>("RANDOM", randomCommand.ToString().ToLower()));
                                        var first = Program.commandForm.lines.IndexOf(Program.commandForm.lines.First());
                                        var count = Program.commandForm.lines.Count;
                                        if (count > 100)
                                        {
                                            Program.commandForm.lines.RemoveRange(first, count - 50);
                                        }
                                        Program.commandForm.Invalidate();
                                    }));

                                    Thread.Sleep(50);
                                }
                            }
                        }))).Start();
                    }
                    else
                    {
                        processChat("CONSOLE", consoleCommand, true);
                    }
                }

                Thread.Sleep(1000);
            }
        }

        void client_ExceptionThrown(Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }

        void client_OnConnect()
        {
            Console.WriteLine("Connected to IRC");
            client.JoinChannel(Properties.Settings.Default.IrcChannel);
        }

        void client_ChannelMessage(string Channel, string User, string Message)
        {
            Task.Factory.StartNew(() =>
            {
                processChat(User, Message);
            });
        }

        void processChat(string sender, string message, bool ignoreAllowCommandVar = false)
        {
            Command? cmd = null;
            var msg = message.ToLower().Trim();

#if !DRYRUN
            if (msg == "up")
                cmd = Command.Up;
            else if (msg == "down")
                cmd = Command.Down;
            else if (msg == "left")
                cmd = Command.Left;
            else if (msg == "right")
                cmd = Command.Right;
            else if (msg == "b")
                cmd = Command.B;
            else if (msg == "a")
                cmd = Command.A;
            else if (msg == "select")
                cmd = Command.Select;
            else if (msg == "start")
                cmd = Command.Start;

            if (cmd.HasValue && (ignoreAllowCommandVar || allowCommand))
            {
                bool allowthiscommand = true;

                if (Properties.Settings.Default.SlowModeUsers.Contains(sender.ToLower()))
                {
                    lock (lastSlowModeCommandTime)
                    {
                        if (lastSlowModeCommandTime.ContainsKey(sender.ToLower()))
                        {
                            var lasttime = lastSlowModeCommandTime[sender.ToLower()];
                            if (DateTime.Now.Subtract(lasttime).TotalSeconds <= 10)
                            {
                                allowthiscommand = false;
                            }
                            else
                            {
                                lastSlowModeCommandTime[sender.ToLower()] = DateTime.Now;
                            }
                        }
                        else
                        {
                            lastSlowModeCommandTime.Add(sender.ToLower(), DateTime.Now);
                        }
                    }
                }

                if (allowthiscommand)
                {
                    messageQueues[(byte)cmd].Enqueue(cmd.Value);

                    commands.WriteByte((byte)cmd);
                
                    Program.commandForm.Invoke(new Action(() =>
                    {
                        Program.commandForm.lines.Add(new KeyValuePair<string, string>(sender, msg));
                        var first = Program.commandForm.lines.IndexOf(Program.commandForm.lines.First());
                        var count = Program.commandForm.lines.Count;
                        if (count > 100)
                        {
                            Program.commandForm.lines.RemoveRange(first, count - 50);
                        }
                        Program.commandForm.Invalidate();
                    }));
                }
                else
                {
                    Console.WriteLine("Disallowing command from " + sender);
                }
            }
#endif

            var print = sender + "\t" + msg + "\n";
            Console.Write(print);
            chat.Write(print);
        }

        void ircWatchdog()
        {
            while (true)
            {
                ;
            }

        }

        void messageProcessor(object o)
        {
            int index = (int)o;
            var queue = messageQueues[index];
            while (true)
            {
                try
                {
                    Command cmd;
                    #region Press Command
                    if (queue.TryDequeue(out cmd))
                    {
                        if (cmd == Command.A)
                        {
                            lock (port)
                            {
                                port.Write("q");
                            }
                        }
                        else if (cmd == Command.B)
                        {
                            lock (port)
                            {
                                port.Write("w");
                            }
                        }
                        else if (cmd == Command.Up)
                        {
                            lock (port)
                            {
                                port.Write("e");
                            }
                        }
                        else if (cmd == Command.Down)
                        {
                            lock (port)
                            {
                                port.Write("r");
                            }
                        }
                        else if (cmd == Command.Left)
                        {
                            lock (port)
                            {
                                port.Write("t");
                            }
                        }
                        else if (cmd == Command.Right)
                        {
                            lock (port)
                            {
                                port.Write("y");
                            }
                        }
                        else if (cmd == Command.Start)
                        {
                            lock (lastStartPressLock)
                            {
                                if (DateTime.Now.Subtract(lastStartPress).TotalSeconds <= 25)
                                {
                                    Console.WriteLine("Skipping start for cooldown!");
                                    continue;
                                }
                                lastStartPress = DateTime.Now;
                            }
                            lock (port)
                            {
                                port.Write("u");
                            }
                        }
                        else if (cmd == Command.Select)
                        {
                            lock (lastStartPressLock)
                            {
                                if (DateTime.Now.Subtract(lastStartPress).TotalSeconds <= 2)
                                {
                                    Console.WriteLine("Skipping select for reset protection!");
                                    continue;
                                }
                            }
                            lock (lastSelectPressLock)
                            {
                                if (DateTime.Now.Subtract(lastSelectPress).TotalSeconds <= 20)
                                {
                                    Console.WriteLine("Skipping select for cooldown!");
                                    continue;
                                }
                                lastSelectPress = DateTime.Now;
                            }
                            lock (port)
                            {
                                port.Write("i");
                            }
                        }
                    #endregion

                        if (cmd == Command.A || cmd == Command.B)
                        {
                            Thread.Sleep(150);
                        }
                        else
                        {
                            Thread.Sleep(75);
                        }

                    #region Stop Press Command
                        if (cmd == Command.A)
                        {
                            lock (port)
                            {
                                port.Write("a");
                            }
                        }
                        else if (cmd == Command.B)
                        {
                            lock (port)
                            {
                                port.Write("s");
                            }
                        }
                        else if (cmd == Command.Up)
                        {
                            lock (port)
                            {
                                port.Write("d");
                            }
                        }
                        else if (cmd == Command.Down)
                        {
                            lock (port)
                            {
                                port.Write("f");
                            }
                        }
                        else if (cmd == Command.Left)
                        {
                            lock (port)
                            {
                                port.Write("g");
                            }
                        }
                        else if (cmd == Command.Right)
                        {
                            lock (port)
                            {
                                port.Write("h");
                            }
                        }
                        else if (cmd == Command.Start)
                        {
                            lock (port)
                            {
                                port.Write("j");
                            }
                        }
                        else if (cmd == Command.Select)
                        {
                            lock (port)
                            {
                                port.Write("k");
                            }
                        }
                    }
                    #endregion

                    Thread.Sleep(5);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

    }

    enum Command : byte
    {
        A = 0,
        B = 1,
        Up = 2,
        Down = 3,
        Left = 4,
        Right = 5,
        Start = 6,
        Select = 7
    }

    class Queue<T>
    {
        /// <summary>Used as a lock target to ensure thread safety.</summary>
        private readonly Object _Locker = new Object();

        private readonly System.Collections.Generic.Queue<T> _Queue = new System.Collections.Generic.Queue<T>();

        /// <summary></summary>
        public void Enqueue(T item)
        {
            lock (_Locker)
            {
                _Queue.Enqueue(item);
            }
        }

        /// <summary>Enqueues a collection of items into this queue.</summary>
        public virtual void EnqueueRange(IEnumerable<T> items)
        {
            lock (_Locker)
            {
                if (items == null)
                {
                    return;
                }

                foreach (T item in items)
                {
                    _Queue.Enqueue(item);
                }
            }
        }

        /// <summary></summary>
        public T Dequeue()
        {
            lock (_Locker)
            {
                return _Queue.Dequeue();
            }
        }

        /// <summary></summary>
        public void Clear()
        {
            lock (_Locker)
            {
                _Queue.Clear();
            }
        }

        /// <summary></summary>
        public Int32 Count
        {
            get
            {
                lock (_Locker)
                {
                    return _Queue.Count;
                }
            }
        }

        /// <summary></summary>
        public Boolean TryDequeue(out T item)
        {
            lock (_Locker)
            {
                if (_Queue.Count > 0)
                {
                    item = _Queue.Dequeue();
                    return true;
                }
                else
                {
                    item = default(T);
                    return false;
                }
            }
        }
    }
}
