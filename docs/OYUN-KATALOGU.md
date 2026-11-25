# Oyun Kataloğu (15.11.2026)
Tüm sayılar GEÇİCİ (dengeleme). Seviye ölçeği: can/hasar/üretim = taban × (100 + 20l + 2l²)% (l = seviye−1; Sv10 ≈ 4,4×, Sv30 ≈ 23,6×).

## 1. Askerler (10)
Can/hasar Sv1 değeri. Hedef: Goblin önce kaynak depoları, Atlı Kılıçlı önce kuleler; diğer tüm yer askerleri (Okçu ve Büyücü dahil) yol boyunca en yakın yapıya, uçanlar kuş uçuşu en yakın yapıya saldırır (Duvar yakındaki yer askerlerini üstüne çeker). Aralık = saldırı arası (tick; savaş ekranda saniyede 6 tick ilerler, yani 10 tick ≈ 1,7 sn). Savaş süresi 5 dk. Menzil/hız hücre cinsinden.

| Asker | Açılış | Can | Hasar | Aralık | Menzil | Hız | Özellik | Eğitim maliyeti | Eğitim |
|---|---|---|---|---|---|---|---|---|---|
| Kılıçlı | Sv2 | 70 | 14 | 8 | 1,5 | 0,07 | Zırh %10, en yakın yapıya saldırır | 40 odun | 2 dk |
| Okçu | Sv3 | 60 | 17 | 10 | 2,0 | 0,07 | Uzaktan, kırılgan, en yakın yapı | 45 odun | 3 dk |
| Goblin | Sv4 | 35 | 13 | 6 | 1,5 | 0,16 | Çok hızlı, önce kaynak depoları, sonra diğer yapılar | 20 odun | 2 dk |
| Mızraklı | Sv5 | 150 | 12 | 10 | 2,0 | 0,04 | Zırh %30 | 60 odun, 15 taş | 5 dk |
| Büyücü | Sv7 | 50 | 43 | 18 | 2,0 | 0,06 | Alan hasarı 1,5, ateş direnci %25 | 70 odun, 30 taş | 5 dk |
| Atlı Kılıçlı | Sv9 | 260 | 22 | 10 | 1,5 | 0,15 | Zırh %20, atlı, önce kuleler, sonra diğer yapılar | 90 odun, 60 taş | 10 dk |
| Peri | Sv11 | 55 | 9 | 8 | 2,0 | 0,10 | Uçar, yolu izlemez | 100 odun, 80 taş | 5 dk |
| Dev | Sv13 | 600 | 30 | 15 | 1,5 | 0,03 | Zırh %25, en yakın yapı | 160 odun, 110 taş | 10 dk |
| Mancınık | Sv17 | 140 | 70 | 35 | 2,0 | 0,03 | Kuşatma, alan 1,5 | 220 odun, 140 taş, 60 altın | 10 dk |
| Ejderha Binicisi | Sv21 | 420 | 34 | 12 | 2,0 | 0,09 | Uçar, alan 1,0, ateş direnci %50 | 260 odun, 200 taş, 110 altın | 10 dk |

**Orduda kapladığı yer:** Kılıçlı/Okçu/Goblin 1, Mızraklı/Peri 2, Büyücü/Atlı 3, Dev 5, Mancınık 6, Ejderha Binicisi 10. Ordu kapasitesi = 20 + 4 × kışla seviyeleri toplamı (yer). Yolda: askerler 3 şeride dağılır, üst üste binmez (Dev ve Mancınık 2 şerit); birbirini engellemez.

**Geliştirme:** Kışlaya dokununca açılan pencerede tür başına yapılır; işçi + kaynak (eğitim maliyeti × 8 × seviye çarpanı) + süre gerekir. Süre eğitim süresiyle orantılıdır (Goblin hızlı, Dev/Mancınık/Ejderha yavaş). Hedef seviye, kışla seviyesini ve oyuncu seviyesini aşamaz.

**Yetenekler** (asker Sv2 / Sv3 / Sv8'de açılır, savaşta otomatik çalışır):

| Asker | Sv2 | Sv3 | Sv8 |
|---|---|---|---|
| Kılıçlı | Kalkan: gelen hasar −%20 | Öfke: canı yarının altındayken hasar +%30 | Girdap: saldırıları çevreye de vurur |
| Okçu | Kartal Gözü: menzil +%25 | Çift Ok: her 3. atış 2 kat | Kule Avcısı: kulelere +%40 |
| Büyücü | Geniş Alan: alan büyür | Taş Eritme: kulelere +%25 | Meteor: her 5. büyü 3 kat |
| Goblin | Çevik: hız +%20 | Yağmacı: kaynak binalarına +%50 | Kaçak: gelen hasar −%30 |
| Atlı | Hücum: ilk vuruş 3 kat | Ağır Zırh: zırh +10 | Ezici: alan hasarı |
| Mızraklı | Falanks: zırh +10 | Delici: kulelere +%30 | Sarsılmaz: yavaşlama ve kapan işlemez |
| Peri | Hızlı Kanat: hız +%20 | Şifa: 2 sn'de bir yakındakileri iyileştirir | Büyü Kalkanı: gelen hasar −%25 |
| Dev | Taş Deri: zırh +10 | Sarsıntı: alan hasarı | Yenilenme: saniyede canının %1'i |
| Mancınık | Uzun Kol: menzil +%20 | Ateş Güllesi: kulelere +%30 | Çifte Atış: her 2. atış 2 kat |
| Ejderha | Ateş Nefesi: alan büyür | Pul Zırh: gelen hasar −%20 | Ejder Öfkesi: canı azken +%40 |

**Rütbe:** Acemi (1-2) → Eğitimli (3-5) → Uzman (6-9) → Zırhlı (10-14) → Usta (15-19) → Kahraman (20-24) → Efsane (25+).

**Görsel (her geliştirme bir parça ekler, birikimli):**
- **Sv2-11:** kemer, deri yelek, bileklik, deri omuzluk, demir göğüslük, demir omuzluk, miğfer siperi, sorguç, pelerin, çelik parıltısı
- **Sv12-20:** mavi aura, işlemeli pelerin, silah parıltısı, yörünge kıvılcımı, altın toka, altın omuzluk, mor aura, boynuzlu miğfer, hale
- **Sv21-30:** altın zırh, ikinci kıvılcım, uzun pelerin, parlayan gözler, taç, altın aura, üçüncü kıvılcım, ışık kanatları, ışık sütunu, efsane halkası

Atlı'da zırhı binici giyer ve ata da zırh takılır; Ejderha'da binici giyer; Mancınık'ta gövdeye levha ve bayrak eklenir.

## 2. Kuleler (7)
Hedef: önce yakın dövüşçüler, yoksa uzaktan saldıranlar; aynı grupta canı (yüzde) en az olan. Hakkı (toplam): başta 3; Sv6/Sv12/Sv20'de +1, Akademi araştırma görevleriyle +2 → en çok 8. Kuleler de yola bağlı olmalıdır (yalnızca sağ/sol/üst/alt; çapraz sayılmaz).

| Kule | Açılış | Can | Hasar | Aralık | Menzil | Havayı vurur | Özellik | Kurulum |
|---|---|---|---|---|---|---|---|---|
| Okçu Kulesi | Sv2 | 1000 | 20 | 5 | 2,5 | Evet | Hızlı atış | 120 odun |
| Duvar | Sv4 | 3200 | — | — | — | — | Saldırmaz; 2,5 hücredeki yer askerlerini üstüne çeker | 160 odun |
| Büyücü Kulesi | Sv6 | 800 | 20 | 14 | 2,5 | Evet | Alan 1,2, zırh delme %30 | 200 odun, 80 taş |
| Elf Kulesi | Sv8 | 750 | 20 | 3 | 3,0 | Evet | Uçanlara +%100 | 180 odun, 120 taş |
| Top Kulesi | Sv10 | 1100 | 20 | 22 | 3,0 | Hayır | Alan 1,2 | 300 odun, 150 taş |
| Buz Kulesi | Sv12 | 900 | 20 | 7 | 2,5 | Evet | Vurduğunu 2,5 sn yavaşlatır | 250 odun, 200 taş |
| Şimşek Kulesi | Sv16 | 850 | 20 | 13 | 2,5 | Evet | 2 askere daha zincirleme sıçrar (%70) | 300 odun, 200 taş, 120 altın |

**Geliştirme:** İşçi + kaynak + süre; hedef seviye oyuncu seviyesini aşamaz. Hasar alan kulede kalıcı aşınma oluşur (en çok %70), onarılması gerekir.

**Görsel kademeleri** (Sv1-4 / 5-9 / 10-14 / 15-19 / 20+): ahşap → taş → mazgallı → altın bantlı → sancak + dönen kristal.
- **Duvar:** kazık çit → taş duvar → mazgallı → altın şerit → sancaklar
- **Top:** namlu + gülle yığını
- **Buz:** seviyeyle çoğalan buz kristalleri
- **Şimşek:** bobinli paratoner + parlayan küre

## 3. Tuzaklar (8)
Hakkı: 4, Akademi "Tuzak Ustalığı" araştırmasıyla 6. Uçan askerleri etkilemez.

| Tuzak | Açılış | Etki | Maliyet |
|---|---|---|---|
| Çivili | Sv3 | 20 hasar | 80 odun |
| Kaygan | Sv5 | 3 sn yavaşlatır | 100 odun, 40 taş |
| Ateşli | Sv8 | 3 sn yakar (ateş direnci azaltır) | 150 odun, 60 taş |
| Zehirli | Sv10 | 6 sn zehirler | 180 odun, 90 taş |
| Buz | Sv12 | 1,5 sn dondurur, sonra yavaşlatır | 200 odun, 120 taş |
| Patlayıcı | Sv15 | 130 alan hasarı, savaş başına 1 kez | 250 odun, 150 taş, 50 altın |
| Kapan | Sv18 | 3 sn yakalar + 30 hasar | 260 odun, 160 taş, 60 altın |
| Şimşek | Sv22 | 45 hasar + 2 askere sıçrar | 320 odun, 200 taş, 90 altın |

## 4. Yollar (5 basamak)
Yol hakkı: Sv1'de 10, her seviyede +1. Geliştirme için önce Akademi'de yol araştırması yapılır (1 gün; 5000 odun, 1000 taş). Sonra her yol tek tek işçi + süre ile geliştirilir.

| Basamak | Şart | Maliyet | Süre | Düşmanı yavaşlatır | Tuzak hasarı | Görünüm |
|---|---|---|---|---|---|---|
| Tahta | — | — | — | %0 | %100 | Toprak + parke taşı |
| Taş | Sv5 | 200 odun, 150 taş | 10 dk | %10 | %100 | Taş levhalar |
| Altın+Tahta | Sv15 | 300 odun, 100 altın | 30 dk | %15 | %110 | Altın süslü tahta |
| Demir | Sv25 | 400 odun, 150 altın, 80 demir | 1 sa | %25 | %150 | Raylı demir |
| Kristal | Sv28 | 700 odun, 400 taş, 300 altın, 200 demir | 2 sa | %35 | %200 | Parlayan kristal oluklar |

## 5. Binalar (11)
| Bina | Açılış | En çok | Görev | Kurulum |
|---|---|---|---|---|
| Orman | Sv1 | 1 | 1500 odun/sa | 80 odun |
| Kereste Deposu | Sv1 | 1 | +2500 odun depo (kurulunca Sv2 garanti) | 100 odun |
| Köylü Evi | Sv1 | 1 (görevle 3) | +1 işçi | 60 odun |
| Kışla | Sv2 | 1 (Sv13'te 2, Sv26'da 3) | Asker eğitir; ordu = 20 + 4 × kışla seviyeleri toplamı (yer) | 150 odun |
| Taş Ocağı | Sv5 | 1 | 900 taş/sa | 160 odun, 60 taş |
| Taş Deposu | Sv5 | 1 | +1800 taş depo | 180 odun, 80 taş |
| Akademi | Sv5 | 1 | Araştırma (474), görevler, yol ve tuzak araştırmaları | 300 odun, 150 taş |
| Altın Madeni | Sv15 | 1 | 500 altın/sa | 400 odun, 250 taş, 100 altın |
| Banka | Sv15 | 1 | +1000 altın depo | 450 odun, 300 taş, 150 altın |
| Demir Madeni | Sv25 | 1 | 300 demir/sa | 800 odun, 500 taş, 300 altın, 100 demir |
| Demir Deposu | Sv25 | 1 | +600 demir depo | 900 odun, 600 taş, 350 altın, 200 demir |

**Geliştirme:** İşçi + kaynak (taban × (L²/2 + 1)) + süre (20L + 6L² sn); hedef seviye oyuncu seviyesini aşamaz. Üretim ve depo değeri seviye ölçeğiyle artar.

**Görsel kademeleri:**
- **Ev çatısı:** saman → kırmızı kiremit → mavi arduvaz → koyu arduvaz + altın mahya → bordo
- **Ev yapısı:** Sv5'te taş alt kat, Sv10'da 2. kat
- **Tüm binalara eklenenler:** taş temel (Sv5), fener (Sv10), altın süs (Sv15), sancak (Sv20)

## 6. Akademi yapı hakkı araştırmaları (Köy Gelişimi, 4. sütun)
| Araştırma | Sv | Ödül |
|---|---|---|
| Köylü Evi I | 1 | +1 köylü evi |
| Köylü Evi II | 3 | +1 köylü evi |
| Kule Hakkı I | 6 | +1 kule |
| Kule Hakkı II | 15 | +1 kule |

## 7. İlerleme
- **Kaynaklar:** Odun Sv1, Taş Sv5, Altın Sv15, Demir Sv25.
- **XP eğrisi:** seviye başına 100 + 85l + 18l² (Sv1→30 toplam ≈ 175 bin). Sv30'a 2 saatte bir giren oyuncu ≈ 93 gün.
- **Araştırma:** 7 ağaç, 478 araştırma (tam liste: `docs/akademi-arastirmalari.md`).
- **Savaş:** Zafer için köyün %75'i yıkılmalı (1 yıldız); %90'da 2, %100'de 3 yıldız. Rastgele savaşlarda her seviyede ≈ %50 zafer hedeflenir (bkz. `docs/battle-verify.txt`).
