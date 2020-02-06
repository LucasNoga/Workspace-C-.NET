namespace DataUser
{
    /// <summary>
    /// Utilisateur du l'application
    /// </summary>
    public class User
    {
        /// <summary>
        /// // description supplementaire fourni par la reponse
        /// </summary>
        public string description { get; set; }

        /// <summary>
        /// Les donnees sur l'utilisateur
        /// </summary>
        public Items items { get; set; }

        /// <summary>
        /// le code de status de la requete
        /// </summary>
        public string status { get; set; }

        /// <summary>
        /// nom de la requete effectuer sur la BD
        /// </summary>
        public string title { get; set; }

        /// <summary>
        /// Affiche dans la console l'utilisateur
        /// </summary>
        public override string ToString()
        {
            return "Id: " + this.items.id + "\nNom: " + this.items.nom +
                "\nPrenom: " + this.items.prenom + "\nLoginAD: " + this.items.username_ad +
                "\nGUID: " + this.items.guid_ad + "\nIDServiceCurrent: " + this.items.id_serv_default +
                "\ndate_creat: " + this.items.date_creat + "\ndate_ferm: " +
                this.items.date_ferm + "\ndate_maj: " + this.items.date_maj + "\ndroits: " + this.items.droits +
                "\nfax: " + this.items.fax + "\ntel: " + this.items.tel + "\nnouveau: " + this.items.nouveau +
                "\nmail: " + this.items.mail + "\nmaj_groupe: " + this.items.maj_grp +
                "\nversion: " + this.items.version + "\nutil_general: " + this.items.util_gen;
        }
    }

    /// <summary>
    /// Contient les donnees de l'utilisateur
    /// </summary>
    public class Items
    {
        /// <summary>
        /// Date de création de l'utilisateur
        /// </summary>
        public string date_creat { get; set; }

        /// <summary>
        /// Date de fermeture de l'utilisateur
        /// </summary>
        public object date_ferm { get; set; }

        /// <summary>
        /// Date de mise à jour de l'utilisateur
        /// </summary>
        public string date_maj { get; set; }

        /// <summary>
        /// Droits de l'utilisateur.
        /// Peut modifier les tables principales : +1
        /// Peut modifier les tables secondaires : +2
        /// Peut modifier les droits utilisateur : +4
        /// Peut afficher la supervision : +8
        /// Peut usurper l'identité : +16
        /// </summary>
        public int droits { get; set; }

        /// <summary>
        /// Num fax de l'utilisateur
        /// </summary>
        public string fax { get; set; }

        /// <summary>
        /// le guid de la machine
        /// </summary>
        public string guid_ad { get; set; }

        /// <summary>
        /// identifiant de l'utilisateur de la base de donnees
        /// </summary>
        public int id { get; set; }

        /// <summary>
        /// Identifiant du service d'affectation par défaut de l'utilisateur
        /// </summary>
        public int id_serv_default { get; set; } // ID service par defaut

        /// <summary>
        /// mail de l'utilisateur
        /// </summary>
        public string mail { get; set; }

        /// <summary>
        /// Les groupes de l'utilisateur doivent t'ils êtres mis a jour
        /// </summary>
        public string maj_grp { get; set; }

        /// <summary>
        /// nom de l'utilisateur
        /// </summary>
        public string nom { get; set; }

        /// <summary>
        /// La création de l'utilisateur est nouvelle (O/N)
        /// </summary>
        public string nouveau { get; set; }

        /// <summary>
        /// prenom de l'utilisateur
        /// </summary>
        public string prenom { get; set; }

        /// <summary>
        /// Num téléphone de l'utilisateur
        /// </summary>
        public string tel { get; set; }

        /// <summary>
        /// Infos AD de l'utilisateur
        /// </summary>
        public string username_ad { get; set; }

        /// <summary>
        /// L'utilisateur est il un utilisateur général (Tout le monde peut t'il voir ses fax
        /// </summary>
        public int util_gen { get; set; }

        /// <summary>
        /// Dernière version de HylaOnER utilisée
        /// </summary>
        public object version { get; set; }
    }
}
