# Simple Pokemon Go Bot. C#. Based on shiftcodeYT/PokeBot2, but with faster riskier approach to farming, up to 120K exp per hour without eggs or evolve.


##Features:
- [x] Farm Pokestops
- [x] Catch Pokemon
- [x] Transfer Pokemon
- [x] Refresh token support. (for google auth)
- [x] Login with PTC
- [x] Login with Google
- [x] Hatch Eggs
- [x] Evolve Pokemon
- [x] Add awesome colorZ

# Files
- [x] token.txt | Contains: 1 Line with Refresh Token | Don't delete!
- [x] external.config | Contains: configuration

# Setup Guide
- Download and extract the newest release [here](https://github.com/shiftcodeYT/PokeBot2/releases/latest)
- Edit external.config:
- Change AuthType to "Google" or "Ptc"
- If using "Ptc", change username and password to your login
- Change DefaultLatitude and DefaultLongitude to the GPS coords of your liking
- Change language to "english" or "german"
- Change transfertype to "none"/"cp"/"leaveStrongest"/"duplicate"/"all"
- If using "cp", change TransferCPtreshold to your liking
- Change EvolveAllGivenPokemons to "true" or "false"
- Run the bot 
