using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DankMemes.GPSOAuthSharp;
using Newtonsoft.Json;
using PokemonGo.RocketAPI.Exceptions;

namespace PokemonGo.RocketAPI.Login
{
    public static class GoogleLoginGPSOAuth
    {
        public static async Task<string> DoLogin(string username, string password)
        {
            GPSOAuthClient client = new GPSOAuthClient(username, password);
            Dictionary<string, string> response = client.PerformMasterLogin();

            if (response.ContainsKey("Error"))
                throw new Exception(response["Error"]);
            
            if (!response.ContainsKey("Auth"))
                throw new Exception();

            Dictionary<string, string> oauthResponse = client.PerformOAuth(response["Token"],
                "audience:server:client_id:848232511240-7so421jotr2609rmqakceuu1luuq0ptb.apps.googleusercontent.com",
                "com.nianticlabs.pokemongo",
                "321187995bc7cdc2b5fc91b11a96e2baa8602c62");

            if (!oauthResponse.ContainsKey("Auth"))
                throw new Exception();

            return oauthResponse["Auth"];
        }
    }
}