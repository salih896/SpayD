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
