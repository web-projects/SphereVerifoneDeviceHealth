===============================================================================
DEVICE CONFIGURATION TOOL REVISON HISTORY
===============================================================================

VERSION 1.063.00 (01/24/2022)

1. SFTP file transfer support.
2. Reformatted output file columns for uniqueness and proper spacing.

-------------------------------------------------------------------------------
VERSION 1.062.00 (12/20/2021)

1. VIPA 6.8.2.23 support.
   * EPIC EMV Configs: 211215
   * NJT EMV Configs : 211217

-------------------------------------------------------------------------------
VERSION 1.061.00 (12/14/2021)

1. VIPA 6.8.2.23 support.
   * EPIC EMV Configs: 211207
   * NJT EMV Configs : 211207

-------------------------------------------------------------------------------
VERSION 1.060.00 (12/07/2021)

1. VIPA 6.8.2.23 support.
   * EPIC EMV Configs: 211203
   * NJT EMV Configs : 211129

2. UX301 Validation does not include IDLE screen check.

-------------------------------------------------------------------------------
VERSION 1.059.00 (12/02/2021)

1. Fixes to enhance timestamp validation.

-------------------------------------------------------------------------------
VERSION 1.057.00 (11/30/2021)

1. KIF StandAlone default mode.

-------------------------------------------------------------------------------
VERSION 1.056.00 (11/29/2021)

1. Update configuration bundles for TransLimit DISABLED
2. Added EPIC EMV bundles
3. Multi-device connection

-------------------------------------------------------------------------------
VERSION 1.055.00 (10/14/2021)

1. UX301 validation support

-------------------------------------------------------------------------------
VERSION 1.054.00 (10/08/2021)

1. Corrected FAILED state with valid configuration

-------------------------------------------------------------------------------
VERSION 1.053.00 (10/01/2021)

1. UL deliverable set to default to standalone mode

### VALIDATE DEVICE ITEMS ###

* 1. PRODUCTION ADE KEY
* 2. DEBIT PIN KEY
* 3. Terminal UTC timestamp
* 4. 24 hour reboot set to 07:00:00
* 5. Bundle versions (VIPA_VER, EMV_VER, IDLE_VER)

-------------------------------------------------------------------------------
VERSION 1.051.00 (08/27/2021)

1. Idle images update to correct missing version schema attribute.
2. Added console fore/back ground configurable colorization.

-------------------------------------------------------------------------------
VERSION 1.050.00 (08/05/2021)

1. Removed active configuration setting and changed it to dynamic method.
   Defaults is 'UNSIGNED'

2. Improved STATUS information to indicate IPP key slot and bundle versions.

-------------------------------------------------------------------------------
VERSION 1.049.00 (07/20/2021)

1. Corrected VIPA minor version transposition and added check to confirm version
2. Added logger for troubleshooting purposes

-------------------------------------------------------------------------------
VERSION 1.048.00 (07/13/2021)

1. <ALT><V> option to display VIPA bundle versions
2. Signing artifact deployment via appsettings.json SigningMethodActive
3. Configuration artifact deployment via appsettings.json ConfigurationPackageActive

-------------------------------------------------------------------------------
VERSION 1.047.00 (05/25/2021)

1. Device signing option: Sphere or Verifone-DEV.
2. Attended configurations: 05/25/2021
3. Slot change 0,8 for both Sphere and NJT.
4. Idle images CustId: 199, 250.
5. 24 hour reboot

-------------------------------------------------------------------------------
VERSION 1.046.00 (05/10/2021)

1. KIF EMV Configuration Signed Packages in TGZ format.

-------------------------------------------------------------------------------
VERSION 1.045.00 (05/03/2021)

1. V1 EMV Configuration Signed Packages in TGZ format.


-------------------------------------------------------------------------------
VERSION 1.044.00 (04/19/2021)

1. V1 EMV Configuration Signed Packages in TGZ format.

-------------------------------------------------------------------------------
VERSION 1.044.00 (04/08/2021)

1. V0 EMV Configuration Signed Packages in TGZ format.
2. Added Idle Screen Update for RAPTOR/ENGAGE devices (P200, P400, M400).

-------------------------------------------------------------------------------
VERSION 1.043.00 (03/19/2021)

1. Updated NJT and EPIC configurations to support VIPA 6.8.2.17

-------------------------------------------------------------------------------
VERSION 1.042.00 (02/11/2021)

1. M400 Device Support.

-------------------------------------------------------------------------------
VERSION 1.039.00 (09/18/2020)

1. Fixes to configuration files line-ending: must always be UNIX style.

-------------------------------------------------------------------------------
VERSION 1.035.00 (07/23/2020)

1. CONTLEMV.CFG
   - ATTENDED/UNATTENDED VISA PIN ENTRY LIMIT SET

-------------------------------------------------------------------------------
VERSION 1.034.01 (07/17/2020)

1. CONTLEMV.CFG
   - ATTENDED/UNATTENDED RAISED CONTACTLESS LIMITS

-------------------------------------------------------------------------------
VERSION 1.034 (07/10/2020)

1. CONTLEMV.CFG
   - ENGAGE/UX301 SPECIFIC CONFIGURATIONS TO ALLOW FOR AMEX TRANSACTION AMOUNTS
     GREATER THAN $10.00
2. TDOL.CFG
   - ALL DEVICES TO REPORT TAG 5F20 (CARDHOLDER NAME)

-------------------------------------------------------------------------------
VERSION 1.033 (06/25/2020)

1. CONTLEMV.CFG
   - ENGAGE/UX301 SPECIFIC CONFIGURATIONS TO ALLOW FOR CONTACTLESS ONLINE PIN
     REQUEST

2. ICCDATA.DAT
   - ENGAGE/UX301 SPECIFIC CONFIGURATIONS TO ALLOW FOR CONTACTLESS ONLINE PIN
     REQUEST

-------------------------------------------------------------------------------
VERSION 1.032 (06/10/2020)

I. Unattended using the Ux 301/401/100 (EMV L2 Version 7.0.3l)
   Terminal 25 ??? Config 8C - CHECKSUM=D196BA9D

II. Attended using the Engage P200 (EMV L2 Version 7.0.3r)
    Terminal 22 ??? Config 1C - CHECKSUM=96369E1F
-------------------------------------------------------------------------------

1. CONTLEMV.CFG
   - ENGAGE/UX301 SPECIFIC CONFIGURATIONS

2. ICCDATA.DAT
   - ENGAGE/UX301 SPECIFIC CONFIGURATIONS

3. ICCKEYS.KEY
   - ENGAGE/UX301 SPECIFIC CONFIGURATIONS

4. Added EMV Kernel checksum validation
   - ENGAGE/UX301 SPECIFIC CONFIGURATIONS

5. Device Extended Reset to reload CONFIGURATION after applying it.

-------------------------------------------------------------------------------
VERSION 1.031 (06/05/2020)
-------------------------------------------------------------------------------
1. CONTLEMV.CFG
   - Changed TAG 9F35 from '22' to '25'
   - Changed TAG 9F33 from '60 08 08' to 'E0 48 C8'

2. ICCDATA.DAT
   - Changed TAG 9F35 from '22' to '25'
   - Changed TAG 9F33 from 'E048C8' to 'E0B8C8'

3. ICCKEYS.KEY
   - Changed TAG 9F35 from '22' to '25'
   - Changed TAG 9F33 from 'E048C8' to 'E0B8C8'

4. Added EMV Kernel checksum validation

5. Added CONFIGURATION check for STATUS selection

6. Added Firmware version to STATUS report

-------------------------------------------------------------------------------
VERSION 1.030 (06/03/2020)
-------------------------------------------------------------------------------
1. Added a000000384.c1
2. Added cicapp.cfg
3. Added mapp.cfg

-------------------------------------------------------------------------------
VERSION 1.027 (06/02/2020
-------------------------------------------------------------------------------
1. Initial release with unvalidated config files
