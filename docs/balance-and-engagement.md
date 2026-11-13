# Denge ve Bağlılık Notları (ÖRNEK DEĞERLER – onay bekler)

Tüm sayılar `Core/Combat/ContentCatalog.cs` ve ilgili Rules sınıflarındaki yer tutuculardır; GDD içeriği (kule/asker/bina listeleri) dışına çıkılmamıştır.

## Uzun süre oynatan tasarım öğeleri (uygulandı)
- **Karşıtlık sistemi:** Mızrak kulesi↔Atlı, Elf kulesi↔Peri, Büyücü sıçraması↔kalabalık, Goblin↔kaynak avı. Tek tip ordu işe yaramaz; oyuncu kompozisyon düşünür.
- **Yıldız (0/1/2/3) ve XP:** %51 zafer, %75 2 yıldız, %100 3 yıldız; XP = 10 + hasar/4 + yıldız×15.
- **Seviye ile kilit açma:** asker/kule/bina/kaynak seviye kademeleriyle açılır (sürekli yeni hedef).
- **Kalıcı kule aşınması + tamir döngüsü:** saldırı sonrası kule hasarı (30/20/10/5/5, max %70) → tamir maliyeti/süresi; savunmacıyı köyüne geri çeker.
- **Yağma:** savunmacı kaynağı × %70 × depo hasarı; yalnız açık kaynaklar.
- **Geri çekilme riski:** sağlıklı askerin %25'i feda (tohumlu RNG).
- **Birim/kule seviye ölçeği:** 100+20l+2l² %.

## Sonraki adımlar (öneri)
Araştırma kataloğu JSON'u, üniversite kapısı, asker/kule yükseltme maliyetleri, günlük görev/ödül döngüsü (onay gerekir).

## Test durumu
Core: 87 EditMode testi geçti. İstemci prototipi: Sv9 örnek köy, saldırı, yıkım, XP/yağma sonucu, tamir Unity'de elle doğrulandı.
