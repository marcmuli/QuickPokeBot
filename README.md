## Based on shiftcodeYT PokeBot2 
Faster and riskier approach to farming, up to 120K exp per hour without eggs or evolve.

[DOWNLOAD] (https://github.com/fededevi/QuickPokeBot/releases)


![](https://cloud.githubusercontent.com/assets/5583580/17182425/be384104-5423-11e6-9193-870a311fce4f.png)


##Features:
- [x] Farm Pokestops
- [x] Catch Pokemon
- [x] Transfer Pokemon
- [x] Login with PTC
- [x] Login with Google, Refresh token support. (for google auth)
- [x] Evolve Pokemon
- [x] Add awesome colorZ

# Files
- [x] token.txt | Contains: 1 Line with Refresh Token | Don't delete!
- [x] external.config | Contains: configuration

# Setup Guide
- Download and extract the newest release [here](https://github.com/fededevi/QuickPokeBot/releases)
- Edit external.config:
- Change AuthType to "Google" or "Ptc"
- If using "Ptc", change ptc username and password to your login
- If using "Google", change google email and password to your login
- - If using 2 step authentication you can generate a new password for the bot here: https://security.google.com/settings/security/apppasswords
- Change DefaultLatitude and DefaultLongitude to the GPS coords of your liking
- Change transfertype to "none"/"cp"/"leaveStrongest"/"duplicate"/"all"
- If using "cp", change TransferCPtreshold to your liking
- Change EvolveAllGivenPokemons to "true" or "false"
- Run the bot 
