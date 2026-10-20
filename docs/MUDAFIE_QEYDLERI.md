# Müdafiə qeydləri — Tripio

Təqdimatda istifadə etmək üçün qısa plan və müəllimin verə biləcəyi suallara cavablar.

---

## 1. Təqdimat sırası (sənəddəki 5.1-ə uyğun)

1. **Demo (min. 8 feature)** — ~6 dəq
   Otaq axtarışı → otaq səhifəsi (şəkillər, rəylər, şəhər bələdçisi) → rezerv (promo kod) →
   təsdiq kodu və QR voucher → My Bookings (dəyiş / ləğv et) → Find booking (kod + email).
2. **Əlavə feature-lər (yaradıcılıq)** — ~4 dəq
   **Packages** (uçuş + otel + taksi, endirim 8→10→12%) → paketin səhifəsi → PDF voucher-i yüklə →
   Admin → **Emails** (göndərilən məktub) → **Hotels** xəritəsi və otelin Location bölməsi →
   **Profile** (statistika, səviyyə, avatar seçimi) → Flights / Car rental / Airport taxis / Attractions.
   Admin panel: **Analytics** (gəlir, doluluq proqnozu, top şəhərlər), otaq redaktəsi + şəkil yükləmə,
   statuslar, occupancy calendar, promo kodlar.
3. **GitHub repo** — branch-lar (`feature/...`), pull request-lər, commit tarixçəsi,
   **Actions** tab-ı (hər push-da avtomatik build + testlər) və README-dəki yaşıl CI nişanı.
4. **Swagger** — login → Authorize → qorunan endpoint (`/api/bookings/mine`) → admin endpoint.
5. **Code base / sistem dizaynı** — aşağıdakı 2-ci bölmə.

> Təqdimatdan əvvəl: **F5** bas və Swagger tam açılana qədər gözlə, sonra sayta keç.

---

## 2. Arxitekturanı necə izah edim (1–2 dəqiqə)

"Layihə **Clean Architecture** ilə qurulub və tapşırıqdakı **Variant B**-yə uyğundur:

* **Domain** — yalnız entity-lər (Hotel, Room, Booking…). Heç nədən asılı deyil.
* **Application** — biznes məntiqi: servislər, DTO-lar, qaydalar (BookingRules, PricingCalculator).
* **Infrastructure** — EF Core, SQL Server, Identity, migration və seed.
* **API** — REST endpoint-lər, JWT, Swagger. Verilənlər bazasına **yalnız API** qoşulur.
* **Web (MVC)** — istifadəçi interfeysi. Bazaya toxunmur, hər şeyi **HttpClient** ilə API-dən alır.

Asılılıqlar içəriyə doğrudur: Domain heç nəyi tanımır, Infrastructure və API isə Application-dan asılıdır.
Bu, məntiqi UI-dan və bazadan ayırır — məsələn, MVC-ni API-yə keçirəndə controller-ləri
dəyişməli olmadım, yalnız servis interfeyslərinin implementasiyasını dəyişdim."

**Göstər:** Solution Explorer → `HotelTravel.Web/Services/Api/RoomApiService.cs` (HttpClient)
və `HotelTravel.API/Controllers/RoomsController.cs` (endpoint) → `RoomService` (EF Core).

---

## 3. Mümkün suallar və cavablar

**Niyə Clean Architecture?**
Məsuliyyətlər ayrılır: biznes qaydaları UI-dan və bazadan asılı deyil. Test yazmaq asandır
(qaydalar sadə siniflərdir, servisləri InMemory bazası ilə test edirik). Bir hissəni dəyişmək
(məs. MVC → API) digərlərinə toxunmur.

**MVC niyə bazaya birbaşa qoşulmur?**
Tapşırığın Variant B tələbidir: backend API, frontend MVC, əlaqə HttpClient. Faydası: məlumata
giriş bir yerdədir (API), eyni API-ni gələcəkdə mobil tətbiq də istifadə edə bilər, təhlükəsizlik
qaydaları bir yerdə yoxlanılır.

**MVC API ilə necə danışır?**
`ApiClient` sinfi (`HttpClient`) JSON göndərir/qəbul edir. `IRoomService`, `IBookingService` və s.
interfeysləri MVC-də `RoomApiService`, `BookingApiService` kimi siniflər implement edir.
`ApiTokenHandler` hər sorğuya JWT token-i əlavə edir.

**Login necə işləyir? JWT nədir?**
MVC email/şifrəni `POST /api/auth/login`-ə göndərir. API Identity ilə şifrəni yoxlayır və imzalanmış
JWT token qaytarır (içində istifadəçi Id-si, email, rollar, bitmə vaxtı). MVC token-i şifrələnmiş
cookie-də saxlayır və hər API sorğusunda `Authorization: Bearer ...` başlığı ilə göndərir.
API token-in imzasını və vaxtını yoxlayır.

**İstifadəçi başqasının rezervini ləğv edə bilərmi?**
Xeyr. API istifadəçi Id-sini sorğudan yox, **token-dən** götürür (`CurrentUserId`). Servis
rezervin həmin istifadəçiyə aid olduğunu yoxlayır. Bunun üçün test də var
(`CancelByUserAsync_ShouldOnlyCancelOwnBooking`).

**İki nəfər eyni anda eyni otağı rezerv etsə?**
`BookingCreationService` **Serializable** tranzaksiya istifadə edir: yoxlama və yazma eyni
tranzaksiyadadır, ikinci sorğu birincini gözləyir və otaq artıq doludursa rədd olunur.
SQL Server deadlock verərsə (xəta 1205) əməliyyat 3 dəfəyə qədər təkrarlanır.

**Admin endpoint-ləri necə qorunur?**
`[Authorize(Roles = "Admin")]`. Token-də Admin rolu yoxdursa API 403 qaytarır. MVC-də də
admin area `[Authorize(Roles = "Admin")]` ilə qorunur.

**Validasiya harada edilir?**
İki yerdə: MVC formda (DataAnnotations, istifadəçiyə dərhal xəta göstərmək üçün) və API-də
(`[ApiController]` avtomatik 400 qaytarır). Biznes qaydaları (tarix keçmişdə ola bilməz, tutum,
promo kodun vaxtı) servislərdə və `BookingRules` kimi siniflərdədir.

**DTO nədir, niyə entity-ni birbaşa qaytarmırsınız?**
DTO yalnız lazım olan sahələri daşıyır. Entity-ni qaytarmaq daxili sahələri (şifrə hash-i,
naviqasiya əlaqələri) açıq edə və JSON-da dövri istinad yarada bilər.

**Silinmiş otel saytda görünürmü?**
Xeyr — **soft delete** (`IsDeleted`). Məlumat bazada qalır (köhnə rezervlər pozulmasın), amma
sorğularda süzülür. Gələcək rezervi olan otel/otaq silinə bilməz.

**Endirimlər necə hesablanır?**
`PricingCalculator`: promo kod (aktiv, vaxtı keçməyib, limit dolmayıb) və ya "explorer" endirimi
(başqa şəhərdəki otelimizdə qalmış qonağa 10%). Qiymət həmişə **serverdə** hesablanır, formdan
gələn qiymətə etibar edilmir.

**Uçuşda gəliş vaxtı necə hesablanır?**
`FlightRules.GetArrival`: uçuş vaxtı + müddət + şəhərlərin saat qurşağı fərqi
(Bakı UTC+4 → İstanbul UTC+3). Qiymət uçuşa 14 gündən az qalanda +15%, 3 gündən az qalanda +30%.

**Xəta olanda nə baş verir?**
API-də `ExceptionHandlingMiddleware` xətanı loglayır və səliqəli JSON (500) qaytarır.
MVC-də `ApiExceptionFilter`: token köhnəlibsə → login səhifəsi, API işləmirsə → "We can't reach
our server" səhifəsi, API xəta qaytarıbsa → mesaj göstərilir.

**Şəkillər harada saxlanır?**
Fayl MVC-nin `wwwroot/uploads` qovluğunda (statik fayl kimi sürətli verilir), bazada isə yalnız
ünvanı (API vasitəsilə). Başlanğıc şəkillər `wwwroot/assets/images`-dədir.

**Baza necə yaranır?**
EF Core migration-ları (`Update-Database ... -StartupProject HotelTravel.API`). API ilk dəfə
açılanda seed: rollar, otellər, otaqlar, şəhər bələdçisi, maşınlar, reyslər.

**Testlər nəyi yoxlayır?**
140-dan çox xUnit testi: rezerv qaydaları, qiymət və endirimlər, deadlock retry, rəylər, wishlist,
maşın icarəsi, taksi, uçuşlar (saat qurşağı, yer limiti, ləğv qaydası), analitika hesablamaları,
profil səviyyəsi, avatarlar, məktub şablonları (HTML encode) və trip paketinin qaydaları.
Servis testləri EF Core InMemory bazası ilə işləyir.

**CI nədir, sizdə necə işləyir?**
Continuous Integration: kod GitHub-a push olunanda **GitHub Actions** avtomatik olaraq solution-u
build edir və bütün testləri işə salır (`.github/workflows/ci.yml`). Nəsə sınarsa, PR-da qırmızı
görünür, yəni xətalı kod `master`-ə düşmür. README-dəki nişan son nəticəni göstərir.

**Swagger-də endpoint təsvirləri haradan gəlir?**
Controller-lərdəki `/// <summary>` XML şərhlərindən. Layihə build olanda XML fayl yaranır
(`GenerateDocumentationFile`), Swagger onu oxuyub hər endpoint-in və bölmənin təsvirini göstərir.

**Health check nədir?**
`GET /health` API-nin və SQL Server bağlantısının vəziyyətini JSON kimi qaytarır (`Healthy` /
`Unhealthy`). Real sistemlərdə monitorinq alətləri (və ya Docker/Kubernetes) bu ünvanı yoxlayır
və servis düşəndə xəbər verir.

**Performansı necə izləyirsiniz?**
`RequestTimingMiddleware` hər sorğunun müddətini loglayır, cavaba `X-Response-Time` başlığı yazır,
500 ms-dən uzun çəkən sorğuları xəbərdarlıq kimi qeyd edir.

**Trip paketi necə işləyir? Biri alınmasa nə olur?**
`TripPackageService` mövcud servisləri birləşdirir: əvvəlcə heç nə yaratmadan qiymətləri və yerləri
yoxlayır, sonra ardıcıl olaraq otel → gediş uçuşu → dönüş uçuşu → taksi yaradır. Hansısa addım
alınmasa, əvvəl yaradılan hissələr ləğv edilir (**saga / kompensasiya** yanaşması), beləcə yarımçıq
paket qalmır. Endirim `TripPackageRules`-dadır: uçuş + otel 8%, dönüş uçuşu +2%, taksi +2%.
Taksi uçuş enəndən 45 dəqiqə sonraya avtomatik təyin olunur.

**Niyə hamısını bir tranzaksiyada etmədiniz?**
Hər hissənin öz qaydaları və öz servisi var (məs. otel rezervi Serializable tranzaksiya ilə
ayrıca yaradılır). Onları bir böyük tranzaksiyaya yığmaq servisləri bir-birinə bağlayardı.
Kompensasiya real sistemlərdə (məs. ayrı-ayrı servislər, mikroservislər) istifadə olunan üsuldur.

**PDF və e-poçt necə yaradılır?**
PDF API-də **QuestPDF** ilə yaradılır, QR kodu **QRCoder** çəkir (skan edəndə rezerv açılır).
Məktubun HTML-i `BookingEmailTemplates`-də qurulur və istifadəçi məlumatı **HtmlEncode** olunur
(XSS olmasın, bunun testi var). Göndərmə `IEmailSender` interfeysi ilədir: SMTP verilibsə real
göndərir, yoxdursa `.eml` faylı yazır. Hər məktub `EmailLog` cədvəlinə düşür və Admin → Emails-də
görünür. Məktub alınmasa belə rezervasiya pozulmur.

**Analitika necə hesablanır?**
`AnalyticsService` API-də bütün xidmətlərin (otel, maşın, taksi, uçuş) sifarişlərini oxuyur,
ləğv olunanları gəlirdən çıxır. Doluluq proqnozu = növbəti 30 gündə tutulmuş otaq-gecələr /
(otaq sayı × 30). Hesablama düsturları `AnalyticsCalculator`-dadır və testlə yoxlanılır.
MVC yalnız hazır nəticəni (DTO) alıb Chart.js ilə çəkir.

**Xəritə necə işləyir?**
Otellərin koordinatları bazadadır. MVC onları JSON kimi səhifəyə verir, **Leaflet** kitabxanası
**OpenStreetMap** üzərində pin-ləri çəkir (pulsuzdur, API açarı lazım deyil). Koordinatsız otelə
seed zamanı şəhər mərkəzi verilir.

**Profil və avatar necə saxlanır?**
`AppUser`-ə `AvatarKey` sahəsi əlavə olunub. API yalnız siyahıdakı avatarları qəbul edir
(`AvatarCatalog.IsValid`), yoxsa 400 qaytarır. Profil yenilənəndə MVC cookie-ni yeni məlumatla
yenidən yaradır, ona görə header-dəki ad və avatar dərhal dəyişir.

**Git ilə necə işlədiniz?**
Hər feature ayrıca branch-da (`feature/flights`, `feature/api-mvc`…), sonra pull request və
`master`-ə merge. `master` həmişə işlək qalır.

---

## 4. Göstərmək üçün faydalı fayllar

| Mövzu | Fayl |
|---|---|
| API qeydiyyatı, JWT, Swagger | `HotelTravel.API/Program.cs` |
| Ortaq controller bazası | `HotelTravel.API/Controllers/ApiControllerBase.cs` |
| MVC → API əlaqəsi | `HotelTravel.Web/Services/Api/ApiClient.cs` |
| Token-in əlavə olunması | `HotelTravel.Web/Services/Api/ApiTokenHandler.cs` |
| Login (cookie + JWT) | `HotelTravel.Web/Controllers/AccountController.cs` |
| Double booking qorunması | `HotelTravel.Infrastructure/Services/BookingCreationService.cs` |
| Biznes qaydaları | `HotelTravel.Application/Services/BookingRules.cs`, `FlightRules.cs` |
| Bütün servislərin qeydiyyatı | `HotelTravel.Infrastructure/DependencyInjection.cs` |
| Trip paketi (saga / kompensasiya) | `HotelTravel.Application/Services/TripPackageService.cs` |
| PDF voucher (QuestPDF + QR) | `HotelTravel.Infrastructure/Services/QuestPdfVoucherService.cs` |
| Məktublar (şablon + jurnal) | `HotelTravel.Application/Services/BookingEmailService.cs`, `BookingEmailTemplates.cs` |
| Analitika | `HotelTravel.Application/Services/AnalyticsService.cs` |
| CI (GitHub Actions) | `.github/workflows/ci.yml` |
