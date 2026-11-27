# OYUN TEKNOLOJİ VE ARAŞTIRMA AĞAÇLARI
## 7 Araştırma Ağacı – 30 Seviyelik Tasarım

### GENEL SİSTEM KURALLARI

Oyunda toplam 7 araştırma ağacı bulunacaktır:

1. Ekonomi
2. Savunma
3. Askerî
4. Mimarlık
5. Köy Gelişimi
6. Bilim
7. Element

Genel kurallar:

- Üniversite köy seviyesi 5'te açılır.
- Üniversitenin maksimum seviyesi 30'dur.
- Her ağaçta her seviyede 1 güçlü bonus araştırması; özel araştırmalar (haklar, askerler, elementler) farklı seviyelere dağılmıştır (kullanıcı kararı 25.11.2026: düşük değerli araştırmalar kaldırıldı).
- Hücre biçimi: `Ad [Etki:Değer]` — Etki koddaki ResearchStat adı, Değer ‰ (bonus) ya da adet/kimlik.
- Araştırmaların kendi kademe/rank seviyeleri ile üniversite seviyeleri birbirinden bağımsızdır.
- Araştırma ön koşulları sunucu tarafından doğrulanmalıdır.
- Araştırma kilitleri ağaç bazında bağımsız ilerler.
- Bir araştırma ağacındaki ilerleme diğer ağaçların ilerlemesini otomatik olarak engellemez.
- Ancak Element ağacındaki temel saldırı ve savunma kilitleri diğer ağaçlarda da geçerlidir.
- Araştırma maliyetleri, süreleri ve bonusları ayrıca dengelenecektir.

---

# 1. EKONOMİ AĞACI

Amaç: Kaynak üretimi, depolama ve ek kaynak yapıları (her kaynak yapısı en çok 2).

| Seviye | Araştırma 1 | Araştırma 4 |
|---|---|---|
| 1 | Verimli Üretim I [ProductionPct:40] | — |
| 2 | Geniş Depolar I [StoragePct:40] | — |
| 3 | Verimli Üretim II [ProductionPct:40] | — |
| 4 | Geniş Depolar II [StoragePct:40] | — |
| 5 | Verimli Üretim III [ProductionPct:40] | — |
| 6 | Geniş Depolar III [StoragePct:40] | — |
| 7 | Verimli Üretim IV [ProductionPct:40] | — |
| 8 | Geniş Depolar IV [StoragePct:40] | Odun Bölgesi II [WoodSlot:1] |
| 9 | Verimli Üretim V [ProductionPct:40] | — |
| 10 | Geniş Depolar V [StoragePct:40] | — |
| 11 | Verimli Üretim VI [ProductionPct:40] | — |
| 12 | Geniş Depolar VI [StoragePct:40] | — |
| 13 | Verimli Üretim VII [ProductionPct:40] | — |
| 14 | Geniş Depolar VII [StoragePct:40] | Taş Bölgesi II [StoneSlot:1] |
| 15 | Verimli Üretim VIII [ProductionPct:40] | — |
| 16 | Geniş Depolar VIII [StoragePct:40] | — |
| 17 | Verimli Üretim IX [ProductionPct:40] | — |
| 18 | Geniş Depolar IX [StoragePct:40] | — |
| 19 | Verimli Üretim X [ProductionPct:40] | — |
| 20 | Geniş Depolar X [StoragePct:40] | Altın Bölgesi II [GoldSlot:1] |
| 21 | Verimli Üretim XI [ProductionPct:40] | — |
| 22 | Geniş Depolar XI [StoragePct:40] | — |
| 23 | Verimli Üretim XII [ProductionPct:40] | — |
| 24 | Geniş Depolar XII [StoragePct:40] | — |
| 25 | Verimli Üretim XIII [ProductionPct:40] | — |
| 26 | Geniş Depolar XIII [StoragePct:40] | — |
| 27 | Verimli Üretim XIV [ProductionPct:40] | Demir Bölgesi II [IronSlot:1] |
| 28 | Geniş Depolar XIV [StoragePct:40] | — |
| 29 | Verimli Üretim XV [ProductionPct:40] | — |
| 30 | Geniş Depolar XV [StoragePct:40] | — |

- Bölge II araştırmaları: +1 üretim binası ve +1 depo hakkı (Orman + Kereste Deposu, Taş Ocağı + Taş Deposu, Altın Madeni + Banka, Demir Madeni + Demir Deposu). Her kaynak yapısı en çok 2.

# 2. SAVUNMA AĞACI

Amaç: Kule hasarı, kule canı, ek kule ve tuzak hakları.

| Seviye | Araştırma 1 | Araştırma 4 | Araştırma 5 |
|---|---|---|---|
| 1 | Keskin Oklar I [TowerDamage:25] | — | — |
| 2 | Sağlam Kuleler I [TowerHp:25] | — | — |
| 3 | Keskin Oklar II [TowerDamage:25] | Kule Hakkı I [TowerSlot:1] | — |
| 4 | Sağlam Kuleler II [TowerHp:25] | — | — |
| 5 | Keskin Oklar III [TowerDamage:25] | — | — |
| 6 | Sağlam Kuleler III [TowerHp:25] | Kule Hakkı II [TowerSlot:1] | — |
| 7 | Keskin Oklar IV [TowerDamage:25] | — | Tuzak Hakkı I [TrapSlot:1] |
| 8 | Sağlam Kuleler IV [TowerHp:25] | — | — |
| 9 | Keskin Oklar V [TowerDamage:25] | — | — |
| 10 | Sağlam Kuleler V [TowerHp:25] | Kule Hakkı III [TowerSlot:1] | — |
| 11 | Keskin Oklar VI [TowerDamage:25] | — | — |
| 12 | Sağlam Kuleler VI [TowerHp:25] | — | — |
| 13 | Keskin Oklar VII [TowerDamage:25] | — | — |
| 14 | Sağlam Kuleler VII [TowerHp:25] | — | Tuzak Hakkı II [TrapSlot:1] |
| 15 | Keskin Oklar VIII [TowerDamage:25] | Kule Hakkı IV [TowerSlot:1] | — |
| 16 | Sağlam Kuleler VIII [TowerHp:25] | — | — |
| 17 | Keskin Oklar IX [TowerDamage:25] | — | — |
| 18 | Sağlam Kuleler IX [TowerHp:25] | — | — |
| 19 | Keskin Oklar X [TowerDamage:25] | — | — |
| 20 | Sağlam Kuleler X [TowerHp:25] | Kule Hakkı V [TowerSlot:1] | — |
| 21 | Keskin Oklar XI [TowerDamage:25] | — | — |
| 22 | Sağlam Kuleler XI [TowerHp:25] | — | — |
| 23 | Keskin Oklar XII [TowerDamage:25] | — | — |
| 24 | Sağlam Kuleler XII [TowerHp:25] | — | — |
| 25 | Keskin Oklar XIII [TowerDamage:25] | — | — |
| 26 | Sağlam Kuleler XIII [TowerHp:25] | — | — |
| 27 | Keskin Oklar XIV [TowerDamage:25] | — | — |
| 28 | Sağlam Kuleler XIV [TowerHp:25] | — | — |
| 29 | Keskin Oklar XV [TowerDamage:25] | — | — |
| 30 | Sağlam Kuleler XV [TowerHp:25] | — | — |

- Kule hakkı: oyun başında 3; Kule Hakkı I-V her biri +1 → en çok 8. Her biri bir öncekini ister.
- Tuzak hakkı: oyun başında 4; Tuzak Hakkı I-II her biri +1 → en çok 6 (eski Tuzak Ustalığı araştırmasının yerine; kullanıcı kararı 27.11.2026).

# 3. ASKERÎ AĞACI

Amaç: Asker hasarı, asker canı ve yeni askerler.

| Seviye | Araştırma 1 | Araştırma 4 |
|---|---|---|
| 1 | Savaş Eğitimi I [SoldierDamage:25] | — |
| 2 | Dayanıklılık I [SoldierHp:25] | — |
| 3 | Savaş Eğitimi II [SoldierDamage:25] | — |
| 4 | Dayanıklılık II [SoldierHp:25] | Kalkanlı [SoldierUnlock:10] |
| 5 | Savaş Eğitimi III [SoldierDamage:25] | — |
| 6 | Dayanıklılık III [SoldierHp:25] | — |
| 7 | Savaş Eğitimi IV [SoldierDamage:25] | — |
| 8 | Dayanıklılık IV [SoldierHp:25] | — |
| 9 | Savaş Eğitimi V [SoldierDamage:25] | Suikastçı [SoldierUnlock:11] |
| 10 | Dayanıklılık V [SoldierHp:25] | — |
| 11 | Savaş Eğitimi VI [SoldierDamage:25] | — |
| 12 | Dayanıklılık VI [SoldierHp:25] | — |
| 13 | Savaş Eğitimi VII [SoldierDamage:25] | — |
| 14 | Dayanıklılık VII [SoldierHp:25] | Şifacı [SoldierUnlock:12] |
| 15 | Savaş Eğitimi VIII [SoldierDamage:25] | — |
| 16 | Dayanıklılık VIII [SoldierHp:25] | — |
| 17 | Savaş Eğitimi IX [SoldierDamage:25] | — |
| 18 | Dayanıklılık IX [SoldierHp:25] | — |
| 19 | Savaş Eğitimi X [SoldierDamage:25] | Arbaletli [SoldierUnlock:13] |
| 20 | Dayanıklılık X [SoldierHp:25] | — |
| 21 | Savaş Eğitimi XI [SoldierDamage:25] | — |
| 22 | Dayanıklılık XI [SoldierHp:25] | — |
| 23 | Savaş Eğitimi XII [SoldierDamage:25] | — |
| 24 | Dayanıklılık XII [SoldierHp:25] | Bombacı [SoldierUnlock:14] |
| 25 | Savaş Eğitimi XIII [SoldierDamage:25] | — |
| 26 | Dayanıklılık XIII [SoldierHp:25] | — |
| 27 | Savaş Eğitimi XIV [SoldierDamage:25] | — |
| 28 | Dayanıklılık XIV [SoldierHp:25] | — |
| 29 | Savaş Eğitimi XV [SoldierDamage:25] | — |
| 30 | Dayanıklılık XV [SoldierHp:25] | — |

- Yeni askerler (kışlada eğitilir): Kalkanlı (zırhlı, kule ateşini üstüne çeker), Suikastçı (çok hızlı, kulelere büyük hasar, az can), Şifacı (saldırmaz, yakındaki askerleri iyileştirir), Arbaletli (uzun menzil, kulelere bonus), Bombacı (yapıya ulaşınca patlar: büyük alan hasarı, kendisi ölür).

# 4. MİMARLIK AĞACI

Amaç: Bina canı, yükseltme maliyeti, otomatik tamir ve işçi iş sırası.

| Seviye | Araştırma 1 | Araştırma 4 |
|---|---|---|
| 1 | Güçlü Temeller I [BuildingHp:40] | — |
| 2 | Tasarruflu Yapım I [UpgradeCost:20] | — |
| 3 | Güçlü Temeller II [BuildingHp:40] | — |
| 4 | Tasarruflu Yapım II [UpgradeCost:20] | — |
| 5 | Güçlü Temeller III [BuildingHp:40] | — |
| 6 | Tasarruflu Yapım III [UpgradeCost:20] | — |
| 7 | Güçlü Temeller IV [BuildingHp:40] | Otomatik Tamir [AutoRepair:1] |
| 8 | Tasarruflu Yapım IV [UpgradeCost:20] | — |
| 9 | Güçlü Temeller V [BuildingHp:40] | — |
| 10 | Tasarruflu Yapım V [UpgradeCost:20] | — |
| 11 | Güçlü Temeller VI [BuildingHp:40] | İş Sırası I [JobQueue:1] |
| 12 | Tasarruflu Yapım VI [UpgradeCost:20] | — |
| 13 | Güçlü Temeller VII [BuildingHp:40] | — |
| 14 | Tasarruflu Yapım VII [UpgradeCost:20] | — |
| 15 | Güçlü Temeller VIII [BuildingHp:40] | — |
| 16 | Tasarruflu Yapım VIII [UpgradeCost:20] | — |
| 17 | Güçlü Temeller IX [BuildingHp:40] | — |
| 18 | Tasarruflu Yapım IX [UpgradeCost:20] | — |
| 19 | Güçlü Temeller X [BuildingHp:40] | — |
| 20 | Tasarruflu Yapım X [UpgradeCost:20] | — |
| 21 | Güçlü Temeller XI [BuildingHp:40] | — |
| 22 | Tasarruflu Yapım XI [UpgradeCost:20] | İş Sırası II [JobQueue:1] |
| 23 | Güçlü Temeller XII [BuildingHp:40] | — |
| 24 | Tasarruflu Yapım XII [UpgradeCost:20] | — |
| 25 | Güçlü Temeller XIII [BuildingHp:40] | — |
| 26 | Tasarruflu Yapım XIII [UpgradeCost:20] | — |
| 27 | Güçlü Temeller XIV [BuildingHp:40] | — |
| 28 | Tasarruflu Yapım XIV [UpgradeCost:20] | — |
| 29 | Güçlü Temeller XV [BuildingHp:40] | — |
| 30 | Tasarruflu Yapım XV [UpgradeCost:20] | — |

- Otomatik Tamir: savunma sonrası kulelerdeki kalıcı aşınma kendiliğinden (ücretsiz) onarılır.
- İş Sırası I-II: bir köylüye sırayla 2, sonra 3 iş verilebilir (ör. önce kule, sonra kışla, sonra Akademi); işler sırayla yapılır.

# 5. KÖY GELİŞİMİ AĞACI

Amaç: İnşa ve eğitim süresi, köylü evi, kışla, Akademi, yol hakları ve yol geliştirme.

| Seviye | Araştırma 1 | Araştırma 4 | Araştırma 5 | Araştırma 6 |
|---|---|---|---|---|
| 1 | Hızlı İnşaat I [BuildTime:25] | Köylü Evi I [VillagerHouseSlot:1] | — | — |
| 2 | Hızlı Eğitim I [TrainTime:25] | — | — | — |
| 3 | Hızlı İnşaat II [BuildTime:25] | Köylü Evi II [VillagerHouseSlot:1] | — | — |
| 4 | Hızlı Eğitim II [TrainTime:25] | — | — | — |
| 5 | Hızlı İnşaat III [BuildTime:25] | — | Yol Hakkı I [RoadSlot:2] | Yol Geliştirme I [RoadTier:1] |
| 6 | Hızlı Eğitim III [TrainTime:25] | — | — | — |
| 7 | Hızlı İnşaat IV [BuildTime:25] | — | — | — |
| 8 | Hızlı Eğitim IV [TrainTime:25] | Kışla II [BarracksSlot:1] | — | — |
| 9 | Hızlı İnşaat V [BuildTime:25] | — | — | — |
| 10 | Hızlı Eğitim V [TrainTime:25] | — | Yol Hakkı II [RoadSlot:2] | — |
| 11 | Hızlı İnşaat VI [BuildTime:25] | — | — | — |
| 12 | Hızlı Eğitim VI [TrainTime:25] | Akademi II [AcademySlot:1] | — | — |
| 13 | Hızlı İnşaat VII [BuildTime:25] | — | — | — |
| 14 | Hızlı Eğitim VII [TrainTime:25] | — | — | — |
| 15 | Hızlı İnşaat VIII [BuildTime:25] | — | — | Yol Geliştirme II [RoadTier:2] |
| 16 | Hızlı Eğitim VIII [TrainTime:25] | — | Yol Hakkı III [RoadSlot:2] | — |
| 17 | Hızlı İnşaat IX [BuildTime:25] | — | — | — |
| 18 | Hızlı Eğitim IX [TrainTime:25] | Kışla III [BarracksSlot:1] | — | — |
| 19 | Hızlı İnşaat X [BuildTime:25] | — | — | — |
| 20 | Hızlı Eğitim X [TrainTime:25] | — | — | — |
| 21 | Hızlı İnşaat XI [BuildTime:25] | — | — | — |
| 22 | Hızlı Eğitim XI [TrainTime:25] | — | — | — |
| 23 | Hızlı İnşaat XII [BuildTime:25] | — | — | — |
| 24 | Hızlı Eğitim XII [TrainTime:25] | — | Yol Hakkı IV [RoadSlot:2] | — |
| 25 | Hızlı İnşaat XIII [BuildTime:25] | — | — | Yol Geliştirme III [RoadTier:3] |
| 26 | Hızlı Eğitim XIII [TrainTime:25] | — | — | — |
| 27 | Hızlı İnşaat XIV [BuildTime:25] | — | — | — |
| 28 | Hızlı Eğitim XIV [TrainTime:25] | — | — | Yol Geliştirme IV [RoadTier:4] |
| 29 | Hızlı İnşaat XV [BuildTime:25] | — | — | — |
| 30 | Hızlı Eğitim XV [TrainTime:25] | — | — | — |

- Köylü evi: 1 başta + Köylü Evi I-II → 3. Kışla: 1. Sv3, + Kışla II-III → 3. Akademi: 1. Sv5, + Akademi II → 2 (2. Akademi +1 araştırma yuvası).
- Yol Hakkı I-IV: her biri +2 yol (toplam +8).
- Yol Geliştirme I-IV: yolları bir üst malzemeye geliştirme izni — I Taş (Sv5), II Altın+Tahta (Sv15), III Demir (Sv25), IV Kristal (Sv28); eski tek yol araştırmasının yerine (kullanıcı kararı 27.11.2026).

# 6. BİLİM AĞACI

Amaç: Araştırma süresi ve maliyeti; saldıran askerler ve kuleler için zırh ve ekipman.

| Seviye | Araştırma 1 | Araştırma 4 | Araştırma 5 |
|---|---|---|---|
| 1 | Hızlı Araştırma I [ResearchTime:20] | — | — |
| 2 | Ucuz Araştırma I [ResearchCost:15] | — | — |
| 3 | Hızlı Araştırma II [ResearchTime:20] | — | — |
| 4 | Ucuz Araştırma II [ResearchCost:15] | — | — |
| 5 | Hızlı Araştırma III [ResearchTime:20] | — | — |
| 6 | Ucuz Araştırma III [ResearchCost:15] | Asker Zırhı I [SoldierArmor:3] | — |
| 7 | Hızlı Araştırma IV [ResearchTime:20] | — | — |
| 8 | Ucuz Araştırma IV [ResearchCost:15] | — | — |
| 9 | Hızlı Araştırma V [ResearchTime:20] | — | Kule Zırhı I [TowerArmor:5] |
| 10 | Ucuz Araştırma V [ResearchCost:15] | — | — |
| 11 | Hızlı Araştırma VI [ResearchTime:20] | — | — |
| 12 | Ucuz Araştırma VI [ResearchCost:15] | — | — |
| 13 | Hızlı Araştırma VII [ResearchTime:20] | — | — |
| 14 | Ucuz Araştırma VII [ResearchCost:15] | — | — |
| 15 | Hızlı Araştırma VIII [ResearchTime:20] | Asker Zırhı II [SoldierArmor:3] | — |
| 16 | Ucuz Araştırma VIII [ResearchCost:15] | — | — |
| 17 | Hızlı Araştırma IX [ResearchTime:20] | — | — |
| 18 | Ucuz Araştırma IX [ResearchCost:15] | — | Kule Zırhı II [TowerArmor:5] |
| 19 | Hızlı Araştırma X [ResearchTime:20] | — | — |
| 20 | Ucuz Araştırma X [ResearchCost:15] | — | — |
| 21 | Hızlı Araştırma XI [ResearchTime:20] | — | — |
| 22 | Ucuz Araştırma XI [ResearchCost:15] | — | — |
| 23 | Hızlı Araştırma XII [ResearchTime:20] | — | — |
| 24 | Ucuz Araştırma XII [ResearchCost:15] | — | — |
| 25 | Hızlı Araştırma XIII [ResearchTime:20] | Asker Zırhı III [SoldierArmor:3] | — |
| 26 | Ucuz Araştırma XIII [ResearchCost:15] | — | — |
| 27 | Hızlı Araştırma XIV [ResearchTime:20] | — | — |
| 28 | Ucuz Araştırma XIV [ResearchCost:15] | — | Kule Zırhı III [TowerArmor:5] |
| 29 | Hızlı Araştırma XV [ResearchTime:20] | — | — |
| 30 | Paralel Araştırma [ResearchTime:20] | — | — |

- Asker Zırhı I-III: saldıran tüm askerlere +3 zırh (her biri). Kule Zırhı I-III: kuleler askerlerden %5 daha az hasar alır (her biri).
- Paralel Araştırma (Sv30): 2. araştırma yuvası.

# 7. ELEMENT AĞACI

Amaç: Kule saldırı ve savunma elementleri, asker saldırı ve savunma elementleri, element ustalığı.

| Seviye | Araştırma 1 | Araştırma 4 | Araştırma 5 | Araştırma 6 |
|---|---|---|---|---|
| 1 | Element Ustalığı 1 [ElementMastery:8] | Ok [TowerAttackElement:1] | — | — |
| 2 | Element Ustalığı 2 [ElementMastery:8] | — | — | — |
| 3 | Element Ustalığı 3 [ElementMastery:8] | Ateş [TowerAttackElement:2] | — | — |
| 4 | Element Ustalığı 4 [ElementMastery:8] | — | — | — |
| 5 | Element Ustalığı 5 [ElementMastery:8] | — | Ateş Kalkanı [TowerDefenseElement:2] | — |
| 6 | Element Ustalığı 6 [ElementMastery:8] | Buz [TowerAttackElement:3] | — | — |
| 7 | Element Ustalığı 7 [ElementMastery:8] | — | — | Ateş Silahları [SoldierAttackElement:2] |
| 8 | Element Ustalığı 8 [ElementMastery:8] | — | — | — |
| 9 | Element Ustalığı 9 [ElementMastery:8] | Zehir [TowerAttackElement:4] | — | — |
| 10 | Element Ustalığı 10 [ElementMastery:8] | — | — | Ateş Zırhı [SoldierDefenseElement:2] |
| 11 | Element Ustalığı 11 [ElementMastery:8] | — | Buz Kalkanı [TowerDefenseElement:3] | — |
| 12 | Element Ustalığı 12 [ElementMastery:8] | — | — | — |
| 13 | Element Ustalığı 13 [ElementMastery:8] | Büyü [TowerAttackElement:5] | — | — |
| 14 | Element Ustalığı 14 [ElementMastery:8] | — | — | Buz Silahları [SoldierAttackElement:3] |
| 15 | Element Ustalığı 15 [ElementMastery:8] | — | — | — |
| 16 | Element Ustalığı 16 [ElementMastery:8] | — | Zehir Kalkanı [TowerDefenseElement:4] | — |
| 17 | Element Ustalığı 17 [ElementMastery:8] | Şimşek [TowerAttackElement:6] | — | — |
| 18 | Element Ustalığı 18 [ElementMastery:8] | — | — | — |
| 19 | Element Ustalığı 19 [ElementMastery:8] | — | — | Buz Zırhı [SoldierDefenseElement:3] |
| 20 | Element Ustalığı 20 [ElementMastery:8] | — | — | Zehir Silahları [SoldierAttackElement:4] |
| 21 | Element Ustalığı 21 [ElementMastery:8] | — | — | — |
| 22 | Element Ustalığı 22 [ElementMastery:8] | — | — | — |
| 23 | Element Ustalığı 23 [ElementMastery:8] | — | — | — |
| 24 | Element Ustalığı 24 [ElementMastery:8] | — | — | — |
| 25 | Element Ustalığı 25 [ElementMastery:8] | — | — | Zehir Zırhı [SoldierDefenseElement:4] |
| 26 | Element Ustalığı 26 [ElementMastery:8] | — | — | — |
| 27 | Element Ustalığı 27 [ElementMastery:8] | — | — | — |
| 28 | Element Ustalığı 28 [ElementMastery:8] | — | — | — |
| 29 | Element Ustalığı 29 [ElementMastery:8] | — | — | — |
| 30 | Element Ustalığı 30 [ElementMastery:8] | — | — | — |

- Kule saldırı elementleri (Okçu Kulesine uygulanır): Ok, Ateş, Buz, Zehir, Büyü, Şimşek.
- Kule savunma elementleri ve asker saldırı/savunma elementleri: Ateş, Buz, Zehir.
- Döngü: Ateş > Zehir > Buz > Ateş. Zayıf elementle saldıran %40 az hasar verir (üstün olana ek hasar yok). Direnç: bir elemente karşı güçlendirilmiş (aynı savunma elementli) hedef o elementten %35 az hasar alır. Ok, Büyü, Şimşek nötrdür.
