using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Input;
using System.Net;
using System.Web.Http;
using System.Collections;
using System.Net.Sockets;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.ComponentModel;
using System.Collections.Generic;
using System.Deployment.Application;
using System.Reflection;

//POPUP
using Tulpep.NotificationWindow;

//JSON
using Newtonsoft.Json;

//namespace solution
using DataUser;
using MessagesRabbit;
using QueueHylafax;
using System.Runtime.InteropServices; // TODO msgWindows

//TODO voir hostname et username useful ou non
namespace HylaNOTIFY
{
    //Regarder pour le changement de service par defaut
    /// <summary>
    /// genere la partie interface utilisateur
    /// </summary>
    public partial class HylaNotify : Form
    {
        /// <summary>
        /// Fonction Windows
        /// </summary>
        /// <param name="hwnd">handle</param>
        /// <param name="msg">aa</param>
        /// <param name="wparam">aa</param>
        /// <param name="lparam">aa</param>
        /// <returns>booleen pour savoir si le message est envoyé</returns>
        [DllImport("User32.dll")] // TODO msgWindows
        public static extern bool SendMessage(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam); // TODO msgWindows

        /// <summary>
        /// Fonction windows
        /// </summary>
        /// <param name="message">aa</param>
        /// <returns>id du message</returns>
        [DllImport("User32.dll")] // TODO msgWindows
        public static extern int RegisterWindowMessage(string message); //TODO msgWindows

        /// <summary>
        /// Gestion des logs
        /// </summary>
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().
            DeclaringType);

        /// <summary>
        /// Represente le handle de la fenetre HylaOnER TODO msgWindows
        /// </summary>
        public IntPtr HylaOnER;

        /// <summary>
        /// Popup s'affichant lorsqu'on recoit des fax avec les infos du fax
        /// </summary>
        public PopupNotifier Popup;

        /// <summary>
        /// booleen qui represente l'affichage de la popup. true = visible, false = invisible
        /// </summary>
        public bool NotifIsGone = false;

        /// <summary>
        /// host de l'utilisateur de la machine
        /// </summary>
        public string Hostname = "";

        /// <summary>
        ///  username de la session connectee
        /// </summary>
        public string Username = "";

        /// <summary>
        /// represente la configuration de l'aplication
        /// </summary>
        public Config Config;

        /// <summary>
        /// Represente la gestion de rabbit
        /// </summary>
        RabbitMQManager RabbitMQ;

        /// <summary>
        /// objet de connexion a l'API pour recuperer les donnees du user dans la base
        /// </summary>
        DataApi Api;

        /// <summary>
        /// Thread pour gerer la partie connexion a l'API
        /// </summary>
        Thread threadConnexion;

        /// <summary>
        /// l'utilisateur de l'application
        /// </summary>
        User User = null;

        /// <summary>
        /// booleen pour savoir si le curseur et 
        /// </summary>
        bool CursorOnIcon;

        /// <summary>
        /// Config %ALLUSERSPATH%
        /// </summary>
        string allUserPath = null;

        /// <summary>
        /// Config HylaNotify.exe
        /// </summary>
        string exePath = null;

        /// <summary>
        /// Connexion true = connecté a l'api. false = echec connexion avec l'api
        /// </summary>
        bool IsConnected = false;

        /// <summary>
        /// Config a true = configuration de l'application établie
        /// </summary>
        bool IsConfigUpdate = false;

        /// <summary>
        /// Authentification du user dans la base reussi = true
        /// </summary>
        bool IsAuthentified = false;

        /// <summary>
        /// Version du projet
        /// </summary>
        public string CurrentVersion = GetVersion();

        /// <summary>
        /// initialisation de l'ihm
        /// </summary>
        public HylaNotify()
        {
            InitializeComponent(); // Creer les composants pour l'appli
        }

        /// <summary>
        /// Version du projet
        /// </summary>
        /// <returns>Retourne la version du projet</returns>
        public static string GetVersion()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fvi.FileVersion;
        }

        /// <summary>
        /// Chargement de l'IHM et declenchement de la requete POST
        /// </summary>
        private void Form1_Load(object sender, EventArgs e)
        {
            Log.Info("Initialisation de l'application : Form.Load");

            Visible = false; // Hide form window.
            ShowInTaskbar = false; // Remove from taskbar.

            //recupere le nom host et le nom de l'utilisateur
            InitNotify();

            //TODO voir ce qu'on en fait
            Log.Debug("Version du projet: " + CurrentVersion); ;

            //Initalisation du design du popup
            InitPopup();

            // Mise en place de la connexion avec l'API
            Api = new DataApi(this);

            // Recupère dynamiquement la configuration de l'application
            SetupConfiguration();

            // Met en place le menu de maniere dynamique
            SetMenu();

            //Connexion a l'API
            threadConnexion = new Thread(ConnexionApi);
            threadConnexion.IsBackground = true;
            threadConnexion.Start();

            //TODO Verifie si il y a une nouvelle version d'HylaNotify
            //new AppUpdate(Config.items.update);
        }


        /// <summary>
        /// Procédure d'initialisation des variables et objets
        /// Récuperation de l'hostname et de l'username de la session
        /// </summary>
        private void InitNotify()
        {
            //recup hostname local
            try
            {
                Hostname = Dns.GetHostName();
                if (Hostname.Length == 0)
                {
                    //Debug.Log("Hostname vide => on quitte l'application !");
                    ExitApplication();
                }
            }
            catch (SocketException e)
            {
                Log.Error("SocketException !!!");
                Log.Error("Source : " + e.Source);
                Log.Error("Message : " + e.Message);
            }

            //recup login AD du user connecte
            Username = Environment.UserName;
            Log.Info("User name (login AD session): " + Username);
            if (Username.Length == 0)
            {
                Log.Info("Username vide => on quitte !");
                ExitApplication();
            }
        }

        //TODO voir si je ne peux pas la mettre dans le designer
        /// <summary>
        /// Inititalisation des parametres du popup en terme d'affichage et d'evenement
        /// </summary>
        private void InitPopup()
        {
            Popup = new PopupNotifier();
            Popup.Size = new Size(300, 60);
            Popup.Disappear += new System.EventHandler(this.IsGone);
            Popup.Image = Properties.Resources.inbox_fax;
            Popup.ImagePadding = new Padding(5);
            Popup.TitleColor = Color.FromName("Black");
            Popup.TitlePadding = new Padding(0, 5, 0, 0);
            Popup.Scroll = false;
            Popup.AnimationInterval = 10; //interval de rafraichissement pendant l'animation
            Popup.Delay = 5000; //Temps d'affichage de la popup
            Popup.AnimationDuration = 1000; //temps de l'animation //1000
            Popup.BodyColor = Color.FromArgb(187, 182, 181);
            Popup.Click += new System.EventHandler(this.PopupClick);
        }

       

        //TODO mettre cette methode dans la Config en static
        //TODO tester avec 2 users pour voir les accès concurent
        /// <summary>
        /// Recupere le fichier de configuration en local (config.remote.json) 
        /// ou dans le repertoire ProgramData\HylaNotify\config.remote.json
        /// permettant de recuperer les donnees essentielles au fonctionnement du programme 
        /// (menu, adresse Api, Adresse RabbitMQ, etc...)
        /// </summary>
        /// <returns>La config locale</returns>
        private void SetupConfiguration()
        {
            // Fichier %ALLUSERSPROFILE%
            allUserPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
                + @"\HylaNotify\config.remote.json";

            //conf dans l'exe
            exePath = Application.StartupPath + @"\" + "config.local.json";
            Log.Info(File.Exists(allUserPath) ?
                "fichier config %ALLUSERS% existe"
                : "fichier config %ALLUSERS% existe pas");
            Log.Info(File.Exists(exePath) ?
                "fichier config EXE Hylanotify existe"
                : "fichier config EXE Hylanotify existe pas");

            bool allUserExist = File.Exists(allUserPath);

            // Recupère la config local soit dans %ALLUSERSPROFILE% soit dans l'EXE
            Config = Config.getLocalFileConfig(allUserPath, exePath);

            //Si pas de config ni dans %ALLUSER% ni dans l'exe
            if (Config == null)
            {
                string message = "Aucun fichier de configuration détecté\nArrêt de HylaNotify !";
                string caption = "Erreur de démarrage";
                MessageBoxButtons buttons = MessageBoxButtons.OK;
                MessageBox.Show(message, caption, buttons);
                ExitApplication();
            }
            ////////////////////////////METTRE DANS GETLOCALCONFIG
            // On recupere un serveur valide ayant acces a l'API
            Api.Server = Api.GetServer();

            // TODO on recupere les 2IPs et on fait un appel a l'API
            // Tant que la config n'est pas a jour
            while (!IsConfigUpdate)
            {
                Log.Debug("Checksum locale: " + Config.items.conf.checksum);
                Log.Debug("Préparation de la configuration");
                CheckConfig();
                System.Threading.Thread.Sleep(1000);
                if (!IsConfigUpdate)
                {
                    //Si le fichier dans alluser n'existe pas on attend sa réecriture
                    if (!allUserExist)
                    {
                        CheckConfig();
                        System.Threading.Thread.Sleep(3000);
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(3000);
                    }
                }
            }
        }

        /// <summary>
        /// Verifie si la config recupere en local est a jour
        /// </summary>
        /// <returns>si le checksum de la config local est egale au checksum de la config distante</returns>
        private async void CheckConfig()
        {
            // Config recuperé via l'Api;
            Config configRemote = await Api.GetConfigJson(Config.items.api, Config.items.conf.checksum)
                .ConfigureAwait(false);

            // Si l'API n'est pas joignable, on conserve la version stocké dans le rep %ALLUSERSPROFILE%
            if (configRemote == null)
            {
                NotifyUser("ERROR", "Echec Connexion", "Le fichier de configuration n'est pas disponible", false);
            }
            // Si l'Api nous renvoie une config
            else
            {
                // si config.remote.json est a jour
                if (configRemote.status.Equals("OKIDEM"))
                {
                    NotifyUser("INFO", "Configuration", "La configuration est déjà à jour", false);

                    // Valide la configuration récuperer
                    IsConfigUpdate = true;
                }

                // si config.remote.json est a mettre a jour
                else if (configRemote.status.Equals("OK"))
                {
                    NotifyUser("INFO", "Configuration", "Mise a jour de la config en local", false);

                    // Sauvegarde de la config API dans le %ALLUSERSPROFILE%;
                    Config.SaveConfig(allUserPath, configRemote);

                    // Lecture de la config du %ALLUSERSPROFILE%
                    Config = Config.getLocalFileConfig(allUserPath, exePath);

                    // Valide la configuration récuperer
                    IsConfigUpdate = true;
                    Log.Debug("Nouveau checksum: " + Config.items.conf.checksum);
                }

                // Si la config de l'api n'est pas valide
                else if (configRemote.status.Equals("KO"))
                {
                    NotifyUser("INFO", "Configuration", "La configuration de l'api n'est pas valide", false);
                }
            }
        }

        /// <summary>
        /// Ajoute les routes du menu contextuel de maniere dynamique 
        /// (à partir du fichier de conf recuperer via la méthode SetConfiguration())
        /// </summary>
        private void SetMenu()
        {
            //remise a jour du menu dans le cas d'une reactualisation
            contextMenuStrip.Items.Clear();

            Log.Debug("Config dynamique du menu");
            //recupere le menu de l'application
            Config.Menu menu = Config.items.menu;
            int indexItem = 0, indexSubMenu = 0;

            //Création des sous menus Sinon on fait des sous-menu 
            // Pour chaque item du menuItem on lui assigne son url
            foreach (Config.SubMenu subMenu in menu.subMenu)
            {
                ToolStripMenuItem itemMenu = new ToolStripMenuItem();
                itemMenu.Text = subMenu.libelle;
                itemMenu.Tag = indexSubMenu++;
                itemMenu.DropDownItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.toolStripDropDownItem_DropDownItemClicked);
                foreach (Config.Item item in subMenu.items)
                {
                    Console.WriteLine("nom de l'item " + item.libelle);
                    ToolStripItem it = new ToolStripMenuItem();
                    it.Text = item.libelle;
                    it.Tag = indexItem++;
                    // Blocage de l'item dans le cas ou il n'y a pas d'element
                    if (item.parametres == "" && item.programme == "" && item.url == "")
                        it.Enabled = false;

                    // Ajout des items dans le menu
                    if (menu.subMenu.Count == 1)
                    {
                        contextMenuStrip.Items.Add(it);
                    }

                    // Ajout des items dans le sous-menu
                    else
                    {
                        contextMenuStrip.Items.Add(itemMenu);
                        itemMenu.DropDownItems.Add(it);
                    }
                }
            }
        }

        /// <summary>
        /// Demande a l'automate de mettre a jour les données de l'utilisateur dans la BD puis valide la connexion
        /// </summary>
        private void ConnexionApi()
        {
            IsConnected = false;
            IsAuthentified = false;
            icone.Icon = Properties.Resources.sablier;
            //Annonce de l'utilisateur
            while (!IsConnected)
            {
                NotifyUser("INFO", "Demande d'authentification", "Connexion en cours", false);
                Log.Debug("Anonnce de l'utilisateur");
                Log.Debug("Connexion en cours");
                icone.Icon = Properties.Resources.sablier; //TODO animer le sablier                
                AnnonceUser();
                System.Threading.Thread.Sleep(1000);
                //temps d'attente avant relance de la requete
                if (!IsConnected)
                    System.Threading.Thread.Sleep(500);
            }
            System.Threading.Thread.Sleep(500);

            //Authentification de l'utilisateur
            while (!IsAuthentified)
            {
                Log.Debug("Authentification de l'utilisateur");
                icone.Icon = Properties.Resources.sablier; //TODO animer le sablier
                AuthentifieUser();
                //temps d'attente avant relance de la requete
                if (!IsAuthentified)
                    System.Threading.Thread.Sleep(1000);
            }


            // Id de service de l'utilisateur valide
            if (CheckUserIdService())
            {
                NotifyUser("INFO", "Service par défaut: " + User.items.id_serv_default, "ID de service valide", false);
                // Creation des queues et des consumers Rabbit
                NotifyUser("INFO", "Queues Rabbit", "Création des queues", false);
                CreateQueueReceive();
                // Authentification terminé
                NotifyUser("INFO", "Authentification réussi", "Authentification réussi " + DateTime.Now, false);
                icone.Icon = Properties.Resources.fax;
            }

            // Id de service de l'utilisateur non valide
            else
            {
                NotifyUser("ERROR", "Service par défaut: " + User.items.id_serv_default, "ID de service non valide", false);
                // TODO voir ce qu'on fait ensuite
            }
            //connexion terminé on arrete le thread
            threadConnexion.Abort();
        }

        /// <summary>
        /// Verifie si l'utilisateur à un service par defaut valide 
        /// </summary>
        /// <returns>true si id valide, false sinon</returns>
        private bool CheckUserIdService()
        {
            string id = "" + User.items.id_serv_default;
            int value;
            // Si c'est un chiffre
            if (int.TryParse(id, out value))
                return true;

            // Si c'est différent d'un chiffre (NULL)
            else
                return false;
        }

        /// <summary>
        /// Requete POST effectuer pour modifier le champ maj_grp à O
        /// Demande a l'automate de mettre a jour les données de l'utilisateur dans la BD puis valide la connexion
        /// Executé Tant que la requete POST n'est pas passé
        /// </summary>
        private async void AnnonceUser()
        {
            Application.DoEvents(); //TODO msg windows

            // Demande à l'automate de mettre a jour les données du USER
            string retourPost = await Api.PostUser(Config.items.api).ConfigureAwait(false);

            //si la requete n'as rien retourné
            if (retourPost == null)
                NotifyUser("ERROR", "Echec connexion", "La connexion au serveur a échoué", false);

            // si la requete retourne quelque chose
            else
            {
                NotifyUser("INFO", "Connexion API", "L'utilisateur s'est connecté à l'api", false);
                IsConnected = true;
            }
            Log.Debug("Contenu de la requete POST: " + retourPost);
            return; //evite les appels recursifs
        }


        /// <summary>
        /// Vérifie si l'automate est bien passé pour mettre à jour les utilisateurs
        /// Executé Tant que la requete GET n'est pas passé
        /// </summary>
        private async void AuthentifieUser()
        {
            Application.DoEvents(); //gere les messages windows

            //url pour acceder a l'api
            User = await Api.GetUser(Config.items.api).ConfigureAwait(false);

            // Si la requete n'as rien donné
            if (User == null)
                NotifyUser("ERROR", "Echec connexion", "La connexion au serveur a échoué", false);

            // Si la requete retourne les donnees sur le user
            else
            {
                //Verifie que l'automate a bien mis a jour les donnes du User
                if (User.items.maj_grp.Equals("N"))
                {
                    NotifyUser("INFO", "Utilisateur à jour", "Utilisateur à jour", false);
                    IsAuthentified = true; //on stop les requetes GET a l'API
                    //return; // TODO a tester
                }
                Log.Debug("Contenu de la reponse du GET a l'api: " + User);
            }
            return;
        }

        //TODO voir si je ne peux pas la mettre dans le designer
        /// <summary>
        /// Appeler lorsque la popup de nouveau fax disparait
        /// </summary>
        /// <param name="sender">la popup</param>
        /// <param name="e">egale a null</param>
        public void IsGone(object sender, EventArgs e)
        {
            NotifIsGone = true;
        }

        /// <summary>
        /// Creer les queues et les consumers System, Abonnement
        /// </summary>
        private void CreateQueueReceive()
        {
            //on recupere l'id du service par defaut du user
            string idServiceDefaut = User.items.id_serv_default.ToString();
            Log.Info("Id par defaut du User: " + idServiceDefaut);
            RabbitMQ = new RabbitMQManager(this);

            // Abonnement fax
            RabbitMQ.CreateQueueAbonnement(idServiceDefaut);
            RabbitMQ.CreateConsumerSubscribing(idServiceDefaut);
            NotifyUser("INFO", "Abonnement fax", "Abonnement au service par defaut pour la réception des fax", false);

            // messages systemes
            RabbitMQ.CreateQueueSystem();
            RabbitMQ.CreateConsumerSystem();
            NotifyUser("INFO", "Messages systèmes", "Réception des messages systèmes", false);
        }

        /// <summary>
        /// Methode affichant la popup avec les donnees du fax
        /// </summary>
        /// <param name="fax">le fax</param>
        public void AfficherPopup(FaxRabbit fax)
        {
            icone.Icon = Properties.Resources.document_new;//on modifie l'icone
            Popup.TitleText = "Nouveau fax en réception";
            Popup.ContentText = "Reçu de " + fax.numsender + " - " + fax.npages + (fax.npages == 1 ? " page" : " pages ");

            //Gestion du Thread ou la popup est appelée 
            if (InvokeRequired)
            {
                this.Invoke(new MethodInvoker(delegate { Popup.Popup(); }));
                // Affiche la popup avec les données du fax en parametres
                while (!NotifIsGone)
                {
                    System.Threading.Thread.Sleep(1);
                }
                NotifIsGone = false;
            }
        }

        /// <summary>
        /// Lancement de HylaOnER lorsqu'on double clic sur l'icone
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void icone_DoubleClick(object sender, EventArgs e)
        {
            //Accès si le user est authentifé dans la bd
            if (IsAuthentified)
            {
                icone.Icon = Properties.Resources.fax;
                //TODO Ouvrir de HylaOner
                // TODO recuperer l'item ayant le Text gestion des fax voir si l'enumerateur ne le fait pas
                OpenService(Config.items.menu.subMenu[0].items[0], 0);// TODO en attendant
            }
            else
            {
                NotifyUser("ERROR", "Accès refusé", "Vous n'avez pas accès à cette application", false);
            }
        }

        //TODO a finir a tester et a commenter dans le cas ou plusieurs sous-menu voir 
        //si je peux de ce sous-menu cliqué annuler les actions de la methode dessous
        void toolStripDropDownItem_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            string msg = String.Format("Item clicked: {0}", e.ClickedItem.Text);
            Log.Debug(msg);
        }


        /// <summary>
        /// Methode qui gere le clique sur un des items du menu contextuel
        /// </summary>
        /// <param name="sender">le menu contextuel</param>
        /// <param name="e">l'item cliqué</param>
        private void contextMenuStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            //TODO il faut gerer le click sur le menu puis sur l'item
            //nom de l'item cliqué

            int index = int.Parse(e.ClickedItem.Tag.ToString());
            Config.Item service = Config.items.menu.subMenu[0].items[index];

            // Si il est authentifié il peut lancer le service
            if (IsAuthentified)
            {
                OpenService(service, index);
            }
            else
            {
                NotifyUser("ERROR", "Erreur", "Vous n'êtes pas encore authentifié", true);
            }
        }

        /// <summary>
        /// Declenche lorsque l'on clique sur la popup de notification
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PopupClick(object sender, EventArgs e)
        {
            Log.Info("Popup Cliqué");
            icone.Icon = Properties.Resources.fax;
            // TODO Ouvrir de HylaOner
            // TODO recuperer l'item ayant le Text gestion des fax voir si l'enumerateur ne le fait pas
            OpenService(Config.items.menu.subMenu[0].items[0], 0);// TODO en attendant
        }

        /// <summary>
        /// Ouvre le service demandé par l'utilisateur dans le menu contextuel
        /// </summary>
        /// <param name="item">l'item selectionné dans le menu</param>
        /// <param name="index">indice ou se trouve l'item dans le menu</param>
        /// //TODO a meliorer la methode en recuperant le nom du service
        private void OpenService(Config.Item item, int index)
        {
            Log.Debug("Lancement de " + item.libelle);
            switch (item.parametres)
            {
                case "reactualiser":
                    // Ré-Authentification de l'utilisateur
                    Reactualisation();
                    break;
                case "quitter":
                    // Demande de l'utilisatuer pour quitter l'application 
                    QuitterApplication();
                    break;
                default:
                    // si une url alors on lance le programme avec l'url
                    if (item.url != "")
                    {
                        // Lancement du navigateur
                        LaunchNav(item.url, item.programme, item.parametres);
                    }
                    else
                    {
                        // on lance le programme correspondant au tag
                        try
                        {
                            Process.Start(item.programme);

                        }
                        catch (Exception e)
                        {
                            Log.Info(e.StackTrace);
                            contextMenuStrip.Items[index].Enabled = false;
                            NotifyUser("ERROR", "Erreur", "Le programme que vous avez demandé n'existe pas", true);
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Reactualisation des fax
        /// Nouvelle authentification de l'utilisateur
        /// </summary>
        private void Reactualisation()
        {
            Log.Debug("Reactualisation des fax");
            // on recharge la configuration
            Log.Debug("Rechargement de la configuration");
            SetupConfiguration();
            SetMenu();

            // On remet a jour l'utilisateur
            Log.Debug("Réauthentification de l'utilisateur");
            threadConnexion.Abort();
            threadConnexion = new Thread(ConnexionApi);
            threadConnexion.IsBackground = true;
            threadConnexion.Start();
        }

        /// <summary>
        /// Methode qui lance le navigateur
        /// </summary>
        /// <param name="url">>url demandé contenant protocole, port et ressource</param>
        /// <param name="nav">navigateur a executer</param>
        public void LaunchNav(string url, string nav, string options_nav)
        {
            // si des options sont fournis
            if (options_nav != null)
                options_nav = options_nav + " ";

            // sinon on renvoie null
            else
                options_nav = "";

            //si l'url n'est pas vide
            if (url != null || !url.Equals(""))
            {
                Process proc = new Process();
                ProcessStartInfo processStarInf = new ProcessStartInfo(); ;
                processStarInf.FileName = nav; // Le processus sait quel programme lancer
                processStarInf.Arguments = options_nav + url; // Direction du navigateur vers la bonne url

                // Test si le navigateur est disponible sur la machine
                try
                {
                    proc = Process.Start(processStarInf);
                }

                // Lancement du nav par defaut si navigateur inconnu
                catch (Exception e)
                {
                    Log.Error("Erreur lors du lancement du navigateur: " + e.Message + e.GetType().ToString());
                    Log.Info("le navigateur demandé n'existe pas");
                    Log.Info("Lancement du navigateur par défaut");
                    proc = new Process();
                    proc = Process.Start(url);
                    NotifyUser("ERROR", "Erreur", "Le programme que vous avez demandé n'existe pas", true);
                }
            }
        }

        /// <summary>
        /// Demande a l'utilisateur une confirmation pour quitter l'application
        /// </summary>
        private void QuitterApplication()
        {
            string message = "Voulez-vous quitter HylaNotify ?";
            string caption = "Quitter HylaNotify";
            MessageBoxButtons buttons = MessageBoxButtons.YesNo;
            DialogResult dialogResult = MessageBox.Show(message, caption, buttons);
            if (dialogResult.Equals(DialogResult.Yes))
            {
                Log.Info("L'utilisateur veut quitter Hylanotify");
                ExitApplication();
            }
            else
            {
                Log.Info("L'utilisateur ne veut pas quitter Hylanotify");
            }
        }

        /// <summary>
        /// Quitte l'application HylaNotify et l'ensemble de ses processus
        /// </summary>
        public void ExitApplication()
        {
            Environment.Exit(1); // Quitte les threads secondaires
        }

        /// <summary>
        /// Detecte lorsque l'utilisateur a son curseur su la trayIcon
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void icone_MouseMove(object sender, MouseEventArgs e)
        {
            //TODO a finir
            /*  Log.Debug("sender: " + sender.ToString());
              Log.Debug("Location sender : " + sender);
              Log.Debug("LOcation e " + e.Location);
              Log.Debug("X e " + e.X);
              Log.Debug("Y e " + e.Y);
              Log.Debug("args: " + e.ToString());


              Point position = Cursor.Position; // Te donne la position en coordonnées "écran" 
              Log.Debug("Position " + position);
              if (position.X == 0 && position.Y == 0)
              {
                  Log.Debug("curseur on");
              }
              else
              {
                  
              }

              // TODO a faire CursorOnIcon = true;*/
            //Log.Debug("curseur of");
        }

        /// <summary>
        /// Notifie l'utilisateur le résultat des actions qu'il entreprend avec HylaNotify
        /// </summary>
        /// <param name="statut">le type du message</param>
        /// <param name="title">le titre du balloon</param>
        /// <param name="text">le contenu du balloon</param>
        /// <param name="display">booleen pour savoir si par defaut on affiche ou non le balloob</param>
        public void NotifyUser(string statut, string title, string text, bool display)
        {
            ToolTipIcon toolTipIcon = ToolTipIcon.None;
            Icon icon = null;
            // ERROR ou INFO
            switch (statut)
            {
                case "ERROR":
                    toolTipIcon = ToolTipIcon.Error;
                    icon = Properties.Resources.Exclamation_mark_icon;
                    Log.Error(text);
                    break;

                case "INFO":
                    toolTipIcon = ToolTipIcon.Info;
                    Log.Info(text);
                    break;
            }

            icone.Text = text;

            //On ne change pas de l'icone courrante
            if (icon != null)
                icone.Icon = icon;

            // Si le curseur est sur l'icone, affichage de BalloonTip
            //TODO a finir if(CursorOnIcon)
            //{
            // si on doit positionner le curseur sur l'icone
            icone.BalloonTipText = text;
            icone.BalloonTipIcon = toolTipIcon;
            icone.BalloonTipTitle = title;
            if (display)
            {
                icone.ShowBalloonTip(1000);
            }
        }

        //TODO msgWindows
        /// <summary>
        /// Methode qui traite l'ensemble des messages windows
        /// </summary>
        /// <param name="m">le message</param>
        protected override void WndProc(ref Message m) //TODO msgWindows
        {
            // Si HylaOnER est lancé
            if (m.Msg == RegisterWindowMessage("HY_Message_OnER"))
            {
                icone.Icon = Properties.Resources.fax; //modifie l'icone de HylaNotify
                HylaOnER = m.WParam;
                Log.Debug("id message Windows :" + m.Msg);
                Log.Debug("Wparam de HyLaOner: " + m.WParam);
                Log.Debug("Handle de HylaNotify" + this.Handle);
                string str = "HY_Handle_client";
                SendMessage(m.WParam, RegisterWindowMessage(str), this.Handle, IntPtr.Zero);
                Console.WriteLine("Message: " + str + " de HylaNotify a HylaOnER");
            }

            // Si HylaOnER est arreté
            if (m.Msg == RegisterWindowMessage("HY_SetStatusOff"))
            {
                Log.Info("HylaOnER est arreté");
                HylaOnER = IntPtr.Zero;
            }
            base.WndProc(ref m);
        }

        // lancer hylaOner
        private void icone_MouseDoubleClick(object sender, MouseEventArgs e)
        {

        }
    }
}