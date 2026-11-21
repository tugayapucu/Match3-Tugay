# Closed-test cohort (Phase 17 Step 15)

Preparation for the Closed track **Closed testing - Alpha**. Nothing here invites anyone: the opt-in link works only after the Closed release is published in Step 16. No tester names, email addresses or other personal data go in this repository; the ledger records anonymous counts only.

## Target

- Invite **15-20** people; the goal is **at least 12 continuously opted in for at least 14 days** (Play's rule for new personal accounts before applying for production).
- **Owner-controlled accounts are not counted:** neither the owner's main account nor any second account or device the owner controls. They may still be used for the owner's own checks.
- Eligible tester: an **Android phone with Android 8.0 or newer** (minSdk 26; ARM64, which covers almost every current phone) and the Google account they use for the Play Store. iPhones cannot take part.
- Tester-track rule (plan Policy Baseline): the Closed cohort is never used for Internal testing. Anyone in Internal who joins Closed leaves Internal first; their Closed continuity starts at the Closed opt-in.

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
Merhaba! Galaxplode adında küçük bir mobil bulmaca oyunu geliştiriyorum (taş eşleştirme / match-3). Oyunu Google Play'de yayınlayabilmem için en az 12 kişinin 14 gün boyunca kapalı teste katılması gerekiyor. Bana yardım eder misin?

Gerekenler:
• Android telefon (Android 8 veya üzeri; iPhone maalesef olmuyor)
• Play Store'da kullandığın Google (Gmail) adresi
• 14 gün boyunca test programından çıkmaman; arada bir oyunu açıp birkaç bölüm oynaman yeterli

Oyun ücretsiz; hesap açmanı istemiyor, reklam ve uygulama içi satın alma yok.

Uygunsan Play Store'da kullandığın Gmail adresini bana gönderir misin? Test başladığında sana katılma bağlantısını yollayacağım.
```

## Message 2 - when the Closed release is live (Turkish, Step 16)

Fill in the two placeholders before sending.

```text
Galaxplode testi başladı, desteğin için çok teşekkürler!

1. Bu bağlantıyı Play Store'da kullandığın Google hesabıyla aç: [KATILMA BAĞLANTISI]
2. "Test kullanıcısı ol" düğmesine bas (İngilizce görünürse "Become a tester").
3. Sonra sayfadaki Google Play bağlantısından oyunu yükle. Oyun hemen görünmezse birkaç saat sonra tekrar dene.
4. Lütfen 14 gün boyunca testten çıkma ve oyunu silme; arada bir açıp birkaç bölüm oynaman yeterli.

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

Step 16's pre-launch gate includes "the cohort is ready". Recommended meaning: **at least 12 eligible people, not counting owner-controlled accounts, have agreed and given their Play account address**, ideally 15. The human decides when to start.
