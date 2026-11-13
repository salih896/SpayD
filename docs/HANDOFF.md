# DEVİR TESLİM – Oyun Durumu ve Devam Planı
Son güncelleme: 13.11.2026 • Dal: `feature/combat-prototype` • Son commit: "Tam oyun döngüsü…" • Testler: 105/105 geçti (Unity EditMode)

> Claude Code'a ilk mesaj: "docs/HANDOFF.md, CLAUDE.md, docs/approved-mechanics.md ve docs/open-questions.md dosyalarını oku, sonra Bölüm 5'teki sıradan devam et."

## 1. Oyun nedir?
İzometrik 2.5D mobil strateji oyunu: **köy kurma + Tower Defense + asenkron PvP**. Oyuncu kaynak toplar, köyünü yollar/kuleler/tuzaklarla savunmaya hazırlar, asker eğitip başkalarının köyüne saldırır. Seviye 1–30 ilerleme, araştırma ağaçları, görevler ve günlük baskınlarla uzun süre oynatmak hedeflenir.
Teknoloji: Unity 6.3 LTS (6000.3.28f1), URP, yeni Input System, C#. Android + iOS tek proje. Tek geliştirici Claude, karar yetkisi kullanıcıda.

## 2. Mimari (kısa)
- `Core/` = saf C# simülasyon (Unity'siz, tamsayı matematik, `float` yok, tick tabanlı 10/sn, tohumlu RNG, sabit komşu sırası). İstemci ve ileride sunucu aynı kodu kullanır.
- `GameClient/` = Unity istemcisi (asmdef: GameCore, GameClient, GameCore.Tests). Core, yerel paket olarak bağlı.
- Klasörler: `Core/{Combat,Economy,Game,Grid,Military,Progression,Village}`, `Testing/UnitTests`, `Tools/gen_research.py`, `GameDesign/GDD.md`, `GameDesign/TechTrees.md`, `docs/`.
- Derleme/test komut satırından: `powershell -ExecutionPolicy Bypass -File Tools\run-tests.ps1` (`-CompileOnly`, `-Filter <ad>`). Proje Editör'de açıkken çalışmaz. 13.11.2026: 105/105 geçti (~1 dk).

## 3. Yapılanlar (GDD aşamalarına göre)
- **Aşama 1 – Hazırlık:** ✔ GDD, mimari, Unity projesi, test altyapısı, CLAUDE.md. (GitHub deposu henüz yok.)
- **Aşama 2 – Köy prototipi:** ✔ İzometrik kamera, 11×11 grid, bina/yol/kule yerleştirme, kamera hareketi, yakınlaştırma. Android cihazda denenmedi.
- **Aşama 3 – Köy ve ekonomi:** ✔ Odun/Taş/Altın/Demir, Sv'ye göre açılma, üretim, depolama kapasitesi, inşaat/yükseltme süreleri, işçi (köylü evi başı +1, en çok 6), çevrim dışı üretim (24 saate kadar).
- **Aşama 4 – Savunma:** ✔ Giriş noktaları, yol ağı ve bağlantı doğrulama (VillageLayout.Validate), tuzaklar, kule saldırısı/hasarı, kule aşınması, onarım (şimdilik anlık ve ücretsiz).
- **Aşama 5 – Asker ve savaş:** ✔ Kışla, ortak üretim kuyruğu, envanter, gönderme alanı, otomatik hareket/hedef seçimi, geri çekilme, kayıplar (ölü/yaralı/feda), savaş raporu, yağma. Deterministik BattleSim.
- **Aşama 6 – Araştırma ve ilerleme:** ✔ 7 ağaç, 474 araştırma, Üniversite kapısı, Bilim sınırları (süre -%30, maliyet -%20, 2. yuva Sv30), XP eğrisi (Sv30), seviyeye bağlı yapı seviyesi, asker/kule gelişimleri, kilometre taşı + 8 günlük görev, seviye atlama pencereleri.
- **Oyun döngüsü (ek):** 3 zorlukta hedef köy üretimi (Kolay/Normal/Zor), bot savaş sonucu, günlük baskın (Sv≥3, ≥1 kule), yerel kayıt (`save.txt`, 15 sn otomatik), OnGUI arayüzü: Köy/Kışla/Araştırma/Görevler/Saldır/Menü sekmeleri + test araçları (Menü: +1 dk/saat/gün, +XP, kaynak, "Sv 30 yap").
- **Unity'de elle doğrulananlar:** derleme temiz, oyun açılıyor, yerleştirme, kışla kuyruğu, saldırı + sonuç ekranı (yenilgi, XP, ölü sayısı), Görevler ve Araştırma sekmeleri.

## 4. Henüz doğrulanmayanlar / bilinen eksikler
- Elle denenmedi: zafer yolu, baskın raporu, kayıt-yükleme, araştırma başlatma/bitirme, seviye atlama sırası, Android/iOS cihaz.
- Sv30 testte 16 adet Sv1 askerle Sv28 köye saldırı kesin yenilgi (beklenen) → asker seviye gelişimi ve ordu kapasitesi dengesi oynayarak ayarlanacak.
- Element ağaçlarının çapraz kilitleri YOK. 3. sütun ön koşulu bir varsayım.
- Yol malzeme basamağı (Tahta/Altın+Tahta/Demir) yükseltme ve araştırması istemciye bağlı değil (TechTrees'te yol kaydı gelmedi). Yol araştırma maliyeti örnek.
- Harita genişletme (11→15) uygulanmadı (koşullu onay vardı).
- Tüm sayılar yer tutucu: XP eğrisi 100+12l+2l², seviye ölçeği 100+20l+2l², araştırma süre/maliyet, üretim/kapasite, TowerScalePct=200, görev ödülleri. Başlangıç noktası: `docs/balance-report.txt` (test çıktısı), `docs/balance-and-engagement.md`.
- Arayüz geçici OnGUI; görseller kutu/küp. Ses, müzik, animasyon, efekt yok.
- Açık sorular tam liste: `docs/open-questions.md` (banka/altın madeni açılış seviyesi çakışması, kalkan = koruma mı, vb.).

## 5. Sıradaki işler (öneri sırası)
1. **Ortam:** ✔ Depo açıldı (`Documents\GameStudio-repo`), eksik Unity iskeleti (ProjectSettings, Packages, sahne, .meta) depoya eklendi, `Tools/run-tests.ps1` yazıldı. ⏳ GitHub özel depo kullanıcıyı bekliyor (gh CLI kurulu değil).
2. **Elle test turu:** Bölüm 4'teki doğrulanmayanları sırayla dene, hataları düzelt.
3. **Denge turu:** Zafer kazanılabilir ama kolay olmayan savaş; asker/kule hasarları, ordu kapasitesi, XP/ödül hızları, günlük baskın sıklığı. Hedef: Sv1→30 toplam birkaç hafta aktif oyun.
4. **Eksik sistemler:** yol malzeme yükseltmesi + araştırması, Element kilitleri, onarım maliyeti/süresi, harita genişletme (onay şartıyla), tuzak çeşitleri/gelişimi.
5. **İlk Android build:** cihazda performans (60 FPS orta cihaz, ≤300 asker, <150 draw call), dokunmatik kontrol, ekran boyutları. İlk Aşama 2 hedefini gerçekten tamamlar.
6. **İnceleme geçişi:** kodu başkası yazmış gibi eleştir (güvenlik, determinizm, taşma/sınır durumları).
7. **Aşama 10 (görsel kalite)** – OnGUI yerine Unity UI/UI Toolkit, modeller, animasyon, ses. Sunucudan bağımsız yapılabilir.
8. **Aşama 7+ (çevrim içi, rekabet, sosyal):** ASP.NET Core + PostgreSQL + Redis önerisi; **ücretli hizmet ve mimari karar olduğu için önce kullanıcı onayı.** Core aynı kodla sunucuda savaşı yeniden oynatır (istemci güvenilmez).
9. **Aşama 11:** cihaz/performans/hile/veri kaybı testleri, Google Play hazırlığı (yayın için onay şart).

## 6. Kurallar (özet, tam hali CLAUDE.md)
- Onaylı mekanikleri izinsiz değiştirme; çelişki → `docs/open-questions.md` + tek soru.
- Onay iste: mekanik değişikliği, büyük mimari değişiklik, ücretli hizmet, gerçek oyuncu verisi silme, mağaza/yayın. `main` yalnızca kullanıcı onayıyla.
- Küçük anlamlı commit, `feature/...` dalı. Kullanıcı bu turda "bana sorma, hepsini sen ayarla" dedi; bu yetki mevcut dengeleme/uygulama işleri içindir, sunucu/ücretli/yayın için geçerli değil.
- Sunucu, hesap, rank, eşleştirme, lonca, mağaza henüz yapılmadı ve onaysız yapılmayacak.

## 7. Taşıma notu
**Çalışma kopyası artık `C:\Users\salih\Documents\GameStudio-repo` (git deposu).** `Documents\GameStudio` git'siz eski kopyadır; Unity Hub'da projeyi `GameStudio-repo\GameClient` olarak açın.
Git geçmişi `GameStudio.bundle` dosyasındadır (`C:\Users\salih\Documents\GameStudio\`). Açmak: `git clone GameStudio\GameStudio.bundle GameStudio-repo` → `git checkout feature/combat-prototype`. GitHub için `salih896/GameStudio` özel deposu henüz açılmadı.

## 8. Unity kısa notları
- Test: Window > General > Test Runner > EditMode > Run All (105 test). Denge raporu `docs/balance-report.txt` dosyasına yazılır.
- Oyun: SampleScene'de Play; `VillageBootstrap` (partial: .cs, .View, .Battle, .Ui) sahneyi ve arayüzü kodla kurar.
- Kayıt dosyası: `Application.persistentDataPath/save.txt` (silince yeni oyun).
