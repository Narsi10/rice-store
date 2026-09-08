import { Injectable, computed, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError, of, tap } from 'rxjs';
import {
  Product, CartItem, RiceType, Order, OrderStatus, AppNotification,
} from './models';
import { AuthService } from './auth.service';
import { runtimeConfig } from './app-config';

/**
 * Central store state. Talks to the API Gateway when it's available, but
 * falls back to a built-in sample catalog so the UI renders even before the
 * .NET/Docker backend is running. Cart state is held client-side with signals.
 */
@Injectable({ providedIn: 'root' })
export class StoreService {
  private http = inject(HttpClient);
  private auth = inject(AuthService);
  private get gateway() { return runtimeConfig.gatewayUrl; }

  // One shared list of ALL orders (across customers) so the admin can review
  // them even in offline mode. In backend mode this is populated from the API.
  private readonly ordersKey = 'rice-store-all-orders';
  private readonly notificationsKey = 'rice-store-notifications';
  private readonly productsKey = 'rice-store-products';

  private get customerId(): string {
    return this.auth.user()?.userId ?? 'demo-customer';
  }
  private get customerEmail(): string {
    return this.auth.user()?.email ?? 'demo@ricestore.test';
  }

  readonly products = signal<Product[]>([]);
  readonly cart = signal<CartItem[]>([]);

  /** Every order in the system (admin sees all of these). */
  readonly allOrders = signal<Order[]>(this.loadOrdersFromStorage());
  readonly usingMockData = signal(false);

  /** Orders belonging to the signed-in customer (the "My Orders" view). */
  readonly orders = computed(() =>
    this.allOrders().filter((o) => o.customerId === this.customerId),
  );

  readonly orderCount = computed(() => this.orders().length);

  /** Orders awaiting admin review (Confirmed = placed & awaiting approval). */
  readonly pendingReviewCount = computed(
    () => this.allOrders().filter((o) => o.status === OrderStatus.Confirmed).length,
  );

  /**
   * Sales analytics computed from APPROVED orders. Revenue = what customers
   * paid; cost = what those goods cost the store (from each product's costPrice);
   * profit = revenue - cost. Falls back to a 80%-of-price cost estimate for any
   * item whose product no longer exists in the catalog.
   */
  readonly salesStats = computed(() => {
    const productById = new Map(this.products().map((p) => [p.id, p]));
    const approved = this.allOrders().filter(
      (o) => o.status === OrderStatus.Approved,
    );

    let revenue = 0;
    let cost = 0;
    let unitsSold = 0;

    for (const order of approved) {
      for (const item of order.items) {
        revenue += item.unitPrice * item.quantity;
        const product = productById.get(item.productId);
        const unitCost = product ? product.costPrice : item.unitPrice * 0.8;
        cost += unitCost * item.quantity;
        unitsSold += item.quantity;
      }
    }

    return {
      revenue,
      cost,
      profit: revenue - cost,
      unitsSold,
      approvedOrders: approved.length,
      totalOrders: this.allOrders().length,
    };
  });

  /** Inventory valuation: stock on hand at cost and at retail. */
  readonly inventoryStats = computed(() => {
    const products = this.products();
    const stockUnits = products.reduce((s, p) => s + p.stock, 0);
    const costValue = products.reduce((s, p) => s + p.stock * p.costPrice, 0);
    const retailValue = products.reduce((s, p) => s + p.stock * p.price, 0);
    const lowStock = products.filter((p) => p.stock <= 10).length;
    return { productCount: products.length, stockUnits, costValue, retailValue, lowStock };
  });

  /** All notifications (shared store); filtered to the current customer below. */
  readonly allNotifications = signal<AppNotification[]>(
    this.loadNotificationsFromStorage(),
  );

  /** Notifications for the signed-in customer, newest first. */
  readonly myNotifications = computed(() =>
    this.allNotifications()
      .filter((n) => n.customerId === this.customerId)
      .sort(
        (a, b) =>
          new Date(b.createdOnUtc).getTime() - new Date(a.createdOnUtc).getTime(),
      ),
  );

  readonly unreadCount = computed(
    () => this.myNotifications().filter((n) => !n.read).length,
  );

  readonly cartCount = computed(() =>
    this.cart().reduce((sum, i) => sum + i.quantity, 0),
  );
  readonly cartTotal = computed(() =>
    this.cart().reduce((sum, i) => sum + i.unitPrice * i.quantity, 0),
  );

  constructor() {
    // Keep orders/notifications in sync across browser tabs. If a customer
    // places an order in one tab, the admin tab picks it up automatically.
    if (typeof window !== 'undefined') {
      window.addEventListener('storage', (e) => {
        if (e.key === this.ordersKey) {
          this.allOrders.set(this.loadOrdersFromStorage());
        } else if (e.key === this.notificationsKey) {
          this.allNotifications.set(this.loadNotificationsFromStorage());
        }
      });
    }
  }

  loadProducts(): void {
    // Backend is the single source of truth so admin changes are visible to
    // ALL users (localStorage is per-browser and can't be shared). We only
    // fall back to local/mock data when the backend is unreachable.
    this.http
      .get<Product[]>(`${this.gateway}/api/products`)
      .pipe(catchError(() => of(null)))
      .subscribe((serverProducts) => {
        if (serverProducts) {
          this.usingMockData.set(false);
          this.products.set(serverProducts);
          this.persistProducts();
        } else {
          // Offline: use locally saved products, or the sample set.
          this.usingMockData.set(true);
          const stored = this.loadProductsFromStorage();
          this.products.set(stored.length > 0 ? stored : MOCK_PRODUCTS);
        }
      });
  }

  // ----- Admin product management (CRUD) -----

  createProduct(input: Omit<Product, 'id'>): Product {
    const product: Product = { ...input, id: crypto.randomUUID() };
    // Optimistic add so the admin sees it immediately.
    this.products.set([product, ...this.products()]);
    this.persistProducts();

    // Save to the backend (shared for all users), then re-sync so we pick up
    // the authoritative record (real id) and it becomes visible to customers.
    this.http
      .post<Product>(`${this.gateway}/api/products`, product)
      .pipe(catchError(() => of(null)))
      .subscribe((saved) => {
        if (saved) this.loadProducts();
      });

    return product;
  }

  updateProduct(product: Product): void {
    this.products.set(
      this.products().map((p) => (p.id === product.id ? { ...product } : p)),
    );
    this.persistProducts();

    this.http
      .put(`${this.gateway}/api/products/${product.id}`, product)
      .pipe(catchError(() => of(null)))
      .subscribe(() => this.loadProducts());
  }

  deleteProduct(id: string): void {
    this.products.set(this.products().filter((p) => p.id !== id));
    this.persistProducts();

    this.http
      .delete(`${this.gateway}/api/products/${id}`)
      .pipe(catchError(() => of(null)))
      .subscribe(() => this.loadProducts());
  }

  private persistProducts(): void {
    try {
      localStorage.setItem(this.productsKey, JSON.stringify(this.products()));
    } catch {
      /* ignore */
    }
  }

  private loadProductsFromStorage(): Product[] {
    try {
      const raw = localStorage.getItem(this.productsKey);
      return raw ? (JSON.parse(raw) as Product[]) : [];
    } catch {
      return [];
    }
  }

  addToCart(product: Product): void {
    const items = [...this.cart()];
    const existing = items.find((i) => i.productId === product.id);
    if (existing) {
      existing.quantity += 1;
    } else {
      items.push({
        productId: product.id,
        productName: product.name,
        unitPrice: product.price,
        quantity: 1,
      });
    }
    this.cart.set(items);
  }

  changeQty(productId: string, delta: number): void {
    const items = this.cart()
      .map((i) =>
        i.productId === productId ? { ...i, quantity: i.quantity + delta } : i,
      )
      .filter((i) => i.quantity > 0);
    this.cart.set(items);
  }

  removeFromCart(productId: string): void {
    this.cart.set(this.cart().filter((i) => i.productId !== productId));
  }

  clearCart(): void {
    this.cart.set([]);
  }

  /**
   * Places an order. It lands as "Awaiting Approval" (Confirmed) so an admin
   * can approve or reject it. Tries the Order service via the gateway; if that's
   * not reachable it records the order locally so the flow still works.
   */
  placeOrder(payment?: {
    method: string;
    instrument: string;
    transactionId: string;
  }): Order {
    const items = [...this.cart()];
    const total = this.cartTotal();

    const localOrder: Order = {
      id: crypto.randomUUID(),
      createdOnUtc: new Date().toISOString(),
      customerId: this.customerId,
      customerEmail: this.customerEmail,
      status: OrderStatus.Confirmed, // awaiting admin approval
      totalAmount: total,
      items,
      paymentMethod: payment?.method ?? null,
      paymentInstrument: payment?.instrument ?? null,
      transactionId: payment?.transactionId ?? null,
    };

    // Show the order immediately (optimistic), then reconcile with the server.
    this.allOrders.set([localOrder, ...this.allOrders()]);
    this.persistOrders();
    this.clearCart();

    // Attempt the real backend saga. On success, replace the local record with
    // the authoritative server order (which has the real id + saga status).
    this.http
      .post<Order>(`${this.gateway}/api/orders`, {
        customerId: this.customerId,
        customerEmail: this.customerEmail,
        items: items.map((i) => ({
          productId: i.productId,
          productName: i.productName,
          quantity: i.quantity,
          unitPrice: i.unitPrice,
        })),
        paymentMethod: payment?.method ?? null,
        paymentInstrument: payment?.instrument ?? null,
        transactionId: payment?.transactionId ?? null,
      })
      .pipe(catchError(() => of(null)))
      .subscribe((serverOrder) => {
        if (serverOrder) {
          // Remove the temp local order, add the server one.
          this.upsertOrder(serverOrder, localOrder.id);
          // Then refresh this customer's orders so status reflects the saga.
          setTimeout(() => this.loadOrders(), 1500);
        }
      });

    return localOrder;
  }

  /**
   * Loads the current customer's orders from the Order service. Falls back to
   * locally stored orders when the backend isn't reachable.
   */
  loadOrders(): void {
    this.http
      .get<Order[]>(`${this.gateway}/api/orders/customer/${this.customerId}`)
      .pipe(catchError(() => of(null)))
      .subscribe((serverOrders) => {
        if (!serverOrders) return; // offline: keep local orders

        // Merge this customer's server orders in, replacing their local copies.
        const others = this.allOrders().filter(
          (o) => o.customerId !== this.customerId,
        );
        this.allOrders.set(
          [...others, ...serverOrders].sort(
            (a, b) =>
              new Date(b.createdOnUtc).getTime() -
              new Date(a.createdOnUtc).getTime(),
          ),
        );
        this.persistOrders();
      });
  }

  /** Admin: loads ALL orders from the Order service (falls back to local). */
  loadAllOrders(): void {
    this.http
      .get<Order[]>(`${this.gateway}/api/orders/all`)
      .pipe(catchError(() => of(null)))
      .subscribe((serverOrders) => {
        if (serverOrders) {
          // Backend is the source of truth: REPLACE the list entirely so stale
          // local duplicates don't linger. This is what makes Refresh reliable.
          this.allOrders.set(
            [...serverOrders].sort(
              (a, b) =>
                new Date(b.createdOnUtc).getTime() -
                new Date(a.createdOnUtc).getTime(),
            ),
          );
          this.persistOrders();
        } else {
          // Offline: fall back to whatever is stored locally.
          this.allOrders.set(this.loadOrdersFromStorage());
        }
      });
  }

  /**
   * Customer: delete/cancel one of their own orders. Only allowed while the
   * order is still awaiting approval (Confirmed). Removes it from the shared
   * list so it also disappears from the admin queue, and tries the backend.
   */
  deleteOrder(orderId: string): boolean {
    const order = this.allOrders().find((o) => o.id === orderId);
    // Guard: only the owner may delete, and only while awaiting approval.
    if (
      !order ||
      order.customerId !== this.customerId ||
      order.status !== OrderStatus.Confirmed
    ) {
      return false;
    }

    this.allOrders.set(this.allOrders().filter((o) => o.id !== orderId));
    this.persistOrders();

    // Best-effort backend delete (ignored if the API isn't running).
    this.http
      .delete(`${this.gateway}/api/orders/${orderId}`)
      .pipe(catchError(() => of(null)))
      .subscribe();

    return true;
  }

  /** Admin: approve an order. */
  approveOrder(orderId: string, note = ''): void {
    this.reviewOrder(orderId, OrderStatus.Approved, 'approve', note);
  }

  /** Admin: reject an order. */
  rejectOrder(orderId: string, note = ''): void {
    this.reviewOrder(orderId, OrderStatus.Rejected, 'reject', note);
  }

  private reviewOrder(
    orderId: string,
    newStatus: OrderStatus,
    action: 'approve' | 'reject',
    note: string,
  ): void {
    const reviewedBy = this.auth.user()?.email ?? 'admin';

    const target = this.allOrders().find((o) => o.id === orderId);

    // Optimistic local update so the admin sees it immediately.
    this.allOrders.set(
      this.allOrders().map((o) =>
        o.id === orderId
          ? {
              ...o,
              status: newStatus,
              reviewedBy,
              reviewedOnUtc: new Date().toISOString(),
              reviewNote: note,
            }
          : o,
      ),
    );
    this.persistOrders();

    // Notify the customer who placed the order.
    if (target) {
      const shortId = target.id.substring(0, 8).toUpperCase();
      const approved = newStatus === OrderStatus.Approved;
      this.addNotification({
        customerId: target.customerId,
        orderId: target.id,
        type: approved ? 'approved' : 'rejected',
        message: approved
          ? `Good news! Your order #${shortId} has been approved and is being prepared.`
          : `Your order #${shortId} was rejected${note ? ': ' + note : '.'}`,
      });
    }

    // Try to persist to the backend too.
    this.http
      .post<Order>(`${this.gateway}/api/orders/${orderId}/${action}`, {
        reviewedBy,
        note,
      })
      .pipe(catchError(() => of(null)))
      .subscribe((serverOrder) => {
        if (serverOrder) this.upsertOrder(serverOrder);
      });
  }

  /** Merge a batch of server orders into the shared list. */
  private mergeServerOrders(serverOrders: Order[]): void {
    const map = new Map(this.allOrders().map((o) => [o.id, o]));
    for (const so of serverOrders) map.set(so.id, so);
    this.allOrders.set(
      [...map.values()].sort(
        (a, b) =>
          new Date(b.createdOnUtc).getTime() - new Date(a.createdOnUtc).getTime(),
      ),
    );
    this.persistOrders();
  }

  /** Insert or update a single order (optionally replacing a temp local id). */
  private upsertOrder(order: Order, replaceId?: string): void {
    const list = this.allOrders().filter(
      (o) => o.id !== order.id && o.id !== replaceId,
    );
    this.allOrders.set([order, ...list]);
    this.persistOrders();
  }

  // ----- Notifications -----

  private addNotification(
    n: Omit<AppNotification, 'id' | 'createdOnUtc' | 'read'>,
  ): void {
    const notification: AppNotification = {
      ...n,
      id: crypto.randomUUID(),
      createdOnUtc: new Date().toISOString(),
      read: false,
    };
    this.allNotifications.set([notification, ...this.allNotifications()]);
    this.persistNotifications();
  }

  markNotificationsRead(): void {
    const mine = new Set(this.myNotifications().map((n) => n.id));
    this.allNotifications.set(
      this.allNotifications().map((n) =>
        mine.has(n.id) ? { ...n, read: true } : n,
      ),
    );
    this.persistNotifications();
  }

  clearMyNotifications(): void {
    this.allNotifications.set(
      this.allNotifications().filter((n) => n.customerId !== this.customerId),
    );
    this.persistNotifications();
  }

  private persistNotifications(): void {
    try {
      localStorage.setItem(
        this.notificationsKey,
        JSON.stringify(this.allNotifications()),
      );
    } catch {
      /* ignore */
    }
  }

  private loadNotificationsFromStorage(): AppNotification[] {
    try {
      const raw = localStorage.getItem(this.notificationsKey);
      return raw ? (JSON.parse(raw) as AppNotification[]) : [];
    } catch {
      return [];
    }
  }

  private persistOrders(): void {
    try {
      localStorage.setItem(this.ordersKey, JSON.stringify(this.allOrders()));
    } catch {
      /* localStorage unavailable — ignore */
    }
  }

  private loadOrdersFromStorage(): Order[] {
    try {
      const raw = localStorage.getItem(this.ordersKey);
      return raw ? (JSON.parse(raw) as Order[]) : [];
    } catch {
      return [];
    }
  }
}

const MOCK_PRODUCTS: Product[] = [
  { id: '1', name: 'HMT Premium Rice', description: 'Popular fine-grain HMT rice, soft and non-sticky when cooked.', riceType: RiceType.HMT, brand: 'Sri Lakshmi', price: 620, costPrice: 500, stock: 120, weightKg: 5, imageUrl: '', isActive: true },
  { id: '2', name: 'Sona Masoori Premium', description: 'Lightweight, aromatic South Indian rice for everyday meals.', riceType: RiceType.SonaMasoori, brand: 'Sri Lalitha', price: 850, costPrice: 700, stock: 75, weightKg: 10, imageUrl: '', isActive: true },
  { id: '3', name: 'India Gate Classic Basmati', description: 'Long-grain aged basmati rice, perfect for biryani and pulao.', riceType: RiceType.Basmati, brand: 'India Gate', price: 650, costPrice: 520, stock: 100, weightKg: 5, imageUrl: '', isActive: true },
  { id: '4', name: 'Single Polish Rice', description: 'Lightly polished rice retaining more nutrients and flavor.', riceType: RiceType.SinglePolish, brand: 'Annapurna', price: 560, costPrice: 450, stock: 80, weightKg: 5, imageUrl: '', isActive: true },
  { id: '5', name: 'Jai Shree Ram Rice', description: 'Premium quality Jai Shree Ram brand rice for daily use.', riceType: RiceType.JaiShreeRam, brand: 'Jai Shree Ram', price: 900, costPrice: 740, stock: 60, weightKg: 10, imageUrl: '', isActive: true },
];
