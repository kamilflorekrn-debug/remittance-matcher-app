# RemittanceMatcherApp - uruchomienie

## 1) Podglad GUI na prywatnym laptopie
- Uruchom: `C:\Users\kamil\Desktop\Kamil\Codex\Remittance Advice\RemittanceMatcherApp\bin\Debug\net8.0-windows\RemittanceMatcherApp.exe`

## 2) Wersja gotowa na firmowy laptop
- Folder publish (self-contained win-x64):
- `C:\Users\kamil\Desktop\Kamil\Codex\Remittance Advice\RemittanceMatcherApp\publish`
- Uruchamiasz plik:
- `C:\Users\kamil\Desktop\Kamil\Codex\Remittance Advice\RemittanceMatcherApp\publish\RemittanceMatcherApp.exe`

## 3) Wymagania na firmowym laptopie
- Outlook desktop (klasyczny)
- Word desktop (uzywany do ekstrakcji tekstu z PDF przez COM)

## 4) Ustawienia
- Ustawienia zapisuja sie automatycznie do:
- `%LOCALAPPDATA%\RemittanceMatcherApp\settings.json`

## 5) Cache juz sprawdzonych PDF
- Cache zapisuje sie w tym samym katalogu co `transactions.csv`:
- `processed_pdf_cache.csv`
