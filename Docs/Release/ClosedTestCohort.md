# Closed-test cohort (Phase 17 Step 15)

Preparation for the Closed track **Closed testing - Alpha**. Nothing here invites anyone: the opt-in link works only after the Closed release is published in Step 16. No tester names, email addresses or other personal data go in this repository; the ledger records anonymous counts only.

## Milestones (keep them separate)

| Milestone | Means | Does not mean |
| --- | --- | --- |
| Submitted | The human sent the Closed release and pending setup changes to Google | Approved, available, or any tester opted in |
| Approved and published | Google's review passed and the Closed release is available on the track | Anyone has opted in |
| Opted in | A tester accepted the opt-in page with their Play account | That tester has met 14 days |
| Phase 17 clock started | The plan's Step 16 definition: release published **and** at least 12 eligible accounts actually opted in, none still in Internal | The 14-day requirement is met |
| Production-eligibility requirement | When applying (Phase 22), **at least 12 testers have each been continuously opted in for the preceding 14 days** | Production approval: Google also looks at engagement and may ask for more testing ([answer/14151465](https://support.google.com/googleplay/android-developer/answer/14151465)) |

A tester who leaves and rejoins does not keep their earlier days. "Fourteen days after the 12th opt-in" is only true if those 12 stay enrolled for the whole period.

## Target

- Invite **15-20** people so that drop-outs still leave **at least 12** continuously opted in.
- **Project rule (conservative):** owner-controlled accounts are not counted toward the 12, including the owner's main account and any second account or device the owner controls. Google's guidance does not state a blanket exclusion; this rule is ours, so the count does not depend on how Google treats them. These accounts may still be used for the owner's own checks.
- Eligible tester: an **Android phone with Android 8.0 or newer** (minSdk 26; ARM64, which covers almost every current phone) and the **Google account they use for the Play Store**. The account's address does not have to be Gmail. iPhones cannot take part.
- Tester-track rule (plan Policy Baseline): the Closed cohort is never used for Internal testing. Anyone in Internal who joins Closed leaves Internal first; their Closed continuity starts at the Closed opt-in.

## Owner devices

- **S23:** its account **stays on Internal** for now; its enrollment and installed game are not changed.
- **Second phone (approved 2026-11-22):** a compatible Android device with a separate Google account that has **never joined Internal**. It is the first clean Closed-delivery check: opt in, then install from the Closed track, without touching the S23. Check its model, Android version and compatibility before use. Evidence must include the account's Closed enrollment and Play showing the app on the Closed track; the installer and signing certificate alone do not identify the track. Not counted toward the 12.

## Setup (human, in Step 16)

| Choice | Decision |
| --- | --- |
| How testers are added | **Email list** on the Closed track; the owner enters the addresses in the Console |
| Feedback channel | The **public contact email** already approved for the listing, entered as the track's feedback address (Play shows it on the opt-in page) |
| Analytics | None added for testing |

## Private tracker (outside the repository)

Kept by the owner only, for example in a private spreadsheet. One row per person:

| Column | Example |
| --- | --- |
| Tester ID | T01 |
| Android confirmed | yes |
| Asked / agreed | 2026-11-22 / 2026-11-22 |
| Added to Console list | date |
| Opted in | date, and how it was confirmed (Console count or tester's confirmation) |
| Still opted in | dates checked |
| Feedback received | yes / summary |

The ledger gets only counts: invited, opted in, still opted in on given dates.

## Message 1 - asking now (Turkish)

```text
Merhaba! Galaxplode adında küçük bir mobil bulmaca oyunu geliştiriyorum (taş eşleştirme / match-3). Oyunu Google Play'de yayınlayabilmem için en az 12 kişinin kapalı teste katılıp en az 14 gün boyunca testte kalması gerekiyor. Bana yardım eder misin?

Gerekenler:
• Android telefon (Android 8 veya üzeri; iPhone maalesef olmuyor)
• Play Store'da kullandığın Google hesabının e-posta adresi (Gmail olmak zorunda değil)
• En az 14 gün boyunca, mümkünse test bitene kadar test programında kalman
• Oyunu gerçekten oynayıp gördüğün hataları ve fikirlerini bana yazman

Oyun ücretsiz; hesap açmanı istemiyor, reklam ve uygulama içi satın alma yok.

Uygunsan Play Store'da kullandığın Google hesabının e-posta adresini bana gönderir misin? Test başladığında sana katılma bağlantısını yollayacağım.
```

## Message 2 - when the Closed release is live (Turkish, Step 16)

Send only after the Closed release is approved and available. Fill in the two placeholders before sending.

```text
Galaxplode testi başladı, desteğin için çok teşekkürler!

1. Bu bağlantıyı Play Store'da kullandığın Google hesabıyla aç: [KATILMA BAĞLANTISI]
2. "Test kullanıcısı ol" düğmesine bas (İngilizce görünürse "Become a tester").
3. Sonra sayfadaki Google Play bağlantısından oyunu yükle. Oyun hemen görünmezse birkaç saat sonra tekrar dene.
4. Lütfen en az 14 gün, mümkünse test bitene kadar testten çıkma ve oyunu silme.
5. Oyunu farklı günlerde oynamaya ve mümkün olduğunca çok bölüme ulaşmaya çalış. Özel taşları oluşturmayı, menüleri ve geri tuşunu da dene. Bir hata görürsen ya da bir şey kafana takılırsa hemen yaz.

Geri bildirimini [GERİ BİLDİRİM E-POSTASI] adresine gönderebilirsin. Şunları yazarsan çok yardımcı olur:
• Telefon modelin ve Android sürümün (Ayarlar > Telefon hakkında)
• Oyun çöktü, dondu ya da kapandı mı? Ne yaparken oldu?
• Dokunma veya kaydırma sorunu oldu mu? Taşlar istediğin gibi yer değiştirdi mi?
• Kafa karıştıran bir ekran ya da düğme var mıydı?
• Bölümler çok mu kolay, çok mu zor? Hangi bölümde takıldın?
• Yazılar ve taşlar rahat okunuyor ve ayırt ediliyor mu?
• Yükleme veya güncelleme sırasında sorun yaşadın mı?
Ekran görüntüsü eklersen daha da iyi olur.
```

## Readiness for Step 16

The plan's Step 16 gate includes "the cohort is ready". **Exception approved 2026-11-22 (human; ChatGPT and Codex reviews):** Step 16 may be prepared and submitted while recruiting continues (about 8-10 people so far), reusing the verified code 4. Record "submitted while recruiting" only once submission has actually happened. Phase 17 stays open until the clock-start definition above is met; the 14-day requirement is not recorded as met until at least 12 qualifying testers have each been continuously opted in for 14 days.
