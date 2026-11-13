# Açık Sorular (ilgili aşamada sorulacak)

## Aşama 3 (ekonomi)
- Banka yalnızca odunla mı kurulsun? (öneri: evet)
- Rehberdeki ilk kule ücretsiz mi? (öneri: evet)
- Seviye 5–6 aralığında XP aralığı/rehber adımı ("altın birikiyor, banka gelince kullanacaksın").

## Aşama 8 (rekabet)
- Rank'ta yıkılan savunan puan kaybeder mi? (öneri: saldırganın kazancının bir kısmı)
- Rank'ta saldırgan altın üretimi (enflasyon) için kaynak çıkışı (sink).
- Kayıp rank saldırgana koruma verilir mi? (varsayım: hayır)

## Teknik (GDD çelişki/riskleri)
- Savaşa hasarlı giren kule %51 hesabına nasıl girer? (öneri: yalnızca savaş içi hasar)
- Hangi yapılar genel hasar ortalamasına dahil?
- Bağlantı kopması: kopma sonrası yeni gönderim yok, sahadaki askerler savaşı bitirir (öneri).
- Geri çekilme %25 yuvarlama ve sunucu tohumlu RNG kaydı.
- Peri/uçan birlik rota kuralı.
- Savaş başına üst asker sınırı ve hücre yığılma sınırı.
- Bina/kule boyutları ile 11×11 alan bütçesi.
- GDD madde 29'daki 18 soru (ilgili aşamada).

## Yollar (Aşama 2-4, kullanıcıdan bekleyen)
- Başlangıç yol sayısı 7 mi 10 mu? Yol hakkı = toplam yol kapasitesi mi, yeni yerleştirilebilir parça mı? (şu an sınır YOK)
- Yol malzemesi geliştirme: maliyet, süre, araştırma şartı (şu an yalnızca görünüm; "Yol:" düğmesi serbestçe değiştirir). Altın kuralı: altın+tahta yol seviye 5+ içeriği, demir yol araştırmayla.
- Yol çıkmazlarına izin var mı? (şu an serbest; yalnızca giriş→bina bağlantısı zorunlu)
- Yol hücresi (+) menüsü: zemin özelliği/tuzak slotları.

## ÇÖZÜLDÜ (12.11.2026) — Yol soruları
Başlangıç 10 yol (sınır 10, hepsi zorunlu), tek giriş, hepsi girişe bağlı, dallanma serbest, çıkmaz serbest → bkz. approved-mechanics.md #16-21.
Hâlâ açık: giriş karesi 10'dan sayılıyor mu (varsayım: evet); malzeme geliştirme maliyeti/süresi/araştırması; yol hücresi (+) menüsü.

## AÇIK (12.11.2026 – yol geliştirme)
- Tek yol geliştirmesinin maliyeti/süresi ve Demir için ayrı araştırma gerekir mi?
- Yol hücresine dokununca açılacak (+) menü.

## AÇIK (12.11.2026 – kaynak sistemi)
- approved-mechanics #6-#11 (altın madeni Sv5, banka Sv6, altın yağma koruması) yeni açılma seviyeleriyle (Taş 5, Altın 15, Demir 25) çakışıyor; banka/madenler hangi seviyede, hangi bina?
- Yol araştırma maliyeti (şu an ÖRNEK: 5000 tahta, 1000 altın, 100 demir) Sv1'de ödenemez; yeniden belirlenmeli.
- Yol malzeme basamakları (Tahta / Altın+Tahta / Demir) yeni kaynaklarla nasıl eşleşecek?
- Kaynak ayarları JSON'a taşınsın mı (öneri: Core/Data/resources.json)?

## AÇIK (12.11.2026 – teknoloji/araştırma ağaçları, kaynak: GameDesign/TechTrees.md)
- "Kalkan" = "Normal koruma (3 saat)" mı?
- Araştırma maliyet/süre/bonus değerleri yok (belgede "ayrıca dengelenecek"): prototipte geçici değer kullanılacak.
- Tek araştırma yuvası Bilim ağacının 30. seviyesine kadar sürüyor; bu hedeflenen mi?
- (Belge 12.11.2026 tam sürüm) Belgenin kendi "onay bekleyen" listesi: bonus yüzdeleri, maliyetler, süreler, maksimum rank, üniversite seviyesiyle açılma, iptalde kaynak iadesi, üniversite yıkılınca davranış. Bunlar onaysız kesin kural yapılmayacak.
- Her araştırma tek rank mı (I-IV ayrı kayıtlar) yoksa çok rank mı? Prototip: tek rank, "I-IV" ayrı kayıt.
- Diğer ağaçlardaki Element ön koşulları (hangi araştırma hangi Element araştırmasına bağlı) için anahtar kelimeyle eşleme önerilecek; onay gerekir.

## AÇIK (12.11.2026 gece – kullanıcı cevaplarından sonra)
- Güncellenmiş TechTrees.md bekleniyor: yol geliştirme kaydı, yeniden adlandırmalar (Element Kontrolü, tekrar eden adlar), kalkan düzenlemesi bu oturuma ulaşmadı.
- Harita Genişletme: koşullu onay ("sistemi çok karıştırmazsa"). Taban 11, araştırmayla 15; uygulamada karmaşıklık raporlanacak. approved #15 bu yüzden henüz değişmedi.
- Ön koşul zinciri yorumu (ONAY LAZIM): her araştırma, aynı kanaldaki (sütundaki) bir önceki seviyenin araştırmasını gerektirir; kanal atlanamaz. 3. sütun (Sv5,10,15...) için kural belirsiz.

## AÇIK (13.11.2026 – oyun döngüsü tamamlama, kullanıcı: "bana sorma, hepsini sen ayarla")
Aşağıdakiler KULLANICI OYNAYARAK İNCELEYECEK; tamamı yer tutucu/varsayımdır:
- Element çapraz-ağaç kilitleri UYGULANMADI (sadece aynı sütun ön koşulu + 3. sütun için aynı seviyedeki 1. ve 2. sütun + önceki 3. sütun varsayımı).
- Yol malzeme basamağı yükseltme/araştırması istemciye bağlanmadı (TechTrees yol kaydı gelmedi); yapı onarımı anlık ve ücretsizdir.
- Tüm sayılar (XP eğrisi 100+12l+2l², seviye ölçeği 100+20l+2l², araştırma süre/maliyet, kaynak üretim/kapasite, hedef kule sayısı TowerScalePct=200, görev ödülleri, günlük baskın) docs/balance-report.txt ile ayarlanmalı.
- Seviye 30 test durumunda 16 adet Sv1 askerle Sv28 köye saldırı kesin yenilgi (beklenen): asker seviye yükseltmesi/ordu kapasitesi oynarken dengelenecek.
- Sunucu/hesap/sıralama/eşleştirme/lonca/mağaza dokunulmadı; yerel kayıt (save.txt) geçicidir.
- main'e birleştirme kullanıcı onayı bekler.
