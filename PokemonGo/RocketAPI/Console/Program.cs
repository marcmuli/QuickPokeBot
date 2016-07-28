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

                await client.SetServer();
                var profile = await client.GetProfile();
                var settings = await client.GetSettings();
                var mapObjects = await client.GetMapObjects();
                var inventory = await client.GetInventory();
                var pokemons =
                    inventory.InventoryDelta.InventoryItems.Select(i => i.InventoryItemData?.Pokemon)
                        .Where(p => p != null && p?.PokemonId > 0);

                ColoredConsoleWrite(ConsoleColor.Yellow, "----------------------------");
                ColoredConsoleWrite(ConsoleColor.Cyan, Language.GetPhrases()["account"].Replace("[username]", ClientSettings.PtcUsername));
                ColoredConsoleWrite(ConsoleColor.Cyan, Language.GetPhrases()["password"].Replace("[password]", ClientSettings.PtcPassword + "\n"));
                ColoredConsoleWrite(ConsoleColor.DarkGray, Language.GetPhrases()["latitude"].Replace("[latitude]", Convert.ToString(ClientSettings.DefaultLatitude)));
                ColoredConsoleWrite(ConsoleColor.DarkGray, Language.GetPhrases()["longtitude"].Replace("[longtitude]", Convert.ToString(ClientSettings.DefaultLongitude)));
                ColoredConsoleWrite(ConsoleColor.Yellow, "----------------------------");
                ColoredConsoleWrite(ConsoleColor.DarkGray, Language.GetPhrases()["your_account"] + "\n");
                ColoredConsoleWrite(ConsoleColor.DarkGray, Language.GetPhrases()["username"].Replace("[username]", profile.Profile.Username));
                ColoredConsoleWrite(ConsoleColor.DarkGray, Language.GetPhrases()["team"].Replace("[team]", Convert.ToString(profile.Profile.Team)));
                ColoredConsoleWrite(ConsoleColor.DarkGray, Language.GetPhrases()["stardust"].Replace("[stardust]", Convert.ToString(profile.Profile.Currency.ToArray()[1].Amount)));

                ColoredConsoleWrite(ConsoleColor.Cyan, "\n" + Language.GetPhrases()["farming_started"]);
                ColoredConsoleWrite(ConsoleColor.Yellow, "----------------------------");

                ColoredConsoleWrite(ConsoleColor.Red, "TransferType loading");
                if (ClientSettings.TransferType == "leaveStrongest")
                    await TransferAllButStrongestUnwantedPokemon(client);
                else if (ClientSettings.TransferType == "all")
                    await TransferAllGivenPokemons(client, pokemons);
                else if (ClientSettings.TransferType == "duplicate")
                    await TransferDuplicatePokemon(client);
                else if (ClientSettings.TransferType == "cp")
                    await TransferAllWeakPokemon(client, ClientSettings.TransferCPThreshold);
                else
                    ColoredConsoleWrite(ConsoleColor.DarkGray, $"[{DateTime.Now.ToString("HH:mm:ss")}] {Language.GetPhrases()["transfering_disabled"]}");
                ColoredConsoleWrite(ConsoleColor.Red, "TransferType loaded");
                if (ClientSettings.EvolveAllGivenPokemons)
                    await EvolveAllGivenPokemons(client, pokemons);
                ColoredConsoleWrite(ConsoleColor.Red, "Recycling Items");
                client.RecycleItems(client);
                ColoredConsoleWrite(ConsoleColor.Red, "Finished recycling");
                await Task.Delay(5000);
                await ConsoleLevelTitle(profile.Profile.Username, client);
                PrintLevel(client);

                ColoredConsoleWrite(ConsoleColor.Red, "ExecuteFarmingPokestopsAndPokemons");
                await ExecuteFarmingPokestopsAndPokemons(client);
                ColoredConsoleWrite(ConsoleColor.Red, $"[{DateTime.Now.ToString("HH:mm:ss")}] {Language.GetPhrases()["no_nearby_loc_found"]}");
                
                Execute();
                await Task.Delay(30000);
            }
            catch (TaskCanceledException tce) { ColoredConsoleWrite(ConsoleColor.White, $"[{DateTime.Now.ToString("HH:mm:ss")}] {Language.GetPhrases()["task_canceled_ex"]}"); Execute(); }
            catch (UriFormatException ufe) { ColoredConsoleWrite(ConsoleColor.White, $"[{DateTime.Now.ToString("HH:mm:ss")}] {Language.GetPhrases()["sys_uri_format_ex"]}"); Execute(); }
            catch (ArgumentOutOfRangeException aore) { ColoredConsoleWrite(ConsoleColor.White, $"[{DateTime.Now.ToString("HH:mm:ss")}] {Language.GetPhrases()["arg_out_of_range_ex"]}"); Execute(); }
            catch (ArgumentNullException ane) { ColoredConsoleWrite(ConsoleColor.White, $"[{DateTime.Now.ToString("HH:mm:ss")}] {Language.GetPhrases()["arg_null_ref"]}"); Execute(); }
            catch (NullReferenceException nre) { ColoredConsoleWrite(ConsoleColor.White, $"[{DateTime.Now.ToString("HH:mm:ss")}] {Language.GetPhrases()["null_ref"]}"); Execute(); }
            //await ExecuteCatchAllNearbyPokemons(client);
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
            var mapObjects = await client.GetMapObjects();

            var pokemons = mapObjects.MapCells.SelectMany(i => i.CatchablePokemons);

            var inventory2 = await client.GetInventory();
            var pokemons2 = inventory2.InventoryDelta.InventoryItems
                .Select(i => i.InventoryItemData?.Pokemon)
                .Where(p => p != null && p?.PokemonId > 0)
                .ToArray();

            foreach (var pokemon in pokemons)
            {
                var update = await client.UpdatePlayerLocation(pokemon.Latitude, pokemon.Longitude);
                
                var encounterPokemonResponse = await client.EncounterPokemon(pokemon.EncounterId, pokemon.SpawnpointId);
                var pokemonCP = encounterPokemonResponse?.WildPokemon?.PokemonData?.Cp;
                CatchPokemonResponse caughtPokemonResponse;
                do
                {
                    caughtPokemonResponse =
                        await
                            client.CatchPokemon(pokemon.EncounterId, pokemon.SpawnpointId, pokemon.Latitude,
                                pokemon.Longitude, MiscEnums.Item.ITEM_POKE_BALL, pokemonCP);
                    ; //note: reverted from settings because this should not be part of settings but part of logic
                } while (caughtPokemonResponse.Status == CatchPokemonResponse.Types.CatchStatus.CatchMissed);
                string pokemonName = Language.GetPokemons()[Convert.ToString(pokemon.PokemonId)];

                if (caughtPokemonResponse.Status == CatchPokemonResponse.Types.CatchStatus.CatchSuccess)
                {
                    ColoredConsoleWrite(ConsoleColor.Green, $"[{DateTime.Now.ToString("HH:mm:ss")}] {Language.GetPhrases()["caught_pokemon"].Replace("[pokemon]", pokemonName).Replace("[cp]", Convert.ToString(encounterPokemonResponse?.WildPokemon?.PokemonData?.Cp))}");
                    TotalPokemon++;
                    TotalExperience += 210;
                }
                else
                    ColoredConsoleWrite(ConsoleColor.Red, $"[{DateTime.Now.ToString("HH:mm:ss")}] {Language.GetPhrases()["pokemon_got_away"].Replace("[pokemon]", pokemonName).Replace("[cp]", Convert.ToString(encounterPokemonResponse?.WildPokemon?.PokemonData?.Cp))}");

                if (ClientSettings.TransferType == "leaveStrongest")
                    await TransferAllButStrongestUnwantedPokemon(client);
                else if (ClientSettings.TransferType == "all")
                    await TransferAllGivenPokemons(client, pokemons2);
                else if (ClientSettings.TransferType == "duplicate")
                    await TransferDuplicatePokemon(client);
                else if (ClientSettings.TransferType == "cp")
                    await TransferAllWeakPokemon(client, ClientSettings.TransferCPThreshold);

                await Task.Delay(1500);
            }
        }

        private static async Task ExecuteFarmingPokestopsAndPokemons(Client client)
        {
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
                var update = await client.UpdatePlayerLocation(endLocation.latitude, endLocation.longitude);
                ColoredConsoleWrite(ConsoleColor.White, $"[{DateTime.Now.ToString("HH:mm:ss")}] Moved {(int)distanceToPokestop}m, wait {25.0 * (int)distanceToPokestop}ms, Number of Pokestop in this zone: {query.Count}");
                await Task.Delay((int)(25.0 * distanceToPokestop));

                var fortInfo = await client.GetFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);
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
                    //ColoredConsoleWrite(ConsoleColor.White, "Coded by Ferox - edited by NecronomiconCoding");
                    //CheckVersion();
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
            //System.Console.ReadLine();
        }

        private static async Task TransferAllButStrongestUnwantedPokemon(Client client)
        {
            //ColoredConsoleWrite(ConsoleColor.White, $"[{DateTime.Now.ToString("HH:mm:ss")}] Firing up the meat grinder");

            var unwantedPokemonTypes = new[]
            {
                PokemonId.Pidgey,
                PokemonId.Rattata,
                PokemonId.Weedle,
                PokemonId.Zubat,
                PokemonId.Caterpie,
                PokemonId.Pidgeotto,
                PokemonId.Paras,
                PokemonId.Venonat,
                PokemonId.Psyduck,
                PokemonId.Poliwag,
                PokemonId.Slowpoke,
                PokemonId.Drowzee,
                PokemonId.Gastly,
                PokemonId.Goldeen,
                PokemonId.Staryu,
                PokemonId.Magikarp,
                PokemonId.Clefairy,
                PokemonId.Eevee,
                PokemonId.Tentacool,
                PokemonId.Dratini,
                PokemonId.Ekans,
                PokemonId.Jynx,
                PokemonId.Lickitung,
                PokemonId.Spearow,
                PokemonId.NidoranFemale,
                PokemonId.NidoranMale
            };

            var inventory = await client.GetInventory();
            var pokemons = inventory.InventoryDelta.InventoryItems
                .Select(i => i.InventoryItemData?.Pokemon)
                .Where(p => p != null && p?.PokemonId > 0)
                .ToArray();

            foreach (var unwantedPokemonType in unwantedPokemonTypes)
            {
                var pokemonOfDesiredType = pokemons.Where(p => p.PokemonId == unwantedPokemonType)
                    .OrderByDescending(p => p.Cp)
                    .ToList();

                var unwantedPokemon =
                    pokemonOfDesiredType.Skip(1) // keep the strongest one for potential battle-evolving
                        .ToList();

                //ColoredConsoleWrite(ConsoleColor.White, $"[{DateTime.Now.ToString("HH:mm:ss")}] Grinding {unwantedPokemon.Count} pokemons of type {unwantedPokemonType}");
                await TransferAllGivenPokemons(client, unwantedPokemon);
            }

            //ColoredConsoleWrite(ConsoleColor.White, $"[{DateTime.Now.ToString("HH:mm:ss")}] Finished grinding all the meat");
        }

        public static float Perfect(PokemonData poke)
        {
            return ((float)(poke.IndividualAttack + poke.IndividualDefense + poke.IndividualStamina) / (3.0f * 15.0f)) * 100.0f;
        }

        private static async Task TransferAllGivenPokemons(Client client, IEnumerable<PokemonData> unwantedPokemons, float keepPerfectPokemonLimit = 80.0f)
        {
            foreach (var pokemon in unwantedPokemons)
            {
                if (Perfect(pokemon) >= keepPerfectPokemonLimit) continue;
                ColoredConsoleWrite(ConsoleColor.White, $"[{DateTime.Now.ToString("HH:mm:ss")}] {Language.GetPhrases()["pokemon_iv_percent_less_than"].Replace("[pokemon]", Language.GetPokemons()[Convert.ToString(pokemon.PokemonId)]).Replace("[cp]", Convert.ToString(pokemon.Cp)).Replace("[percent]", Convert.ToString(keepPerfectPokemonLimit))}");

                if (pokemon.Favorite == 0)
                {
                    var transferPokemonResponse = await client.TransferPokemon(pokemon.Id);

                    /*
                    ReleasePokemonOutProto.Status {
                        UNSET = 0;
                        SUCCESS = 1;
                        POKEMON_DEPLOYED = 2;
                        FAILED = 3;
                        ERROR_POKEMON_IS_EGG = 4;
                    }*/

                    if (transferPokemonResponse.Status == 1)
                    {
                        ColoredConsoleWrite(ConsoleColor.Magenta, $"[{DateTime.Now.ToString("HH:mm:ss")}] {Language.GetPhrases()["transferred_pokemon"].Replace("[pokemon]", Language.GetPokemons()[Convert.ToString(pokemon.PokemonId)]).Replace("[cp]", Convert.ToString(pokemon.Cp))}");
                    }
                    else
                    {
                        var status = transferPokemonResponse.Status;

                        ColoredConsoleWrite(ConsoleColor.Red, $"[{DateTime.Now.ToString("HH:mm:ss")}] {Language.GetPhrases()["transferred_pokemon_failed"].Replace("[pokemon]", Language.GetPokemons()[Convert.ToString(pokemon.PokemonId)]).Replace("[cp]", Convert.ToString(pokemon.Cp))}");
                    }

                    await Task.Delay(3000);
                }
            }
        }

        private static async Task TransferDuplicatePokemon(Client client)
        {

            //ColoredConsoleWrite(ConsoleColor.White, $"Check for duplicates");
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
                        var transfer = await client.TransferPokemon(dubpokemon.Id);
                        ColoredConsoleWrite(ConsoleColor.DarkGreen,
                            $"[{DateTime.Now.ToString("HH:mm:ss")}] {Language.GetPhrases()["transferred_low_pokemon"].Replace("[pokemon]", Language.GetPokemons()[Convert.ToString(dubpokemon.PokemonId)]).Replace("[cp]", Convert.ToString(dubpokemon.Cp)).Replace("[high_cp]", Convert.ToString(dupes.ElementAt(i).Last().value.Cp))}");

                    }
                }
            }
        }

        private static async Task TransferAllWeakPokemon(Client client, int cpThreshold)
        {
            //ColoredConsoleWrite(ConsoleColor.White, $"[{DateTime.Now.ToString("HH:mm:ss")}] Firing up the meat grinder");

            var doNotTransfer = new[] //these will not be transferred even when below the CP threshold
            {
                //PokemonId.Pidgey,
                //PokemonId.Rattata,
                //PokemonId.Weedle,
                //PokemonId.Zubat,
                //PokemonId.Caterpie,
                //PokemonId.Pidgeotto,
                //PokemonId.NidoranFemale,
                //PokemonId.Paras,
                //PokemonId.Venonat,
                //PokemonId.Psyduck,
                //PokemonId.Poliwag,
                //PokemonId.Slowpoke,
                //PokemonId.Drowzee,
                //PokemonId.Gastly,
                //PokemonId.Goldeen,
                //PokemonId.Staryu,
                PokemonId.Magikarp,
                PokemonId.Eevee,
                //PokemonId.Dratini
            };

            var inventory = await client.GetInventory();
            var pokemons = inventory.InventoryDelta.InventoryItems
                                .Select(i => i.InventoryItemData?.Pokemon)
                                .Where(p => p != null && p?.PokemonId > 0)
                                .ToArray();

            //foreach (var unwantedPokemonType in unwantedPokemonTypes)
            {
                var pokemonToDiscard = pokemons.Where(p => !doNotTransfer.Contains(p.PokemonId) && p.Cp < cpThreshold)
                                                   .OrderByDescending(p => p.Cp)
                                                   .ToList();

                //var unwantedPokemon = pokemonOfDesiredType.Skip(1) // keep the strongest one for potential battle-evolving
                //                                          .ToList();
                ColoredConsoleWrite(ConsoleColor.Gray, $"[{DateTime.Now.ToString("HH:mm:ss")}] {Language.GetPhrases()["grinding_pokemon"].Replace("[number]", Convert.ToString(pokemonToDiscard.Count)).Replace("[cp]", Convert.ToString(cpThreshold))}");
                await TransferAllGivenPokemons(client, pokemonToDiscard);

            }

            ColoredConsoleWrite(ConsoleColor.Gray, $"[{DateTime.Now.ToString("HH:mm:ss")}] {Language.GetPhrases()["finished_grinding"]}");
        }

        public static async Task PrintLevel(Client client)
        {
            var inventory = await client.GetInventory();
            var stats = inventory.InventoryDelta.InventoryItems.Select(i => i.InventoryItemData?.PlayerStats).ToArray();
            foreach (var v in stats)
                if (v != null)
                {
                    int XpDiff = GetXpDiff(client, v.Level);
                    if (ClientSettings.LevelOutput == "time")
                        ColoredConsoleWrite(ConsoleColor.Yellow, $"[{DateTime.Now.ToString("HH:mm:ss")}] {Language.GetPhrases()["current_lv"]} " + v.Level + " (" + (v.Experience - v.PrevLevelXp - XpDiff) + "/" + (v.NextLevelXp - v.PrevLevelXp - XpDiff) + ")");
                    else if (ClientSettings.LevelOutput == "levelup")
                        if (Currentlevel != v.Level)
                        {
                            Currentlevel = v.Level;
                            ColoredConsoleWrite(ConsoleColor.Magenta, $"[{DateTime.Now.ToString("HH:mm:ss")}] {Language.GetPhrases()["current_lv"]}: " + v.Level + $". {Language.GetPhrases()["rpt"]} " + (v.NextLevelXp - v.Experience));
                        }
                }

            await Task.Delay(ClientSettings.LevelTimeInterval * 1000);
            PrintLevel(client);
        }

        public static async Task ConsoleLevelTitle(string Username, Client client)
        {
            var inventory = await client.GetInventory();
            var stats = inventory.InventoryDelta.InventoryItems.Select(i => i.InventoryItemData?.PlayerStats).ToArray();
            var profile = await client.GetProfile();
            foreach (var v in stats)
                if (v != null)
                {
                    int XpDiff = GetXpDiff(client, v.Level);
                    System.Console.Title = string.Format(Username + "|lvl:{0:0}-({1:0}/{2:0})|STD:{3:0}", v.Level, (v.Experience - v.PrevLevelXp - XpDiff), (v.NextLevelXp - v.PrevLevelXp - XpDiff), profile.Profile.Currency.ToArray()[1].Amount) + "|XP/h:" + Math.Round(TotalExperience / GetRuntime()) + "|PM/h:" + Math.Round(TotalPokemon / GetRuntime())+ "|Lat/Lng:" + Convert.ToString(client.getCurrentLat()) + "/" + Convert.ToString(client.getCurrentLng());
                }
            await Task.Delay(1000);
            ConsoleLevelTitle(Username, client);
        }

        public static int GetXpDiff(Client client, int Level)
        {
            switch (Level)
            {
                case 1:
                    return 0;
                case 2:
                    return 1000;
                case 3:
                    return 2000;
                case 4:
                    return 3000;
                case 5:
                    return 4000;
                case 6:
                    return 5000;
                case 7:
                    return 6000;
                case 8:
                    return 7000;
                case 9:
                    return 8000;
                case 10:
                    return 9000;
                case 11:
                    return 10000;
                case 12:
                    return 10000;
                case 13:
                    return 10000;
                case 14:
                    return 10000;
                case 15:
                    return 15000;
                case 16:
                    return 20000;
                case 17:
                    return 20000;
                case 18:
                    return 20000;
                case 19:
                    return 25000;
                case 20:
                    return 25000;
                case 21:
                    return 50000;
                case 22:
                    return 75000;
                case 23:
                    return 100000;
                case 24:
                    return 125000;
                case 25:
                    return 150000;
                case 26:
                    return 190000;
                case 27:
                    return 200000;
                case 28:
                    return 250000;
                case 29:
                    return 300000;
                case 30:
                    return 350000;
                case 31:
                    return 500000;
                case 32:
                    return 500000;
                case 33:
                    return 750000;
                case 34:
                    return 1000000;
                case 35:
                    return 1250000;
                case 36:
                    return 1500000;
                case 37:
                    return 2000000;
                case 38:
                    return 2500000;
                case 39:
                    return 1000000;
                case 40:
                    return 1000000;
            }
            return 0;
        }
    }
}
