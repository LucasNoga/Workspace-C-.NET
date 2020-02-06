using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices; // TODO msgWindows
using System.Windows.Forms;
using Newtonsoft.Json;

using HylaNOTIFY;
namespace MessagesRabbit
{
    /// <summary>
    /// Classe pour recuperer les données d'un fax passé par RabbitMQ
    /// </summary>
    public class FaxRabbit
    {
        /// <summary>
        /// numero du fax d'emission
        /// </summary>
        public string numsender { get; set; }

        /// <summary>
        /// nombre de page du fax
        /// </summary>
        public int npages { get; set; }

        /// <summary>
        /// le service qui a emis ou recu le fax
        /// </summary>
        public int id_service { get; set; }

        /// <summary>
        /// le type de message, reception ou emission (RX-NEW, RX-UPD, TX-UPD)
        /// </summary>
        public string type { get; set; }
    }

    /// <summary>
    /// message recuperer dans la queue System
    /// </summary>
    class MessageSystem
    {
        /// <summary>
        /// Type du messge NEW_FAX, SERVICE_CHANGE
        /// </summary>
        public string type { get; set; }

        /// <summary>
        /// Données du message
        /// </summary>
        public Items items { get; set; }

        /// <summary>
        /// Données du message
        /// </summary>
        public class Items
        {
            /// <summary>
            /// Url sur laquelle diriger l'utilisateur
            /// </summary>
            public string url = null;

            /// <summary>
            /// le navigateur a utiliser s'il est spécifié
            /// </summary>
            public string navigateur = null;

            /// <summary>
            /// Paramètres fourni au navigateur
            /// </summary>
            public string options_nav = null;

            /// <summary>
            /// si l'utilisateur change de service par defaut, il sera spécifié ici
            /// </summary>
            public string idService = null;
        }
    }

    /// <summary>
    /// Classe gerant les messages rabbit
    /// </summary>
    public class MessageRabbit
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
        /// Gestion des logs
        /// </summary>
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Methodes traitant les messages rabbit Abonnement ou System
        /// </summary>
        /// <param name="Context">Le context de l'application</param>
        /// <param name="json">le message rabbit</param>
        /// <param name="typeMessage">le type de message abonnement ou system</param>
        public static void processingMessage(HylaNotify Context, string json, string typeMessage)
        {
            switch (typeMessage)
            {
                //si c'est un message via la queue abonnement
                case "subscribing":
                    processingSubscribing(json, Context);
                    break;
                //si c'est un message system
                case "system":
                    processingSystem(json, Context);
                    break;
            }
        }

        /// <summary>
        /// Traitement un message de la queue abonnement
        /// </summary>
        /// <param name="json">le message rabbit</param>
        /// <param name="Context">Le context de l'application</param>
        public static void processingSubscribing(string json, HylaNotify Context)
        {
            FaxRabbit fax = null;
            bool success;
            try
            {
                fax = JsonConvert.DeserializeObject<FaxRabbit>(json);
                success = true;
            }
            catch (JsonReaderException)
            {
                Log.Debug("Fax invalide\n" + json);
                success = false;
            }

            //Si la deserialisation s'est bien passé
            if (success)
            {
                Log.Info("Fax valide");
                //TODO passe le message dans une methode appeler traitement_MESSAGE_ABO
                Log.Debug("Type de fax" + fax.type.Substring(0, 2));

                switch (fax.type.Substring(0, 2))
                {
                    // Si le fax est une reception
                    case "RX":
                        Log.Debug("fax en reception");

                        // Si HylaOnEr est lancé
                        if (Context.HylaOnER != IntPtr.Zero) // TODO msgWindows
                        {
                            if (Context.InvokeRequired) // TODO msgWindows
                            {
                                //Envoie le message de mise a jour a hylaOner
                                Context.Invoke(new MethodInvoker(delegate
                                {
                                    // TODO msgWindows
                                    SendMessage(Context.HylaOnER, RegisterWindowMessage("Hylafax_UPDATEREC"), Context.Handle, IntPtr.Zero);
                                }));
                            }
                        }
                        //on verifie si c'est un nouveau fax 
                        if (fax.type.Equals("RX-NEW"))
                        {
                            Context.AfficherPopup(fax);//dans ce cas on affiche de la popup
                            Log.Debug("Nouveau fax, affichage popup");
                        }
                        break;

                    // Si le fax est en emission
                    case "TX":
                        Log.Debug("fax en emission");
                        // Si HylaOnEr est lancé
                        if (Context.HylaOnER != IntPtr.Zero) // TODO msgWindows
                        {
                            if (Context.InvokeRequired) // TODO msgWindows
                            {
                                //Envoie le message de mise a jour a hylaOner
                                Context.Invoke(new MethodInvoker(delegate
                                {
                                    // TODO msgWindows
                                    SendMessage(Context.HylaOnER, RegisterWindowMessage("Hylafax_MajEmission"), Context.Handle, IntPtr.Zero);
                                }));
                            }
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Traitement un message de la queue System
        /// </summary>
        /// <param name="json">le message rabbit</param>
        /// <param name="Context">Le context de l'application</param>
        public static void processingSystem(string json, HylaNotify Context)
        {
            MessageSystem msgSystem = null;
            bool success;
            try
            {
                msgSystem = JsonConvert.DeserializeObject<MessageSystem>(json);
                success = true;
            }
            catch (JsonReaderException)
            {
                Log.Error("Message system invalide\n" + json);
                success = false;
            }
            if (success)
            {
                Log.Info("Message systeme valide");
                // TODO passe le message dans une methode appeler traitement_MESSAGE_SYSTEM
                // Execution differente dependant du type de message
                switch (msgSystem.type)
                {
                    case "NEW_FAX_SEND":
                        Log.Debug("Systeme: Envoi d'un nouveau fax");

                        // Si pas de nav renseigné on met le navigateur par default
                        if (msgSystem.items.navigateur.Equals("") || msgSystem.items.navigateur == null)
                            msgSystem.items.navigateur = "chrome.exe";
                        // On lance l'url dans le nav
                        Context.LaunchNav(msgSystem.items.url, msgSystem.items.navigateur, msgSystem.items.options_nav);
                        break;
                    case "SERVICE_CHANGE":
                        Log.Debug("Systeme: Changement de service");
                        break;

                }
            }
        }
    }
}
