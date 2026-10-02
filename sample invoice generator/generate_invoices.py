from reportlab.lib.pagesizes import letter
from reportlab.pdfgen import canvas

invoices = [
    {"vendor": "Acme Electronics Pvt Ltd", "num": "INV-2026-001", "date": "2026-01-15",
     "items": [("iPhone 15 Pro", 1, 129999), ("Samsung Galaxy S24", 2, 79999)]},
    {"vendor": "Textile World", "num": "TW-2026-042", "date": "2026-01-22",
     "items": [("Levi's Jeans", 50, 3999), ("Nike Dri-FIT T-Shirt", 100, 1999)]},
    {"vendor": "Books & Beyond", "num": "BB-2026-007", "date": "2026-02-03",
     "items": [("Clean Code", 20, 2499), ("Pragmatic Programmer", 15, 2799)]},
]

for inv in invoices:
    filename = f"{inv['num'].replace('/', '-')}.pdf"
    c = canvas.Canvas(filename, pagesize=letter)
    c.setFont("Helvetica-Bold", 20)
    c.drawString(50, 750, inv["vendor"])
    c.setFont("Helvetica", 11)
    c.drawString(50, 730, f"Invoice Number: {inv['num']}")
    c.drawString(50, 715, f"Invoice Date: {inv['date']}")
    c.drawString(50, 700, "Due Date: 2026-03-15")

    y = 650
    c.setFont("Helvetica-Bold", 12)
    c.drawString(50, y, "Description")
    c.drawString(350, y, "Qty")
    c.drawString(420, y, "Unit Price")
    c.drawString(510, y, "Total")
    y -= 20

    subtotal = 0
    c.setFont("Helvetica", 11)
    for desc, qty, price in inv["items"]:
        total = qty * price
        subtotal += total
        c.drawString(50, y, desc)
        c.drawString(350, y, str(qty))
        c.drawString(420, y, f"{price:,.2f}")
        c.drawString(510, y, f"{total:,.2f}")
        y -= 18

    tax = subtotal * 0.18
    grand = subtotal + tax

    y -= 20
    c.setFont("Helvetica-Bold", 11)
    c.drawString(420, y, "Subtotal:")
    c.drawString(510, y, f"{subtotal:,.2f}")
    y -= 18
    c.drawString(420, y, "Tax (18%):")
    c.drawString(510, y, f"{tax:,.2f}")
    y -= 18
    c.setFont("Helvetica-Bold", 13)
    c.drawString(420, y, "Total:")
    c.drawString(510, y, f"{grand:,.2f}")

    c.save()
    print(f"Generated {filename}")