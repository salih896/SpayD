# ANA OYUN TASARIM DOKÜMANI
## MASTER GDD – OYUN GELİŞTİRME PROJESİ

**Proje türü:** Mobil strateji, köy kurma, Tower Defense, asenkron PvP  
**Hedef platform:** Android  
**Görsel tarz:** İzometrik 2.5D  
**Oyun motoru:** Unity 6.3 LTS  
**Ana programlama dili:** C#  
**Geliştirme yöntemi:** Yapay zekâ destekli, aşamalı oyun geliştirme  
**Proje sahibi:** Kullanıcı  
**Ana geliştirme ajanı:** Codex  
**Çalışma modeli:** Yapay zekâ geliştirir, proje sahibi önemli aşamaları onaylar.

---

# 1. YAPAY ZEKÂYA VERİLEN ANA GÖREV

Sen bu projede tek başına çalışan sıradan bir kod yazma asistanı değilsin.

Profesyonel bir mobil oyun stüdyosunun aşağıdaki uzmanlıklarını üstleniyorsun:

- Oyun yönetmeni
- Kıdemli oyun tasarımcısı
- Unity geliştiricisi
- C# yazılım mimarı
- Backend geliştiricisi
- Veritabanı mimarı
- Oyun ekonomisi tasarımcısı
- Savaş ve yapay zekâ sistemleri geliştiricisi
- UI/UX tasarımcısı
- Grafik ve animasyon koordinatörü
- Test ve kalite güvence uzmanı
- Siber güvenlik uzmanı

Amacın, aşağıda açıklanan oyunu baştan sona geliştirmek, test etmek, hatalarını düzeltmek ve Android cihazlarda oynanabilir, güvenli, kaliteli bir mobil oyuna dönüştürmektir.

## Çalışma kuralları

1. Kullanıcının onayladığı oyun mekaniklerini izinsiz değiştirme.
2. Yeni özellik eklemeden önce mevcut sistemlerle uyumluluğunu incele.
3. Eksik kararları kendin kesinleştirme; önce öneri hazırla ve kullanıcıdan onay al.
4. Teknik uygulama ayrıntılarını kullanıcıya gereksiz yere yükleme.
5. Kodları modüler, okunabilir ve sürdürülebilir yaz.
6. Her sistemi bağımsız test edilebilir şekilde geliştir.
7. Her geliştirme aşamasında test raporu oluştur.
8. Hata bulduğunda yalnızca rapor verme; mümkünse hatayı düzelt ve tekrar test et.
9. Çalışan sistemleri gereksiz yere yeniden yazma.
10. Önemli değişikliklerden önce mevcut sürümü yedekle.
11. Gerçek oyuncu verilerini silme, ücretli servis satın alma veya oyunu yayınlama gibi işlemler için kullanıcı onayı al.
12. Onaylanmış oyun tasarımını ana GDD dosyasında güncel tut.

## Kullanıcı ile çalışma şekli

Kullanıcı kodlama yapmak istemiyor.

Kod yazma, proje dosyalarını düzenleme, test çalıştırma, teknik sorunları çözme ve gerekli dosyaları oluşturma görevlerini sen üstleneceksin.

Kullanıcıya yalnızca:
- Tasarım kararları,
- Görsel tercihler,
- Oynanış değerlendirmeleri,
- Önemli geliştirme onayları,
- Yayınlama kararları

sunulacak.

Her geliştirme aşamasının sonunda şu formatta rapor hazırla:

**Tamamlanan işler**
- Yapılan geliştirmeler.

**Test sonuçları**
- Başarılı testler.
- Başarısız testler.
- Düzeltilen hatalar.

**Görsel sonuç**
- Oyunun mevcut görünümü.
- Oynanabilir özellikler.

**Eksik işler**
- Henüz tamamlanmayan özellikler.

**Kullanıcıdan beklenen**
- Yalnızca gerçekten gerekli karar ve onaylar.

Kullanıcının onayını bekleyen bir konu varsa, o konuya bağlı olmayan geliştirmelere devam edebilirsin.

---

# 2. OYUNUN GENEL KONSEPTİ

Oyun, köy kurma ve Tower Defense mekaniklerini asenkron çevrim içi PvP ile birleştiren bir mobil strateji oyunudur.

Oyuncular kendi köylerini kurar, yollarını tasarlar, savunma kulelerini yerleştirir, kaynak üretir, asker yetiştirir, araştırmalar yapar ve diğer oyuncuların köylerine saldırır.

Oyunun temel döngüsü:

1. Köyünü kur.
2. Kaynak üret.
3. Binalarını geliştir.
4. Savunma yollarını tasarla.
5. Kulelerini yerleştir.
6. Asker üret.
7. Diğer oyunculara saldır.
8. Kaynak ve ödül kazan.
9. Yeni özellikler aç.
10. Rank ve derece sisteminde yüksel.
11. Lonca kur veya bir loncaya katıl.
12. Daha güçlü köyler ve savunmalar oluştur.

Oyun sürekli gelişen bir içerik yapısına sahip olacak.

Başlangıçta oyuncu seviye sınırı 30 olacak. Ancak ileride yeni seviyeler, kuleler, askerler, binalar, araştırmalar ve etkinlikler eklenebilecek.

Oyun 30. seviyede tamamlanmış veya bitmiş hissettirmemeli.

---

# 3. PLATFORM VE GÖRSEL TASARIM

## Kesin kararlar

- Platform: Android.
- Oyun motoru: Unity 6.3 LTS.
- Programlama dili: C#.
- Görsel tarz: İzometrik 2.5D.
- Oyun görünümü: Tepeden, açılı izometrik kamera.
- Harita: Grid tabanlı.
- Kontroller: Mobil dokunmatik ekran.
- Bütçe yaklaşımı: Kalite öncelikli.

## Görsel kalite hedefleri

Oyun profesyonel mobil strateji oyunları seviyesinde görünmelidir.

İstenen görsel özellikler:

- Kaliteli izometrik bina modelleri.
- Detaylı kule tasarımları.
- Farklı asker karakterleri.
- Akıcı asker animasyonları.
- Saldırı ve savunma efektleri.
- Patlama, büyü ve tuzak animasyonları.
- Kaynak üretim animasyonları.
- Bina yükseltme animasyonları.
- Gelişim seviyelerine göre değişen görünümler.
- Akıcı kamera hareketleri.
- Mobil ekrana uygun kullanıcı arayüzü.

İlk aşamada basit prototip görseller kullanılabilir. Ancak prototip görselleri nihai grafik olarak kabul edilmemelidir.

Görsel kalite aşamalı olarak yükseltilmelidir.

---

# 4. KÖY HARİTASI VE GRID SİSTEMİ

## Savunma haritası

KESİN KARAR:

Savunmacının köy haritası 11×11 kareden oluşacak.

Toplam:
121 grid hücresi.

Harita üzerinde:
- Binalar,
- Kuleler,
- Yollar,
- Tuzaklar,
- Kaynak yapıları

bulunacak.

Bina ve kulelerin boyutları ileride farklı olabilir. Bütün nesneler grid sistemine uygun yerleştirilmelidir.

İzometrik görünüm ile gerçek grid koordinatları birbirinden ayrılmalıdır.

Yani görsel olarak açılı görünen harita, teknik olarak düzenli grid sistemiyle çalışmalıdır.

## Saldırı alanı

Saldırganın asker gönderme alanı 4×11 olacak.

Toplam:
44 gönderim hücresi.

Oyuncu askerlerini bu hücrelerden gönderir.

---

# 5. GİRİŞ VE YOL SİSTEMİ

## Giriş sistemi

KESİN KARAR:

Savunmacı yalnızca haritanın alt kenarından asker giriş noktaları oluşturabilir.

- Kuzey kenarından giriş olmayacak.
- Doğu kenarından giriş olmayacak.
- Batı kenarından giriş olmayacak.
- Yalnızca güney/alt kenar girişleri kullanılacak.

Alt kenarda 11 olası giriş konumu bulunacak.

Başlangıçta bir veya daha fazla giriş aktif olacak. Araştırma ve gelişim yoluyla aktif giriş sayısı 1 ile 10 arasında artırılabilecek.

Kesin açılma seviyeleri henüz belirlenmedi.

## Yol sistemi

Yollar grid hücrelerinden oluşacak.

Oyuncu yolları tek tek yerleştirecek.

Yol biçimleri:
- Düz.
- L.
- T.
- U.
- Zikzak.
- Dallanan yol ağları.

Oyuncu kendi savunma yolunu tasarlayabilecek.

Binalar yol hücrelerine yerleştirilemeyecek.

Kuleler yol çevresindeki uygun hücrelere yerleştirilecek.

Her aktif girişten en az bir yağmalanabilir kaynak binasına geçerli yol bulunmalıdır.

Yol sistemi bağlantı kontrolü yapmalıdır.

Bir girişten kaynak binasına ulaşan geçerli yol bulunmuyorsa savunma düzeni kaydedilmemelidir.

## Yol hareket algoritması

Askerler yol ağı üzerinde hedeflerine ulaşmak için en kısa geçerli rotayı kullanacak.

Askerler tuzaklardan kaçınmak için alternatif rota aramayacak.

Birden fazla eşit uzunlukta yol varsa deterministik bir seçim algoritması kullanılacak.

Aynı savaşta aynı koşullarda aynı rota seçilmelidir.

Saldırı alanından seçilen girişe ulaşma algoritması ayrıca geliştirilecek.

## Yol geliştirme

Yolların farklı geliştirme seviyeleri olacak.

Planlanan malzeme aşamaları:

1. Tahta yol.
2. Altın ve tahta geliştirmeli yol.
3. Demir yol.

Demir yol için araştırma gerekecek.

Yol geliştirildikçe görünümü değişecek.

## Yol hakkı

KESİN KARAR:

Oyuncu her seviye atladığında +1 yeni yol hakkı kazanacak.

Örnek:
- 2. seviyede +1.
- 3. seviyede +1.
- 4. seviyede +1.
- Bu sistem 30. seviyeye kadar devam edecek.

30. seviyeye ulaşana kadar başlangıç hakkına ek olarak 29 yol hakkı kazanılacak.

Yol hakkının teknik olarak toplam yol kapasitesine mi yoksa kullanılabilir yeni yol parçasına mı karşılık geleceği henüz netleştirilmedi.

Bu konu kullanıcıya sorulmalıdır.

## Yol hücrelerine özellik ekleme

Yol hücrelerinin üzerinde bir geliştirme menüsü (+) bulunacak.

Bu menüden zemin özelliği veya tuzak eklenebilecek.

Önerilen yapı:
- Bir ana zemin özelliği.
- Bir ek tuzak özelliği.

İleri araştırmalarla ilave slot açılması değerlendirilebilir.

Aynı karede birbiriyle çelişen zemin özellikleri bulunamaz.

---

# 6. TUZAK SİSTEMİ

Tuzaklar yol hücrelerine yerleştirilecek.

Tuzaklar araştırma ile açılacak.

Planlanan tuzak türleri:

## Doğa tuzakları
- Sarmaşıklı.
- Dikenli sarmaşıklı.
- Zehirli sarmaşıklı.
- Köklü.

## Zemin tuzakları
- Islak zemin.
- Kaygan zemin.
- Buzlu zemin.
- Kumlu zemin.
- Çamurlu zemin.

## Ateş ve sıcaklık tuzakları
- Sıcak zemin.
- Ateşli zemin.
- Lavlı zemin.

## Fiziksel tuzaklar
- Çivili zemin.
- Dikenli zemin.
- Telli zemin.
- Dikenli telli zemin.
- Gizli çukur.

Tuzakların etkileri askerlerin türüne, zırhına, direncine ve özelliklerine göre değişmelidir.

Örneğin:
- Zırhlı asker dikenli zeminden daha az etkilenebilir.
- Uçan askerler yerdeki bazı tuzaklardan etkilenmeyebilir.
- Ateş direnci olan askerler ateş tuzaklarından daha az hasar alabilir.

Kesin hasar, süre ve direnç değerleri oyun dengesi aşamasında belirlenecek.

---

# 7. BAŞLANGIÇ BİNALARI VE EKONOMİ

## Başlangıçta bulunan yapılar

KESİN KARAR:

Oyuncu başlangıçta şu yapılara sahip olacak:

1. Bir köylü.
2. Bir orman.
3. Bir kereste deposu.
4. Bir altın madeni.
5. Bir banka.

Kereste fabrikası ve kereste deposu iki ayrı bina değildir.

Tek bina olarak kullanılacak.

Orman odun üretecek.

Kereste deposu odunu depolayacak.

Altın madeni altın üretecek.

Banka altını depolayacak.

## Kaynaklar

Başlangıç kaynakları:

- Odun.
- Altın.

İleri aşamalarda:
- Demir.
- Yeni üretim kaynakları.

eklenebilir.

## Üretim

Kaynak üretimi oyuncu çevrim dışıyken de devam edecek.

Üretim 24 saate kadar birikecek.

Üretim hızları bina seviyesine göre değişebilecek.

Üretim sistemi sunucu tarafından doğrulanmalıdır.

## Bina geliştirme

Binaların seviyeleri olacak.

Seviye yükseldikçe:
- Üretim kapasitesi.
- Depolama kapasitesi.
- Üretim hızı.
- Görsel kalite.

değişebilecek.

## Başlangıç binaları için önemli kural

Kulelerin dışında köy binaları için kulelerdeki gibi kalıcı hasar ve tamir sistemi uygulanmayacak.

Köy binalarının saldırı sırasında hasar alıp almayacağı, yıkılabilirliği ve kaynak yağmalama davranışı ayrıca tasarlanmalıdır.

Bu konu henüz kesinleştirilmemiştir.

---

# 8. KÖYLÜ VE İNŞAAT SİSTEMİ

Köylüler kaynak üretmez.

Köylülerin temel görevi:
- Bina inşa etmek.
- Bina geliştirmek.
- Yol geliştirmek.
- Savunma yapılarını geliştirmek.
- Araştırma ve inşaat işlemlerinde görev almak.

Başlangıçta bir köylü bulunacak.

Köylü evi yükseltildikçe köylü kapasitesi artırılabilecek.

## Köylü çalışma sistemi

Bir köylü aynı anda yalnızca bir işlem yapabilir.

Birden fazla köylü farklı işlemlerde görev alabilir.

Bütün köylüler ortak seviyede olacak.

Kişisel XP veya farklı uzmanlık sistemi bulunmayacak.

Önerilen gelişim seviyeleri:
- Acemi.
- Deneyimli.
- Usta.
- Uzman.
- Baş mimar.

Bu isimler ve gelişim etkileri henüz onaylanmamıştır.

---

# 9. SAVUNMA KULELERİ

## Başlangıç kule kapasitesi

KESİN KARAR:

Oyuncu başlangıçta 5 kule seçebilecek.

İlerleyen seviyelerde kule kapasitesi 10'a kadar yükseltilebilecek.

Oyuncu açılmış kule türleri arasından istediğini seçebilecek.

Aynı türden birden fazla kule kullanmak serbest olacak.

Örnek:
- 3 büyücü + 2 okçu.
- 5 okçu.
- 2 mızrak + 2 büyücü + 1 elf.

## Başlangıç kule türleri

- Okçu kulesi.
- Büyücü kulesi.
- Mızrak kulesi.
- Elf kulesi.

Yeni kuleler ileride eklenebilecek.

## Yeni kuleler için genişletilebilir tasarım

İleride değerlendirilebilecek kule türleri:
- Buz kulesi.
- Top kulesi.
- Zehir kulesi.
- Şimşek kulesi.

Bunlar öneridir; kesin oyun içeriği olarak kabul edilmemelidir.

Yeni kule türleri kolayca eklenebilecek modüler bir sistem kurulmalıdır.

Her kule türü farklı:
- Hasar.
- Menzil.
- Saldırı hızı.
- Özel yetenek.
- Hedef önceliği.
- Görsel efekt.

özelliklerine sahip olmalıdır.

## Kule geliştirme

Her kulenin geliştirme menüsü (+) olacak.

Kule seviyeleri:
- Hasar.
- Menzil.
- Saldırı hızı.
- Kritik vuruş.
- Özel yetenek.

gibi özellikleri etkileyebilir.

Kesin statlar ve geliştirme maliyetleri daha sonra belirlenecek.

---

# 10. KULE HASAR VE TAMİR SİSTEMİ

KESİN KARAR:

Kalıcı hasar ve tamir sistemi yalnızca savunma kulelerinde bulunacak.

Köy binalarında bu sistem uygulanmayacak.

## Kalıcı hasar sınırı

Bir kuleye saldırı sırasında verilen gerçek hasar ile savaş sonrası kalıcı olarak kalan hasar farklı olabilir.

Kalıcı hasar sınırları:

| Saldırı sırası | Ek kalıcı hasar sınırı |
|---|---:|
| 1. savaş | %30 |
| 2. savaş | %20 |
| 3. savaş | %10 |
| 4. savaş | %5 |
| 5. savaş | %5 |
| Toplam | %70 |

Kule en fazla %70 kalıcı hasara ulaşabilir.

Bu sınıra ulaşan kule çalışamaz ve saldırı yapamaz.

Tamir edilene kadar devre dışı kalır.

## Tamir sistemi

Tamir için:
- Odun.
- Altın.
- Zaman.

gerekecek.

Tamir sırasında kule saldırı yapamaz.

Tamir tamamlandığında kule tekrar çalışır.

Tamir süresi kule seviyesine göre değişecek.

Her %5 hasar bir tamir birimi olarak kabul edilecek.

Örnek süre taslağı:

| Kule seviyesi | Her %5 hasar için |
|---|---:|
| Seviye 1 | 0,5 dakika |
| Seviye 2 | 1 dakika |
| Seviye 3 | 3 dakika |

Örnek:
Seviye 1 bir kule %70 hasarlıysa:

70 / 5 = 14 birim.

14 × 0,5 dakika = 7 dakika.

Bu süreler dengeleme aşamasında değiştirilebilir.

---

# 11. SALDIRI ASKERLERİ

## Başlangıç asker türleri

- Kılıçlı.
- Okçu.
- Büyücü.
- Goblin.
- Atlı kılıçlı.
- Mızraklı.
- Peri.

İleride yeni askerler eklenebilecek.

## Asker gelişimi

Her asker türünün ayrı gelişim ağacı olacak.

Örnek kılıçlı gelişimi:
1. Kılıçlı.
2. Eğitimli kılıçlı.
3. Uzman kılıçlı.
4. Zırhlı kılıçlı.
5. Gladyatör.

Bu yapı diğer asker türlerine de uygulanacak.

Gelişimle:
- Görünüm.
- Hasar.
- Can.
- Hız.
- Zırh.
- Direnç.
- Özel yetenek.

değişebilir.

Asker gelişim sistemi bireysel XP yerine tür bazlı/global geliştirme mantığına göre tasarlanacak. Bu sistemin kesin uygulama ayrıntısı kullanıcıyla doğrulanmalıdır.

---

# 12. ASKER ÜRETİM SİSTEMİ

Askerler saldırıdan önce kışlada üretilecek.

Üretilen askerler envantere eklenecek.

Saldırıya gönderilen askerler envanterden düşecek.

## Üretim kuyruğu

KESİN KARAR:

Dört ayrı üretim yuvası yerine tek bir ortak üretim kuyruğu olacak.

Oyuncu farklı asker türlerini istediği sırada kuyruğa ekleyebilecek.

Örnek:

Kılıçlı → Okçu → Büyücü → Kılıçlı.

Üretim sırayla devam edecek.

Tamamlanan asker otomatik envantere eklenecek.

Kuyruk sürükle-bırak yöntemiyle yeniden sıralanabilecek.

Bekleyen askerler kuyruktan silinebilecek.

İptal edilen askerlerin kaynak iadesi henüz belirlenmedi.

Birden fazla aynı tür asker kuyruğa eklenebilecek.

## Üretim süreleri

Üretim süreleri 2, 3, 5 ve 10 dakika kategorilerinde olacak.

En uzun başlangıç üretim süresi 10 dakika olarak planlanıyor.

Asker türlerine göre kesin süreler daha sonra belirlenecek.

---

# 13. OTOMATİK SAVAŞ VE ASKER YAPAY ZEKÂSI

Saldırgan oyuncu askerlerini doğrudan kontrol etmeyecek.

Oyuncu:
- Asker türünü seçer.
- Askeri göndereceği kareyi seçer.
- İstediği zaman asker gönderir.
- Gerekirse geri çekilme komutu verir.

Askerler otomatik hareket eder ve otomatik saldırır.

## Asker hedef öncelikleri

Başlangıç hedef sistemi:

| Asker | Öncelik |
|---|---|
| Goblin | En yakın kaynak binası |
| Kılıçlı | En yakın kule veya kaynak |
| Atlı kılıçlı | En yakın kule |
| Okçu | Uzaktan savunma hedefleri |
| Büyücü | Alan saldırısı ve uygun hedef grubu |
| Mızraklı | Atlı birlikler |
| Peri | Uçan birlik özelliği; kaynak ve kule hedefleri |

Hedef öncelikleri geliştirilebilir.

## Hedef kilitleme

Asker hedefini seçtikten sonra sürekli hedef değiştirmeyecek.

Hedef yok edilirse yeni hedef seçecek.

Askerlerin davranışı deterministik ve test edilebilir olmalıdır.

## Normal köy savunmasında asker savaşı

KESİN KARAR:

Normal köy savunmasında köy sahibinin askerleri saldıran askerlerle savaşmayacak.

Savunmayı yalnızca kuleler gerçekleştirecek.

Köy sahibinin askerleri bulunması normal saldırıda savaş davranışını değiştirmeyecek.

Lonca savaşları veya gelecekteki farklı oyun modlarında savunma askerleri ayrıca değerlendirilebilir.

---

# 14. ASKER GÖNDERME KONTROLLERİ

Saldırganın 4×11 gönderim alanı bulunacak.

44 hücrenin tamamı kullanılabilecek.

Oyuncu asker türünü seçmek için ekrandaki asker ikonlarına dokunacak.

## Tek dokunuş

Oyuncu seçili asker türünü bir kareye gönderir.

## Basılı tutma

Oyuncu aynı kareye basılı tuttuğunda seçili türden askerler seri olarak gönderilir.

Parmak kaldırıldığında gönderim durur.

Seçili asker türünün stoğu biterse gönderim durur.

## Birden fazla asker

Aynı hücreye birden fazla asker gönderilebilir.

Askerler görsel olarak üst üste gelebilir.

Birbirlerinin hareketini engellemezler.

Her asker bağımsız bir oyun birimidir.

Asker gönderildiği anda hareket etmeye ve savaşa başlamalıdır.

Hazırlık fazı olmayacak.

---

# 15. ASKER KAYIPLARI VE GERİ ÇEKİLME

Savaş sonrası:
- Sağlıklı askerler envantere döner.
- Ölen askerler kalıcı kayıp sayılır.
- Yaralı askerler de kayıp sayılır ve tekrar kullanılamaz.

Yaralı sayılma can eşiği henüz belirlenmedi.

## Geri çekilme

KESİN KARAR:

Oyuncu geri çekilme komutu verdiğinde hayatta ve sağlıklı kalan gönderilmiş askerlerin %25'i rastgele seçilerek kaybedilir.

Ölü ve yaralı askerler zaten kayıptır.

Kalan sağlıklı askerler envantere geri döner.

Envanterde bekleyen ve savaşa gönderilmeyen askerler etkilenmez.

Örnek:

20 asker gönderildi.

- 2 öldü.
- 2 yaralandı.
- 16 sağlıklı kaldı.

Geri çekilme:

16 × %25 = 4 asker kaybedilir.

12 sağlıklı asker envantere döner.

Kayıp raporunda bu askerler için:

"Kendini feda etti."

gibi tematik ifadeler kullanılabilir.

Rastgele seçim sunucu tarafında yapılmalıdır.

---

# 16. SAVAŞ SİMÜLASYONU VE ASENKRON PVP

Oyun asenkron çevrim içi PvP sistemine sahip olacak.

Saldırılan oyuncunun çevrim içi olması gerekmeyecek.

Savunma düzeni sunucuda saklanacak.

Saldırı başlatıldığında savunma düzeninin değişmez bir kopyası alınacak.

Bu kopya savaş süresince kullanılacak.

Saldırı sırasında savunmacının köyünde yaptığı değişiklikler devam eden savaşı etkilemeyecek.

## Sunucu otoritesi

Savaşın gerçek sonucu sunucuda hesaplanacak.

İstemci yalnızca komut ve görüntüleme görevini yapacak.

Sunucu:
- Asker gönderimini doğrular.
- Asker envanterini kontrol eder.
- Savaş hareketlerini hesaplar.
- Kule hasarını hesaplar.
- Kaynak hasarını hesaplar.
- Zafer veya yenilgiyi belirler.
- Ödülleri hesaplar.
- Asker kayıplarını kaydeder.

Oyuncunun internet bağlantısı kesilse bile savaş sunucuda devam etmeli ve sonuçlanmalıdır.

Savaş sırasında oyuncunun bağlantısının kopması, savaşın veya kayıpların iptal edilmesine neden olmamalıdır.

---

# 17. SAVAŞ KAZANMA VE HASAR HESABI

KESİN KARAR:

Normal savaş ve rank savaşında saldırganın kazanması için genel hasarın en az %51 olması gerekir.

## Genel hasar hesaplama yöntemi

Her yapının kendi maksimum canına göre aldığı hasar yüzdesi hesaplanır.

Bütün yapıların hasar yüzdelerinin basit aritmetik ortalaması alınır.

Toplam can üzerinden ağırlıklı ortalama kullanılmayacak.

Örnek:

Altı yapı var.

Hasarlar:
- 1. yapı: %100
- 2. yapı: %100
- 3. yapı: %100
- 4. yapı: %6
- 5. yapı: %0
- 6. yapı: %0

Genel hasar:

(100 + 100 + 100 + 6 + 0 + 0) / 6 = %51.

Saldırgan kazanır.

Bu hesaplama yöntemi normal savaş ve rank savaşında uygulanacak.

Yapıların genel hasar hesabına hangi türlerinin dahil edileceği, yıkılmış ve yağmalanabilir binaların ayrı ele alınıp alınmayacağı teknik tasarım aşamasında kullanıcıya sorulmalıdır.

---

# 18. NORMAL SAVAŞ VE DERECE SİSTEMİ

Normal savaşta eşleştirme oyuncu seviyesine göre yapılmayacak.

Yakın derece puanına sahip oyuncular eşleştirilecek.

Normal savaş kazanıldığında derece puanı kazanılacak.

Puan formülü henüz belirlenmedi.

## Kaynak yağmalama

KESİN KARAR:

Normal savaşta rakibin gerçek mevcut kaynağının yalnızca %70'i ödül hesabına esas alınacak.

Örnek:

Savunmacının 1000 altını var.

Ödül hesabına esas miktar:

1000 × %70 = 700 altın.

Bankaya %50 hasar verilirse ve saldırgan kazanırsa:

700 × %50 = 350 altın.

Aynı yöntem odun için de uygulanacak.

Saldırgan savaşı kaybederse kaynak ödülü alamaz.

Normal savaşta kaybedilebilecek altın için üst sınır %70 olarak belirlenmiştir.

Savunmacının gerçek kaynak kaybı ile saldırganın kazandığı miktarın tam eşitliği ve diğer yağmalama sınırları daha sonra kesinleştirilecek.

## Koruma

Normal savaş sonrası 6 saatlik koruma süresi olacak.

Oyuncu herhangi bir saldırı başlatırsa bu koruma bozulacak.

Bu kuralın diğer koruma sistemleriyle etkileşimi ayrıca korunmalıdır.

---

# 19. RANK SİSTEMİ

Rank sistemi normal derece sisteminden tamamen bağımsız olacak.

Rank savaşları özel rekabetçi maçlar olarak tasarlanacak.

## Rank giriş ücreti

Rank maçına giriş ücreti:

Altın deposu kapasitesinin %15'i.

Rakip değiştirme ücreti:

Altın deposu kapasitesinin %5'i.

Ücretler oyuncunun mevcut altınından ödenecek.

Örnek:

Altın kapasitesi 1000.

- Maç giriş ücreti: 150 altın.
- Rakip değiştirme: 50 altın.

Oyuncunun 150 altını varsa maça girebilir ama rakibi değiştiremez.

200 altını varsa maça girip bir kez rakip değiştirebilir.

250 altını varsa iki kez rakip değiştirebilir.

Giriş ücretini karşılayamıyorsa rank maçına giremez.

## Rank savaşında kaynaklar

Rank maçında rakibin altın ve odun depoları gerçek miktarı ne olursa olsun dolu görünür.

Bu yalnızca savaş ekranı ve ödül hesabı için kullanılan simülasyondur.

Rank savaşını kazanan gerçek altın ve odun kazanır.

Ödül ilgili depoya verilen hasar yüzdesine göre hesaplanır.

Örnek:

Rakibin depo kapasitesi 1000.

Bankaya %10 hasar verilmiş.

Kazanılması mümkün ödül:

100 altın.

Saldırgan kaybederse bankayı tamamen yıksa bile kaynak kazanamaz.

Kaybeden savunmacının gerçek kaynakları azalmaz.

## Rank puanı

Rank puanı sabit bir +9/-3 sistemi olmayacak.

Özel bir algoritma kullanılacak.

Algoritmada:
- Oyuncunun mevcut rank puanı.
- Rakibin rank durumu.
- Oyuncu seviyesi.
- Rakibin seviyesi.
- Rakip zorluğu.
- Savaş performansı.
- Sonuç.

gibi faktörler değerlendirilebilir.

Kazanma puanı dinamik olacak.

Kaybetme cezası kazanma puanının %50'si olacak.

Örnek:

Kazanma +10 ise kaybetme -5.

Kesin algoritma daha sonra tasarlanacak.

## Eşleştirme

Rank eşleştirmesi hem oyuncu seviyesini hem rakibin rank durumunu dikkate alacak.

## Rank koruması

Rank savaşını kaybeden oyuncu 12 saat koruma kazanacak.

Bu koruma yalnızca rakiplerin oyuncuya rank saldırısı yapmasını engeller.

Oyuncu:
- Normal saldırı yapabilir.
- Normal saldırı alabilir.

Normal saldırı korumayı bozmaz.

Oyuncu 12 saat dolmadan kendisi rank saldırısı başlatırsa koruma sona erer.

---

# 20. ARAŞTIRMA SİSTEMİ

Araştırma merkezi ayrı bir bina olacak.

Araştırmalar kaynak ve zaman gerektirecek.

Araştırma merkezi seviyesi yeni araştırmaları açabilecek.

Araştırma kategorileri:

1. Asker araştırmaları.
2. Savunma ve kule araştırmaları.
3. Tuzak ve yol araştırmaları.
4. Ekonomi araştırmaları.

Araştırma tamamlandıktan sonra ilgili nesnenin geliştirme (+) menüsünden uygulanabilecek.

Araştırmalar:
- Yeni asker özellikleri.
- Yeni kule özellikleri.
- Yeni tuzaklar.
- Yeni yol malzemeleri.
- Yeni üretim avantajları.
- Yeni giriş noktaları.

açabilir.

Aynı anda kaç araştırmanın yapılabileceği henüz belirlenmedi.

Araştırma sistemi yeni içerik eklemeye uygun modüler tasarlanmalıdır.

---

# 21. OYUNCU SEVİYE VE İLERLEME SİSTEMİ

Başlangıç maksimum oyuncu seviyesi 30 olacak.

Gelecekte seviye sınırı artırılabilir.

## XP kazanımı

Oyuncu XP'yi:
- Savaşlardan.
- Görevlerden.

kazanacak.

## Seviye ödülleri

KESİN KARAR:

Her seviye atlandığında:
- Yeni özellik veya gelişim.
- Altın ödülü.
- Odun ödülü.
- +1 yol hakkı.

kazanılacak.

Ödüllerin miktarları ve açılan özelliklerin seviyeleri daha sonra belirlenecek.

## İlk oyuncu deneyimi

Oyuncu ilk kez oyuna girdiğinde rehber sistemi başlayacak.

Seviye 1:
- Orman bulunur.
- Bir köylü bulunur.
- Rehber oyuncuya kereste deposu inşa ettirir.

Kereste deposu tamamlandığında oyuncu 2. seviyeye yükselir.

Seviye 2:
- İlk kule rehber eşliğinde inşa edilir.

Bundan sonra rehber sona erer.

Oyuncu köyünü kendisi tasarlamaya başlar.

## XP formülü

Önceki taslak formül:

XP = 100 + 12 × (L-1) + 2 × (L-1)²

Bu formül henüz onaylanmamıştır.

Oyun dengesi aşamasında yeniden değerlendirilecek.

---

# 22. LONCA SİSTEMİ

Lonca sistemi oyunun sosyal özelliklerinden biridir.

KESİN KARAR:

Aşağıdaki özellikler bulunacak:

- Lonca kurma.
- Loncalara katılma.
- Lonca savaşları.
- Üyelerin birbirine asker göndermesi.
- Ortak lonca köyü.
- Lonca görevleri.
- Lonca ödülleri.

Ayrıca:
- Lonca sohbeti.
- Üyelik ve yetkiler.
- Lonca sıralamaları.

gibi özellikler değerlendirilecek.

Lonca sistemi bağımsız ve genişletilebilir tasarlanmalıdır.

---

# 23. GÖREV, ETKİNLİK VE ÖDÜL SİSTEMİ

Oyunda şu sistemler bulunacak:

- Günlük görevler.
- Haftalık görevler.
- Özel etkinlikler.
- Sezon ödülleri.
- Günlük giriş ödülleri.
- Lonca görevleri.
- Başarımlar.

Görevler oyuncunun oyunda ilerlemesini desteklemeli.

Ödüller oyun ekonomisini bozmayacak şekilde dengelenmeli.

---

# 24. KOZMETİK VE ÖZEL TASARIMLAR

Oyunda özel tasarım içerikler bulunabilecek:

- Köy görünümleri.
- Kule görünümleri.
- Asker görünümleri.
- Tematik dekorasyonlar.
- Özel efektler.

Önerilen tasarım ilkesi:

Kozmetik içerikler savaş gücünü değiştirmemelidir.

---

# 25. YENİ İÇERİK EKLEME MİMARİSİ

Oyun sürekli genişletilebilir tasarlanmalıdır.

Yeni kule, bina, asker, tuzak, araştırma veya etkinlik eklemek için mevcut sistemlerin baştan yazılması gerekmemelidir.

Örneğin ileride:
- Demir madeni.
- Yeni üretim binaları.
- Belediye binası.
- Yeni kuleler.
- Yeni asker türleri.
- Yeni tuzaklar.
- Yeni kaynaklar.

eklenebilmelidir.

Başlangıç içeriği ile ileride eklenecek içerikler birbirinden ayrılmalıdır.

Yeni içerikler mümkün olduğunca veri odaklı tanımlanmalıdır.

---

# 26. TEKNİK MİMARİ VE GÜVENLİK

## İstemci

Unity Android uygulaması.

Görevleri:
- Görsel sunum.
- Animasyon.
- Dokunmatik kontroller.
- Kullanıcı arayüzü.
- Sunucu ile iletişim.
- Savaşın görsel olarak oynatılması.

## Sunucu

Savaş ve ekonomi için otorite sunucuda olmalıdır.

Sunucu:
- Oyuncu hesaplarını doğrular.
- Köy düzenini kaydeder.
- Asker envanterini kontrol eder.
- Saldırıları doğrular.
- Savaşı simüle eder.
- Hasar ve ödülleri hesaplar.
- Kaynakları günceller.
- Koruma sürelerini yönetir.
- Rank ve derece puanlarını günceller.

## Veritabanı

Oyuncu bilgileri, köy düzenleri, kaynaklar, askerler, savaş kayıtları, rank ve lonca bilgileri güvenli biçimde saklanmalıdır.

## Güvenlik kuralları

İstemciye güvenilmemelidir.

Oyuncu kendi cihazından:
- Altın miktarını.
- Odun miktarını.
- Asker sayısını.
- Savaş sonucunu.
- Hasar miktarını.
- Rank puanını.

doğrudan değiştirememelidir.

Bütün kritik işlemler sunucu tarafından doğrulanmalıdır.

Tekrarlanan istekler, bağlantı kopmaları, hileli komutlar ve sahte savaş sonuçları test edilmelidir.

---

# 27. ÖNERİLEN PROJE KLASÖR YAPISI

Ana proje klasörü:

GameStudio/

İçeriği:

GameProject/
- Unity projesi.

GameDesign/
- GDD.md
- GameMechanics.md
- Economy.md
- CombatSystem.md
- Progression.md
- SocialSystems.md

Technical/
- Architecture.md
- Backend.md
- Database.md
- Security.md
- API.md

Art/
- Buildings/
- Towers/
- Soldiers/
- Environment/
- Effects/
- Animations/
- UI/

Testing/
- UnitTests/
- IntegrationTests/
- CombatTests/
- PerformanceTests/
- SecurityTests/

Reports/
- DevelopmentReports/
- TestReports/
- ApprovalHistory/

Documentation/
- Installation.md
- DevelopmentGuide.md
- ReleaseGuide.md

Bu klasör yapısı gerektiğinde genişletilebilir.

---

# 28. GELİŞTİRME AŞAMALARI

Geliştirme aşağıdaki sırayla ilerlemelidir.

## AŞAMA 1 – PROJE HAZIRLIĞI

- GDD dosyasını oluştur.
- Teknik mimariyi hazırla.
- Unity projesini oluştur.
- GitHub sürüm kontrolünü kur.
- Android geliştirme ortamını hazırla.
- Kod standartlarını belirle.
- Test altyapısını hazırla.

Bu aşamada henüz bütün oyunu kodlamaya başlama.

Önce teknik planı ve proje yapısını hazırla.

## AŞAMA 2 – İZOMETRİK KÖY PROTOTİPİ

- İzometrik kamera.
- 11×11 grid.
- Grid koordinat sistemi.
- Bina yerleştirme.
- Yol yerleştirme.
- Kule yerleştirme.
- Kamera hareketi.
- Yakınlaştırma.
- Dokunmatik kontroller.

Hedef: Android cihazda çalışan ilk köy prototipi.

## AŞAMA 3 – KÖY VE EKONOMİ

- Orman.
- Kereste deposu.
- Altın madeni.
- Banka.
- Köylü.
- İnşaat.
- Geliştirme.
- Üretim.
- Depolama.
- Çevrim dışı üretim.

## AŞAMA 4 – SAVUNMA SİSTEMİ

- Giriş noktaları.
- Yol ağı.
- Yol bağlantı kontrolü.
- Tuzak sistemi.
- Kule yerleşimi.
- Kule saldırı algoritması.
- Kule hasarı.
- Kule tamiri.

## AŞAMA 5 – ASKER VE SAVAŞ

- Kışla.
- Asker üretimi.
- Ortak üretim kuyruğu.
- Asker envanteri.
- Asker gönderme alanı.
- Otomatik hareket.
- Hedef seçimi.
- Saldırı davranışı.
- Geri çekilme.
- Asker kayıpları.
- Savaş raporu.

## AŞAMA 6 – ARAŞTIRMA VE İLERLEME

- Araştırma merkezi.
- Araştırma kategorileri.
- Oyuncu XP sistemi.
- Seviye sistemi.
- Seviye ödülleri.
- Yeni içerik açılımları.
- Asker gelişimleri.
- Kule gelişimleri.
- Yol ve tuzak gelişimleri.

## AŞAMA 7 – ÇEVRİM İÇİ ALTYAPI

- Oyuncu hesapları.
- Sunucu.
- Veritabanı.
- Köy kayıtları.
- Asenkron saldırılar.
- Sunucu tarafında savaş simülasyonu.
- Kaynak hesapları.
- Savaş sonuçları.
- Koruma sistemi.

## AŞAMA 8 – REKABETÇİ SİSTEMLER

- Normal savaş.
- Derece sistemi.
- Rank savaşları.
- Rank puan algoritması.
- Eşleştirme.
- Ödüller.
- Koruma süreleri.

## AŞAMA 9 – SOSYAL SİSTEMLER

- Lonca.
- Lonca savaşları.
- Ortak köy.
- Asker bağışı.
- Lonca görevleri.
- Sohbet.
- Lonca sıralaması.

## AŞAMA 10 – GÖRSEL KALİTE

- Nihai bina modelleri.
- Kule tasarımları.
- Asker modelleri.
- Animasyonlar.
- Saldırı efektleri.
- Sesler.
- Müzikler.
- Arayüz iyileştirmeleri.
- Performans optimizasyonu.

## AŞAMA 11 – TEST VE YAYIN

- Android cihaz testleri.
- Farklı ekran boyutları.
- Performans testleri.
- Sunucu yük testleri.
- Güvenlik testleri.
- Hile testleri.
- Veri kaybı testleri.
- Google Play hazırlığı.

---

# 29. HENÜZ KARARLAŞTIRILMAMIŞ KONULAR

Bu konularda kendi başına kesin karar verme.

Kullanıcıya öneri sun ve onay bekle.

1. Başlangıç yol sayısı 7 mi, 10 mu?
2. Yol hakkı toplam yol kapasitesi mi, yeni yerleştirilebilir yol parçası mı?
3. Yol çıkmazlarına izin verilecek mi?
4. Kule kapasitesi hangi seviyelerde artırılacak?
5. Yeni kule ve binalar hangi seviyelerde açılacak?
6. Bina ve kule boyutları neler olacak?
7. Askerlerin yaralı sayılma can eşiği nedir?
8. Asker üretim süreleri türlere göre nasıl dağıtılacak?
9. Üretim kuyruğundan silinen askerlerin kaynak iadesi olacak mı?
10. Araştırma aynı anda kaç tane yapılabilecek?
11. Normal savaşta yapıların hasar hesabına hangi binalar dahil olacak?
12. Normal savaşta kaynak kaybı ile saldırgan ödülü arasındaki tam ilişki nasıl olacak?
13. Rank puanı algoritmasının kesin formülü ne olacak?
14. XP formülü ve seviye ödülleri nasıl olacak?
15. Köy binaları saldırıda yağmalanırken yıkılabilecek mi?
16. Lonca savaşlarının normal savaşlardan farkları neler olacak?
17. Yeni kule, bina ve asker açılımlarının seviyesi ve araştırma koşulları ne olacak?
18. Kesin oyun ekonomisi ve kaynak maliyetleri ne olacak?

Bu soruları tek seferde kullanıcıya yükleme.

İlgili geliştirme aşamasına gelindiğinde gerekli olan kararları sırayla sor.

---

# 30. İLK GÖREVİN

Şimdi bu dokümanı temel alarak projeye hazırlan.

Henüz bütün oyunu kodlamaya başlama.

Öncelikle aşağıdaki işleri gerçekleştir:

1. Dokümanı incele.
2. Kesin kararları ve belirsiz konuları birbirinden ayır.
3. Çelişen veya teknik açıdan sorun çıkarabilecek kuralları tespit et.
4. Eksik teknik gereksinimleri listele.
5. Unity 6.3 LTS için uygun mimari öner.
6. Android performans hedeflerini belirle.
7. İzometrik grid sisteminin teknik tasarımını hazırla.
8. Sunucu ve veritabanı mimarisini öner.
9. Proje klasör yapısını oluştur.
10. Geliştirme aşamalarını görev listelerine dönüştür.
11. Test ve kalite kontrol planını hazırla.
12. Kullanıcıdan onay gerektiren kararları raporla.

İlk aşamada kullanıcıya kod yazdırma.

Kurulum ve proje hazırlığı işlemlerini mümkün olduğunca otomatik gerçekleştir.

Her işlemde proje dosyalarını koru.

Önemli değişikliklerden önce Git sürümü oluştur.

---

# 31. SON HEDEF

Bu projenin sonunda ortaya çıkacak oyun:

- Android telefonlarda çalışmalı.
- İzometrik 2.5D görsel kaliteye sahip olmalı.
- Akıcı ve profesyonel bir kullanıcı arayüzü sunmalı.
- Köy kurma ve geliştirme sistemi içermeli.
- 11×11 savunma haritasına sahip olmalı.
- 4×11 saldırı gönderim alanına sahip olmalı.
- Kule ve tuzak tabanlı savunma sunmalı.
- Otomatik asker savaş sistemine sahip olmalı.
- Asenkron PvP içermeli.
- Sunucu tarafında güvenli savaş simülasyonu yapmalı.
- Rank ve derece sistemlerine sahip olmalı.
- Lonca ve sosyal özellikler içermeli.
- Yeni içeriklerle genişletilebilmeli.
- Uzun vadede güncellenebilir ve sürdürülebilir olmalı.

Bu projeyi yalnızca çalışan bir prototip olarak değil, gerçek oyunculara sunulabilecek, uzun süre geliştirilebilecek profesyonel bir mobil oyun olarak ele al.

**ÖNCELİK: Önce doğru mimari, sonra çalışan sistemler, ardından görsel kalite ve içerik zenginliği.**

Kullanıcının onayladığı oyun kurallarını koru. Kullanıcı onayı olmadan temel oyun mekaniklerini değiştirme.