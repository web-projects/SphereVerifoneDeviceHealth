# README #

This an application to query a Verifone device for its health status.

### What is this repository for? ###

* VerifoneDeviceHealth APPLICATION
* git remote add origin https://github.com/web-projects/SphereVerifoneDeviceHealth.git

### VALIDATE DEVICE ITEMS ###

* 1. PRODUCTION ADE KEY
* 2. DEBIT PIN KEY
* 3. Terminal UTC timestamp
* 4. 24 hour reboot set to 00:00:00
* 5. Bundle versions (VIPA_VER, EMV_VER, IDLE_VER)

### GIT REPOSITORY TAGGING ###

* git tag -a GA_RELEASE_1_00_0_53_001 -m "GA_RELEASE_1_00_0_53_001"

### Build self-contained executable ###
* dotnet publish -r win10-x64 -c "Release" --self-contained true

### HISTORY ###

* 20210928 - Initial repository
* 20210930 - GA_RELEASE_1_00_0_53_001
* 20211001 - GA_RELEASE_1_00_0_53_002
* 20211014 - GA_RELEASE_1_00_0_55_000
* 20211129 - GA_RELEASE_1_00_0_56_000
* 20211202 - GA_RELEASE_1_00_0_59_000
* 20211203 - GA_RELEASE_1_00_0_60_000
* 20211214 - GA_RELEASE_1_00_0_61_000
* 20211220 - GA_RELEASE_1_00_0_62_000
* 20220223 - GA_RELEASE_1_00_0_64_001
