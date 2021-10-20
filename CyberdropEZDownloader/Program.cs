using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net;
using System.IO;

namespace CyberdropEZDownloader
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // initialisation programme
            Console.Title = "Cyberdrop EZ Downloader - Bolzofstil";
            Console.WriteLine("Cyberdrop EZ Downloader");
            Console.WriteLine("Entre le lien ici :");

            // récupération du lien
            string givenLink =  Console.ReadLine();

            // vérification que le lien donné est bien une URL valide, sinon rejet
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            using HttpClient client = new HttpClient();
            bool result = Uri.TryCreate(givenLink, UriKind.Absolute, out Uri uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            Uri link = new Uri(givenLink);

            // vérification supplémentaire avec le lien, pour éviter qu'un malin joue avec d'autres sites.
            if (result == true && link.Host == "cyberdrop.me")
            {
                // récupération de l'HTML de la page pour analyse
                var response = await client.GetStringAsync(givenLink);

                // regex 1 : détecter le nom donné au dossier Cyberdrop
                Match regex1 = Regex.Match(response, "<h1 id=\"title\" class=\"title has-text-centered\" title=\"(.+?)\">", RegexOptions.Compiled);
                var nom_album = regex1.Groups[1].Value;

                // regex 2 : trouver le nombre de fichiers inclus dans le dossier
                MatchCollection matches = Regex.Matches(response, "class=\"image\" href=\"(https://fs-0([0-9]).cyberdrop.(cc|to)/(.+?))\"", RegexOptions.Compiled);

                // le compteur est surtout visuel
                int compteur = 0;

                // ça permet aussi de voir si le lien Cyberdrop est valide, sans vraiment le dire
                if (matches.Count != 0)
                {
                    // changement du titre CMD
                    Console.Title = "Cyberdrop EZ Downloader - " + nom_album;
					
                    string lienDossier = Environment.CurrentDirectory + '\\' + nom_album;
                    if (!Directory.Exists(lienDossier))
                    {
                        Directory.CreateDirectory(lienDossier);
                    }

                    foreach (Match links in matches)
                    {
                        // incrémenter le compteur à chaque fichier téléchargé
                        compteur += 1;
                        
                        // récupérer le lien Cyberdrop depuis le match regex.
                        string lien = links.Groups[1].Value;

                        // retrouver le nom du fichier sur le lien de "téléchargement"
                        Uri url = new Uri(lien);
                        string filename = Path.GetFileName(url.LocalPath);

                        // le console.clear() est pour éviter de tout afficher quand il télécharge, 1 à la fois ça suffit
                        Console.Clear();
                        Console.WriteLine("Téléchargement de : " + filename);
                        Console.WriteLine("Progression : " + compteur + " / " + matches.Count);

                        // logique téléchargement avec try/catch pour vérif.
                        using var downloader = new HttpClient();
                        try
                        {
                            var request = new HttpRequestMessage(HttpMethod.Get, lien);
                            var sendTask = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                            var document = sendTask.Result.EnsureSuccessStatusCode();
                            var httpStream = await document.Content.ReadAsStreamAsync();
                            var lien_fichier = Path.Combine(lienDossier, filename);
                            using var fileStream = File.Create(lien_fichier);
                            using var reader = new StreamReader(httpStream);
                            httpStream.CopyTo(fileStream);
                            fileStream.Flush();
                        }
                        catch (HttpRequestException ex)
                        {
                            Console.WriteLine("Un ou plusieurs fichiers n'ont pas pu être téléchargés : ", ex.Message);
                        }
                    }
                    // bien vu
                    Console.WriteLine("Terminé ! Profite !");
                }
                else
                {
                    // si le lien est mort, qui ne possède aucun fichier...
                    Console.WriteLine("Lien mort ou aucun fichier trouvé sur le lien donné.");
                }
            } else
            {
                // si l'URI n'est pas valide ou n'est pas Cyberdrop, rejeter la demande.
                Console.WriteLine("Soit le lien donné n'est pas un lien, soit vous m'avez donné un lien qui ne provient pas du site Cyberdrop. Veuillez réessayer avec un nouveau lien.");
            }
        }    
    }
}
