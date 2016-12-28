﻿namespace NServiceBus.SqlServer.AcceptanceTests.MultiCatalog
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;
    using Transport.SQLServer;

    public class When_using_multicatalog : NServiceBusAcceptanceTest
    {
        static string SenderConnectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=nservicebus1;Integrated Security=True";
        static string ReceiverConnectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=nservicebus2;Integrated Security=True";
        static string ReceiverEndpoint => Conventions.EndpointNamingConvention(typeof(Receiver));
        static string SenderEndpoint => Conventions.EndpointNamingConvention(typeof(Sender));


        [Test]
        public Task Should_be_able_to_send_message_to_input_queue_in_different_catalog()
        {
            return Scenario.Define<Context>()
                .WithEndpoint<Sender>(c => c.When(s => s.Send(new Message())))
                .WithEndpoint<Receiver>()
                .Done(c => c.ReplyReceived)
                .Run();
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    var routing = c.UseTransport<SqlServerTransport>()
                        .ConnectionString(SenderConnectionString)
                        .UseSchemaForEndpoint(ReceiverEndpoint, "receiver")
                        .UseCatalogForEndpoint(ReceiverEndpoint, "nservicebus2")
                        .Routing();

                    routing.RouteToEndpoint(typeof(Message), ReceiverEndpoint);
                });
            }


            class Handler : IHandleMessages<Reply>
            {
                public Context Context { get; set; }

                public Task Handle(Reply message, IMessageHandlerContext context)
                {
                    Context.ReplyReceived = true;

                    return Task.FromResult(0);
                }
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.UseTransport<SqlServerTransport>()
                        .ConnectionString(ReceiverConnectionString)
                        .UseSchemaForEndpoint(ReceiverEndpoint, "receiver")
                        .UseCatalogForEndpoint(SenderEndpoint, "nservicebus1");
                });
            }

            class Handler : IHandleMessages<Message>
            {
                public Context Context { get; set; }

                public Task Handle(Message message, IMessageHandlerContext context)
                {
                    return context.Reply(new Reply());
                }
            }
        }

        protected class Message : ICommand
        {
        }

        protected class Reply : IMessage
        {
        }

        protected class Context : ScenarioContext
        {
            public bool ReplyReceived { get; set; }
        }
    }
}