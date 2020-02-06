using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.DirectoryServices.AccountManagement;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

using HylaNOTIFY;

namespace DataUser
{
    /// <summary>
    /// Classe qui appelle l'api pour mettre a jour les donnees de l'utilisateur de la session(maj_grp = 'O')
    /// peut aussi recuperer les informations dans la base de donnees sur l'utilisateur
    /// </summary>
    /// 
    public class DataApi
    {
        /// <summary>
        /// Gestion des logs
        /// </summary>
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Application HylaNotify
        /// </summary>
        private HylaNotify Context;

        /// <summary>
        /// Serveur utilisé pour la connexion a l'api
        /// </summary>
        public Config.Server Server {get; set;}

        /// <summary>
        /// Constructeur pour l'API
        /// </summary>
        /// <param name="context">l'application HylaNotify</param>
        public DataApi(HylaNotify context) { 
            Context = context;
        }
        /// <summary>
        /// Retourne le premier serveur valide pour l'API
        /// </summary>
        public Config.Server GetServer(){
            foreach(Config.Server serv in Context.Config.items.api.server)
            {
                string adresseServer = serv.protocol + @"://" + serv.host + @":" + serv.port + @"/";
                Log.Debug("Test avec l'adresse serveur : " + adresseServer);
                HttpClient httpClient = new HttpClient();
                bool success = false;
                try
                {
                    httpClient.BaseAddress = new Uri(adresseServer); //adresse ou se trouve l'API
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var response = httpClient.GetAsync(adresseServer).Result;
                    Log.Debug("resultat de l'adresse " + adresseServer + " :::: " + response.StatusCode);
                    success = true;
                }
                catch (UriFormatException)
                {
                    Log.Info("L'API à l'adresse " + adresseServer + " n'est pas valide");
                    success = false;
                }
                catch (Exception)
                {
                    Log.Info("L'API " + adresseServer + " n'est pas joignable");
                    success = false;
                }
                // si API valide on retourne l'adresse du serveur sinon on continue
                if (success)
                    return serv;
            }
            return null;
        }

        /// <summary>
        /// Connexion a l'api avec le bon entete
        /// </summary>
        /// <param name="addressServer">adresse du serveur</param>
        /// <returns></returns>
        static HttpClient ConnectAPI(string addressServer)
        {
            Log.Debug("Adresse serveur : " + addressServer);
            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(addressServer); //adresse ou se trouve l'API
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return httpClient;
        }

        /// <summary>
        /// Effectue une requete GET de maniere aynchrone sur l'API pour recuperer les donnees sur l'utilisateur
        /// </summary>
        /// <param name="api">les donnees sur l'api, adresse du serveur etc...</param>
        /// <returns>l'objet FaxUser</returns>
        public async Task<User> GetUser(Config.Api api)
        {
            string guid = GetUserGUID(Environment.UserName).ToString("D");
            Log.Info("Requete GET Verification de l'authentification de l'utilisateur ayant un GUID:" + guid);
            // Connexion au serveur
            string addressServer = Server.protocol + @"://" + Server.host + @":" + Server.port + @"/";
            HttpClient httpClient = ConnectAPI(addressServer); //Connexion a l'api

            // On recupere le GUID de la machine pour recuperer la bonne url
            string url = api.ressource.user + @"/" + guid;
            Log.Debug("Url de la ressource: " + url);
            User user = null;

            try
            {
                // Recuperation du User
                HttpResponseMessage response = await httpClient.GetAsync(url).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                user = JsonConvert.DeserializeObject<User>(json);
            }
            catch (Exception e)
            {
                Log.Error("Erreur requete GET: " + e.Message + e.GetType().ToString());
                return null;
            }
            return user;
        }

        /// <summary>
        /// Effectue une requete POST de maniere aynchrone sur l'API
        /// </summary>
        /// <param name="api">les donnees sur l'api, adresse du serveur etc...</param>
        /// <returns>la reponse de la requete</returns>
        public async Task<string> PostUser(Config.Api api)
        {
            string guid = GetUserGUID(Environment.UserName).ToString("D");
            Log.Info("Requete POST, Annonce de l'utilisateur ayant un GUID: " + guid);

            //Connexion au serveur
            string addressServer = Server.protocol + @"://" + Server.host + @":" + Server.port + @"/";
            HttpClient httpClient = ConnectAPI(addressServer); //Connexion a l'api

            // On recupere le GUID de la machine pour recuperer la bonne url
            string url = api.ressource.user + @"/" + guid;
            Log.Debug("Url de la ressource: " + url);
            string responseBody = null;

            try
            {
                var data = "";
                StringContent queryString = new StringContent(data);
                HttpResponseMessage response = await httpClient.PostAsync(url, queryString).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                responseBody = await response.Content.ReadAsStringAsync();
            }
            catch (Exception e)
            {
                Log.Error("Erreur requete POST: " + e.Message + e.GetType().ToString());
                Context.NotifyUser("ERROR", "Connexion impossible", "Vous n'etes pas connecté au serveur", false);
                return null;
            }
            return responseBody;
        }

        /// <summary>
        /// Requete GET a l'api pour recuperer la configuration de l'application au format JSON
        /// </summary>
        /// <param name="api">l'API ou faire les requetes</param>
        /// <param name="checksum">le checksum de la conf</param>
        /// <returns>la config de l'application</returns>
        public async Task<Config> GetConfigJson(Config.Api api, string checksum)
        {
            //TODO Voir si je rajout l'ip IPV4 en c# du client
            //Connexion au serveur
            string addressServer = Server.protocol + @"://" + Server.host + @":" + Server.port + @"/";
            HttpClient httpClient = ConnectAPI(addressServer); //Connexion a l'api

            // On recupere le GUID de la machine pour recuperer la bonne url
            string url = api.ressource.config + @"?checksum=" + checksum;
            Log.Debug("Url de la ressource: " + url);

            Config config = null;

            try
            {
                // test si l'url est valide 
                HttpResponseMessage response = await httpClient.GetAsync(url).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                config = JsonConvert.DeserializeObject<Config>(json);
            }
            catch (Exception e)
            {
                Log.Error("Erreur requete GET: " + e.Message + e.GetType().ToString());
                return null;
            }
            return config;
        }

        /// <summary>
        /// retourne le guid de la machine
        /// </summary>
        /// <param name="user_loginAD">le login de l'utilisateur sur la machine</param>
        /// <returns>le guid de la machine</returns>
        public static System.Guid GetUserGUID(string user_loginAD)
        {
            PrincipalContext ctx = new PrincipalContext(ContextType.Domain, "stef-tfe.nt", "dc=stef-tfe, dc=nt");
            UserPrincipal user = UserPrincipal.FindByIdentity(ctx, user_loginAD);
            return (System.Guid)user.Guid;
        }
    }
}