#region

using AllEnum;
using PokemonGo.RocketAPI.Enums;
using PokemonGo.RocketAPI.Exceptions;
using PokemonGo.RocketAPI.Extensions;
using PokemonGo.RocketAPI.GeneratedCode;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace PokemonGo.RocketAPI.Console
{
    internal class Program
    {
        private static readonly ISettings ClientSettings = new Settings();
        private static Thread commandthread;
        private static DateTime TimeStarted = DateTime.Now;
        public static DateTime InitSessionDateTime = DateTime.Now;
        public static int defaultDelay = 200;
        public static int expDone = 0;
        public static int pokemonCaught = 0;

        static string userName;

        public static double GetRuntime()
        {
            return ((DateTime.Now - TimeStarted).TotalSeconds) / 3600;
        }
        public static void ColoredConsoleWrite(ConsoleColor color, string text)
        {
            ConsoleColor originalColor = System.Console.ForegroundColor;
            System.Console.ForegroundColor = color;
            System.Console.WriteLine(text);
            System.Console.ForegroundColor = originalColor;
        }

        public static string _getSessionRuntimeInTimeFormat()
        {
            return (DateTime.Now - InitSessionDateTime).ToString(@"dd\.hh\:mm\:ss");
        }
        public static void updateTitle()
        {
            System.Console.Title = $"{userName} | Exp/h {(int)(expDone / GetRuntime())} | Exp {expDone} | Pmon {pokemonCaught} | Pmon/h {(int)(pokemonCaught / GetRuntime())} ";
        }

        public static void addExp(int exp)
        {

            expDone += exp;
            updateTitle();
        }

        public static void addPokemon()
        {
            expDone += 210;
            pokemonCaught++;
            updateTitle();
        }

        private static async void Execute()
        {
            ColoredConsoleWrite(ConsoleColor.Green, $"QuickPokeBOT 1.2 - Fast exp bot");
            ColoredConsoleWrite(ConsoleColor.Red, $"This bot will transfer duplicate pokemons (keeping the highest cp one).");
            ColoredConsoleWrite(ConsoleColor.White, $"Before Starting check external.config file.");
            ColoredConsoleWrite(ConsoleColor.White, $"Increase/adjust requestDelay.");
            ColoredConsoleWrite(ConsoleColor.White, $"Check Credentials settings and mode [Google/Ptc].");
            ColoredConsoleWrite(ConsoleColor.White, $"Adjust item recycle settings.");
            ColoredConsoleWrite(ConsoleColor.White, $"This bot will not evolve anything.");            
            ColoredConsoleWrite(ConsoleColor.White, $"This bot will automatically wait for softban to finish.");
            ColoredConsoleWrite(ConsoleColor.White, $"");
            ColoredConsoleWrite(ConsoleColor.Green, $"This bot will start in 5 seconds...");
            ColoredConsoleWrite(ConsoleColor.White, $"");
            await Task.Delay(5000);
            var client = new Client(ClientSettings);
            try
            {
                defaultDelay = Int32.Parse(ClientSettings.requestsDelay);
                Client.requestDelay = defaultDelay;
                await Task.Delay(defaultDelay);
                if (ClientSettings.AuthType == AuthType.Ptc)
                    await client.DoPtcLogin(ClientSettings.PtcUsername, ClientSettings.PtcPassword);
                else if (ClientSettings.AuthType == AuthType.Google)
                    await client.DoGoogleLogin(ClientSettings.GoogleEmail, ClientSettings.GooglePassword);


                await Task.Delay(defaultDelay); //ColoredConsoleWrite(ConsoleColor.White, $"SetServer");
                await client.SetServer();
                await Task.Delay(defaultDelay); //ColoredConsoleWrite(ConsoleColor.White, $"GetProfile");
                var profile = await client.GetProfile();
                userName = profile.Profile.Username;
                await Task.Delay(defaultDelay); //ColoredConsoleWrite(ConsoleColor.White, $"GetSettings");
                var settings = await client.GetSettings();
                await Task.Delay(defaultDelay); //ColoredConsoleWrite(ConsoleColor.White, $"GetMapObjects");
                var mapObjects = await client.GetMapObjects();
                await Task.Delay(defaultDelay); //ColoredConsoleWrite(ConsoleColor.White, $"GetInventory");
                var inventory = await client.GetInventory();
                var pokemons = inventory.InventoryDelta.InventoryItems.Select(i => i.InventoryItemData?.Pokemon).Where(p => p != null && p?.PokemonId > 0);
                
                await Task.Delay(defaultDelay); //ColoredConsoleWrite(ConsoleColor.White, $"Transfer PK");
                await TransferDuplicatePokemon(client);

                //ColoredConsoleWrite(ConsoleColor.Red, "Recycling Items");
                await Task.Delay(defaultDelay); //ColoredConsoleWrite(ConsoleColor.White, $"client.RecycleItems(client)");
                await client.RecycleItems(client);


                //ColoredConsoleWrite(ConsoleColor.Red, "ExecuteFarmingPokestopsAndPokemons");
                await ExecuteFarmingPokestopsAndPokemons(client);

                ColoredConsoleWrite(ConsoleColor.Red, $"Finished Farming this zone. Wait 15 seconds then restart.");
                await Task.Delay(15000);
                Execute();
            }
            catch (TaskCanceledException tce) { ColoredConsoleWrite(ConsoleColor.White, $"[{DateTime.Now.ToString("HH:mm:ss")}] {Language.GetPhrases()["task_canceled_ex"]}"); Execute(); }
            catch (UriFormatException ufe) { ColoredConsoleWrite(ConsoleColor.White, $"[{DateTime.Now.ToString("HH:mm:ss")}] {Language.GetPhrases()["sys_uri_format_ex"]}"); Execute(); }
            catch (ArgumentOutOfRangeException aore) { ColoredConsoleWrite(ConsoleColor.White, $"[{DateTime.Now.ToString("HH:mm:ss")}] {Language.GetPhrases()["arg_out_of_range_ex"]}"); Execute(); }
            catch (ArgumentNullException ane) { ColoredConsoleWrite(ConsoleColor.White, $"[{DateTime.Now.ToString("HH:mm:ss")}] {Language.GetPhrases()["arg_null_ref"]}"); Execute(); }
        }

        private static void CommandIOThread()
        {
            string input;
            while (true)
            {
                input = System.Console.ReadLine();
                if (input == "exit")
                {
                    commandthread.Abort();
                    System.Environment.Exit(1);
                }
            } 
        }


        private static async Task ExecuteCatchAllNearbyPokemons(Client client)
        {
            await Task.Delay(defaultDelay); //ColoredConsoleWrite(ConsoleColor.White, $"GetMapObjects (pokemons)");
            var mapObjects = await client.GetMapObjects();

            var pokemons = mapObjects.MapCells.SelectMany(i => i.CatchablePokemons);

            foreach (var pokemon in pokemons)
            {
                await Task.Delay(defaultDelay); //ColoredConsoleWrite(ConsoleColor.White, $"EncounterPokemon");
                var encounterPokemonResponse = await client.EncounterPokemon(pokemon.EncounterId, pokemon.SpawnpointId);
                
                var pokemonCP = encounterPokemonResponse?.WildPokemon?.PokemonData?.Cp;
                CatchPokemonResponse caughtPokemonResponse;
                do
                {
                    await Task.Delay(defaultDelay); //ColoredConsoleWrite(ConsoleColor.White, $"CatchPokemon");
                    caughtPokemonResponse =
                        await
                            client.CatchPokemon(pokemon.EncounterId, pokemon.SpawnpointId, pokemon.Latitude,
                                pokemon.Longitude, MiscEnums.Item.ITEM_POKE_BALL, pokemonCP);
                    ;
                    if (caughtPokemonResponse.Status == CatchPokemonResponse.Types.CatchStatus.CatchMissed)
                    {
                        ColoredConsoleWrite(ConsoleColor.White, $"Retry");
                    }
                } while (caughtPokemonResponse.Status == CatchPokemonResponse.Types.CatchStatus.CatchMissed);
                string pokemonName = Language.GetPokemons()[Convert.ToString(pokemon.PokemonId)];

                if (caughtPokemonResponse.Status == CatchPokemonResponse.Types.CatchStatus.CatchSuccess)
                {
                    ColoredConsoleWrite(ConsoleColor.Green, $"{Language.GetPhrases()["caught_pokemon"].Replace("[pokemon]", pokemonName).Replace("[cp]", Convert.ToString(encounterPokemonResponse?.WildPokemon?.PokemonData?.Cp))}");
                    addPokemon();
                }
                else
                    ColoredConsoleWrite(ConsoleColor.Red, $"{Language.GetPhrases()["pokemon_got_away"].Replace("[pokemon]", pokemonName).Replace("[cp]", Convert.ToString(encounterPokemonResponse?.WildPokemon?.PokemonData?.Cp))}");
                
            }
        }

        private static async Task ExecuteFarmingPokestopsAndPokemons(Client client)
        {
            await Task.Delay(defaultDelay); //ColoredConsoleWrite(ConsoleColor.White, $"GetMapObjects (Pokestops)");
            var mapObjects = await client.GetMapObjects();

            var pokeStops = mapObjects.MapCells.SelectMany(i => i.Forts).Where(i => i.Type == FortType.Checkpoint && i.CooldownCompleteTimestampMs < DateTime.UtcNow.ToUnixTime());
            ColoredConsoleWrite(ConsoleColor.Red, $"Number of Pokestop: {pokeStops.Count()}");

            Location startLocation = new Location(client.getCurrentLat(), client.getCurrentLng());
            IList<FortData> query = pokeStops.ToList();
            
            while (query.Count > 10) //Ignore last 10 pokestop, usually far away
            {
                startLocation = new Location(client.getCurrentLat(), client.getCurrentLng());
                query = query.OrderBy(pS => Spheroid.CalculateDistanceBetweenLocations(startLocation, new Location(pS.Latitude, pS.Longitude))).ToList();
                var pokeStop = query.First();
                query.RemoveAt(0);

                await Task.Delay(defaultDelay); //ColoredConsoleWrite(ConsoleColor.White, $"UpdatePlayerLocation");
                var update = await client.UpdatePlayerLocation(pokeStop.Latitude, pokeStop.Longitude);
                //ColoredConsoleWrite(ConsoleColor.White, $"[{DateTime.Now.ToString("HH:mm:ss")}] Moved {(int)distanceToPokestop}m, wait {25.0 * (int)distanceToPokestop}ms, Number of Pokestop in this zone: {query.Count}");




                var pokeStopExp = 0;
                do
                {
                    await Task.Delay(defaultDelay);  //ColoredConsoleWrite(ConsoleColor.White, $"fortInfo");
                    var fortInfo = await client.GetFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);
                    await Task.Delay(defaultDelay);
                    var fortSearch = await client.SearchFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);
                    StringWriter PokeStopOutput = new StringWriter();

                    if (fortInfo.Name != string.Empty)
                        PokeStopOutput.Write($"PS {query.Count} | ");
                    if (fortSearch.ExperienceAwarded != 0)
                    {
                        PokeStopOutput.Write($"XP: {Convert.ToString(fortSearch.ExperienceAwarded)}" );
                    }
                    else
                    {
                        System.Console.ForegroundColor = ConsoleColor.Cyan;
                        System.Console.Write(".");
                    }
                    if (fortSearch.GemsAwarded != 0)
                        PokeStopOutput.Write($", {Language.GetPhrases()["gem"].Replace("[gem]", Convert.ToString(fortSearch.GemsAwarded))}");
                    if (fortSearch.PokemonDataEgg != null)
                        PokeStopOutput.Write($", {Language.GetPhrases()["egg"].Replace("[egg]", Convert.ToString(fortSearch.PokemonDataEgg))}");
                    if (GetFriendlyItemsString(fortSearch.ItemsAwarded) != string.Empty)
                        PokeStopOutput.Write($", {Language.GetPhrases()["item"].Replace("[item]", GetFriendlyItemsString(fortSearch.ItemsAwarded))}");
                    pokeStopExp = fortSearch.ExperienceAwarded;
                    if (fortSearch.ExperienceAwarded != 0)
                        ColoredConsoleWrite(ConsoleColor.Cyan, PokeStopOutput.ToString());
                    addExp(fortSearch.ExperienceAwarded);
                } while (pokeStopExp == 0);


                
                await ExecuteCatchAllNearbyPokemons(client);
                
            }
            ColoredConsoleWrite(ConsoleColor.White, $"[{DateTime.Now.ToString("HH:mm:ss")}] Finished pokestop route, reset position and restart.");

        }


        private static string GetFriendlyItemsString(IEnumerable<FortSearchResponse.Types.ItemAward> items)
        {
            var enumerable = items as IList<FortSearchResponse.Types.ItemAward> ?? items.ToList();

            if (!enumerable.Any())
                return string.Empty;

            return
                enumerable.GroupBy(i => i.ItemId)
                    .Select(kvp => new { ItemName = kvp.Key.ToString(), Amount = kvp.Sum(x => x.ItemCount) })
                    .Select(y => $"{y.Amount} x {y.ItemName}")
                    .Aggregate((a, b) => $"{a}, {b}");
        }

        private static void Main(string[] args)
        {
            try
            {
                commandthread = new Thread(CommandIOThread);
                commandthread.Start();
            }
            catch (Exception ex)
            {
                ColoredConsoleWrite(ConsoleColor.Red, $"[{DateTime.Now.ToString("HH:mm:ss")}] Unhandled exception: \n{ex}");
                ColoredConsoleWrite(ConsoleColor.White, $"[{DateTime.Now.ToString("HH:mm:ss")}] Press any key to exit the program...");
                System.Console.ReadKey();
                System.Environment.Exit(1);
            }

            try
            {
                Language.LoadLanguageFile("en_us");
            }
            catch (Exception ex)
            {
                ColoredConsoleWrite(ConsoleColor.Red, $"[{DateTime.Now.ToString("HH:mm:ss")}] Something's wrong when loading language file: \n{ex}");
                try
                {
                    ColoredConsoleWrite(ConsoleColor.White, $"[{DateTime.Now.ToString("HH:mm:ss")}] Using default en_us instead.");
                    Language.LoadLanguageFile("en_us");
                }
                catch
                {
                    ColoredConsoleWrite(ConsoleColor.Red, $"[{DateTime.Now.ToString("HH:mm:ss")}] Something's wrong when loading default language file again: \n{ex}");
                    ColoredConsoleWrite(ConsoleColor.White, $"[{DateTime.Now.ToString("HH:mm:ss")}] Please check if your language files are valid. Press any key to exit the program...");
                    System.Console.ReadKey();
                    System.Environment.Exit(1);
                }

            }
            Task.Run(() =>
            {
                try
                {
                    Execute();
                }
                catch (PtcOfflineException)
                {
                    ColoredConsoleWrite(ConsoleColor.Red, $"[{DateTime.Now.ToString("HH:mm:ss")}] {Language.GetPhrases()["ptc_server_down"]}");
                }
                catch (Exception ex)
                {
                    ColoredConsoleWrite(ConsoleColor.Red, $"[{DateTime.Now.ToString("HH:mm:ss")}] {Language.GetPhrases()["unhandled_ex"].Replace("[ex]", Convert.ToString(ex))}");
                }
            });
        }



        private static async Task TransferDuplicatePokemon(Client client)
        {
            await Task.Delay(defaultDelay);
            var inventory = await client.GetInventory();
            var allpokemons =
                inventory.InventoryDelta.InventoryItems.Select(i => i.InventoryItemData?.Pokemon)
                    .Where(p => p != null && p?.PokemonId > 0);

            var dupes = allpokemons.OrderBy(x => x.Cp).Select((x, i) => new { index = i, value = x })
                .GroupBy(x => x.value.PokemonId)
                .Where(x => x.Skip(1).Any());

            for (var i = 0; i < dupes.Count(); i++)
            {
                for (var j = 0; j < dupes.ElementAt(i).Count() - 1; j++)
                {
                    var dubpokemon = dupes.ElementAt(i).ElementAt(j).value;
                    if (dubpokemon.Favorite == 0)
                    {
                        await Task.Delay(defaultDelay); //ColoredConsoleWrite(ConsoleColor.White, $"TransferPokemon");
                        var transfer = await client.TransferPokemon(dubpokemon.Id);
                        ColoredConsoleWrite(ConsoleColor.DarkGreen,
                            $"{Language.GetPhrases()["transferred_low_pokemon"].Replace("[pokemon]", Language.GetPokemons()[Convert.ToString(dubpokemon.PokemonId)]).Replace("[cp]", Convert.ToString(dubpokemon.Cp)).Replace("[high_cp]", Convert.ToString(dupes.ElementAt(i).Last().value.Cp))}");

                    }
                }
            }
        }


    }
}
