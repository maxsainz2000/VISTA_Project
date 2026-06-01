# Manager Interview Questionnaire
### *Villon Integrated Supply and Trade Application*

| ⏱ Estimated Time | ❓ Total Questions | 📂 Modules Covered |
|---|---|---|
| **45 – 60 minutes** | **37 questions** | **All 4 modules** |

---

| | |
|---|---|
| **Interview Date:** | |
| **Interviewer:** | |
| **Respondent:** | |
| **Position:** | |

---

## 📋 MODULE 1 · Purchasing & Procurement
*(Questions 1 – 9)*

**1. How do you currently decide when it's time to reorder a product?**
> *ⓘ Reorder triggers — manual vs threshold. Key for predictive alerts.*

- [x] Gut feel / shelf check
- [ ] Fixed quantity threshold
- [ ] Set schedule
- [ ] Supplier reminds me

**✎ Answer:** Based on gut feel and shelf check. The manager visually inspects shelves and decides to reorder when stock visibly looks low, without using a fixed quantity threshold or a set schedule.

---

**2. Walk me through your process of ordering from a supplier — from decision to delivery.**
> *ⓘ End-to-end purchasing workflow for SOP Q1.*

**✎ Answer:** The manager first checks which items are running low in stock. He then contacts the supplier to verify stock availability. Once the supplier confirms available items, the manager places the order. After the order is placed, the manager waits for delivery and manually verifies the received goods against the order upon arrival.

---

**3. How do you contact your suppliers, and do prices change every order or are they fixed?**
> *ⓘ Communication channel and pricing stability.*

- [x] Phone/text
- [x] In-person visit
- [ ] Email
- [ ] Fixed price list
- [x] Prices change per order

**✎ Answer:** The manager contacts suppliers primarily via phone or text message, and also through in-person visits. Prices are not fixed and may change per order depending on the supplier.

---

**4. How do you record a purchase order today, and how do you track if it has been delivered?**
> *ⓘ Current PO documentation method — SOP Q1 baseline.*

**✎ Answer:** The manager manually checks each delivered item and records it in a physical record book. Delivery tracking is done by comparing the record book entry against what was actually received.

---

**5. How many active suppliers do you deal with? Do any products come from multiple suppliers, and how do you choose which one to order from?**
> *ⓘ Supplier count and multi-supplier selection logic.*

**✎ Answer:** The manager deals with multiple active suppliers. When choosing which supplier to order from, he considers stock availability, pricing, product quality, and delivery lead time.

---

**6. When you receive goods, how do you verify the quantity and condition — and what happens if something is wrong or missing?**
> *ⓘ Receiving process and discrepancy resolution.*

**✎ Answer:** Upon receiving goods, the manager personally inspects each item, with particular attention to expiration dates. If items are missing or damaged, he contacts the supplier immediately to report the discrepancy and coordinate a resolution.

---

**7. Do you pay suppliers on delivery (cash) or on credit terms (30/60 days)? How do you track what you still owe and when it's due?**
> *ⓘ AP workflow — feeds AP Aging Report design.*

**✎ Answer:** Payment terms depend on the supplier agreement. For credit purchases, the supplier visits the store when payment is due to collect the outstanding amount. Tracking of what is owed is managed informally based on supplier visit schedules rather than a formal accounts payable ledger.

---

**8. Are there months or seasons when purchasing activity is significantly higher? Can you describe the pattern?**
> *ⓘ Seasonal demand — predictive analytics input.*

**✎ Answer:** Yes. Purchasing activity increases significantly during the palay (rice) planting and growing seasons, as farmers purchase more agricultural inputs — such as fertilizers, pesticides, and feeds — when their crops are in the growing stage.

---

**9. What is your biggest frustration with your current purchasing process, and what one piece of information would help you make better purchasing decisions?**
> *ⓘ SOP Q1 + Q2 combined. Open-ended — let them answer freely.*

**✎ Answer:** The biggest frustration is price volatility from suppliers — when a reordered item costs significantly more than the previous purchase, the retail price must be updated manually. To address this, the manager intends to stock up more on high-demand items in advance, especially before price increases occur for the most in-demand products.

---

## 📦 MODULE 2 · Inventory Management
*(Questions 10 – 19)*

**10. How do you currently know what stock you have on hand at any moment? How often do you do a full physical count, and how long does it take?**
> *ⓘ Baseline tracking method — SOP Q1.*

**✎ Answer:** Stock on hand is monitored by maintaining a physical record book that is updated with every incoming and outgoing transaction. A full physical inventory count is conducted regularly — approximately once a week — and takes between one to three hours to complete.

---

**11. Approximately how many distinct products do you carry? How do you categorize them?**
> *ⓘ SKU scale and category structure.*

- [ ] < 50 products
- [x] 50 – 150 products
- [ ] 150 – 300 products
- [ ] > 300 products

**✎ Answer:** The store carries approximately 50 distinct products. They are organized and categorized by type of usage (e.g., pesticides, seeds, feeds, fertilizers).

---

**12. Do any of your products have an expiry date that must be tracked? If yes, which categories?**
> *ⓘ Expiry-date tracking requirement.*

- [x] Yes — pesticides/chemicals
- [x] Yes — seeds
- [x] Yes — feeds
- [ ] No expiry items

**✎ Answer:** Yes. Expiry dates must be tracked for pesticides and chemicals, seeds, and animal feeds.

---

**13. When the same product is bought at different prices across deliveries, which cost do you use when it's sold — the oldest batch's price (FIFO), or an average of all purchase prices?**
> *ⓘ FIFO vs Weighted Average — critical for Inventory Valuation and COGS.*

- [x] FIFO (oldest cost first)
- [ ] Weighted average
- [ ] Not sure / never thought about it

**✎ Answer:** FIFO (First In, First Out) — the oldest batch cost is used first when valuing sold items.

---

**14. Does stock ever go missing, expire, or get damaged before it can be sold? How do you record that, and how significant is the financial loss?**
> *ⓘ Shrinkage frequency and recording — Inventory Shrinkage Report.*

**✎ Answer:** Yes, stock losses occur occasionally due to damage, expiry, or inventory discrepancies. These incidents are recorded in the inventory book with a notation indicating the cause of loss. The financial impact is generally minimal and manageable.

---

**15. For each product, do you have a defined minimum stock level below which you know you must reorder? How long does a typical supplier delivery take after you place an order?**
> *ⓘ Reorder point and lead time — predictive order timing engine.*

**✎ Answer:** Yes, the manager maintains a minimum stock level for each product that signals when it is time to reorder. After placing an order, delivery typically arrives within a few days, though the exact lead time varies by supplier.

---

**16. Can you name 3–5 products that sell significantly faster during certain months? What is the pattern?**
> *ⓘ Seasonal demand examples — predictive analytics model.*

**✎ Answer:** Products that sell significantly faster during certain periods include:
1. UNO animal feeds for pigs — sold consistently every month
2. Insecticides — seasonal, peaking during planting and growing seasons
3. Fertilizers — seasonal, aligned with the palay cropping calendar

---

**17. Describe the last time you ran out of a product a customer needed. What happened and what was the cost?**
> *ⓘ Real stockout incident — SOP Q1 evidence.*

**✎ Answer:** The most recent stockout was caused by higher-than-expected customer demand combined with delayed replenishment from the supplier. The business impact included lost revenue from unfulfilled sales and a risk to customer satisfaction, as some customers may have purchased from a competitor.

---

**18. Do you currently know the total monetary value of all your stock on hand? How do you estimate it?**
> *ⓘ Inventory Valuation Report — current state vs desired.*

**✎ Answer:** The manager does not have an exact, real-time figure for the total monetary value of all stock on hand. Inventory value is not systematically calculated.

---

**19. What inventory information do you currently not have, that you wish you could see on a single screen every day?**
> *ⓘ Dashboard requirements — SOP Q2.*

**✎ Answer:** A single, real-time dashboard screen showing all key inventory information at once — including current stock levels, low-stock alerts, and recent movement — is what the manager most needs but currently lacks.

---

## 🛒 MODULE 3 · Point of Sale (POS) / Sales
*(Questions 20 – 28)*

**20. Walk me through a typical sale — from when a customer arrives to when they leave with goods. What paperwork or recording happens?**
> *ⓘ End-to-end sales workflow — SOP Q1 baseline.*

**✎ Answer:** A typical sale begins when a customer arrives and inquires about a product. The manager assists by showing available items and helps the customer choose based on their need. Once the customer decides, the manager confirms availability and price, collects payment (cash, GCash, bank transfer, or credit), and issues an official receipt. For credit transactions, the amount is recorded in the credit ledger. The customer leaves with the goods after payment or credit confirmation.

---

**21. What payment methods do your customers use? Do you extend credit ('utang') to any customers — and if so, how many, and how do you track what they owe?**
> *ⓘ Payment modes + AR scope.*

- [ ] Cash only
- [x] GCash / e-wallet
- [x] Bank transfer
- [x] Credit / charge account

**✎ Answer:** Customers pay using cash, GCash/e-wallet, bank transfer, or credit (charge account / utang). Credit is extended to select customers, and outstanding balances are tracked manually.

---

**22. For credit customers — do you have a credit limit per customer, and how do you follow up on overdue balances?**
> *ⓘ AR aging and collection process — feeds AR Aging Report.*

**✎ Answer:** There is no formal credit limit per customer. However, the manager does not allow a customer to incur additional credit if they have an existing unpaid balance. Collections are followed up when the customer visits the store; if the customer does not come in, the manager personally visits them to collect payment.

---

**23. Is your price list fixed, or do prices vary by customer, quantity, or season? Do you offer discounts to regular or bulk buyers?**
> *ⓘ Pricing and discount rules — POS and Sales Reports.*

**✎ Answer:** The price list is generally fixed to maintain pricing consistency. Discounts are offered to bulk buyers and regular customers to maintain long-term business relationships.

---

**24. How do you handle a product return or exchange from a customer? How is it recorded and how does it affect the original sale?**
> *ⓘ Sales Returns & Allowances process.*

**✎ Answer:** When a customer returns a product, the manager first inspects the item and confirms the reason for the return. If the return is deemed valid, the item is replaced. Refunds or adjustments to the original sale record are handled on a case-by-case basis, though no formal sales return recording system is currently in place.

---

**25. Are you registered with the BIR? Do you issue official receipts, and do you charge VAT on your products? Are any products VAT-exempt?**
> *ⓘ Tax compliance and VAT Report requirements.*

- [x] BIR registered — issues ORs
- [ ] Not registered
- [ ] VAT-registered (12%)
- [ ] Some products VAT-exempt

**✎ Answer:** Yes, the store is BIR-registered and issues Official Receipts (ORs) for transactions. VAT registration status was not specified during the interview.

---

**26. Without looking anything up, can you answer: What was your top-selling product last month? Which day of the week are you busiest? Which customer buys the most from you?**
> *ⓘ Tests existing sales analytics — SOP Q1.*

- [x] Can answer all 3
- [ ] Can answer some
- [ ] Cannot answer any — no records

**✎ Answer:** The manager can answer all three questions without referring to records:
1. Top-selling product last month — insecticides
2. Busiest day of the week — Sunday, which is the local market day
3. Customer who buys the most — insecticides and feeds are the top-selling product categories, though a specific top customer by name was not identified

---

**27. Have you ever had a cashiering error or a customer disputed a price? How was it resolved without a proper transaction record?**
> *ⓘ Manual error pain point — SOP Q1.*

**✎ Answer:** Yes, cashiering errors and price disputes with customers have occurred. The manager resolves these by calmly reviewing the relevant records — price list, receipts, or prior transaction notes — and discussing the issue directly with the customer to reach a clarification.

---

**28. What 3 numbers would you most want to see on an automatic daily sales summary? And what one warning alert would be most valuable for your sales operations?**
> *ⓘ KPI + prescriptive alert design — SOP Q2.*

**✎ Answer:** The three most important numbers for a daily sales summary are:
1. Total sales for the day
2. Number of transactions
3. Remaining stock levels

The most valuable warning alert would be a low-stock alert for fast-moving items, enabling the manager to reorder before running out.

---

## 📒 MODULE 4 · Accounting & Financial Reporting
*(Questions 29 – 37)*

**29. Who handles bookkeeping today, and what tool do they use — cashbook, spreadsheet, or nothing formal?**
> *ⓘ Current accounting baseline — SOP Q1.*

- [ ] Manager does it
- [x] Owner does it
- [ ] External bookkeeper/accountant
- [ ] No formal bookkeeping

**✎ Answer:** The store owner personally handles bookkeeping. Records are maintained manually.

---

**30. What financial information does the owner ask to see, how often, and how is it currently delivered?**
> *ⓘ Owner reporting cadence — Owner KPI dashboard design.*

**✎ Answer:** The owner typically requests daily or weekly reports covering total sales, revenue, expenses, and inventory status. These reports are currently delivered manually — either as written summaries or through messaging updates (e.g., text or chat).

---

**31. Do you currently know your gross profit margin on individual products? Are there products you suspect are not actually profitable?**
> *ⓘ Product-level profitability awareness.*

**✎ Answer:** The manager has a general sense of overall business profitability but does not consistently track gross profit margin at the individual product level. For some products, lower profitability is suspected based on supplier cost trends, retail pricing, and slow inventory turnover — but a detailed per-product margin breakdown is not currently available.

---

**32. Have there been months where the business was profitable on paper but you didn't have enough cash to restock or pay suppliers? Describe that situation.**
> *ⓘ Cash flow gap — justifies the Cash Flow Statement.*

**✎ Answer:** Yes. Cash flow shortfalls have occurred when a large portion of sales are on credit and customer payments are delayed, while supplier payment obligations come due sooner. This creates a gap between reported revenue and actual available cash.

---

**33. If shown a standard Income Statement right now, could you interpret it on your own — or would you need it explained?**
> *ⓘ Financial literacy level — determines depth of auto-interpretation feature in Accounting module.*

- [ ] Yes, I can read it fully
- [x] Partially — some parts
- [ ] No, I would need help

**✎ Answer:** The manager can partially interpret a standard Income Statement — familiar with some sections but would need assistance understanding all parts.

---

**34. What 3 things would you most want a monthly financial report to highlight or explain in plain language?**
> *ⓘ Descriptive analytics — report interpretation feature design.*

**✎ Answer:** The three things most wanted in a monthly financial report are:
1. Actual profit or loss for the month — to clearly see if the business is earning
2. Cash flow status — to understand actual liquidity
3. Inventory movement — to see what sold, what remained, and what needs replenishing

---

**35. Are there early warning signs of financial trouble you watch for — like slow sales, a big supplier bill due, or a customer with a large unpaid balance?**
> *ⓘ Prescriptive alert triggers — Accounting module.*

**✎ Answer:** Yes. Early warning signs the manager watches for include: slow or declining sales, large supplier bills coming due, and customers with significant unpaid balances that remain uncollected.

---

**36. Has Villon Farm Supply ever been audited by the BIR? How long do you keep financial records — receipts, invoices, ledger pages?**
> *ⓘ Audit history and record retention — DA10 compliance.*

**✎ Answer:** Yes, Villon Farm Supply has been audited by the BIR. Financial records — including receipts, invoices, and ledger entries — are retained for a long-term period to ensure proper tracking and compliance with regulatory requirements.

---

**37. If the new system could change ONE thing about how you currently manage the financial side of the business, what would have the biggest positive impact?**
> *ⓘ Highest-value financial requirement — SOP Q2.*

**✎ Answer:** The single most impactful change would be real-time, automated visibility of both cash flow and profitability. Currently, financial tracking is done manually and updated only after transactions occur, making it difficult to see the true financial position instantly. A system that automatically updates sales, expenses, receivables, payables, and inventory in one integrated platform would greatly improve decision-making.

---

*Thank you for your time. Your answers will directly shape the system built for Villon Farm Supply.*

| Interviewer Signature | | Respondent Signature |
|---|---|---|
| | | |

---

*CONFIDENTIAL — FOR SYSTEM DEVELOPMENT USE ONLY*
