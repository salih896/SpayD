# Denge ve Bağlılık Notları (ÖRNEK DEĞERLER – onay bekler)

Tüm sayılar `Core/Combat/ContentCatalog.cs` ve ilgili Rules sınıflarındaki yer tutuculardır; GDD içeriği (kule/asker/bina listeleri) dışına çıkılmamıştır.

## Uzun süre oynatan tasarım öğeleri (uygulandı)
- **Karşıtlık sistemi:** Mızrak kulesi↔Atlı, Elf kulesi↔Peri, Büyücü sıçraması↔kalabalık, Goblin↔kaynak avı. Tek tip ordu işe yaramaz; oyuncu kompozisyon düşünür.
- **Yıldız (0/1/2/3) ve XP:** %51 zafer, %75 2 yıldız, %100 3 yıldız; XP = 10 + hasar/4 + yıldız×15, hedef seviyesinin her basamağı +%6.
- **Seviye ile kilit açma:** asker/kule/bina/kaynak seviye kademeleriyle açılır (sürekli yeni hedef).
- **Kalıcı kule aşınması + tamir döngüsü:** saldırı sonrası kule hasarı (30/20/10/5/5, max %70) → tamir maliyeti/süresi; savunmacıyı köyüne geri çeker.
- **Yağma:** savunmacı kaynağı × %70 × depo hasarı; yalnız açık kaynaklar.
- **Geri çekilme riski:** sağlıklı askerin %25'i feda (tohumlu RNG).
- **Birim/kule seviye ölçeği:** 100+20l+2l² %.

## Sonraki adımlar (öneri)
Araştırma kataloğu JSON'u, üniversite kapısı, asker/kule yükseltme maliyetleri, günlük görev/ödül döngüsü (onay gerekir).

## Denge turu 1 (13.11.2026) — simülasyonla ayarlandı
Araç: `Testing/UnitTests/BalanceSimTests.cs`. Kurallara uyan "aktif oyuncu" botu (günde 16 saat uyanık; 2 ve 4 saatte bir giriş) oyunu gerçek GameState API'siyle oynar; sonuç `docs/balance-report.txt`. Her sayı değişikliğinden sonra testler çalıştırılınca rapor yenilenir.

Bulunan sorunlar ve değişiklikler:
| Sorun (önce) | Değişiklik |
|---|---|
| Sv30'a 7,6 günde (2 saatlik oyuncu), Sv10'a birkaç saatte ulaşılıyordu | XP eğrisi 100+12l+2l² → **100+40l+8l²** (Sv1→2 yine 100; toplam ≈81 bin). Yükseltme XP'si seviye başına 12 → **8**. Savaş XP'si hedef seviyesi çarpanı %12 → **%6** |
| Gerçekçi oyuncu Sv10'dan sonra Kolay hedefi bile yenemiyordu; baskınlar hep kaybediliyordu | Yapay köy ve baskıncı gücü artık ham oyuncu seviyesine değil, **o seviyedeki tipik oyuncunun yapı seviyesine** bağlı: `GameRules.ExpectedStructureLevel` (Sv10→2, 15→4, 20→6, 25→9, 30→12) |
| Hedef kule sayısı seviyeyle ayrı büyüyordu (TowerScalePct=200) | Kule sayısı = oyuncunun kule hakkı × Kolay %60 / Normal %90 / Zor %100; seviye Kolay −1 / Zor +1. TowerScalePct=100 |
| Geç oyunda saldırgan araştırma bonusu alıyor, hedef almıyordu | Hedefe tipik savunma araştırması kadar bonus: ‰ = Sv²/6 (Sv30 ≈ +%15) |
| Geç oyunda ordu kulelerden çok hızlı büyüyordu (Sv30 ≈ 226 asker) | Ordu kapasitesi 10 + 6×kışla seviyesi → **10 + 3×** (Sv30 ≈ 130) |
| Baskıncı ordusu 4 + 1,5×Sv | **4 + Sv** |

Sonuç (bot, tam ordu, 6 tohum): Kolay ≈ %100, Normal ≈ %85-95 (0,7-2,8 yıldız), Zor ≈ %0-50 (ort. ~%25). Sv30: 2 saatlik oyuncu ≈ 24 gün, 4 saatlik ≈ 45 gün. Baskınların ~%70'i savunuluyor.

Bilinen sınırlar / sonraki tur: bot askerleri tek seferde yayıyor (insan oyuncu daha iyi oynar → Normal pratikte daha kolay olabilir); Normal hedefi biraz kolay, ince ayar gerçek oyun testinden sonra. Kaynak üretimi/maliyetleri, görev ödülleri, araştırma süre/maliyeti bu turda değişmedi.

## Denge turu 2 (14.11.2026) — 10 asker + GDD kule hakkı (5→10)
Kule sayısı 33'ten 10'a inince ordular her şeyi eziyordu (%100 zafer, baskınların hiçbiri savunulamıyordu). Değişiklikler:
- Kule can/hasar ×2,5 (az ama güçlü kule).
- Ordu kapasitesi 20 + 1,2 × kışla seviyesi (Sv30 ≈ 80-90).
- Hedef kuleleri `GameRules.ExpectedTowerLevel` (Sv10→3, 15→5, 20→8, 25→11, 30→16); baskıncı askerleri `ExpectedStructureLevel`.
- Hedef kule sayısı: Kolay %50, Normal %80, Zor %100 (+1 seviye) kule hakkı. Baskıncı ordusu 6 + 1,5×Sv.
Sonuç (bot): saldırıların %77-80'i zafer (ort. ~2 yıldız), Normal ~%70, Zor ~%25; Sv30 ≈ 23 gün (2 saatte bir) / 45 gün (4 saatte bir); baskınların bir kısmı savunuluyor.

## Test durumu
Core: 118 EditMode testi geçti (14.11.2026).
