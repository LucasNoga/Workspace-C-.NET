using System;
using System.Text;
using System.Windows.Forms;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

using Newtonsoft.Json;

using HylaNOTIFY;
using MessagesRabbit;
using DataUser;

using System.Runtime.InteropServices; // TODO msgWindows

namespace QueueHylafax
{
    /// <summary>
    /// Gestion des queues RabbitMQ
    /// </summary>
    public class RabbitMQManager
    {
        /// <summary>
        /// Fonction Windows
        /// </summary>
        /// <param name="hwnd">aa</param>
        /// <param name="msg">aa</param>
        /// <param name="wparam">aa</param>
        /// <param name="lparam">aa</param>
        /// <returns></returns>
        [DllImport("User32.dll")] // TODO msgWindows
        public static extern bool SendMessage(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam); // TODO msgWindows

        /// <summary>
        /// Fonction windows
        /// </summary>
        /// <param name="message">aa</param>
        /// <returns></returns>
        [DllImport("User32.dll")] // TODO msgWindows
        public static extern int RegisterWindowMessage(string message); // TODO msgWindows

        /// <summary>
        /// la connexion au serveur RabbitMQ
        /// </summary>
        private IConnection Connection = null;

        /// <summary>
        /// //le channel qui contient les queues abonnement et system
        /// </summary>
        public IModel Channel = null;

        /// <summary>
        /// Context de l'application pour manipuler le popup
        /// </summary>
        private HylaNotify Context = null;

        /// <summary>
        /// Donnees relative a Rabbit
        /// </summary>
        private Config.Rabbit Rabbit;
        
        /// <summary>
        /// Nom de la queue abonnement
        /// </summary>
        private string QUEUE_NAME_SUBSCRIBING = null;

        /// <summary>
        /// Nom de la queue System
        /// </summary>
        private string QUEUE_NAME_SYSTEM = null;

        /// <summary>
        /// nom du virtual Host Rabbit
        /// </summary>
        private static string VIRTUAL_HOST = "HYLAFAX";

        /// <summary>
        /// Gestion des logs
        /// </summary>
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Objet pour creer les queue de reception
        /// </summary>
        /// <param name="context">l'appli HylaNotify</param>
        public RabbitMQManager(HylaNotify context)
        {
            this.Context = context; //on recupere le context de l'application
            Rabbit = Context.Config.items.rabbit;//recupere les donnees de rabbit
            QUEUE_NAME_SUBSCRIBING = context.Hostname.ToUpper() + @"\" + context.Username.ToUpper() + @"\" + "HYL" + @"\" + "ABO";
            QUEUE_NAME_SYSTEM = context.Hostname.ToUpper() + @"\" + context.Username.ToUpper() + @"\" + "HYL" + @"\" + "SYS";
            var factory = new ConnectionFactory()
            {
                HostName = Rabbit.host,
                VirtualHost = VIRTUAL_HOST
            };

            Log.Debug("Connexion Rabbit");
            try
            {
                Connection = factory.CreateConnection();
                Channel = Connection.CreateModel();
            }
            catch (Exception)
            {
                System.Threading.Thread.Sleep(10000); // 1 min avant de relancer
                Log.Debug("Impossible de se connecter au serveur rabbit");
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Creation de la queue pour recevoir les fax via un systeme de routage
        /// le routage es effectué avec une cle qui represente l'id du service du consumer
        /// </summary>
        public void CreateQueueAbonnement(string idServiceDefaut)
        {
            Channel.ExchangeDeclare(exchange: Rabbit.exchange, type: "direct");

            Channel.QueueDeclare(queue: QUEUE_NAME_SUBSCRIBING,
                          durable: false, //queue non persistante
                          exclusive: false, //on veut pas une queue pour un seul consumer
                          autoDelete: true, //on veut que la queue soit delete lorsqu'elle n' a plus de consumer
                          arguments: null);

            // Routing pour la reception
            string routingReception = "RX" + idServiceDefaut;
            Channel.QueueBind(queue: QUEUE_NAME_SUBSCRIBING,
                                  exchange: Rabbit.exchange,
                                  routingKey: routingReception);

            // Routing pour l'emission
            string routingEmission = "TX" + idServiceDefaut;
            Channel.QueueBind(queue: QUEUE_NAME_SUBSCRIBING,
                                  exchange: Rabbit.exchange,
                                  routingKey: routingEmission);
        }

        /// <summary>
        /// Creation de la queue pour recevoir les messages systemes tel que fax emis
        /// </summary>
        public void CreateQueueSystem()
        {
            Channel.QueueDeclare(queue: QUEUE_NAME_SYSTEM,
                                    durable: false,
                                    exclusive: false,
                                    autoDelete: true,
                                    arguments: null);

        }

        /// <summary>
        /// Creation du consumer pour la queue Abonnement de RabbitMQ qui recupere les messages routes pour son l'id de son service
        /// </summary>
        /// <param name="idService">l'id du service de l'utilisateur</param>
        /// <returns></returns>
        public void CreateConsumerSubscribing(string idService)
        {
            var consumer = new EventingBasicConsumer(Channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body;
                var json = Encoding.UTF8.GetString(body); //on recupere le fax en json
                var routingKey = ea.RoutingKey;

                MessageRabbit.processingMessage(this.Context, json, "subscribing");
                
            };

            Channel.BasicConsume(queue: QUEUE_NAME_SUBSCRIBING,
                                 autoAck: true,
                                 consumer: consumer);
        }

        /// <summary>
        /// Envoi un message system a la queue Rabbit nottament pour envoyer vers les applications correspondantes
        /// </summary>
        /// <param name="message">le json pour envoyer l'utilisateur vers la bonne url</param>
        public void publishMessage(string message)
        {
            Channel.BasicPublish(addr: new PublicationAddress("", "", QUEUE_NAME_SYSTEM),
                basicProperties: null,
                body: Encoding.UTF8.GetBytes(message));

            Console.WriteLine(" [x] Sent {0}", message);
        }

        /// <summary>
        /// Creer un consumer pour la queue system
        /// </summary>
        public void CreateConsumerSystem()
        {
            var consumer = new EventingBasicConsumer(Channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body;
                var json = Encoding.UTF8.GetString(body);
                Log.Info("Le consumer " + consumer.ConsumerTag + " a recu le message systeme: " + json);

                MessageRabbit.processingMessage(this.Context, json, "system");
               
            };

            Channel.BasicConsume(queue: QUEUE_NAME_SYSTEM,
                                 autoAck: true,
                                 consumer: consumer);
        }
    }
}
