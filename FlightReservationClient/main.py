import tkinter as tk
from tkinter import ttk, messagebox, filedialog
import base64
import threading
from datetime import datetime

from soap_client import FlightSoapClient

WSDL_URL_HTTP = "http://localhost:5000/FlightService.asmx?wsdl"
WSDL_URL_HTTPS = "https://localhost:5001/FlightService.asmx?wsdl"


class FlightReservationApp:
    def __init__(self, root: tk.Tk):
        self.root = root
        self.root.title("System Rezerwacji Biletów Lotniczych")
        self.root.geometry("950x700")
        self.root.minsize(900, 650)

        self.client: FlightSoapClient | None = None
        self.flights_data: list = []

        style = ttk.Style()
        style.theme_use("clam")
        style.configure("Title.TLabel", font=("Segoe UI", 14, "bold"))
        style.configure("Header.TLabel", font=("Segoe UI", 11, "bold"))
        style.configure("Treeview", rowheight=26, font=("Segoe UI", 9))
        style.configure("Treeview.Heading", font=("Segoe UI", 9, "bold"))

        self._build_connection_frame()
        self.notebook = ttk.Notebook(root)
        self.notebook.pack(fill=tk.BOTH, expand=True, padx=10, pady=(0, 10))
        self._build_flights_tab()
        self._build_buy_tab()
        self._build_reservation_tab()
        self.status_var = tk.StringVar(value="Nie połączono z serwisem.")
        ttk.Label(root, textvariable=self.status_var, relief=tk.SUNKEN, anchor=tk.W).pack(
            fill=tk.X, side=tk.BOTTOM, padx=10, pady=(0, 5))

    # --- Connection ---
    def _build_connection_frame(self):
        frame = ttk.LabelFrame(self.root, text="Połączenie z serwisem", padding=8)
        frame.pack(fill=tk.X, padx=10, pady=10)

        ttk.Label(frame, text="WSDL URL:").grid(row=0, column=0, sticky=tk.W, padx=(0, 5))
        self.url_var = tk.StringVar(value=WSDL_URL_HTTP)
        ttk.Entry(frame, textvariable=self.url_var, width=60).grid(row=0, column=1, sticky=tk.EW, padx=5)

        self.use_https_var = tk.BooleanVar(value=False)
        ttk.Checkbutton(frame, text="HTTPS (SSL/TLS)", variable=self.use_https_var,
                        command=self._toggle_https).grid(row=0, column=2, padx=5)

        self.connect_btn = ttk.Button(frame, text="Połącz", command=self._connect)
        self.connect_btn.grid(row=0, column=3, padx=5)
        frame.columnconfigure(1, weight=1)

    def _toggle_https(self):
        if self.use_https_var.get():
            self.url_var.set(WSDL_URL_HTTPS)
        else:
            self.url_var.set(WSDL_URL_HTTP)

    def _connect(self):
        url = self.url_var.get().strip()
        if not url:
            messagebox.showwarning("Uwaga", "Podaj URL WSDL.")
            return
        self.connect_btn.config(state=tk.DISABLED)
        self.status_var.set("Łączenie...")
        threading.Thread(target=self._connect_thread, args=(url,), daemon=True).start()

    def _connect_thread(self, url: str):
        try:
            self.client = FlightSoapClient(url, verify_ssl=not self.use_https_var.get())
            self.root.after(0, self._on_connected)
        except Exception as e:
            self.root.after(0, lambda: self._on_connect_error(str(e)))

    def _on_connected(self):
        self.connect_btn.config(state=tk.NORMAL)
        proto = "HTTPS" if self.use_https_var.get() else "HTTP"
        self.status_var.set(f"Połączono z serwisem ({proto}).")
        messagebox.showinfo("Sukces", "Połączono z serwisem SOAP.")
        self._load_all_flights()

    def _on_connect_error(self, msg: str):
        self.connect_btn.config(state=tk.NORMAL)
        self.status_var.set("Błąd połączenia.")
        messagebox.showerror("Błąd połączenia", f"Nie udało się połączyć:\n{msg}")

    # --- Flights tab ---
    def _build_flights_tab(self):
        tab = ttk.Frame(self.notebook, padding=10)
        self.notebook.add(tab, text="  Wyszukiwanie lotów  ")

        search_frame = ttk.LabelFrame(tab, text="Filtruj loty", padding=8)
        search_frame.pack(fill=tk.X, pady=(0, 10))

        ttk.Label(search_frame, text="Miasto od:").grid(row=0, column=0, sticky=tk.W, padx=2)
        self.search_from = tk.StringVar()
        ttk.Entry(search_frame, textvariable=self.search_from, width=20).grid(row=0, column=1, padx=5)

        ttk.Label(search_frame, text="Miasto do:").grid(row=0, column=2, sticky=tk.W, padx=2)
        self.search_to = tk.StringVar()
        ttk.Entry(search_frame, textvariable=self.search_to, width=20).grid(row=0, column=3, padx=5)

        ttk.Label(search_frame, text="Data (RRRR-MM-DD):").grid(row=0, column=4, sticky=tk.W, padx=2)
        self.search_date = tk.StringVar()
        ttk.Entry(search_frame, textvariable=self.search_date, width=14).grid(row=0, column=5, padx=5)

        ttk.Button(search_frame, text="Szukaj", command=self._search_flights).grid(row=0, column=6, padx=5)
        ttk.Button(search_frame, text="Pokaż wszystkie", command=self._load_all_flights).grid(row=0, column=7, padx=5)

        cols = ("id", "nr_lotu", "skad", "dokad", "data", "godzina", "cena", "miejsca")
        self.flights_tree = ttk.Treeview(tab, columns=cols, show="headings", selectmode="browse")
        for col, heading, width in [
            ("id", "ID", 40), ("nr_lotu", "Nr lotu", 80), ("skad", "Skąd", 110),
            ("dokad", "Dokąd", 110), ("data", "Data", 100), ("godzina", "Godz.", 60),
            ("cena", "Cena (PLN)", 90), ("miejsca", "Wolne miejsca", 100)
        ]:
            self.flights_tree.heading(col, text=heading)
            self.flights_tree.column(col, width=width, anchor=tk.CENTER)

        vsb = ttk.Scrollbar(tab, orient="vertical", command=self.flights_tree.yview)
        self.flights_tree.configure(yscrollcommand=vsb.set)
        self.flights_tree.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        vsb.pack(side=tk.RIGHT, fill=tk.Y)

    def _load_all_flights(self):
        if not self.client:
            return
        self.status_var.set("Pobieranie lotów...")
        threading.Thread(target=self._fetch_flights_thread, args=(None,), daemon=True).start()

    def _search_flights(self):
        if not self.client:
            messagebox.showwarning("Uwaga", "Najpierw połącz się z serwisem.")
            return
        self.status_var.set("Wyszukiwanie...")
        search = {
            "city_from": self.search_from.get().strip() or None,
            "city_to": self.search_to.get().strip() or None,
            "date": self.search_date.get().strip() or None,
        }
        threading.Thread(target=self._fetch_flights_thread, args=(search,), daemon=True).start()

    def _fetch_flights_thread(self, search):
        try:
            if search:
                flights = self.client.search_flights(
                    search.get("city_from"), search.get("city_to"), search.get("date"))
            else:
                flights = self.client.get_all_flights()
            self.root.after(0, lambda: self._populate_flights(flights))
        except Exception as e:
            self.root.after(0, lambda: self._show_error("Błąd pobierania lotów", str(e)))

    def _populate_flights(self, flights):
        self.flights_data = flights
        for row in self.flights_tree.get_children():
            self.flights_tree.delete(row)
        for f in flights:
            dep_date = f.get("DepartureDate", "")
            if isinstance(dep_date, datetime):
                dep_date = dep_date.strftime("%Y-%m-%d")
            elif dep_date and "T" in str(dep_date):
                dep_date = str(dep_date).split("T")[0]
            self.flights_tree.insert("", tk.END, values=(
                f.get("Id", ""), f.get("FlightNumber", ""),
                f.get("CityFrom", ""), f.get("CityTo", ""),
                dep_date, f.get("DepartureTime", ""),
                f"{f.get('Price', 0):.2f}", f.get("AvailableSeats", 0)
            ))
        self.status_var.set(f"Znaleziono {len(flights)} lotów.")

    # --- Buy ticket tab ---
    def _build_buy_tab(self):
        tab = ttk.Frame(self.notebook, padding=10)
        self.notebook.add(tab, text="  Kup bilet  ")

        form = ttk.LabelFrame(tab, text="Dane zakupu", padding=15)
        form.pack(fill=tk.X, pady=(0, 10))

        ttk.Label(form, text="ID lotu:").grid(row=0, column=0, sticky=tk.W, pady=4)
        self.buy_flight_id = tk.StringVar()
        ttk.Entry(form, textvariable=self.buy_flight_id, width=10).grid(row=0, column=1, sticky=tk.W, padx=5)
        ttk.Label(form, text="(wybierz z zakładki 'Wyszukiwanie lotów')").grid(row=0, column=2, sticky=tk.W)

        ttk.Label(form, text="Imię i nazwisko:").grid(row=1, column=0, sticky=tk.W, pady=4)
        self.buy_name = tk.StringVar()
        ttk.Entry(form, textvariable=self.buy_name, width=40).grid(row=1, column=1, columnspan=2, sticky=tk.W, padx=5)

        ttk.Label(form, text="Email:").grid(row=2, column=0, sticky=tk.W, pady=4)
        self.buy_email = tk.StringVar()
        ttk.Entry(form, textvariable=self.buy_email, width=40).grid(row=2, column=1, columnspan=2, sticky=tk.W, padx=5)

        ttk.Button(form, text="Kup bilet", command=self._buy_ticket).grid(row=3, column=1, sticky=tk.W, pady=10, padx=5)

        result_frame = ttk.LabelFrame(tab, text="Wynik zakupu", padding=15)
        result_frame.pack(fill=tk.BOTH, expand=True)

        self.buy_result_text = tk.Text(result_frame, height=10, wrap=tk.WORD, font=("Consolas", 10))
        self.buy_result_text.pack(fill=tk.BOTH, expand=True)

    def _buy_ticket(self):
        if not self.client:
            messagebox.showwarning("Uwaga", "Najpierw połącz się z serwisem.")
            return
        flight_id = self.buy_flight_id.get().strip()
        name = self.buy_name.get().strip()
        email = self.buy_email.get().strip()
        if not flight_id or not name or not email:
            messagebox.showwarning("Uwaga", "Wypełnij wszystkie pola.")
            return
        try:
            flight_id_int = int(flight_id)
        except ValueError:
            messagebox.showwarning("Uwaga", "ID lotu musi być liczbą.")
            return

        self.status_var.set("Kupowanie biletu...")
        threading.Thread(target=self._buy_ticket_thread,
                         args=(flight_id_int, name, email), daemon=True).start()

    def _buy_ticket_thread(self, flight_id, name, email):
        try:
            result = self.client.buy_ticket(flight_id, name, email)
            self.root.after(0, lambda: self._show_buy_result(result))
        except Exception as e:
            self.root.after(0, lambda: self._show_error("Błąd zakupu", str(e)))

    def _show_buy_result(self, result):
        self.buy_result_text.delete("1.0", tk.END)
        success = result.get("Success", False)
        message = result.get("Message", "")
        res_num = result.get("ReservationNumber", "")
        lines = [
            f"Status: {'SUKCES' if success else 'BŁĄD'}",
            f"Wiadomość: {message}",
        ]
        if res_num:
            lines.append(f"Numer rezerwacji: {res_num}")
            lines.append("")
            lines.append("Zapisz numer rezerwacji! Możesz go użyć w zakładce")
            lines.append("'Sprawdź rezerwację' aby pobrać potwierdzenie PDF.")
        self.buy_result_text.insert("1.0", "\n".join(lines))
        status_msg = f"Bilet kupiony: {res_num}" if success else f"Błąd: {message}"
        self.status_var.set(status_msg)

    # --- Reservation tab ---
    def _build_reservation_tab(self):
        tab = ttk.Frame(self.notebook, padding=10)
        self.notebook.add(tab, text="  Sprawdź rezerwację  ")

        check_frame = ttk.LabelFrame(tab, text="Sprawdź rezerwację", padding=10)
        check_frame.pack(fill=tk.X, pady=(0, 10))

        ttk.Label(check_frame, text="Numer rezerwacji:").grid(row=0, column=0, sticky=tk.W, padx=(0, 5))
        self.res_number_var = tk.StringVar()
        ttk.Entry(check_frame, textvariable=self.res_number_var, width=30).grid(row=0, column=1, padx=5)
        ttk.Button(check_frame, text="Sprawdź", command=self._check_reservation).grid(row=0, column=2, padx=5)
        ttk.Button(check_frame, text="Pobierz PDF", command=self._download_pdf).grid(row=0, column=3, padx=5)

        detail_frame = ttk.LabelFrame(tab, text="Szczegóły rezerwacji", padding=15)
        detail_frame.pack(fill=tk.BOTH, expand=True)

        self.res_detail_text = tk.Text(detail_frame, height=15, wrap=tk.WORD, font=("Consolas", 10))
        self.res_detail_text.pack(fill=tk.BOTH, expand=True)

    def _check_reservation(self):
        if not self.client:
            messagebox.showwarning("Uwaga", "Najpierw połącz się z serwisem.")
            return
        res_num = self.res_number_var.get().strip()
        if not res_num:
            messagebox.showwarning("Uwaga", "Podaj numer rezerwacji.")
            return
        self.status_var.set("Sprawdzanie rezerwacji...")
        threading.Thread(target=self._check_reservation_thread, args=(res_num,), daemon=True).start()

    def _check_reservation_thread(self, res_num):
        try:
            result = self.client.check_reservation(res_num)
            self.root.after(0, lambda: self._show_reservation_details(result))
        except Exception as e:
            self.root.after(0, lambda: self._show_error("Błąd", str(e)))

    def _show_reservation_details(self, details):
        self.res_detail_text.delete("1.0", tk.END)
        if not details.get("Found", False):
            self.res_detail_text.insert("1.0", "Nie znaleziono rezerwacji o podanym numerze.")
            self.status_var.set("Rezerwacja nie znaleziona.")
            return

        dep_date = details.get("DepartureDate", "")
        if isinstance(dep_date, datetime):
            dep_date = dep_date.strftime("%Y-%m-%d")
        elif dep_date and "T" in str(dep_date):
            dep_date = str(dep_date).split("T")[0]

        purchase_date = details.get("PurchaseDate", "")
        if isinstance(purchase_date, datetime):
            purchase_date = purchase_date.strftime("%Y-%m-%d %H:%M")

        lines = [
            "=" * 45,
            "       SZCZEGÓŁY REZERWACJI",
            "=" * 45,
            f"  Nr rezerwacji:  {details.get('ReservationNumber', '')}",
            f"  Pasażer:        {details.get('PassengerName', '')}",
            f"  Email:          {details.get('PassengerEmail', '')}",
            "",
            f"  Nr lotu:        {details.get('FlightNumber', '')}",
            f"  Trasa:          {details.get('CityFrom', '')} → {details.get('CityTo', '')}",
            f"  Data wylotu:    {dep_date}",
            f"  Godzina:        {details.get('DepartureTime', '')}",
            f"  Miejsce:        {details.get('SeatNumber', '')}",
            "",
            f"  Data zakupu:    {purchase_date}",
            "=" * 45,
        ]
        self.res_detail_text.insert("1.0", "\n".join(lines))
        self.status_var.set(f"Wyświetlono rezerwację: {details.get('ReservationNumber', '')}")

    def _download_pdf(self):
        if not self.client:
            messagebox.showwarning("Uwaga", "Najpierw połącz się z serwisem.")
            return
        res_num = self.res_number_var.get().strip()
        if not res_num:
            messagebox.showwarning("Uwaga", "Podaj numer rezerwacji.")
            return
        self.status_var.set("Pobieranie PDF (MTOM)...")
        threading.Thread(target=self._download_pdf_thread, args=(res_num,), daemon=True).start()

    def _download_pdf_thread(self, res_num):
        try:
            result = self.client.get_reservation_pdf(res_num)
            self.root.after(0, lambda: self._save_pdf(result))
        except Exception as e:
            self.root.after(0, lambda: self._show_error("Błąd PDF", str(e)))

    def _save_pdf(self, result):
        if not result.get("Success", False):
            messagebox.showerror("Błąd", result.get("Message", "Nie udało się pobrać PDF."))
            return

        pdf_data = result.get("PdfData")
        if not pdf_data:
            messagebox.showerror("Błąd", "Brak danych PDF w odpowiedzi.")
            return

        if isinstance(pdf_data, str):
            pdf_bytes = base64.b64decode(pdf_data)
        elif isinstance(pdf_data, bytes):
            pdf_bytes = pdf_data
        else:
            pdf_bytes = bytes(pdf_data)

        filename = result.get("FileName", "bilet.pdf")
        save_path = filedialog.asksaveasfilename(
            defaultextension=".pdf",
            filetypes=[("PDF files", "*.pdf")],
            initialfile=filename,
            title="Zapisz potwierdzenie PDF"
        )
        if save_path:
            with open(save_path, "wb") as f:
                f.write(pdf_bytes)
            self.status_var.set(f"PDF zapisany: {save_path}")
            messagebox.showinfo("Sukces", f"Potwierdzenie PDF zapisane:\n{save_path}")

    def _show_error(self, title, msg):
        self.status_var.set(f"Błąd: {msg[:80]}")
        messagebox.showerror(title, msg)


def main():
    root = tk.Tk()
    FlightReservationApp(root)
    root.mainloop()


if __name__ == "__main__":
    main()
