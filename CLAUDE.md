# CLAUDE.md — Proje Kuralları

Proje: İzometrik 2.5D mobil strateji (köy + Tower Defense + asenkron PvP). Unity 6.3 LTS, C#. Android + iOS, tek proje. Tek geliştirici: Claude. Karar yetkisi: kullanıcı.

## Okuma sırası (token tasarrufu)
1. Bu dosya. 2. `docs/approved-mechanics.md` (bağlayıcı kararlar). 3. Yalnızca görevle ilgili `GameDesign/GDD.md` bölümü. Tüm GDD'yi her seferinde okuma.

## Kurallar
- Onaylı mekanikleri izinsiz değiştirme. Çelişki bulursan `docs/open-questions.md`'ye yaz ve sor.
- Kullanıcıdan onay iste: mekanik değişikliği, büyük mimari değişiklik, ücretli hizmet, gerçek oyuncu verisi silme, mağazaya yükleme/yayın.
- Eksik kararı kendin verme; kısa öneri + tek soru. İlgili aşama gelmeden sorma.
- Git: her görev `feature/...` dalı, `main`'e yalnızca kullanıcı onayıyla. Küçük, anlamlı commit.
- Büyük özellik sonrası "inceleme geçişi": kodu başkası yazmış gibi eleştir (özellikle güvenlik/sunucu).

## Mimari
- `Core/` saf C# simülasyon kütüphanesi (Unity bağımsız), sabit noktalı tamsayı matematik, `float` yok. İstemci ve sunucu aynı kodu kullanır. Determinizm: tick tabanlı, tohumlu RNG, sabit komşu sırası.
- Sunucu otoritedir (ASP.NET Core + PostgreSQL + Redis, Aşama 7'de onaya sunulur). İstemci güvenilmez.
- İçerik veri odaklı (sürümlü JSON): kule, asker, tuzak, araştırma, süreler, koruma değerleri. Sabit kodlama yok.
- Mantıksal grid tamsayı koordinat; izometri yalnızca kamera/görsel (ortografik kamera, ışın-düzlem seçimi).
- Unity: Force Text, Visible Meta Files. `.unity/.prefab/.meta` çatışmasına dikkat.

## Performans hedefi (öneri)
Orta cihaz 60 FPS, giriş seviyesi 30 FPS, ≤300 asker, <150 draw call, açılış <5 sn, indirme <150 MB.

## Aşamalar
GDD Bölüm 28 sırası. Şu an: Aşama 2 (izometrik köy prototipi).
