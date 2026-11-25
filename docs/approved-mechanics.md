# Onaylı Kararlar (GDD'yi geçersiz kılar)

## Platform / ekip
1. Android + iOS, tek Unity projesi. Hesap/ilerleme platformlar arası (Sign in with Apple, hesap silme şartı).
2. Tek geliştirici Claude. Codex yok. Karar yetkisi kullanıcıda.

## Başlangıç ve rehber
3. Seviye 1: yalnızca orman + köylü.
4. Rehber oyuncuya kereste deposunu inşa ettirir; bitince seviye 2 GARANTİ verilir (XP formülünden bağımsız).
5. Seviye 2: ilk kule rehberle kurulur.

## Altın kuralı
6. Altın madeni seviye 5'te, banka seviye 6'da açılır.
7. Seviye 1–4: hiçbir şey altın istemez (yapı, geliştirme, tamir, özellik) → yalnızca odun.
8. Seviye 5'te açılan yeni özellik/geliştirme altın ister; seviye 5 öncesi içerik odunla kalır. Sonradan eklenen içerik bu ilkeye uyar.
9. Seviye ödüllerinde altın seviye 6'dan başlar.
10. Orman ve altın madeninin küçük dahili deposu vardır (depo/banka kurulana kadar yeter). Depo yokken üretim durmaz.
11. Banka kurulana kadar altın yağmalanamaz (madenin deposu dahil). Odun yağmalanabilir.

## Rank ve koruma
12. Rank seviye 6'dan başlar; havuz seviye 6+ oyuncular.
13. Rank koruması 12 saat: rank maçında yıkılan SAVUNANA verilir. Yalnızca rank saldırılarını engeller.
14. Normal koruma 3 saat (6'dan düşürüldü). İki koruma bağımsızdır. Değerler veri dosyasında.

## GDD'den değişen yerler
- Bölüm 7: başlangıç yapıları yalnızca orman + köylü. Kereste deposu, maden, banka sonradan açılır.
- Bölüm 18: koruma 3 saat. Bölüm 19: rank koruması savunana.
- Bölüm 3/26/28: Android → Android + iOS; Aşama 11'e App Store hazırlığı eklenecek.

## Prototip sonrası onaylar
15. Savunma haritası 11×11 olarak KALIR (prototip denendi, 11.11.2026). Boyut `VillageLayout` varsayılanıdır; değişirse tek yerden güncellenir.

## Yol ve giriş kuralları (12.11.2026, güncel)
16. Yol hakkı seviyeye bağlı: Sv1=10, Sv2=11, Sv3=12 (+1/seviye, Sv30=39). Savunmada yol sayısı TAM bu sayı olmalı; ne eksik ne fazla. Giriş karesi yol sayısından sayılır (onaylandı).
17. En az 1 giriş karesi zorunlu (alt kenar); birden fazla giriş konabilir (üst sınır 10, GDD); araştırma gerekmez, baştan açık (onaylandı). Her giriş bir binaya ulaşmalı.
18. Yol sayısı ne olursa olsun bütün yollar girişe bağlı olmak zorunda; kopuk yol parçası olamaz.
19. Dallanma ve çıkmaz yol serbest.
20. Saldırganın asker gönderme alanı 4×11, giriş kenarının dışındadır.
21. Seviye atlayınca yol hakkı artar; yeni yol konana kadar savunma geçersiz sayılır.
22. Yol geliştirme her seviyede açıktır (seviye şartı yok) ama önce "yol geliştirme" araştırması yapılmalı. Araştırmanın süresi, maliyeti ve şartı vardır (ÖRNEK: 1 gün; 5000 tahta, 1000 altın, 100 demir — kesin denge değil).
23. Araştırma bitince oyuncu yollara tek tek dokunup her yolu ayrı ayrı geliştirir (Tahta → Altın+Tahta → Demir). Her yol geliştirmesinin maliyeti/süresi AÇIK (open-questions.md).

## Kaynaklar (12.11.2026) — #6-#11'deki altın seviyelerini GEÇERSİZ KILAR (ayrıntı open-questions.md)
24. 4 kaynak: Odun (Sv1), Taş (Sv5), Altın (Sv15), Demir (Sv25). Yiyecek kaynağı YOKTUR.
25. Açılmamış kaynak üretilemez, harcanamaz, oyuncuya gösterilmez; ilgili bina/geliştirme erişilemez.
26. Açılma seviyeleri tek merkezde tutulur (Core/Economy/ResourceConfig). Üretim, süre, depo, harcama alanı, yağma kuralı kaynak başına ayarlanır.
27. Kaynak üretimi/harcaması sunucuda doğrulanır; istemci miktarı değiştiremez (sunucu Aşama 7'de kurulur, kurallar Core'da yazılır).

## Araştırma sistemi (12.11.2026) — kaynak: GameDesign/TechTrees.md
28. Araştırma maliyeti yalnızca o seviyede açık kaynaklarla ödenir (kilitli kaynak maliyette olamaz).
29. Üniversite seviyesi arttıkça o seviyedeki araştırmalar açılır: Üniversite Sv N ise en çok N. seviye araştırmalar yapılabilir (örn. köy 10, üniversite 8 → 9 ve 10. seviye araştırmalar kapalı).
30. Bir araştırma yapılabilmek için ağaçta tam kendinden önceki araştırma (aynı kanalda) tamamlanmış olmalı; kanal atlanamaz (kesin işleyiş açık-sorular'da).
31. Aynı adda iki araştırma olmaz. Ekonomi ağacının Sv25'inde 2 araştırma olması doğrudur.
32. Yol geliştirme araştırması araştırma ağacına eklendi (kayıt güncel belgeyle gelecek).

## İçerik ve görsel (14.11.2026, kullanıcı isteği)
33. 10 asker türü olacak (kullanıcı: "oyunda 10 çeşit savaşçı olacak"). GDD'deki 7'ye ek: Dev (Sv13), Mancınık (Sv17), Ejderha Binicisi (Sv21). Adlar/roller Claude önerisi; kullanıcı değiştirebilir.
34. ~~Kule hakkı seviyeyle 5→10~~ → #39 ile değişti (14.11.2026).
35. Saldırıda kullanılabilecek asker sayısı = ordu kapasitesi (20 + 1,2 × toplam kışla seviyesi; Sv30 ≈ 80-90). Değer dengeleme içindir.
36. Görsel yön: düşük poligonlu, sıcak renkli "köy havası" (kodla üretilen modeller; ileride gerçek modellerle değiştirilebilir).

## Yapı sınırları ve Akademi (14.11.2026, kullanıcı kararı — GDD Bölüm 9'u günceller)
37. Üretim binaları (Orman, Taş Ocağı, Altın Madeni, Demir Madeni) ve depolar (Kereste, Taş, Banka, Demir) 1'er adet. ~~Kışla 3~~ → #49, Akademi 1.
38. "Akademi" = eski Üniversite (araştırma binası). ~~Akademi görevleri yapı hakkı verir~~ → #88 (yapı hakları artık araştırma).
39. Köylü evi başta 1, ~~Akademi görevleriyle~~ Köylü Evi I-II araştırmalarıyla (#88) en çok 3. ~~Kule başta 5 (seviyeyle artmaz), 3 Akademi göreviyle en çok 8~~ → #48.
40. Yol malzeme geliştirme: Akademi'de "yol geliştirme" araştırması (RoadRules örnek: 1 gün; 5000 odun, 1000 taş), sonra yola dokunarak Tahta → Altın+Tahta (300 odun + 100 altın; altın Sv15'te açılır) → Demir (400 odun + 150 altın + 80 demir; demir Sv25). Maliyetler/süre GEÇİCİ; yol basamağının savaşa etkisi henüz yok (open-questions).
41. Görünüm seviyeyle gelişir: yapılar 5 kademe (Sv1-4/5-9/10-14/15-19/20+). Askerlerde HER geliştirme bir görsel ekler (Sv2..Sv30, birikimli: kemer → zırh → pelerin → aura → kıvılcım → hale → taç → ışık kanatları; liste `VillageArt.LevelVisualName`), rütbe adları Acemi → Eğitimli → Uzman → Zırhlı → Usta → Kahraman → Efsane (kullanıcı isteği 14.11.2026).
42. Kule türleri 7 (kullanıcı isteği 14.11.2026): Okçu, Büyücü, **Duvar** (eski Mızrak Kulesi; saldırmaz, can ≈ 3 kule, 2,5 hücredeki yer askerlerini üstüne çeker, uçanlar yok sayar), Elf, **Top** (Sv10, alan hasarı, hava yok), **Buz** (Sv12, yavaşlatır), **Şimşek** (Sv16, 2 ek hedefe zincir). Hepsi kule hakkından sayılır; görünüm 5 kademede gelişir. Değerler GEÇİCİ.
43. Asker yetenekleri (kullanıcı isteği 14.11.2026): her askere Sv5/10/20'de açılan 3 yetenek, savaşta otomatik (liste `Core/Combat/SoldierAbilities.cs`). Asker geliştirme süresi eğitim süresiyle orantılı. Kışla'da askere dokununca şimdiki/sonraki değerler, görsel ve yetenekler görünür.
44. Yol basamakları 5 (Tahta → Taş → Altın+Tahta → Demir → Kristal; #23'ü genişletir). Geliştirme işçi + süre ister (10 dk / 30 dk / 1 sa / 2 sa), seviye şartı Sv5/15/25/28. Savaş etkisi: düşman yavaşlaması %0/10/15/25/35, tuzak hasarı %100/100/110/150/200. Değerler GEÇİCİ.
45. Tuzaklar: 8 tür (Çivili Sv3, Kaygan Sv5, Ateşli Sv8, Zehirli Sv10, Buz Sv12, Patlayıcı Sv15 tek sefer, Kapan Sv18, Şimşek Sv22), ücretli, uçanları etkilemez. Tuzak hakkı 4; Akademi "Tuzak Ustalığı" (12 sa; 3000 odun, 1500 taş) ile 6. Değerler GEÇİCİ.
46. İlerleme hızı (kullanıcı kararı 14.11.2026): Sv30'a ≈ 100 gün (2 saatte bir giren aktif oyuncu; sim 101 gün, 4 saatte bir giren ≈ 146 gün). XP eğrisi 100 + 90l + 19l² (toplam ≈ 186 bin); hedef köy eğrileri buna göre yeniden ayarlandı. Geç oyun zorluğu (kullanıcı isteği): ordu kapasitesi 20 + 0,7×kışla seviyesi, hedeflere savunma araştırması Sv²/2 ‰, Normal Sv20+ +1 / Zor +2 (Sv20+ +3) kule seviyesi, baskıncılara saldırı araştırması Sv²/3 ‰. Sonuç (sim): 103 gün, saldırıların %79'u zafer, baskınların %46'sı savunulur.

## Köy Düzeni ve ordu (15.11.2026, kullanıcı kararı)
47. **Köy Düzeni modu:** ana ekranda "KÖY DÜZENİ" düğmesi. Yollar dahil her şey (yol hakları, binalar, kuleler, tuzaklar) envanterde görünür, sürükle-bırak ile yerleştirilir. Köy Düzeni'ne girmeden hiçbir yapı/yol/tuzak kurulamaz (mağaza ayrı ekran değil, Düzen'in Bina/Kule/Tuzak sekmeleri). "Onayla" ile değişiklikler köye tek seferde aktarılır, "İptal" hepsini atar. Yeni yapının bedeli onayda ödenir; satılan yapı onayda %50 iade (tuzak iadesiz, yol satılamaz). Yapılar seviyesi, aşınması ve süren yükseltmesiyle birlikte taşınır; geliştirilmekte olan yol taşınamaz.
48. **Kule hakkı (toplam):** 3 ile başlar; Sv6, Sv12, Sv20'de +1 (seviyeyle en çok 6); ~~Akademi araştırma görevleriyle +2~~ Kule Hakkı I-II araştırmalarıyla +2 (#88) → en çok 8. Kullanıcı seçimi: "Toplam sınır 3'ten başlar".
49. **Kışla:** 1 adet; Sv13'te 2., Sv26'da 3. kışla açılır.
50. **Asker yeri (yumuşak tablo, kullanıcı seçimi):** Kılıçlı/Okçu/Goblin 1, Mızraklı/Peri 2, Büyücü/Atlı 3, Dev 5, Mancınık 6, Ejderha Binicisi 10. Ordu kapasitesi yer birimidir: 20 + 4 × kışla seviyeleri toplamı (#35'i günceller).
51. **Kılıçlı** en yakın yapıya saldırır (tür ayrımı yok; Duvar'ın çekmesi yine önceliklidir).
52. **Yapıya dokununca açılır pencere:** Ordu ve Akademi düğmeleri kaldırıldı. Kışlaya dokununca asker eğitimi/geliştirme + kışla yükseltme penceresi; Akademi'ye dokununca araştırma penceresi; diğer yapılarda bilgi + yükseltme kartı; yola dokununca yol geliştirme.
53. **Görünürlük:** kaynaklar üstte küçük şeritte simgeleriyle (kütük, taş, altın sikke, demir külçe); yükseltilen yapının üstünde kalan süre; boştaki köylü yollarda dolaşır, çalışan köylü yükseltilen yapının yanında çekiç sallar.
54. **Düzen kuralları (onayda):** yollar birbirine (girişe) bağlı olmalı; binalar VE kuleler yola bağlı olmalı (8 komşudan biri yol; kullanıcı seçimi "Binalar ve kuleler"); envanterde yerleştirilmemiş öğe kalmamalı (tüm yol hakları kullanılır, #16-#21). Kural bozulursa ekranın ortasında "DÜZEN KABUL EDİLMEDİ" bildirimi çıkar; taslakta sorunlu hücreler kırmızı yanar.
55. Denge (sim, 15.11.2026): hedef köy gücü yeniden kalibre edildi (rastgele savaşlar toplam %48 zafer; 16/28 seviye %45-55 bandında, Sv3-10 basamaklı). İlerleme hızını korumak için XP eğrisi 100 + 75l + 15l² (Sv30 ≈ 93 gün, 2 saatte bir giren oyuncu). Değerler GEÇİCİ.
56. **Kule hasarı %30 düşürüldü** (kullanıcı kararı 15.11.2026: "kuleler çok güçlü"): Okçu 21, Büyücü 46, Elf 13, Top 98, Buz 14, Şimşek 39 (canlar aynı). %50 zafer hedefi bilerek bırakıldı (kullanıcı seçimi "Zafer oranı yükselsin"): rastgele savaşlarda toplam ≈ %54, bot oyuncu ≈ %73 kazanır. Sv30 ≈ 100 gün korunmak için XP eğrisi 100 + 80l + 17l² (sim 93 gün).
57. **Test saldırısı:** Ayarlar → "Kendi köyüne saldır (test)". Kurduğun savunmaya, açık asker türlerinden ordu kapasitesi kadar (kışla yoksa 40 yer) test ordusuyla saldırırsın; gerçek ordu harcanmaz, yağma/XP/görev ilerlemesi yoktur.
58. **İkinci denge turu (kullanıcı kararı 15.11.2026):** tuzak hasarı yarıya indi (Çivili 20, Ateşli 2/tick, Zehirli 1/tick, Patlayıcı 130, Kapan 30, Şimşek 45; Çivili artık Goblin'i tek basışta öldürmez). Kule hasarı %25 daha düştü (ilk değerlere göre ≈ %47): Okçu 16, Büyücü 35, Elf 10, Top 74, Buz 11, Şimşek 29.
59. **Zafer koşulu %75 yıkım** (GDD Bölüm 17'deki %51 yerine). Yıldız: %75 → 1, %90 → 2, %100 → 3.
60. **Kışla Sv2'de kurulabilir** (önce Sv3). Kılıçlı da Sv2'de açılır (kullanıcı: "Evet, Kılıçlı Sv2"); Okçu Sv3.
61. Sonuç (sim): rastgele savaşlar toplam ≈ %52 zafer; bot ≈ %68. Sv30 ≈ 93 gün için XP eğrisi 100 + 85l + 18l².
62. **Köy Düzeni yardımcıları (kullanıcı isteği 15.11.2026):** "Tümünü envantere al" düğmesi köydeki her şeyi (tuzak, yol, bina, kule; girişler kalkar) envantere alır, geliştirilmekte olan yollar yerinde kalır. Yanlış/eksik olan şeyler okla gösterilir: kurala uymayan hücrelerin (kopuk yol, yola bağlı olmayan yapı) ve giriş yoksa alt kenarın üstünde zıplayan kırmızı ok; envanterde yerleştirilmemiş öğe varsa Envanter sekmesinin üstünde "Yerleştir" oku.
63. **Asker hedefleri (kullanıcı kararı 15.11.2026, #51'i genişletir):** Kılıçlı, Okçu, Dev, Mızraklı, Peri, Mancınık, Ejderha Binicisi ve Büyücü en yakın yapıya saldırır; Goblin önce kaynak depolarına, sonra diğer yapılara; Atlı Kılıçlı önce kulelere, sonra diğer yapılara. Duvar'ın yakındaki yer askerlerini çekmesi önceliklidir; uçanlar Duvar'a yalnızca başka yapı kalmazsa saldırır (#42).
64. **"En yakın" = yol boyunca** (kullanıcı geri bildirimi 15.11.2026): yer askerleri hedefi kuş uçuşu değil, yapının yola değdiği kareye yürüme mesafesiyle seçer (yalnızca çaprazdan değiyorsa +1 adım; eşitlikte kuş uçuşu). Uçanlar kuş uçuşu. Test saldırısında askerleri oyuncu seçer (her tür, en çok 300 asker; kilitli türler testte serbest). Sv30 ≈ 100 gün için XP eğrisi 100 + 80l + 17l².
65. **Yol kullanım alanı sağ/sol/üst/alt** (kullanıcı kararı 15.11.2026): bina ve kule yalnızca yan karesinde yol varsa yola bağlıdır (çapraz sayılmaz; #54'ü günceller). Yakın dövüşçüler (menzil ≤ 2: Kılıçlı, Goblin, Atlı, Mızraklı, Dev) yapıya yalnızca yan karedeki yoldan saldırır, çaprazdan vuramaz. Uzaktan saldıranlar (Okçu, Büyücü, Mancınık) menzildeki, kendilerine kuş uçuşu en yakın yapıyı vurur; uçanlar yolu izlemez. Yapay hedef köyler de aynı kurala uyar. XP eğrisi 100 + 85l + 18l² (Sv30 ≈ 94 gün).
66. **Menziller (kullanıcı kararı 15.11.2026):** tüm kuleler 2 kare (Duvar saldırmaz); Okçu, Büyücü, Peri, Ejderha Binicisi 1 kare; Mancınık 2 kare (kullanıcı seçimi "Hepsi 1 kare", Mancınık kuşatma olarak 2). Okçu/Büyücü/Mancınık uzaktan saldıran sayılır (menzildeki en yakın yapı). Not: Okçu 'Kartal Gözü' (+%25) ve Mancınık 'Uzun Kol' (+%20) yetenekleri menzili biraz artırır (Mancınık 2,4 kare: 2 karelik kulelerin biraz dışından vurur). XP eğrisi 100 + 78l + 16l² (Sv30 ≈ 103 gün; bot ≈ %56 zafer).
67. **Menziller güncellendi (kullanıcı kararı 15.11.2026, #66'nın yerine):** menzilli askerler (Okçu, Büyücü, Peri, Ejderha Binicisi, Mancınık) 2 kare; kuleler türe göre: Okçu/Büyücü/Buz/Şimşek 2,5 kare, Elf ve Top 3 kare (Duvar saldırmaz). Yakın dövüşçüler değişmedi (1,5; Mızraklı 2). Yeteneklerle Okçu 2,5, Mancınık 2,4 kare olur; kuleler yine en az eşit menzilde. Sonuç (sim): bot ≈ %60 zafer, Sv30 ≈ 97 gün (XP eğrisi 100 + 78l + 16l² değişmedi).
68. **Hedef ve görünürlük (kullanıcı geri bildirimi 15.11.2026):** Okçu, Büyücü ve Mancınık da yol boyunca en yakın yapıya saldırır (menzile girdiği ilk yol karesine yürüme mesafesi; eşitlikte kuş uçuşu) — #65'teki "kuş uçuşu en yakın" kaldırıldı. Uçanlar kuş uçuşu. Savaşta saldıran her askerin başının üstünde can çubuğu görünür (yeşil > sarı > kırmızı).
69. **Asker çarpışması (kullanıcı kararı 15.11.2026):** yol karesi 3 şerit genişliğindedir; Dev ve Mancınık 2 şerit, diğer yer askerleri 1 şerit kaplar (yan yana 3 küçük asker ya da 1 Dev + 1 küçük asker). Kare doluysa asker arkada bekler; girişe yürürken askerler üst üste binmeden sıraya girer; uzun süre takılan asker dolu kareleri aşmadan ulaşabileceği bir hedef arar. Uçanlar çarpışmaz (görselde dağılırlar). Sonuç: saldırılar zorlaştı (rastgele savaşlar ≈ %43 zafer; kullanıcı seçimi "Böyle kalsın"). Sv30 ≈ 90 gün için XP eğrisi 100 + 65l + 13l².
70. **Çarpışma yumuşatıldı (kullanıcı kararı 15.11.2026, #69'un yerine):** askerler birbirini engellemez (takılma yok) ama üst üste de gelmez: her yol karesi 3 şerit, asker girdiği karede en boş şeride geçer (Dev/Mancınık 2 şerit), şeritler doluysa 0,3 kare geriye dizilir. Bu yalnızca duruştur; yürüyüş, menzil ve hedef etkilenmez. Sonuç (sim): bot ≈ %64 zafer, Sv30 ≈ 93 gün (XP eğrisi 100 + 78l + 16l²).
71. **Asker ayarı (kullanıcı kararı 15.11.2026):** Kılıçlı canı 120 → 80; hız: Mızraklı 65 → 40, Dev 55 → 30, Mancınık 45 → 30. Ekrandaki kaymalar için askerin şeridi ve sırası köye girerken bir kez seçilip sabit kalır (her karede yeniden seçilince yana/ileri-geri kayıyordu). Sonuç (sim): bot ≈ %58 zafer, Sv30 ≈ 102 gün.
72. **Kule hasarı 20** (kullanıcı kararı 15.11.2026): saldıran tüm kulelerin taban hasarı 20 (Okçu, Büyücü, Elf, Top, Buz, Şimşek; özellikler — alan, zincir, yavaşlatma, uçana bonus — aynı). Sonuç (sim): bot ≈ %73 zafer; Sv30 ≈ 91 gün için XP eğrisi 100 + 85l + 18l². Editörde Game penceresinin fare tekerleğiyle yakınlaşıp arayüzü kesmesi `Assets/Editor/GameViewScaleLock.cs` ile engellendi (yalnızca editör).
73. **Kule hasarı 30, saldırı hızı arttı** (kullanıcı kararı 15.11.2026): tüm saldıran kuleler 30 hasar; vuruş aralığı (tick, 10 = 1 sn): Okçu 8→6, Büyücü 20→14, Elf 4→3, Top 30→22, Buz 10→7, Şimşek 18→13 (≈ %35 daha hızlı). Arayüzde saldırı hızı görünür (kışla, test ordusu, kule kartı: "Saldırı 0,6 sn (1,7/sn)"). Sonuç (sim): rastgele savaşlar ≈ %35 zafer, bot ≈ %23; Sv30 ≈ 98 gün için XP eğrisi 100 + 65l + 13l².
74. **Kule hasarı 20, Kılıçlı canı 100** (kullanıcı kararı 15.11.2026; saldırı hızları #73'teki gibi kalır). Sonuç (sim): bot ≈ %55 zafer, Sv30 ≈ 99 gün (XP eğrisi 100 + 78l + 16l²).
75. **Kılıçlı canı 80** (kullanıcı kararı 15.11.2026, #74'teki 100'ün yerine). Sonuç (sim): bot ≈ %54 zafer, Sv30 ≈ 100 gün.
76. **Kılıçlı canı 70** (kullanıcı kararı 15.11.2026, #75'in yerine). Sonuç (sim): bot ≈ %51 zafer, Sv30 ≈ 107 gün.
77. **Kule hedefi (kullanıcı kararı 15.11.2026):** kule menzilindeki vurulabilir askerlerden önce yakın dövüşçüleri (Kılıçlı, Goblin, Atlı, Mızraklı, Dev) vurur; yakın dövüşçü yoksa uzaktan saldıranları/uçanları. Aynı grupta canı yüzde olarak en az olanı seçer (ör. Okçu yarım canda olsa da yakın dövüşçü varken önce o vurulur). Atış görseli de aynı hedefe gider. Sonuç (sim): bot ≈ %55 zafer, Sv30 ≈ 100 gün.
78. **Okçu Kulesi saldırı hızı 0,5 sn** (kullanıcı kararı 15.11.2026; 6 → 5 tick). Sonuç (sim): bot ≈ %52 zafer, Sv30 ≈ 107 gün.
79. **Asker ayarı (kullanıcı kararı 15.11.2026):** Okçu hasarı 10 → 17, Büyücü 22 → 43, Goblin 6 → 13 ve hızı 130 → 160. Sonuç (sim): bot ≈ %70 zafer; Sv30 ≈ 102 gün için XP eğrisi 100 + 85l + 18l².
80. **Savaş %40 yavaş (kullanıcı kararı 15.11.2026):** savaş simülasyonu ekranda saniyede 6 adım ilerler (önce 10). Yürüyüş, saldırı, kule atışı, tuzak ve yetenek süreleri birlikte yavaşlar; hasar dengesi ve simülasyon sonucu değişmez (sunucuyla aynı). Savaş süresi 180 savaş saniyesi = ekranda 5 dk. Arayüzdeki saldırı hızı ve savaş saati ekrandaki gerçek süreyi gösterir (ör. Okçu Kulesi 0,8 sn).
81. **Görsel yenileme (kullanıcı isteği 15.11.2026):** askerler savaş adımları arasında ara konumla akıcı ilerler ("nokta nokta" değil). İnsansı karakterler: kalçadan dönen bacaklar, omuzdan dönen kollar, çizme, etek, boyun, gözler, burun, saç; yürürken bacak/kol sallanır, saldırıda sağ kol vurur (CharacterRig); atın bacakları da adım atar. Harita: uzak karlı dağlar, gölet (taş kenar + saz), çimen tutamları, devrik kütük, mantar; köy içindeki boş karelerde çimen. Binaların önünde çalı ve çiçek. Yalnızca görsel; kurallar değişmez.
82. **Köşe ışınlanması giderildi (15.11.2026):** askerin şerit kayması artık yumuşatılmış yürüme yönüne göre hesaplanır (sağ/sol tarafı hep aynı kalır, köşede yön kavis çizerek döner); önceden kayma ekseni birden değişiyor ve taraf ters dönüyordu.
83. **Askerler tek tek hareket eder (kullanıcı geri bildirimi 15.11.2026):** her askerin yürüme temposu kendine özgüdür (±%6, kimliğe bağlı, deterministik); aynı noktaya basılı tutunca askerler ±0,35 kare dağınık doğar. Görselde her yer askeri yolun içinde kendine ait sabit bir sapmayla yürür (köşede blok halinde dönmez) ve birbirine 0,34 kareden fazla yaklaşan askerler yumuşakça ayrışır. Şerit/sıra dizilimi görselde kullanılmaz. Sonuç (sim): bot ≈ %69 zafer, Sv30 ≈ 104 gün.
84. **Asker aralığı sıkılaştırıldı (kullanıcı geri bildirimi 15.11.2026):** ayrışma mesafesi 0,34 → 0,26 kare, itme daha yumuşak ve çabuk söner (itişip titremesinler), kişisel sapma ±0,15 kare, doğuş dağılımı ±0,2 kare. Askerler iç içe girmez ama dağınık da durmaz.
85. **Hazır CC0 modeller (kullanıcı kararı 16.11.2026: ücretsiz paketler önce):** askerler, binalar ve kuleler hazır ücretsiz (CC0) modellerle çizilir: KayKit Adventurers + Character Animations (Kılıçlı=Knight, Okçu=Ranger, Büyücü=Mage, Goblin=Rogue (yeşil), Mızraklı=Rogue_Hooded, Dev=Barbarian, Peri=küçük Mage + kanat; bekleme/yürüme/saldırı animasyonlu, silah elde), Kenney Castle Kit (kuleler, sur, mancınık) ve Kenney Fantasy Town Kit (evler, kışla, akademi, banka, depolar, orman). Atlı Kılıçlı = Quaternius atı + şövalye, Ejderha Binicisi = Quaternius kızıl ejderhası + barbar (binek koşar/uçar, binici saldırır). Model bulunamazsa eski kodla çizilen görünüme döner. Can çubuğu model boyuna göre başın üstündedir. Lisanslar: `GameClient/Assets/Resources/ThirdParty/LICENSES.md`. Yalnızca görsel; kurallar değişmez.
86. **Savaş animasyonları (kullanıcı isteği 16.11.2026):** yürüme animasyonu askerin ekrandaki gerçek hızına uyar (ayak kayması yok; hız çarpanı ile de uyumlu), dönüşler kare hızından bağımsız yumuşak. Asker dururken saldırdığı yapıya döner. Yakın dövüşçüler vuruşları sırayla değiştirir (Kılıçlı 3, Goblin 3, Mızraklı 2, Dev 4 farklı vuruş) ve vuruş saldırı aralığına sığar; darbe anında yapıda kıvılcım çıkar. Menzilliler mermi atar: Okçu ok, Büyücü normal boy alev topu, Peri pembe büyü, Ejderha Binicisi büyük ateş topu (alev izli), Mancınık dönen taş; mermi animasyonun bırakma anında çıkar, isabette parlar. Yalnızca görsel; hasar ve sonuç değişmez.
87. **Goblin ve Mancınık görünümü (kullanıcı isteği 16.11.2026):** Goblin yeşil başlı, sivri büyük kulaklı, sarı gözlü, iri burunlu; kahverengi deri giysi, pelerinsiz, sırtında altın taşan yağma çuvalı; biraz daha küçük (0,48). Mancınık modeli ~%30 büyütüldü. Yalnızca görsel.
88. **Yapı hakları araştırma oldu (kullanıcı kararı 25.11.2026):** "Akademi görevleri" kaldırıldı. Yerine Köy Gelişimi ağacında 4. sütun: Köylü Evi I (Sv1, +1 köylü evi), Köylü Evi II (Sv3, +1), Kule Hakkı I (Sv6, +1 kule), Kule Hakkı II (Sv15, +1 kule). Diğer araştırmalar gibi kaynak ve süre harcar; sırayla açılır (her biri bir öncekini ister). Sınırlar aynı: köylü evi en çok 3, kule en çok 8. Eski kayıtlarda alınmış görevler aynı hakkı veren araştırmaya tamamlanmış olarak çevrilir. Toplam araştırma 474 → 478. Kullanıcı seçimi: "Ayrı 4 araştırma".
