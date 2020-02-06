using System;
using System.Windows.Forms;
using System.Threading;

//TODO mettre un switch case args pour lancer en mode console 

namespace HylaNOTIFY
{
    static class Program
    {
        /// <summary>
        /// Mutex pour le singleton
        /// </summary>
        public static Mutex _machineLocalAppInstanceMutex = null;

        /// <summary>
        /// Application hylanotify
        /// </summary>
        static HylaNotify instance;

        /// <summary>
        /// Gestion des logs
        /// </summary>
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Point d'entrée principal de l'application.
        /// </summary>
        [STAThread]
        static void Main(String[] args)
        {
            // Demarrage classique de l'application
            if (args.Length == 0)
            {
                string mutexName = string.Format("Global\\~{0}~{1}~{2}", Application.ProductName, Environment.UserDomainName, Environment.UserName);
                Log.Debug("Nom du mutex: " + mutexName);

                Boolean mutexIsNew;
                _machineLocalAppInstanceMutex = new System.Threading.Mutex(true, mutexName, out mutexIsNew);

                //bloque le thread principale pour qu'une seule instance de l'application Hylanotify soit lancé
                if (mutexIsNew)
                {
                    Log.Debug("first launch");
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    instance = new HylaNotify();
                    Application.Run(instance);
                }
                else
                {
                    string message = "Une instance de HylaNotify est déjà lancée !";
                    string caption = "Erreur de démarrage";
                    MessageBoxButtons buttons = MessageBoxButtons.OK;
                    MessageBox.Show(message, caption, buttons);
                    Log.Info("Erreur, HylaNotify deja lancée" + instance);
                }
            }
        }
    }
}
