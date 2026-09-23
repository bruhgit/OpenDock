using System;
using System.Collections.Generic;
using System.Globalization;

namespace OpenDock
{
    public static class InstallerLocalization
    {
        public sealed class Strings
        {
            public string Title { get; set; } = "OpenDock";
            public string DragInstruction { get; set; } = "To install OpenDock, drag the icon to Applications";
            public string Applications { get; set; } = "Applications";
            public string Installing { get; set; } = "Installing OpenDock...";
            public string Complete { get; set; } = "Installation complete! Launching...";
            public string Error { get; set; } = "Installation error: ";

            public string FormatDragInstruction(string appName)
            {
                return DragInstruction.Replace("OpenDock", appName);
            }

            public string FormatInstalling(string appName)
            {
                return Installing.Replace("OpenDock", appName);
            }
        }

        private static readonly Dictionary<string, Strings> Map = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = new Strings
            {
                Title = "OpenDock",
                DragInstruction = "To install OpenDock, drag the icon to Applications",
                Applications = "Applications",
                Installing = "Installing OpenDock...",
                Complete = "Installation complete! Launching...",
                Error = "Installation error: "
            },
            ["tr"] = new Strings
            {
                Title = "OpenDock",
                DragInstruction = "OpenDock'u yüklemek için simgeyi Uygulamalar klasörüne sürükleyin",
                Applications = "Uygulamalar",
                Installing = "OpenDock sisteme yükleniyor...",
                Complete = "Kurulum başarıyla tamamlandı! Başlatılıyor...",
                Error = "Kurulum hatası: "
            },
            ["de"] = new Strings
            {
                Title = "OpenDock",
                DragInstruction = "Ziehen Sie OpenDock in den Ordner „Programme“, um es zu installieren",
                Applications = "Programme",
                Installing = "OpenDock wird installiert...",
                Complete = "Installation abgeschlossen! Wird gestartet...",
                Error = "Installationsfehler: "
            },
            ["fr"] = new Strings
            {
                Title = "OpenDock",
                DragInstruction = "Pour installer OpenDock, faites glisser l'icône vers Applications",
                Applications = "Applications",
                Installing = "Installation d'OpenDock...",
                Complete = "Installation terminée ! Lancement...",
                Error = "Erreur d'installation : "
            },
            ["es"] = new Strings
            {
                Title = "OpenDock",
                DragInstruction = "Para instalar OpenDock, arrastra el icono a Aplicaciones",
                Applications = "Aplicaciones",
                Installing = "Instalando OpenDock...",
                Complete = "¡Instalación completada! Iniciando...",
                Error = "Error de instalación: "
            },
            ["it"] = new Strings
            {
                Title = "OpenDock",
                DragInstruction = "Per installare OpenDock, trascina l'icona in Applicazioni",
                Applications = "Applicazioni",
                Installing = "Installazione di OpenDock...",
                Complete = "Installazione completata! Avvio in corso...",
                Error = "Errore di installazione: "
            },
            ["pt"] = new Strings
            {
                Title = "OpenDock",
                DragInstruction = "Para instalar o OpenDock, arraste o ícone para Aplicativos",
                Applications = "Aplicativos",
                Installing = "Instalando o OpenDock...",
                Complete = "Instalação concluída! Iniciando...",
                Error = "Erro de instalação: "
            },
            ["ru"] = new Strings
            {
                Title = "OpenDock",
                DragInstruction = "Чтобы установить OpenDock, перетащите значок в «Программы»",
                Applications = "Программы",
                Installing = "Установка OpenDock...",
                Complete = "Установка завершена! Запуск...",
                Error = "Ошибка установки: "
            },
            ["zh"] = new Strings
            {
                Title = "OpenDock",
                DragInstruction = "若要安装 OpenDock，请将图标拖移至“应用程序”文件夹",
                Applications = "应用程序",
                Installing = "正在安装 OpenDock...",
                Complete = "安装完成！正在启动...",
                Error = "安装出错："
            },
            ["ja"] = new Strings
            {
                Title = "OpenDock",
                DragInstruction = "OpenDock をインストールするには、アイコンを「アプリケーション」にドラッグします",
                Applications = "アプリケーション",
                Installing = "OpenDock をインストール中...",
                Complete = "インストールが完了しました！起動中...",
                Error = "インストールエラー："
            },
            ["ko"] = new Strings
            {
                Title = "OpenDock",
                DragInstruction = "OpenDock을(를) 설치하려면 아이콘을 응용 프로그램으로 드래그하십시오",
                Applications = "응용 프로그램",
                Installing = "OpenDock 설치 중...",
                Complete = "설치가 완료되었습니다! 시작 중...",
                Error = "설치 오류: "
            },
            ["ar"] = new Strings
            {
                Title = "OpenDock",
                DragInstruction = "لتثبيت OpenDock، اسحب الأيقونة إلى التطبيقات",
                Applications = "التطبيقات",
                Installing = "جارٍ تثبيت OpenDock...",
                Complete = "اكتمل التثبيت! جارٍ التشغيل...",
                Error = "خطأ في التثبيت: "
            },
            ["nl"] = new Strings
            {
                Title = "OpenDock",
                DragInstruction = "Sleep het pictogram naar Apps om OpenDock te installeren",
                Applications = "Apps",
                Installing = "OpenDock installeren...",
                Complete = "Installatie voltooid! Starten...",
                Error = "Installatiefout: "
            },
            ["pl"] = new Strings
            {
                Title = "OpenDock",
                DragInstruction = "Aby zainstalować OpenDock, przeciągnij ikonę do Programów",
                Applications = "Programy",
                Installing = "Instalowanie OpenDock...",
                Complete = "Instalacja zakończona! Uruchamianie...",
                Error = "Błąd instalacji: "
            },
            ["sv"] = new Strings
            {
                Title = "OpenDock",
                DragInstruction = "Dra symbolen till Program för att installera OpenDock",
                Applications = "Program",
                Installing = "Installerar OpenDock...",
                Complete = "Installationen är klar! Startar...",
                Error = "Installationsfel: "
            },
            ["uk"] = new Strings
            {
                Title = "OpenDock",
                DragInstruction = "Щоб встановити OpenDock, перетягніть піктограму до «Програм»",
                Applications = "Програми",
                Installing = "Встановлення OpenDock...",
                Complete = "Встановлення завершено! Запуск...",
                Error = "Помилка встановлення: "
            },
            ["cs"] = new Strings
            {
                Title = "OpenDock",
                DragInstruction = "Chcete-li nainstalovat OpenDock, přetáhněte ikonu do složky Aplikace",
                Applications = "Aplikace",
                Installing = "Instalace OpenDock...",
                Complete = "Instalace dokončena! Spouštění...",
                Error = "Chyba instalace: "
            },
            ["hu"] = new Strings
            {
                Title = "OpenDock",
                DragInstruction = "Az OpenDock telepítéséhez húzza az ikont az Alkalmazások mappába",
                Applications = "Alkalmazások",
                Installing = "OpenDock telepítése...",
                Complete = "A telepítés befejeződött! Indítás...",
                Error = "Telepítési hiba: "
            },
            ["ro"] = new Strings
            {
                Title = "OpenDock",
                DragInstruction = "Pentru a instala OpenDock, glisați pictograma în Aplicații",
                Applications = "Aplicații",
                Installing = "Se instalează OpenDock...",
                Complete = "Instalare finalizată! Se lansează...",
                Error = "Eroare de instalare: "
            },
            ["da"] = new Strings
            {
                Title = "OpenDock",
                DragInstruction = "Træk symbolet til Programmer for at installere OpenDock",
                Applications = "Programmer",
                Installing = "Installerer OpenDock...",
                Complete = "Installationen er fuldført! Starter...",
                Error = "Installationsfejl: "
            },
            ["fi"] = new Strings
            {
                Title = "OpenDock",
                DragInstruction = "Asenna OpenDock vetämällä kuvake Apit-kansioon",
                Applications = "Apit",
                Installing = "Asennetaan OpenDockia...",
                Complete = "Asennus valmis! Käynnistetään...",
                Error = "Asennusvirhe: "
            },
            ["no"] = new Strings
            {
                Title = "OpenDock",
                DragInstruction = "Dra symbolet til Programmer for å installere OpenDock",
                Applications = "Programmer",
                Installing = "Installerer OpenDock...",
                Complete = "Installasjonen er fullført! Starter...",
                Error = "Installasjonsfeil: "
            },
            ["el"] = new Strings
            {
                Title = "OpenDock",
                DragInstruction = "Για να εγκαταστήσετε το OpenDock, σύρετε το εικονίδιο στις Εφαρμογές",
                Applications = "Εφαρμογές",
                Installing = "Εγκατάσταση του OpenDock...",
                Complete = "Η εγκατάσταση ολοκληρώθηκε! Εκκίνηση...",
                Error = "Σφάλμα εγκατάστασης: "
            },
            ["he"] = new Strings
            {
                Title = "OpenDock",
                DragInstruction = "כדי להתקין את OpenDock, גרור את הסמל ליישומים",
                Applications = "יישומים",
                Installing = "מתקין את OpenDock...",
                Complete = "ההתקנה הושלמה! מפעיל...",
                Error = "שגיאת התקנה: "
            },
            ["hi"] = new Strings
            {
                Title = "OpenDock",
                DragInstruction = "OpenDock स्थापित करने के लिए, आइकन को Applications में खींचें",
                Applications = "Applications",
                Installing = "OpenDock इंस्टॉल हो रहा है...",
                Complete = "इंस्टॉलेशन पूरा हुआ! प्रारंभ हो रहा है...",
                Error = "इंस्टॉलेशन त्रुटि: "
            },
            ["id"] = new Strings
            {
                Title = "OpenDock",
                DragInstruction = "Untuk menginstal OpenDock, seret ikon ke Aplikasi",
                Applications = "Aplikasi",
                Installing = "Menginstal OpenDock...",
                Complete = "Instalasi selesai! Meluncurkan...",
                Error = "Kesalahan instalasi: "
            },
            ["vi"] = new Strings
            {
                Title = "OpenDock",
                DragInstruction = "Để cài đặt OpenDock, hãy kéo biểu tượng vào Ứng dụng",
                Applications = "Ứng dụng",
                Installing = "Đang cài đặt OpenDock...",
                Complete = "Cài đặt hoàn tất! Đang khởi chạy...",
                Error = "Lỗi cài đặt: "
            },
            ["th"] = new Strings
            {
                Title = "OpenDock",
                DragInstruction = "หากต้องการติดตั้ง OpenDock ให้ลากไอคอนไปยังแอปพลิเคชัน",
                Applications = "แอปพลิเคชัน",
                Installing = "กำลังติดตั้ง OpenDock...",
                Complete = "การติดตั้งเสร็จสมบูรณ์! กำลังเริ่ม...",
                Error = "ข้อผิดพลาดในการติดตั้ง: "
            }
        };

        public static Strings Current
        {
            get
            {
                string lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
                if (Map.TryGetValue(lang, out var match))
                {
                    return match;
                }

                // Try prefix match for zh-CN vs zh-TW etc.
                string full = CultureInfo.CurrentUICulture.Name.ToLowerInvariant();
                if (full.StartsWith("zh"))
                {
                    return Map["zh"];
                }
                if (full.StartsWith("nb") || full.StartsWith("nn"))
                {
                    return Map["no"];
                }

                return Map["en"];
            }
        }
    }
}
