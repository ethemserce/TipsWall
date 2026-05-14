# Play Store / App Store listing — TipsWall

Single source of truth for the **store assets** (titles, descriptions,
screenshots, data-safety form answers, content rating). Update this file
when copy changes; copy-paste from here into the Play Console / App Store
Connect forms. Privacy policy and terms of service live separately under
`api/deploy/legal/{privacy,terms}.html` (served by Caddy).

---

## URLs to enter in Play Console

| Field | Value |
|---|---|
| Privacy policy | `https://www.tipswall.com/privacy` |
| Terms of service | `https://www.tipswall.com/terms` |
| Support email | `ethemserce@gmail.com` |
| Marketing / website | `https://www.tipswall.com` (placeholder page until a real landing site ships) |

> The api.tipswall.com host also serves both URLs, so you can swap to
> `https://api.tipswall.com/privacy` if DNS for tipswall.com isn't ready
> at submission time. Both resolve to the same content.

---

## Listing fields — Play Console / App Store Connect

### App name
- **Tr / Default**: `TipsWall — Maç Analizi`
- **En**: `TipsWall — Match Analysis`

(Play Store cap: 30 chars. Both fit.)

### Short description (Play Store: 80 chars; App Store subtitle: 30)

- **Tr (80)**: `Maç analizleri, isabetli tahmin takibi ve sezon istatistikleri.`
- **En (80)**: `Match analytics, tip-tracking history and season-long stats.`
- **iOS subtitle Tr (30)**: `Maç analizi & tahmin takibi`
- **iOS subtitle En (30)**: `Match analytics & tip tracker`

### Full description (Play Store: 4000 chars)

#### Türkçe

```
TipsWall, futbol maçlarını ciddi takip edenler için tasarlanmış bir analiz ve
tahmin günlüğü uygulamasıdır. Kupon oynatmaz, bahis sitesine yönlendirmez,
para işlemi yapmaz — sadece veriyi yorumlamayı kolaylaştırır.

NELERİ YAPAR
• Günlük maçları durumlarına göre süzer (canlı, yaklaşan, biten).
• Her maçın detayında kadrolar, olaylar, istatistikler, hakem geçmişi, eksikler,
  beklenen goller (xG) ve baş-başa karşılaşmaları tek ekranda toplar.
• Sezon boyunca takım ve oyuncu performansını (maç başına gol, asist, kart,
  başlama oranı) gösterir.
• 30+ marketi (Maç Sonucu, KG, Ü/A, Çifte Şans, Asya Handikap, Tam Skor,
  HT/MS) skor üzerinden gerçek zamanlı analiz eder; tahmininin canlı durumunu
  oran değeri göstermeden takip edersin.
• Sezon karneliği tutar — hangi marketlerde başarılıyım, kalibrasyonum nasıl,
  hangi risk profilini tercih ediyorum diye karşılaştırma sunar.

NELERİ YAPMAZ
• Bahis kuponu kabul etmez.
• Bahisçiyle entegre değildir, üçüncü taraf bahis sitesine yönlendirmez.
• Hiçbir maç sonucunu garanti etmez. Tahminler kişisel analiz amacıyla
  sunulur, finansal tavsiye değildir.

KİMLER İÇİN
• Maçtan önce takım analizini ciddiye alan futbol meraklıları.
• Sezon boyunca tahmin geçmişini ölçen, kendini kalibre etmek isteyen kişiler.
• Bahis-merkezli olmayan, sade ve sayısal bir analiz aracı arayanlar.

ÖZELLEŞTİRME
• Türkçe / İngilizce — telefon ayarından otomatik veya elle seçim.
• Açık / koyu / sistem teması.
• İsteğe bağlı: oran değerleri tamamen gizlenebilir; tahminler sadece
  metin olarak gösterilir.

GİZLİLİK
TipsWall reklam ağı kullanmaz, üçüncü taraflarla pazarlama amacıyla veri
paylaşmaz, kişisel veriyi minimumda tutar. Anonim kullanım istatistikleri
kullanılır mı kararı tamamen sana aittir (Ayarlar → Kullanım verisi paylaş).
Hesabını uygulama içinden istediğin zaman silebilirsin.

SORU?
Geri bildirim için: ethemserce@gmail.com
Gizlilik politikası: https://www.tipswall.com/privacy
```

#### English

```
TipsWall is a match-analytics and prediction-tracking app for people who
take football seriously. It does not place bets, does not link out to
bookmakers, does not handle payments — it just makes the data easier to
read.

WHAT IT DOES
• Filters today's matches by state (live, upcoming, finished).
• Every fixture page collects lineups, events, statistics, referee
  history, sidelined players, expected goals (xG), and head-to-head
  records in one place.
• Tracks team and player performance across the season (goals, assists,
  cards, minutes played, start ratio).
• Real-time score-based grading for 30+ markets (FT result, BTTS, O/U,
  Double Chance, Asian Handicap, Correct Score, HT/FT) — see whether
  your pick is currently winning without exposing odd values.
• Season scorecard: which markets you hit, your calibration vs. system,
  your preferred risk profile.

WHAT IT DOESN'T DO
• Does not accept bet slips.
• Is not integrated with any bookmaker and does not redirect to one.
• Does not guarantee any match outcome. Predictions are for personal
  analysis only — not financial advice.

WHO IT'S FOR
• Football fans who actually do pre-match analysis.
• People who want to measure their own season-long calibration.
• Anyone looking for a quiet, numbers-driven analytics tool without
  the betting circus.

PERSONALISATION
• Turkish / English — auto from device locale or set manually.
• Light / dark / system theme.
• Optional: hide odd values entirely; only the named tip is shown.

PRIVACY
TipsWall uses no ad networks, shares no data for marketing, and keeps
personal data to a minimum. Anonymous usage analytics are entirely
opt-in (Settings → Share usage data). You can delete your account
from inside the app at any time.

QUESTIONS?
Feedback: ethemserce@gmail.com
Privacy policy: https://www.tipswall.com/privacy
```

---

## Content rating (Play Console questionnaire)

The questionnaire is mostly yes/no. Answer these consistently with the
app being a **data analytics + tip tracker**, not a betting client.

| Question category | Answer | Rationale |
|---|---|---|
| Violence, blood, sexual content, drugs, alcohol, tobacco | **No** to all | App has none of this |
| Gambling — does the app simulate gambling? | **No** | No virtual coins, no win/lose mechanic with monetary surrogate, no wheel-of-fortune UI |
| Gambling — does the app provide real-money gambling? | **No** | Cannot place bets, cannot deposit money |
| References to gambling (e.g. odds shown, betting tips) | **Yes — informational only** | The app references markets and tip outcomes; odd values are hidden by default but visible if the user opts in. Disclose this honestly to avoid a rating bump later. |
| User-generated content / social interaction | **No** (currently) | When tipster leaderboard ships, flip to Yes |
| Sharing user location / personal info | **No** | Only email + username for account |
| In-app purchases | **No** (currently); flip to **Yes** when Premium ships | |

**Likely outcome:** PEGI 12 / ESRB Teen-equivalent. Some markets in
several countries gate "References to gambling" behind 18+; the app is
already declared 18+ in Terms of Service, so this is consistent.

---

## Data safety — Play Console form

| Data type | Collected? | Shared? | Optional? | Purpose | Notes |
|---|---|---|---|---|---|
| Email address | Yes | No | Required for sign-up | Account management | Stored on app's own backend |
| User IDs (username) | Yes | No | Required for sign-up | Account management | |
| Approx. location | No | — | — | — | We do not read location |
| Precise location | No | — | — | — | We do not read GPS |
| Photos / videos / files | No | — | — | — | App does not access media |
| Contacts | No | — | — | — | |
| Calendar | No | — | — | — | |
| App activity (page views, tap events) | Yes | Yes — Google (Firebase Analytics) | **Yes — opt-in** | App functionality, Analytics | Default is OFF; the user must opt in via the consent prompt or Settings toggle. |
| App info & performance (crash logs, diagnostic data) | Yes | Yes — Functional Software Inc. (Sentry) | No | Crash diagnosis | Default-on; user can delete the account to wipe upstream data |
| Device or other identifiers (anonymous device UUID for guest quota) | Yes | No | No | Guest-mode daily quota enforcement | Mobile-minted UUID, not the OS advertising ID |
| Authentication credentials (password hash, JWT tokens) | Yes | No | Required for sign-up | Account management | Bcrypt hashed; tokens HMAC-SHA256 signed |
| Financial info (payments) | No | — | — | — | No in-app purchases yet |

**Security practices section** — check these boxes:

- ✅ Data is encrypted in transit (TLS 1.2+ via Let's Encrypt)
- ✅ User can request data deletion (in-app Account → Delete)
- ❌ Data is encrypted at rest beyond standard volume encryption (be honest — Postgres is not yet column-level encrypted; mark as "no" or "partial" depending on Play Console wording)
- ✅ Independent security review (mark "No" unless an audit happens)

---

## Target audience

- **Target audience**: **Adults (18+)**. Justification: app references
  sports betting markets even though it does not place bets.
- **Appeals to children?**: No.
- This selection skips the "Families" policy compliance.

---

## Tags / categories

- **Primary category**: Sports
- **Secondary** (App Store): News / Sports
- **Tags** (Play Store, max 5):
  - Football
  - Sports analytics
  - Match prediction
  - Football stats
  - Sports tracker

---

## Screenshots — what to capture

Play Store needs 2-8 screenshots per phone form factor. Capture on a
device with 1080×1920 (or 1440×2960) resolution, portrait. Crop or
letterbox to 16:9. Names below are for filing — use them as the file
names in `mobile/store-assets/screenshots/` once captured.

| # | Screen | Caption (TR) | Caption (EN) |
|---|---|---|---|
| 1 | Home — today's matches, state filter pill row | "Bugünün maçları, durumuna göre süzülmüş" | "Today's matches, filtered by state" |
| 2 | Fixture detail — hero + score + live timer | "Anlık skor + canlı dakika" | "Live score + match minute" |
| 3 | Lineups tab — pitch view with both teams | "Kadro saha üzerinde" | "Pitch-view lineups" |
| 4 | Odds rates — HIT / ROI / IMP columns | "Her tahminin tutturma + verim yüzdesi" | "Hit rate + ROI per tip" |
| 5 | Player detail — bio + season stats | "Oyuncu sezon karneliği" | "Player season scorecard" |
| 6 | Coupons — your saved tip lists | "Tahmin listelerin sezon boyunca takipte" | "Your tip lists, tracked all season" |
| 7 | Settings — odds-hidden toggle + analytics consent | "Oranlar gizli, ne paylaştığını sen seç" | "Odds hidden by default, you choose what to share" |

### Feature graphic (1024 × 500, Play Store required)

Layout brief (delivery is your call — Figma / Canva / Photoshop):

- Background: app's brand dark surface (`#0f1115` or similar)
- Left third: TipsWall logo (white) + tagline "Maç analizi, sadece veri"
- Right two-thirds: a flat illustration of the fixture-detail screen
  (don't screenshot a real phone — use a stylised mockup), tilted ~10°,
  with three callout chips: `HIT %`, `ROI`, `IMP`
- Avoid: any monetary symbols, currency, the words "kupon / bahis /
  para / kazan" — Play Store reviewers scan for these

### Promo video (optional, App Store + Play Store)

15-30 seconds. Suggested cuts:
1. App icon → home screen (1 s)
2. Today's matches filter swipe (3 s)
3. Open a fixture, scroll through tabs (5 s)
4. Tap a tip, watch it appear in the coupon sheet (4 s)
5. Settings → odds hidden toggle (3 s)
6. Logo + URL (3 s)

---

## Closed testing (the 14-day, 20-tester gate)

Personal Play Console accounts must run **Closed testing with 20+
unique testers for 14 consecutive days** before they can apply for
production access. Practical playbook:

1. Build a signed AAB via EAS:
   ```
   cd mobile
   npx eas build --profile production --platform android
   ```
   `production` profile in `eas.json` already produces an `app-bundle`.
2. Upload the AAB to Play Console → Testing → Closed testing → Create
   track.
3. Add an **email list** with at least 20 tester addresses (friends,
   family, colleagues, your other accounts). Each must accept the
   tester invite link AT LEAST ONCE.
4. Have testers install the app via the opt-in link and use it. The
   14-day clock counts calendar days, not active days.
5. After 14 days, Play Console exposes "Apply for production access".

While the 14 days run, you can also:
- Set up **Internal testing** (no tester minimum, max 100 testers) for
  fast iteration.
- Polish screenshots, content rating, description text.
- Submit for review on the iOS side (Apple has no 14-day gate).

---

## Submission checklist

- [ ] `google-services.json` + `GoogleService-Info.plist` present and
      gitignored, build profile picks them up
- [ ] Privacy policy live at https://www.tipswall.com/privacy (or
      api.tipswall.com/privacy until DNS lands)
- [ ] Terms of service live at https://www.tipswall.com/terms
- [ ] Data safety form filled (above table)
- [ ] Content rating questionnaire completed
- [ ] App icon 512×512 (`assets/images/icon.png` — already shipped)
- [ ] Feature graphic 1024×500 PNG
- [ ] Min 2 phone screenshots (recommend all 7 above)
- [ ] Short + full descriptions in TR and EN
- [ ] Target audience set to 18+
- [ ] Closed testing track created with ≥ 20 invited testers
- [ ] Signed AAB uploaded
- [ ] Tester opt-in link distributed
