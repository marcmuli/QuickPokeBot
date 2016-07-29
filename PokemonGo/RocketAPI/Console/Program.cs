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
        static int Currentlevel = -1;
        private static int TotalExperience = 0;
        private static int TotalPokemon = 0;
        private static DateTime TimeStarted = DateTime.Now;
        public static DateTime InitSessionDateTime = DateTime.Now;
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

        private static async Task EvolveAllGivenPokemons(Client client, IEnumerable<PokemonData> pokemonToEvolve)
        {
            foreach (var pokemon in pokemonToEvolve)
            {

                var countOfEvolvedUnits = 0;
                var xpCount = 0;

                EvolvePokemonOut evolvePokemonOutProto;
                do
                {
                    evolvePokemonOutProto = await client.EvolvePokemon(pokemon.Id);
                    //todo: someone check whether this still works

                    if (evolvePokemonOutProto.Result == 1)
                    {
                        ColoredConsoleWrite(ConsoleColor.Cyan,
                            $"[{DateTime.Now.ToString("HH:mm:ss")}] Evolved {pokemon.PokemonId} successfully for {evolvePokemonOutProto.ExpAwarded}xp");

                        countOfEvolvedUnits++;
                        xpCount += evolvePokemonOutProto.ExpAwarded;
                    }
                    else
                    {
                        var result = evolvePokemonOutProto.Result;
                    }
                } while (evolvePokemonOutProto.Result == 1);

                if (countOfEvolvedUnits > 0)
                    ColoredConsoleWrite(ConsoleColor.Cyan,
                        $"[{DateTime.Now.ToString("HH:mm:ss")}] Evolved {countOfEvolvedUnits} pieces of {pokemon.PokemonId} for {xpCount}xp");

                await Task.Delay(3000);
            }
        }

        private static async void Execute()
        {
            var client = new Client(ClientSettings);
            try
            {
                if (ClientSettings.AuthType == AuthType.Ptc)
                    await client.DoPtcLogin(ClientSettings.PtcUsername, ClientSettings.PtcPassword);
                else if (ClientSettings.AuthType == AuthType.Google)
                    await client.DoGoogleLogin(ClientSettings.GoogleEmail, ClientSettings.GooglePassword);

                await Task.Delay(defaultDelay); ColoredConsoleWrite(ConsoleColor.White, $"SetServer");
                await client.SetServer();
                await Task.Delay(defaultDelay); ColoredConsoleWrite(ConsoleColor.White, $"GetProfile");
                var profile = await client.GetProfile();
                await Task.Delay(defaultDelay); ColoredConsoleWrite(ConsoleColor.White, $"GetSettings");
                var settings = await client.GetSettings();
                await Task.Delay(defaultDelay); ColoredConsoleWrite(ConsoleColor.White, $"GetMapObjects");
                var mapObjects = await client.GetMapObjects();
                await Task.Delay(defaultDelay); ColoredConsoleWrite(ConsoleColor.White, $"GetInventory");
                var inventory = await client.GetInventory();
                var pokemons = inventory.InventoryDelta.InventoryItems.Select(i => i.InventoryItemData?.Pokemon).Where(p => p != null && p?.PokemonId > 0);
                await Task.Delay(defaultDelay); ColoredConsoleWrite(ConsoleColor.White, $"Transfer PK");
                await TransferDuplicatePokemon(client);



                ColoredConsoleWrite(ConsoleColor.Red, "Recycling Items");
                await Task.Delay(defaultDelay); ColoredConsoleWrite(ConsoleColor.White, $"client.RecycleItems(client)");
                await client.RecycleItems(client);


                ColoredConsoleWrite(ConsoleColor.Red, "ExecuteFarmingPokestopsAndPokemons");
                await ExecuteFarmingPokestopsAndPokemons(client);

                ColoredConsoleWrite(ConsoleColor.Red, $"[{DateTime.Now.ToString("HH:mm:ss")}] {Language.GetPhrases()["no_nearby_loc_found"]}");
                await Task.Delay(2000);
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
            await Task.Delay(defaultDelay); ColoredConsoleWrite(ConsoleColor.White, $"GetMapObjects (pokemons)");
            var mapObjects = await client.GetMapObjects();

            var pokemons = mapObjects.MapCells.SelectMany(i => i.CatchablePokemons);

            var inventory2 = await client.GetInventory();
            var pokemons2 = inventory2.InventoryDelta.InventoryItems
                .Select(i => i.InventoryItemData?.Pokemon)
                .Where(p => p != null && p?.PokemonId > 0)
                .ToArray();

            foreach (var pokemon in pokemons)
            {
                await Task.Delay(defaultDelay); ColoredConsoleWrite(ConsoleColor.White, $"EncounterPokemon");
                var encounterPokemonResponse = await client.EncounterPokemon(pokemon.EncounterId, pokemon.SpawnpointId);
                
                var pokemonCP = encounterPokemonResponse?.WildPokemon?.PokemonData?.Cp;
                CatchPokemonResponse caughtPokemonResponse;
                do
                {
                    await Task.Delay(defaultDelay); ColoredConsoleWrite(ConsoleColor.White, $"CatchPokemon");
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
                    ColoredConsoleWrite(ConsoleColor.Green, $"[{DateTime.Now.ToString("HH:mm:ss")}] {Language.GetPhrases()["caught_pokemon"].Replace("[pokemon]", pokemonName).Replace("[cp]", Convert.ToString(encounterPokemonResponse?.WildPokemon?.PokemonData?.Cp))}");
                    TotalPokemon++;
                    expDone += 210;
                    ColoredConsoleWrite(ConsoleColor.Red, $"Exp/H: {expDone / GetRuntime()}");
                }
                else
                    ColoredConsoleWrite(ConsoleColor.Red, $"[{DateTime.Now.ToString("HH:mm:ss")}] {Language.GetPhrases()["pokemon_got_away"].Replace("[pokemon]", pokemonName).Replace("[cp]", Convert.ToString(encounterPokemonResponse?.WildPokemon?.PokemonData?.Cp))}");

                await Task.Delay(defaultDelay); ColoredConsoleWrite(ConsoleColor.White, $"Transfer");
                await TransferDuplicatePokemon(client);

                
            }
        }
        public static int defaultDelay = 350;

        public static int expDone = 0;
        private static async Task ExecuteFarmingPokestopsAndPokemons(Client client)
        {
            await Task.Delay(defaultDelay); ColoredConsoleWrite(ConsoleColor.White, $"GetMapObjects (Pokestops)");
            var mapObjects = await client.GetMapObjects();

            var pokeStops = mapObjects.MapCells.SelectMany(i => i.Forts).Where(i => i.Type == FortType.Checkpoint && i.CooldownCompleteTimestampMs < DateTime.UtcNow.ToUnixTime());
            ColoredConsoleWrite(ConsoleColor.Red, $"[{DateTime.Now.ToString("HH:mm:ss")}] Number of Pokestop: {pokeStops.Count()}");

            Location startLocation = new Location(client.getCurrentLat(), client.getCurrentLng());
            IList<FortData> query = pokeStops.ToList();
            
            while (query.Count > 10) //Ignore last 10 pokestop, usually far away
            {
                startLocation = new Location(client.getCurrentLat(), client.getCurrentLng());
                query = query.OrderBy(pS => Spheroid.CalculateDistanceBetweenLocations(startLocation, new Location(pS.Latitude, pS.Longitude))).ToList();
                var pokeStop = query.First();
                query.RemoveAt(0);
                Location endLocation = new Location(pokeStop.Latitude, pokeStop.Longitude);
                var distanceToPokestop = Spheroid.CalculateDistanceBetweenLocations(startLocation, endLocation);

                await Task.Delay(defaultDelay); ColoredConsoleWrite(ConsoleColor.White, $"UpdatePlayerLocation");
                var update = await client.UpdatePlayerLocation(endLocation.latitude, endLocation.longitude);
                ColoredConsoleWrite(ConsoleColor.White, $"[{DateTime.Now.ToString("HH:mm:ss")}] Moved {(int)distanceToPokestop}m, wait {25.0 * (int)distanceToPokestop}ms, Number of Pokestop in this zone: {query.Count}");

                int delay = (int)(25.0 * distanceToPokestop);
                if (delay < defaultDelay) delay = defaultDelay;
                await Task.Delay(delay);  ColoredConsoleWrite(ConsoleColor.White, $"fortInfo");


                var fortInfo = await client.GetFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);
                await Task.Delay(defaultDelay); ColoredConsoleWrite(ConsoleColor.White, $"fortSearch");
                var fortSearch = await client.SearchFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);

                StringWriter PokeStopOutput = new StringWriter();
                PokeStopOutput.Write($"[{DateTime.Now.ToString("HH:mm:ss")}] ");
                if (fortInfo.Name != string.Empty)
                    PokeStopOutput.Write(Language.GetPhrases()["pokestop"].Replace("[pokestop]", fortInfo.Name));
                if (fortSearch.ExperienceAwarded != 0)
                    PokeStopOutput.Write($", {Language.GetPhrases()["xp"].Replace("[xp]", Convert.ToString(fortSearch.ExperienceAwarded))}");
                if (fortSearch.GemsAwarded != 0)
                    PokeStopOutput.Write($", {Language.GetPhrases()["gem"].Replace("[gem]", Convert.ToString(fortSearch.GemsAwarded))}");
                if (fortSearch.PokemonDataEgg != null)
                    PokeStopOutput.Write($", {Language.GetPhrases()["egg"].Replace("[egg]", Convert.ToString(fortSearch.PokemonDataEgg))}");
                if (GetFriendlyItemsString(fortSearch.ItemsAwarded) != string.Empty)
                    PokeStopOutput.Write($", {Language.GetPhrases()["item"].Replace("[item]", GetFriendlyItemsString(fortSearch.ItemsAwarded))}");
                ColoredConsoleWrite(ConsoleColor.Cyan, PokeStopOutput.ToString());

                expDone += fortSearch.ExperienceAwarded;
                ColoredConsoleWrite(ConsoleColor.Red, $"Exp/H: {expDone/GetRuntime()}");

                if (fortSearch.ExperienceAwarded != 0)
                    TotalExperience += (fortSearch.ExperienceAwarded);

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
                Language.LoadLanguageFile(ClientSettings.Language);
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
                        await Task.Delay(defaultDelay); ColoredConsoleWrite(ConsoleColor.White, $"TransferPokemon");
                        var transfer = await client.TransferPokemon(dubpokemon.Id);
                        ColoredConsoleWrite(ConsoleColor.DarkGreen,
                            $"[{DateTime.Now.ToString("HH:mm:ss")}] {Language.GetPhrases()["transferred_low_pokemon"].Replace("[pokemon]", Language.GetPokemons()[Convert.ToString(dubpokemon.PokemonId)]).Replace("[cp]", Convert.ToString(dubpokemon.Cp)).Replace("[high_cp]", Convert.ToString(dupes.ElementAt(i).Last().value.Cp))}");

                    }
                }
            }
        }


    }
}
