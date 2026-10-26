OTAQ FOTOLARI ƏLAVƏ ETMƏK (kod yazmadan)

1) Fotoları bu qovluğa at (.jpg, .png, .webp).
2) API-ni yenidən işə sal — fotolar avtomatik otaqlara paylanır:
   - əvvəl şəkli olmayan / təkrarlanan otaqlara,
   - sonra güzgü əksi (variants) şəkli olan otaqlara.
   Beləliklə hər yeni foto dərhal saytda görünür.

3) Fotonun adını otaq tipi ilə başlasan, əvvəl həmin tipə verilir:
     king-1.jpg, king-2.jpg          -> King otaqlar
     suite-sea-view.jpg              -> Suite
     family-1.jpg                    -> Family
     twin-1.jpg, studio-1.jpg        -> Twin, Studio
     villa-1.jpg, penthouse-1.jpg    -> Villa, Penthouse
     presidential-1.jpg              -> Presidential
     deluxe-1.jpg, double-1.jpg, single-1.jpg

Admin paneldən yüklənmiş şəkillərə (/uploads/...) heç vaxt toxunulmur.
Konkret bir otağa konkret şəkil lazımdırsa: Admin → Hotels → 🛏 → Edit → Main image.
