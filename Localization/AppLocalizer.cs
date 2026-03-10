namespace RemittanceMatcherApp.Localization;

public sealed record LanguageOption(string Code, string DisplayName);

public static class AppLocalizer
{
    private static readonly Dictionary<string, string> En = new(StringComparer.OrdinalIgnoreCase)
    {
        ["header_main"] = "GETINGE - REMITTANCE ADVICE MATCHER",
        ["app_title"] = "Getinge Remittance Matcher",
        ["label_lookback"] = "Lookback days:",
        ["label_language"] = "Language:",
        ["tooltip_lookback"] = "How many days back the app should scan emails. Example: 7 means today plus the previous 7 days.",
        ["btn_about"] = "About",
        ["btn_settings"] = "Settings",
        ["btn_load_settings"] = "Load settings",
        ["btn_save_settings"] = "Save settings",
        ["btn_fetch_transactions"] = "Fetch transactions",
        ["btn_start"] = "Start",
        ["btn_stop"] = "Stop",
        ["status_ready"] = "Ready",
        ["status_starting"] = "Starting...",
        ["status_done"] = "Completed",
        ["status_canceled"] = "Canceled",
        ["status_error"] = "Error",
        ["label_live_log"] = "Live log",
        ["log_app_started"] = "Application started.",
        ["log_ready"] = "Application ready.",
        ["log_settings_applied"] = "Settings applied.",
        ["log_canceled"] = "Process canceled by user.",
        ["log_search_started"] = "Started searching for remittance advice.",
        ["log_search_finished"] = "Search finished.",
        ["log_process_summary"] = "Summary: transactions={0}, matched remittances={1}, unmatched={2}.",
        ["log_language_changed_prefix"] = "Language changed to:",
        ["log_settings_changed_prefix"] = "Changed settings:",
        ["log_fetch_dev_clicked"] = "Fetch transactions clicked (developer mode).",
        ["log_settings_saved_default"] = "Settings saved to default local profile.",
        ["msg_error"] = "Error",
        ["msg_warning"] = "Warning",
        ["msg_success"] = "Success",
        ["msg_bad_settings_prefix"] = "Invalid settings:",
        ["msg_process_error_title"] = "Process error",
        ["msg_save_fail_prefix"] = "Could not save settings:",
        ["msg_load_fail_prefix"] = "Could not load settings:",
        ["msg_settings_saved"] = "Settings were saved.",
        ["msg_fetch_dev_title"] = "Developer mode",
        ["msg_fetch_dev_body"] = "This option is currently in developer mode and will be available in a future release.",
        ["msg_about_missing"] = "Description file not found.",
        ["msg_about_open_fail"] = "Could not open description file:",
        ["dlg_save_settings"] = "Save settings",
        ["dlg_load_settings"] = "Load settings",
        ["log_settings_saved_prefix"] = "Settings saved:",
        ["log_settings_loaded_prefix"] = "Settings loaded:",
        ["msg_out_of_range"] = "{0}: value out of range {1}-{2}.",

        ["settings_title"] = "Settings",
        ["settings_paths_header"] = "Paths and work mode",
        ["settings_label_feban_folder"] = "FEBAN CSV folder",
        ["settings_label_feban_file"] = "FEBAN CSV filename",
        ["settings_label_remit_folder"] = "Remittance output folder",
        ["settings_tt_feban_folder"] = "Folder that contains transactions.csv. Wrong folder means process start will fail.",
        ["settings_tt_feban_file"] = "Transaction filename. Usually transactions.csv.",
        ["settings_tt_remit_folder"] = "Base destination folder for saved remittances. Date subfolders are created automatically.",

        ["settings_btn_advanced"] = "Advanced settings",
        ["settings_btn_apply"] = "Apply",
        ["settings_btn_cancel"] = "Cancel",
        ["settings_btn_add_root"] = "Add ROOT",
        ["settings_btn_remove_root"] = "Remove ROOT",
        ["settings_btn_add_subfolder"] = "Add subfolder",
        ["settings_btn_remove_subfolder"] = "Remove subfolder",
        ["settings_group_roots"] = "ROOT mailboxes",
        ["settings_group_subfolders"] = "Inbox subfolders for selected ROOT",
        ["settings_mode_header"] = "Work mode",
        ["settings_mode_pass2"] = "Pass #2: combine mail + PDF",
        ["settings_mode_pass3"] = "Pass #3: save .msg (without PDF)",
        ["settings_mode_cache"] = "Cache: skip already processed PDFs",
        ["settings_tt_pass2"] = "Second matching pass using additional mail context. Improves success rate but may take longer.",
        ["settings_tt_pass3"] = "If no PDF is attached but mail body still looks like remittance, app can save .msg.",
        ["settings_tt_cache"] = "Skips PDFs already processed in the lookback window.",
        ["settings_label_max_pdf"] = "Maximum PDF size (MB)",
        ["settings_tt_max_pdf"] = "Hard size limit. Example: 15 means only PDFs up to 15 MB (inclusive) are processed.",
        ["settings_label_ui_refresh"] = "Refresh UI every N emails",
        ["settings_tt_ui_refresh"] = "How often progress and log are refreshed.",
        ["settings_mailbox_header"] = "Outlook mailboxes (ROOT + Inbox subfolders)",
        ["settings_tt_mailboxes"] = "If ROOT has subfolders configured, only those are scanned. If no subfolders, full Inbox is scanned.",
        ["settings_root_placeholder"] = "Enter ROOT mailbox name",
        ["settings_subfolder_placeholder"] = "Enter subfolder name (for selected ROOT)",

        ["settings_msg_enter_root"] = "Enter ROOT mailbox name.",
        ["settings_msg_root_exists"] = "This ROOT mailbox already exists.",
        ["settings_msg_select_root"] = "Select ROOT first.",
        ["settings_msg_enter_subfolder"] = "Enter subfolder name (e.g. Remittance or Finance\\Remittance).",
        ["settings_msg_subfolder_exists"] = "This subfolder already exists for selected ROOT.",
        ["settings_msg_need_root"] = "Add at least one ROOT mailbox.",

        ["adv_title"] = "Advanced settings",
        ["adv_header"] = "Developer options (change only if you know what you are doing)",
        ["adv_btn_apply"] = "Apply",
        ["adv_btn_cancel"] = "Cancel",
        ["adv_msg_regex_required"] = "Invoice regex cannot be empty.",

        ["adv_lbl_score_min"] = "Minimum score to save",
        ["adv_lbl_score_margin"] = "Minimum margin",
        ["adv_lbl_require_hard"] = "Require hard signal or 2 signals",
        ["adv_lbl_require_total_context"] = "Require TOTAL/PAID context",
        ["adv_lbl_invoice_regex"] = "Invoice regex",
        ["adv_lbl_invoice_window_lines"] = "Invoice context window (lines)",
        ["adv_lbl_single_invoice_score_bonus"] = "Single invoice score bonus",
        ["adv_lbl_single_invoice_margin_bonus"] = "Single invoice margin bonus",
        ["adv_lbl_total_context_window_chars"] = "TOTAL context window (chars)",
        ["adv_lbl_strong_neighbor_lines"] = "Neighbor lines: STRONG TOTAL",
        ["adv_lbl_total_neighbor_lines"] = "Neighbor lines: TOTAL keyword",
        ["adv_lbl_allow_loose_digits"] = "Allow loose digit amount match",
        ["adv_lbl_min_loose_digits"] = "Min digits for loose match",
        ["adv_lbl_strong_score_weight"] = "Score weight: STRONG TOTAL",
        ["adv_lbl_total_score_weight"] = "Score weight: TOTAL context",
        ["adv_lbl_keyword_score_weight"] = "Score weight: Keyword window",
        ["adv_lbl_invoice_penalty_weight"] = "Score penalty: invoice near amount",
        ["adv_lbl_prefer_hard_total"] = "Prefer hard total over higher score",
        ["adv_lbl_block_invoice_context"] = "Block invoice context without strong total",
        ["adv_lbl_strong_keywords"] = "STRONG TOTAL keywords (one per line)",
        ["adv_lbl_total_keywords"] = "TOTAL keywords (one per line)",

        ["adv_tt_score_min"] = "Base acceptance threshold. Higher value = safer but fewer matches.",
        ["adv_tt_score_margin"] = "Required score gap between best and second candidate.",
        ["adv_tt_require_hard"] = "Requires a strong signal (or multiple signals) before saving.",
        ["adv_tt_require_total_context"] = "Amount must appear in payment-total context (TOTAL/PAID).",
        ["adv_tt_invoice_regex"] = "Pattern for invoice number detection, e.g. 3129xxxxx.",
        ["adv_tt_invoice_window_lines"] = "How many lines around amount are checked for invoice context.",
        ["adv_tt_single_invoice_score_bonus"] = "Additional score threshold for true one-invoice remittance cases.",
        ["adv_tt_single_invoice_margin_bonus"] = "Additional margin required for one-invoice exception.",
        ["adv_tt_total_context_window_chars"] = "Character window around TOTAL keywords used for amount capture.",
        ["adv_tt_strong_neighbor_lines"] = "Line proximity for strong total keywords.",
        ["adv_tt_total_neighbor_lines"] = "Line proximity for general total keywords.",
        ["adv_tt_allow_loose_digits"] = "Allows digit-only amount match (useful for OCR issues).",
        ["adv_tt_min_loose_digits"] = "Minimum number of digits required for loose matching.",
        ["adv_tt_strong_score_weight"] = "Score points awarded for strong total signal.",
        ["adv_tt_total_score_weight"] = "Score points awarded for total context signal.",
        ["adv_tt_keyword_score_weight"] = "Score points for keyword-window signal.",
        ["adv_tt_invoice_penalty_weight"] = "Negative score for amounts that look like invoice line items.",
        ["adv_tt_prefer_hard_total"] = "Prefer candidate with hard total signal even if raw score is slightly lower.",
        ["adv_tt_block_invoice_context"] = "Strong safeguard against line-item false matches.",
        ["adv_tt_strong_keywords"] = "List of strong payment-total phrases (one phrase per line).",
        ["adv_tt_total_keywords"] = "List of additional total-context phrases (one phrase per line)."
    };

    private static readonly Dictionary<string, string> Pl = new(StringComparer.OrdinalIgnoreCase)
    {
        ["header_main"] = "GETINGE - REMITTANCE ADVICE MATCHER",
        ["app_title"] = "Getinge Remittance Matcher",
        ["label_lookback"] = "Zakres dni wstecz:",
        ["label_language"] = "Język:",
        ["tooltip_lookback"] = "Ta liczba określa, ile dni wstecz aplikacja ma szukać maili. Przykład: 7 = dziś + poprzednie 7 dni.",
        ["btn_about"] = "Opis",
        ["btn_settings"] = "Ustawienia",
        ["btn_load_settings"] = "Wczytaj ustawienia",
        ["btn_save_settings"] = "Zapisz ustawienia",
        ["btn_fetch_transactions"] = "Pobierz transakcje",
        ["btn_start"] = "Start",
        ["btn_stop"] = "Stop",
        ["status_ready"] = "Gotowe",
        ["status_starting"] = "Uruchamianie...",
        ["status_done"] = "Zakończono",
        ["status_canceled"] = "Przerwano",
        ["status_error"] = "Błąd",
        ["label_live_log"] = "Log na żywo",
        ["log_app_started"] = "Aplikacja uruchomiona.",
        ["log_ready"] = "Aplikacja gotowa do działania.",
        ["log_settings_applied"] = "Ustawienia zastosowane.",
        ["log_canceled"] = "Proces przerwany przez użytkownika.",
        ["log_search_started"] = "Rozpoczęto wyszukiwanie remittance advice.",
        ["log_search_finished"] = "Wyszukiwanie zakończone.",
        ["log_process_summary"] = "Podsumowanie: transakcje={0}, znalezione remitki={1}, nieznalezione={2}.",
        ["log_language_changed_prefix"] = "Zmieniono język na:",
        ["log_settings_changed_prefix"] = "Zmienione ustawienia:",
        ["log_fetch_dev_clicked"] = "Kliknięto 'Pobierz transakcje' (tryb deweloperski).",
        ["log_settings_saved_default"] = "Ustawienia zapisane do domyślnego profilu lokalnego.",
        ["msg_error"] = "Błąd",
        ["msg_warning"] = "Uwaga",
        ["msg_success"] = "Sukces",
        ["msg_bad_settings_prefix"] = "Błędne ustawienia:",
        ["msg_process_error_title"] = "Błąd procesu",
        ["msg_save_fail_prefix"] = "Nie udało się zapisać ustawień:",
        ["msg_load_fail_prefix"] = "Nie udało się wczytać ustawień:",
        ["msg_settings_saved"] = "Ustawienia zostały zapisane.",
        ["msg_fetch_dev_title"] = "Tryb deweloperski",
        ["msg_fetch_dev_body"] = "Ta opcja jest jeszcze w trybie deweloperskim i będzie gotowa do działania w przyszłości.",
        ["msg_about_missing"] = "Nie znaleziono pliku opisu.",
        ["msg_about_open_fail"] = "Nie udało się otworzyć pliku opisu:",
        ["dlg_save_settings"] = "Zapisz ustawienia",
        ["dlg_load_settings"] = "Wczytaj ustawienia",
        ["log_settings_saved_prefix"] = "Ustawienia zapisane:",
        ["log_settings_loaded_prefix"] = "Ustawienia wczytane:",
        ["msg_out_of_range"] = "{0}: wartość poza zakresem {1}-{2}.",

        ["settings_title"] = "Ustawienia",
        ["settings_paths_header"] = "Ścieżki i tryb pracy",
        ["settings_label_feban_folder"] = "Folder CSV FEBAN",
        ["settings_label_feban_file"] = "Nazwa pliku CSV",
        ["settings_label_remit_folder"] = "Folder zapisu remitek",
        ["settings_tt_feban_folder"] = "Folder, w którym leży transactions.csv. Błędna ścieżka zatrzyma start procesu.",
        ["settings_tt_feban_file"] = "Nazwa pliku transakcji. Najczęściej: transactions.csv.",
        ["settings_tt_remit_folder"] = "Folder bazowy zapisu. Podfoldery dat tworzą się automatycznie.",

        ["settings_btn_advanced"] = "Ustawienia zaawansowane",
        ["settings_btn_apply"] = "Zastosuj",
        ["settings_btn_cancel"] = "Anuluj",
        ["settings_btn_add_root"] = "Dodaj ROOT",
        ["settings_btn_remove_root"] = "Usuń ROOT",
        ["settings_btn_add_subfolder"] = "Dodaj podfolder",
        ["settings_btn_remove_subfolder"] = "Usuń podfolder",
        ["settings_group_roots"] = "Skrzynki ROOT",
        ["settings_group_subfolders"] = "Podfoldery Inbox dla wybranego ROOT",
        ["settings_mode_header"] = "Tryb pracy",
        ["settings_mode_pass2"] = "Pass #2: łącz mail + PDF",
        ["settings_mode_pass3"] = "Pass #3: zapis maila .msg (bez PDF)",
        ["settings_mode_cache"] = "Cache: pomijaj już sprawdzone PDF",
        ["settings_tt_pass2"] = "Dodatkowy przebieg dopasowania (mail + PDF). Zwiększa skuteczność, ale może wydłużyć czas.",
        ["settings_tt_pass3"] = "Gdy nie ma PDF, ale treść maila wygląda jak remittance, aplikacja może zapisać .msg.",
        ["settings_tt_cache"] = "Pomija PDF już przetworzone w aktywnym zakresie dni.",
        ["settings_label_max_pdf"] = "Maksymalny rozmiar PDF (MB)",
        ["settings_tt_max_pdf"] = "Twardy limit rozmiaru. Przykład: 15 oznacza, że aplikacja bierze pod uwagę tylko PDF do 15 MB włącznie.",
        ["settings_label_ui_refresh"] = "Odśwież UI co N maili",
        ["settings_tt_ui_refresh"] = "Jak często odświeżać postęp i log.",
        ["settings_mailbox_header"] = "Skrzynki Outlook (ROOT + podfoldery Inbox)",
        ["settings_tt_mailboxes"] = "Gdy ROOT ma podfoldery, skanowane są tylko one. Gdy nie ma podfolderów, skanowany jest cały Inbox ROOT.",
        ["settings_root_placeholder"] = "Wpisz nazwę skrzynki ROOT",
        ["settings_subfolder_placeholder"] = "Wpisz nazwę podfolderu (dla wybranego ROOT)",

        ["settings_msg_enter_root"] = "Podaj nazwę skrzynki ROOT.",
        ["settings_msg_root_exists"] = "Ta skrzynka ROOT już istnieje na liście.",
        ["settings_msg_select_root"] = "Najpierw wybierz ROOT.",
        ["settings_msg_enter_subfolder"] = "Podaj nazwę podfolderu (np. Remittance albo Finanse\\Remittance).",
        ["settings_msg_subfolder_exists"] = "Ten podfolder już istnieje dla wybranego ROOT.",
        ["settings_msg_need_root"] = "Dodaj przynajmniej jedną skrzynkę ROOT.",

        ["adv_title"] = "Ustawienia zaawansowane",
        ["adv_header"] = "Opcje deweloperskie (zmieniaj tylko gdy wiesz, co robisz)",
        ["adv_btn_apply"] = "Zastosuj",
        ["adv_btn_cancel"] = "Anuluj",
        ["adv_msg_regex_required"] = "Regex numeru faktury nie może być pusty.",

        ["adv_lbl_score_min"] = "Minimalny score do zapisu",
        ["adv_lbl_score_margin"] = "Minimalna przewaga (margin)",
        ["adv_lbl_require_hard"] = "Wymagaj hard signal lub 2 sygnały",
        ["adv_lbl_require_total_context"] = "Wymagaj kontekstu TOTAL/PAID",
        ["adv_lbl_invoice_regex"] = "Regex numeru faktury",
        ["adv_lbl_invoice_window_lines"] = "Szerokość bloku faktury (linie)",
        ["adv_lbl_single_invoice_score_bonus"] = "Bonus score (single invoice)",
        ["adv_lbl_single_invoice_margin_bonus"] = "Bonus margin (single invoice)",
        ["adv_lbl_total_context_window_chars"] = "Okno kontekstu TOTAL (znaki)",
        ["adv_lbl_strong_neighbor_lines"] = "Linie sąsiedztwa: STRONG TOTAL",
        ["adv_lbl_total_neighbor_lines"] = "Linie sąsiedztwa: TOTAL keyword",
        ["adv_lbl_allow_loose_digits"] = "Dopuść luźny match cyfr",
        ["adv_lbl_min_loose_digits"] = "Min. cyfr przy luźnym match",
        ["adv_lbl_strong_score_weight"] = "Waga score: STRONG TOTAL",
        ["adv_lbl_total_score_weight"] = "Waga score: TOTAL context",
        ["adv_lbl_keyword_score_weight"] = "Waga score: Keyword window",
        ["adv_lbl_invoice_penalty_weight"] = "Kara score: invoice near amount",
        ["adv_lbl_prefer_hard_total"] = "Preferuj hard total nad wyższym score",
        ["adv_lbl_block_invoice_context"] = "Blokuj invoice context bez strong total",
        ["adv_lbl_strong_keywords"] = "Słowa kluczowe STRONG TOTAL (jedno w linii)",
        ["adv_lbl_total_keywords"] = "Słowa kluczowe TOTAL (jedno w linii)",

        ["adv_tt_score_min"] = "Podstawowy próg akceptacji. Wyżej = bezpieczniej, ale mniej zapisów.",
        ["adv_tt_score_margin"] = "Wymagana różnica punktów między najlepszym a drugim kandydatem.",
        ["adv_tt_require_hard"] = "Wymusza mocny sygnał (lub kombinację sygnałów) przed zapisem.",
        ["adv_tt_require_total_context"] = "Kwota musi wystąpić w kontekście sumy płatności (TOTAL/PAID).",
        ["adv_tt_invoice_regex"] = "Wzorzec wykrywania numeru faktury, np. 3129xxxxx.",
        ["adv_tt_invoice_window_lines"] = "Ile linii wokół kwoty sprawdzać pod kątem kontekstu faktury.",
        ["adv_tt_single_invoice_score_bonus"] = "Dodatkowy próg score dla wyjątków typu jedna faktura.",
        ["adv_tt_single_invoice_margin_bonus"] = "Dodatkowa przewaga punktowa dla wyjątku jednej faktury.",
        ["adv_tt_total_context_window_chars"] = "Ile znaków wokół słów TOTAL analizować.",
        ["adv_tt_strong_neighbor_lines"] = "Ile linii obok mocnych słów TOTAL sprawdzać.",
        ["adv_tt_total_neighbor_lines"] = "Ile linii obok zwykłych słów TOTAL/PAID sprawdzać.",
        ["adv_tt_allow_loose_digits"] = "Pozwala dopasować kwotę po samych cyfrach (pomocne przy słabym OCR).",
        ["adv_tt_min_loose_digits"] = "Minimalna liczba cyfr wymagana dla luźnego dopasowania.",
        ["adv_tt_strong_score_weight"] = "Punkty za bardzo mocny sygnał sumy.",
        ["adv_tt_total_score_weight"] = "Punkty za kontekst TOTAL/PAID.",
        ["adv_tt_keyword_score_weight"] = "Punkty za sygnał z okna słowa kluczowego.",
        ["adv_tt_invoice_penalty_weight"] = "Ujemne punkty za kwoty wyglądające jak line-item faktury.",
        ["adv_tt_prefer_hard_total"] = "Preferuj kandydata z twardym sygnałem TOTAL nawet przy nieco niższym score.",
        ["adv_tt_block_invoice_context"] = "Najsilniejsza ochrona przed false-match kwoty pojedynczej faktury.",
        ["adv_tt_strong_keywords"] = "Lista mocnych fraz płatności (jedna fraza w linii).",
        ["adv_tt_total_keywords"] = "Lista dodatkowych fraz kontekstu płatności (jedna fraza w linii)."
    };

    private static readonly Dictionary<string, string> Es = new(StringComparer.OrdinalIgnoreCase)
    {
        ["label_lookback"] = "Días de búsqueda:",
        ["label_language"] = "Idioma:",
        ["btn_about"] = "Descripción",
        ["btn_settings"] = "Configuración",
        ["btn_load_settings"] = "Cargar ajustes",
        ["btn_save_settings"] = "Guardar ajustes",
        ["btn_fetch_transactions"] = "Descargar transacciones",
        ["btn_start"] = "Iniciar",
        ["btn_stop"] = "Detener",
        ["status_ready"] = "Listo",
        ["status_starting"] = "Iniciando...",
        ["status_done"] = "Finalizado",
        ["status_canceled"] = "Cancelado",
        ["status_error"] = "Error",
        ["label_live_log"] = "Registro en vivo",
        ["settings_title"] = "Configuración",
        ["adv_title"] = "Configuración avanzada"
    };

    private static readonly Dictionary<string, string> Fr = new(StringComparer.OrdinalIgnoreCase)
    {
        ["label_lookback"] = "Jours de recherche:",
        ["label_language"] = "Langue:",
        ["btn_about"] = "Description",
        ["btn_settings"] = "Paramètres",
        ["btn_load_settings"] = "Charger les paramètres",
        ["btn_save_settings"] = "Enregistrer les paramètres",
        ["btn_fetch_transactions"] = "Télécharger les transactions",
        ["btn_start"] = "Démarrer",
        ["btn_stop"] = "Arrêter",
        ["status_ready"] = "Prêt",
        ["status_starting"] = "Démarrage...",
        ["status_done"] = "Terminé",
        ["status_canceled"] = "Annulé",
        ["status_error"] = "Erreur",
        ["label_live_log"] = "Journal en direct",
        ["settings_title"] = "Paramètres",
        ["adv_title"] = "Paramètres avancés"
    };

    private static readonly Dictionary<string, string> De = new(StringComparer.OrdinalIgnoreCase)
    {
        ["label_lookback"] = "Rückblicktage:",
        ["label_language"] = "Sprache:",
        ["btn_about"] = "Beschreibung",
        ["btn_settings"] = "Einstellungen",
        ["btn_load_settings"] = "Einstellungen laden",
        ["btn_save_settings"] = "Einstellungen speichern",
        ["btn_fetch_transactions"] = "Transaktionen laden",
        ["btn_start"] = "Start",
        ["btn_stop"] = "Stopp",
        ["status_ready"] = "Bereit",
        ["status_starting"] = "Starten...",
        ["status_done"] = "Abgeschlossen",
        ["status_canceled"] = "Abgebrochen",
        ["status_error"] = "Fehler",
        ["label_live_log"] = "Live-Protokoll",
        ["settings_title"] = "Einstellungen",
        ["adv_title"] = "Erweiterte Einstellungen"
    };

    private static readonly Dictionary<string, Dictionary<string, string>> Strings = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = En,
        ["pl"] = MergeWithEnglish(Pl),
        ["es"] = MergeWithEnglish(Es),
        ["fr"] = MergeWithEnglish(Fr),
        ["de"] = MergeWithEnglish(De)
    };

    public static IReadOnlyList<LanguageOption> SupportedLanguages { get; } =
    [
        new("pl", "Polski"),
        new("en", "English"),
        new("es", "Español"),
        new("fr", "Français"),
        new("de", "Deutsch")
    ];

    public static string NormalizeLanguageCode(string? code)
    {
        var c = (code ?? "pl").Trim().ToLowerInvariant();
        return Strings.ContainsKey(c) ? c : "pl";
    }

    public static string T(string code, string key)
    {
        var lang = NormalizeLanguageCode(code);
        if (Strings.TryGetValue(lang, out var dict) && dict.TryGetValue(key, out var value))
        {
            return value;
        }

        return En.TryGetValue(key, out var fallback) ? fallback : key;
    }

    private static Dictionary<string, string> MergeWithEnglish(Dictionary<string, string> current)
    {
        var merged = new Dictionary<string, string>(En, StringComparer.OrdinalIgnoreCase);
        foreach (var pair in current)
        {
            merged[pair.Key] = pair.Value;
        }

        return merged;
    }
}
