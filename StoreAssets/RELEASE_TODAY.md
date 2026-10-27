# One Block: release-day runbook

Work through these in order. The steps marked **(you)** involve accounts, passwords or payments, so only you can do them.

---

## 0. Check first: can you publish to Production today?
Play Console > **Dashboard**. If you see **"Apply for production access"** or a requirement for **"closed test with at least 12 testers for 14 days"**, your account is a *personal account created after Nov 2023*. That means:
- **Today:** publish to **Closed testing** (real Play Store install link for your testers).
- **In 14 days:** apply for production access, then promote the same build to Production.

Organisation accounts, and personal accounts created before Nov 2023, can go straight to Production.

---

## 1. AdMob (you, ~15 min)
1. https://admob.google.com > sign in > **Apps > Add app** > Android > *"Is the app listed on a supported app store?"* **No** (you'll link it once it's live).
2. App name `One Block`. Copy the **App ID** (`ca-app-pub-XXXXXXXX~YYYYYYYY`).
3. **Ad units > Add ad unit > Rewarded**, named `Rewarded`, then copy its ID (`ca-app-pub-XXXXXXXX/ZZZZZZZZ`).
4. **Ad units > Add ad unit > Interstitial**, named `Between runs`, then copy its ID.
5. **Payments**: add your payment/tax details so AdMob can pay you.
6. **Settings > Test devices > Add**: your own phone. Never tap real ads on your own device, or the account can be banned.

In Unity, select `Assets/_Game/Data/AdsConfig.asset`:
- **Android App Id / Rewarded Unit Id / Interstitial Unit Id** → paste your three IDs.
- **Use Test Ads** → **untick** it.

> New AdMob apps serve few or no ads until the app is live on Play and linked in AdMob (step 7). Until then, the revive, x2 coins and bundle buttons stay greyed out, because they only enable once an ad has loaded. That's expected and fills in over the first days.

---

## 2. Signing key (you, 5 min, do this once and keep it forever)
1. Unity: **Edit > Project Settings > Player > Android tab > Publishing Settings**.
2. **Keystore Manager > Keystore... > Create New > Anywhere…** and save as `D:\Keys\oneblock.keystore` (outside the project folder).
3. Enter a strong **password** twice.
4. **New Key Values**: Alias `oneblock`, the same or another password, Validity **50**, First and Last Name = your name, then **Add Key**. Click **Yes** to use it.
5. Publishing Settings should now show *Custom Keystore* ticked, your file, and alias `oneblock`.
6. **Back up** `oneblock.keystore` and both passwords: a password manager plus a USB drive or cloud copy. Every future update must be signed with this key.

Unity forgets these passwords when it restarts. Re-type them in Publishing Settings before every release build.

---

## 3. Build the release bundle (~10–15 min)
Unity menu **One Block > Build > Android Release App Bundle (.aab)**.
- If it says **BLOCKING**, fix what it names (keystore or passwords).
- If it warns about **test ads**, go back to step 1.

Result: `Builds/OneBlock-1.0.0-2.aab`. That's the file you upload.

(Optional but smart: first run **Android Test APK**, install it on your phone, and check that the tutorial works and a real ad loads. The first load can take a minute.)

---

## 4. Privacy policy URL (you, 10 min)
1. Open `StoreAssets/privacy_policy.html` in a text editor and replace `<DEVELOPER NAME>`, `<SUPPORT EMAIL>` (3 places) and `<DATE>`.
2. Host it for free. Easiest is **Google Sites** (sites.google.com): new site > paste the text > **Publish** > copy the URL.
   Alternative: a GitHub repo, upload the file as `index.html`, then Settings > Pages.
3. Keep the URL for step 5.

---

## 5. Play Console: create the app (you, 45–60 min)
https://play.google.com/console > **Create app**
- App name `One Block` · Default language `English (United States)` · **Game** · **Free**
- Tick both declarations > **Create app**

### 5a. Store listing (Grow users > Store presence > Main store listing)
Copy from `StoreAssets/store_listing.md`:
- App name, Short description, Full description
- App icon → `icon_512.png`
- Feature graphic → `feature_graphic_1024x500.png`
- Phone screenshots → all 7 from `screenshots/`
- 7-inch and 10-inch tablet screenshots → the same 7 files (recommended)
- **Save**

### 5b. Store settings (Grow users > Store presence > Store settings)
- Category **Game > Arcade** · Tags: Arcade, Casual, Endless runner, Offline, Single player
- Contact email = your support email · **Save**

### 5c. App content (Monitor and improve > Policy and programs > App content)
Complete every card:

| Card | Answer |
|---|---|
| Privacy policy | URL from step 4 |
| App access | All functionality available without special access |
| Ads | **Yes**, my app contains ads |
| Content rating | Email · Category **Game** · Violence: only *cartoon/fantasy*, not realistic, no blood · everything else **No** (no user chat, no location sharing, no gambling, no digital purchases) · Submit |
| Target audience | Ages **13–15, 16–17, 18 and over** (do **not** pick under 13) · Appeals to children? **No** |
| Data safety | See below |
| Advertising ID | **Yes**, used for **Advertising or marketing** and **Analytics** |
| Government apps | No |
| Financial features | None |
| Health | None |
| News app | No |

**Data safety form**
- Collects or shares required user data? **Yes**
- All data encrypted in transit? **Yes**
- Account creation: **My app does not allow users to create an account**
- Users can request data deletion? **No**
- Data types (from the Google Mobile Ads SDK). For each: **Collected: Yes · Shared: Yes · Processed ephemerally: No · Required** · Purposes **Advertising or marketing, Analytics, Fraud prevention, security and compliance**:
  - Location > **Approximate location**
  - App activity > **App interactions**
  - App info and performance > **Crash logs**, **Diagnostics**
  - Device or other IDs > **Device or other IDs**
- Save > Submit

---

## 6. Upload and release
**Closed testing** (personal-account path) or **Production** (if step 0 says you can):

1. **Test and release > Testing > Closed testing** (or **Production**) > **Create release** (for Closed testing, first **Create track** or use the default "Alpha").
2. **Play App Signing**: accept *"Use Google-generated key"* (recommended).
3. **Upload** `Builds/OneBlock-1.0.0-2.aab`.
4. Release name `1.0.0` · Release notes:
   ```
   <en-US>
   First release! Tap to build your way forward through 8 worlds.
   </en-US>
   ```
5. **Next**, fix any red errors (usually a missing App content card), then **Save**.
6. **Countries/regions**: add all (or pick your markets).
7. Closed testing only: **Testers** tab > create an email list with at least 12 Gmail addresses > copy the **opt-in link** and send it to them. Each tester must opt in and install.
8. **Publishing overview** (left menu) > **Send changes for review**.

Google's review of a brand-new app usually takes **1–7 days**, so "released today" really means *submitted today*. You'll get an email when it's approved.

---

## 7. After it goes live
- AdMob > your app > **App settings > Link to app store** > search One Block. This unlocks full ad serving.
- AdMob will ask for **app-ads.txt** on a developer website. Add it if you have one (the Google Sites page works if you set it as your developer website in Play Console).
- Closed testing path: after 14 days with 12+ opted-in testers, go to **Dashboard > Apply for production** > answer the questionnaire > then **Production > Create release > Add from library** (reuse the same `.aab`).

## Every future update
1. Player Settings > **Version** → `1.0.1` (the build menu bumps the version code automatically).
2. Re-enter the keystore passwords > **One Block > Build > Android Release App Bundle**.
3. Play Console > your track > **Create release** > upload > **Send for review**.
