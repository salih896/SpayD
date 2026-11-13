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
