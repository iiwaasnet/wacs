using System;
using System.Diagnostics;
using System.Threading;
using wacs.Communication.Hubs.Client;
using wacs.Configuration;
using wacs.Messaging.Messages;
using wacs.Messaging.Messages.Client.wacs;
using ZeroMQ;

namespace wacs.Client
{
    internal class Program
    {
        private static readonly string ServerEndpoint = "tcp://127.0.0.1:4030";

        private static void Main(string[] args)
        {
            using (var context = ZmqContext.Create())
            {
                using (var socket = context.CreateSocket(SocketType.REQ))
                {
                    socket.Connect(ServerEndpoint);
                    var timer = new Stopwatch();
                    while (true)
                    {
                        timer.Start();
                        var request = new CreateNodeRequest(new Messaging.Messages.Process{Id = 0},
                                                            new CreateNodeRequest.Payload
                                                            {
                                                                NodeName = "A"
                                                            });
                        socket.SendMessage(new ZmqMessage(new ClientMultipartMessage(request).Frames));
                        var resp = socket.ReceiveMessage();
                        var response = new ClientMultipartMessage(resp);
                        var msg = new Message(new Envelope {Sender = new Messaging.Messages.Process{Id = response.GetSenderId()}},
                                              new Body
                                              {
                                                  MessageType = response.GetMessageType(),
                                                  Content = response.GetMessage()
                                              });
                        var payload = new CreateNodeResponse(msg).GetPayload();

                        timer.Stop();

                        Console.WriteLine("NodeIndex: {0} done in {1} msec", payload.NodeIndex, timer.ElapsedMilliseconds);
                        timer.Reset();

                        //Thread.Sleep(TimeSpan.FromSeconds(5));
                    }
                }
            }
        }
    }
}