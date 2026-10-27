# One Block: Google Play release checklist

Do these steps yourself. Anything involving passwords, payments or accounts is yours to enter.

## A. Accounts (one-time)
- [ ] **Google Play developer account** at https://play.google.com/console ($25 one-time fee plus identity verification). Personal accounts created after Nov 2023 must run a **closed test with at least 12 testers for 14 days** before production access.
- [ ] **AdMob account** at https://admob.google.com, then add an app: Android, "not yet published" (link it to the Play listing later).
  - [ ] Create a **Rewarded** ad unit and an **Interstitial** ad unit.
  - [ ] Copy the **App ID** (`ca-app-pub-…~…`) and both **ad unit IDs** (`ca-app-pub-…/…`).
- [ ] In Unity, open `Assets/_Game/Data/AdsConfig.asset` and paste the App ID and unit IDs. Keep **Use Test Ads ON** while you test on your own phone (clicking your own real ads can get the AdMob account banned), then turn it **OFF** for the production build.
- [ ] Add your phone as a test device in AdMob (Settings > Test devices) for extra safety.
- [ ] Once published, add an **app-ads.txt** file to your developer website (AdMob shows the exact line).

## B. Signing key (one-time, keep it safe)
1. Unity: **Edit > Project Settings > Player > Android > Publishing Settings**.
2. **Keystore Manager > Keystore... > Create New > Anywhere**, and save it **outside** the project, e.g. `D:\Keys\oneblock.keystore`.
3. Choose a strong password, add a key alias `oneblock`, validity 50 years, then enter your name and **Add Key**.
4. Back in Publishing Settings, tick **Custom Keystore**, select the file, and pick the alias.
5. **Back up the keystore file and both passwords** (a password manager and a second drive). Play App Signing protects the store key, but you still need this upload key for every update.
6. Unity forgets the passwords when it restarts, so re-enter them before each release build.

## C. Build
- [ ] Menu **One Block > Build > Android Test APK**: install on your phone and play through: tutorial, a few runs, revive ad, x2 coins ad, bundle ad, shop, missions, settings > privacy.
- [ ] Set the real AdMob IDs and turn **Use Test Ads** off.
- [ ] Menu **One Block > Build > Android Release App Bundle (.aab)**. It checks the keystore and ads config, bumps the version code and writes `Builds/OneBlock-<version>-<code>.aab`.

## D. Play Console: create the app
- [ ] **Create app**: name `One Block`, default language English (US), **Game**, **Free**, and accept the declarations.

### App content (Policy > App content)
- [ ] **Privacy policy**: host `StoreAssets/privacy_policy.html` publicly (fill in placeholders first) and paste the URL. Free options: GitHub Pages or Google Sites.
- [ ] **Ads**: *Yes, my app contains ads*.
- [ ] **App access**: *All functionality is available without special access*.
- [ ] **Content rating** (IARC questionnaire): category **Game**.
  - Violence: *cartoon/fantasy violence, not realistic* (enemies are squashed, character "dies" with a poof). No blood.
  - Fear, sexuality, language, controlled substances, gambling: **No**.
  - User interaction/chat: **No**. Shares location: **No**. Digital purchases: **No**.
  - Expected result: about **Everyone / PEGI 3 or 7**.
- [ ] **Target audience and content**: age groups **13–15, 16–17, 18+** (don't tick under-13, which would put the app under the Families policy). "Could unintentionally appeal to children?" Answer honestly; if asked, explain the ads use a PG max rating and the app isn't designed for children.
- [ ] **Data safety** (from the Google Mobile Ads SDK; the game itself collects nothing):
  - Does your app collect or share user data? **Yes**
  - Encrypted in transit? **Yes**. Can users request deletion? **No** (no account; data isn't held by you). You may note that users can reset the advertising ID.
  - Data types, all *Collected* and *Shared*, **not** required-by-user-choice optional, purpose **Advertising or marketing**, **Analytics**, **Fraud prevention, security and compliance**:
    - **Location > Approximate location** (from IP)
    - **App activity > App interactions**
    - **App info and performance > Crash logs, Diagnostics**
    - **Device or other IDs > Device or other IDs**
  - Check Google's current list at https://developers.google.com/admob/android/privacy/play-data-disclosure before submitting.
- [ ] **Advertising ID**: *Yes*, used for **Advertising or marketing** and **Analytics** (the AD_ID permission is added by the Ads SDK).
- [ ] **Government apps / financial features / health**: No.
- [ ] **News app**: No.

### Store presence
- [ ] **Main store listing**: text from `store_listing.md`, icon, feature graphic, screenshots.
- [ ] **Store settings**: category Arcade, contact email.

## E. Testing tracks → production
1. **Internal testing**: upload the `.aab`, add your own email and install from the opt-in link (fastest check that signing works).
2. **Closed testing**: at least 12 testers opted in for 14 days (required for new personal accounts). Ask friends and share the opt-in link.
3. **Production**: apply for production access in the Dashboard, then **Create release**, upload the latest `.aab`, add release notes ("First release!"), choose countries and roll out.
4. After publishing, link the app in **AdMob > App settings > Link to app store**.

## F. Every update
- [ ] Bump the version (Player Settings > Version, e.g. 1.0.1); the release menu bumps the version code automatically.
- [ ] Re-enter keystore passwords, build the `.aab`, upload to a track and roll out.
