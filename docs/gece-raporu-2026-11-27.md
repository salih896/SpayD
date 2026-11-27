# Gece çalışması raporu (27.11.2026)

İstek: "görsel ve grafiklerini geliştir, animasyonlar ekle, oynanış şeklini geliştir, harita görünümünü geliştir; bunlar dışında da geliştirmeler yap."
Dal: `feature/combat-prototype` (main'e birleştirilmedi). Kural (mekanik) değişikliği yapılmadı; eklenenler görsel ve kolaylık özellikleri.

## Eklenenler (commit sırasıyla)

| Commit | Ne |
|---|---|
| 5b59fcb | Son işleme: parlama (kor, kristal, element ışıkları gerçekten parlıyor), renk ayarı, hafif kenar kararması, kenar yumuşatma. Ayarlar: **Grafik Yüksek / Düşük**. Köy çevresinde kuşlar, kayan bulut gölgeleri, kelebekler, gölet pırıltısı |
| 88fc575 | Savaş hissi: yapı hasarı uçan sayılarla; yıkılınca toz + seken moloz + "YIKILDI" + kamera sarsıntısı; ölen asker devrilip gömülüyor (uçanlar düşüyor); ateş eden kule geri tepiyor; yıldız eşiği duyuruları (%50/75/90/100); zaferde konfeti, askerler zıplıyor |
| bda1e0a | Saldırı ekranında hedef köyün 3B keşif görüntüsü (tuzaklar gizli). Savaşta **Hepsini Gönder**. Saldırı alanı asker gönderilmeden önce kırmızı nabızla parlıyor. Savaş düğmelerinde asker görseli |
| a836a44 | **Gerçek saate bağlı gün/gece** (şafak, gündüz, gün batımı, okunaklı gece; gece pencere ışıkları). Ayarlar: Gün/Gece Gerçek saat / Hep gündüz. Sonuç ekranı: yıldızlar sırayla parlıyor, hasar sayarak artıyor, **Yeni Saldırı** düğmesi |
| 22e3286 | Köyde üretim binalarının üstünde "+N kaynak" yazıları; seviye atlayınca duyuru + konfeti; Köy Düzeni'nde yerleştirme tozu; kamera bırakınca kayarak duruyor |
| e8c9888 | Harita: sağda kıvrılan nehir (kıyı, taş, saz, akıntı pırıltısı), su değirmeni (dönen çark), arkada buğday (rüzgârda dalgalanıyor) ve sebze tarlaları, korkuluk, yel değirmeni |
| 65ffb3b | Performans: küreler 768 yerine ~120 üçgen (sahne ~800 bin → ~215 bin üçgen), yakın renkler tek materyal, küçük süsler gölgesiz; Düşük grafikte gölge ve süs efektleri kapalı |
| 46c0cdd | **Savunmayı İzle**: HUD'da "Son baskın" yanında İzle düğmesi; günlük baskındaki türden bot şimdiki köyüne saldırıyor, sen izliyorsun (sonuç sayılmaz) |

Testler: 183 toplam, 181 geçti, 0 kaldı, 2 atlandı. Her parça Unity'de derlendi ve oyunda görüldü.

## Ölçümler (editör, köy ekranı)

| | Batch | Üçgen |
|---|---|---|
| Başlangıç | ~1600 | ~800 bin |
| Şimdi, Yüksek grafik | ~1070 | ~215 bin |
| Şimdi, Düşük grafik | ~630 | ~135 bin |

## Açık konular (senin kararın)

1. **Batch sayısı hâlâ yüksek** (hedef <150). Asıl yük orman halkası (yüzlerce ağaç parçası). Elle örgü birleştirme ve GPU instancing denedim; ikisi de bu projede kötüleştirdi, geri aldım. Sonraki adım: ormanı köşe renkli tek bir gölgelendiriciyle birleştirmek ya da ağaç sayısını azaltmak.
2. **Denge:** Savunmayı İzle'de Sv30 baskıncısı köyünü %100 yıktı, hiç asker kaybetmedi. Daha önce not ettiğimiz bot kazanma oranı (%94) sorunuyla aynı; seviye gücü kalibrasyonu bekliyor (`docs/open-questions.md`).
3. **Telefonda denenmedi.** Son işleme ve parçacıklar telefonda pil/ısı yükü getirebilir; sorun olursa Ayarlar → Grafik: Düşük.
4. Editörde pencerelerin sağ üstündeki tıklamaları **NVIDIA ekran katmanı** yutuyor (oyun hatası değil).
