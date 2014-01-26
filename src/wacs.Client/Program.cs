using System;
using System.Diagnostics;
using wacs.Communication.Hubs.Client;
using wacs.Messaging.Messages;
using wacs.Messaging.Messages.Client.wacs;
using ZeroMQ;
using Process = wacs.Messaging.Messages.Process;

namespace wacs.Client
{
    internal class Program
    {
        private static readonly string ServerEndpoint = "tcp://127.0.0.1:4030";

        private static void Main(string[] args)
        {
            var commandTimeout = TimeSpan.FromSeconds(1);
            using (var context = ZmqContext.Create())
            {
                while (true)
                {
                    try
                    {
                        using (var socket = context.CreateSocket(SocketType.REQ))
                        {
                            socket.Connect(ServerEndpoint);
                            var timer = new Stopwatch();
                            while (true)
                            {
                                SendRequests(timer, socket, commandTimeout);
                            }
                        }
                    }
                    catch (Exception err)
                    {
                        Console.WriteLine(err);
                    }
                }
            }
        }

        private static void SendRequests(Stopwatch timer, ZmqSocket socket, TimeSpan commandTimeout)
        {
            timer.Start();
            var request = new CreateNodeRequest(new Process {Id = 0},
                                                new CreateNodeRequest.Payload
                                                {
                                                    NodeName = "A"
                                                });
            socket.SendMessage(new ZmqMessage(new ClientMultipartMessage(request).Frames));

            var resp = socket.ReceiveMessage(commandTimeout);
            if (resp.IsComplete)
            {
                var response = new ClientMultipartMessage(resp);
                var msg = new Message(new Envelope {Sender = new Process {Id = response.GetSenderId()}},
                                      new Body
                                      {
                                          MessageType = response.GetMessageType(),
                                          Content = response.GetMessage()
                                      });
                var payload = new CreateNodeResponse(msg).GetPayload();

                timer.Stop();

                Console.WriteLine("NodeIndex: {0} done in {1} msec", payload.NodeIndex, timer.ElapsedMilliseconds);
            }
            timer.Reset();

            //Thread.Sleep(TimeSpan.FromSeconds(5));
        }
    }
}