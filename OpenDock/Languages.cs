using System;
using System.Collections.Generic;

namespace OpenDock
{
    public static class Loc
    {
        private static readonly string CurrentLang;
        private static readonly Dictionary<string, Dictionary<string, string>> Translations;

        static Loc()
        {
            CurrentLang = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLower();
            Translations = new Dictionary<string, Dictionary<string, string>>();

            // 1. English (Default Fallback)
            Translations["en"] = new Dictionary<string, string> {
                {"refresh", "Refresh"}, {"appearance", "Appearance"}, {"select_logo", "Choose Menu Logo (32x32)"},
                {"default_logo", "Default Windows Logo"}, {"dock_color", "Dock Color"}, {"menu_color", "Menu Color"},
                {"search_color", "Search Box Color"}, {"position", "Position"}, {"pos_bottom", "Bottom (Horizontal)"},
                {"pos_top", "Top (Horizontal)"}, {"pos_left", "Left (Vertical)"}, {"pos_right", "Right (Vertical)"},
                {"dock_opacity", "Dock Opacity"}, {"dock_icon_size", "Dock Icon Size"}, {"show_clock", "Show Clock"},
                {"edit_css", "Edit QuickCSS (quick.css)"}, {"game_mode", "Game Mode"}, {"trans_settings", "Transition Settings"},
                {"cam_fov", "Camera Zoom / FOV"}, {"cam_depth", "Camera Depth"}, {"capture_depth", "Overlay Pre-capture Depth"},
                {"deceleration", "Transition Deceleration"}, {"slices_count", "Performance / Slices Count"},
                {"gaps_padding", "Gaps / Slice Overlap Margin"}, {"switch_delay", "Transition Switch Delay"},
                {"bg_stars", "Background Stars"}, {"show_stars", "Show Stars"}, {"stars_count", "Stars Count"},
                {"bg_image", "Background Image"}, {"choose_image", "Choose Image..."}, {"reset_gradient", "Reset to Default Gradient"},
                {"run_startup", "Run at Startup"}, {"exit", "Exit"}, {"help_text", "Drag and release to rotate • ESC to cancel"},
                {"desktop", "Desktop"}, {"dialog_ok", "OK"}, {"dialog_cancel", "Cancel"},
                {"title_fov", "Zoom / FOV Settings"}, {"prompt_fov", "Enter camera zoom factor (e.g. 2.8):"},
                {"title_depth", "Camera Depth Settings"}, {"prompt_depth", "Enter camera depth value (negative, e.g. -4.5):"},
                {"title_cap_depth", "Overlay Depth Settings"}, {"prompt_cap_depth", "Enter capture depth (0-99):"},
                {"title_decel", "Transition Deceleration Settings"}, {"prompt_decel", "Enter deceleration coefficient (0.01 - 1.0, e.g. 0.15):"},
                {"title_slices", "Slices Count Settings"}, {"prompt_slices", "Enter 3D slices count (10 - 200, e.g. 50):"},
                {"title_padding", "Overlap Margin Settings"}, {"prompt_padding", "Enter slice overlap margin (e.g. 1.5):"},
                {"title_delay", "Switch Delay Settings"}, {"prompt_delay", "Enter desktop transition switch delay (ms, e.g. 100):"},
                {"title_stars", "Stars Count Settings"}, {"prompt_stars", "Enter stars count (0 - 500):"},
                {"title_opacity", "Dock Opacity Settings"}, {"prompt_opacity", "Enter opacity value (0 - 255):"},
                {"title_size", "Icon Size Settings"}, {"prompt_size", "Enter icon pixel size (16 - 128):"}
            };

            // 2. Turkish (tr)
            Translations["tr"] = new Dictionary<string, string> {
                {"refresh", "Yenile"}, {"appearance", "Görünüm"}, {"select_logo", "32x32 logo seç"},
                {"default_logo", "Varsayılan Windows logosu"}, {"dock_color", "Dock rengi"}, {"menu_color", "Menü rengi"},
                {"search_color", "Arama kutusu rengi"}, {"position", "Konum"}, {"pos_bottom", "Alt (Yatay)"},
                {"pos_top", "Üst (Yatay)"}, {"pos_left", "Sol (Dikey)"}, {"pos_right", "Sağ (Dikey)"},
                {"dock_opacity", "Dock Opaklığı"}, {"dock_icon_size", "Dock İkon Boyutu"}, {"show_clock", "Saati Göster"},
                {"edit_css", "QuickCSS Düzenle (quick.css)"}, {"game_mode", "Oyun Modu"}, {"trans_settings", "Dönüş Ayarları"},
                {"cam_fov", "Kamera Zoom / FOV"}, {"cam_depth", "Kamera Derinliği"}, {"capture_depth", "Overlay Yakalama Derinliği"},
                {"deceleration", "Dönüş Yumuşaklığı (Deceleration)"}, {"slices_count", "Performans / Dilim Sayısı"},
                {"gaps_padding", "Gaps / Dilim Çakışma Payı"}, {"switch_delay", "Geçiş Tetikleme Gecikmesi"},
                {"bg_stars", "Arka Plan Yıldızları"}, {"show_stars", "Yıldızları Göster"}, {"stars_count", "Yıldız Sayısı"},
                {"bg_image", "Arka Plan Görseli"}, {"choose_image", "Görsel Seç..."}, {"reset_gradient", "Varsayılan Gradient Yap"},
                {"run_startup", "Başlangıçta çalıştır"}, {"exit", "Çıkış"}, {"help_text", "Döndürmek için sürükleyip bırakın • İptal etmek için ESC"},
                {"desktop", "Masaüstü"}, {"dialog_ok", "Tamam"}, {"dialog_cancel", "İptal"},
                {"title_fov", "Zoom / FOV Ayarı"}, {"prompt_fov", "Kamera zoom faktörünü girin (örn. 2.8):"},
                {"title_depth", "Kamera Derinliği Ayarı"}, {"prompt_depth", "Kamera derinlik değerini girin (negatif, örn. -4.5):"},
                {"title_cap_depth", "Overlay Derinlik Ayarı"}, {"prompt_cap_depth", "Yakalama derinliğini girin (0-99):"},
                {"title_decel", "Dönüş Yumuşaklığı Ayarı"}, {"prompt_decel", "Yavaşlama katsayısını girin (0.01 - 1.0, örn. 0.15):"},
                {"title_slices", "Dilim Sayısı Ayarı"}, {"prompt_slices", "3D dilim sayısını girin (10 - 200, örn. 50):"},
                {"title_padding", "Çakışma Payı Ayarı"}, {"prompt_padding", "Dilim çakışma payını girin (örn. 1.5):"},
                {"title_delay", "Geçiş Gecikmesi Ayarı"}, {"prompt_delay", "Masaüstü geçişler arası gecikmeyi girin (ms, örn. 100):"},
                {"title_stars", "Yıldız Sayısı Ayarı"}, {"prompt_stars", "Yıldız sayısını girin (0 - 500):"},
                {"title_opacity", "Dock Opaklık Ayarı"}, {"prompt_opacity", "Opaklık değerini girin (0 - 255):"},
                {"title_size", "İkon Boyutu Ayarı"}, {"prompt_size", "İkon piksel boyutunu girin (16 - 128):"}
            };

            // 3. Spanish (es)
            Translations["es"] = new Dictionary<string, string> {
                {"refresh", "Actualizar"}, {"appearance", "Apariencia"}, {"select_logo", "Elegir logotipo (32x32)"},
                {"default_logo", "Logotipo predeterminado"}, {"dock_color", "Color de dock"}, {"menu_color", "Color de menú"},
                {"search_color", "Color de búsqueda"}, {"position", "Posición"}, {"pos_bottom", "Abajo"},
                {"pos_top", "Arriba"}, {"pos_left", "Izquierda"}, {"pos_right", "Derecha"},
                {"dock_opacity", "Opacidad de dock"}, {"dock_icon_size", "Tamaño de icono"}, {"show_clock", "Mostrar reloj"},
                {"edit_css", "Editar QuickCSS"}, {"game_mode", "Modo de juego"}, {"trans_settings", "Ajustes de transición"},
                {"desktop", "Escritorio"}, {"dialog_ok", "Aceptar"}, {"dialog_cancel", "Cancelar"},
                {"help_text", "Arrastra y suelta para rotar • ESC para cancelar"}
            };

            // 4. French (fr)
            Translations["fr"] = new Dictionary<string, string> {
                {"refresh", "Actualiser"}, {"appearance", "Apparence"}, {"select_logo", "Choisir le logo (32x32)"},
                {"default_logo", "Logo par défaut"}, {"dock_color", "Couleur du dock"}, {"menu_color", "Couleur du menu"},
                {"search_color", "Couleur de recherche"}, {"position", "Position"}, {"pos_bottom", "Bas"},
                {"pos_top", "Haut"}, {"pos_left", "Gauche"}, {"pos_right", "Droite"},
                {"dock_opacity", "Opacité du dock"}, {"dock_icon_size", "Taille d'icône"}, {"show_clock", "Afficher l'horloge"},
                {"edit_css", "Modifier QuickCSS"}, {"game_mode", "Mode jeu"}, {"trans_settings", "Transition"},
                {"desktop", "Bureau"}, {"dialog_ok", "OK"}, {"dialog_cancel", "Annuler"},
                {"help_text", "Faites glisser et relâchez pour pivoter • ESC pour annuler"}
            };

            // 5. German (de)
            Translations["de"] = new Dictionary<string, string> {
                {"refresh", "Aktualisieren"}, {"appearance", "Aussehen"}, {"select_logo", "Logo auswählen (32x32)"},
                {"default_logo", "Standard-Logo"}, {"dock_color", "Dock-Farbe"}, {"menu_color", "Menü-Farbe"},
                {"search_color", "Suchfarbe"}, {"position", "Position"}, {"pos_bottom", "Unten"},
                {"pos_top", "Oben"}, {"pos_left", "Links"}, {"pos_right", "Rechts"},
                {"dock_opacity", "Dock-Deckkraft"}, {"dock_icon_size", "Symbolgröße"}, {"show_clock", "Uhr anzeigen"},
                {"edit_css", "QuickCSS bearbeiten"}, {"game_mode", "Spielmodus"}, {"trans_settings", "Übergang"},
                {"desktop", "Desktop"}, {"dialog_ok", "OK"}, {"dialog_cancel", "Abbrechen"},
                {"help_text", "Ziehen und loslassen zum Drehen • ESC zum Abbrechen"}
            };

            // 6. Italian (it)
            Translations["it"] = new Dictionary<string, string> {
                {"refresh", "Aggiorna"}, {"appearance", "Aspetto"}, {"select_logo", "Scegli logo (32x32)"},
                {"default_logo", "Logo predefinito"}, {"dock_color", "Colore Dock"}, {"menu_color", "Colore Menu"},
                {"search_color", "Colore Ricerca"}, {"position", "Posizione"}, {"pos_bottom", "Sotto"},
                {"pos_top", "Sopra"}, {"pos_left", "Sinistra"}, {"pos_right", "Destra"},
                {"dock_opacity", "Opacità Dock"}, {"dock_icon_size", "Dimensione Icone"}, {"show_clock", "Mostra Orologio"},
                {"edit_css", "Modifica QuickCSS"}, {"game_mode", "Modalità Gioco"}, {"trans_settings", "Transizione"},
                {"desktop", "Desktop"}, {"dialog_ok", "OK"}, {"dialog_cancel", "Annulla"},
                {"help_text", "Trascina e rilascia per ruotare • ESC per annullare"}
            };

            // 7. Portuguese (pt)
            Translations["pt"] = new Dictionary<string, string> {
                {"refresh", "Atualizar"}, {"appearance", "Aparência"}, {"select_logo", "Escolher logo (32x32)"},
                {"default_logo", "Logo padrão"}, {"dock_color", "Cor do Dock"}, {"menu_color", "Cor do Menu"},
                {"search_color", "Cor da busca"}, {"position", "Posição"}, {"pos_bottom", "Baixo"},
                {"pos_top", "Topo"}, {"pos_left", "Esquerda"}, {"pos_right", "Direita"},
                {"dock_opacity", "Opacidade do Dock"}, {"dock_icon_size", "Tamanho do ícone"}, {"show_clock", "Mostrar relógio"},
                {"edit_css", "Editar QuickCSS"}, {"game_mode", "Modo de jogo"}, {"trans_settings", "Transição"},
                {"desktop", "Ambiente de Trabalho"}, {"dialog_ok", "OK"}, {"dialog_cancel", "Cancelar"},
                {"help_text", "Arraste e solte para girar • ESC para cancelar"}
            };

            // 8. Russian (ru)
            Translations["ru"] = new Dictionary<string, string> {
                {"refresh", "Обновить"}, {"appearance", "Внешний вид"}, {"select_logo", "Выбрать логотип (32x32)"},
                {"default_logo", "Логотип по умолчанию"}, {"dock_color", "Цвет док-панели"}, {"menu_color", "Цвет меню"},
                {"search_color", "Цвет поиска"}, {"position", "Положение"}, {"pos_bottom", "Снизу"},
                {"pos_top", "Сверху"}, {"pos_left", "Слева"}, {"pos_right", "Справа"},
                {"dock_opacity", "Прозрачность"}, {"dock_icon_size", "Размер иконок"}, {"show_clock", "Показывать часы"},
                {"edit_css", "Изменить QuickCSS"}, {"game_mode", "Игровой режим"}, {"trans_settings", "Настройки перехода"},
                {"desktop", "Рабочий стол"}, {"dialog_ok", "ОК"}, {"dialog_cancel", "Отмена"},
                {"help_text", "Перетащите и отпустите для вращения • ESC для отмены"}
            };

            // 9. Chinese (zh)
            Translations["zh"] = new Dictionary<string, string> {
                {"refresh", "刷新"}, {"appearance", "外观"}, {"select_logo", "选择菜单Logo (32x32)"},
                {"default_logo", "默认Logo"}, {"dock_color", "Dock顏色"}, {"menu_color", "菜单颜色"},
                {"search_color", "搜索框颜色"}, {"position", "位置"}, {"pos_bottom", "底部"},
                {"pos_top", "顶部"}, {"pos_left", "左侧"}, {"pos_right", "右侧"},
                {"dock_opacity", "Dock透明度"}, {"dock_icon_size", "图标大小"}, {"show_clock", "显示时钟"},
                {"edit_css", "编辑 QuickCSS"}, {"game_mode", "游戏模式"}, {"trans_settings", "切换动画"},
                {"desktop", "桌面"}, {"dialog_ok", "确定"}, {"dialog_cancel", "取消"},
                {"help_text", "拖拽并释放以旋转 • 按ESC取消"}
            };

            // 10. Japanese (ja)
            Translations["ja"] = new Dictionary<string, string> {
                {"refresh", "更新"}, {"appearance", "外観"}, {"select_logo", "メニューロゴ選択 (32x32)"},
                {"default_logo", "デフォルトロゴ"}, {"dock_color", "Dockの色"}, {"menu_color", "メニューの色"},
                {"search_color", "検索の色"}, {"position", "配置"}, {"pos_bottom", "下"},
                {"pos_top", "上"}, {"pos_left", "左"}, {"pos_right", "右"},
                {"dock_opacity", "不透明度"}, {"dock_icon_size", "アイコンサイズ"}, {"show_clock", "時計表示"},
                {"edit_css", "QuickCSSの編集"}, {"game_mode", "ゲームモード"}, {"trans_settings", "アニメーション"},
                {"desktop", "デスクトップ"}, {"dialog_ok", "OK"}, {"dialog_cancel", "キャンセル"},
                {"help_text", "ドラッグして回転、離して決定 • ESCでキャンセル"}
            };

            // 11. Korean (ko)
            Translations["ko"] = new Dictionary<string, string> {
                {"refresh", "새로고침"}, {"appearance", "외관"}, {"select_logo", "로고 선택 (32x32)"},
                {"default_logo", "기본 로고"}, {"dock_color", "독 색상"}, {"menu_color", "메뉴 색상"},
                {"search_color", "검색 색상"}, {"position", "위치"}, {"pos_bottom", "아래"},
                {"pos_top", "위"}, {"pos_left", "왼쪽"}, {"pos_right", "오른쪽"},
                {"dock_opacity", "독 투명도"}, {"dock_icon_size", "아이콘 크기"}, {"show_clock", "시계 표시"},
                {"edit_css", "QuickCSS 편집"}, {"game_mode", "게임 모드"}, {"trans_settings", "전환 설정"},
                {"desktop", "데스크톱"}, {"dialog_ok", "확인"}, {"dialog_cancel", "취소"},
                {"help_text", "드래그 후 놓아서 회전 • 취소하려면 ESC"}
            };

            // 12. Arabic (ar)
            Translations["ar"] = new Dictionary<string, string> {
                {"refresh", "تحديث"}, {"appearance", "المظهر"}, {"select_logo", "اختر الشعار (32x32)"},
                {"default_logo", "الشعار الافتراضي"}, {"dock_color", "لون الشريط"}, {"menu_color", "لون القائمة"},
                {"search_color", "لون البحث"}, {"position", "الموقع"}, {"pos_bottom", "الأسفل"},
                {"pos_top", "الأعلى"}, {"pos_left", "اليسار"}, {"pos_right", "اليمين"},
                {"dock_opacity", "شفافية الشريط"}, {"dock_icon_size", "حجم الأيقونة"}, {"show_clock", "عرض الساعة"},
                {"edit_css", "تعديل QuickCSS"}, {"game_mode", "وضع الألعاب"}, {"trans_settings", "إعدادات الانتقال"},
                {"desktop", "سطح المكتب"}, {"dialog_ok", "موافق"}, {"dialog_cancel", "إلغاء"},
                {"help_text", "اسحب وأفلت للتدوير • ESC للإلغاء"}
            };

            // 13. Hindi (hi)
            Translations["hi"] = new Dictionary<string, string> {
                {"refresh", "ताज़ा करें"}, {"appearance", "दिखावट"}, {"select_logo", "लोगो चुनें (32x32)"},
                {"default_logo", "डिफ़ॉल्ट लोगो"}, {"dock_color", "डॉक रंग"}, {"menu_color", "मेनू रंग"},
                {"search_color", "खोज रंग"}, {"position", "स्थिति"}, {"pos_bottom", "नीचे"},
                {"pos_top", "ऊपर"}, {"pos_left", "बाएं"}, {"pos_right", "दाएं"},
                {"dock_opacity", "डॉक अपारदर्शिता"}, {"dock_icon_size", "आइकन आकार"}, {"show_clock", "घड़ी दिखाएं"},
                {"edit_css", "QuickCSS संपादित करें"}, {"game_mode", "गेम मोड"}, {"trans_settings", "परिवर्तन सेटिंग्स"},
                {"desktop", "डेस्कटॉप"}, {"dialog_ok", "ठीक है"}, {"dialog_cancel", "रद्द करें"},
                {"help_text", "घुमाने के लिए खींचें और छोड़ें • रद्द करने के लिए ESC"}
            };

            // 14. Dutch (nl)
            Translations["nl"] = new Dictionary<string, string> {
                {"refresh", "Vernieuwen"}, {"appearance", "Uiterlijk"}, {"select_logo", "Logo kiezen (32x32)"},
                {"default_logo", "Standaard logo"}, {"dock_color", "Dock kleur"}, {"menu_color", "Menu kleur"},
                {"position", "Positie"}, {"pos_bottom", "Onder"}, {"pos_top", "Boven"},
                {"show_clock", "Klok tonen"}, {"game_mode", "Spelmodus"}, {"desktop", "Bureaublad"},
                {"dialog_ok", "OK"}, {"dialog_cancel", "Annuleren"}, {"help_text", "Sleep en laat los om te draaien • ESC om te annuleren"}
            };

            // 15. Polish (pl)
            Translations["pl"] = new Dictionary<string, string> {
                {"refresh", "Odśwież"}, {"appearance", "Wygląd"}, {"select_logo", "Wybierz logo (32x32)"},
                {"default_logo", "Domyślne logo"}, {"dock_color", "Kolor docka"}, {"menu_color", "Kolor menu"},
                {"position", "Pozycja"}, {"pos_bottom", "Dół"}, {"pos_top", "Góra"},
                {"show_clock", "Pokaż zegar"}, {"game_mode", "Tryb gry"}, {"desktop", "Pulpit"},
                {"dialog_ok", "OK"}, {"dialog_cancel", "Anuluj"}, {"help_text", "Przeciągnij i upuść, aby obrócić • ESC aby anulować"}
            };

            // 16. Ukrainian (uk)
            Translations["uk"] = new Dictionary<string, string> {
                {"refresh", "Оновити"}, {"appearance", "Вигляд"}, {"select_logo", "Вибрати логотип (32x32)"},
                {"default_logo", "Стандартний логотип"}, {"dock_color", "Колір док-панелі"}, {"menu_color", "Колір меню"},
                {"position", "Положення"}, {"pos_bottom", "Знизу"}, {"pos_top", "Зверху"},
                {"show_clock", "Показувати годинник"}, {"game_mode", "Ігровий режим"}, {"desktop", "Робочий стіл"},
                {"dialog_ok", "ОК"}, {"dialog_cancel", "Скасувати"}, {"help_text", "Перетягніть і відпустіть для обертання • ESC для скасування"}
            };

            // 17. Swedish (sv)
            Translations["sv"] = new Dictionary<string, string> {
                {"refresh", "Uppdatera"}, {"appearance", "Utseende"}, {"select_logo", "Välj logotyp (32x32)"},
                {"default_logo", "Standardlogotyp"}, {"dock_color", "Dockfärg"}, {"menu_color", "Menyfärg"},
                {"position", "Position"}, {"pos_bottom", "Nederkant"}, {"pos_top", "Överkant"},
                {"show_clock", "Visa klocka"}, {"game_mode", "Spelläge"}, {"desktop", "Skrivbord"},
                {"dialog_ok", "OK"}, {"dialog_cancel", "Avbryt"}, {"help_text", "Dra och släpp för att rotera • ESC för att avbryta"}
            };

            // 18. Norwegian (no)
            Translations["no"] = new Dictionary<string, string> {
                {"refresh", "Oppdater"}, {"appearance", "Utseende"}, {"select_logo", "Velg logo (32x32)"},
                {"default_logo", "Standard logo"}, {"dock_color", "Dockfarge"}, {"menu_color", "Menyfarge"},
                {"position", "Posisjon"}, {"pos_bottom", "Bunn"}, {"pos_top", "Topp"},
                {"show_clock", "Vis klokke"}, {"game_mode", "Spillmodus"}, {"desktop", "Skrivebord"},
                {"dialog_ok", "OK"}, {"dialog_cancel", "Avbryt"}, {"help_text", "Dra og slipp for å rotere • ESC for å avbryte"}
            };

            // 19. Danish (da)
            Translations["da"] = new Dictionary<string, string> {
                {"refresh", "Opdater"}, {"appearance", "Udseende"}, {"select_logo", "Vælg logo (32x32)"},
                {"default_logo", "Standard logo"}, {"dock_color", "Dock farve"}, {"menu_color", "Menu farve"},
                {"position", "Position"}, {"pos_bottom", "Bund"}, {"pos_top", "Top"},
                {"show_clock", "Vis ur"}, {"game_mode", "Spiltilstand"}, {"desktop", "Skrivebord"},
                {"dialog_ok", "OK"}, {"dialog_cancel", "Annuller"}, {"help_text", "Træk og slip for at rotere • ESC for at annullere"}
            };

            // 20. Finnish (fi)
            Translations["fi"] = new Dictionary<string, string> {
                {"refresh", "Päivitä"}, {"appearance", "Ulkoasu"}, {"select_logo", "Valitse logo (32x32)"},
                {"default_logo", "Oletuslogo"}, {"dock_color", "Telakan väri"}, {"menu_color", "Valikon väri"},
                {"position", "Sijainti"}, {"pos_bottom", "Alaosa"}, {"pos_top", "Yläosa"},
                {"show_clock", "Näytä kello"}, {"game_mode", "Pelitila"}, {"desktop", "Työpöytä"},
                {"dialog_ok", "OK"}, {"dialog_cancel", "Peruuta"}, {"help_text", "Vedä ja vapauta pyörittääksesi • ESC peruuttaaksesi"}
            };

            // 21. Czech (cs)
            Translations["cs"] = new Dictionary<string, string> {
                {"refresh", "Obnovit"}, {"appearance", "Vzhled"}, {"select_logo", "Vybrat logo (32x32)"},
                {"default_logo", "Výchozí logo"}, {"dock_color", "Barva docku"}, {"menu_color", "Barva menu"},
                {"position", "Pozice"}, {"pos_bottom", "Dole"}, {"pos_top", "Nahoře"},
                {"show_clock", "Zobrazit hodiny"}, {"game_mode", "Herní režim"}, {"desktop", "Plocha"},
                {"dialog_ok", "OK"}, {"dialog_cancel", "Storno"}, {"help_text", "Táhnutím a uvolněním otočíte • ESC pro storno"}
            };

            // 22. Hungarian (hu)
            Translations["hu"] = new Dictionary<string, string> {
                {"refresh", "Frissítés"}, {"appearance", "Megjelenés"}, {"select_logo", "Logó kiválasztása (32x32)"},
                {"default_logo", "Alapértelmezett logó"}, {"dock_color", "Dokk színe"}, {"menu_color", "Menü színe"},
                {"position", "Pozíció"}, {"pos_bottom", "Alul"}, {"pos_top", "Felül"},
                {"show_clock", "Óra mutatása"}, {"game_mode", "Játék mód"}, {"desktop", "Asztal"},
                {"dialog_ok", "OK"}, {"dialog_cancel", "Mégse"}, {"help_text", "Húzza és engedje el a forgatáshoz • ESC a kilépéshez"}
            };

            // 23. Romanian (ro)
            Translations["ro"] = new Dictionary<string, string> {
                {"refresh", "Reîmprospătare"}, {"appearance", "Aspect"}, {"select_logo", "Alege logo (32x32)"},
                {"default_logo", "Logo implicit"}, {"dock_color", "Culoare Dock"}, {"menu_color", "Culoare Meniu"},
                {"position", "Poziție"}, {"pos_bottom", "Jos"}, {"pos_top", "Sus"},
                {"show_clock", "Arată ceasul"}, {"game_mode", "Mod Joc"}, {"desktop", "Secretariat / Desktop"},
                {"dialog_ok", "OK"}, {"dialog_cancel", "Anulează"}, {"help_text", "Trageți și eliberați pentru a roti • ESC pentru anulare"}
            };

            // 24. Greek (el)
            Translations["el"] = new Dictionary<string, string> {
                {"refresh", "Ανανέωση"}, {"appearance", "Εμφάνιση"}, {"select_logo", "Επιλογή λογότυπου (32x32)"},
                {"default_logo", "Προεπιλεγμένο λογότυπο"}, {"dock_color", "Χρώμα Dock"}, {"menu_color", "Χρώμα μενού"},
                {"position", "Θέση"}, {"pos_bottom", "Κάτω"}, {"pos_top", "Πάνω"},
                {"show_clock", "Εμφάνιση ρολογιού"}, {"game_mode", "Λειτουργία παιχνιδιού"}, {"desktop", "Επιφάνεια εργασίας"},
                {"dialog_ok", "OK"}, {"dialog_cancel", "Ακύρωση"}, {"help_text", "Σύρετε και αφήστε για περιστροφή • ESC για ακύρωση"}
            };

            // 25. Slovak (sk)
            Translations["sk"] = new Dictionary<string, string> {
                {"refresh", "Obnoviť"}, {"appearance", "Vzhľad"}, {"select_logo", "Vybrať logo (32x32)"},
                {"default_logo", "Predvolené logo"}, {"dock_color", "Farba docku"}, {"menu_color", "Farba menu"},
                {"position", "Pozícia"}, {"pos_bottom", "Dole"}, {"pos_top", "Hore"},
                {"show_clock", "Zobraziť hodiny"}, {"game_mode", "Herný režim"}, {"desktop", "Plocha"},
                {"dialog_ok", "OK"}, {"dialog_cancel", "Zrušiť"}, {"help_text", "Ťahaním a uvoľnením otočíte • ESC pre zrušenie"}
            };

            // 26. Bulgarian (bg)
            Translations["bg"] = new Dictionary<string, string> {
                {"refresh", "Обнови"}, {"appearance", "Външен вид"}, {"select_logo", "Избор на лого (32x32)"},
                {"default_logo", "Лого по подразбиране"}, {"dock_color", "Цвят на док"}, {"menu_color", "Цвят на меню"},
                {"position", "Позиция"}, {"pos_bottom", "Отдолу"}, {"pos_top", "Отгоре"},
                {"show_clock", "Покажи часовника"}, {"game_mode", "Режим игра"}, {"desktop", "Работен плот"},
                {"dialog_ok", "OK"}, {"dialog_cancel", "Отказ"}, {"help_text", "Плъзнете и пуснете за завъртане • ESC за отказ"}
            };

            // 27. Croatian (hr)
            Translations["hr"] = new Dictionary<string, string> {
                {"refresh", "Osvježi"}, {"appearance", "Izgled"}, {"select_logo", "Odaberi logotip (32x32)"},
                {"default_logo", "Zadani logotip"}, {"dock_color", "Boja docka"}, {"menu_color", "Boja izbornika"},
                {"position", "Položaj"}, {"pos_bottom", "Dolje"}, {"pos_top", "Gore"},
                {"show_clock", "Prikaži sat"}, {"game_mode", "Način igre"}, {"desktop", "Radna površina"},
                {"dialog_ok", "U redu"}, {"dialog_cancel", "Odustani"}, {"help_text", "Povuci i pusti za rotaciju • ESC za odustajanje"}
            };

            // 28. Serbian (sr)
            Translations["sr"] = new Dictionary<string, string> {
                {"refresh", "Освежи"}, {"appearance", "Изглед"}, {"select_logo", "Изабери лого (32x32)"},
                {"default_logo", "Подразумевани лого"}, {"dock_color", "Боја дока"}, {"menu_color", "Боја менија"},
                {"position", "Положај"}, {"pos_bottom", "Доле"}, {"pos_top", "Горе"},
                {"show_clock", "Прикажи сат"}, {"game_mode", "Режим игре"}, {"desktop", "Радна површина"},
                {"dialog_ok", "У реду"}, {"dialog_cancel", "Откажи"}, {"help_text", "Превуци и пусти за ротацију • ESC за отказ"}
            };

            // 29. Slovenian (sl)
            Translations["sl"] = new Dictionary<string, string> {
                {"refresh", "Osveži"}, {"appearance", "Videz"}, {"select_logo", "Izberi logotip (32x32)"},
                {"default_logo", "Privzeti logotip"}, {"dock_color", "Barva docka"}, {"menu_color", "Barva menija"},
                {"position", "Položaj"}, {"pos_bottom", "Spodaj"}, {"pos_top", "Zgoraj"},
                {"show_clock", "Prikaži uro"}, {"game_mode", "Igralni način"}, {"desktop", "Namizje"},
                {"dialog_ok", "V redu"}, {"dialog_cancel", "Prekliči"}, {"help_text", "Povlecite in spustite za vrtenje • ESC za preklic"}
            };

            // 30. Estonian (et)
            Translations["et"] = new Dictionary<string, string> {
                {"refresh", "Värskenda"}, {"appearance", "Välimus"}, {"select_logo", "Vali logo (32x32)"},
                {"default_logo", "Vaikimisi logo"}, {"dock_color", "Doki värv"}, {"menu_color", "Menüü värv"},
                {"position", "Asukoht"}, {"pos_bottom", "All"}, {"pos_top", "Ülal"},
                {"show_clock", "Näita kella"}, {"game_mode", "Mängurežiim"}, {"desktop", "Töölauad"},
                {"dialog_ok", "OK"}, {"dialog_cancel", "Tühista"}, {"help_text", "Lohista ja vabasta pööramiseks • Tühistamiseks ESC"}
            };

            // 31. Latvian (lv)
            Translations["lv"] = new Dictionary<string, string> {
                {"refresh", "Atsvaidzināt"}, {"appearance", "Izskats"}, {"select_logo", "Izvēlēties logo (32x32)"},
                {"default_logo", "Noklusējuma logo"}, {"dock_color", "Doka krāsa"}, {"menu_color", "Izvēlnes krāsa"},
                {"position", "Novietojums"}, {"pos_bottom", "Apakšā"}, {"pos_top", "Augšā"},
                {"show_clock", "Rādīt pulksteni"}, {"game_mode", "Spēļu režīms"}, {"desktop", "Darbvirsma"},
                {"dialog_ok", "Labi"}, {"dialog_cancel", "Atcelt"}, {"help_text", "Velciet un atlaidiet, lai pagrieztu • Atcelt ar ESC"}
            };

            // 32. Lithuanian (lt)
            Translations["lt"] = new Dictionary<string, string> {
                {"refresh", "Atnaujinti"}, {"appearance", "Išvaizda"}, {"select_logo", "Pasirinkti logotipą (32x32)"},
                {"default_logo", "Numatytasis logotipas"}, {"dock_color", "Doko spalva"}, {"menu_color", "Meniu spalva"},
                {"position", "Padėtis"}, {"pos_bottom", "Apačioje"}, {"pos_top", "Viršuje"},
                {"show_clock", "Rodyti laikrodį"}, {"game_mode", "Žaidimų režimas"}, {"desktop", "Darbalaukis"},
                {"dialog_ok", "Gerai"}, {"dialog_cancel", "Atšaukti"}, {"help_text", "Vilkite ir paleiskite, kad pasuktumėte • Atšaukti su ESC"}
            };

            // 33. Vietnamese (vi)
            Translations["vi"] = new Dictionary<string, string> {
                {"refresh", "Làm mới"}, {"appearance", "Giao diện"}, {"select_logo", "Chọn logo (32x32)"},
                {"default_logo", "Logo mặc định"}, {"dock_color", "Màu Dock"}, {"menu_color", "Màu Menu"},
                {"position", "Vị trí"}, {"pos_bottom", "Dưới"}, {"pos_top", "Trên"},
                {"show_clock", "Hiện đồng hồ"}, {"game_mode", "Chế độ game"}, {"desktop", "Màn hình chính"},
                {"dialog_ok", "OK"}, {"dialog_cancel", "Hủy"}, {"help_text", "Kéo và thả để xoay • ESC để hủy"}
            };

            // 34. Thai (th)
            Translations["th"] = new Dictionary<string, string> {
                {"refresh", "รีเฟรช"}, {"appearance", "รูปลักษณ์"}, {"select_logo", "เลือกโลโก้ (32x32)"},
                {"default_logo", "โลโก้เริ่มต้น"}, {"dock_color", "สีด็อก"}, {"menu_color", "สีเมนู"},
                {"position", "ตำแหน่ง"}, {"pos_bottom", "ล่าง"}, {"pos_top", "บน"},
                {"show_clock", "แสดงนาฬิกา"}, {"game_mode", "โหมดเกม"}, {"desktop", "เดสก์ท็อป"},
                {"dialog_ok", "ตกลง"}, {"dialog_cancel", "ยกเลิก"}, {"help_text", "ลากแล้วปล่อยเพื่อหมุน • ESC เพื่อยกเลิก"}
            };

            // 35. Indonesian (id)
            Translations["id"] = new Dictionary<string, string> {
                {"refresh", "Segarkan"}, {"appearance", "Tampilan"}, {"select_logo", "Pilih logo (32x32)"},
                {"default_logo", "Logo default"}, {"dock_color", "Warna Dock"}, {"menu_color", "Warna Menu"},
                {"position", "Posisi"}, {"pos_bottom", "Bawah"}, {"pos_top", "Atas"},
                {"show_clock", "Tampilkan jam"}, {"game_mode", "Mode Game"}, {"desktop", "Desktop"},
                {"dialog_ok", "OK"}, {"dialog_cancel", "Batal"}, {"help_text", "Seret dan lepas untuk memutar • ESC untuk batal"}
            };

            // 36. Malay (ms)
            Translations["ms"] = new Dictionary<string, string> {
                {"refresh", "Segar semula"}, {"appearance", "Paparan"}, {"select_logo", "Pilih logo (32x32)"},
                {"default_logo", "Logo lalai"}, {"dock_color", "Warna Dock"}, {"menu_color", "Warna Menu"},
                {"position", "Posisi"}, {"pos_bottom", "Bawah"}, {"pos_top", "Atas"},
                {"show_clock", "Tunjukkan jam"}, {"game_mode", "Mod Permainan"}, {"desktop", "Desktop"},
                {"dialog_ok", "OK"}, {"dialog_cancel", "Batal"}, {"help_text", "Seret dan lepaskan untuk memutar • ESC untuk batal"}
            };

            // 37. Filipino (fil)
            Translations["fil"] = new Dictionary<string, string> {
                {"refresh", "I-refresh"}, {"appearance", "Hitsura"}, {"select_logo", "Pumili ng logo (32x32)"},
                {"default_logo", "Default na logo"}, {"dock_color", "Kulay ng Dock"}, {"menu_color", "Kulay ng Menu"},
                {"position", "Posisyon"}, {"pos_bottom", "Ibaba"}, {"pos_top", "Itaas"},
                {"show_clock", "Ipakita ang Orasan"}, {"game_mode", "Mode ng Laro"}, {"desktop", "Desktop"},
                {"dialog_ok", "OK"}, {"dialog_cancel", "Kanselahin"}, {"help_text", "I-drag at i-release para i-rotate • ESC para ikansela"}
            };

            // 38. Hebrew (he)
            Translations["he"] = new Dictionary<string, string> {
                {"refresh", "רענן"}, {"appearance", "מראה"}, {"select_logo", "בחר לוגו (32x32)"},
                {"default_logo", "לוגו ברירת מחדל"}, {"dock_color", "צבע המעגן"}, {"menu_color", "צבע התפריט"},
                {"position", "מיקום"}, {"pos_bottom", "למטה"}, {"pos_top", "למעלה"},
                {"show_clock", "הצג שעון"}, {"game_mode", "מצב משחק"}, {"desktop", "שולحן עבודה"},
                {"dialog_ok", "אישור"}, {"dialog_cancel", "ביטול"}, {"help_text", "גרור ושחרר כדי לסובב • ESC לביטול"}
            };

            // 39. Persian (fa)
            Translations["fa"] = new Dictionary<string, string> {
                {"refresh", "بروزرسانی"}, {"appearance", "ظاهر"}, {"select_logo", "انتخاب لوگو (32x32)"},
                {"default_logo", "لوگوی پیش‌فرض"}, {"dock_color", "رنگ داک"}, {"menu_color", "رنگ منو"},
                {"position", "موقعیت"}, {"pos_bottom", "پایین"}, {"pos_top", "بالا"},
                {"show_clock", "نمایش ساعت"}, {"game_mode", "حالت بازی"}, {"desktop", "دسکتاپ"},
                {"dialog_ok", "تایید"}, {"dialog_cancel", "لغو"}, {"help_text", "بکشید و رها کنید تا بچرخد • ESC برای لغو"}
            };

            // 40. Urdu (ur)
            Translations["ur"] = new Dictionary<string, string> {
                {"refresh", "تازہ کریں"}, {"appearance", "ظاہری شکل"}, {"select_logo", "لوگو منتخب کریں (32x32)"},
                {"default_logo", "طے شدہ لوگو"}, {"dock_color", "ڈاک کا رنگ"}, {"menu_color", "مینو کا رنگ"},
                {"position", "مقام"}, {"pos_bottom", "نیچے"}, {"pos_top", "اوپر"},
                {"show_clock", "گھڑی دکھائیں"}, {"game_mode", "گیم موڈ"}, {"desktop", "ڈیسک ٹاپ"},
                {"dialog_ok", "ٹھیک ہے"}, {"dialog_cancel", "منسوخ کریں"}, {"help_text", "گھمانے کے لیے کھینچیں اور چھوڑیں • منسوخ کرنے کے لیے ESC"}
            };

            // 41. Bengali (bn)
            Translations["bn"] = new Dictionary<string, string> {
                {"refresh", "রিফ্রেশ"}, {"appearance", "চেহারা"}, {"select_logo", "লোগো নির্বাচন করুন (32x32)"},
                {"default_logo", "ডিফল্ট লোগো"}, {"dock_color", "ডক রঙ"}, {"menu_color", "মেনু রঙ"},
                {"position", "অবস্থান"}, {"pos_bottom", "নিচে"}, {"pos_top", "উপরে"},
                {"show_clock", "ঘড়ি দেখান"}, {"game_mode", "গেম মোড"}, {"desktop", "ডেস্কটপ"},
                {"dialog_ok", "ঠিক আছে"}, {"dialog_cancel", "বাতিল"}, {"help_text", "ঘোরানোর জন্য টেনে ছেড়ে দিন • বাতিল করতে ESC"}
            };

            // 42. Tamil (ta)
            Translations["ta"] = new Dictionary<string, string> {
                {"refresh", "புதுப்பி"}, {"appearance", "தோற்றம்"}, {"select_logo", "லோகோ தேர்வு (32x32)"},
                {"default_logo", "இயல்புநிலை லோகோ"}, {"dock_color", "டாக் நிறம்"}, {"menu_color", "மெனு நிறம்"},
                {"position", "நிலை"}, {"pos_bottom", "கீழே"}, {"pos_top", "மேலே"},
                {"show_clock", "கடிகாரம் காட்டு"}, {"game_mode", "விளையாட்டு முறை"}, {"desktop", "டெஸ்க்டாப்"},
                {"dialog_ok", "சரி"}, {"dialog_cancel", "ரத்துசெய்"}, {"help_text", "சுழற்ற இழுத்து விடுங்கள் • ரத்து செய்ய ESC"}
            };

            // 43. Telugu (te)
            Translations["te"] = new Dictionary<string, string> {
                {"refresh", "తాజా చేయి"}, {"appearance", "రూపం"}, {"select_logo", "ಲೋಗೋ ಎంచుకోండి (32x32)"},
                {"default_logo", "டிఫాల్ట్ లోగో"}, {"dock_color", "డాక్ రంగు"}, {"menu_color", "మెనూ రంగు"},
                {"position", "స్థానం"}, {"pos_bottom", "க్రింద"}, {"pos_top", "పైన"},
                {"show_clock", "గడియారం చూపు"}, {"game_mode", "गेम मोड"}, {"desktop", "డెస్క్‌టాప్"},
                {"dialog_ok", "సరే"}, {"dialog_cancel", "రద్దు చేయి"}, {"help_text", "తిప్పడానికి లాగి వదలండి • రద్దు చేయుటకు ESC"}
            };

            // 44. Marathi (mr)
            Translations["mr"] = new Dictionary<string, string> {
                {"refresh", "रिफ्रेश"}, {"appearance", "दिसणे"}, {"select_logo", "लोगो निवडा (32x32)"},
                {"default_logo", "डीफॉल्ट लोगो"}, {"dock_color", "डॉकचा रंग"}, {"menu_color", "मेनूचा रंग"},
                {"position", "स्थिती"}, {"pos_bottom", "खाली"}, {"pos_top", "वर"},
                {"show_clock", "घड्याळ दाखवा"}, {"game_mode", "गेम मोड"}, {"desktop", "डेस्कटॉप"},
                {"dialog_ok", "ठीक आहे"}, {"dialog_cancel", "रद्द करा"}, {"help_text", "फिरवण्यासाठी ड्रॅग करा आणि सोडा • रद्द करण्यासाठी ESC"}
            };

            // 45. Gujarati (gu)
            Translations["gu"] = new Dictionary<string, string> {
                {"refresh", "તાજું કરો"}, {"appearance", "દેખાવ"}, {"select_logo", "લોગો પસંદ કરો (32x32)"},
                {"default_logo", "ડિફોલ્ટ લોગો"}, {"dock_color", "ડૉક રંગ"}, {"menu_color", "મેનુ રંગ"},
                {"position", "સ્થિતિ"}, {"pos_bottom", "નીચે"}, {"pos_top", "ઉપર"},
                {"show_clock", "ઘડિયાળ બતાવો"}, {"game_mode", "ગેม મોડ"}, {"desktop", "ડેસ્કટોપ"},
                {"dialog_ok", "બરાબર"}, {"dialog_cancel", "રદ કરો"}, {"help_text", "ફેરવવા માટે ખેંચો અને છોડો • રદ કરવા માટે ESC"}
            };

            // 46. Kannada (kn)
            Translations["kn"] = new Dictionary<string, string> {
                {"refresh", "ಮರುಲೋಡ್"}, {"appearance", "ದೃಶ್ಯಾವಳಿ"}, {"select_logo", "ಲೋಗೋ ಆಯ್ಕೆ (32x32)"},
                {"default_logo", "ಡೀಫாಲ್ಟ್ ಲೋಗೋ"}, {"dock_color", "ಡಾಕ್ ಬಣ್ಣ"}, {"menu_color", "ಮೆನು ಬಣ್ಣ"},
                {"position", "ಸ್ಥಾನ"}, {"pos_bottom", "ಕೆಳगे"}, {"pos_top", "ಮೇಲೆ"},
                {"show_clock", "ಗಡಿಯಾರ ತೋರಿಸು"}, {"game_mode", "గీమ్ మోడ్"}, {"desktop", "ಡೆಸ್ಕ್‌ಟಾಪ್"},
                {"dialog_ok", "ಸರಿ"}, {"dialog_cancel", "ರದ್ದುಮಾಡು"}, {"help_text", "ತಿರುಗಿಸಲು ಎಳೆದು ಬಿಡಿ • ರದ್ದುಗೊಳಿಸಲು ESC"}
            };

            // 47. Malayalam (ml)
            Translations["ml"] = new Dictionary<string, string> {
                {"refresh", "പുതുക്കുക"}, {"appearance", "രൂപം"}, {"select_logo", "ലോഗോ തിരഞ്ഞെടുക്കുക (32x32)"},
                {"default_logo", "ഡിഫോൾട്ട് ലോഗോ"}, {"dock_color", "ഡോക് നിറം"}, {"menu_color", "മെനു നിറം"},
                {"position", "സ്ഥാനം"}, {"pos_bottom", "താഴെ"}, {"pos_top", "മുകളിൽ"},
                {"show_clock", "ക്ലോക്ക് കാണിക്കുക"}, {"game_mode", "ഗെയിം മോഡ്"}, {"desktop", "ഡെസ്ക്ടോപ്പ്"},
                {"dialog_ok", "ശരി"}, {"dialog_cancel", "റദ്ദാക്കുക"}, {"help_text", "തിരിക്കാൻ വലിച്ച് വിടുക • റദ്ദാക്കാൻ ESC അമർത്തുക"}
            };

            // 48. Punjabi (pa)
            Translations["pa"] = new Dictionary<string, string> {
                {"refresh", "ਤਾਜ਼ਾ ਕਰੋ"}, {"appearance", "ਦਿੱਖ"}, {"select_logo", "ਲੋਗੋ ਚੁਣੋ (32x32)"},
                {"default_logo", "ਡਿਫੌਲਟ ਲੋਗੋ"}, {"dock_color", "ਡਾਕ ਦਾ ਰੰਗ"}, {"menu_color", "ਮੇਨੂ ਦਾ ਰੰਗ"},
                {"position", "ਸਥਿਤੀ"}, {"pos_bottom", "ਹੇਠਾਂ"}, {"pos_top", "ਉੱਪਰ"},
                {"show_clock", "ਘੜੀ ਦਿਖਾਓ"}, {"game_mode", "ਗੇਮ ਮੋਡ"}, {"desktop", "ਡੈਸਕਟਾਪ"},
                {"dialog_ok", "ਠੀਕ ਹੈ"}, {"dialog_cancel", "ਰੱਦ ਕਰੋ"}, {"help_text", "घुमाउन ਲਈ ਖਿੱਚੋ ਅਤੇ ਛੱਡੋ • ਰੱਦ ਕਰਨ ਲਈ ESC"}
            };

            // 49. Azerbaijani (az)
            Translations["az"] = new Dictionary<string, string> {
                {"refresh", "Yenilə"}, {"appearance", "Görünüş"}, {"select_logo", "32x32 logo seç"},
                {"default_logo", "Varsayılan Windows logosu"}, {"dock_color", "Dock rəngi"}, {"menu_color", "Menyu rəngi"},
                {"position", "Mövqe"}, {"pos_bottom", "Aşağı"}, {"pos_top", "Yuxarı"},
                {"show_clock", "Saat Göstər"}, {"game_mode", "Oyun Modu"}, {"desktop", "Masaüstü"},
                {"dialog_ok", "Tamam"}, {"dialog_cancel", "İmtina"}, {"help_text", "Döndərmək üçün sürükləyin və buraxın • ESC ilə çıxın"}
            };

            // 50. Kazakh (kk)
            Translations["kk"] = new Dictionary<string, string> {
                {"refresh", "Жаңарту"}, {"appearance", "Сыртқы түрі"}, {"select_logo", "Логотипті таңдау (32x32)"},
                {"default_logo", "Әдепкі логотип"}, {"dock_color", "Док түсі"}, {"menu_color", "Мәзір түсі"},
                {"position", "Орналасуы"}, {"pos_bottom", "Төменде"}, {"pos_top", "Жоғарыда"},
                {"show_clock", "Сағатты көрсету"}, {"game_mode", "Ойын режимі"}, {"desktop", "Жұмыс үстелі"},
                {"dialog_ok", "Иә"}, {"dialog_cancel", "Бас тарту"}, {"help_text", "Айналдыру үшін сүйреп жіберіңіз • Бас тарту үшін ESC"}
            };

            // 51. Uzbek (uz)
            Translations["uz"] = new Dictionary<string, string> {
                {"refresh", "Yangilash"}, {"appearance", "Ko'rinish"}, {"select_logo", "Logo tanlash (32x32)"},
                {"default_logo", "Birlamchi logo"}, {"dock_color", "Dok rangi"}, {"menu_color", "Menyu rangi"},
                {"position", "Joylashuv"}, {"pos_bottom", "Pastki"}, {"pos_top", "Yuqori"},
                {"show_clock", "Soatni ko'rsatish"}, {"game_mode", "O'yin rejimi"}, {"desktop", "Ish stoli"},
                {"dialog_ok", "Ok"}, {"dialog_cancel", "Bekor qilish"}, {"help_text", "Aylantirish uchun sudrab qo'yib yuboring • Chiqish uchun ESC"}
            };

            // 52. Kyrgyz (ky)
            Translations["ky"] = new Dictionary<string, string> {
                {"refresh", "Жаңыртуу"}, {"appearance", "Сырткы көрүнүшү"}, {"select_logo", "Логотип тандоо (32x32)"},
                {"default_logo", "Баштапкы логотип"}, {"dock_color", "Док түсү"}, {"menu_color", "Меню түсү"},
                {"position", "Абалы"}, {"pos_bottom", "Төмөн"}, {"pos_top", "Өйдө"},
                {"show_clock", "Саатты көрсөтүү"}, {"game_mode", "Оюн режими"}, {"desktop", "Иш үстөлү"},
                {"dialog_ok", "ОК"}, {"dialog_cancel", "Жокко чыгаруу"}, {"help_text", "Айландыруу үчүн сүйрөп коё бериңиз • Бас тарту үчүн ESC"}
            };

            // 53. Tajik (tg)
            Translations["tg"] = new Dictionary<string, string> {
                {"refresh", "Навсозӣ"}, {"appearance", "Намуди зоҳирӣ"}, {"select_logo", "Интихоби логотип (32x32)"},
                {"default_logo", "Логотипи аввалия"}, {"dock_color", "Ранги док"}, {"menu_color", "Ранги меню"},
                {"position", "Мавқеъ"}, {"pos_bottom", "Поён"}, {"pos_top", "Боло"},
                {"show_clock", "Нишон додани соат"}, {"game_mode", "Реҷаи бозӣ"}, {"desktop", "Мизи корӣ"},
                {"dialog_ok", "ОК"}, {"dialog_cancel", "Лағв"}, {"help_text", "Барои чархондан кашед ва сар диҳед • Барои лағв ESC"}
            };

            // 54. Turkmen (tk)
            Translations["tk"] = new Dictionary<string, string> {
                {"refresh", "Täzele"}, {"appearance", "Görünüş"}, {"select_logo", "Logo saýla (32x32)"},
                {"default_logo", "Bellenilen logo"}, {"dock_color", "Dok reňki"}, {"menu_color", "Menýu reňki"},
                {"position", "Ýerleşişi"}, {"pos_bottom", "Aşak"}, {"pos_top", "Ýokary"},
                {"show_clock", "Sagady görkez"}, {"game_mode", "Oýun mody"}, {"desktop", "Iş stoly"},
                {"dialog_ok", "Bolýar"}, {"dialog_cancel", "Goýbolsun et"}, {"help_text", "Aýlamak üçin süýräň we goýberiň • ESC bilen ýatyryň"}
            };

            // 55. Mongolian (mn)
            Translations["mn"] = new Dictionary<string, string> {
                {"refresh", "Сэргээх"}, {"appearance", "Харагдах байдал"}, {"select_logo", "Лого сонгох (32x32)"},
                {"default_logo", "Үндсэн лого"}, {"dock_color", "Докны өнгө"}, {"menu_color", "Цэсийн өнгө"},
                {"position", "Байршил"}, {"pos_bottom", "Доор"}, {"pos_top", "Дээр"},
                {"show_clock", "Цаг харуулах"}, {"game_mode", "Тоглоомын горим"}, {"desktop", "Дэлгэц"},
                {"dialog_ok", "Тийм"}, {"dialog_cancel", "Цуцлах"}, {"help_text", "Эргүүлэхийн туلد чирээд тавь • Цуцлах бол ESC"}
            };

            // 56. Armenian (hy)
            Translations["hy"] = new Dictionary<string, string> {
                {"refresh", "Թարմացնել"}, {"appearance", "Արտաքին տեսք"}, {"select_logo", "Ընտրել լոգո (32x32)"},
                {"default_logo", "Սովորական լոգո"}, {"dock_color", "Դոկի գույնը"}, {"menu_color", "Մենյուի գույնը"},
                {"position", "Դիրքը"}, {"pos_bottom", "Ներքև"}, {"pos_top", "Վերև"},
                {"show_clock", "Ցուցադրել ժամացույցը"}, {"game_mode", "Խաղային ռեժիմ"}, {"desktop", "Աշխատասեղան"},
                {"dialog_ok", "Լավ"}, {"dialog_cancel", "Չեղարկել"}, {"help_text", "Պտտելու համար քաշեք և բաց թողեք • Չեղարկել ESC-ով"}
            };

            // 57. Georgian (ka)
            Translations["ka"] = new Dictionary<string, string> {
                {"refresh", "განახლება"}, {"appearance", "გაფორმება"}, {"select_logo", "ლოგოს არჩევა (32x32)"},
                {"default_logo", "სტანდარტული ლოგო"}, {"dock_color", "დოკის ფერი"}, {"menu_color", "მენიუს ფერი"},
                {"position", "მდებარეობა"}, {"pos_bottom", "ქვემოთ"}, {"pos_top", "ზემოთ"},
                {"show_clock", "საათის ჩვენება"}, {"game_mode", "თამაშის რეჟიმი"}, {"desktop", "სამუშაო მაგიდა"},
                {"dialog_ok", "კარგი"}, {"dialog_cancel", "გაუქმება"}, {"help_text", "დასატრიალებლად გადაათრიეთ და გაუშვით • გასაუქმებლად ESC"}
            };

            // 58. Latin (la)
            Translations["la"] = new Dictionary<string, string> {
                {"refresh", "Reficere"}, {"appearance", "Species"}, {"select_logo", "Eligere Insigne (32x32)"},
                {"default_logo", "Insigne Defectum"}, {"dock_color", "Color Navis"}, {"menu_color", "Color Index"},
                {"position", "Positio"}, {"pos_bottom", "Imo"}, {"pos_top", "Summo"},
                {"show_clock", "Monstrare Horologium"}, {"game_mode", "Modus Ludi"}, {"desktop", "Scrinium"},
                {"dialog_ok", "Optime"}, {"dialog_cancel", "Inducere"}, {"help_text", "Trahe et dimitte ad rotandum • ESC inducere"}
            };

            // 59. Albanian (sq)
            Translations["sq"] = new Dictionary<string, string> {
                {"refresh", "Rifresko"}, {"appearance", "Pamja"}, {"select_logo", "Zgjidh logon (32x32)"},
                {"default_logo", "Logo e paracaktuar"}, {"dock_color", "Ngjyra e dokut"}, {"menu_color", "Ngjyra e menusë"},
                {"position", "Pozicioni"}, {"pos_bottom", "Poshtë"}, {"pos_top", "Lart"},
                {"show_clock", "Shfaq orën"}, {"game_mode", "Modaliteti lojë"}, {"desktop", "Hapësira e punës"},
                {"dialog_ok", "OK"}, {"dialog_cancel", "Anulo"}, {"help_text", "Zvarrit dhe lësho për të rrotulluar • ESC për të anuluar"}
            };

            // 60. Macedonian (mk)
            Translations["mk"] = new Dictionary<string, string> {
                {"refresh", "Освежи"}, {"appearance", "Изглед"}, {"select_logo", "Избери лого (32x32)"},
                {"default_logo", "Стандардно лого"}, {"dock_color", "Боја на док"}, {"menu_color", "Боја на мени"},
                {"position", "Позиција"}, {"pos_bottom", "Долу"}, {"pos_top", "Горе"},
                {"show_clock", "Прикажи часовник"}, {"game_mode", "Режим за игра"}, {"desktop", "Работна површина"},
                {"dialog_ok", "Во ред"}, {"dialog_cancel", "Откажи"}, {"help_text", "Влечете и пуштете за ротација • ESC за откажување"}
            };

            // 61. Bosnian (bs)
            Translations["bs"] = new Dictionary<string, string> {
                {"refresh", "Osvježi"}, {"appearance", "Izgled"}, {"select_logo", "Odaberi logotip (32x32)"},
                {"default_logo", "Zadani logotip"}, {"dock_color", "Boja doka"}, {"menu_color", "Boja menija"},
                {"position", "Pozicija"}, {"pos_bottom", "Dolje"}, {"pos_top", "Gore"},
                {"show_clock", "Prikaži sat"}, {"game_mode", "Režim igre"}, {"desktop", "Radna površina"},
                {"dialog_ok", "U redu"}, {"dialog_cancel", "Odustani"}, {"help_text", "Povuci i pusti za rotaciju • ESC za odustajanje"}
            };

            // 62. Maltese (mt)
            Translations["mt"] = new Dictionary<string, string> {
                {"refresh", "Aġġorna"}, {"appearance", "Dehra"}, {"select_logo", "Agħżel logo (32x32)"},
                {"default_logo", "Logo default"}, {"dock_color", "Kulur tad-dock"}, {"menu_color", "Kulur tal-menu"},
                {"position", "Pożizzjoni"}, {"pos_bottom", "Isfel"}, {"pos_top", "Fuq"},
                {"show_clock", "Uri l-arloġġ"}, {"game_mode", "Game Mode"}, {"desktop", "Desktop"},
                {"dialog_ok", "OK"}, {"dialog_cancel", "Ikkanċella"}, {"help_text", "Iddragja u itlaq biex iddur • ESC biex tikkanċella"}
            };

            // 63. Welsh (cy)
            Translations["cy"] = new Dictionary<string, string> {
                {"refresh", "Adnewyddu"}, {"appearance", "Ymddangosiad"}, {"select_logo", "Dewis logo (32x32)"},
                {"default_logo", "Logo rhagosodedig"}, {"dock_color", "Lliw doc"}, {"menu_color", "Lliw dewislen"},
                {"position", "Safle"}, {"pos_bottom", "Gwaelod"}, {"pos_top", "Top"},
                {"show_clock", "Dangos cloc"}, {"game_mode", "Modd gêm"}, {"desktop", "Penbwrdd"},
                {"dialog_ok", "Iawn"}, {"dialog_cancel", "Canslo"}, {"help_text", "Llusgwch a gollyngwch i gylchdroi • ESC i ganslo"}
            };

            // 64. Irish (ga)
            Translations["ga"] = new Dictionary<string, string> {
                {"refresh", "Athnuaigh"}, {"appearance", "Cuma"}, {"select_logo", "Roghnaigh lógó (32x32)"},
                {"default_logo", "Lógó réamhshocraithe"}, {"dock_color", "Dath an duga"}, {"menu_color", "Dath an roghchláir"},
                {"position", "Suíomh"}, {"pos_bottom", "Bun"}, {"pos_top", "Barr"},
                {"show_clock", "Taispeáin clog"}, {"game_mode", "Mód Cluiche"}, {"desktop", "Deasc"},
                {"dialog_ok", "Ceart go leor"}, {"dialog_cancel", "Cealaigh"}, {"help_text", "Tarraing agus scaoil chun rothlú • ESC chun cealú"}
            };

            // 65. Icelandic (is)
            Translations["is"] = new Dictionary<string, string> {
                {"refresh", "Endurhlaða"}, {"appearance", "Útlit"}, {"select_logo", "Velja merki (32x32)"},
                {"default_logo", "Sjálfgefið merki"}, {"dock_color", "Litur á dokku"}, {"menu_color", "Litur á valmynd"},
                {"position", "Staðsetning"}, {"pos_bottom", "Neðst"}, {"pos_top", "Efst"},
                {"show_clock", "Sýna klukku"}, {"game_mode", "Leikjastilling"}, {"desktop", "Skrifborð"},
                {"dialog_ok", "Í lagi"}, {"dialog_cancel", "Hætta við"}, {"help_text", "Draga og sleppa til að snúa • ESC til að hætta við"}
            };

            // 66. Swahili (sw)
            Translations["sw"] = new Dictionary<string, string> {
                {"refresh", "Pakia upya"}, {"appearance", "Muonekano"}, {"select_logo", "Chagua nembo (32x32)"},
                {"default_logo", "Nembo ya msingi"}, {"dock_color", "Rangi ya kizimbani"}, {"menu_color", "Rangi ya menyu"},
                {"position", "Mahali"}, {"pos_bottom", "Chini"}, {"pos_top", "Juu"},
                {"show_clock", "Onyesha saa"}, {"game_mode", "Hali ya mchezo"}, {"desktop", "Skrini ya Kazi"},
                {"dialog_ok", "Sawa"}, {"dialog_cancel", "Ghairi"}, {"help_text", "Buruta na uachilie ili kuzungusha • ESC ili ughairi"}
            };

            // 67. Afrikaans (af)
            Translations["af"] = new Dictionary<string, string> {
                {"refresh", "Verfris"}, {"appearance", "Voorkoms"}, {"select_logo", "Kies logo (32x32)"},
                {"default_logo", "Verstek logo"}, {"dock_color", "Dok kleur"}, {"menu_color", "Kieslys kleur"},
                {"position", "Posisie"}, {"pos_bottom", "Onder"}, {"pos_top", "Bo"},
                {"show_clock", "Wys horlosie"}, {"game_mode", "Speelmodus"}, {"desktop", "Werkskerm"},
                {"dialog_ok", "OK"}, {"dialog_cancel", "Kanselleer"}, {"help_text", "Sleep en laat los om te draai • ESC om te kanselleer"}
            };

            // 68. Amharic (am)
            Translations["am"] = new Dictionary<string, string> {
                {"refresh", "አድስ"}, {"appearance", "ገጽታ"}, {"select_logo", "ምልክት ምረጥ (32x32)"},
                {"default_logo", "መደበኛ ምልክት"}, {"dock_color", "የዶክ ቀለም"}, {"menu_color", "የምናሌ ቀለም"},
                {"position", "ቦታ"}, {"pos_bottom", "ታች"}, {"pos_top", "ላይ"},
                {"show_clock", "ሰዓት አሳይ"}, {"game_mode", "የጨዋта ሁነታ"}, {"desktop", "ዴስክቶፕ"},
                {"dialog_ok", "እሺ"}, {"dialog_cancel", "ሰርዝ"}, {"help_text", "ለማዞር ጎትተው ይልቀቁ • ለመሰረዝ ESC ይጫኑ"}
            };

            // 69. Somali (so)
            Translations["so"] = new Dictionary<string, string> {
                {"refresh", "Cusbooneysii"}, {"appearance", "Muuqaalka"}, {"select_logo", "Dooro astaanta (32x32)"},
                {"default_logo", "Astaanta caadiga ah"}, {"dock_color", "Midabka Dekadda"}, {"menu_color", "Midabka Liiska"},
                {"position", "Booska"}, {"pos_bottom", "Hoos"}, {"pos_top", "Koor"},
                {"show_clock", "Muuji saacadda"}, {"game_mode", "Habka Ciyaarta"}, {"desktop", "Desktop-ka"},
                {"dialog_ok", "Haa"}, {"dialog_cancel", "Fliri"}, {"help_text", "Jiid oo sii daa si aad u rogto • ESC si aad u baajiso"}
            };

            // 70. Zulu (zu)
            Translations["zu"] = new Dictionary<string, string> {
                {"refresh", "Vuselela"}, {"appearance", "Ukubukeka"}, {"select_logo", "Khetha ilogo (32x32)"},
                {"default_logo", "Ilogo ezenzakalelayo"}, {"dock_color", "Umbala wedokodo"}, {"menu_color", "Umbala wemenyu"},
                {"position", "Indawo"}, {"pos_bottom", "Phansi"}, {"pos_top", "Phezulu"},
                {"show_clock", "Bonisa iwashi"}, {"game_mode", "Imodi yomdlalo"}, {"desktop", "Ideskithophu"},
                {"dialog_ok", "Kulungile"}, {"dialog_cancel", "Khansela"}, {"help_text", "Donsa bese udedela ukuze uzungezise • ESC ukukhansela"}
            };

            // 71. Xhosa (xh)
            Translations["xh"] = new Dictionary<string, string> {
                {"refresh", "Hlaziya"}, {"appearance", "Inkangeleko"}, {"select_logo", "Khetha ilogo (32x32)"},
                {"default_logo", "Ilogo engagqibekanga"}, {"dock_color", "Umbala wedock"}, {"menu_color", "Umbala wemenyu"},
                {"position", "Indawo"}, {"pos_bottom", "Ezantsi"}, {"pos_top", "Phezulu"},
                {"show_clock", "Bonisa iwotshi"}, {"game_mode", "Imodi yomdlalo"}, {"desktop", "Idesktop"},
                {"dialog_ok", "Kulungile"}, {"dialog_cancel", "Cima"}, {"help_text", "Tsala uze ukhulule ukuze ujikeleze • ESC ukucima"}
            };

            // 72. Galician (gl)
            Translations["gl"] = new Dictionary<string, string> {
                {"refresh", "Actualizar"}, {"appearance", "Aparencia"}, {"select_logo", "Escoller logo (32x32)"},
                {"default_logo", "Logo por defecto"}, {"dock_color", "Color do dock"}, {"menu_color", "Color do menú"},
                {"position", "Posición"}, {"pos_bottom", "Abaixo"}, {"pos_top", "Arriba"},
                {"show_clock", "Amosar reloxo"}, {"game_mode", "Modo xogo"}, {"desktop", "Escritorio"},
                {"dialog_ok", "Aceptar"}, {"dialog_cancel", "Cancelar"}, {"help_text", "Arrastra e solta para rotar • ESC para cancelar"}
            };

            // 73. Basque (eu)
            Translations["eu"] = new Dictionary<string, string> {
                {"refresh", "Berritu"}, {"appearance", "Itxura"}, {"select_logo", "Aukeratu logoa (32x32)"},
                {"default_logo", "Logo lehenetsia"}, {"dock_color", "Dock kolorea"}, {"menu_color", "Menu kolorea"},
                {"position", "Kokapena"}, {"pos_bottom", "Behean"}, {"pos_top", "Goian"},
                {"show_clock", "Erakutsi erlojua"}, {"game_mode", "Joko modua"}, {"desktop", "Mahaigaina"},
                {"dialog_ok", "Ados"}, {"dialog_cancel", "Utzi"}, {"help_text", "Arrastatu eta askatu biratzeko • ESC uzteko"}
            };

            // 74. Catalan (ca)
            Translations["ca"] = new Dictionary<string, string> {
                {"refresh", "Actualitzar"}, {"appearance", "Aparença"}, {"select_logo", "Triar logotip (32x32)"},
                {"default_logo", "Logotip per defecte"}, {"dock_color", "Color del dock"}, {"menu_color", "Color del menú"},
                {"position", "Posició"}, {"pos_bottom", "A baix"}, {"pos_top", "A dalt"},
                {"show_clock", "Mostrar rellotge"}, {"game_mode", "Mode joc"}, {"desktop", "Escriptori"},
                {"dialog_ok", "D'acord"}, {"dialog_cancel", "Cancel·lar"}, {"help_text", "Arrossega i deixa anar per girar • ESC per cancel·lar"}
            };

            // 75. Esperanto (eo)
            Translations["eo"] = new Dictionary<string, string> {
                {"refresh", "Aktualigi"}, {"appearance", "Aspekto"}, {"select_logo", "Elekti logotipon (32x32)"},
                {"default_logo", "Implicita logotipo"}, {"dock_color", "Doko-koloro"}, {"menu_color", "Menuo-koloro"},
                {"position", "Pozicio"}, {"pos_bottom", "Malsupre"}, {"pos_top", "Supre"},
                {"show_clock", "Montri horloĝon"}, {"game_mode", "Lud-reĝimo"}, {"desktop", "Labortablo"},
                {"dialog_ok", "Bone"}, {"dialog_cancel", "Nuligi"}, {"help_text", "Treni kaj lasi por turni • ESC por nuligi"}
            };

            // 76. Yiddish (yi)
            Translations["yi"] = new Dictionary<string, string> {
                {"refresh", "דערפרישן"}, {"appearance", "אויסזען"}, {"select_logo", "קלייַבן לאָגאָ (32x32)"},
                {"default_logo", "פעיל לאָגאָ"}, {"dock_color", "דאָק קאָлир"}, {"menu_color", "מעניו קאָлир"},
                {"position", "פּۆזיציע"}, {"pos_bottom", "אונטן"}, {"pos_top", "אอยבן"},
                {"show_clock", "ווייַזן זייגער"}, {"game_mode", "שפּיל מאָדע"}, {"desktop", "דעסקטۆפּ"},
                {"dialog_ok", "גוט"}, {"dialog_cancel", "بטל"}, {"help_text", "שלעפּן און לאָזן גיין צו דרייען • ESC צו בטל"}
            };

            // 77. Kurdish (ku)
            Translations["ku"] = new Dictionary<string, string> {
                {"refresh", "Nûkirin"}, {"appearance", "Diyarî"}, {"select_logo", "Logoyek hilbijêre (32x32)"},
                {"default_logo", "Logoya standard"}, {"dock_color", "Renga dockê"}, {"menu_color", "Renga menyuyê"},
                {"position", "Cîh"}, {"pos_bottom", "Jêr"}, {"pos_top", "Jor"},
                {"show_clock", "Demjimêr nîşan bide"}, {"game_mode", "Moda lîstikê"}, {"desktop", "Maseya xebatê"},
                {"dialog_ok", "Temam"}, {"dialog_cancel", "Betal bike"}, {"help_text", "Kişandin û berdan ji bo zivirandinê • ESC ji bo betalkirinê"}
            };

            // 78. Kurdish Sorani (ckb)
            Translations["ckb"] = new Dictionary<string, string> {
                {"refresh", "نوێکردنەوە"}, {"appearance", "ڕوکار"}, {"select_logo", "لۆگۆ هەڵبژێرە (32x32)"},
                {"default_logo", "لۆگۆی بنەڕەتی"}, {"dock_color", "ڕەنگی دۆک"}, {"menu_color", "ڕەنگی مینیو"},
                {"position", "شوێن"}, {"pos_bottom", "خوارەوە"}, {"pos_top", "سەرەوە"},
                {"show_clock", "پیشاندانی کاتژمێر"}, {"game_mode", "دۆخی ياری"}, {"desktop", "شاشەی کار"},
                {"dialog_ok", "باشە"}, {"dialog_cancel", "پاشگەزبوونەوە"}, {"help_text", "ڕاکێشە و بەریبدە بۆ سوڕانەوە • ESC بۆ پاشگەزبوونەوە"}
            };

            // 79. Pashto (ps)
            Translations["ps"] = new Dictionary<string, string> {
                {"refresh", "نوې کول"}, {"appearance", "بڼه"}, {"select_logo", "لوګو غوره کړه (32x32)"},
                {"default_logo", "اصلي لوګو"}, {"dock_color", "د ډاک رنګ"}, {"menu_color", "د مینو رنګ"},
                {"position", "موقیعت"}, {"pos_bottom", "ښکته"}, {"pos_top", "پورته"},
                {"show_clock", "ساعت ښودل"}, {"game_mode", "د لوبې حالت"}, {"desktop", "کارځای"},
                {"dialog_ok", "سمه ده"}, {"dialog_cancel", "لغوه کول"}, {"help_text", "د څرخولو لپاره کش کړئ او خوشې کړئ • د لغوه کولو لپاره ESC"}
            };

            // 80. Sindhi (sd)
            Translations["sd"] = new Dictionary<string, string> {
                {"refresh", "تازو ڪريو"}, {"appearance", "ظاهري روپ"}, {"select_logo", "لوگو چونڊيو (32x32)"},
                {"default_logo", "اصل logo"}, {"dock_color", "ڊاڪ رنگ"}, {"menu_color", "ميوو رنگ"},
                {"position", "جڳھ"}, {"pos_bottom", "هيٺ"}, {"pos_top", "مٿي"},
                {"show_clock", "گھڙي ڏيکاريو"}, {"game_mode", "گيم موڊ"}, {"desktop", "ڊيسڪ ٽاپ"},
                {"dialog_ok", "ٺيڪ آهي"}, {"dialog_cancel", "رد ڪريو"}, {"help_text", "ڦيرائڻ لاءِ ڇڪيو ۽ ڇڏيو • رد ڪرڻ لاءِ ESC دٻايو"}
            };

            // 81. Nepali (ne)
            Translations["ne"] = new Dictionary<string, string> {
                {"refresh", "ताजा गर्नुहोस्"}, {"appearance", "स्वरूप"}, {"select_logo", "लोगो चयन गर्नुहोस् (32x32)"},
                {"default_logo", "पूर्वनिर्धारित लोगो"}, {"dock_color", "डक रङ"}, {"menu_color", "मेनु रङ"},
                {"position", "स्थिति"}, {"pos_bottom", "तल"}, {"pos_top", "माथि"},
                {"show_clock", "घडी देखाउनुहोस्"}, {"game_mode", "खेल मोड"}, {"desktop", "डेस्कटप"},
                {"dialog_ok", "ठिक छ"}, {"dialog_cancel", "रद्द गर्नुहोस्"}, {"help_text", "घुमाउन तान्नुहोस् र छोड्नुहोस् • रद्द गर्न ESC थिच्नुहोस्"}
            };

            // 82. Sinhala (si)
            Translations["si"] = new Dictionary<string, string> {
                {"refresh", "යාවත්කාලීන කරන්න"}, {"appearance", "පෙනුම"}, {"select_logo", "လိုගෝ තෝරන්න (32x32)"},
                {"default_logo", "පෙරනිමි ලෝගෝව"}, {"dock_color", "ඩොක් වර්ණය"}, {"menu_color", "මෙනු වර්ණය"},
                {"position", "පිහිටීම"}, {"pos_bottom", "පහළ"}, {"pos_top", "ඉහළ"},
                {"show_clock", "ඔරලෝසුව පෙන්වන්න"}, {"game_mode", "ක්‍රීඩා ප්‍රකාරය"}, {"desktop", "වැඩතලය"},
                {"dialog_ok", "හරි"}, {"dialog_cancel", "අවලංගු කරන්න"}, {"help_text", "කරකැවීමට ඇද හැර දමන්න • අවලංගု කිරීමට ESC ඔබන්න"}
            };

            // 83. Khmer (km)
            Translations["km"] = new Dictionary<string, string> {
                {"refresh", "ធ្វើឱ្យស្រស់"}, {"appearance", "រូបរាង"}, {"select_logo", "ជ្រើសរើសឡូហ្គោ (32x32)"},
                {"default_logo", "ឡូហ្គោលំនាំដើម"}, {"dock_color", "ពណ៌ដុក"}, {"menu_color", "ពណ៌ម៉ឺនុយ"},
                {"position", "ទីតាំង"}, {"pos_bottom", "ខាងក្រោម"}, {"pos_top", "ខាងលើ"},
                {"show_clock", "បង្ហាញនាឡិកា"}, {"game_mode", "របៀបលេងហ្គេម"}, {"desktop", "ផ្ទៃការងារ"},
                {"dialog_ok", "យល់ព្រម"}, {"dialog_cancel", "បោះបង់"}, {"help_text", "អូសហើយលែងដើម្បីបង្វិល • ចុច ESC ដើម្បីបោះបង់"}
            };

            // 84. Lao (lo)
            Translations["lo"] = new Dictionary<string, string> {
                {"refresh", "ໂຫຼດໃຫມ່"}, {"appearance", "ຮູບລັກສະນະ"}, {"select_logo", "ເລືອກໂລໂກ້ (32x32)"},
                {"default_logo", "ໂລໂກ້ເລີ່ມຕົ້ນ"}, {"dock_color", "ສີດັອກ"}, {"menu_color", "ສີເມນູ"},
                {"position", "ຕຳແໜ່ງ"}, {"pos_bottom", "ລຸ່ມ"}, {"pos_top", "ເທິງ"},
                {"show_clock", "ສະແດງໂມງ"}, {"game_mode", "ໂໝດເກມ"}, {"desktop", "ເດັສທັອບ"},
                {"dialog_ok", "ຕົกลົງ"}, {"dialog_cancel", "ຍົກເລີກ"}, {"help_text", "ລາກແລ້ວປ່ອຍເພື່ອໝຸນ • ກົດ ESC ເພື່ອຍົກເລີກ"}
            };

            // 85. Burmese (my)
            Translations["my"] = new Dictionary<string, string> {
                {"refresh", "လတ်ဆတ်ဆန်းသစ်ပါ"}, {"appearance", "ရုပ်သွင်"}, {"select_logo", "လိုဂိုရွေးပါ (32x32)"},
                {"default_logo", "မူလလိုဂို"}, {"dock_color", "ဒေါ့အရောင်"}, {"menu_color", "မီနူးအရောင်"},
                {"position", "တည်နေရာ"}, {"pos_bottom", "အောက်ခြေ"}, {"pos_top", "ထိပ်ပိုင်း"},
                {"show_clock", "နာရီပြပါ"}, {"game_mode", "ဂိမ်းမုဒ်"}, {"desktop", "ဒက်စတော့"},
                {"dialog_ok", "ကောင်းပြီ"}, {"dialog_cancel", "ပယ်ဖျက်ပါ"}, {"help_text", "လှည့်ရန် ဆွဲပြီးလွှတ်ပါ • ပယ်ဖျက်ရန် ESC နှိပ်ပါ"}
            };

            // 86. Tibetan (bo)
            Translations["bo"] = new Dictionary<string, string> {
                {"refresh", "གསར་སྒྱུར།"}, {"appearance", "ཕྱི་ཚུལ།"}, {"select_logo", "རྟགས་རིས་འདེམས་པ། (32x32)"},
                {"default_logo", "སྔོན་འཇོག་རྟགས་རིས།"}, {"dock_color", "སྒྲོམ་གཞིའི་ཚོན་མදོག།"}, {"menu_color", "དཀར་ཆག་ཚོན་མදོག།"},
                {"position", "གནས་ས།"}, {"pos_bottom", "འོག་མ།"}, {"pos_top", "གོང་མ།"},
                {"show_clock", "ཆུ་ཚོད་སྟོན་པ།"}, {"game_mode", "རྩེད་མོའི་རྣམ་པ།"}, {"desktop", "མདུན་ལྗོངས།"},
                {"dialog_ok", "ཡོང་བ།"}, {"dialog_cancel", "ཕྱིར་འթེན།"}, {"help_text", "བསྐོར་བར་དྲུད་དེ་གློད་དགོས། • ཕྱིར་འթེནལ་ ESC གནོན།"}
            };

            // 87. Luxembourgish (lb)
            Translations["lb"] = new Dictionary<string, string> {
                {"refresh", "Erfrëschen"}, {"appearance", "Ausgesinn"}, {"select_logo", "Logo auswielen (32x32)"},
                {"default_logo", "Standard-Logo"}, {"dock_color", "Dock-Faarf"}, {"menu_color", "Menü-Faarf"},
                {"position", "Positioun"}, {"pos_bottom", "Ënnen"}, {"pos_top", "Uewen"},
                {"show_clock", "Auer weisen"}, {"game_mode", "Spillmodus"}, {"desktop", "Desktop"},
                {"dialog_ok", "OK"}, {"dialog_cancel", "Ofbriechen"}, {"help_text", "Zéien a lassloossen fir ze dréien • ESC fir ofzebriechen"}
            };

            // 88. Frisian (fy)
            Translations["fy"] = new Dictionary<string, string> {
                {"refresh", "Fernije"}, {"appearance", "Foarkommen"}, {"select_logo", "Logo kieze (32x32)"},
                {"default_logo", "Standert logo"}, {"dock_color", "Dock kleur"}, {"menu_color", "Menu kleur"},
                {"position", "Posysje"}, {"pos_bottom", "Under"}, {"pos_top", "Boppe"},
                {"show_clock", "Klok sjen litte"}, {"game_mode", "Spulmodus"}, {"desktop", "Buroblêd"},
                {"dialog_ok", "OK"}, {"dialog_cancel", "Ofbrekke"}, {"help_text", "Slepe en loslitte om te draaien • ESC om ôf te brekken"}
            };

            // 89. Corsican (co)
            Translations["co"] = new Dictionary<string, string> {
                {"refresh", "Rinfrescà"}, {"appearance", "Aspettu"}, {"select_logo", "Sceglie u logu (32x32)"},
                {"default_logo", "Logu predefinitu"}, {"dock_color", "Culore Dock"}, {"menu_color", "Culore Menu"},
                {"position", "Posizione"}, {"pos_bottom", "Sottu"}, {"pos_top", "Sopra"},
                {"show_clock", "Mostrà l'orologio"}, {"game_mode", "Modu Ghjocu"}, {"desktop", "Scrivania"},
                {"dialog_ok", "OK"}, {"dialog_cancel", "Annullà"}, {"help_text", "Trascinà è lascià andà per girà • ESC per annullà"}
            };

            // 90. Tatar (tt)
            Translations["tt"] = new Dictionary<string, string> {
                {"refresh", "Яңарту"}, {"appearance", "Тышкы күренеш"}, {"select_logo", "Логотип сайлау (32x32)"},
                {"default_logo", "Стандарт логотип"}, {"dock_color", "Док төсе"}, {"menu_color", "Меню төсе"},
                {"position", "Урын"}, {"pos_bottom", "Астында"}, {"pos_top", "Өстендә"},
                {"show_clock", "Сәгатьне күрсәтү"}, {"game_mode", "Уен режимы"}, {"desktop", "Иш өстәле"},
                {"dialog_ok", "ОК"}, {"dialog_cancel", "Баш тарту"}, {"help_text", "Әйләндерү үшін сөйрәп җибәрегез • Баш тарту үшін ESC"}
            };

            // 91. Bashkir (ba)
            Translations["ba"] = new Dictionary<string, string> {
                {"refresh", "Яңыртыу"}, {"appearance", "Тышҡы күренеш"}, {"select_logo", "Логотип һайлау (32x32)"},
                {"default_logo", "Башланғыс логотип"}, {"dock_color", "Док төҫө"}, {"menu_color", "Menu төҫө"},
                {"position", "Урын"}, {"pos_bottom", "Аҫта"}, {"pos_top", "Өҫтә"},
                {"show_clock", "Сәғәтте күрһәтеү"}, {"game_mode", "Уйын режимы"}, {"desktop", "Эш өҫтәле"},
                {"dialog_ok", "ОК"}, {"dialog_cancel", "Баш тартыу"}, {"help_text", "Өйөрөү өсөн һөйрәп ебәрегеҙ • Баш тартыу өсөн ESC"}
            };

            // 92. Belarusian (be)
            Translations["be"] = new Dictionary<string, string> {
                {"refresh", "Абнавіць"}, {"appearance", "Вонкавы выгляд"}, {"select_logo", "Выбраць лагатып (32x32)"},
                {"default_logo", "Стандартны лагатып"}, {"dock_color", "Колер док-панэлі"}, {"menu_color", "Колер меню"},
                {"position", "Становішча"}, {"pos_bottom", "Знізу"}, {"pos_top", "Зверху"},
                {"show_clock", "Паказваць гадзіннік"}, {"game_mode", "Гульнявы рэжым"}, {"desktop", "Рабочы стол"},
                {"dialog_ok", "ОК"}, {"dialog_cancel", "Адмена"}, {"help_text", "Перацягніце і адпусціце для кручэння • ESC для адмены"}
            };

            // 93. Hawaiian (haw)
            Translations["haw"] = new Dictionary<string, string> {
                {"refresh", "Hoʻomaʻemaʻe"}, {"appearance", "Maka"}, {"select_logo", "Koho Logo (32x32)"},
                {"default_logo", "Logo Paʻamau"}, {"dock_color", "Waihoʻoluʻu Uapo"}, {"menu_color", "Waihoʻoluʻu Papa Kuhikuhi"},
                {"position", "Kūlana"}, {"pos_bottom", "Lalo"}, {"pos_top", "Luna"},
                {"show_clock", "Hōʻike Uaki"}, {"game_mode", "Pāʻani Mode"}, {"desktop", "Papapihi"},
                {"dialog_ok", "Hiki"}, {"dialog_cancel", "Hoʻōki"}, {"help_text", "Kauo a hoʻokuʻu e kaʻapuni • ESC e hoʻōki"}
            };

            // 94. Maori (mi)
            Translations["mi"] = new Dictionary<string, string> {
                {"refresh", "Whakahou"}, {"appearance", "Ahua"}, {"select_logo", "Tīpako Tohu (32x32)"},
                {"default_logo", "Tohu Tunoa"}, {"dock_color", "Kahu Uapo"}, {"menu_color", "Kahu Tahua"},
                {"position", "Tūranga"}, {"pos_bottom", "Raro"}, {"pos_top", "Runga"},
                {"show_clock", "Whakaatu Karaka"}, {"game_mode", "Hākinakina"}, {"desktop", "Papamahi"},
                {"dialog_ok", "E pai ana"}, {"dialog_cancel", "Whakakore"}, {"help_text", "Tōia ka tuku kia huri • ESC ki te whakakore"}
            };

            // 95. Icelandic (is)
            Translations["is"] = new Dictionary<string, string> {
                {"refresh", "Endurnýja"}, {"appearance", "Útlit"}, {"select_logo", "Velja merki (32x32)"},
                {"default_logo", "Sjálfgefið merki"}, {"dock_color", "Litur á dokku"}, {"menu_color", "Litur á valmynd"},
                {"position", "Staðsetning"}, {"pos_bottom", "Neðst"}, {"pos_top", "Efst"},
                {"show_clock", "Sýna klukku"}, {"game_mode", "Leikjastilling"}, {"desktop", "Skrifborð"},
                {"dialog_ok", "Í lagi"}, {"dialog_cancel", "Hætta við"}, {"help_text", "Draga og sleppa til að snúa • ESC til að hætta við"}
            };

            // 96. Kurdish Badini (ku)
            Translations["ku"] = new Dictionary<string, string> {
                {"refresh", "Nûkirin"}, {"appearance", "Diyarî"}, {"select_logo", "Hilbijartina logo (32x32)"},
                {"default_logo", "Logoya standard"}, {"dock_color", "Rengê dokê"}, {"menu_color", "Rengê menyûyê"},
                {"position", "Cih"}, {"pos_bottom", "Jêr"}, {"pos_top", "Jor"},
                {"show_clock", "Nîşandana demjimêrê"}, {"game_mode", "Moda lîstikê"}, {"desktop", "Maseya xebatê"},
                {"dialog_ok", "Belê"}, {"dialog_cancel", "Betal bike"}, {"help_text", "Kişandin û berdan ji bo zivirandinê • ESC ji bo betalkirinê"}
            };

            // 97. Yiddish (yi)
            Translations["yi"] = new Dictionary<string, string> {
                {"refresh", "דערפרעשן"}, {"appearance", "אויסזען"}, {"select_logo", "קלייַבן לאָגאָ (32x32)"},
                {"default_logo", "פעיל לאָגאָ"}, {"dock_color", "דאָк קאָлир"}, {"menu_color", "מעניו קۆլיר"},
                {"position", "פּۆזיציע"}, {"pos_bottom", "אונטן"}, {"pos_top", "אויבן"},
                {"show_clock", "ווייַזן זייגער"}, {"game_mode", "שפּיל מאָדע"}, {"desktop", "דעסקטۆפּ"},
                {"dialog_ok", "גוט"}, {"dialog_cancel", "بטל"}, {"help_text", "שלעפּן און לאָזן גיין צו דרייען • ESC צו בטל"}
            };

            // 98. Latin (la)
            Translations["la"] = new Dictionary<string, string> {
                {"refresh", "Reficere"}, {"appearance", "Species"}, {"select_logo", "Eligere Insigne (32x32)"},
                {"default_logo", "Insigne Defectum"}, {"dock_color", "Color Navis"}, {"menu_color", "Color Index"},
                {"position", "Positio"}, {"pos_bottom", "Imo"}, {"pos_top", "Summo"},
                {"show_clock", "Monstrare Horologium"}, {"game_mode", "Modus Ludi"}, {"desktop", "Scrinium"},
                {"dialog_ok", "Optime"}, {"dialog_cancel", "Inducere"}, {"help_text", "Trahe et dimitte ad rotandum • ESC inducere"}
            };

            // 99. Galician (gl)
            Translations["gl"] = new Dictionary<string, string> {
                {"refresh", "Actualizar"}, {"appearance", "Aparencia"}, {"select_logo", "Escoller logo (32x32)"},
                {"default_logo", "Logo por defecto"}, {"dock_color", "Color do dock"}, {"menu_color", "Color do menú"},
                {"position", "Posición"}, {"pos_bottom", "Abaixo"}, {"pos_top", "Arriba"},
                {"show_clock", "Amosar reloxo"}, {"game_mode", "Modo xogo"}, {"desktop", "Escritorio"},
                {"dialog_ok", "Aceptar"}, {"dialog_cancel", "Cancelar"}, {"help_text", "Arrastra e solta para rotar • ESC para cancelar"}
            };

            // 100. Basque (eu)
            Translations["eu"] = new Dictionary<string, string> {
                {"refresh", "Berritu"}, {"appearance", "Itxura"}, {"select_logo", "Aukeratu logoa (32x32)"},
                {"default_logo", "Logo lehenetsia"}, {"dock_color", "Dock kolorea"}, {"menu_color", "Menu kolorea"},
                {"position", "Kokapena"}, {"pos_bottom", "Behean"}, {"pos_top", "Goian"},
                {"show_clock", "Erakutsi erlojua"}, {"game_mode", "Joko modua"}, {"desktop", "Mahaigaina"},
                {"dialog_ok", "Ados"}, {"dialog_cancel", "Utzi"}, {"help_text", "Arrastatu eta askatu biratzeko • ESC uzteko"}
            };
        }

        public static string Get(string key)
        {
            if (Translations.TryGetValue(CurrentLang, out var dict) && dict.TryGetValue(key, out var val))
            {
                return val;
            }
            if (Translations["en"].TryGetValue(key, out var fallbackVal))
            {
                return fallbackVal;
            }
            return key;
        }
    }
}
