using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace TheGrid
{
    internal class Program
    {
        private static TcpListener listener;
        private static string clu;
        private static int port;
        private static List<Connection> connections = new List<Connection>();

        private static void Main(string[] args) {
            for (var i = 0; i < args.Length; i++) {
                switch (args[i]) {
                case "-clu":
                    clu = args[++i];
                    break;
                case "-listen":
                    port = int.Parse(args[++i]);
                    break;
                }
            }

            if (!string.IsNullOrEmpty(clu)) {
                var client = new TcpClient();
                client.Connect(clu.Split(':')[0], int.Parse(clu.Split(':')[1]));
                Debug($"[info] Connected to {clu}");
                var c = new Connection(client);

                var t = new Thread(con => Listen((Connection) con));
                t.Start(c);

                while (true) {
                    var input = Console.ReadLine();

                    if (input.StartsWith("/cmd ")) {
                        c.Write(new PackageContainer("command", new CommandPackage {
                            Command = input.Substring(5)
                        }));
                    }
                }
            }

            if (port > 1024) {
                listener = new TcpListener(new IPEndPoint(IPAddress.Any, port));
                listener.Start();
                Debug("[info] Server started");

                var t = new Thread(ListenForConnection);
                t.Start();

                while (true) {
                    var input = Console.ReadLine();

                    if (input.StartsWith("/cmd ")) {
                        connections.ForEach(c => c.Write(new PackageContainer("command", new CommandPackage {
                            Command = input.Substring(5)
                        })));
                    } else if (input.StartsWith("/getfile ")) {
                        connections.ForEach(c => c.Write(new PackageContainer("fileRequest", new FileRequestPackage {
                            FileName = input.Substring(9)
                        })));
                    } else if (input.StartsWith("/sendfile ")) {
                        try {
                            connections.ForEach(c => c.Write(new PackageContainer("file", new FilePackage {
                                Content = File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), input.Substring(10))),
                                FileName = input.Substring(10)
                            })));
                        } catch { }
                    } else if (input == "/shutdown") {
                        listener.Stop();
                        connections.ForEach(c => c.Write(new PackageContainer("control", new ControlPackage("shutdown"))));
                        connections.ForEach(c => {
                            try {
                                c.Client.GetStream().Close();
                                c.Client.GetStream().Dispose();
                                c.Writer.Close();
                                c.Writer.Dispose();
                                c.Reader.Close();
                                c.Reader.Dispose();
                                c.Client.Close();
                                c.Client.Dispose();
                            } catch { }
                        });
                        Environment.Exit(0);
                    }
                }
            }
        }

        private static void ListenForConnection() {
            while (true) {
                try {
                    var client = listener.AcceptTcpClient();

                    Debug($"[info] Received connection from {client.Client.RemoteEndPoint}");

                    var c = new Connection(client);
                    connections.Add(c);

                    var t = new Thread(con => Listen((Connection) con));
                    t.Start(c);
                } catch {
                    return;
                }
            }
        }

        private static void Listen(Connection c) {
            while (true) {
                try {
                    var container = c.Read<PackageContainer>();

                    if (container.Type == "command") {
                        var command = container.GetPackage<CommandPackage>();

                        var psi = new ProcessStartInfo {
                            FileName = "sh",
                            Arguments = $"-c \"{command.Command.Replace("\"", "\\\"")}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true
                        };

                        var p = new Process {
                            StartInfo = psi
                        };

                        p.Start();
                        var output = p.StandardOutput.ReadToEnd();
                        p.WaitForExit();

                        var response = new CommandOutputPackage {
                            ExitCode = p.ExitCode,
                            Output = output
                        };

                        c.Write(new PackageContainer("commandOutput", response));
                    } else if (container.Type == "commandOutput") {
                        var output = container.GetPackage<CommandOutputPackage>();

                        Console.WriteLine(output.Output);
                        Debug($"[info] Execution finished on remote host with code ({output.ExitCode})");
                    } else if (container.Type == "fileRequest") {
                        var request = container.GetPackage<FileRequestPackage>();

                        Debug($"[info] {c.EndPoint} requested \"{Path.Combine(Directory.GetCurrentDirectory(), request.FileName)}\"");

                        try {
                            var response = new FilePackage {
                                Content = File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), request.FileName)),
                                Extension = "",
                                FileName = request.FileName
                            };

                            c.Write(new PackageContainer("file", response));
                        } catch { }
                    } else if (container.Type == "file") {
                        var response = container.GetPackage<FilePackage>();

                        Debug($"[info] Received \"{Path.Combine(Directory.GetCurrentDirectory(), response.FileName)}\"");
                        File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), response.FileName), response.Content);
                    } else if (container.Type == "control") {
                        var action = container.GetPackage<ControlPackage>().Action;

                        switch (action) {
                        case "shutdown":
                            Environment.Exit(0);
                            break;
                        }
                    }
                } catch {
                    Debug($"[info] Lost connection from {c.EndPoint}");

                    if (!string.IsNullOrEmpty(clu)) {
                        try {
                            c.Client.GetStream().Close();
                            c.Client.GetStream().Dispose();
                            c.Writer.Close();
                            c.Writer.Dispose();
                            c.Reader.Close();
                            c.Reader.Dispose();
                            c.Client.Close();
                            c.Client.Dispose();
                        } catch { }
                        Environment.Exit(0);
                        return;
                    }

                    connections.Remove(c);
                    return;
                }
            }
        }

        public static void Debug(string s) {
            Console.ForegroundColor = (ConsoleColor) new Random().Next(16);
            Console.WriteLine(s);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
