import { Component, OnInit, computed, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { StoreService } from './store.service';
import { AuthService } from './auth.service';
import { PaymentService } from './payment.service';
import {
  RiceType, RiceTypeLabel, OrderStatusLabel, Product,
  PaymentMethod, PaymentDetails,
} from './models';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule],
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App implements OnInit {
  store = inject(StoreService);
  auth = inject(AuthService);
  payments = inject(PaymentService);

  search = signal('');
  activeType = signal<number | 'all'>('all');
  cartOpen = signal(false);
  orderPlaced = signal(false);
  view = signal<'catalog' | 'orders' | 'admin'>('catalog');
  adminTab = signal<'orders' | 'products' | 'reports'>('orders');

  // Product editor state.
  productModalOpen = signal(false);
  editingProductId = signal<string | null>(null);
  pName = '';
  pBrand = '';
  pDescription = '';
  pRiceType = 0;
  pPrice = 0;
  pCostPrice = 0;
  pStock = 0;
  pWeightKg = 1;
  productError = signal('');

  // Notification dropdown state.
  notifOpen = signal(false);

  // Payment modal state.
  payOpen = signal(false);
  payMethod = signal<PaymentMethod>('card');
  payCardType: 'credit' | 'debit' = 'credit';
  payCardNumber = '';
  payCardName = '';
  payCardExpiry = '';
  payCardCvv = '';
  payUpiId = '';
  payError = signal('');
  payBusy = signal(false);

  // Auth modal state.
  authOpen = signal(false);
  authMode = signal<'login' | 'register'>('login');
  authEmail = '';
  authPassword = '';
  authFullName = '';
  authError = signal('');
  authBusy = signal(false);

  readonly riceTypeLabel = RiceTypeLabel;
  readonly orderStatusLabel = OrderStatusLabel;

  readonly isAdmin = computed(() => this.auth.user()?.role === 'Admin');

  // Filter chips shown under the header.
  readonly filters: { label: string; value: number | 'all' }[] = [
    { label: 'All', value: 'all' },
    { label: 'HMT', value: RiceType.HMT },
    { label: 'Sona Masoori', value: RiceType.SonaMasoori },
    { label: 'Basmati', value: RiceType.Basmati },
    { label: 'Single Polish', value: RiceType.SinglePolish },
    { label: 'Jai Shree Ram', value: RiceType.JaiShreeRam },
  ];

  readonly visibleProducts = computed(() => {
    const term = this.search().toLowerCase().trim();
    const type = this.activeType();
    return this.store.products().filter((p) => {
      const matchesType = type === 'all' || p.riceType === type;
      const matchesTerm =
        !term ||
        p.name.toLowerCase().includes(term) ||
        p.brand.toLowerCase().includes(term);
      return matchesType && matchesTerm;
    });
  });

  ngOnInit(): void {
    this.store.loadProducts();
    if (this.auth.isLoggedIn()) {
      this.store.loadOrders();
    }
  }

  onSearch(event: Event): void {
    this.search.set((event.target as HTMLInputElement).value);
  }

  add(product: Product): void {
    this.store.addToCart(product);
  }

  async checkout(): Promise<void> {
    // Require a signed-in customer before placing an order.
    if (!this.auth.isLoggedIn()) {
      this.cartOpen.set(false);
      this.openAuth('login');
      return;
    }
    if (this.store.cart().length === 0) return;

    this.payError.set('');
    this.cartOpen.set(false);

    // If real Razorpay is configured on the backend, use it. Otherwise fall
    // back to the built-in simulated payment form.
    const razorpayEnabled = await this.payments.isRazorpayEnabled();
    if (razorpayEnabled) {
      await this.payWithRazorpay();
    } else {
      this.payOpen.set(true); // open simulated payment modal
    }
  }

  private async payWithRazorpay(): Promise<void> {
    const amount = this.store.cartTotal();
    const email = this.auth.user()?.email ?? 'customer@ricestore.test';
    // A temporary reference id for this checkout.
    const ref = crypto.randomUUID();

    const result = await this.payments.payWithRazorpay(ref, amount, email);
    if (!result.success) {
      // Show the error briefly via the toast area.
      this.payError.set(result.error ?? 'Payment failed.');
      this.payOpen.set(true); // let them retry via the modal
      return;
    }

    // Payment verified — place the order.
    this.store.placeOrder({
      method: 'Razorpay',
      instrument: result.maskedInstrument,
      transactionId: result.transactionId!,
    });
    this.orderPlaced.set(true);
    setTimeout(() => this.orderPlaced.set(false), 4000);
    this.goToOrders();
  }

  submitPayment(): void {
    const details: PaymentDetails = {
      method: this.payMethod(),
      cardType: this.payCardType,
      cardNumber: this.payCardNumber,
      cardName: this.payCardName,
      cardExpiry: this.payCardExpiry,
      cardCvv: this.payCardCvv,
      upiId: this.payUpiId,
    };

    const validationError = this.payments.validate(details);
    if (validationError) {
      this.payError.set(validationError);
      return;
    }

    this.payError.set('');
    this.payBusy.set(true);

    this.payments.process(details).subscribe((result) => {
      this.payBusy.set(false);
      if (!result.success) {
        this.payError.set(result.error ?? 'Payment failed. Please try again.');
        return;
      }

      const methodLabel =
        result.method === 'upi'
          ? 'UPI'
          : `${this.payCardType === 'credit' ? 'Credit' : 'Debit'} Card`;

      this.store.placeOrder({
        method: methodLabel,
        instrument: result.maskedInstrument,
        transactionId: result.transactionId!,
      });

      // Reset form + close, then confirm.
      this.resetPaymentForm();
      this.payOpen.set(false);
      this.orderPlaced.set(true);
      setTimeout(() => this.orderPlaced.set(false), 4000);
      this.goToOrders();
    });
  }

  private resetPaymentForm(): void {
    this.payCardNumber = this.payCardName = this.payCardExpiry = this.payCardCvv = '';
    this.payUpiId = '';
    this.payCardType = 'credit';
    this.payMethod.set('card');
  }

  goToOrders(): void {
    this.view.set('orders');
    // Pull the latest from the Order service (falls back to local if offline).
    this.store.loadOrders();
  }

  goToAdmin(): void {
    this.view.set('admin');
    this.store.loadAllOrders();
  }

  cancelOrder(orderId: string): void {
    if (confirm('Cancel this order? This cannot be undone.')) {
      this.store.deleteOrder(orderId);
    }
  }

  approve(orderId: string): void {
    this.store.approveOrder(orderId);
  }



  reject(orderId: string): void {
    const note = prompt('Reason for rejection (optional):') ?? '';
    this.store.rejectOrder(orderId, note);
  }

  // ----- Admin: product management -----
  readonly riceTypeOptions = [
    { value: RiceType.HMT, label: 'HMT' },
    { value: RiceType.SonaMasoori, label: 'Sona Masoori' },
    { value: RiceType.Basmati, label: 'Basmati' },
    { value: RiceType.SinglePolish, label: 'Single Polish' },
    { value: RiceType.JaiShreeRam, label: 'Jai Shree Ram' },
    { value: RiceType.Other, label: 'Other' },
  ];

  openNewProduct(): void {
    this.editingProductId.set(null);
    this.pName = this.pBrand = this.pDescription = '';
    this.pRiceType = 0;
    this.pPrice = this.pCostPrice = this.pStock = 0;
    this.pWeightKg = 1;
    this.productError.set('');
    this.productModalOpen.set(true);
  }

  openEditProduct(p: Product): void {
    this.editingProductId.set(p.id);
    this.pName = p.name;
    this.pBrand = p.brand;
    this.pDescription = p.description;
    this.pRiceType = p.riceType;
    this.pPrice = p.price;
    this.pCostPrice = p.costPrice;
    this.pStock = p.stock;
    this.pWeightKg = p.weightKg;
    this.productError.set('');
    this.productModalOpen.set(true);
  }

  saveProduct(): void {
    if (!this.pName.trim()) return this.productError.set('Product name is required.');
    if (this.pPrice <= 0) return this.productError.set('Selling price must be greater than 0.');
    if (this.pCostPrice < 0) return this.productError.set('Cost price cannot be negative.');
    if (this.pStock < 0) return this.productError.set('Stock cannot be negative.');

    const data = {
      name: this.pName.trim(),
      brand: this.pBrand.trim(),
      description: this.pDescription.trim(),
      riceType: Number(this.pRiceType),
      price: Number(this.pPrice),
      costPrice: Number(this.pCostPrice),
      stock: Number(this.pStock),
      weightKg: Number(this.pWeightKg),
      imageUrl: '',
      isActive: true,
    };

    const editingId = this.editingProductId();
    if (editingId) {
      this.store.updateProduct({ ...data, id: editingId });
    } else {
      this.store.createProduct(data);
    }
    this.productModalOpen.set(false);
  }

  deleteProduct(p: Product): void {
    if (confirm(`Delete "${p.name}"? This removes it from the store.`)) {
      this.store.deleteProduct(p.id);
    }
  }

  // ----- Auth modal -----
  openAuth(mode: 'login' | 'register'): void {
    this.authMode.set(mode);
    this.authError.set('');
    this.authOpen.set(true);
  }

  submitAuth(): void {
    this.authError.set('');
    this.authBusy.set(true);

    const done = (result: { ok: boolean; error?: string }) => {
      this.authBusy.set(false);
      if (result.ok) {
        this.authOpen.set(false);
        this.authEmail = this.authPassword = this.authFullName = '';
      } else {
        this.authError.set(result.error ?? 'Please try again.');
      }
    };

    if (this.authMode() === 'login') {
      this.auth.login(this.authEmail, this.authPassword).subscribe(done);
    } else {
      this.auth
        .register(this.authEmail, this.authPassword, this.authFullName)
        .subscribe(done);
    }
  }

  logout(): void {
    this.auth.logout();
    this.notifOpen.set(false);
    this.view.set('catalog');
  }

  toggleNotifications(): void {
    const opening = !this.notifOpen();
    this.notifOpen.set(opening);
    // Opening the panel marks everything as read.
    if (opening) {
      setTimeout(() => this.store.markNotificationsRead(), 1500);
    }
  }

  // Deterministic pastel background per rice type (stand-in for product images).
  swatch(type: number): string {
    const colors = ['#e8d3a2', '#c9a06a', '#e4ddc4', '#d8c48f', '#efe7cf', '#dccaa0'];
    return colors[type % colors.length];
  }
}
