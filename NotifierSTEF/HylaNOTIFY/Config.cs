using System;
using System.Collections;
using System.IO;
using Newtonsoft.Json;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Collections.Generic;

namespace DataUser
{
    /// <summary>
    /// Classe de configuration
    /// </summary>
    public class Config
    {
        /// <summary>
        /// Gestion des logs
        /// </summary>
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Descrption de la config
        /// </summary>
        public string description { get; set; }

        /// <summary>
        /// donnees récuperé pour la config
        /// </summary>
        public Items items { get; set; }

        /// <summary>
        /// Code de status renvoyé
        /// </summary>
        public string status { get; set; }

        /// <summary>
        /// Titre de la requete
        /// </summary>
        public string title { get; set; }

        /// <summary>
        /// serveur sur lequel se connecter pour acceder a l'api
        /// </summary>
        public class Server
        {
            /// <summary>
            /// Protocol de communication : http
            /// </summary>
            public string protocol { get; set; }

            /// <summary>
            /// Adresse serveur
            /// </summary>
            public string host { get; set; }


            /// <summary>
            /// port utilisé sur le serveur
            /// </summary>
            public string port { get; set; }
        }

        /// <summary>
        /// Contient les ressources
        /// </summary>
        public class Ressource
        {
            /// <summary>
            /// url de la requete: pour les users (mise a jour groupe, etc...)
            /// </summary>
            public string user { get; set; }

            /// <summary>
            /// url de la requete: pour la config
            /// </summary>
            public string config { get; set; }
        }


        /// <summary>
        /// Classe pour acceder a l'API
        /// </summary>
        public class Api
        {
            /// <summary>
            /// List des server pour se connecter a l'API
            /// </summary>
            public List<Server> server { get; set; }

            /// <summary>
            /// Contient les urls pour acceder au données
            /// </summary>
            public Ressource ressource { get; set; }
        }

        /// <summary>
        /// Classe contenant la description de ce fichier de conf
        /// </summary>
        public class Conf
        {
            /// <summary>
            /// Version de la config (MD5)
            /// </summary>
            public string checksum { get; set; }
        }

        /// <summary>
        /// Represente un item du menu contextuel
        /// </summary>
        public class Item
        {
            /// <summary>
            /// Nom de l'item
            /// </summary>
            public string libelle { get; set; }

            /// <summary>
            /// Url a acceder dans le cas de la version web
            /// </summary>
            public string url { get; set; }

            /// <summary>
            /// Exe ou navigateur a lancer dans le cas ou une url est referencé
            /// </summary>
            public string programme { get; set; }

            /// <summary>
            /// Parametre supplementaires en plus du lancement du programme
            /// ou action exceptionnel definit quitter, reactualiser
            /// </summary>
            public string parametres { get; set; }
        }

        /// <summary>
        /// Sous-menu dans le cas du menu contextuel 
        /// </summary>
        public class SubMenu
        {
            /// <summary>
            /// Nom du sous menu
            /// </summary>
            public string libelle { get; set; }

            /// <summary>
            /// List des items a placer dans ce sous-menu
            /// </summary>
            public List<Item> items { get; set; }
        }

        /// <summary>
        ///  Menu contextuel de l'application 
        /// </summary>
        public class Menu
        {
            /// <summary>
            /// Liste des sous-menus
            /// </summary>
            public List<SubMenu> subMenu { get; set; }
        }

        /// <summary>
        /// Classe de donnees pour la connexion et la gestion des queues RabbitMQ
        /// </summary>
        public class Rabbit
        {
            /// <summary>
            /// adresse du serveur rabbit
            /// </summary>
            public string host { get; set; }

            /// <summary>
            /// Nom de l'exchange rabbit
            /// </summary>
            public string exchange { get; set; }

            /// <summary>
            /// Nom de la queue Abonnement de fax
            /// </summary>
            public string QueueAbo { get; set; }

            /// <summary>
            /// Nom de la queue System
            /// </summary>
            public string QueueSystem { get; set; }
        }

        /// <summary>
        /// Items de la requete
        /// </summary>
        public class Items
        {
            /// <summary>
            /// Donnees relative a l'api
            /// </summary>
            public Api api { get; set; }

            /// <summary>
            /// Donnees relative a Rabbit
            /// </summary>
            public Rabbit rabbit { get; set; }

            /// <summary>
            /// configuration de l'application
            /// </summary>
            public Conf conf { get; set; }

            /// <summary>
            /// Donnees relative au menu
            /// </summary>
            public Menu menu { get; set; }

            /// <summary>
            /// Chemin Json pour l'update
            /// </summary>
            public string update { get; set; }
        }

        /// <summary>
        /// Regarde si une config existent et la renvoi a HylaNotify
        /// </summary>
        /// <param name="allUserPath">config %ALLUSERSPROFILE%</param>
        /// <param name="exePath">config HylaNotify.exe</param>
        /// <returns>la config de l'appli</returns>
        public static Config getLocalFileConfig(string allUserPath, string exePath)
        {
            Log.Debug("Emplacement du fichier JSON remote " + allUserPath);
            Log.Debug("Emplacement du fichier JSON local " + exePath);
            Config localConfig = null;

            // Si la config dans %ALLUSERSPROFILE% existe on la recupere
            if (File.Exists(allUserPath))
            {
                Log.Debug("Fichier json existant, emplacement du Json " + allUserPath);
                localConfig = getConfig(allUserPath);
            }
            // Si la config dans %ALLUSERSPROFILE% n'existe pas
            else
            {
                // Si la config locale existante
                if (File.Exists(exePath))
                {
                    Log.Debug("Fichier json existant, emplacement du Json " + exePath);
                    localConfig = getConfig(exePath);
                }
                // config locale inexistante
                else
                {
                    Log.Debug("Aucun JSON existent");
                    return null;
                }
            }
            return localConfig;
        }

        /// <summary>
        /// Recupere la config present dans path
        /// </summary>
        /// <param name="path">fichier stockant la config</param>
        /// <returns>la config de l'appli</returns>
        public static Config getConfig(string path)
        {
            try
            {
                Log.Info("La config " + path + " existe");
                string json = File.ReadAllText(path); // Recupere le contenu du fichier de config
                return JsonConvert.DeserializeObject<Config>(json);
            }
            catch (JsonReaderException)
            {
                Log.Debug("Json Incomplet");
                Log.Info("Informations manquantes dans le json");
                return null;
            }
        }

        /// <summary>
        /// Sauvegarde le json dans le repertoire %ALLUSERSPROFILE% si il n'existe pas
        /// </summary>
        /// <param name="configRemotePath">le chemin de la config distante</param>
        public static void SaveConfig(string target, Config config)
        {
            string json = JsonConvert.SerializeObject(config);
            Log.Info("ecriture de la config dans le rep: " + target);
            FileInfo targetFile = new FileInfo(target);

            // Gestion du dossier
            Log.Debug("Chemin du rep HylaNotify " + targetFile.Directory.FullName);

            bool exists = System.IO.Directory.Exists(targetFile.Directory.FullName);
            if (!exists)
            {
                Log.Info("Dossier " + targetFile.Directory.Name +" en cours de création");
                // Sauvegarde du JSON
                DirectoryInfo di = System.IO.Directory.CreateDirectory(targetFile.Directory.FullName);
                DirectoryInfo dInfo = new DirectoryInfo(targetFile.Directory.FullName);
                DirectorySecurity dSecurity = dInfo.GetAccessControl();
                dSecurity.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.FullControl, InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit, PropagationFlags.NoPropagateInherit, AccessControlType.Allow));
                dInfo.SetAccessControl(dSecurity);
                Log.Info("Droits ajoutés au dossier " + targetFile.Directory.Name);
                Log.Info("Dossier " + targetFile.Directory.Name + " créé");
            }

            // Gestion du fichier
            try
            {
                //TODO Utiliser le formatting JSON pour mettre au format json le config.remote.json
                StreamWriter sw = new StreamWriter(target);
                sw.WriteLine(json);
                sw.Close();
                Log.Debug("Stockage du json dans le fichier");
            }
            catch (Exception)
            {
                Log.Error("Impossible de créer le fichier de conf au niveau de: " + target);
            }
        }
    }
}
